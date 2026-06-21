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

namespace Fractron9000.OpenCLEngine
{
	public class OpenCLFractalEngine : FractalEngine
	{
		public const int AALevel = 2;
		public const int RasterGroupWidth = 8;
		public const int RasterGroupHeight = 8;
		public const int SubPixelByteSize = 16;

		public const int IterGroupSize = 128;
		
		OpenCL.Platform platform;
		OpenCL.Device device;
		OpenCL.Context context;
		OpenCL.Program program;
		CommandQueue queue;

		bool useLowProfile = false; //this will be set to true for low performance devices

		Kernel initIteratorsKernel;
		Kernel resetIteratorsKernel;
		Kernel resetOutputKernel;
		Kernel iterateKernel;
		Kernel updateStatsKernel;
		Kernel updateOutputKernel;

		Random rand = new Random();

		OpenCL.Event evCycle, evStat, evAcquireGL, evToneMap, evReleaseGL;

		int xRes;
		int yRes;
		int iterBlockCount = 2;
		Affine2D vpsTransform = Affine2D.Identity;

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

		OpenCL.Buffer accumBuffer;
		Size paletteSize;
		Image2D paletteImage;
		OpenCL.Buffer paletteBuffer;
		Sampler paletteSampler;
		OpenCL.Buffer outputBuffer;

		uint glOutputBufferID = 0;
		int glOutputTexID = 0;
		
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

		public int RasterWorkGroupXCount{get{
			return (xRes+RasterGroupWidth-1)/RasterGroupWidth;
		}}

		public int RasterWorkGroupYCount{get{
			return (yRes+RasterGroupHeight-1)/RasterGroupHeight;
		}}

		public int RasterGlobalWorkWidth{get{
			return RasterWorkGroupXCount * RasterGroupWidth;
		}}

		public int RasterGlobalWorkHeight{get{
			return RasterWorkGroupYCount * RasterGroupHeight;
		}}

		[DllImport("opengl32.dll")]
		extern private static IntPtr wglGetCurrentDC();

		internal OpenCLFractalEngine(OpenTK.Graphics.IGraphicsContext graphicsContext, Platform platform, Device device)
		{
			if(graphicsContext == null || !(graphicsContext is OpenTK.Graphics.IGraphicsContextInternal))
				throw new ArgumentException("Invalid graphics context for OpenCLFractalEngine.", "graphicsContext");

			if(platform == null)
				throw new ArgumentException("Invalid platform for OpenCLFractalEngine", "platform");

			if(device == null)
				throw new ArgumentException("Invalid device for OpenCLFractalEngine", "device");

			this.platform = platform;
			this.device = device;

			useLowProfile = !device.ImageSupport;

			IntPtr curDC = wglGetCurrentDC();
			OpenTK.Graphics.IGraphicsContextInternal gCtx = (OpenTK.Graphics.IGraphicsContextInternal)graphicsContext;
			
			context = OpenCL.Context.Create(new Device[]{device},
				new ContextParam(ContextProperties.Platform, platform),
				new ContextParam(ContextProperties.GlContext, gCtx.Context.Handle),
				new ContextParam(ContextProperties.WglHdc, curDC) );

			iterBlockCount = Util.Clamp( (int)device.MaxComputeUnits * 2, 2, 64);
			
			string source = null;
			if(useLowProfile)
				source = Kernels.KernelResources.kernels_low_cl;
			else
				source = Kernels.KernelResources.kernels_cl;
			
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
			iterateKernel =         Kernel.Create(program, "iterate_kernel");
			updateStatsKernel =     Kernel.Create(program, "update_stats_kernel");
			resetOutputKernel =     Kernel.Create(program, "reset_output_kernel");
			updateOutputKernel =    Kernel.Create(program, "update_output_kernel");
			glOutputBufferID = 0;			
			
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

			paletteSize = new Size(0,0);
			paletteImage = null;
			paletteBuffer = null;
			if(!useLowProfile)
			{
				paletteSampler = Sampler.Create(context, true, AddressingMode.ClampToEdge, FilterMode.Linear);
			}

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
			Util.DisposeAndNullify(ref fractalBuffer);
			Util.DisposeAndNullify(ref branchBuffer);
			Util.DisposeAndNullify(ref variWeightBuffer);
			Util.DisposeAndNullify(ref iterPosStateBuffer);
			Util.DisposeAndNullify(ref iterColorStateBuffer);
			Util.DisposeAndNullify(ref iterStatBuffer);
			Util.DisposeAndNullify(ref globalStatBuffer);
			Util.DisposeAndNullify(ref entropyXBuffer);
			Util.DisposeAndNullify(ref entropyCBuffer);
			Util.DisposeAndNullify(ref entropySeedBuffer);
			Util.DisposeAndNullify(ref paletteImage);
			Util.DisposeAndNullify(ref paletteSampler);
			Util.DisposeAndNullify(ref paletteBuffer);

			Util.DisposeAndNullify(ref evCycle);
			Util.DisposeAndNullify(ref evStat);
			Util.DisposeAndNullify(ref evAcquireGL);
			Util.DisposeAndNullify(ref evToneMap);
			Util.DisposeAndNullify(ref evReleaseGL);

			Util.DisposeAndNullify(ref queue);

			Util.DisposeAndNullify(ref initIteratorsKernel);
			Util.DisposeAndNullify(ref resetIteratorsKernel);
			Util.DisposeAndNullify(ref iterateKernel);
			Util.DisposeAndNullify(ref updateStatsKernel);
			Util.DisposeAndNullify(ref resetOutputKernel);
			Util.DisposeAndNullify(ref updateOutputKernel);

			Util.DisposeAndNullify(ref program);
			Util.DisposeAndNullify(ref context);
		}
		
		public override bool IsAllocated()
		{
			return glOutputTexID != 0;
		}
		
		public override void Allocate(int xRes, int yRes)
		{
			queue.Finish();
			
			Deallocate();
			this.xRes = xRes;
			this.yRes = yRes;
			
			accumBuffer = context.CreateBuffer(MemFlags.ReadWrite, SubPixelByteSize*AALevel*AALevel*xRes*yRes);

			GL.GenBuffers(1, out glOutputBufferID);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, glOutputBufferID);
			GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(4*xRes*yRes), IntPtr.Zero, BufferUsageHint.StreamCopy);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);

			outputBuffer = OpenCL.Buffer.CreateFromGLBuffer(context, MemFlags.ReadWrite, glOutputBufferID);

			glOutputTexID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, glOutputTexID);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
				xRes, yRes, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
		
		public override void Deallocate()
		{
			queue.Finish();
			this.xRes = 0;
			this.yRes = 0;

			Util.DisposeAndNullify(ref accumBuffer);
			Util.DisposeAndNullify(ref outputBuffer);
			if(glOutputBufferID != 0){
				GL.DeleteBuffers(1, ref glOutputBufferID);
				glOutputBufferID = 0;
			}
			if(glOutputTexID != 0){
				GL.DeleteTexture(glOutputTexID);
				glOutputTexID = 0;
			}
		}

		public override bool IsBusy()
		{
			return (evCycle != null && !evCycle.Complete) || (evReleaseGL != null && !evReleaseGL.Complete);
		}

		public override void Synchronize()
		{
			queue.Finish();
		}

		public override void ApplyParameters(Fractal fractal)
		{
			queue.Finish();

			NativeFractal nativeFractal;
			NativeBranch[] nativeBranches;
			float[] nativeVariWeights;
			fractal.GetNativeFractal(xRes, yRes, out nativeFractal, out nativeBranches, out nativeVariWeights);
			fractalBuffer.Write(queue, nativeFractal);
			branchBuffer.Write(queue, nativeBranches);
			variWeightBuffer.Write(queue, nativeVariWeights);
			
			queue.Finish();
		}

		public override void ApplyPalette(Palette palette)
		{
			queue.Finish();

			if(palette.Width <= 0 || palette.Height <= 0)
				throw new ArgumentException("palette may not be empty.");

			Util.DisposeAndNullify(ref paletteImage);
			Util.DisposeAndNullify(ref paletteBuffer);

			paletteSize = new Size(palette.Width, palette.Height);

			if(useLowProfile)
			{
				paletteBuffer = context.CreateBuffer(MemFlags.ReadOnly, 4*palette.Width*palette.Height);
				uint[] pixels = new uint[palette.Height*palette.Width];
				Color col;
				int i = 0;
				for(int y = 0; y < palette.Height; y++)
				{
					for(int x = 0; x < palette.Width; x++)
					{
						col = palette.GetPixel(x,y);
						pixels[i] = (0xFF000000 | (uint)col.B << 16 | (uint)col.G << 8 | (uint)col.R);
						i++;
					}
				}
				paletteBuffer.Write(queue, pixels);
			}
			else
			{
				paletteImage = context.CreateImage2D(MemFlags.ReadOnly, new ImageFormat(ChannelOrder.Rgba, ChannelType.UnormInt8), palette.Width, palette.Height);

				uint[,] pixels = new uint[palette.Height,palette.Width];
				Color col;		
				for(int y = 0; y < palette.Height; y++)
				{
					for(int x = 0; x < palette.Width; x++)
					{
						col = palette.GetPixel(x,y);
						pixels[y,x] = (0xFF000000 | (uint)col.B << 16 | (uint)col.G << 8 | (uint)col.R);
					}
				}
				
				paletteImage.Write(queue, pixels);
			}
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

			outputBuffer.AcquireGL(queue);
			resetOutputKernel.SetArgs(xRes, yRes, accumBuffer, outputBuffer);
			Event roEvt;
			resetOutputKernel.EnqueueLaunch(queue, RasterGlobalWorkWidth, RasterGlobalWorkHeight, RasterGroupWidth, RasterGroupHeight, out roEvt);
			roEvt.Wait();
			roEvt.Dispose();
			outputBuffer.ReleaseGL(queue);

			queue.Finish();
		}
		
		public override void DoIterationCycle(int numIterationsPerThread)
		{
			if(!IsAllocated()) return;
			
			Util.DisposeAndNullify(ref evCycle);

			if(useLowProfile)
			{   //the low profile kernel can't handle images, so pass a buffer instead
				iterateKernel.SetArgs(
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
					accumBuffer,
					(uint)paletteSize.Width,
					(uint)paletteSize.Height,
					paletteBuffer,
					(uint)numIterationsPerThread
				);
				iterateKernel.EnqueueLaunch(queue, IteratorCount, IterGroupSize, null, out evCycle);
			}
			else
			{
				iterateKernel.SetArgs(
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
					accumBuffer,
					paletteImage,
					paletteSampler,
					(uint)numIterationsPerThread
				);		
				iterateKernel.EnqueueLaunch(queue, IteratorCount, IterGroupSize, null, out evCycle);
			}

			Util.DisposeAndNullify(ref evStat);
			updateStatsKernel.SetArgs(
				xRes,
				yRes,
				fractalBuffer,
				(uint)IteratorCount,
				iterStatBuffer,
				globalStatBuffer
			);
			updateStatsKernel.EnqueueLaunch(queue, 1, 1, null, out evStat);
			queue.Flush();
		}

		public override void CalcToneMap()
		{
			Util.DisposeAndNullify(ref evAcquireGL);
			outputBuffer.EnqueueAcquireGL(queue, null, out evAcquireGL);
			
			Util.DisposeAndNullify(ref evToneMap);
			updateOutputKernel.SetArgs(
				xRes,
				yRes,
				fractalBuffer,
				globalStatBuffer,
				accumBuffer,
				outputBuffer
			);
			updateOutputKernel.EnqueueLaunch(queue, RasterGlobalWorkWidth, RasterGlobalWorkHeight, RasterGroupWidth, RasterGroupHeight, out evToneMap);

			Util.DisposeAndNullify(ref evReleaseGL);
			outputBuffer.EnqueueReleaseGL(queue, null, out evReleaseGL);
			queue.Flush();
		}

		public override void CopyToneMap()
		{
			if(!IsAllocated()) return;
			queue.Finish();
			
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, glOutputBufferID);
			GL.BindTexture(TextureTarget.Texture2D, glOutputTexID);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, xRes, yRes,
				PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);		
		}

		public override FractalEngine.Stats GatherStats()
		{
			queue.Finish();

			Stats stats = new Stats();

			NativeGlobalStatEntry gStats = new NativeGlobalStatEntry();

			globalStatBuffer.Read(queue, out gStats);

			stats.TotalIterCount = gStats.IterCount;
			stats.TotalDotCount = gStats.DotCount;
			stats.meanDotsPerSubpixel = (float)((double)stats.TotalDotCount / (double)(xRes*yRes*AALevel*AALevel));

			return stats;
		}

		public override int GetOutputTextureId()
		{
			return glOutputTexID;
		}
		
		public override Color[,] GetOutputPixels()
		{
			if(!IsAllocated())
				throw new FractronException("The fractal engine is not ready.");
			queue.Finish();

			return GetPixelsFromBuffer(xRes, yRes, glOutputBufferID);
		}



	}
}