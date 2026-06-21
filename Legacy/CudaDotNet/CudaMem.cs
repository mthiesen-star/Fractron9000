#region License
/*
    CudaDotNet - Compatability layer between NVidia CUDA and the .NET framework
    Copyright (C) 2009 Michael J. Thiesen
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
using System.Runtime.InteropServices;

namespace Cuda
{
	enum MemoryType{
		Invalid = 0x00,
		Host = 0x01,
		Device = 0x02,
		Array = 0x03
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct MemCopy2DParams
	{
		public UInt32 srcXInBytes;
		public UInt32 srcY;
		MemoryType srcMemoryType;
	}

	public static class CudaMem
	{
		/// <summary>
		/// Copies data from host memory to device memory. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination pointer</param>
		/// <param name="sizeInBytes">The number of bytes to copy</param>
		public static void CopyRaw(IntPtr srcPtr, DevicePtr dest, IntPtr sizeInBytes)
		{
			if(srcPtr == IntPtr.Zero || dest == DevicePtr.Zero)
				throw new NullPointerException();

			CudaUtil.Call(cuMemcpyHtoD(dest.ptr, srcPtr, (uint)sizeInBytes));
		}

		/// <summary>
		/// Copies data from host memory to device memory asynchronously. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination pointer, must be to page-locked memory</param>
		/// <param name="sizeInBytes">The number of bytes to copy</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyRawAsync(IntPtr srcPtr, DevicePtr dest, IntPtr sizeInBytes, Stream stream)
		{
			if(srcPtr == IntPtr.Zero || dest == DevicePtr.Zero)
				throw new NullPointerException();

			CudaUtil.Call(cuMemcpyHtoDAsync(dest.ptr, srcPtr, (uint)sizeInBytes, stream.Handle));
		}

		/// <summary>
		/// Copies data from host memory to device memory. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination pointer</param>
		/// <param name="widthInBytes">The size of each row in bytes</param>
		/// <param name="height">The number of rows</param>
		public static void CopyRaw2D(IntPtr srcPtr, DevicePtr2D dest, IntPtr widthInBytes, IntPtr height)
		{
			if(srcPtr == IntPtr.Zero || dest == DevicePtr2D.Zero)
				throw new NullPointerException();

			CudaUtil.Call(CudaMem.memcpy2DHelper(
				MemoryType.Host,   0, 0, srcPtr,           (uint)widthInBytes,
				MemoryType.Device, 0, 0, (IntPtr)dest.ptr, (uint)dest.Pitch,
				(uint)widthInBytes, (uint)height)
			);
		}

		/// <summary>
		/// Copies data from host memory to device memory asynchronously. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source pointer, must be to page locked memory</param>
		/// <param name="dest">The destination pointer</param>
		/// <param name="widthInBytes">The size of each row in bytes</param>
		/// <param name="height">The number of rows</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyRaw2DAsync(IntPtr srcPtr, DevicePtr2D dest, IntPtr widthInBytes, IntPtr height, Stream stream)
		{
			if(srcPtr == IntPtr.Zero || dest == DevicePtr2D.Zero)
				throw new NullPointerException();

			CudaUtil.Call(CudaMem.memcpy2DAsyncHelper(
				MemoryType.Host,   0, 0, srcPtr,           (uint)widthInBytes,
				MemoryType.Device, 0, 0, (IntPtr)dest.ptr, (uint)dest.Pitch,
				(uint)widthInBytes, (uint)height, stream.Handle
			));
		}

		/// <summary>
		/// Copies data from device memory to host memory. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination buffer</param>
		/// <param name="sizeInBytes">The number of bytes to copy</param>
		public static void CopyRaw(DevicePtr src, IntPtr destPtr, IntPtr sizeInBytes)
		{
			if(src == DevicePtr.Zero || destPtr == IntPtr.Zero)
				throw new NullPointerException();
			CudaUtil.Call(cuMemcpyDtoH(destPtr, src.ptr, (uint)sizeInBytes));
		}
		
		/// <summary>
		/// Copies data from device memory to host memory asynchronously. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination pointer, must be to page locked memory</param>
		/// <param name="sizeInBytes">The number of bytes to copy</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyRawAsync(DevicePtr src, IntPtr destPtr, IntPtr sizeInBytes, Stream stream)
		{
			if(src == DevicePtr.Zero || destPtr == IntPtr.Zero)
				throw new NullPointerException();
			CudaUtil.Call(cuMemcpyDtoHAsync(destPtr, src.ptr, (uint)sizeInBytes, stream.Handle));
		}
		
		/// <summary>
		/// Copies data from host memory to device memory.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination pointer</param>
		public static void Copy<T>(HostBuffer1D<T> src, DeviceBuffer dest) where T : struct
		{
			if(src.IsNull() || dest.ptr==DevicePtr.Zero)
				throw new NullPointerException();
			if(src.SizeInBytes != dest.SizeInBytes)
				throw new ArgumentException("The source and destination sizes do not match.");

			CudaUtil.Call(cuMemcpyHtoD(dest.ptr.ptr, src.ptr, (uint)src.SizeInBytes));
		}

		/// <summary>
		/// Copies data from host memory to device memory asynchronously.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination pointer</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyAsync<T>(HostBuffer1D<T> src, DeviceBuffer dest, Stream stream) where T : struct
		{
			if(src.IsNull() || dest.ptr==DevicePtr.Zero)
				throw new NullPointerException();
			if(src.SizeInBytes != dest.SizeInBytes)
				throw new ArgumentException("The source and destination sizes do not match.");

			CudaUtil.Call(cuMemcpyHtoDAsync(dest.ptr.ptr, src.ptr, (uint)src.SizeInBytes, stream.Handle));
		}

		/// <summary>
		/// Copies a single struct to device memory.
		/// </summary>
		/// <param name="src">The source struct</param>
		/// <param name="dest">The destination buffer</param>
		public static void CopyStruct<T>(T src, DeviceBuffer dest) where T : struct
		{
			if(dest.ptr == DevicePtr.Zero)
				throw new NullPointerException();
			uint sizeInBytes = (uint)Marshal.SizeOf(src);
			if(sizeInBytes != dest.sizeInBytes)
				throw new ArgumentException("The source and destination sizes do not match.");

			IntPtr srcPtr = Marshal.AllocHGlobal((IntPtr)sizeInBytes);
			Marshal.StructureToPtr(src, srcPtr, false);
			CudaUtil.Call(cuMemcpyHtoD(dest.ptr.ptr, srcPtr, sizeInBytes));
			Marshal.FreeHGlobal(srcPtr);
		}

		/// <summary>
		/// Copies data from host memory to device memory.
		/// </summary>
		/// <param name="src">The source array</param>
		/// <param name="dest">The destination buffer</param>
		public static void Copy<T>(T[] src, DeviceBuffer dest) where T : struct
		{
			if(src == null || dest.ptr == DevicePtr.Zero)
				throw new NullPointerException();

			IntPtr srcPtr = AllocArrayFor(src, dest.sizeInBytes);
			ArrayToPtr(src, srcPtr);
			CudaUtil.Call(cuMemcpyHtoD(dest.ptr.ptr, srcPtr, dest.sizeInBytes));
			Marshal.FreeHGlobal(srcPtr);
		}

		/// <summary>
		/// Copies data from host memory to device memory.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination buffer</param>
		public static void Copy<T>(HostBuffer2D<T> src, DeviceBuffer2D dest) where T : struct
		{
			if(src.IsNull() || dest.ptr == DevicePtr2D.Zero)
				throw new NullPointerException();
			if((uint)src.RowSizeBytes != dest.WidthInBytes || (uint)src.Height != dest.Height)
				throw new ArgumentException("The source and destination sizes do not match.");

			CudaUtil.Call(CudaMem.memcpy2DHelper(
				MemoryType.Host,   0, 0, src.ptr, (uint)src.RowSizeBytes,
				MemoryType.Device, 0, 0, (IntPtr)dest.ptr.ptr, dest.ptr.pitch,
				(uint)src.RowSizeBytes, (uint)src.height)
			);
		}

		/// <summary>
		/// Copies data from host memory to device memory asynchronously.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination buffer</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void Copy<T>(HostBuffer2D<T> src, DeviceBuffer2D dest, Stream stream) where T : struct
		{
			if(src.IsNull() || dest.ptr == DevicePtr2D.Zero)
				throw new NullPointerException();
			if((uint)src.RowSizeBytes != dest.WidthInBytes || (uint)src.Height != dest.Height)
				throw new ArgumentException("The source and destination sizes do not match.");

			CudaUtil.Call(CudaMem.memcpy2DAsyncHelper(
				MemoryType.Host,   0, 0, src.ptr, (uint)src.RowSizeBytes,
				MemoryType.Device, 0, 0, (IntPtr)dest.ptr.ptr, dest.ptr.pitch,
				(uint)src.RowSizeBytes, (uint)src.height, stream.Handle
			));
		}

		/// <summary>
		/// Copies data from host memory to device memory.
		/// </summary>
		/// <param name="src">The source array</param>
		/// <param name="dest">The destination pointer</param>
		public static void Copy<T>(T[,] src, DeviceBuffer2D dest) where T : struct
		{
			if(src == null || dest.ptr == DevicePtr2D.Zero)
				throw new NullPointerException();


			IntPtr srcPtr = AllocArrayFor(src, dest.widthInBytes, dest.height);
			ArrayToPtr(src, srcPtr);
			CudaUtil.Call(memcpy2DHelper(
				MemoryType.Host,   0, 0, srcPtr, dest.widthInBytes,
				MemoryType.Device, 0, 0, (IntPtr)dest.ptr.ptr, dest.ptr.pitch,
				dest.widthInBytes, dest.height)
			);

			Marshal.FreeHGlobal(srcPtr);
		}

		/// <summary>
		/// Copies data from host memory to device memory.
		/// </summary>
		/// <param name="src">The source array</param>
		/// <param name="dest">The destination CUDA array</param>
		public static void Copy<T>(T[,] src, CudaArray dest) where T : struct
		{
			if(src == null || dest.handle == IntPtr.Zero)
				throw new NullPointerException();

			int elemSize = Marshal.SizeOf(typeof(T));
			int width  = src.GetLength(1);
			int height = src.GetLength(0);

			CudaArray.Descriptor desc = dest.GetDescriptor();

			if(desc.Width != width)
				throw new ArgumentException("The source and destination width do not match.");
			if(desc.Height != height)
				throw new ArgumentException("The source and destination height do not match.");

			IntPtr srcPtr = Marshal.AllocHGlobal(elemSize * width * height);
			ArrayToPtr(src, srcPtr);
			CudaUtil.Call(memcpy2DHelper(
				MemoryType.Host,   0, 0, srcPtr, (uint)(elemSize*width),
				MemoryType.Array,  0, 0, dest.handle, 0,
				(uint)(elemSize*width), (uint)height)
			);

			Marshal.FreeHGlobal(srcPtr);
		}

		/// <summary>
		/// Copies data from host memory to a cuda array. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination pointer</param>
		public static void Copy<T>(HostBuffer2D<T> src, CudaArray dest) where T : struct
		{
			if(src.IsNull() || dest.IsNull())
				throw new NullPointerException();

			CudaUtil.Call(CudaMem.memcpy2DHelper(
				MemoryType.Host,  0, 0, src.ptr, (uint)src.RowSizeBytes,
				MemoryType.Array, 0, 0, dest.handle, 0,
				(uint)src.RowSizeBytes, (uint)src.height)
			);
		}

		/// <summary>
		/// Copies data from host memory to a cuda array asynchronously. No size checks are performed, it is
		/// up to the programmer to ensure the destination is large enough to hold the data.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination pointer</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyAsync<T>(HostBuffer2D<T> src, CudaArray dest, Stream stream) where T : struct
		{
			if(src.IsNull() || dest.IsNull())
				throw new NullPointerException();

			CudaUtil.Call(CudaMem.memcpy2DAsyncHelper(
				MemoryType.Host,  0, 0, src.ptr, (uint)src.RowSizeBytes,
				MemoryType.Array, 0, 0, dest.handle, 0,
				(uint)src.RowSizeBytes, (uint)src.height, stream.Handle
			));
		}

		/// <summary>
		/// Copies data from device memory to host memory.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination buffer</param>
		public static void Copy<T>(DeviceBuffer src, HostBuffer1D<T> dest) where T : struct
		{
			if(src.ptr == DevicePtr.Zero || dest.IsNull())
				throw new NullPointerException();
			if(src.sizeInBytes != (uint)dest.SizeInBytes)
				throw new ArgumentException("The source and destination sizes do not match.");

			CudaUtil.Call(cuMemcpyDtoH(dest.ptr, src.ptr.ptr, (uint)dest.SizeInBytes));
		}

		/// <summary>
		/// Copies data from device memory to host memory asynchronously.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination buffer</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyAsync<T>(DeviceBuffer src, HostBuffer1D<T> dest, Stream stream) where T : struct
		{
			if(src.ptr == DevicePtr.Zero || dest.IsNull())
				throw new NullPointerException();
			if(src.sizeInBytes != (uint)dest.SizeInBytes)
				throw new ArgumentException("The source and destination sizes do not match.");

			CudaUtil.Call(cuMemcpyDtoHAsync(dest.ptr, src.ptr.ptr, (uint)dest.SizeInBytes, stream.Handle));
		}

		/// <summary>
		/// Copies data from device memory to host memory.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination buffer</param>
		public static void Copy<T>(DeviceBuffer2D src, HostBuffer2D<T> dest) where T : struct
		{
			if(src.ptr == DevicePtr2D.Zero || dest.IsNull())
				throw new NullPointerException();
			if(src.widthInBytes != (uint)dest.RowSizeBytes || src.height != (uint)dest.height)
				throw new ArgumentException("The source and destination sizes do not match.");
			
			CudaUtil.Call(memcpy2DHelper(
				MemoryType.Device, 0, 0, (IntPtr)src.ptr.ptr, src.ptr.pitch,
				MemoryType.Host,   0, 0, dest.Ptr, (uint)dest.RowSizeBytes,
				(uint)dest.RowSizeBytes, (uint)dest.Height )
			);
		}

		/// <summary>
		/// Copies data from device memory to host memory asynchronously.
		/// </summary>
		/// <param name="src">The source pointer</param>
		/// <param name="dest">The destination buffer</param>
		/// <param name="stream">The stream in which to queue the copy operation</param>
		public static void CopyAsync<T>(DeviceBuffer2D src, HostBuffer2D<T> dest, Stream stream) where T : struct
		{
			if(src.ptr == DevicePtr2D.Zero || dest.IsNull())
				throw new NullPointerException();
			if(src.widthInBytes != (uint)dest.RowSizeBytes || src.height != (uint)dest.height)
				throw new ArgumentException("The source and destination sizes do not match.");
			
			CudaUtil.Call(memcpy2DAsyncHelper(
				MemoryType.Device, 0, 0, (IntPtr)src.ptr.ptr, src.ptr.pitch,
				MemoryType.Host,   0, 0, dest.Ptr, (uint)dest.RowSizeBytes,
				(uint)dest.RowSizeBytes, (uint)dest.Height, stream.Handle )
			);
		}

		/// <summary>
		/// Copies data from device memory to a host struct
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination struct</param>
		public static void CopyStruct<T>(DeviceBuffer src, out T dest) where T : struct
		{
			if(src.ptr == DevicePtr.Zero)
				throw new NullPointerException();
			uint sizeInBytes = (uint)Marshal.SizeOf(typeof(T));
			if(sizeInBytes != src.sizeInBytes)
				throw new ArgumentException("The source and destination sizes do not match.");

			IntPtr destPtr = Marshal.AllocHGlobal((IntPtr)sizeInBytes);
			CudaUtil.Call(cuMemcpyDtoH(destPtr, src.ptr.ptr, sizeInBytes));
			dest = (T)Marshal.PtrToStructure(destPtr, typeof(T));
			Marshal.FreeHGlobal(destPtr);
		}

		/// <summary>
		/// Copies data from device memory to a host struct. No size checking is performed.
		/// </summary>
		/// <param name="src">The source buffer</param>
		/// <param name="dest">The destination struct</param>
		public static void CopyStructRaw<T>(DevicePtr src, out T dest) where T : struct
		{
			if(src == DevicePtr.Zero)
				throw new NullPointerException();
			uint sizeInBytes = (uint)Marshal.SizeOf(typeof(T));

			IntPtr destPtr = Marshal.AllocHGlobal((IntPtr)sizeInBytes);
			CudaUtil.Call(cuMemcpyDtoH(destPtr, src.ptr, sizeInBytes));
			dest = (T)Marshal.PtrToStructure(destPtr, typeof(T));
			Marshal.FreeHGlobal(destPtr);
		}

	
		/// <summary>
		/// Copies data from device memory to host memory.
		/// </summary>
		/// <param name="src">The source array</param>
		/// <param name="dest">The destination array</param>
		public static void Copy<T>(DeviceBuffer src, T[] dest) where T : struct
		{
			if(src.ptr == DevicePtr.Zero || dest == null)
				throw new NullPointerException();

			IntPtr destPtr = AllocArrayFor(dest, src.sizeInBytes);
			CudaUtil.Call(cuMemcpyDtoH(destPtr, src.ptr.ptr, src.sizeInBytes));
			PtrToArray(destPtr,dest);
			Marshal.FreeHGlobal(destPtr);
		}

		/// <summary>
		/// Copies data from device memory to host memory.
		/// </summary>
		/// <param name="src">The source array</param>
		/// <param name="dest">The destination array</param>
		public static void Copy<T>(DeviceBuffer2D src, T[,] dest) where T : struct
		{
			if(src.ptr == DevicePtr2D.Zero || dest == null)
				throw new NullPointerException();

			IntPtr destPtr = AllocArrayFor(dest, src.widthInBytes, src.height);
			CudaUtil.Call(memcpy2DHelper(
				MemoryType.Device, 0, 0, (IntPtr)src.ptr.ptr, src.ptr.pitch,
				MemoryType.Host,   0, 0, destPtr, src.widthInBytes,
				src.widthInBytes, src.height)
			);
			PtrToArray(destPtr,dest);
			Marshal.FreeHGlobal(destPtr);
		}

		/// <summary>
		/// Copies data from device memory to device memory.
		/// </summary>
		/// <param name="src">The source array</param>
		/// <param name="dest">The destination array</param>
		public static void Copy(DeviceBuffer2D src, DeviceBuffer2D dest)
		{
			if(src.ptr == DevicePtr2D.Zero || dest.ptr == DevicePtr2D.Zero)
				throw new NullPointerException();

			CudaUtil.Call(memcpy2DUnalignedHelper(
				MemoryType.Device, 0, 0, (IntPtr)src.ptr.ptr,  src.ptr.pitch,
				MemoryType.Device, 0, 0, (IntPtr)dest.ptr.ptr, dest.ptr.pitch,
				src.widthInBytes, src.height)
			);
		}


		private static IntPtr AllocArrayFor<T>(T[] arr, uint sizeInBytes)
		{
			uint sb = (uint)Marshal.SizeOf(typeof(T)) * (uint)arr.Length;
			if(sizeInBytes != sb)
				throw new ArgumentException("The source and destination sizes do not match.");
			return Marshal.AllocHGlobal((IntPtr)sb);
		}
		private static IntPtr AllocArrayFor<T>(T[,] arr, uint widthInBytes, uint height)
		{
			uint wb = (uint)arr.GetLength(1) * (uint)Marshal.SizeOf(typeof(T));
			if(height != (uint)arr.GetLength(0) || widthInBytes != wb)
				throw new ArgumentException("The source and destination sizes do not match.");
			return Marshal.AllocHGlobal((IntPtr)(wb*height));
		}

		private static void ArrayToPtr<T>(T[] arr, IntPtr ptr)
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint sizeInBytes = elemSize * (uint)arr.Length;
			unsafe{
				byte* elemPtr = (byte*)ptr;
				for(int i = 0; i < arr.Length; i++)
				{
					Marshal.StructureToPtr(arr[i], (IntPtr)elemPtr, false);
					elemPtr += elemSize;
				}
			}
		}
		private static void ArrayToPtr<T>(T[,] arr, IntPtr ptr)
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint widthInBytes = elemSize * (uint)arr.GetLength(1);
			uint height = (uint)arr.GetLength(0);

			unsafe{
				byte* elemPtr = (byte*)ptr;
				for(int r = 0; r < arr.GetLength(0); r++)
				{
					for(int c = 0; c < arr.GetLength(1); c++)
					{
						Marshal.StructureToPtr(arr[r,c], (IntPtr)elemPtr, false);
						elemPtr += elemSize;
					}
				}
			}
		}

		private static void PtrToArray<T>(IntPtr ptr, T[] arr)
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint sizeInBytes = elemSize * (uint)arr.Length;

			unsafe{
				byte* elemPtr = (byte*)ptr;
				for(int i = 0; i < arr.Length; i++)
				{
					arr[i] = (T)Marshal.PtrToStructure((IntPtr)elemPtr, typeof(T));
					elemPtr += elemSize;
				}
			}
		}

		private static void PtrToArray<T>(IntPtr ptr, T[,] arr)
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint widthInBytes = elemSize * (uint)arr.GetLength(1);
			uint height = (uint)arr.GetLength(0);

			unsafe{
				byte* elemPtr = (byte*)ptr;
				for(int r = 0; r < arr.GetLength(0); r++)
				{
					for(int c = 0; c < arr.GetLength(1); c++)
					{
						arr[r,c] = (T)Marshal.PtrToStructure((IntPtr)elemPtr, typeof(T));
						elemPtr += elemSize;
					}
				}
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MemCpy2D
		{
			public MemCpy2D(
				MemoryType srcType, UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
				MemoryType dstType, UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
				UInt32 widthInBytes, UInt32 height )
			{
				this.SrcXInBytes = srcXInBytes;
				this.SrcY = srcY;
				this.SrcMemoryType = (uint)srcType;
				this.SrcHost = IntPtr.Zero;
				this.SrcDevice = 0;
				this.SrcArray = IntPtr.Zero;
				switch(srcType){
					case MemoryType.Host:   this.SrcHost = srcPtr;           break;
					case MemoryType.Device: this.SrcDevice = (UInt32)srcPtr; break;
					case MemoryType.Array:  this.SrcArray = srcPtr;          break;
				}
				this.SrcPitch = srcPitch;

				this.DstXInBytes = dstXInBytes;
				this.DstY = dstY;
				this.DstMemoryType = (uint)dstType;
				this.DstHost = IntPtr.Zero;
				this.DstDevice = 0;
				this.DstArray = IntPtr.Zero;
				switch(dstType){
					case MemoryType.Host:   this.DstHost = dstPtr;           break;
					case MemoryType.Device: this.DstDevice = (UInt32)dstPtr; break;
					case MemoryType.Array:  this.DstArray = dstPtr;          break;
				}
				this.DstPitch = dstPitch;

				this.WidthInBytes = widthInBytes;
				this.Height = height;
			}

			public uint   SrcXInBytes;    ///< Source X in bytes
			public uint   SrcY;           ///< Source Y
			public uint   SrcMemoryType;  ///< Source memory type (host, device, array)
			public IntPtr SrcHost;        ///< Source host pointer
			public uint   SrcDevice;      ///< Source device pointer
			public IntPtr SrcArray;       ///< Source array reference
			public uint   SrcPitch;       ///< Source pitch (ignored when src is array)

			public uint   DstXInBytes;    ///< Destination X in bytes
			public uint   DstY;           ///< Destination Y
			public uint   DstMemoryType;  ///< Destination memory type (host, device, array)
			public IntPtr DstHost;        ///< Destination host pointer
			public uint   DstDevice;      ///< Destination device pointer
			public IntPtr DstArray;       ///< Destination array reference
			public uint   DstPitch;       ///< Destination pitch (ignored when dst is array)

			public uint   WidthInBytes;   ///< Width of 2D memory copy in bytes
			public uint   Height;         ///< Height of 2D memory copy
		}
		
		private static CudaResult memcpy2DHelper(
			MemoryType srcType, UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
			MemoryType dstType, UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
			UInt32 widthInBytes, UInt32 height
		){
			MemCpy2D args = new MemCpy2D(
				srcType, srcXInBytes, srcY, srcPtr, srcPitch,
				dstType, dstXInBytes, dstY, dstPtr, dstPitch,
				widthInBytes, height );

			return cuMemcpy2D(ref args);
		}
		
		private static CudaResult memcpy2DAsyncHelper(
			MemoryType srcType,
			UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
			MemoryType dstType,
			UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
			UInt32 widthInBytes, UInt32 height, IntPtr hStream
		)
		{
			MemCpy2D args = new MemCpy2D(
				srcType, srcXInBytes, srcY, srcPtr, srcPitch,
				dstType, dstXInBytes, dstY, dstPtr, dstPitch,
				widthInBytes, height );

			return cuMemcpy2DAsync(ref args, hStream);
		}
		
		private static CudaResult memcpy2DUnalignedHelper(
			MemoryType srcType, UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
			MemoryType dstType, UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
			UInt32 widthInBytes, UInt32 height
		)
		{
			MemCpy2D args = new MemCpy2D(
				srcType, srcXInBytes, srcY, srcPtr, srcPitch,
				dstType, dstXInBytes, dstY, dstPtr, dstPitch,
				widthInBytes, height );

			return cuMemcpy2DUnaligned(ref args);
		}

        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemAllocHost(out IntPtr hostPtr, UInt32 count);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemFreeHost(IntPtr hostPtr);

        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemcpyHtoD(UInt32 dstDevPtr, IntPtr srcHostPtr, UInt32 count);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemcpyHtoDAsync(UInt32 dstDevPtr, IntPtr srcHostPtr, UInt32 count, IntPtr hStream);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemcpyHtoA(IntPtr dstArray, UInt32 dstIndex, IntPtr srcHostPtr, UInt32 count);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemcpyHtoAAsync(IntPtr dstArray, UInt32 dstIndex, IntPtr srcHostPtr, UInt32 count, IntPtr hStream);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemcpyDtoH(IntPtr dstHostPtr, UInt32 srcDevPtr, UInt32 count);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemcpyDtoHAsync(IntPtr dstHostPtr, UInt32 srcDevPtr, UInt32 count, IntPtr hStream);

		
        [DllImport("nvcuda.dll")]
		private  static extern CudaResult cuMemcpy2D(ref MemCpy2D args);
        [DllImport("nvcuda.dll")]
		private  static extern CudaResult cuMemcpy2DAsync(ref MemCpy2D args, IntPtr hStream);
        [DllImport("nvcuda.dll")]
		private  static extern CudaResult cuMemcpy2DUnaligned(ref MemCpy2D args);

		//[DllImport("cuda9k.dll")]
		//internal static extern CudaResult memcpy2DHelper(
		//	[MarshalAs(UnmanagedType.U4)] MemoryType srcType,
		//	UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
		//	[MarshalAs(UnmanagedType.U4)] MemoryType dstType,
		//	UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
		//	UInt32 widthInBytes, UInt32 height
		//);
		
		//[DllImport("cuda9k.dll")]
		//internal static extern CudaResult memcpy2DAsyncHelper(
		//	[MarshalAs(UnmanagedType.U4)] MemoryType srcType,
		//	UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
		//	[MarshalAs(UnmanagedType.U4)] MemoryType dstType,
		//	UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
		//	UInt32 widthInBytes, UInt32 height, IntPtr hStream
		//);
		
		//[DllImport("cuda9k.dll")]
		//internal static extern CudaResult memcpy2DUnalignedHelper(
		//	[MarshalAs(UnmanagedType.U4)] MemoryType srcType,
		//	UInt32 srcXInBytes, UInt32 srcY, IntPtr srcPtr, UInt32 srcPitch,
		//	[MarshalAs(UnmanagedType.U4)] MemoryType dstType,
		//	UInt32 dstXInBytes, UInt32 dstY, IntPtr dstPtr, UInt32 dstPitch,
		//	UInt32 widthInBytes, UInt32 height
		//);

        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemAlloc(out UInt32 ptr, UInt32 count);

        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemAllocPitch(
			out UInt32 devPtr, out UInt32 pitch,
			UInt32 widthInBytes, UInt32 height, UInt32 elementSizeBytes
		);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemFree(UInt32 ptr);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemsetD8(UInt32 dstDevPtr, byte value, UInt32 count);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuMemsetD2D8(uint dstDevPtr, uint dstPitch, byte value, uint width, uint height );


        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuGLMapBufferObject(out UInt32 ptr, out UInt32 size, UInt32 bufferId);
        [DllImport("nvcuda.dll")]
		internal static extern CudaResult cuGLUnmapBufferObject(UInt32 bufferId); 
	}
}