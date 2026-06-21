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
using System.Text;
using System.Reflection;
using System.IO;

namespace Cuda
{
	/// <summary>
	/// Encapsulates a 1D block of memory on a CUDA device.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DeviceBuffer
	{
		internal DevicePtr ptr;          //The device pointer
		internal uint sizeInBytes;              //The buffer size in bytes

		internal DeviceBuffer(DevicePtr ptr, uint sizeInBytes)
		{
			this.ptr = ptr;
			this.sizeInBytes = sizeInBytes;
		}
		
		/// <summary>
		/// Gets a device pointer to the begining of the buffer
		/// </summary>
		public DevicePtr Ptr{
			get{ return ptr; }
		}

		/// <summary>
		/// Gets the buffer size in bytes
		/// </summary>
		public uint SizeInBytes{
			get{ return sizeInBytes; }
		}
		
		/// <summary>
		/// Allocates a block of memory sized to hold a 1D array and returns a pointer to it.
		/// </summary>
		/// <param name="elemSizeBytes">The size of each array element in bytes</param>
		/// <param name="width">The number of elements to allocate memory for</param>
		/// <returns>A pointer to the array</returns>
		public static DeviceBuffer Alloc(int elemSizeBytes, int width)
		{
			DeviceBuffer result = new DeviceBuffer();
			result.sizeInBytes = (uint)elemSizeBytes*(uint)width;
			CudaUtil.Call(CudaMem.cuMemAlloc(out result.ptr.ptr, result.sizeInBytes));
			return result;
		}

		/// <summary>
		/// Releases memory pointed to by this pointer. This method may be called on a null
		/// pointer, but will have no effect.
		/// </summary>
		public void Free()
		{
			if(ptr.ptr != 0){
				ptr.Free();
				sizeInBytes = 0;
			}
		}
	
		/// <summary>
		/// Maps a buffer to an OpenGL buffer object.
		/// </summary>
		/// <param name="bufferId">The unsigned integer name of the OpenGL buffer</param>
		/// <returns>A pointer mapped to the OpenGL buffer</returns>
		public static DeviceBuffer GLMapBufferObject(UInt32 bufferId)
		{
			DeviceBuffer result = new DeviceBuffer();
			CudaUtil.Call(CudaMem.cuGLMapBufferObject(out result.ptr.ptr, out result.sizeInBytes, bufferId));
			return result;
		}

		/// <summary>
		/// Removes a mapping to an OpenGL buffer and nullifies this buffer's pointer.
		/// </summary>
		/// <param name="bufferId">The unsigned integer name of the OpenGL buffer</param>
		public void GLUnmapBufferObject(UInt32 bufferId)
		{
			CudaUtil.Call(CudaMem.cuGLUnmapBufferObject(bufferId));
			ptr.ptr = 0;
			sizeInBytes = 0;
		}

		public override string ToString()
		{
			return String.Format("{0}({1} bytes)", ptr, sizeInBytes);
		}

		public override int GetHashCode()
		{
			return ptr.GetHashCode() ^ sizeInBytes.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if(o is DeviceBuffer)
				return (DeviceBuffer)o == this;
			else
				return false;
		}

		public static bool operator==(DeviceBuffer a, DeviceBuffer b)
		{
			return a.ptr == b.ptr && a.sizeInBytes != b.SizeInBytes;
		}
		public static bool operator!=(DeviceBuffer a, DeviceBuffer b)
		{
			return !(a.ptr == b.ptr);
		}
	}
}
