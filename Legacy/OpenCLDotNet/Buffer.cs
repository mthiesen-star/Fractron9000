#region License
/*
    OpenCLDotNet - Compatability layer between OpenCL and the .NET framework
    Copyright (C) 2010 Michael J. Thiesen
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
using System.Diagnostics;

namespace OpenCL
{
	public class Buffer : MemObject, IDisposable, IHasInfo<MemInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new Buffer. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged buffer handle.</param>
		public Buffer(IntPtr handle) : base(handle) {}

		public static Buffer Create(Context context, MemFlags flags, IntPtr size)
		{
			IntPtr handle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;

			unsafe{
				handle = Native.CreateBuffer(context.Handle, flags, size, IntPtr.Zero, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			return new Buffer(handle);
		}

		public static Buffer Create(Context context, MemFlags flags, int size)
		{
			return Buffer.Create(context, flags, (IntPtr)size);
		}
		
		public static Buffer CreateFromGLBuffer(Context context, MemFlags flags, uint glBuffer)
		{
			IntPtr handle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;

			unsafe{
				handle = Native.GL.CreateFromGLBuffer(context.Handle, flags, glBuffer, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			return new Buffer(handle);
		}
		#endregion

		#region IHasInfo<MemInfo> Members
		unsafe ErrorCode IHasInfo<MemInfo>.GetInfo(MemInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetMemObjectInfo(handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		#region Mapping
		public unsafe IntPtr Map(CommandQueue queue, MapFlags flags, IntPtr offset, IntPtr size)
		{
			ErrorCode errorCode = ErrorCode.Success;
			IntPtr result = Native.EnqueueMapBuffer(queue.Handle, this.Handle, true, flags, offset, size, 0, null, null, &errorCode);
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
			return result;
		}

		public unsafe void Unmap(CommandQueue queue, ref IntPtr mappedPtr)
		{
			IntPtr evtHandle = IntPtr.Zero;
			Native.Call(Native.EnqueueUnmapMemObject(queue.Handle, this.Handle, mappedPtr, 0, null, &evtHandle));
			Event uEvt = new Event(evtHandle);
			uEvt.Wait();
			uEvt.Dispose();
			mappedPtr = IntPtr.Zero;
		}
		#endregion

		#region Reading
		public unsafe void ReadRaw(CommandQueue queue, IntPtr offset, IntPtr size, IntPtr destPtr)
		{
			Native.Call(Native.EnqueueReadBuffer(queue.Handle, this.handle, true, offset, size, destPtr, 0, null, null));
		}

		public unsafe void EnqueueReadRaw(CommandQueue queue, IntPtr offset, IntPtr size, IntPtr destPtr, out Event evt)
		{
			IntPtr evtHandle = IntPtr.Zero;
			Native.Call(Native.EnqueueReadBuffer(queue.Handle, this.handle, false, offset, size, destPtr, 0, null, &evtHandle));
			evt = new Event(evtHandle);
		}

		public void Read<T>(CommandQueue queue, out T value) where T : struct
		{
			IntPtr valSize = (IntPtr)Marshal.SizeOf(typeof(T));
			if((ulong)valSize > (ulong)this.Size)
				throw new OpenCLBufferSizeException("Buffer is too small for value.");
			IntPtr buf = this.Map(queue, MapFlags.Read, (IntPtr)0, valSize);
			try{
				value = (T)Marshal.PtrToStructure(buf, typeof(T));
			}
			finally{
				this.Unmap(queue, ref buf);
			}
		}

		public void Read<T>(CommandQueue queue, T[] values) where T : struct
		{
			if(values == null)
				throw new ArgumentException("values array is null", "values");
			int elemSize = Marshal.SizeOf(typeof(T));
			IntPtr arrSize = (IntPtr)(values.Length*elemSize);
			if((ulong)arrSize > (ulong)this.Size)
				throw new OpenCLBufferSizeException("Buffer is too small for array.");

			IntPtr buf = this.Map(queue, MapFlags.Read, (IntPtr)0, arrSize);
			try{
				copyPtrToArray(buf, values);
			}
			finally{
				this.Unmap(queue, ref buf);
			}
		}
		
		private static void copyPtrToArray<T>(IntPtr ptr, T[] arr) where T : struct
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint sizeInBytes = elemSize * (uint)arr.Length;

			unsafe{
				byte* elemPtr = (byte*)ptr;
				for(int i = 0; i < arr.Length; i++)
				{
					arr[i] = (T)Marshal.PtrToStructure((IntPtr)elemPtr, typeof(T));
					elemPtr += elemSize;
				}
			}
		}
		#endregion

		#region Writing
		public unsafe void WriteRaw(CommandQueue queue, IntPtr offset, IntPtr size, IntPtr srcPtr)
		{
			Native.Call(Native.EnqueueWriteBuffer(queue.Handle, this.handle, true, offset, size, srcPtr, 0, null, null));
		}
		public unsafe void EnqueueWriteRaw(CommandQueue queue, IntPtr offset, IntPtr size, IntPtr srcPtr, out Event evt)
		{
			IntPtr evtHandle = IntPtr.Zero;
			Native.Call(Native.EnqueueWriteBuffer(queue.Handle, this.handle, false, offset, size, srcPtr, 0, null, &evtHandle));
			evt = new Event(evtHandle);
		}

		public void Write<T>(CommandQueue queue, T value) where T : struct
		{
			IntPtr valSize = (IntPtr)Marshal.SizeOf(typeof(T));
			if((ulong)valSize > (ulong)this.Size)
				throw new OpenCLBufferSizeException("Buffer is too small for value.");
			IntPtr buf = this.Map(queue, MapFlags.Write, (IntPtr)0, valSize);
			try{
				Marshal.StructureToPtr(value, buf, false);
			}
			finally{
				this.Unmap(queue, ref buf);
			}
		}

		public void Write<T>(CommandQueue queue, T[] values) where T : struct
		{
			if(values == null)
				throw new ArgumentException("values array is null", "values");
			int elemSize = Marshal.SizeOf(typeof(T));
			IntPtr arrSize = (IntPtr)(values.Length*elemSize);
			if((ulong)arrSize > (ulong)this.Size)
				throw new OpenCLBufferSizeException("Buffer is too small for array.");

			IntPtr buf = this.Map(queue, MapFlags.Write, (IntPtr)0, arrSize);
			try{
				copyArrayToPtr(values, buf);
			}
			finally{
				this.Unmap(queue, ref buf);
			}
		}

		private static void copyArrayToPtr<T>(T[] arr, IntPtr ptr) where T : struct
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint sizeInBytes = elemSize * (uint)arr.Length;
			unsafe{
				byte* elemPtr = (byte*)ptr;
				for(int i = 0; i < arr.Length; i++)
				{
					Marshal.StructureToPtr(arr[i], (IntPtr)elemPtr, false);
					elemPtr += elemSize;
				}
			}
		}
		#endregion
	}
}