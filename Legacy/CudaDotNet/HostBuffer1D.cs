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
using System.Collections.Generic;
using System.IO;

namespace Cuda
{
	[StructLayout(LayoutKind.Sequential)]
	public struct HostBuffer1D<T> : IEnumerable<T> where T : struct 
	{
		internal IntPtr ptr;           //The device pointer
		internal int length;

		/// <summary>
		/// Checks if this buffer is null.
		/// </summary>
		/// <returns>Returns true if this buffer is null.</returns>
		public bool IsNull()
		{
			return ptr == null;
		}

		/// <summary>
		/// Gets the number of elements in the buffer
		/// </summary>
		public int Length{
			get{ return length; }
		}

		/// <summary>
		/// Gets the number of elements in the buffer
		/// </summary>
		public int Count{
			get{ return length; }
		}

		/// <summary>
		/// Gets a pointer to the beginning of the buffer.
		/// </summary>
		public IntPtr Ptr{
			get{ return (IntPtr)ptr; }
		}

		public int ElemSize{
			get{ return Marshal.SizeOf(typeof(T)); }
		}

		public int SizeInBytes{
			get{ return ElemSize*Length; }
		}

		public T this[int index]{
			get{
				return (T)Marshal.PtrToStructure(GetElemPtr(index),typeof(T));
			}
			set{
				Marshal.StructureToPtr(value, GetElemPtr(index), false);
			}
		}

		public static HostBuffer1D<T> Alloc(int length)
		{
			HostBuffer1D<T> result = new HostBuffer1D<T>();
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			CudaUtil.Call(CudaMem.cuMemAllocHost(out result.ptr, elemSize*(uint)length));
			result.length = length;
			return result;
		}

		/// <summary>
		/// Releases any memory used by this buffer. This method may be called on a null
		/// buffer, but will have no effect.
		/// </summary>
		public void Free()
		{
			if(ptr != IntPtr.Zero)
			{
				CudaUtil.Call(CudaMem.cuMemFreeHost(ptr));
				ptr = IntPtr.Zero;
				length = 0;
			}
		}

		public IntPtr GetElemPtr(int index)
		{
			if(index < 0 || index >= length)
				throw new IndexOutOfRangeException();
			unsafe{
				return (IntPtr)((byte*)ptr + ElemSize*index);
			}
		}

		#region IEnumerable Members
		public IEnumerator<T> GetEnumerator()
		{
			for(int i = 0; i < length; i++)
				yield return this[i];
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
