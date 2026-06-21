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

//#define DISABLE_OPENCL //this makes OpenCL appear unsupported

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenCL;

using MTUtil;


namespace Fractron9000.HybridEngine
{
	public class HybridDeviceEntry : DeviceEntry
	{
		private const float baseGpuRank = 15.0f;
		private const float baseCpuRank = 2.0f;
		private const float noImagePenalty = 0.75f;

    private int index;
		private Platform platform;
		private Device device;

		public HybridDeviceEntry(int index, Platform platform, Device device)
		{
      this.index = index;
			this.platform = platform;
			this.device = device;
		}

		public override string Name{
			get{ return device.Name.Trim(); }
		}
		public override string Api{
			get{ return "Open[GL+CL]"; }
		}
		public override EngineType EngineType{
			get { return EngineType.Hybrid; }
		}
    public override string Identifier{
      get { return platform.Name.Trim() + "::" + device.Name.Trim(); }
    }
		public override int Index{
			get { return (int)index; }
		}
		public override float PerformanceRating{
			get{
				float baseRank;
				if((device.Type & DeviceTypeFlags.Cpu) == DeviceTypeFlags.Cpu)
					baseRank = baseCpuRank;
				else
					baseRank = baseGpuRank;
				if(!device.ImageSupport)
					baseRank *= noImagePenalty;

				return baseRank * (float)device.MaxComputeUnits * (float)device.MaxClockFrequency / 2000.0f;
			}
		}

		public override FractalEngine CreateFractalEngine(OpenTK.Graphics.IGraphicsContext graphicsContext)
		{
			return new HybridFractalEngine(graphicsContext, this.platform, this.device);
		}

		private static KeyValuePair<string,object> kv(string key, string fmt, params object[] objs) //used to make GetDeviceInfo a bit more readable
		{
			return new KeyValuePair<string,object>(key, String.Format(fmt, objs));
		}

		public override IEnumerable<KeyValuePair<string,object>> GetDeviceInfo()
		{
			yield return kv("Platform Vendor",       platform.Vendor);
			yield return kv("Platform Name",         platform.Name);
			yield return kv("Version",               device.Version);
			yield return kv("Vendor ID",             device.VendorId.ToString());
			yield return kv("Type",	                 "{0}", device.Type);
			yield return kv("Available",             device.Available ? "yes" : "no");

			yield return kv("MaxComputeUnits",       "{0} multiprocessors", device.MaxComputeUnits);
			yield return kv("Speed",                 "{0} Mhz", device.MaxClockFrequency);

			yield return kv("Global Memory",         "{0} Mb", (device.GlobalMemSize / 1048576));
			yield return kv("Global Cache",          "{0} Kb", (device.GlobalMemCacheSize / 1024));
			yield return kv("Global Cache Type",     device.GlobalMemCacheType.ToString());
			yield return kv("Base Addr. Alignment",  "{0} b", device.MemBaseAddrAlign );
			yield return kv("Min Data Alignment",    "{0} b", device.MinDataTypeAlignSize );

			yield return kv("Max Constant Buffer",   "{0} Kb", (device.MaxConstantBufferSize / 1024) );
			yield return kv("Local Memory",          "{0} Kb per Multiprocessor", (device.LocalMemSize / 1024) );

			yield return kv("Image Support",         device.ImageSupport ? "yes" : "no");

			string[] extArray = device.Extensions.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
			for(int i = 0; i < extArray.Length; i++)
				yield return kv("Extension", extArray[i]);
		}

		#region Device Queries
		private static HybridDeviceEntry[] devEntries = null;

		private static void queryDevices()
		{
			if(devEntries != null) return;

			try{
				CheckSupport();
				int index = 0;
				List<HybridDeviceEntry> devList = new List<HybridDeviceEntry>();
				foreach(Platform plat in Platform.GetPlatforms())
        {
					foreach(Device dev in plat.GetDevices())
          {
						devList.Add(new HybridDeviceEntry(index, plat, dev));
            index++;
          }
        }

				devEntries = devList.ToArray();
			}
			catch{
				devEntries = new HybridDeviceEntry[0];
			}
		}
		
		/// <summary>
		/// Checks to see if OpenCL is supported, and throws an exception if it isn't.
		/// </summary>
		public static void CheckSupport()
		{
#if DISABLE_OPENCL
			throw new NotSupportedException("No OpenCL Capable devices could be found.");
#else
			int devCount = 0;	
			foreach(Platform plat in Platform.GetPlatforms())
				devCount += plat.GetDevices().Length;
			
			if(devCount <= 0)
				throw new NotSupportedException("No OpenCL Capable devices could be found.");
#endif
		}

		public static DeviceEntry[] GetDevices()
		{
			if(devEntries == null) queryDevices();

			return devEntries;
		}
		#endregion
	}
}