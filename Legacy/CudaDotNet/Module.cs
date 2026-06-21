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
	public enum JitOptions
	{
		/*
		 * Max number of registers that a thread may use.
		 */
		MaxRegisters            = 0,

		/**
		 * IN: Specifies minimum number of threads per block to target compilation
		 * for\n
		 * OUT: Returns the number of threads the compiler actually targeted.
		 * This restricts the resource utilization fo the compiler (e.g. max
		 * registers) such that a block with the given number of threads should be
		 * able to launch based on register limitations. Note, this option does not
		 * currently take into account any other resource limitations, such as
		 * shared memory utilization.
		 */
		ThreadsPerBlock,

		/**
		 * Returns a float value in the option of the wall clock time, in
		 * milliseconds, spent creating the cubin
		 */
		WallTime,

		/**
		 * Pointer to a buffer in which to print any log messsages from PTXAS
		 * that are informational in nature
		 */
		InfoLogBuffer,

		/**
		 * IN: Log buffer size in bytes.  Log messages will be capped at this size
		 * (including null terminator)\n
		 * OUT: Amount of log buffer filled with messages
		 */
		InfoLogBufferSizeBytes,

		/**
		 * Pointer to a buffer in which to print any log messages from PTXAS that
		 * reflect errors
		 */
		ErrorLogBuffer,

		/**
		 * IN: Log buffer size in bytes.  Log messages will be capped at this size
		 * (including null terminator)\n
		 * OUT: Amount of log buffer filled with messages
		 */
		ErrorLogBufferSizeBytes,

		/**
		 * Level of optimizations to apply to generated code (0 - 4), with 4
		 * being the default and highest level of optimizations.
		 */
		OptimizationLevel,

		/**
		 * No option value required. Determines the target based on the current
		 * attached context (default)
		 */
		TargetFromContext,

		/**
		 * Target is chosen based on supplied CUjit_target_enum.
		 */
		Target,

		/**
		 * Specifies choice of fallback strategy if matching cubin is not found.
		 * Choice is based on supplied CUjit_fallback_enum.
		 */
		FallbackStrategy
	};

	public class Module : IDisposable
	{
		const int LogBufferSize = 2048;
		const int ErrorBufferSize = 2048;
		
		IntPtr handle = IntPtr.Zero;
		Context context;
		string log;
		string errors;
		
		#region Properties
		internal IntPtr Handle{
			get{ return handle; }
		}
		
		public Context Context{
			get{ return context; }
		}
		
		public string InfoLog{
			get{ return log; }
		}
		public string ErrorLog{
			get{ return errors; }
		}
		#endregion
		
		internal Module(Context context, byte[] binaryImage)
		{
			this.context = context;
			Load(binaryImage);
		}
		internal Module(Context context, System.IO.Stream ioStream)
		{
			this.context = context;
			
			byte[] fileData = new byte[ioStream.Length];
			ioStream.Read(fileData, 0, (int)ioStream.Length);
			Load(fileData);
		}

		internal Module(Context context, string filename)
		{
			this.context = context;
			FileStream fs = File.Open(filename, FileMode.Open, FileAccess.Read);
			byte[] fileData = new byte[fs.Length];
			fs.Read(fileData, 0, (int)fs.Length);
			fs.Close();
			Load(fileData);
		}
		
		private void Load(byte[] image)
		{
			CudaResult result = CudaResult.Unknown;
			IntPtr logBuffer = Marshal.AllocHGlobal(LogBufferSize+1);     //leave room for a null terminator
			IntPtr errorBuffer = Marshal.AllocHGlobal(ErrorBufferSize+1); //leave room for a null terminator
			try{
				int outLogLen = 0;
				int outErrorLen = 0;

				unsafe
				{
					uint* options = stackalloc uint[4];
					void** values = stackalloc void*[4];

					options[0] = (uint)JitOptions.InfoLogBuffer;
					values[0] = (void*)logBuffer;
					
					options[1] = (uint)JitOptions.InfoLogBufferSizeBytes;
					values[1] = (void*)LogBufferSize;				

					options[2] = (uint)JitOptions.ErrorLogBuffer;
					values[2] = (void*)errorBuffer;
					
					options[3] = (uint)JitOptions.ErrorLogBufferSizeBytes;
					values[3] = (void*)ErrorBufferSize;

					fixed(byte* pImage = image)
					{
						result = cuModuleLoadDataEx(out handle, (void*)pImage, 4, options, values);
					}

					outLogLen =   (int)values[1];
					outErrorLen = (int)values[3];

					((byte*)logBuffer)[LogBufferSize] = 0;   //make certain the ANSI strings are properly null terminated
					((byte*)errorBuffer)[ErrorBufferSize] = 0;
				}

				log = Marshal.PtrToStringAnsi(logBuffer);
				errors = Marshal.PtrToStringAnsi(errorBuffer);
			}
			finally
			{
				Marshal.FreeHGlobal(logBuffer);
				Marshal.FreeHGlobal(errorBuffer);
			}
			if(result != CudaResult.Success)
			{
				throw new CudaModuleLoadException(result, log, errors);
			}
		}

		#region IDisposable Members
		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing){
				CudaUtil.Call(cuModuleUnload(handle));
				handle = IntPtr.Zero;
			}
		}
		#endregion
		
		public Kernel GetKernel(string name)
		{
			return new Kernel(this, name);
		}
		
		public DeviceBuffer GetConstantBuffer(string name)
		{
			DeviceBuffer result = new DeviceBuffer();
			CudaUtil.Call(cuModuleGetGlobal(out result.ptr.ptr, out result.sizeInBytes, handle, name));
			return result;
		}

		public TexRef GetTexRef(string name)
		{
			TexRef result = new TexRef();
			CudaUtil.Call(cuModuleGetTexRef(out result.handle, handle, name));
			return result;
		}
		
		public void ReadConstant<T>(out T value, string name) where T : struct
		{	
			DeviceBuffer buffer = GetConstantBuffer(name);
			CudaMem.CopyStruct(buffer, out value);
		}
		
		public void WriteConstant<T>(string name, T value) where T : struct
		{
			DeviceBuffer buffer = GetConstantBuffer(name);
			CudaMem.CopyStruct(value, buffer);
		}
		
		public void WriteConstant<T>(string name, T[] value) where T : struct
		{
			DeviceBuffer buffer = GetConstantBuffer(name);
			CudaMem.Copy(value, buffer);
		}

		[DllImport("nvcuda.dll")]
		unsafe private static extern CudaResult cuModuleLoadDataEx(out IntPtr module, void* image, uint numOptions, uint* options, void** optionValues);
		
		[DllImport("nvcuda.dll")]
		private static extern CudaResult cuModuleGetGlobal(out UInt32 devPtr, out UInt32 size, IntPtr mod, [MarshalAs(UnmanagedType.LPStr)]string globalname);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuModuleGetTexRef(out IntPtr texRef, IntPtr hmod, [MarshalAs(UnmanagedType.LPStr)]string texrefname);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuModuleUnload(IntPtr mod);
	}

	public class CudaModuleLoadException : CudaException
	{
		private string logText;
		public string LogText{
			get{ return logText; }
		}
		private string errorText;
		public string ErrorText{
			get{ return errorText; }
		}

		public CudaModuleLoadException(CudaResult result, string logText, string errorText)
			: base(result)
		{
			this.logText = logText;
			this.Data.Add("Message Log", logText);
			this.errorText = errorText;
			this.Data.Add("Error Log", errorText);
		}

	}

}
