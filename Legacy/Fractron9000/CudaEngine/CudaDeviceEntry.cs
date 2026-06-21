#region License
/*
    Fractron 9000
    Copyright (C) 2009 Michael J. Thiesen
	http://fractron9000.sourceforge.net
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

//#define DISABLE_CUDA //this makes CUDA appear unsupported

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Cuda;
using MTUtil;

namespace Fractron9000.CudaEngine
{
	public class CudaDeviceEntry : DeviceEntry
	{
		private const float baseRank = 14.0f;

		private int idx;
		private Device device;

		public CudaDeviceEntry(int idx, Device device)
		{
			this.idx = idx;
			this.device = device;
		}

		public override string Name{
			get{ return device.Name; }
		}
		public override string Api{
			get{ return "CUDA"; }
		}
		public override EngineType EngineType{
			get { return EngineType.Cuda; }
		}
    public override string Identifier{
      get { return device.Name; }
    }
		public override int Index{
			get { return (int)idx; }
		}
		public override float PerformanceRating{
			get{
				return baseRank * (float)device.MultiprocessorCount * (float)device.ClockRate_mhz / 2000.0f;
			}
		}

		public override FractalEngine CreateFractalEngine(OpenTK.Graphics.IGraphicsContext graphicsContext)
		{
			return new CudaFractalEngine(this.device);
		}

		public override IEnumerable<KeyValuePair<string,object>> GetDeviceInfo()
		{
			yield return new KeyValuePair<string,object>("CUDA Driver Version",
				String.Format("{0}", Cuda.Context.GetDriverVersion() ));

			yield return new KeyValuePair<string,object>("Cores",
				String.Format("{0} multiprocessors", device.MultiprocessorCount));

			yield return new KeyValuePair<string,object>("Speed",
				String.Format("{0} Mhz", device.ClockRate_mhz));

			yield return new KeyValuePair<string,object>("Warp Size",
				String.Format("{0}", device.WarpSize ));

			yield return new KeyValuePair<string,object>("Memory",
				String.Format("{0} Mb", (device.TotalMemory / 1048576) ));

			yield return new KeyValuePair<string,object>("Constant Memory",
				String.Format("{0} Kb", (device.ConstantMemory / 1024) ));

			yield return new KeyValuePair<string,object>("Registers",
				String.Format("{0} per Multiprocessor", device.Registers ));

			yield return new KeyValuePair<string,object>("Shared Memory",
				String.Format("{0} Kb per Multiprocessor", (device.SharedMemory / 1024) ));
		}

		#region Device Queries
		private static CudaDeviceEntry[] devEntries = null;
		
		private static void queryDevices()
		{
			if(devEntries != null) return;
			try{
				CheckSupport();

				Device[] devList = Device.Devices;
				devEntries = new CudaDeviceEntry[devList.Length];
				for(int i = 0; i < devList.Length; i++)
					devEntries[i] = new CudaDeviceEntry(i, devList[i]);
			}
			catch{
				devEntries = new CudaDeviceEntry[0];
			}
		}

		public static void CheckSupport()
		{
#if DISABLE_CUDA
			throw new NotSupportedException("No CUDA Capable devices could be found.");
#else
			if(Device.Devices.Length <= 0)
				throw new NotSupportedException("No CUDA Capable devices could be found.");
#endif
		}

		public static DeviceEntry[] GetDevices()
		{
			if(devEntries == null)
				queryDevices();
 			return devEntries;
		}
		#endregion
	}
}