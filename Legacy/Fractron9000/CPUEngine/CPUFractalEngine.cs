#region License
/*
    Fractron 9000
    Copyright (C) 2009 Michael J. Thiesen
	http://fractron9000.sourceforge.net
	mike@thiesen.us

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

using MTUtil;

namespace Fractron9000.CPUEngine
{
	unsafe public class CPUFractalEngine : FractalEngine
	{
		public const int AALevel = 2;
		public const int DotsPerIterator = 8192;
		public const int MaxUseableIterators = 65536 / DotsPerIterator;

		public const int WarmupIterationCount = 32;

		public const float Tone_C1 = (1.0f/2.0f);
		public const float Tone_C2 = (64.0f/1.0f);

		public const float UpScaleFactor = 1024.0f;
		public const float DownScaleFactor = 1.0f / UpScaleFactor;


		Random rand = new Random();

		int xRes;
		int yRes;
		Affine2D vpsTransform = Affine2D.Identity;
		
		NativeFractal* fractal;
		NativeBranch*  branches;
		float*         variWeights;
		Affine2D       viewTransform;
		Affine2D       projTransform;

		Iterator[] iterators;
		NativeGlobalStatEntry globalStats;
		Dot* dots;
		ushort* dotIndicies;

		int accumTexID = 0;
		int accumFBO = 0;
		int paletteTexID = 0;
		int outputTexID = 0;
		int outputFBO = 0;
		int tonemapVertShader = 0;
		int tonemapFragShader = 0;
		int tonemapProgram = 0;

		int accumSamplerLocation = 0;
		int scaleConstantLocation = 0;
		int brightnessLocation = 0;
		int invGammaLocation = 0;
		int vibrancyLocation = 0;
		int upScaleFactorLocation = 0;
		int subStepXLocation = 0;
		int subStepYLocation = 0;

		public override int XRes{
			get{ return xRes; }
		}
		public override int YRes{
			get{ return yRes; }
		}
		public int IteratorCount{
			get{ return iterators == null ? 0 : iterators.Length; }
		}
		public int DotsPerCycle{
			get{ return IteratorCount * DotsPerIterator; }
		}

		internal CPUFractalEngine(CPUDeviceEntry devEntry)
		{
			int fractalSize, branchesSize, variWeightsSize;
			Fractal.GetNativeFractalSizes(out fractalSize, out branchesSize, out variWeightsSize);

			//these are allocated from unmanaged memory so we don't have to worry about pinning them all the time
			fractal = (NativeFractal*)Marshal.AllocHGlobal(fractalSize);
			branches = (NativeBranch*)Marshal.AllocHGlobal(branchesSize);
			variWeights = (float*)Marshal.AllocHGlobal(variWeightsSize);

			int iterCount = Util.Clamp(2*devEntry.NumCores, 2, MaxUseableIterators);

			iterators = new Iterator[iterCount];

			int dotBufferSize = Marshal.SizeOf(typeof(Dot)) * DotsPerCycle;
			dots = (Dot*)Marshal.AllocHGlobal(dotBufferSize);
			int dotIndiciesSize = Marshal.SizeOf(typeof(ushort)) * DotsPerCycle;
			dotIndicies = (ushort*)Marshal.AllocHGlobal(dotIndiciesSize);
			for(int i = 0; i < DotsPerCycle; i++)
				dotIndicies[i] = (ushort)i;

			for(int i = 0; i < IteratorCount; i++)
			{
				iterators[i] = new Iterator(i);
				iterators[i].SetFractal(this.fractal, branches, variWeights);
				iterators[i].SetOutput(dots);
			}
			
			globalStats = new NativeGlobalStatEntry();

			tonemapVertShader = GLUtil.MakeShader("tonemap_vert_glsl", Kernels.KernelResources.tonemap_vert_glsl, ShaderType.VertexShader);
			tonemapFragShader = GLUtil.MakeShader("tonemap_frag_glsl", Kernels.KernelResources.tonemap_frag_glsl, ShaderType.FragmentShader);
			tonemapProgram = GLUtil.MakeProgram("tonemap_program", tonemapVertShader, tonemapFragShader);
			accumSamplerLocation = GL.GetUniformLocation(tonemapProgram, "accumSampler");
			scaleConstantLocation = GL.GetUniformLocation(tonemapProgram, "scaleConstant");
			brightnessLocation = GL.GetUniformLocation(tonemapProgram, "brightness");
			invGammaLocation = GL.GetUniformLocation(tonemapProgram, "invGamma");
			vibrancyLocation = GL.GetUniformLocation(tonemapProgram, "vibrancy");
			upScaleFactorLocation = GL.GetUniformLocation(tonemapProgram, "upScaleFactor");
			subStepXLocation = GL.GetUniformLocation(tonemapProgram, "subStepX");
			subStepYLocation = GL.GetUniformLocation(tonemapProgram, "subStepY");
#if DEBUG
			FractalManager.Blip += delegate(object obj, EventArgs ea){
				iterators[0].GetStuck();
			};
#endif
		}

		public override void Destroy()
		{
			Deallocate();

			foreach(Iterator iter in iterators)
				iter.Shutdown();
			
			if(fractal != null){     Marshal.FreeHGlobal((IntPtr)fractal);     fractal = null; }
			if(branches != null){    Marshal.FreeHGlobal((IntPtr)branches);    branches = null; }
			if(variWeights != null){ Marshal.FreeHGlobal((IntPtr)variWeights); variWeights = null; }
			

			if(tonemapVertShader != 0){
				GL.DeleteShader(tonemapVertShader);
				tonemapVertShader = 0;
			}
			if(tonemapFragShader != 0){
				GL.DeleteShader(tonemapFragShader);
				tonemapFragShader = 0;
			}
			if(tonemapProgram != 0){
				GL.DeleteProgram(tonemapProgram);
				tonemapProgram = 0;
			}
			if(paletteTexID != 0){
				GL.DeleteTexture(paletteTexID);
				paletteTexID = 0;
			}
		}
		
		public override bool IsAllocated()
		{
			return outputTexID != 0;
		}
		
		public override void Allocate(int xRes, int yRes)
		{
			Deallocate();
			this.xRes = xRes;
			this.yRes = yRes;
			
			accumTexID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, accumTexID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba32f,
				xRes*AALevel, yRes*AALevel, 0, PixelFormat.Rgba, PixelType.Float, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			outputTexID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, outputTexID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				xRes, yRes, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			
			GL.GenFramebuffers(1, out accumFBO);
			GL.GenFramebuffers(1, out outputFBO);
		}
		
		public override void Deallocate()
		{
			this.xRes = 0;
			this.yRes = 0;

			if(accumFBO != 0){
				GL.DeleteFramebuffers(1, ref accumFBO);
				accumFBO = 0;
			}
			if(outputFBO != 0){
				GL.DeleteFramebuffers(1, ref outputFBO);
				outputFBO = 0;
			}

			if(accumTexID != 0){
				GL.DeleteTexture(accumTexID);
				accumTexID = 0;
			}
			if(outputTexID != 0){
				GL.DeleteTexture(outputTexID);
				outputTexID = 0;
			}
		}

		public override bool IsBusy()
		{
			//the CPU engine blocks until not busy
			return false;
		}

		public override void Synchronize()
		{
			//the CPU engine is always synchronized
		}

		public override void ApplyParameters(Fractal fractal)
		{
			if(!IsAllocated()) return;

			fractal.FillNativeFractal(xRes, yRes, this.fractal, this.branches, this.variWeights);

			foreach(Iterator iter in iterators)
				iter.SetFractal(this.fractal, this.branches, this.variWeights);
	
			float invAspectRatio = (xRes > 0) ? ((float)yRes / (float)xRes) : 0.0f;
			viewTransform = FractalManager.CameraTransform.Inverse;
			projTransform = new Affine2D(invAspectRatio, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
		}

		public override void ApplyPalette(Palette palette)
		{
			if(palette.Width <= 0 || palette.Height <= 0)
				throw new ArgumentException("palette may not be empty.");
			
			if(paletteTexID != 0){
				GL.DeleteTexture(paletteTexID);
				paletteTexID = 0;
			}

			uint[] pixels = new uint[palette.Height*palette.Width];
			Color col;
			int i = 0;
			for(int y = 0; y < palette.Height; y++)
			{
				for(int x = 0; x < palette.Width; x++)
				{
					col = palette.GetPixel(x,y);
					pixels[i] = (0x000000FF | (uint)col.B << 8 | (uint)col.G << 16 | (uint)col.R << 24);
					i++;
				}
			}

			paletteTexID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, paletteTexID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 0);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				palette.Width, palette.Height, 0, PixelFormat.Rgba, PixelType.UnsignedInt8888, pixels);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}

		public override void ResetOutput()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, accumFBO);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, accumTexID, 0);
			GL.ClearColor(0.0f,0.0f,0.0f,0.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Finish();

			foreach(Iterator iter in iterators)
				iter.StartResetTask();
			foreach(Iterator iter in iterators)
				iter.Finish();
			
			globalStats.Density = 0.0f;
			globalStats.DotCount = 0;
			globalStats.IterCount = 0;
			globalStats.PeakDensity = 0.0f;
			globalStats.ScaleConstant = 0.0f;
		}
		
		public override void DoIterationCycle(int numIterationsPerThread)
		{
			if(!IsAllocated()) return;
			
			int perThreadCount = 0;

			while(perThreadCount < numIterationsPerThread)
			{
				foreach(Iterator iter in iterators)
					iter.StartIterateTask();
				foreach(Iterator iter in iterators)
					iter.Finish();

				globalStats.IterCount += (ulong)DotsPerCycle;
				globalStats.DotCount = 0;
				globalStats.PeakDensity = 0.0f;
				for(int i = 0; i < iterators.Length; i++)
					globalStats.DotCount += iterators[i].Stats.DotCount;
				uint totalSubPixels = (uint)(xRes * yRes * AALevel * AALevel);
				float density = (float)globalStats.DotCount / (float)totalSubPixels;
				float invPixArea = Math.Abs((fractal->VpsTransform.XAxis.X * fractal->VpsTransform.YAxis.Y)-(fractal->VpsTransform.XAxis.Y * fractal->VpsTransform.YAxis.X));
				globalStats.ScaleConstant = Tone_C2*(invPixArea*(float)(AALevel*AALevel)) / (float)globalStats.IterCount;
			
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, accumFBO);
				GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, accumTexID, 0);
				GL.Enable(EnableCap.Blend);
				GL.BlendEquation(BlendEquationMode.FuncAdd);
				GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);

				GL.Disable(EnableCap.PointSmooth);

				GL.PointSize(1.0f);
				GL.Viewport(0, 0, xRes*AALevel, yRes*AALevel);
				GL.MatrixMode(MatrixMode.Projection);
				GLUtil.GLLoadAffineMatrix(projTransform, -1.0f);
				GL.MatrixMode(MatrixMode.Modelview);
				GLUtil.GLLoadAffineMatrix(viewTransform);

				GL.Enable(EnableCap.Texture2D);
				GL.BindTexture(TextureTarget.Texture2D, paletteTexID);

				GL.Color4(DownScaleFactor,DownScaleFactor,DownScaleFactor,DownScaleFactor);		
				GL.EnableClientState(ArrayCap.VertexArray);
				GL.EnableClientState(ArrayCap.TextureCoordArray);
				GL.VertexPointer(2, VertexPointerType.Float, 16, (IntPtr)dots);
				GL.TexCoordPointer(2, TexCoordPointerType.Float, 16, (IntPtr)((byte*)dots + 8));
				GL.DrawElements(BeginMode.Points, DotsPerCycle, DrawElementsType.UnsignedShort, (IntPtr)dotIndicies);
				GL.DisableClientState(ArrayCap.VertexArray);
				GL.DisableClientState(ArrayCap.TextureCoordArray);

				GL.Disable(EnableCap.Texture2D);
				GL.BindTexture(TextureTarget.Texture2D, 0);

				GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
				GL.Finish();

				perThreadCount += DotsPerIterator;
			}
		}

		public override void CalcToneMap()
		{
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, outputFBO);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, outputTexID, 0);
			
			GL.Disable(EnableCap.Blend);
			GL.Disable(EnableCap.PolygonSmooth);

			GL.Viewport(0, 0, xRes, yRes);
			GL.MatrixMode(MatrixMode.Projection); GL.PushMatrix();
			GL.LoadIdentity();
			GL.Ortho(0, 1, 0, 1, -1, 1);
			GL.MatrixMode(MatrixMode.Modelview);  GL.PushMatrix();
			GL.LoadIdentity();

			GL.Enable(EnableCap.Texture2D);
			GL.UseProgram(tonemapProgram);
			
			GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, accumTexID);
			GL.Uniform1(accumSamplerLocation, 0);
			GL.Uniform1(scaleConstantLocation, globalStats.ScaleConstant);
			GL.Uniform1(brightnessLocation, fractal->Brightness);
			GL.Uniform1(invGammaLocation, fractal->InvGamma);
			GL.Uniform1(vibrancyLocation, fractal->Vibrancy);
			GL.Uniform1(upScaleFactorLocation, UpScaleFactor);
			GL.Uniform1(subStepXLocation, 0.25f/(float)xRes);
			GL.Uniform1(subStepYLocation, 0.25f/(float)yRes);

			GL.Begin(BeginMode.Quads);
			GL.Color4   (1.0f, 1.0f, 1.0f, 1.0f);
			GL.TexCoord2(0.0f, 0.0f); GL.Vertex2  (0.0f, 0.0f);
			GL.TexCoord2(1.0f, 0.0f); GL.Vertex2  (1.0f, 0.0f);
			GL.TexCoord2(1.0f, 1.0f); GL.Vertex2  (1.0f, 1.0f);
			GL.TexCoord2(0.0f, 1.0f); GL.Vertex2  (0.0f, 1.0f);
			GL.End();
			
			GL.ActiveTexture(TextureUnit.Texture0); GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.UseProgram(0);

			GL.MatrixMode(MatrixMode.Projection); GL.PopMatrix();
			GL.MatrixMode(MatrixMode.Modelview);  GL.PopMatrix();

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Finish();
		}

		public override void CopyToneMap()
		{
		}

		public override FractalEngine.Stats GatherStats()
		{
			Stats stats = new Stats();

			stats.TotalIterCount = globalStats.IterCount;
			stats.TotalDotCount = globalStats.DotCount;
			stats.meanDotsPerSubpixel = (float)((double)stats.TotalDotCount / (double)(xRes*yRes*AALevel*AALevel));

			return stats;
		}

		public override int GetOutputTextureId()
		{
			return outputTexID;
		}
		
		public override Color[,] GetOutputPixels()
		{
			if(!IsAllocated())
				throw new FractronException("The fractal engine is not ready.");

			return GetPixelsFromTexture(outputTexID);
		}

		#region Iterator class

		private enum IterTask{ None, Reset, Iterate, Shutdown };

		unsafe private class Iterator
		{
			public const int ThreadTimeout_ms = 2000;

			private int id;
			private IntPtr iterHandle;
			private Random rand;

			Thread workThread;
			private Exception workThreadException = null;
			private object syncRoot;
			private AutoResetEvent starter; //used to tell the work thread that it has a new task
			private AutoResetEvent finisher; //used to tell the main thread that a task has been finished
			private IterTask task;
#if DEBUG
			private volatile bool getStuck = false; //used to test locked up thread handling
			public void GetStuck(){
				getStuck = true;
			}
#endif
			/// <summary>
			/// Will be null if everything is OK, or an exception if something went wrong in the worker thread.
			/// </summary>
			public Exception WorkThreadException{
				get{ lock(syncRoot){ return workThreadException; }}
			}

			public IterTask Task{
				get{ lock(syncRoot){ return task; }}
			}
			public NativeIterStatEntry Stats{
				get{
					lock(syncRoot){
						NativeIterStatEntry stats = new NativeIterStatEntry();
						get_iterator_stats(iterHandle, &stats);
						return stats;
					}
				}
			}

			[DllImport("cpu_iterator.dll")]
			extern private static IntPtr create_iterator(int id, uint seed);
			[DllImport("cpu_iterator.dll")]
			extern private static void destroy_iterator(IntPtr handle);
			[DllImport("cpu_iterator.dll")]
			extern private static void set_iterator_fractal(IntPtr iter, NativeFractal* fractal, NativeBranch* branches, float* variWeights);
			[DllImport("cpu_iterator.dll")]
			extern private static void set_iterator_output(IntPtr iter, int dot_count, Dot* output);
			[DllImport("cpu_iterator.dll")]
			extern private static void get_iterator_stats(IntPtr iter, NativeIterStatEntry* stats);
			[DllImport("cpu_iterator.dll")]
			extern private static void reset_iterator(IntPtr iter);
			[DllImport("cpu_iterator.dll")]
			extern private static void iterate_batch(IntPtr iter);

			public Iterator(int id)
			{
				this.id = id;
				this.rand = new Random();
				workThread = new Thread(workProc);
				syncRoot = new object();
				starter = new AutoResetEvent(false);
				finisher = new AutoResetEvent(false);
				task = IterTask.None;
				
				iterHandle = create_iterator(id, (uint)rand.Next());

				workThread.IsBackground = true;
				workThread.Name = "workThread#"+id.ToString();
				workThread.Start();
			}

			/// <summary>
			/// Shuts this iterator down and terminates its thread (by force if neccesary). Blocks until complete.
			/// </summary>
			public void Shutdown()
			{
				if((workThread.ThreadState & System.Threading.ThreadState.Stopped) != System.Threading.ThreadState.Stopped &&
				   (workThread.ThreadState & System.Threading.ThreadState.Aborted) != System.Threading.ThreadState.Aborted
					)
				{
					startTask(IterTask.Shutdown); //first try to shut the thread down nicely
					try{
						Finish();
					}
					catch
					{
						Trace.WriteLine("Warning: a worker thread shutdown timed out.", "warning");
					}
				}
				destroy_iterator(iterHandle);
				iterHandle = IntPtr.Zero;
			}

			public void SetFractal(NativeFractal* fractal, NativeBranch* branches, float* variWeights)
			{
				lock(syncRoot){
					set_iterator_fractal(iterHandle, fractal, branches, variWeights);
				}
			}

			public void SetOutput(Dot* output)
			{
				lock(syncRoot){
					set_iterator_output(iterHandle, DotsPerIterator, output + id*DotsPerIterator);
				}
			}

			/// <summary>
			/// Starts the reset task asynchronously. Finish should be called sometime afterwords to ensure the task completes.
			/// </summary>
			public void StartResetTask()
			{
				startTask(IterTask.Reset);
			}

			/// <summary>
			/// Starts the iterate task asynchronously. Finish should be called sometime afterwords to ensure the task completes.
			/// </summary>
			public void StartIterateTask()
			{
				startTask(IterTask.Iterate);
			}

			/// <summary>
			/// Block until the last assigned task is complete. Should always be called sometime after Starting a task. Throws
			/// an exception if the task can't complete or times out.
			/// </summary>
			public void Finish()
			{
				if(!finisher.WaitOne(ThreadTimeout_ms))
				{
					if((workThread.ThreadState & System.Threading.ThreadState.Stopped) != System.Threading.ThreadState.Stopped &&
					   (workThread.ThreadState & System.Threading.ThreadState.Aborted) != System.Threading.ThreadState.Aborted
						)
					{
						workThread.Abort();
						throw new FractronException("An iterator thread has timed out.");
					}
				}
				Exception ex = WorkThreadException;
				if(ex != null)
					throw ex;
			}

			/// <summary>
			/// Signals the worker thread to start a task
			/// </summary>
			/// <param name="task">The task the worker should start on</param>
			private void startTask(IterTask task)
			{
				lock(syncRoot)
				{
					this.task = task;
				}
				starter.Set();
			}

			private void workProc()
			{
				try{
					bool done = false;
					while(!done)
					{
						starter.WaitOne(); //wait for the main thread to signal that its time to start

						lock(syncRoot)
						{
#if DEBUG
							if(getStuck){ //this gets the thread caught in an infinite loop
								int k = 0;
								int q = 35 / k;
								while(k < 500)
									k = (k + 1) & 0xFF;
							}
#endif
							if(task == IterTask.Reset)
								reset_iterator(iterHandle);
							else if(task == IterTask.Iterate)
								iterate_batch(iterHandle);
							else if(task == IterTask.Shutdown)
								done = true;

							task = IterTask.None;
						}

						finisher.Set(); //tell the main thread that the task is done
					}
				}
				catch(Exception ex)
				{
					lock(syncRoot){
						workThreadException = ex;
					}
					finisher.Set(); //tell the main thread the task isn't running anymore
				}
			}
		}
		#endregion


	}
}