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
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenCL;

using MTUtil;


//This engine splits rendering between OpenCL and OpenGL
namespace Fractron9000.HybridEngine
{
	public class HybridFractalEngine : FractalEngine
	{
		#region Constants
		public const int AALevel = 2;
		public const int IterGroupSize = 128;
		public const int DotsPerIteratorPerLaunch = 512;

		public const float Tone_C1 = (1.0f/2.0f);
		public const float Tone_C2 = (64.0f/1.0f);
		
		public const float UpScaleFactor = 1024.0f;
		public const float DownScaleFactor = 1.0f / UpScaleFactor;
		#endregion

		#region Common Members
		Random rand = new Random();
		int xRes;
		int yRes;
		int iterBlockCount = 2;
		Affine2D vpsTransform = Affine2D.Identity;
		Size paletteSize;
		NativeFractal nativeFractal;
		NativeGlobalStatEntry globalStats;
		Affine2D       viewTransform;
		Affine2D       projTransform;
		#endregion

		#region OpenCL Members
		OpenCL.Platform platform;
		OpenCL.Device device;
		OpenCL.Context context;
		OpenCL.Program program;
		OpenCL.CommandQueue queue;

		OpenCL.Kernel initIteratorsKernel;
		OpenCL.Kernel resetIteratorsKernel;
		OpenCL.Kernel iterateKernelHybrid;
		OpenCL.Kernel updateStatsKernel;

		OpenCL.Event evCycle, evStat, evAcquireGL, evReleaseGL;
		
		OpenCL.Buffer fractalBuffer;
		OpenCL.Buffer branchBuffer;
		OpenCL.Buffer variWeightBuffer;

		OpenCL.Buffer iterPosStateBuffer;
		OpenCL.Buffer iterColorStateBuffer;
		OpenCL.Buffer iterStatBuffer;
		OpenCL.Buffer globalStatBuffer;
		OpenCL.Buffer entropyXBuffer;
		OpenCL.Buffer entropyCBuffer;
		OpenCL.Buffer entropySeedBuffer;
		OpenCL.Buffer dotBuffer;
		#endregion

		#region OpenGL Members
		int dotBuffer_gl = 0;
		int indexBuffer_gl = 0;

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
		#endregion

		#region Properties
		public override int XRes{
			get{ return xRes; }
		}
		public override int YRes{
			get{ return yRes; }
		}
		public int IterBlockCount{
			get{ return iterBlockCount; }
		}
		public int IteratorCount{
			get{ return IterGroupSize * IterBlockCount; }
		}
		public int DotsPerLaunch{
			get{ return IteratorCount * DotsPerIteratorPerLaunch; }
		}
		#endregion

		[DllImport("opengl32.dll")]
		extern private static IntPtr wglGetCurrentDC();

		internal HybridFractalEngine(OpenTK.Graphics.IGraphicsContext graphicsContext, Platform platform, Device device)
		{
			if(graphicsContext == null || !(graphicsContext is OpenTK.Graphics.IGraphicsContextInternal))
				throw new ArgumentException("Invalid graphics context for OpenCLFractalEngine.", "graphicsContext");

			if(platform == null)
				throw new ArgumentException("Invalid platform for OpenCLFractalEngine", "platform");

			if(device == null)
				throw new ArgumentException("Invalid device for OpenCLFractalEngine", "device");

			this.platform = platform;
			this.device = device;

			IntPtr curDC = wglGetCurrentDC();
			OpenTK.Graphics.IGraphicsContextInternal gCtx = (OpenTK.Graphics.IGraphicsContextInternal)graphicsContext;
			
			context = OpenCL.Context.Create(new Device[]{device},
				new ContextParam(ContextProperties.Platform, platform),
				new ContextParam(ContextProperties.GlContext, gCtx.Context.Handle),
				new ContextParam(ContextProperties.WglHdc, curDC) );

			iterBlockCount = Util.Clamp( (int)device.MaxComputeUnits * 2, 2, 64);
			
			string source = Kernels.KernelResources.kernels_cl;
			
			string opts = "";
			try{
				program = OpenCL.Program.CreateFromSource(context, new string[]{source});
				program.Build(new Device[]{device}, opts);
			}
			catch(OpenCLCallFailedException ex)
			{
				if(ex.ErrorCode == OpenCL.ErrorCode.BuildProgramFailure)
				{
					ex.Data.Add("build_log", program.GetBuildLog(device));
				}
				throw ex;
			}
			
			initIteratorsKernel =   Kernel.Create(program, "init_iterators_kernel");
			resetIteratorsKernel =  Kernel.Create(program, "reset_iterators_kernel");
			iterateKernelHybrid =   Kernel.Create(program, "iterate_kernel_hybrid");
			updateStatsKernel =     Kernel.Create(program, "update_stats_kernel");
			
			queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);

			fractalBuffer =          context.CreateBuffer(MemFlags.ReadOnly, Marshal.SizeOf(typeof(NativeFractal)));
			branchBuffer =           context.CreateBuffer(MemFlags.ReadOnly, Marshal.SizeOf(typeof(NativeBranch)) * NativeFractal.MaxBranches);
			variWeightBuffer =       context.CreateBuffer(MemFlags.ReadOnly, 4 * NativeFractal.MaxBranches * NativeFractal.MaxVariations);

			iterPosStateBuffer =     context.CreateBuffer(MemFlags.ReadWrite, (8  * IteratorCount));
			iterColorStateBuffer =   context.CreateBuffer(MemFlags.ReadWrite, (8 * IteratorCount));
			iterStatBuffer =         context.CreateBuffer(MemFlags.ReadWrite, Marshal.SizeOf(typeof(NativeIterStatEntry)) * IteratorCount);
			globalStatBuffer =       context.CreateBuffer(MemFlags.ReadWrite, Marshal.SizeOf(typeof(NativeGlobalStatEntry)));

			entropyXBuffer =         context.CreateBuffer(MemFlags.ReadWrite, (16 * IteratorCount));
			entropyCBuffer =         context.CreateBuffer(MemFlags.ReadWrite, (4  * IteratorCount));
			entropySeedBuffer =      context.CreateBuffer(MemFlags.ReadWrite, (4 * IteratorCount));
			
			uint[] seeds = new uint[IteratorCount];
			for(int i = 0; i < IteratorCount; i++)
				seeds[i] = (uint)rand.Next(65536);
			entropySeedBuffer.Write(queue, seeds);

			int dotBufferSize = Marshal.SizeOf(typeof(Dot)) * DotsPerLaunch;
			Dot[] dotBufferData = new Dot[DotsPerLaunch];
			for(int i = 0; i < dotBufferData.Length; i++)
			{
				double x = (double)i / (double)dotBufferData.Length;
				double y = Math.Sin(8.0*x);
				dotBufferData[i] = new Dot(new Vec2((float)x,(float)y), new Vec2(0.5f,0.5f));
			}
			GL.GenBuffers(1, out dotBuffer_gl);
			GL.BindBuffer(BufferTarget.ArrayBuffer, dotBuffer_gl);
			GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)dotBufferSize, dotBufferData, BufferUsageHint.StreamDraw);
			GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

			dotBuffer = OpenCL.Buffer.CreateFromGLBuffer(context, MemFlags.WriteOnly, (uint)dotBuffer_gl);

			int indexBufferSize = 4 * DotsPerLaunch;
			uint[] indexBufferData = new uint[DotsPerLaunch];
			for(int i = 0; i < indexBufferData.Length; i++)
				indexBufferData[i] = (uint)i;

			GL.GenBuffers(1, out indexBuffer_gl);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer_gl);
			GL.BufferData<uint>(BufferTarget.ElementArrayBuffer, (IntPtr)indexBufferSize, indexBufferData, BufferUsageHint.StreamDraw);
			GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);

			nativeFractal = new NativeFractal();
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

			paletteSize = new Size(0,0);
			paletteTexID = 0;

			initIteratorsKernel.SetArgs(iterStatBuffer, globalStatBuffer, entropyXBuffer, entropyCBuffer, entropySeedBuffer);
			Event initEvt;
			initIteratorsKernel.EnqueueLaunch(queue, IteratorCount, IterGroupSize, null, out initEvt);
			initEvt.Wait();
			Util.DisposeAndNullify(ref initEvt);

			queue.Finish();
		}

		public override void Destroy()
		{
			Deallocate();

			if(tonemapProgram != 0){
				GL.DeleteProgram(tonemapProgram);
				tonemapProgram = 0;
			}
			if(tonemapFragShader != 0){
				GL.DeleteShader(tonemapFragShader);
				tonemapFragShader = 0;
			}
			if(tonemapVertShader != 0){
				GL.DeleteShader(tonemapVertShader);
				tonemapVertShader = 0;
			}

			accumSamplerLocation = 0;
			scaleConstantLocation = 0;
			brightnessLocation = 0;
			invGammaLocation = 0;
			vibrancyLocation = 0;
			upScaleFactorLocation = 0;
			subStepXLocation = 0;
			subStepYLocation = 0;

			if(paletteTexID != 0){
				GL.DeleteTexture(paletteTexID);
				paletteTexID = 0;
			}
			if(indexBuffer_gl != 0){
				GL.DeleteBuffers(1, ref indexBuffer_gl);
				indexBuffer_gl = 0;
			}
			Util.DisposeAndNullify(ref dotBuffer);
			if(dotBuffer_gl != 0){
				GL.DeleteBuffers(1, ref dotBuffer_gl);
				dotBuffer_gl = 0;
			}

			Util.DisposeAndNullify(ref entropySeedBuffer);
			Util.DisposeAndNullify(ref entropyCBuffer);
			Util.DisposeAndNullify(ref entropyXBuffer);

			Util.DisposeAndNullify(ref globalStatBuffer);
			Util.DisposeAndNullify(ref iterStatBuffer);
			Util.DisposeAndNullify(ref iterColorStateBuffer);
			Util.DisposeAndNullify(ref iterPosStateBuffer);

			Util.DisposeAndNullify(ref variWeightBuffer);
			Util.DisposeAndNullify(ref branchBuffer);
			Util.DisposeAndNullify(ref fractalBuffer);

			Util.DisposeAndNullify(ref evCycle);
			Util.DisposeAndNullify(ref evStat);
			Util.DisposeAndNullify(ref evAcquireGL);
			Util.DisposeAndNullify(ref evReleaseGL);

			Util.DisposeAndNullify(ref queue);

			Util.DisposeAndNullify(ref updateStatsKernel);
			Util.DisposeAndNullify(ref iterateKernelHybrid);
			Util.DisposeAndNullify(ref resetIteratorsKernel);
			Util.DisposeAndNullify(ref initIteratorsKernel);

			Util.DisposeAndNullify(ref program);
			Util.DisposeAndNullify(ref context);
		}

		
		public override bool IsAllocated()
		{
			return outputTexID != 0;
		}
		
		public override void Allocate(int xRes, int yRes)
		{
			queue.Finish();
			
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
			queue.Finish();
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
			//This engine always blocks until not busy
			return false;
		}

		public override void Synchronize()
		{
			//This engine synchronizes after every operation
		}

		public override void ApplyParameters(Fractal fractal)
		{
			queue.Finish();

			NativeBranch[] nativeBranches;
			float[] nativeVariWeights;
			fractal.GetNativeFractal(xRes, yRes, out nativeFractal, out nativeBranches, out nativeVariWeights);
			fractalBuffer.Write(queue, nativeFractal);
			branchBuffer.Write(queue, nativeBranches);
			variWeightBuffer.Write(queue, nativeVariWeights);
			
			float invAspectRatio = (xRes > 0) ? ((float)yRes / (float)xRes) : 0.0f;
			viewTransform = FractalManager.CameraTransform.Inverse;
			projTransform = new Affine2D(invAspectRatio, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);

			queue.Finish();
		}

		public override void ApplyPalette(Palette palette)
		{
			queue.Finish();

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
			queue.Finish();

			Event riEvt;
			resetIteratorsKernel.SetArgs(
				xRes,
				yRes,
				fractalBuffer,
				branchBuffer,
				variWeightBuffer,
				iterPosStateBuffer,
				iterColorStateBuffer,
				iterStatBuffer,
				globalStatBuffer,
				entropyXBuffer,
				entropyCBuffer,
				entropySeedBuffer
			);		
			resetIteratorsKernel.EnqueueLaunch(queue, IteratorCount, IterGroupSize, null, out riEvt);
			riEvt.Wait();
			riEvt.Dispose();
			globalStatBuffer.Read(queue, out globalStats);
			queue.Finish();

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, accumFBO);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, accumTexID, 0);
			GL.ClearColor(0.0f,0.0f,0.0f,0.0f);
			GL.Clear(ClearBufferMask.ColorBufferBit);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			GL.Finish();


			queue.Finish();
		}
		
		public override void DoIterationCycle(int numIterationsPerThread)
		{
			if(!IsAllocated()) return;
			
			//Prep OpenGL for accumulating dots
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
			GL.EnableClientState(ArrayCap.VertexArray);
			GL.EnableClientState(ArrayCap.TextureCoordArray);

			int perThreadCount = 0;
			while(perThreadCount < numIterationsPerThread)
			{
				perThreadCount += DotsPerIteratorPerLaunch;
				//First output dots to the dot buffer
				dotBuffer.AcquireGL(queue);
				iterateKernelHybrid.SetArgs(
					xRes,
					yRes,
					fractalBuffer,
					branchBuffer,
					variWeightBuffer,
					iterPosStateBuffer,
					iterColorStateBuffer,
					iterStatBuffer,
					globalStatBuffer,
					entropyXBuffer,
					entropyCBuffer,
					dotBuffer,
					(uint)DotsPerIteratorPerLaunch
				);		
				iterateKernelHybrid.EnqueueLaunch(queue, IteratorCount, IterGroupSize, null, out evCycle);			
				dotBuffer.ReleaseGL(queue);
				evCycle.Wait();
				Util.DisposeAndNullify(ref evCycle);


				//next use OpenGL to draw them to the output buffer
				GL.BindBuffer(BufferTarget.ArrayBuffer, dotBuffer_gl);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer_gl);
				
				GL.Color4(DownScaleFactor,DownScaleFactor,DownScaleFactor,DownScaleFactor);
				GL.VertexPointer(2, VertexPointerType.Float, 16, (IntPtr)0 );
				GL.TexCoordPointer(2, TexCoordPointerType.Float, 16, (IntPtr)8 );
				GL.DrawElements(BeginMode.Points, DotsPerLaunch, DrawElementsType.UnsignedInt, (IntPtr)0 );
				GL.Flush();
				
				GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
				GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
			}
			GL.DisableClientState(ArrayCap.VertexArray);
			GL.DisableClientState(ArrayCap.TextureCoordArray);
			GL.Disable(EnableCap.Texture2D);
			GL.BindTexture(TextureTarget.Texture2D, 0);

			GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
		}

		public override void CalcToneMap()
		{
			updateStatsKernel.SetArgs(
				xRes,
				yRes,
				fractalBuffer,
				(uint)IteratorCount,
				iterStatBuffer,
				globalStatBuffer
			);
			updateStatsKernel.EnqueueLaunch(queue, 1, 1, null, out evStat);
			evStat.Wait();
			Util.DisposeAndNullify(ref evStat);			
			globalStatBuffer.Read(queue, out globalStats);
			//globalStats.DotCount += (ulong)DotsPerLaunch; //DEBUG
			//globalStats.IterCount += (ulong)DotsPerLaunch; //DEBUG

			queue.Finish();

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
			GL.Uniform1(brightnessLocation, nativeFractal.Brightness);
			GL.Uniform1(invGammaLocation, nativeFractal.InvGamma);
			GL.Uniform1(vibrancyLocation, nativeFractal.Vibrancy);
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
			queue.Finish();

			Stats result = new Stats();

			result.TotalIterCount = globalStats.IterCount;
			result.TotalDotCount = globalStats.DotCount;
			result.meanDotsPerSubpixel = globalStats.Density;

			return result;
		}

		public override int GetOutputTextureId()
		{
			return outputTexID;
		}
		
		public override Color[,] GetOutputPixels()
		{
			if(!IsAllocated())
				throw new FractronException("The fractal engine is not ready.");
			queue.Finish();

			return GetPixelsFromTexture(outputTexID);
		}
	}
}