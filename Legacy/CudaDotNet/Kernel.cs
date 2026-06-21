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
using System.IO;

namespace Cuda
{
	public class Kernel
	{
		private IntPtr handle = IntPtr.Zero;
		private Module module;
		private string name;
		private int xGridDim = 1;
		private int yGridDim = 1;
		
		internal Kernel(Module module, string name)
		{
			if(module == null) throw new ArgumentException("module parameter may not be null", "module");
			if(name == null) throw new ArgumentException("name parameter may not be null", "name");
			
			this.module = module;
			this.name = name;
			
			CudaUtil.Call(cuModuleGetFunction(out handle, module.Handle, name));
		}
		
		internal IntPtr Handle{
			get{ return handle; }
		}
		public Module Module{
			get{ return module; }
		}
		public Context Context{
			get{ return module.Context; }
		}
		public string Name{
			get{ return name; }
		}
		
		public void SetBlockShape(int xDim, int yDim, int zDim)
		{
			CudaUtil.Call(cuFuncSetBlockShape(handle, xDim, yDim, zDim));
		}
		
		public void SetGridDim(int xDim, int yDim)
		{
			xGridDim = xDim;
			yGridDim = yDim;
		}
		
		public void SetSharedSize(UInt32 size)
		{
			CudaUtil.Call(cuFuncSetSharedSize(handle, size));
		}

		public void SetTexRef(TexRef texRef)
		{
			CudaUtil.Call(cuParamSetTexRef(handle, CU_PARAM_TR_DEFAULT, texRef.handle));
		}
		
		public void Launch(params ValueType[] args)
		{
			setParams(args);
			CudaUtil.Call(cuLaunchGrid(handle, xGridDim, yGridDim));
		}

		public void LaunchAsync(Stream stream, params ValueType[] args)
		{
			setParams(args);
			CudaUtil.Call(cuLaunchGridAsync(handle, xGridDim, yGridDim, stream.Handle));
		}
		
		private void setParams(ValueType[] args)
		{
			int paramSize = 0;
			byte[] buffer = new byte[32];
			unsafe
			{
				int argSize = 0;
				foreach(ValueType arg in args)
				{
					argSize = Marshal.SizeOf(arg);
					if(buffer.Length < argSize)
						buffer = new byte[argSize];
					fixed(byte* bufferPtr = buffer)
					{
						Marshal.StructureToPtr(arg, (IntPtr)bufferPtr, false);
						CudaUtil.Call(cuParamSetv(handle, (Int32)paramSize, (IntPtr)bufferPtr, (UInt32)argSize));
						paramSize += argSize;
					}
				}
			}
			CudaUtil.Call(cuParamSetSize(handle, (UInt32)paramSize));
		}
		
		private const int CU_PARAM_TR_DEFAULT = -1;

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuModuleGetFunction(out IntPtr handle, IntPtr modHandle, [MarshalAs(UnmanagedType.LPStr)] string funcname);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuFuncSetBlockShape(IntPtr func, Int32 x, Int32 y, Int32 z);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuFuncSetSharedSize(IntPtr func, UInt32 bytes);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuParamSetSize     (IntPtr hfunc, UInt32 numbytes);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuParamSeti        (IntPtr hfunc, Int32 offset, UInt32 value);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuParamSetf        (IntPtr hfunc, Int32 offset, float value);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuParamSetv        (IntPtr hfunc, Int32 offset, IntPtr ptr, UInt32 numbytes);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuParamSetTexRef   (IntPtr hfunc, Int32 texunit, IntPtr hTexRef);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuLaunchGrid(IntPtr func, Int32 grid_width, Int32 grid_height);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuLaunchGridAsync(IntPtr func, Int32 grid_width, Int32 grid_height, IntPtr hStream);

	}
}