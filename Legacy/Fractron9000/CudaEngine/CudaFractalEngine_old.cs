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
using MTUtil.Extensions;

namespace Fractron9000.CudaEngine
{
	public class CudaFractalEngine : FractalEngine
	{
		const int MaxBranches = 16;
		const int MaxFactors = 48;
		const int PaletteTexRes = 128;

		const int AALevel = 2;
		const int RasterBlockSize = 8;
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
		int xRasterBlockCount;
		int yRasterBlockCount;
		int currentIterLoopCount = 32;
		Affine2D vpsTransform = Affine2D.Identity;

		DeviceBuffer2D iterPosStateBuffer;
		DeviceBuffer2D iterColorStateBuffer;
		DeviceBuffer2D entropyXBuffer;
		DeviceBuffer2D entropyCBuffer;
		DeviceBuffer2D entropySeedBuffer;
		DeviceBuffer2D dotCountBuffer;
		DeviceBuffer2D peakDensityBuffer;
		DevicePtr totalIterCountMem;
		DevicePtr totalDotCountMem;
		DevicePtr densityMem;
		DevicePtr peakDensityMem;
		DevicePtr scaleConstantMem;
		DeviceBuffer2D accumBuffer;
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
		public int CurrentIterLoopCount{
			get{ return currentIterLoopCount; }
		}
		public int CurrentIterationsPerCycle{
			get{ return IteratorCount * currentIterLoopCount; }
		}

		public CudaFractalEngine()
		{
			device = Device.Devices[0];
			context = device.CreateContext();

			iterBlockCount = Util.Clamp(device.MultiprocessorCount * 2, 2, 64);


			//System.Reflection.Assembly loadedAssembly = typeof(CudaFractalEngine).Assembly;
			//System.IO.Stream stream = loadedAssembly.GetManifestResourceStream(typeof(CudaFractalEngine), "kernels.ptx");
			System.IO.MemoryStream stream = new System.IO.MemoryStream(CudaResources.kernels_ptx);
			
			module = context.LoadModule(stream);
			initIteratorsKernel = module.GetKernel("init_iterators_kernel");
			resetIteratorsKernel = module.GetKernel("reset_iterators_kernel");
			iterateKernel = module.GetKernel("iterate_kernel");
			updateStatsKernel = module.GetKernel("update_stats_kernel");
			resetOutputKernel = module.GetKernel("reset_output_kernel");
			updateOutputKernel = module.GetKernel("update_output_kernel");
			glOutputBufferID = 0;			
			
			mainStream = new Cuda.Stream();

			iterPosStateBuffer = DeviceBuffer2D.Alloc(8, IterBlockSize, IterBlockCount);
			module.WriteConstant("iterPosStateBuffer", iterPosStateBuffer);
			iterColorStateBuffer = DeviceBuffer2D.Alloc(16, IterBlockSize, IterBlockCount);
			module.WriteConstant("iterColorStateBuffer", iterColorStateBuffer);

			entropyXBuffer = DeviceBuffer2D.Alloc(16, IterBlockSize, IterBlockCount);
			module.WriteConstant("entropyXBuffer", entropyXBuffer);
			entropyCBuffer = DeviceBuffer2D.Alloc(4, IterBlockSize, IterBlockCount);
			module.WriteConstant("entropyCBuffer", entropyCBuffer);
			entropySeedBuffer = DeviceBuffer2D.Alloc(4, IterBlockSize, IterBlockCount);
			module.WriteConstant("entropySeedBuffer", entropySeedBuffer);
			
			HostBuffer2D<uint> hostEntropySeedBuffer = HostBuffer2D<uint>.Alloc(IterBlockSize, IterBlockCount);
			uint rnd;
			for(int y = 0; y < IterBlockCount; y++)
			{
				for(int x = 0; x < IterBlockSize; x++)
				{
					rnd = (uint)rand.Next(65536);
					hostEntropySeedBuffer[y,x] = rnd;
				}
			}

			CudaMem.Copy(hostEntropySeedBuffer, entropySeedBuffer);
			hostEntropySeedBuffer.Free();


			dotCountBuffer = DeviceBuffer2D.Alloc(8, IterBlockSize, IterBlockCount);
			module.WriteConstant("dotCountBuffer", dotCountBuffer);

			peakDensityBuffer = DeviceBuffer2D.Alloc(4, IterBlockSize, IterBlockCount);
			module.WriteConstant("peakDensityBuffer", peakDensityBuffer);
			
			totalIterCountMem = DevicePtr.AllocRaw(8);
			module.WriteConstant("totalIterCountMem", totalIterCountMem);
			totalDotCountMem = DevicePtr.AllocRaw(8);
			module.WriteConstant("totalDotCountMem", totalDotCountMem);
			densityMem = DevicePtr.AllocRaw(4);
			module.WriteConstant("densityMem", densityMem);
			peakDensityMem = DevicePtr.AllocRaw(4);
			module.WriteConstant("peakDensityMem", peakDensityMem);
			scaleConstantMem = DevicePtr.AllocRaw(4);
			module.WriteConstant("scaleConstantMem", scaleConstantMem);

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

			initIteratorsKernel.Launch();
			context.Synchronize();
		}

		public override void Destroy()
		{
			Deallocate();
			iterPosStateBuffer.Free();
			iterColorStateBuffer.Free();
			entropyXBuffer.Free();
			entropyCBuffer.Free();
			dotCountBuffer.Free();
			peakDensityBuffer.Free();
			totalIterCountMem.Free();
			totalDotCountMem.Free();
			densityMem.Free();
			peakDensityMem.Free();
			scaleConstantMem.Free();
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

			xRasterBlockCount = (xRes+RasterBlockSize-1)/RasterBlockSize;
			yRasterBlockCount = (yRes+RasterBlockSize-1)/RasterBlockSize;

			module.WriteConstant("xRes", xRes);
			module.WriteConstant("yRes", yRes);
			module.WriteConstant("iterBlockCount", IterBlockCount);
			
			accumBuffer = DeviceBuffer2D.Alloc(SubPixelByteSize, xRes*4, yRes);
			module.WriteConstant("accumBuffer", accumBuffer);

			GL.GenBuffers(1, out glOutputBufferID);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, glOutputBufferID);
			GL.BufferData(BufferTarget.PixelUnpackBuffer, (IntPtr)(4*xRes*yRes), IntPtr.Zero, BufferUsageHint.StreamCopy);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
			context.GLRegisterBufferObject(glOutputBufferID);


			glOutputTexID = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, glOutputTexID);
			GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Four,
				xRes, yRes,0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.GenerateMipmap, 1);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Clamp);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Clamp);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			
			resetOutputKernel.SetBlockShape(RasterBlockSize,RasterBlockSize,1);
			resetOutputKernel.SetGridDim(xRasterBlockCount,yRasterBlockCount);
			resetOutputKernel.SetSharedSize(0);

			updateOutputKernel.SetBlockShape(RasterBlockSize,RasterBlockSize,1);
			updateOutputKernel.SetGridDim(xRasterBlockCount,yRasterBlockCount);
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

			float invAspectRatio = (XRes > 0) ? ((float)YRes / (float)XRes) : 0.0f;
			Affine2D viewTransform = fractal.CameraTransform.Inverse;
			Affine2D projTransform = new Affine2D(invAspectRatio, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
			float xHalf = (float)XRes / 2.0f;
			float yHalf = (float)YRes / 2.0f;

			Affine2D screenTransform = new Affine2D(xHalf, 0.0f, 0.0f, yHalf, xHalf, yHalf);

			vpsTransform = screenTransform * projTransform * viewTransform;
			module.WriteConstant("vpsTransform", vpsTransform);

			module.WriteConstant("brightness", fractal.Brightness);
			
			float invGamma = 1.0f / fractal.Gamma;
			module.WriteConstant("invGamma", invGamma);

			module.WriteConstant("vibrancy", fractal.Vibrancy);

			module.WriteConstant("bgColor", fractal.BackgroundColor);

			Int32 branchCount = Math.Min(MaxBranches, fractal.Branches.Count);
			module.WriteConstant("branchCount", branchCount);

			Affine2D[] branchPreTransforms = new Affine2D[MaxBranches];
			Affine2D[] branchPostTransforms = new Affine2D[MaxBranches];

			float[] branchLumas = new float[MaxBranches];
			Vec2[] branchChromas = new Vec2[MaxBranches];

			float[] branchFactors = new float[MaxBranches * MaxFactors];
			UInt32[] branchNormWeights = new UInt32[MaxBranches];
			branchNormWeights.AssignAll( (i)=>(UInt32)0x00010000 );
			
			float branchWeightSum = 0.0f;
			for(int bi = 0; bi < branchCount; bi++)
				branchWeightSum += fractal.Branches[bi].Weight;

			float[] branchColorWeights = new float[MaxBranches];

			UInt32 runningSum = 0;
			for(int bi = 0; bi < branchCount; bi++)
			{
				Branch branch = fractal.Branches[bi];

				branchPreTransforms[bi] = branch.PreTransform;
				branchPostTransforms[bi] = branch.PostTransform;

				branchLumas[bi] = branch.Luma;
				branchChromas[bi] = branch.Chroma;

				runningSum += (UInt32)(branch.Weight / branchWeightSum * 65536.0f);
				if(bi < branchCount-1)
					branchNormWeights[bi] = runningSum;
				else
					branchNormWeights[bi] = 0x00010000;

				branchColorWeights[bi] = branch.ColorWeight;

				foreach(Branch.VariEntry ve in branch.Variations)
				{
					if(ve.Index >= 0 && ve.Index < MaxFactors)
						branchFactors[MaxFactors*bi + ve.Index] += ve.Weight;
				}
			}

			module.WriteConstant("branchPreTransforms", branchPreTransforms);
			module.WriteConstant("branchPostTransforms", branchPostTransforms);
			module.WriteConstant("branchLumas", branchLumas);
			module.WriteConstant("branchChromas", branchChromas);
			module.WriteConstant("branchNormWeights", branchNormWeights);
			module.WriteConstant("branchColorWeights", branchColorWeights);
			module.WriteConstant("branchFactors", branchFactors);

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
			HostBuffer2D<Vec4> hostPaletteBuffer = HostBuffer2D<Vec4>.Alloc(palette.Width,palette.Height);

			Color col;
		
			Vec4 colorVec;
			for(int y = 0; y < palette.Height; y++)
			{
				for(int x = 0; x < palette.Width; x++)
				{
					col = palette.GetPixel(x,y);
					colorVec = new Vec4(
						(float)col.R / 255.0f,
						(float)col.G / 255.0f,
						(float)col.B / 255.0f,
						1.0f );

					hostPaletteBuffer[y,x] = colorVec;
				}
			}
			
			CudaMem.Copy(hostPaletteBuffer, paletteImage);
			hostPaletteBuffer.Free();

			paletteTex.Array = paletteImage;
			paletteTex.SetFormat(CudaArrayFormat.Float, 4);
			paletteTex.AddressModeX = TexAddressMode.Clamp;
			paletteTex.AddressModeY = TexAddressMode.Clamp;
			paletteTex.FilterMode = TexFilterMode.Linear;
			paletteTex.Flags = TexFlags.NormalizedCoordinates;

			iterateKernel.SetTexRef(paletteTex);
			context.Synchronize();
		}

		public override void ResetOutput()
		{
			mainStream.Synchronize();
			context.Synchronize();

			//resetBeginEvt.Record();
			resetIteratorsKernel.Launch();
			DeviceBuffer2D glOutputBuffer = DeviceBuffer2D.GLMapBufferObject(glOutputBufferID, (uint)(4*xRes));
			resetOutputKernel.Launch(glOutputBuffer);
			glOutputBuffer.GLUnmapBufferObject(glOutputBufferID);
			//resetEndEvt.Record();

			context.Synchronize();
		}
		
		public override void DoIterationCycle(int numIterationsPerThread)
		{
			if(paletteImage.IsNull())
				return;
			//cycleIterEvt.Record();
			iterateKernel.LaunchAsync(mainStream, (uint)numIterationsPerThread);
			//cycleStatEvt.Record();
			updateStatsKernel.LaunchAsync(mainStream);
			//cycleEndEvt.Record();
			
			//double iterationCycleTime = Event.ElapsedTime(cycleIterEvt,cycleStatEvt);
		}

		public override void CalcToneMap()
		{
			//toneBeginEvt.Record(mainStream);
			DeviceBuffer2D glOutputBuffer = DeviceBuffer2D.GLMapBufferObject(glOutputBufferID, (uint)(4*xRes));
			updateOutputKernel.LaunchAsync(mainStream, glOutputBuffer);
			glOutputBuffer.GLUnmapBufferObject(glOutputBufferID);
			//toneEndEvt.Record(mainStream);
		}

		public override void CopyToneMap()
		{
			mainStream.Synchronize();
			context.Synchronize();

			//Performance.Start("tex_copy");
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, glOutputBufferID);
			GL.BindTexture(TextureTarget.Texture2D, glOutputTexID);
			GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, xRes, yRes,
				PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
			GL.BindTexture(TextureTarget.Texture2D, 0);
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
			//Performance.Stop("tex_copy");
		}

		public override FractalEngine.Stats GatherStats()
		{
			mainStream.Synchronize();
			context.Synchronize();

			Stats stats = new Stats();

			CudaMem.CopyStructRaw(totalIterCountMem, out stats.TotalIterCount);
			CudaMem.CopyStructRaw(totalDotCountMem,  out stats.TotalDotCount);

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
				throw new FractalException("The fractal engine is not ready.");
			mainStream.Synchronize();
			context.Synchronize();

			return GetPixelsFromBuffer(xRes, yRes, glOutputBufferID);
		}

		#region Device Queries
		private static CudaDeviceEntry[] devEntries = new CudaDeviceEntry[0];
		
		private static void queryDevices()
		{
			Device[] devList = Device.Devices;
			devEntries = new CudaDeviceEntry[devList.Length];
			for(int i = 0; i < devList.Length; i++)
				devEntries[i] = new CudaDeviceEntry(devList[i]);
		}

		public static void CheckSupport()
		{
			try{
				if(devEntries.Length <= 0) //Try again to query the devices
					queryDevices();

				if(devEntries.Length <= 0)
					throw new NotSupportedException("No CUDA Capable devices could be found.");
			}
			catch(DllNotFoundException ex)
			{
				throw new NotSupportedException("CUDA library not found.", ex);
			}
		}

		public static DeviceEntry[] GetDevices()
		{
			try{
				if(devEntries.Length <= 0)
					queryDevices();
			}catch{
				devEntries = new CudaDeviceEntry[0];
			}
 			return devEntries;
		}
		#endregion
	}
}