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
	public enum CudaArrayFormat{
		Invalid = 0x00,
		UnsignedInt8 = 0x01,
		UnsignedInt16 = 0x02,
		UnsignedInt32 = 0x03,
		SignedInt8 = 0x08,
		SignedInt16 = 0x09,
		SignedInt32 = 0x0a,
		Half = 0x10,
		Float = 0x20
	};

	[StructLayout(LayoutKind.Sequential)]
	public struct CudaArray
	{
		internal IntPtr handle;

		internal CudaArray(IntPtr handle)
		{
			this.handle = handle;
		}

		public static CudaArray Null{
			get{ return new CudaArray(IntPtr.Zero); }
		}

		public bool IsNull()
		{
			return handle == IntPtr.Zero;
		}

		public static CudaArray Allocate(int width, int height, CudaArrayFormat format, int numChannels)
		{
			CudaArray result = new CudaArray();
			Descriptor desc = new Descriptor();
			desc.Width = (UInt32)width;
			desc.Height = (UInt32)height;
			desc.Format = format;
			desc.NumChannels = (UInt32)numChannels;

			CudaUtil.Call(cuArrayCreate(out result.handle, ref desc));
			return result;
		}
		public void Free()
		{
			if(handle != IntPtr.Zero){
				CudaUtil.Call(cuArrayDestroy(handle));
				handle = IntPtr.Zero;
			}
		}

		public Descriptor GetDescriptor()
		{
			if(handle == IntPtr.Zero)
				return new Descriptor();
			else
			{
				Descriptor result;
				CudaUtil.Call(cuArrayGetDescriptor(out result, handle));
				return result;
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct Descriptor
		{
			public UInt32 Width;
			public UInt32 Height;
			public CudaArrayFormat Format;
			public UInt32 NumChannels;
		}

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuArrayCreate(out IntPtr array, ref Descriptor desc);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuArrayDestroy(IntPtr array);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuArrayGetDescriptor(out Descriptor arrayDesc, IntPtr array);
	}
}