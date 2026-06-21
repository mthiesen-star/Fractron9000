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
	/// Encapsulates a pointer to a two dimensional block of memory on a CUDA device.
	/// Device memory is not managed, and must be released with the Free method when no longer
	/// needed.
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct DevicePtr2D
	{
		internal UInt32 ptr;          //The device pointer
		internal UInt32 pitch;        //The size in bytes allocated to each row
		
		internal DevicePtr2D(uint rawPtr, uint pitch)
		{
			this.ptr = rawPtr;
			this.pitch = pitch;
		}

		/// <summary>
		/// Returns a null 2D device pointer
		/// </summary>
		public static DevicePtr2D Zero{
			get{ return new DevicePtr2D(0,0); }
		}

		/// <summary>
		/// Gets the raw device pointer.
		/// </summary>
		public UInt32 RawPtr{
			get{ return ptr; }
		}

		/// <summary>
		/// Gets the ammount of memory allocated to each row in bytes. This is not necessarily the same ammount
		/// of memory actually used by each row.
		/// </summary>
		public UInt32 Pitch{
			get{ return pitch; }
		}

		/// <summary>
		/// Check if this pointer is null
		/// </summary>
		/// <returns>true if this pointer is null</returns>
		public bool IsNull(){
			return ptr == 0;
		}

		/// <summary>
		/// Allocates a 2D block of memory, and returns a pointer pointing to it. Rows are NOT padded, 
		/// and may not meet the hardware's alignment requirements.
		/// </summary>
		/// <param name="widthInBytes">The number of bytes to allocate for each row.</param>
		/// <param name="height">The number of rows to allocate</param>
		/// <returns>A pointer to the new block</returns>
		public static DevicePtr2D AllocRaw(uint widthInBytes, uint height)
		{
			DevicePtr2D result = new DevicePtr2D();
			result.pitch = widthInBytes;
			
			CudaUtil.Call(CudaMem.cuMemAlloc(out result.ptr, widthInBytes*height));
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
				pitch = 0;
			}
		}

		public override string ToString()
		{
			return ptr.ToString("X");
		}
		public override int GetHashCode()
		{
			return ptr.GetHashCode() ^ pitch.GetHashCode();
		}
		public override bool Equals(object o)
		{
			if(o is DevicePtr2D)
				return (DevicePtr2D)o == this;
			else
				return false;
		}

		public static bool operator==(DevicePtr2D a, DevicePtr2D b)
		{
			return a.ptr == b.ptr && a.pitch == b.pitch;
		}
		public static bool operator!=(DevicePtr2D a, DevicePtr2D b)
		{
			return a.ptr != b.ptr || a.pitch != b.pitch;
		}
	}
}
