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
	public class Context : CLResource, IHasInfo<ContextInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new context. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged context handle.</param>
		public Context(IntPtr handle) : base(handle) {}

		public static Context Create(Device[] devices, params ContextParam[] ctxParams )
		{
			Context result = null;
			IntPtr ctxHandle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;

			IntPtr[] devHandles = CLObject.GetHandles(devices);

			IntPtr[] properties = new IntPtr[2*ctxParams.Length + 1];
			int i = 0;
			foreach(ContextParam cp in ctxParams)
			{
				properties[i] = (IntPtr)cp.Name;
				i++;
				properties[i] = cp.Value;
				i++;
			}
			properties[i] = (IntPtr)0;

			unsafe{
				ctxHandle = Native.CreateContext(properties, (uint)devices.Length, devHandles, IntPtr.Zero, IntPtr.Zero, &errorCode);
			}

			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			result = new Context(ctxHandle);

			return result;
		}

		public static Context CreateFromType(Platform platform, DeviceTypeFlags deviceTypeFlags)
		{
			ContextProperties[] properties = new ContextProperties[3];
			properties[0] = ContextProperties.Platform;
			properties[1] = (ContextProperties)(platform.Handle);
			properties[2] = (ContextProperties)0;

			return CreateFromType(properties, deviceTypeFlags);
		}

		public static Context CreateFromType(ContextProperties[] properties, DeviceTypeFlags deviceTypeFlags)
		{
			Context result = null;
			ErrorCode errorCode = ErrorCode.Success;
			IntPtr ctxHandle = IntPtr.Zero;

			unsafe{
				fixed(ContextProperties* p_properties = properties)
				{
					ctxHandle = Native.CreateContextFromType(p_properties, deviceTypeFlags, IntPtr.Zero, IntPtr.Zero, &errorCode);
				}
			}

			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			result = new Context(ctxHandle);

			return result;
		}

		protected override void Release()
		{
			Native.Call(Native.ReleaseContext(this.Handle));
		}

		public void Retain()
		{
			Native.Call(Native.RetainContext(this.Handle));
		}
		#endregion

		#region Properies
		public uint ReferenceCount{
			get{ return Info.Get_uint(this, ContextInfo.ReferenceCount); }
		}

		public IntPtr[] DeviceIds{
			get{ return Info.Get_IntPtrArray(this, ContextInfo.ContextDevices); }
		}
		
		public Device[] Devices{
			get{
				IntPtr[] deviceIds = DeviceIds;
				Device[] result = new Device[deviceIds.Length];
				for(int i = 0; i < result.Length; i++)
					result[i] = new Device(deviceIds[i]);
				return result;
			}
		}

		public ContextParam[] CreateParams{
			get{
				IntPtr[] vals = Info.Get_IntPtrArray(this, ContextInfo.ContextProperties);

				int paramCount = (vals.Length-1) / 2;
				if(paramCount <= 0)
					return new ContextParam[0];

				ContextParam[] result = new ContextParam[paramCount];

				for(int i = 0; i < paramCount; i++)
				{
					result[i] = new ContextParam(
						unchecked((ContextProperties)vals[2*i]),
						vals[2*i+1]
					);
				}
				return result;
			}
		}
		#endregion

		#region IHasInfo<ContextInfo> Members
		unsafe ErrorCode IHasInfo<ContextInfo>.GetInfo(ContextInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetContextInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		public Buffer CreateBuffer(MemFlags flags, IntPtr size)
		{
			return Buffer.Create(this, flags, size);
		}
		public Buffer CreateBuffer(MemFlags flags, int size)
		{
			return Buffer.Create(this, flags, size);
		}
		public Image2D CreateImage2D(MemFlags flags, ImageFormat format, int width, int height)
		{
			return Image2D.Create(this, flags, format, width, height);
		}
	}

	public struct ContextParam{
		private ContextProperties name;
		private IntPtr value;

		public ContextParam(ContextProperties name, int value){
			this.name = name;
			this.value = (IntPtr)value;
		}

		public ContextParam(ContextProperties name, IntPtr value){
			this.name = name;
			this.value = value;
		}

		public ContextParam(ContextProperties name, CLObject value){
			this.name = name;
			this.value = value.Handle;
		}

		public ContextProperties Name{
			get{ return name; }
		}
		public IntPtr Value{
			get{ return value; }
		}
	}


}