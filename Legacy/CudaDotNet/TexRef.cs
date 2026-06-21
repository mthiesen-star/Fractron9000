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
	public enum TexAddressMode{
		Wrap = 0,
		Clamp = 1,
		Mirror = 2
	};

	public enum TexFilterMode{
		Point = 0,
		Linear = 1
	};

	[Flags]
	public enum TexFlags{
		ReadAsInteger = 0x01,
		NormalizedCoordinates = 0x02
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct TexRef
	{
		internal IntPtr handle;
		private const UInt32 CU_TRSA_OVERRIDE_FORMAT = 0x01;

		public static TexRef Create()
		{
			TexRef result = new TexRef();
			CudaUtil.Call(cuTexRefCreate(out result.handle));
			return result;
		}

		public void Destroy()
		{
			CudaUtil.Call(cuTexRefDestroy(handle));
			handle = IntPtr.Zero;
		}

		public CudaArray Array
		{
			get{
				CudaArray result = new CudaArray();
				CudaUtil.Call(cuTexRefGetArray(out result.handle, handle));
				return result;
			}
			set{
				CudaUtil.Call(cuTexRefSetArray(handle, value.handle, CU_TRSA_OVERRIDE_FORMAT));
			}
		}

		public void SetFormat(CudaArrayFormat componentFormat, int numPackedComponents)
		{
			CudaUtil.Call(cuTexRefSetFormat(handle, (UInt32)componentFormat, (Int32)numPackedComponents));
		}

		public CudaArrayFormat ComponentFormat{
			get{
				UInt32 fmt;
				Int32 numComponents;
				CudaUtil.Call(cuTexRefGetFormat(out fmt, out numComponents, handle));
				return (CudaArrayFormat)fmt;
			}
		}
		public int NumPackedComponents{
			get{
				UInt32 fmt;
				Int32 numComponents;
				CudaUtil.Call(cuTexRefGetFormat(out fmt, out numComponents, handle));
				return numComponents;
			}
		}

		public TexAddressMode AddressModeX
		{
			get{ return GetTexAddressMode(0); }
			set{ SetTexAddressMode(0, value); }
		}
		public TexAddressMode AddressModeY
		{
			get{ return GetTexAddressMode(1); }
			set{ SetTexAddressMode(1, value); }
		}
		public TexAddressMode AddressModeZ
		{
			get{ return GetTexAddressMode(2); }
			set{ SetTexAddressMode(2, value); }
		}

		private TexAddressMode GetTexAddressMode(int dim)
		{
			UInt32 result;
			CudaUtil.Call(cuTexRefGetAddressMode(out result, handle, dim));
			return (TexAddressMode)result;
		}
		private void SetTexAddressMode(int dim, TexAddressMode val)
		{
			CudaUtil.Call(cuTexRefSetAddressMode(handle, dim, (UInt32)val));
		}

		public TexFilterMode FilterMode{
			get{
				UInt32 result;
				CudaUtil.Call(cuTexRefGetFilterMode(out result, handle));
				return (TexFilterMode)result;
			}
			set{
				CudaUtil.Call(cuTexRefSetFilterMode(handle, (UInt32)value));
			}
		}

		public TexFlags Flags{
			get{
				UInt32 result;
				CudaUtil.Call(cuTexRefGetFlags(out result, handle));
				return (TexFlags)result;
			}
			set{
				CudaUtil.Call(cuTexRefSetFlags(handle, (UInt32)value));
			}
		}

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefCreate(out IntPtr pTexRef);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefDestroy(IntPtr hTexRef);

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefSetArray(IntPtr hTexRef, IntPtr hArray, UInt32 Flags);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefSetAddressMode(IntPtr hTexRef, int dim, UInt32 am);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefSetFilterMode(IntPtr hTexRef, UInt32 fm);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefSetFlags(IntPtr hTexRef, UInt32 Flags);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefSetFormat(IntPtr texRef, UInt32 format, int numPackedComponents);

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefGetArray(out IntPtr phArray, IntPtr hTexRef);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefGetAddressMode(out UInt32 pam, IntPtr hTexRef, int dim);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefGetFilterMode(out UInt32 pfm, IntPtr hTexRef);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefGetFlags(out UInt32 pFlags, IntPtr hTexRef);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuTexRefGetFormat(out UInt32 format, out Int32 numPackedComponents, IntPtr texRef); 
	}
}