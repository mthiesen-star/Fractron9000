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
	public class Program : CLResource, IHasInfo<ProgramInfo>
	{
		#region Constructor / Destructor
		/// <summary>
		/// Creates a new Program. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged program handle.</param>
		internal Program(IntPtr handle) : base(handle) {}

		protected override void Release()
		{
			Native.Call(Native.ReleaseProgram(this.Handle));
		}

		public static Program CreateFromSource(Context context, string[] sources)
		{
			IntPtr progHandle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;
			IntPtr[] strSources = new IntPtr[sources.Length];
			try{
				for(int i = 0; i < strSources.Length; i++)
					strSources[i] = Marshal.StringToHGlobalAnsi(sources[i]);

				unsafe{
					fixed(IntPtr* p_strSources = strSources)
					{
						progHandle = Native.CreateProgramWithSource(context.Handle, (uint)strSources.Length, p_strSources, null, &errorCode);
					}
				}
			}
			finally{
				for(int i = 0; i < strSources.Length; i++)
					if(strSources[i] != IntPtr.Zero)
						Marshal.FreeHGlobal(strSources[i]);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
			return new Program(progHandle);
		}
		
		public void Retain()
		{
			Native.Call(Native.RetainProgram(this.Handle));
		}
		#endregion

		#region Properies
		public uint ReferenceCount{
			get{ return Info.Get_uint(this, ProgramInfo.ReferenceCount); }
		}

		/// <summary>
		/// Gets and retains the context object for this program.
		/// </summary>
		public Context Context{
			get{
				IntPtr ctxHandle = Info.Get_IntPtr(this, ProgramInfo.Context);
				Context result = new Context(ctxHandle);
				result.Retain();
				return result;
			}
		}
		public uint NumDevices{
			get{ return Info.Get_uint(this, ProgramInfo.NumDevices); }
		}
		public Device[] Devices{
			get{
				IntPtr[] deviceIds = Info.Get_IntPtrArray(this, ProgramInfo.Devices);
				Device[] result = new Device[deviceIds.Length];
				for(int i = 0; i < result.Length; i++)
					result[i] = new Device(deviceIds[i]);
				return result;
			}
		}
		public string Source{
			get{ return Info.Get_string(this, ProgramInfo.Source); }
		}
		#endregion

		#region IHasInfo<ProgramInfo> Members
		unsafe ErrorCode IHasInfo<ProgramInfo>.GetInfo(ProgramInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetProgramInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		public BuildStatus GetBuildStatus(Device device)
		{
			return (BuildStatus)Info.Get_int(new BuildResult(this,device), ProgramBuildInfo.Status);
		}
		public string GetBuildOptions(Device device)
		{
			return Info.Get_string(new BuildResult(this,device), ProgramBuildInfo.Options);
		}
		public string GetBuildLog(Device device)
		{
			return Info.Get_string(new BuildResult(this,device), ProgramBuildInfo.Log);
		}

		public static void UnloadCompiler()
		{
			Native.Call(Native.UnloadCompiler());
		}

		public void Build(Device[] devices, string options)
		{
			ErrorCode errorCode;
			unsafe{
				IntPtr[] devHandles = CLObject.GetHandles(devices);
				errorCode = Native.BuildProgram(this.Handle, (uint)devices.Length, devHandles, options, IntPtr.Zero, IntPtr.Zero);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
		}

		/// <summary>
		/// Helper struct used to query program build results
		/// </summary>
		private struct BuildResult : IHasInfo<ProgramBuildInfo>
		{
			public BuildResult(Program program, Device device)
			{
				this.progHandle = program.Handle;
				this.devHandle = device.Handle;
			}
			private IntPtr progHandle;
			private IntPtr devHandle;

			#region IHasInfo<ProgramBuildInfo> Members
			public unsafe ErrorCode GetInfo(ProgramBuildInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
			{
				return Native.GetProgramBuildInfo(progHandle, devHandle, param_name, param_value_size, param_value, param_value_size_ret);
			}
			#endregion
		}

	}
}