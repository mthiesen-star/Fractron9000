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
using System.IO;
using System.Runtime.InteropServices;

namespace Cuda
{
	public enum ContextFlags {
		SchedAuto  = 0,
		SchedSpin  = 1,
		SchedYield = 2
	};
	
	public class Context : IDisposable
	{
		IntPtr handle = IntPtr.Zero;
		internal IntPtr Handle{
			get{ return handle; }
		}

		internal Context(Device device)
		{
			CudaUtil.Call(cuGLCtxCreate(out handle, (UInt32)ContextFlags.SchedAuto, device.Handle) );
			//CudaUtil.Call(cuGLInit());
		}
		internal Context(Device device, ContextFlags flags)
		{
			CudaUtil.Call(cuGLCtxCreate(out handle, (UInt32)flags, device.Handle) );
			//CudaUtil.Call(cuGLInit());
		}
		
		#region IDisposable Members
		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing){
				CudaUtil.Call(cuCtxDestroy(handle));
				handle = IntPtr.Zero;
			}
		}
		#endregion

		internal void Push()
		{
			CudaUtil.Call(cuCtxPushCurrent(handle));
		}
		
		internal void Pop()
		{
			IntPtr dummy;
			CudaUtil.Call(cuCtxPopCurrent(out dummy));
		}

		public void Synchronize()
		{
			CudaUtil.Call(cuCtxSynchronize());
		}

		public Module LoadModule(byte[] binaryImage)
		{
			return new Module(this, binaryImage);
		}
		
		public Module LoadModule(System.IO.Stream ioStream)
		{
			return new Module(this, ioStream);
		}
		
		public Module LoadModule(string filename)
		{
			return new Module(this, filename);
		}
		
		public void GLRegisterBufferObject(UInt32 bufferId)
		{
			CudaUtil.Call(cuGLRegisterBufferObject(bufferId));
		}
		
		public void GLUnregisterBufferObject(UInt32 bufferId)
		{
			CudaUtil.Call(cuGLUnregisterBufferObject(bufferId));
		}

		public static int GetDriverVersion()
		{
			int ver = 0;
			cuDriverGetVersion(out ver);
			return ver;
		}
		
		[DllImport("nvcuda.dll")]
		private static extern CudaResult cuCtxCreate(out IntPtr handle, UInt32 flags, UInt32 devHandle);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuGLCtxCreate(out IntPtr handle, UInt32 flags, UInt32 devHandle);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuGLInit();
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuCtxPushCurrent(IntPtr handle);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuCtxPopCurrent(out IntPtr newHandle);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuCtxSynchronize();
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuCtxDestroy(IntPtr handle);

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuGLRegisterBufferObject(UInt32 bufferId);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuGLUnregisterBufferObject(UInt32 bufferId);

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuDriverGetVersion(out Int32 version);
		
	}
}

