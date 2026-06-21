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

//#define DISABLE_CPU_ENGINE //this makes it look like there's no support for the CPU engine

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Management;
using OpenTK;
using OpenTK.Graphics.OpenGL;

using MTUtil;

namespace Fractron9000.CPUEngine
{
	public class CPUDeviceEntry : DeviceEntry
	{
		private const float baseRank = 3.0f;

		string name = null;
		int glVersionMajor = 0;
		int glVersionMinor = 0;
		int glslVersionMajor = 0;
		int glslVersionMinor = 0;
		int numCores = 0;
		int clockRateMhz = 0;

		public CPUDeviceEntry()
		{
			name = "Unknown Processor";
			numCores = 1;
			clockRateMhz = 0;
			ManagementObjectSearcher searcher = new ManagementObjectSearcher("select * from Win32_Processor");
			ManagementObjectCollection objs = searcher.Get();
			ManagementObject procObj = null;
			if(objs.Count > 0)
			{
				foreach(ManagementObject mo in objs){
					procObj = mo;
					break;
				}

				try{
					name = Convert.ToString(procObj["name"]);
				}catch{}

				numCores = Environment.ProcessorCount;

				//This doesn't always seem to give the right answer
				//try{
				//	numCores = Convert.ToInt32(procObj["numberofcores"]); 
				//}catch{}

				try{
					clockRateMhz = Convert.ToInt32(procObj["currentclockspeed"]);
				}catch{}
			}
			glVersionMajor = 0;
			glVersionMinor = 0;
			glslVersionMajor = 0;
			glslVersionMinor = 0;

			parseGLVersionString(GL.GetString(StringName.Version), out glVersionMajor, out glVersionMinor);

			if(glVersionMajor >= 2) //if the GL version is 2.0 or greater, then also get the GLSL version
				parseGLVersionString(GL.GetString(StringName.ShadingLanguageVersion), out glslVersionMajor, out glslVersionMinor);
		}

		public override string Name{
			get{ return name; }
		}
		public override string Api{
			get{ return "CPU+OpenGL"; }
		}
		public override EngineType EngineType{
			get { return EngineType.Cpu; }
		}
    public override string Identifier{
      get { return "System Processor"; }
    }
		public override int Index{
			get { return 0; }
		}
		public override float PerformanceRating{
			get{
				int assumedSpeed = clockRateMhz == 0 ? 2000 : clockRateMhz; //if the clock rate is unknown, just assume 2000mhz
				return baseRank * (float)numCores * (float)assumedSpeed / 2000.0f;
			}
		}

		public int GLVersionMajor{
			get{ return glVersionMajor; }
		}
		public int GLVersionMinor{
			get{ return glVersionMinor; }
		}
		public int GLSLVersionMajor{
			get{ return glslVersionMajor; }
		}
		public int GLSLVersionMinor{
			get{ return glslVersionMinor; }
		}
		public int NumCores{
			get{ return numCores; }
		}

		public override FractalEngine CreateFractalEngine(OpenTK.Graphics.IGraphicsContext graphicsContext)
		{
			return new CPUFractalEngine(this);
		}

		private static KeyValuePair<string,object> kv(string key, string fmt, params object[] objs) //used to make GetDeviceInfo a bit more readable
		{
			return new KeyValuePair<string,object>(key, String.Format(fmt, objs));
		}

		public override IEnumerable<KeyValuePair<string,object>> GetDeviceInfo()
		{
			yield return kv("OpenGL Version", "{0}.{1}", glVersionMajor, glVersionMinor);
			yield return kv("GLSL Version", "{0}.{1}", glslVersionMajor, glslVersionMinor);
			yield return kv("Number of Cores", this.numCores.ToString());
			yield return kv("Clock Speed", this.clockRateMhz == 0 ? "unknown" : this.clockRateMhz.ToString()+" mhz");
		}

		/// <summary>
		/// extracts the major and minor version numbers from an OpenGL version string
		/// </summary>
		private static void parseGLVersionString(string verString, out int major, out int minor)
		{
			major = 0;
			minor = 0;
			if(verString == null)
				return;
			//remove any trailing vendor info
			string[] verSections = verString.Split(new char[]{' '}, StringSplitOptions.RemoveEmptyEntries);
			if(verSections.Length > 0)
			{
				string[] parts = verSections[0].Split(new char[]{'.'}, StringSplitOptions.RemoveEmptyEntries); //split on '.' to get major and minor

				if(parts.Length > 0)
					if(!int.TryParse(parts[0], out major))
						major = 0;
				if(parts.Length > 1)
					if(!int.TryParse(parts[1], out minor))
						minor = 0;
			}
		}

		#region Device Queries
		private static CPUDeviceEntry[] devEntries = null;

		private static void queryDevices()
		{
			if(devEntries != null) return;
			try{
				CheckSupport();
				devEntries = new CPUDeviceEntry[1];
				devEntries[0] = new CPUDeviceEntry();
			}
			catch
			{
				devEntries = new CPUDeviceEntry[0];
			}
		}
		
		/// <summary>
		/// Checks to see if the CPU engine is supported, and throws an exception if it isn't.
		/// </summary>
		public static void CheckSupport()
		{
#if DISABLE_CPU_ENGINE
			//throw new NotSupportedException("Test Error");
			throw new OpenGLVersionException("OpenGL", "2.0", "1.5");
#else
			int glVersionMajor = 0, glVersionMinor = 0;
			int glslVersionMajor = 0, glslVersionMinor = 0;

			parseGLVersionString(GL.GetString(StringName.Version), out glVersionMajor, out glVersionMinor);

			if(glVersionMajor >= 2) //if the GL version is 2.0 or greater, then also get the GLSL version
				parseGLVersionString(GL.GetString(StringName.ShadingLanguageVersion), out glslVersionMajor, out glslVersionMinor);

			if(glVersionMajor < 2)
			{
				throw new OpenGLVersionException("OpenGL", "2.0", glVersionMajor.ToString()+"."+glVersionMinor.ToString());
			}
			if(glslVersionMajor < 1)
			{
				throw new OpenGLVersionException("GL Shading Language", "1.0", glslVersionMajor.ToString()+"."+glslVersionMinor.ToString());
			}
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
	public class OpenGLVersionException : Exception
	{
		public string ComponentName{
			get{
				if(Data.Contains("componentName"))
					return Data["componentName"] as string;
				else
					return "none";
			}
		}
		public string RequiredVersion{
			get{
				if(Data.Contains("requiredVersion"))
					return Data["requiredVersion"] as string;
				else
					return "none";
			}
		}
		public string FoundVersion{
			get{
				if(Data.Contains("foundVersion"))
					return Data["foundVersion"] as string;
				else
					return "none";
			}
		}
		public OpenGLVersionException(string componentName, string requiredVersion, string foundVersion)
			: base(string.Format("{0} version {1} is required, but {0} version {2} was found", componentName, requiredVersion, foundVersion))
		{
			Data["componentName"] = componentName;
			Data["requiredVersion"] = requiredVersion;
			Data["foundVersion"] = foundVersion;
		}
	}
}