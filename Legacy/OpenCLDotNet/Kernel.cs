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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenCL
{
	public class Kernel : CLResource, IHasInfo<KernelInfo>
	{
		#region Creation / Destruction
		/// <summary>
		/// Creates a new Kernel. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged kernel handle.</param>
		public Kernel(IntPtr handle) : base(handle){}

		public static Kernel Create(Program program, string kernelName)
		{
			IntPtr kHandle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;
			
			unsafe{
				kHandle = Native.CreateKernel(program.Handle, kernelName, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
			return new Kernel(kHandle);
		}

		protected override void Release()
		{
			Native.Call(Native.ReleaseKernel(this.Handle));
		}

		public void Retain()
		{
			Native.Call(Native.RetainKernel(this.Handle));
		}
		#endregion

		#region Properies
		public string FunctionName{
			get{ return Info.Get_string(this, KernelInfo.FunctionName); }
		}
		public uint NumArgs{
			get{ return Info.Get_uint(this, KernelInfo.NumArgs); }
		}
		public uint ReferenceCount{
			get{ return Info.Get_uint(this, KernelInfo.ReferenceCount); }
		}
		/// <summary>
		/// Gets and retains the context object for this kernel.
		/// </summary>
		public Context Context{
			get{
				IntPtr ctxHandle = Info.Get_IntPtr(this, KernelInfo.Context);
				Context result = new Context(ctxHandle);
				result.Retain();
				return result;
			}
		}
		/// <summary>
		/// Gets and retains the program object for this kernel
		/// </summary>
		public Program Program{
			get{
				IntPtr progHandle = Info.Get_IntPtr(this, KernelInfo.Program);
				Program result = new Program(progHandle);
				result.Retain();
				return result;
			}
		}
		#endregion

		#region IHasInfo<KernelInfo> Members
		unsafe ErrorCode IHasInfo<KernelInfo>.GetInfo(KernelInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetKernelInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		public uint WorkGroupSize(Device device)
		{
			return (uint)Info.Get_IntPtr(new KernelDevice(this,device), KernelWorkGroupInfo.WorkGroupSize);
		}

		public IntPtr[] CompileWorkGroupSize(Device device)
		{
			return Info.Get_IntPtrArray(new KernelDevice(this,device), KernelWorkGroupInfo.CompileWorkGroupSize);
		}

		public ulong LocalMemSize(Device device)
		{
			return Info.Get_ulong(new KernelDevice(this,device), KernelWorkGroupInfo.LocalMemSize);
		}

		public void SetArg(uint index, CLObject clObj)
		{
			unsafe{
				IntPtr handle = clObj.Handle;
				Native.Call(Native.SetKernelArg(this.Handle, index, (IntPtr)sizeof(IntPtr), (IntPtr)(&handle)));
			}
		}
		public void SetArg(uint index, Sampler sampler)
		{
			unsafe{
				IntPtr handle = sampler.Handle;
				Native.Call(Native.SetKernelArg(this.Handle, index, (IntPtr)sizeof(IntPtr), (IntPtr)(&handle)));
			}
		}
		public void SetArg(uint index, ValueType value)
		{
			int size = Marshal.SizeOf(value.GetType());
			IntPtr buf = Marshal.AllocHGlobal(size);
			try{
				Marshal.StructureToPtr(value, buf, false);
				unsafe{
					Native.Call(Native.SetKernelArg(this.Handle, index, (IntPtr)size, buf));
				}
			}
			finally{
				Marshal.FreeHGlobal(buf);
			}
		}

		/// <summary>
		/// Sets all arguments for this kernel.
		/// </summary>
		/// <param name="args">The arguments to pass to the kernel. These must be either ValueTypes, MemObjects, or Samplers.</param>
		public void SetArgs(params object[] args)
		{
			if((uint)args.Length != this.NumArgs)
				throw new ArgumentException(String.Format("Kernel expected {0} arguments, but was given {1}", this.NumArgs, args.Length));

			for(int i = 0; i < args.Length; i++)
			{
				if(args[i] is CLObject)
					SetArg((uint)i, (args[i] as CLObject));
				else if(args[i] is ValueType)
					SetArg((uint)i, (args[i] as ValueType));
				else
					throw new ArgumentException(String.Format("Kernel argument {0} is not a supported type.", i));
			}
		}

		/// <summary>
		/// Launch a one dimensional kernel and block until completed.
		/// </summary>
		public void Launch(CommandQueue queue, int globalWorkSize, int localWorkSize)
		{
			Event evt;
			this.EnqueueLaunch(queue, globalWorkSize, localWorkSize, null, out evt);
			evt.Wait();
			evt.Dispose();
		}
		/// <summary>
		/// Launch a two dimensional kernel and block until completed.
		/// </summary>
		public void Launch(CommandQueue queue, Dim2D globalWorkSize, Dim2D localWorkSize)
		{
			Event evt;
			this.EnqueueLaunch(queue, globalWorkSize, localWorkSize, out evt);
			evt.Wait();
			evt.Dispose();
		}
		/// <summary>
		/// Launch a three dimensional kernel and block until completed.
		/// </summary>
		public void Launch(CommandQueue queue, Dim3D globalWorkSize, Dim3D localWorkSize)
		{
			Event evt;
			this.EnqueueLaunch(queue, globalWorkSize, localWorkSize, out evt);
			evt.Wait();
			evt.Dispose();
		}

		/// <summary>
		/// Enqueue a one dimensional kernel for launch.
		/// </summary>
		public void EnqueueLaunch(CommandQueue queue, int globalWorkSize, int localWorkSize, Event[] waitList, out Event evt)
		{
			this.EnqueueLaunch(queue, (IntPtr)globalWorkSize, (IntPtr)localWorkSize, waitList, out evt);
		}

		/// <summary>
		/// Enqueue a one dimensional kernel for launch.
		/// </summary>
		public void EnqueueLaunch(CommandQueue queue, IntPtr globalWorkSize, IntPtr localWorkSize, Event[] waitList, out Event evt)
		{
			unsafe{
				uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
				IntPtr[] wlh = CLObject.GetHandles(waitList);
				IntPtr evtHandle = IntPtr.Zero;
				Native.Call(Native.EnqueueNDRangeKernel(
					queue.Handle, this.Handle, 1, null,
					(IntPtr*)(&globalWorkSize), (IntPtr*)(&localWorkSize),
					waitCount, wlh, &evtHandle)
				);
				evt = new Event(evtHandle);
			}
		}


		/// <summary>
		/// Enqueue a two dimensional kernel for launch.
		/// </summary>
		public void EnqueueLaunch(CommandQueue queue, int globalWidth, int globalHeight, int localWidth, int localHeight, out Event evt)
		{
			this.EnqueueLaunch(queue, new Dim2D(globalWidth, globalHeight), new Dim2D(localWidth,  localHeight), out evt);
		}

		/// <summary>
		/// Enqueue a two dimensional kernel for launch.
		/// </summary>
		public void EnqueueLaunch(CommandQueue queue, Dim2D globalWorkSize, Dim2D localWorkSize, out Event evt)
		{
			unsafe{
				IntPtr evtHandle = IntPtr.Zero;
				Native.Call(Native.EnqueueNDRangeKernel(queue.Handle, this.Handle, 2, null, (IntPtr*)(&globalWorkSize), (IntPtr*)(&localWorkSize), 0, null, &evtHandle));
				evt = new Event(evtHandle);
			}
		}

		/// <summary>
		/// Enqueue a three dimensional kernel for launch.
		/// </summary>
		public void EnqueueLaunch(CommandQueue queue, int globalWidth, int globalHeight, int globalDepth, int localWidth, int localHeight, int localDepth, out Event evt)
		{
			this.EnqueueLaunch(queue, new Dim3D(globalWidth, globalHeight, globalDepth), new Dim3D(localWidth,  localHeight,  localDepth), out evt); 
		}

		/// <summary>
		/// Enqueue a three dimensional kernel for launch.
		/// </summary>
		public void EnqueueLaunch(CommandQueue queue, Dim3D globalWorkSize, Dim3D localWorkSize, out Event evt)
		{
			unsafe{
				IntPtr evtHandle = IntPtr.Zero;
				Native.Call(Native.EnqueueNDRangeKernel(queue.Handle, this.Handle, 3, null, (IntPtr*)(&globalWorkSize), (IntPtr*)(&localWorkSize), 0, null, &evtHandle));
				evt = new Event(evtHandle);
			}
		}

		/// <summary>
		/// Helper struct used to query device specific info
		/// </summary>
		private struct KernelDevice : IHasInfo<KernelWorkGroupInfo>
		{
			public KernelDevice(Kernel kernel, Device device)
			{
				this.kernelHandle = kernel.Handle;
				this.devHandle = device.Handle;
			}
			private IntPtr kernelHandle;
			private IntPtr devHandle;

			#region IHasInfo<KernelWorkGroupInfo> Members
			public unsafe ErrorCode GetInfo(KernelWorkGroupInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
			{
				return Native.GetKernelWorkGroupInfo(kernelHandle, devHandle, param_name, param_value_size, param_value, param_value_size_ret);
			}
			#endregion
		}
	}
}