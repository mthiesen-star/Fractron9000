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

namespace Cuda
{
	public class Device
	{
		private static Device[] devices = null;
		
		private UInt32 handle;
		private int index;
		
		private static void Init()
		{
			if(devices == null){
				Int32 count;
				CudaUtil.Call(cuInit(0));
				CudaUtil.Call(cuDeviceGetCount(out count));
				devices = new Device[count];
				for(int i = 0; i < devices.Length; i++)
					devices[i] = new Device(i);
			}
		}
		
		public static Device[] Devices{
			get{
				if(devices == null) Init();
				return devices;
			}
		}

		private Device(int index)
		{
			this.index = index;
			CudaUtil.Call(cuDeviceGet(out handle, index));
		}
				
		internal UInt32 Handle{
			get{ return handle; }
		}

		public int Index{
			get{ return index; }
		}

		public Context CreateContext()
		{
			return new Context(this);
		}

		public Context CreateContext(int minVersion)
		{
			int driverVersion = Context.GetDriverVersion();
			if(driverVersion < minVersion)
				throw new CudaVersionException(minVersion, driverVersion);
			return new Context(this);
		}

		public Context CreateContext(ContextFlags flags)
		{
			return new Context(this, flags);
		}
		
		public string Name{
			get{
				IntPtr strBuf = Marshal.AllocHGlobal((IntPtr)255);
				CudaUtil.Call(cuDeviceGetName(strBuf, 255, handle));
				string result = Marshal.PtrToStringAnsi(strBuf);
				Marshal.FreeHGlobal(strBuf);
				return result;
			}
		}
		
		public int MultiprocessorCount{get{ return GetAttr(DeviceAttribute.MULTIPROCESSOR_COUNT); }}
		public double ClockRate_mhz{get{
				return (double)GetAttr(DeviceAttribute.CLOCK_RATE) / 1000.0;
			}
		}
		public UInt32 TotalMemory{
			get{
				UInt32 size;
				CudaUtil.Call(cuDeviceTotalMem(out size, handle));
				return size;
			}
		}
		
		public int WarpSize{get{ return GetAttr(DeviceAttribute.WARP_SIZE); }}
		public int Registers{get{ return GetAttr(DeviceAttribute.MAX_REGISTERS_PER_BLOCK); }}
		public int SharedMemory{get{ return GetAttr(DeviceAttribute.MAX_SHARED_MEMORY_PER_BLOCK); }}
		public int ConstantMemory{get{ return GetAttr(DeviceAttribute.TOTAL_CONSTANT_MEMORY); }}

		public int MaxBlockDimX{get{ return GetAttr(DeviceAttribute.MAX_BLOCK_DIM_X); }}
		public int MaxBlockDimY{get{ return GetAttr(DeviceAttribute.MAX_BLOCK_DIM_Y); }}
		public int MaxBlockDimZ{get{ return GetAttr(DeviceAttribute.MAX_BLOCK_DIM_Z); }}
		public int MaxGridDimX{get{ return GetAttr(DeviceAttribute.MAX_GRID_DIM_X); }}
		public int MaxGridDimY{get{ return GetAttr(DeviceAttribute.MAX_GRID_DIM_Y); }}
		public int MaxGridDimZ{get{ return GetAttr(DeviceAttribute.MAX_GRID_DIM_Z); }}
		

		
		private int GetAttr(DeviceAttribute attrib){
			Int32 result;
			CudaUtil.Call(cuDeviceGetAttribute(out result, (Int32)attrib, handle));
			return result;
		}

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuInit(UInt32 flags);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuDeviceGetCount(out Int32 count);

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuDeviceGet(out UInt32 device, Int32 ordinal);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuDeviceGetName(IntPtr cstr, Int32 len, UInt32 device);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuDeviceGetAttribute(out Int32 value, Int32 attrib, UInt32 device);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuDeviceTotalMem(out UInt32 size, UInt32 device);
	}
	
	public class CudaVersionException : CudaException
	{

		public CudaVersionException(int minVersion, int driverVersion)
			: base( String.Format("The CUDA driver is out of date. Version {0} is required, but version {1} was found.", minVersion, driverVersion) )
		{
			
		}

	}
}