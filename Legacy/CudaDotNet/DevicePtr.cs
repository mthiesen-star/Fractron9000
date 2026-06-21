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
	/// Encapsulates a pointer to memory on a CUDA device. Device memory is not managed, and must be released
	/// with the Free method when no longer needed.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DevicePtr
	{
		internal UInt32 ptr;          //The device pointer
		
		private DevicePtr(UInt32 rawPtr)
		{
			ptr = rawPtr;
		}

		/// <summary>
		/// Gets a null device pointer.
		/// </summary>
		public static DevicePtr Zero{
			get{ return new DevicePtr(0); }
		}
		
		/// <summary>
		/// Gets the raw device pointer.
		/// </summary>
		public UInt32 RawPtr{
			get{ return ptr; }
		}

		/// <summary>
		/// Check if this pointer is null
		/// </summary>
		/// <returns>true if this pointer is null</returns>
		public bool IsNull(){
			return ptr == 0;
		}
		
		/// <summary>
		/// Allocates a block of memory and returns a pointer to it.
		/// </summary>
		/// <param name="sizeInBytes">The ammount of memory to allocate</param>
		/// <returns>A pointer to the new memory</returns>
		public static DevicePtr AllocRaw(uint sizeInBytes)
		{
			DevicePtr result = new DevicePtr();
			CudaUtil.Call(CudaMem.cuMemAlloc(out result.ptr, sizeInBytes));
			return result;
		}

		/// <summary>
		/// Releases memory pointed to by this pointer. This method may be called on a null
		/// pointer, but will have no effect.
		/// </summary>
		public void Free()
		{
			if(ptr != 0){
				CudaUtil.Call(CudaMem.cuMemFree(ptr));
				ptr = 0;
			}
		}
		
		public override string ToString()
		{
			return string.Format("0x{0:X}", ptr);
		}

		public override int GetHashCode()
		{
			return ptr.GetHashCode();
		}

		public override bool Equals(object o)
		{
			if(o is DevicePtr)
				return (DevicePtr)o == this;
			else
				return false;
		}

		public static bool operator==(DevicePtr a, DevicePtr b)
		{
			return a.ptr == b.ptr;
		}
		public static bool operator!=(DevicePtr a, DevicePtr b)
		{
			return a.ptr != b.ptr;
		}
	}
}
