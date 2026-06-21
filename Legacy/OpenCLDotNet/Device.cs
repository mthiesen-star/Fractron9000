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
	public class Device : CLObject, IHasInfo<DeviceInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new Device.
		/// </summary>
		/// <param name="handle">The unmanaged device handle.</param>
		public Device(IntPtr handle) : base(handle) {}

		public static Device[] GetDevices(Platform platform)
		{
			return platform.GetDevices();
		}

		public static Device[] GetDevices(Platform platform, DeviceTypeFlags deviceTypeFlags)
		{
			return platform.GetDevices(deviceTypeFlags);
		}

		public static Device[] CreateFromHandles(IntPtr[] handles)
		{
			if(handles == null) return null;
			Device[] result = new Device[handles.Length];
			for(int i = 0; i < result.Length; i++)
				result[i] = new Device(handles[i]);
			return result;
		}

		#endregion

		#region Properies		
		public DeviceTypeFlags Type{
			get{ return (DeviceTypeFlags)Info.Get_long(this, DeviceInfo.Type); }
		}
		public uint VendorId{
			get{ return Info.Get_uint(this, DeviceInfo.VendorId); }
		}
		public uint MaxComputeUnits{
			get{ return Info.Get_uint(this, DeviceInfo.MaxComputeUnits); }
		}
		public uint MaxWorkItemDimensions{
			get{ return Info.Get_uint(this, DeviceInfo.MaxWorkItemDimensions); }
		}
		public IntPtr MaxWorkGroupSize{
			get{ return Info.Get_IntPtr(this, DeviceInfo.MaxWorkGroupSize); }
		}
		public IntPtr[] MaxWorkItemSizes{
			get{ return Info.Get_IntPtrArray(this, DeviceInfo.MaxWorkItemSizes); }
		}
		public uint PreferredVectorWidthChar{
			get{ return Info.Get_uint(this, DeviceInfo.PreferredVectorWidthChar); }
		}
		public uint PreferredVectorWidthShort{
			get{ return Info.Get_uint(this, DeviceInfo.PreferredVectorWidthShort); }
		}
		public uint PreferredVectorWidthInt{
			get{ return Info.Get_uint(this, DeviceInfo.PreferredVectorWidthInt); }
		}
		public uint PreferredVectorWidthLong{
			get{ return Info.Get_uint(this, DeviceInfo.PreferredVectorWidthLong); }
		}
		public uint PreferredVectorWidthFloat{
			get{ return Info.Get_uint(this, DeviceInfo.PreferredVectorWidthFloat); }
		}
		public uint PreferredVectorWidthDouble{
			get{ return Info.Get_uint(this, DeviceInfo.PreferredVectorWidthDouble); }
		}
		public uint MaxClockFrequency{
			get{ return Info.Get_uint(this, DeviceInfo.MaxClockFrequency); }
		}
		public uint AddressBits{
			get{ return Info.Get_uint(this, DeviceInfo.AddressBits); }
		}
		public uint MaxReadImageArgs{
			get{ return Info.Get_uint(this, DeviceInfo.MaxReadImageArgs); }
		}
		public uint MaxWriteImageArgs{
			get{ return Info.Get_uint(this, DeviceInfo.MaxWriteImageArgs); }
		}
		public ulong MaxMemAllocSize{
			get{ return Info.Get_ulong(this, DeviceInfo.MaxMemAllocSize); }
		}
		public IntPtr Image2dMaxWidth{
			get{ return Info.Get_IntPtr(this, DeviceInfo.Image2dMaxWidth); }
		}
		public IntPtr Image2dMaxHeight{
			get{ return Info.Get_IntPtr(this, DeviceInfo.Image2dMaxHeight); }
		}
		public IntPtr Image3dMaxWidth{
			get{ return Info.Get_IntPtr(this, DeviceInfo.Image3dMaxWidth); }
		}
		public IntPtr Image3dMaxHeight{
			get{ return Info.Get_IntPtr(this, DeviceInfo.Image3dMaxHeight); }
		}
		public IntPtr Image3dMaxDepth{
			get{ return Info.Get_IntPtr(this, DeviceInfo.Image3dMaxDepth); }
		}
		public bool ImageSupport{
			get{ return Info.Get_bool(this, DeviceInfo.ImageSupport); }
		}
		public IntPtr MaxParameterSize{
			get{ return Info.Get_IntPtr(this, DeviceInfo.MaxParameterSize); }
		}
		public uint MaxSamplers{
			get{ return Info.Get_uint(this, DeviceInfo.MaxSamplers); }
		}
		public uint MemBaseAddrAlign{
			get{ return Info.Get_uint(this, DeviceInfo.MemBaseAddrAlign); }
		}
		public uint MinDataTypeAlignSize{
			get{ return Info.Get_uint(this, DeviceInfo.MinDataTypeAlignSize); }
		}
		public DeviceFpConfigFlags SingleFpConfig{
			get{ return (DeviceFpConfigFlags)Info.Get_long(this, DeviceInfo.SingleFpConfig); }
		}
		public DeviceMemCacheType GlobalMemCacheType{
			get{ return (DeviceMemCacheType)Info.Get_int(this, DeviceInfo.GlobalMemCacheType); }
		}
		public uint GlobalMemCachelineSize{
			get{ return Info.Get_uint(this, DeviceInfo.GlobalMemCachelineSize); }
		}
		public ulong GlobalMemCacheSize{
			get{ return Info.Get_ulong(this, DeviceInfo.GlobalMemCacheSize); }
		}
		public ulong GlobalMemSize{
			get{ return Info.Get_ulong(this, DeviceInfo.GlobalMemSize); }
		}
		public ulong MaxConstantBufferSize{
			get{ return Info.Get_ulong(this, DeviceInfo.MaxConstantBufferSize); }
		}
		public uint MaxConstantArgs{
			get{ return Info.Get_uint(this, DeviceInfo.MaxConstantArgs); }
		}
		public DeviceLocalMemType LocalMemType{
			get{ return (DeviceLocalMemType)Info.Get_int(this, DeviceInfo.LocalMemType); }
		}
		public ulong LocalMemSize{
			get{ return Info.Get_ulong(this, DeviceInfo.LocalMemSize); }
		}
		public bool ErrorCorrectionSupport{
			get{ return Info.Get_bool(this, DeviceInfo.ErrorCorrectionSupport); }
		}
		public IntPtr ProfilingTimerResolution{
			get{ return Info.Get_IntPtr(this, DeviceInfo.ProfilingTimerResolution); }
		}
		public bool EndianLittle{
			get{ return Info.Get_bool(this, DeviceInfo.EndianLittle); }
		}
		public bool Available{
			get{ return Info.Get_bool(this, DeviceInfo.Available); }
		}
		public bool CompilerAvailable{
			get{ return Info.Get_bool(this, DeviceInfo.CompilerAvailable); }
		}
		public DeviceExecCapabilitiesFlags ExecutionCapabilities{
			get{ return (DeviceExecCapabilitiesFlags)Info.Get_long(this, DeviceInfo.ExecutionCapabilities); }
		}
		public CommandQueueFlags QueueProperties{
			get{ return (CommandQueueFlags)Info.Get_long(this, DeviceInfo.QueueProperties); }
		}
		public string Name{
			get{ return Info.Get_string(this, DeviceInfo.Name); }
		}
		public string Vendor{
			get{ return Info.Get_string(this, DeviceInfo.Vendor); }
		}
		public string DriverVersion{
			get{ return Info.Get_string(this, DeviceInfo.DriverVersion); }
		}
		public string Profile{
			get{ return Info.Get_string(this, DeviceInfo.Profile); }
		}
		public string Version{
			get{ return Info.Get_string(this, DeviceInfo.Version); }
		}
		public string Extensions{
			get{ return Info.Get_string(this, DeviceInfo.Extensions); }
		}
		public Platform Platform{
			get{
				IntPtr id = Info.Get_IntPtr(this, DeviceInfo.Platform);
				return new Platform(id);
			}
		}
		#endregion

		#region IHasInfo<DeviceInfo> Members
		unsafe ErrorCode IHasInfo<DeviceInfo>.GetInfo(DeviceInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetDeviceInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		public override string ToString()
		{
			return Name;
		}

	}
}

