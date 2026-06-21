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
	public struct HostBuffer2D<T> : IEnumerable<T> where T : struct
	{
		internal IntPtr ptr;           //The device pointer
		internal int width;
		internal int height;

		internal HostBuffer2D(IntPtr ptr, int width, int height)
		{
			this.ptr = ptr;
			this.width = width;
			this.height = height;
		}

		public static HostBuffer2D<T> Null{
			get{ return new HostBuffer2D<T>(IntPtr.Zero, 0, 0); }
		}

		/// <summary>
		/// Checks if this buffer is null.
		/// </summary>
		/// <returns>Returns true if this buffer is null.</returns>
		public bool IsNull()
		{
			return ptr == null;
		}

		/// <summary>
		/// Gets the number of columns in the buffer
		/// </summary>
		public int Width{
			get{ return width; }
		}

		/// <summary>
		/// Gets the number of rows in the buffer
		/// </summary>
		public int Height{
			get{ return height; }
		}

		/// <summary>
		/// Gets the number of elements in the buffer
		/// </summary>
		public int Count{
			get{ return width*height; }
		}

		/// <summary>
		/// Gets a pointer to the beginning of the buffer.
		/// </summary>
		public IntPtr Ptr{
			get{ return (IntPtr)ptr; }
		}

		/// <summary>
		/// Gets the element size in bytes
		/// </summary>
		public int ElemSize{
			get{ return Marshal.SizeOf(typeof(T)); }
		}

		/// <summary>
		/// Gets the size of each row in bytes
		/// </summary>
		public int RowSizeBytes{
			get{ return ElemSize*width; }
		}

		/// <summary>
		/// Gets the total buffer size in bytes
		/// </summary>
		public int SizeInBytes{
			get{ return ElemSize*width*height; }
		}

		public T this[int row, int col]{
			get{
				return (T)Marshal.PtrToStructure(GetElemPtr(row,col), typeof(T));
			}
			set{
				Marshal.StructureToPtr(value,GetElemPtr(row,col), false);
			}
		}

		public static HostBuffer2D<T> Alloc(int width, int height)
		{
			HostBuffer2D<T> result = new HostBuffer2D<T>();

			result.ptr = RawHostBuffer.Alloc((uint)result.ElemSize*(uint)width*(uint)height);
			result.width = width;
			result.height = height;

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
				CudaMem.cuMemFreeHost(ptr);
				ptr = IntPtr.Zero;
				width = 0;
				height = 0;
			}
		}

		/// <summary>
		/// Gets a pointer to an element
		/// </summary>
		/// <param name="row">The row of the element</param>
		/// <param name="col">The column of the element</param>
		/// <returns></returns>
		public IntPtr GetElemPtr(int row, int col)
		{
			if(row < 0 || row >= height || col < 0 || col >= width)
				throw new IndexOutOfRangeException();
			unsafe{
				return (IntPtr)((byte*)ptr + ElemSize*(width*row + col));
			}
		}

		#region IEnumerable Members
		public IEnumerator<T> GetEnumerator()
		{
			for(int r = 0; r < height; r++)
				for(int c = 0; c < width; c++)
					yield return this[r,c];
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		#endregion
	}
}
