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
	/// Encapsulates a 2D block of memory on a CUDA device
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DeviceBuffer2D
	{
		internal DevicePtr2D ptr;
		internal uint widthInBytes;
		internal uint height;
		
		internal DeviceBuffer2D(DevicePtr2D ptr, uint widthInBytes, uint height)
		{
			this.ptr = ptr;
			this.widthInBytes = widthInBytes;
			this.height = height;
		}

		/// <summary>
		/// Gets the device pointer.
		/// </summary>
		public DevicePtr2D Ptr{
			get{ return ptr; }
		}

		/// <summary>
		/// Gets the ammount of memory used in each row in bytes
		/// </summary>
		public UInt32 WidthInBytes{
			get{ return widthInBytes; }
		}

		/// <summary>
		/// Gets the number of rows
		/// </summary>
		public uint Height{
			get{ return height; }
		}

		/// <summary>
		/// Gets the size of the buffer (not including padding)
		/// </summary>
		public uint SizeInBytes{
			get{ return widthInBytes*height; }
		}


		/// <summary>
		/// Allocates a 2D block of memory, and returns a pointer pointing to it. 
		/// </summary>
		/// <param name="elemSizeInBytes">The byte size of each element.</param>
		/// <param name="width">The number of columns</param>
		/// <param name="height">The number of rows</param>
		/// <returns>A pointer to the block of new memory</returns>
		public static DeviceBuffer2D Alloc(int elemSizeInBytes, int width, int height)
		{
			DeviceBuffer2D result = new DeviceBuffer2D();
			result.widthInBytes = (uint)elemSizeInBytes * (uint)width;
			result.height = (uint)height;

			CudaUtil.Call(CudaMem.cuMemAllocPitch(out result.ptr.ptr, out result.ptr.pitch,
				result.widthInBytes, result.height, (uint)elemSizeInBytes));
			return result;
		}
		
		/// <summary>
		/// Releases memory in this buffer. This method may be called on a null
		/// buffer, but will have no effect.
		/// </summary>
		public void Free()
		{
			if(ptr.ptr != 0){
				ptr.Free();
				widthInBytes = 0;
				height = 0;
			}
		}

		/// <summary>
		/// Fills bytes in this buffer with the given value.
		/// </summary>
		/// <param name="value">The value to fill the bytes with</param>
		/// <param name="widthInBytes">The number of bytes in each row</param>
		/// <param name="height">The number of rows</param>
		public void MemSet(byte value)
		{
			if(ptr.ptr==0) throw new NullPointerException();
			CudaUtil.Call(CudaMem.cuMemsetD2D8(ptr.ptr, ptr.pitch, value, widthInBytes, height));
		}

		public static DeviceBuffer2D GLMapBufferObject(UInt32 bufferId, uint widthInBytes)
		{
			DeviceBuffer2D result = new DeviceBuffer2D();
			UInt32 size;
			CudaUtil.Call(CudaMem.cuGLMapBufferObject(out result.ptr.ptr, out size, bufferId));
			result.ptr.pitch = widthInBytes;
			result.widthInBytes = widthInBytes;
			result.height = size/widthInBytes;
			return result;
		}
		
		public void GLUnmapBufferObject(UInt32 bufferId)
		{
			CudaUtil.Call(CudaMem.cuGLUnmapBufferObject(bufferId));
			ptr = DevicePtr2D.Zero;
			widthInBytes = 0;
			height = 0;
		}

		public override string ToString()
		{
			return String.Format("{0}({1} rows by {2} bytes)", ptr, height, widthInBytes);
		}
		public override int GetHashCode()
		{
			return ptr.GetHashCode() ^ widthInBytes.GetHashCode() ^ height.GetHashCode();
		}
		public override bool Equals(object o)
		{
			if(o is DeviceBuffer2D)
				return (DeviceBuffer2D)o == this;
			else
				return false;
		}

		public static bool operator==(DeviceBuffer2D a, DeviceBuffer2D b)
		{
			return a.ptr == b.ptr && a.widthInBytes == b.widthInBytes && a.height == b.height;
		}
		public static bool operator!=(DeviceBuffer2D a, DeviceBuffer2D b)
		{
			return !(a==b);
		}
	}
}
