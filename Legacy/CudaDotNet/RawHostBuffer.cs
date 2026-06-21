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
	/// This class contains static methods for managing a raw host buffer. The memory is unmanaged and must be
	/// allocated and freed manually. Memory allocated by RawHostBuffer is page locked. This improves data transfer rates
	/// to and from CUDA devices, but excessive use of page locked memory may degrade overall system performance.
	/// </summary>
	public static class RawHostBuffer
	{
		/// <summary>
		/// Creates a new buffer and allocates the specified ammount of memory.
		/// </summary>
		/// <param name="sizeInBytes">The number of bytes to allocate for the buffer</param>
		/// <returns>A pointer to the allocated buffer</returns>
		public static IntPtr Alloc(uint sizeInBytes)
		{
			IntPtr result = IntPtr.Zero;
			CudaUtil.Call(CudaMem.cuMemAllocHost(out result, (UInt32)sizeInBytes));
			return result;
		}

		/// <summary>
		/// Releases any memory used by this buffer, and sets it to null. This method may be
		/// called on a null pointer, but will have no effect.
		/// </summary>
		/// <param name="ptr">A pointer to the buffer to deallocate</param>
		public static void Free(ref IntPtr ptr)
		{
			if(ptr != IntPtr.Zero){
				CudaUtil.Call(CudaMem.cuMemFreeHost(ptr));
				ptr = IntPtr.Zero;
			}
		}
	}
}
