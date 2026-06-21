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

namespace OpenCL
{
	public class Platform : CLObject, IHasInfo<PlatformInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new Platform.
		/// </summary>
		/// <param name="handle">The unmanaged platform handle.</param>
		public Platform(IntPtr handle) : base(handle) {}
		
		public static Platform[] GetPlatforms()
		{
			Platform[] platforms = null;
			IntPtr[] handles = null;
			uint count = 0;
			unsafe
			{
				Native.Call(Native.GetPlatformIDs(0, null, &count));

				handles = new IntPtr[count];

				fixed(IntPtr* p_handles = handles)
				{
					Native.Call(Native.GetPlatformIDs(count, p_handles, null));
				}

				platforms = new Platform[count];
				for(int i = 0; i < count; i++)
					platforms[i] = new Platform(handles[i]);
			}
			return platforms;
		}
		#endregion

		#region Properties
		public string Profile{
			get{ return Info.Get_string(this, PlatformInfo.Profile); }
		}
		public string Version{
			get{ return Info.Get_string(this, PlatformInfo.Version); }
		}
		public string Name{
			get{ return Info.Get_string(this, PlatformInfo.Name); }
		}
		public string Vendor{
			get{ return Info.Get_string(this, PlatformInfo.Vendor); }
		}
		public string Extensions{
			get{ return Info.Get_string(this, PlatformInfo.Extensions); }
		}
		#endregion

		#region IHasInfo<PlatformInfo> Members
		unsafe ErrorCode IHasInfo<PlatformInfo>.GetInfo(PlatformInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetPlatformInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion


		/// <summary>
		/// Gets an array of all OpenCL Devices.
		/// </summary>
		/// <returns>An array of all OpenCL Devices.</returns>
		public Device[] GetDevices()
		{
			return GetDevices(DeviceTypeFlags.All);
		}

		/// <summary>
		/// Gets an array of all OpenCL Devices with the specified flags.
		/// </summary>
		/// <param name="deviceTypeFlags">A bitfield that identifies the type of OpenCL device.</param>
		/// <returns>An array of OpenCL devices with the specified flags.</returns>
		public Device[] GetDevices(DeviceTypeFlags deviceTypeFlags)
		{
			Device[] devices = null;
			uint count = 0;
			unsafe
			{
				ErrorCode errorCode = (ErrorCode)Native.GetDeviceIDs(this.Handle, deviceTypeFlags, 0, null, &count);
				
				if(errorCode == ErrorCode.DeviceNotFound)
					return new Device[0];
				else if(errorCode != ErrorCode.Success)
					throw new OpenCLCallFailedException(errorCode);

				IntPtr[] devHandles = new IntPtr[count];
				devices = new Device[count];

				fixed(IntPtr* p_devHandles = devHandles)
				{
					Native.Call(Native.GetDeviceIDs(this.Handle, deviceTypeFlags, count, p_devHandles, null));
				}

				devices = Device.CreateFromHandles(devHandles);
			}
			return devices;
		}

		public override string ToString()
		{
			if(handle == IntPtr.Zero)
				return "<NULL>";
			else
				return Name;
		}
	}
}
