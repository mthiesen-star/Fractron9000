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
using Cuda;
using MTUtil;

namespace Fractron9000.CudaEngine
{
	public class CudaFractalEngine : FractalEngine
	{
		const int AALevel = 2;
		const int RasterGroupWidth = 8;
		const int RasterGroupHeight = 8;
		const int SubPixelByteSize = 16;

		const int IterBlockSize = 128;
		
		Device device;
		Context context;
		Module module;
		Cuda.Stream mainStream;
		Kernel initIteratorsKernel;
		Kernel resetIteratorsKernel;
		Kernel resetOutputKernel;
		Kernel iterateKernel;
		Kernel updateStatsKernel;
		Kernel updateOutputKernel;

		Random rand = new Random();

		Event resetBeginEvt, resetEndEvt;
		Event cycleIterEvt, cycleStatEvt,  cycleEndEvt;
		Event toneBeginEvt, toneEndEvt;

		int xRes;
		int yRes;
		int iterBlockCount = 2;
		Affine2D vpsTransform = Affine2D.Identity;

		DeviceBuffer iterPosStateBuffer;
		DeviceBuffer iterColorStateBuffer;
		DeviceBuffer iterStatBuffer;
		DeviceBuffer globalStatBuffer;
		DeviceBuffer entropyXBuffer;
		DeviceBuffer entropyCBuffer;
		DeviceBuffer entropySeedBuffer;
		
		DeviceBuffer accumBuffer;
		CudaArray paletteImage;
		TexRef paletteTex;

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
			get{ return IterBlockSize * IterBlockCount; }
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

		internal CudaFractalEngine(Device device)
		{
			if(device == null)
				throw new ArgumentException("Invalid device passed to CudaFractalEngine.", "device");

			this.device = device;

			context = device.CreateContext();

			iterBlockCount = Util.Clamp(device.MultiprocessorCount * 2, 2, 64);

			System.IO.MemoryStream ptxStream = new System.IO.MemoryStream(Kernels.KernelResources.kernels_ptx);
			
			module = context.LoadModule(ptxStream);

			initIteratorsKernel = module.GetKernel("init_iterators_kernel");
			resetIteratorsKernel = module.GetKernel("reset_iterators_kernel");
			iterateKernel = module.GetKernel("iterate_kernel");
			updateStatsKernel = module.GetKernel("update_stats_kernel");
			resetOutputKernel = module.GetKernel("reset_output_kernel");
			updateOutputKernel = module.GetKernel("update_output_kernel");
			glOutputBufferID = 0;			
			
			mainStream = new Cuda.Stream();

			iterPosStateBuffer =   DeviceBuffer.Alloc(8,  IteratorCount);
			iterColorStateBuffer = DeviceBuffer.Alloc(8,  IteratorCount);
			iterStatBuffer =       DeviceBuffer.Alloc(Marshal.SizeOf(typeof(NativeIterStatEntry)), IteratorCount);
			globalStatBuffer =     DeviceBuffer.Alloc(Marshal.SizeOf(typeof(NativeGlobalStatEntry)), 1);

			entropyXBuffer =       DeviceBuffer.Alloc(16, IteratorCount);
			entropyCBuffer =       DeviceBuffer.Alloc(4,  IteratorCount);
			entropySeedBuffer =    DeviceBuffer.Alloc(4,  IteratorCount);
			
			uint[] seeds = new uint[IteratorCount];
			for(int i = 0; i < IteratorCount; i++)
				seeds[i] = (uint)rand.Next(65536);
			CudaMem.Copy(seeds, entropySeedBuffer);

			paletteImage = CudaArray.Null;
			paletteTex = module.GetTexRef("paletteTex");

			resetBeginEvt = new Event();
			resetEndEvt = new Event();
			cycleIterEvt = new Event();
			cycleStatEvt = new Event();
			cycleEndEvt = new Event();
			toneBeginEvt = new Event();
			toneEndEvt = new Event();

			initIteratorsKernel.SetBlockShape(IterBlockSize,1,1);
			initIteratorsKernel.SetGridDim(IterBlockCount,1);
			initIteratorsKernel.SetSharedSize(0);
			resetIteratorsKernel.SetBlockShape(IterBlockSize,1,1);
			resetIteratorsKernel.SetGridDim(IterBlockCount,1);
			resetIteratorsKernel.SetSharedSize(0);
			iterateKernel.SetBlockShape(IterBlockSize,1,1);
			iterateKernel.SetGridDim(IterBlockCount,1);
			iterateKernel.SetSharedSize(0);
			updateStatsKernel.SetBlockShape(1,1,1);
			updateStatsKernel.SetGridDim(1,1);
			updateStatsKernel.SetSharedSize(0);

			initIteratorsKernel.Launch(iterStatBuffer, globalStatBuffer, entropyXBuffer.Ptr.RawPtr, entropyCBuffer.Ptr.RawPtr, entropySeedBuffer.Ptr.RawPtr);

			context.Synchronize();
		}

		public override void Destroy()
		{
			Deallocate();
			iterPosStateBuffer.Free();
			iterColorStateBuffer.Free();
			iterStatBuffer.Free();
			globalStatBuffer.Free();
			entropyXBuffer.Free();
			entropyCBuffer.Free();
			entropySeedBuffer.Free();
			paletteImage.Free();

			module.Dispose();
			context.Dispose();
		}
		
		public override bool IsAllocated()
		{
			return glOutputBufferID != 0;
		}
		
		public override void Allocate(int xRes, int yRes)
		{
			context.Synchronize();
			Deallocate();
			this.xRes = xRes;
			this.yRes = yRes;

			accumBuffer = DeviceBuffer.Alloc(SubPixelByteSize, AALevel*AALevel*xRes*yRes);

			GL.GenBuffers(1, out glOutputBufferID);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, glOutputBufferID);
			GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(4*xRes*yRes), IntPtr.Zero, BufferUsageHint.StreamCopy);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
			context.GLRegisterBufferObject(glOutputBufferID);


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
			
			resetOutputKernel.SetBlockShape(RasterGroupWidth,RasterGroupHeight,1);
			resetOutputKernel.SetGridDim(RasterWorkGroupXCount,RasterWorkGroupYCount);
			resetOutputKernel.SetSharedSize(0);

			updateOutputKernel.SetBlockShape(RasterGroupWidth,RasterGroupHeight,1);
			updateOutputKernel.SetGridDim(RasterWorkGroupXCount,RasterWorkGroupYCount);
			updateOutputKernel.SetSharedSize(0);
		}
		
		public override void Deallocate()
		{
			context.Synchronize();
			this.xRes = 0;
			this.yRes = 0;
			
			accumBuffer.Free();
			if(glOutputBufferID != 0){
				context.GLUnregisterBufferObject(glOutputBufferID);
				glOutputBufferID = 0;
				GL.DeleteBuffers(1, ref glOutputBufferID);
			}
			if(glOutputTexID != 0){
				GL.DeleteTexture(glOutputTexID);
				glOutputTexID = 0;
			}
		}

		public override bool IsBusy()
		{
			return mainStream.Query() == StreamQueryResult.Unfinished;
		}

		public override void Synchronize()
		{
			mainStream.Synchronize();
		}

		public override void ApplyParameters(Fractal fractal)
		{
			context.Synchronize();
			mainStream.Synchronize();

			NativeFractal nativeFractal;
			NativeBranch[] nativeBranches;
			float[] nativeVariWeights;
			fractal.GetNativeFractal(xRes, yRes, out nativeFractal, out nativeBranches, out nativeVariWeights);

			module.WriteConstant("fractalInfo", nativeFractal);
			module.WriteConstant("branchInfo",  nativeBranches);
			module.WriteConstant("variWeightBuffer", nativeVariWeights);

			context.Synchronize();
		}

		public override void ApplyPalette(Palette palette)
		{
			context.Synchronize();
			mainStream.Synchronize();

			if(palette.Width <= 0 || palette.Height <= 0)
				throw new ArgumentException("palette may not be empty.");

			paletteImage.Free();

			paletteImage = CudaArray.Allocate(palette.Width,palette.Height,CudaArrayFormat.Float,4);

			Vec4[,] pix = new Vec4[palette.Height,palette.Width];

			Color col;
			for(int y = 0; y < palette.Height; y++)
			{
				for(int x = 0; x < palette.Width; x++)
				{
					col = palette.GetPixel(x,y);
					pix[y,x] = new Vec4((float)col.R / 255.0f, (float)col.G / 255.0f,(float)col.B / 255.0f, 1.0f);
				}
			}
			
			CudaMem.Copy(pix, paletteImage);

			paletteTex.Array = paletteImage;
			paletteTex.AddressModeX = TexAddressMode.Clamp;
			paletteTex.AddressModeY = TexAddressMode.Clamp;
			paletteTex.FilterMode = TexFilterMode.Point;
			paletteTex.Flags = TexFlags.NormalizedCoordinates;

			iterateKernel.SetTexRef(paletteTex);
			context.Synchronize();
		}

		public override void ResetOutput()
		{
			mainStream.Synchronize();
			context.Synchronize();

			resetIteratorsKernel.Launch(
				xRes,
				yRes,
				iterPosStateBuffer.Ptr.RawPtr,
				iterColorStateBuffer.Ptr.RawPtr,
				iterStatBuffer.Ptr.RawPtr,
				globalStatBuffer.Ptr.RawPtr,
				entropyXBuffer.Ptr.RawPtr,
				entropyCBuffer.Ptr.RawPtr,
				entropySeedBuffer.Ptr.RawPtr
			);
			DeviceBuffer glOutputBuffer = DeviceBuffer.GLMapBufferObject(glOutputBufferID);

			resetOutputKernel.Launch(xRes, yRes, accumBuffer.Ptr.RawPtr, glOutputBuffer.Ptr.RawPtr);

			glOutputBuffer.GLUnmapBufferObject(glOutputBufferID);

			context.Synchronize();
		}
		
		public override void DoIterationCycle(int numIterationsPerThread)
		{
			if(paletteImage.IsNull())
				return;
			
			iterateKernel.LaunchAsync(mainStream,
				xRes,
				yRes,
				iterPosStateBuffer.Ptr.RawPtr,
				iterColorStateBuffer.Ptr.RawPtr,
				iterStatBuffer.Ptr.RawPtr,
				globalStatBuffer.Ptr.RawPtr,
				entropyXBuffer.Ptr.RawPtr,
				entropyCBuffer.Ptr.RawPtr,
				accumBuffer.Ptr.RawPtr,
				(uint)numIterationsPerThread
			);
			
			updateStatsKernel.LaunchAsync(mainStream,
				xRes,
				yRes,
				(uint)IteratorCount,
				iterStatBuffer.Ptr.RawPtr,
				globalStatBuffer.Ptr.RawPtr
			);
			
		}

		public override void CalcToneMap()
		{	
			DeviceBuffer glOutputBuffer = DeviceBuffer.GLMapBufferObject(glOutputBufferID);

			updateOutputKernel.LaunchAsync(mainStream,
				xRes,
				yRes,
				globalStatBuffer.Ptr.RawPtr,
				accumBuffer.Ptr.RawPtr,
				glOutputBuffer.Ptr.RawPtr
			);

			glOutputBuffer.GLUnmapBufferObject(glOutputBufferID);
		}

		public override void CopyToneMap()
		{
			if(!IsAllocated()) return;
			mainStream.Synchronize();
			context.Synchronize();

			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, glOutputBufferID);
			GL.BindTexture(TextureTarget.Texture2D, glOutputTexID);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, xRes, yRes,
				PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
		}

		public override FractalEngine.Stats GatherStats()
		{
			mainStream.Synchronize();
			context.Synchronize();

			Stats stats = new Stats();
			NativeGlobalStatEntry[] gStats = new NativeGlobalStatEntry[1];

			CudaMem.Copy(globalStatBuffer, gStats);

			stats.TotalIterCount = gStats[0].IterCount;
			stats.TotalDotCount  = gStats[0].DotCount;
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
			mainStream.Synchronize();
			context.Synchronize();

			return GetPixelsFromBuffer(xRes, yRes, glOutputBufferID);
		}
	}
}