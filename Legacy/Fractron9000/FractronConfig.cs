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

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace Fractron9000
{
	public enum EngineType{
		Auto,
		Cpu,
		Cuda,
		OpenCL,
		Hybrid,
	};

	public class FractronConfig
	{
		public bool Valid;                //This will be set to true if the engine starts successfuly with this config.
		public bool AutoSizeRenderer;     //Should the fractal output resolution automatically match the window size?
		
		public const int VersionMajor = 0;
		public const int VersionMinor = 5;

		public static string VersionString{
			get{ return string.Format( "{0}.{1}", VersionMajor, VersionMinor); }
		}

		private EngineType engineType;        //Which rendering engine to use (OpenCL, CUDA, etc...)
		public EngineType EngineType{
			get{ return engineType; }
			set{ engineType = value; }
		}

		private string deviceName;
		public string DeviceName{
			get{ return deviceName; }
			set{ deviceName = value; }
		}

		private string deviceIdentifier;
		public string DeviceIdentifier{
			get{ return deviceIdentifier; }
			set{ deviceIdentifier = value; }
		}

		private int deviceIndex;
		public int DeviceIndex{
			get{ return deviceIndex; }
			set{ deviceIndex = value; }
		}

		private Size customRes;
		public Size CustomRes             //For non-autosized configs, what should the renderer resolution be?
		{
			get{ return customRes; }
			set{
				customRes = new Size(Math.Max(value.Width, 8), Math.Max(value.Height, 8));
			}
		}

		private float targetQuality;
		public float TargetQuality        //How high should the output quality be before iteration stops?
		{
			get{ return targetQuality; }
			set{ targetQuality = Math.Max(value, 32.0f); }
		}

		private int redrawQuality;
		public int RedrawQuality          //What quality should the initial frame use?
		{
			get{ return redrawQuality; }
			set{ redrawQuality = Math.Max(value, 32); }
		}
		
		public string ImageDir;           //Where should exported images be saved to?
		public string PaletteDir;         //Where should palettes be loaded from?
		public string CurrentLibraryFile; //What .flame file is currently loaded?


		public static string FractronDataDir{
			get{
				return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Fractron 9000");
			}
		}
		public static string DefaultImageDir{
			get{ return Environment.GetFolderPath(Environment.SpecialFolder.MyPictures); }
		}
		public static string DefaultPaletteDir{
			get{ return Path.Combine(Application.StartupPath, "palettes"); }
		}
		public static string DefaultLibraryFile{
			get{ return Path.Combine(FractronDataDir,"fractals\\My Fractals.flame"); }
		}

		public static string LicenseFileName{
			get{ return Path.Combine(Application.StartupPath, "license.txt"); }
		}
		
		public static string ManualFileName{
			get{ return Path.Combine(Application.StartupPath, "manual.html"); }
		}

		/*
		public static string KernelsDir{
			get{ return Path.Combine(Application.StartupPath, "Kernels"); }
		}

		public static string KernelsIncludeDir{
			get{ return Path.Combine(KernelsDir, "include"); }
		}
		*/

		public FractronConfig() //gets a default configuration
		{
			this.Valid = false;
			this.EngineType = EngineType.Auto;
			this.DeviceName = "";
      this.DeviceIdentifier = "";
      this.DeviceIndex = 0;
			this.AutoSizeRenderer = true;
			this.CustomRes = new Size(800,600);
			this.TargetQuality = 256.0f;
			this.RedrawQuality = 256;

			this.ImageDir = DefaultImageDir;
			this.PaletteDir = DefaultPaletteDir;
			this.CurrentLibraryFile = DefaultLibraryFile;
		}

		public FractronConfig(FractronConfig src) //copies config from src
		{
			this.Valid = src.Valid;
			this.EngineType = src.EngineType;
			this.DeviceName = src.DeviceName;
      this.DeviceIdentifier = src.DeviceIdentifier;
      this.DeviceIndex = src.DeviceIndex;
			this.AutoSizeRenderer = src.AutoSizeRenderer;
			this.CustomRes = src.CustomRes;
			this.TargetQuality = src.TargetQuality;
			this.RedrawQuality = src.RedrawQuality;

			this.ImageDir = src.ImageDir;
			this.PaletteDir = src.PaletteDir;
			this.CurrentLibraryFile = src.CurrentLibraryFile;
		}

		public string FractalDir{
			get{
				string result = "";
				try{
					result = Path.GetDirectoryName(CurrentLibraryFile);
				}catch(Exception){}
				return result;
			}
		}
		

		public static string FractronConfigFileName{
			get{
				return Path.Combine(FractronDataDir, "config.xml");
			}
		}

		public static void DoInitialSetup()
		{
			//create the license if it doesn't exist
			if(!File.Exists(LicenseFileName))
				File.WriteAllText(LicenseFileName, DefaultFiles.License, System.Text.Encoding.ASCII);

			//create the manual if it doesn't exist
			if(!File.Exists(ManualFileName))
				File.WriteAllText(ManualFileName, DefaultFiles.Manual, System.Text.Encoding.UTF8);
			
			//create the user data dir
			if(!Directory.Exists(FractronDataDir))
				Directory.CreateDirectory(FractronDataDir);

			//create the flame dir if needed
			string fractalDir = Path.Combine(FractronDataDir, "fractals");
			if(!Directory.Exists(fractalDir))
				Directory.CreateDirectory(fractalDir);
			//create the default .flame files
			checkFile(DefaultFiles.Flames_My_Fractals,     Path.Combine(fractalDir, "My Fractals.flame"));
			checkFile(DefaultFiles.Flames_Sample_Fractals, Path.Combine(fractalDir, "Sample Fractals.flame"));

			//create the default palette dir if needed
			string paletteDir = DefaultPaletteDir;
			if(!Directory.Exists(paletteDir))
				Directory.CreateDirectory(paletteDir);
			//create the default palettes
			checkPaletteImage(DefaultFiles.Palette_default,  Path.Combine(paletteDir, "default.png"));
			checkPaletteImage(DefaultFiles.Palette_dark,     Path.Combine(paletteDir, "dark.png"));
			checkPaletteImage(DefaultFiles.Palette_frost,    Path.Combine(paletteDir, "frost.png"));
			checkPaletteImage(DefaultFiles.Palette_inferno2, Path.Combine(paletteDir, "inferno2.png"));

			/*
			if(!Directory.Exists(KernelsDir))
				Directory.CreateDirectory(KernelsDir);
			File.WriteAllBytes(Path.Combine(KernelsDir, "kernels.c"), Kernels.KernelResources.kernels_c);
			if(!Directory.Exists(KernelsIncludeDir))
				Directory.CreateDirectory(KernelsIncludeDir);
			File.WriteAllBytes(Path.Combine(KernelsIncludeDir, "interop.h"), Kernels.KernelResources.interop_h);
			File.WriteAllBytes(Path.Combine(KernelsIncludeDir, "random_mwc.h"), Kernels.KernelResources.random_mwc_h);
			File.WriteAllBytes(Path.Combine(KernelsIncludeDir, "fractron_math.h"), Kernels.KernelResources.fractron_math_h);
			File.WriteAllBytes(Path.Combine(KernelsIncludeDir, "config.h"), Kernels.KernelResources.config_h);
			File.WriteAllBytes(Path.Combine(KernelsIncludeDir, "iterate.h"), Kernels.KernelResources.iterate_h);
			 * */
		}

		private static void checkPaletteImage(Bitmap bmp, string fileName)
		{
			if(File.Exists(fileName)) //bail out if the palette already exists
				return;

			//save bmp as a png image
			ImageCodecInfo png_ici = null;
			var encoders = ImageCodecInfo.GetImageEncoders();
			foreach(var ici in encoders)
				if(ici.MimeType == "image/png")
					png_ici = ici;

			if(png_ici == null)
				throw new FractronException("Could not find png encoder.");

			var ep = new EncoderParameters(1);
			ep.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);

			bmp.Save(fileName, png_ici, ep);
		}

		private static void checkFile(byte[] data, string fileName)
		{
			if(!File.Exists(fileName)) //write the file if it doesn't exist already
				File.WriteAllBytes(fileName, data);
		}

		public static FractronConfig Load()
		{
			FileStream fs = null;
			FractronConfig conf = new FractronConfig();

			try{
				fs = File.Open(FractronConfigFileName, FileMode.Open, FileAccess.Read, FileShare.None);
			}
			catch(DirectoryNotFoundException){
				return conf;
			}
			catch(FileNotFoundException){
				return conf;
			}

			try{
				XmlReaderSettings settings = new XmlReaderSettings();
				settings.CheckCharacters = false;
				settings.CloseInput = true;
				settings.ConformanceLevel = ConformanceLevel.Fragment;
				settings.IgnoreComments = false;
				settings.IgnoreWhitespace = false;
				settings.ValidationType = ValidationType.None;
				
				XmlReader reader = XmlReader.Create(fs, settings);

				XmlDocument doc = new XmlDocument();
				
				doc.Load(reader);

				conf.LoadFromXmlDoc(doc);
			}
			finally{
				if(fs != null)
					fs.Close();
			}
			conf.Valid = false;
			return conf;
		}

		public static void Save(FractronConfig conf)
		{
			//it is a bad idea to save a configuration that hasn't been validated
			if(!conf.Valid)
				return;

			FileStream fs = null;
			try{
				fs = File.Open(FractronConfigFileName, FileMode.Create, FileAccess.Write);
				TextWriter writer = new StreamWriter(fs);
				conf.WriteXml(writer);
			}
			catch(Exception ex)
			{
				throw(ex);
			}
			finally
			{
				if(fs != null)
					fs.Close();
			}
		}

		public void WriteXml(TextWriter writer)
		{
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.Indent = true;

			XmlWriter xw = XmlWriter.Create(writer, settings);
			
			xw.WriteStartDocument();
			xw.WriteStartElement("FractronConfig");
			xw.WriteAttributeString("version", VersionString);

			string engineString = "Auto";
			switch(EngineType){
				case EngineType.Auto: engineString = "Auto"; break;
				case EngineType.Cpu: engineString = "Cpu"; break;
				case EngineType.Cuda: engineString = "Cuda"; break;
				case EngineType.OpenCL: engineString = "OpenCL"; break;
				case EngineType.Hybrid: engineString = "Hybrid"; break;
				default: engineString = "Auto"; break;
			}
			xw.WriteStartElement("Engine");
			xw.WriteAttributeString("Type", engineString);
			xw.WriteAttributeString("DeviceName", deviceName.ToString());
			xw.WriteAttributeString("DeviceIdentifier", deviceIdentifier.ToString());
			xw.WriteAttributeString("DeviceIndex", deviceIndex.ToString());
			xw.WriteEndElement();

			xw.WriteElementString("AutoSizeRenderer", AutoSizeRenderer.ToString());

			xw.WriteStartElement("CustomRes");
			xw.WriteElementString("Width", CustomRes.Width.ToString());
			xw.WriteElementString("Height", CustomRes.Height.ToString());
			xw.WriteEndElement();

			xw.WriteElementString("TargetQuality", TargetQuality.ToString());
			xw.WriteElementString("RedrawQuality", RedrawQuality.ToString());
			xw.WriteElementString("ImageDir", ImageDir);
			xw.WriteElementString("PaletteDir", PaletteDir);
			xw.WriteElementString("CurrentLibraryFile", CurrentLibraryFile);

			xw.WriteEndDocument();

			xw.Flush();
		}
		
		private void LoadFromXmlDoc(XmlDocument doc)
		{
			string configVersion = "";

			XmlNode confNode = null;
			foreach(XmlNode topNode in doc.ChildNodes)
				if(topNode.Name == "FractronConfig")
					confNode = topNode;

			if(confNode == null)
				throw new Exception("Could not find configuration information in file.");

			if(confNode.Attributes["version"] != null)
			{
				configVersion = confNode.Attributes["version"].Value;
			}

			foreach(XmlNode node in confNode.ChildNodes)
			{
				if(node.Name == "Engine")
				{
					string typeStr = (node.Attributes["Type"] == null ? null : node.Attributes["Type"].Value.ToLower());

					if(typeStr == null || typeStr == "auto")
						EngineType = EngineType.Auto;
					else if(typeStr == "cpu")
						EngineType = EngineType.Cpu;
					else if(typeStr == "cuda")
						EngineType = EngineType.Cuda;
					else if(typeStr == "opencl")
						EngineType = EngineType.OpenCL;
					else if(typeStr == "hybrid")
						EngineType = EngineType.Hybrid;

          if(node.Attributes["DeviceName"] != null)
            deviceName = node.Attributes["DeviceName"].Value;
          else
            deviceName = "";

          if(node.Attributes["DeviceIdentifier"] != null)
            deviceIdentifier = node.Attributes["DeviceIdentifier"].Value;
          else
            deviceIdentifier = "";

					if(node.Attributes["DeviceIndex"] != null)
					{
						if(!int.TryParse(node.Attributes["DeviceIndex"].Value, out deviceIndex))
							deviceIndex = 0;
					}
				}
				else if(node.Name == "AutoSizeRenderer")
					AutoSizeRenderer = (node.InnerText.ToLowerInvariant() == "true");
				else if(node.Name == "TargetQuality")
					TargetQuality = readFloatElem(node, TargetQuality);
				else if(node.Name == "RedrawQuality")
					RedrawQuality= readIntElem(node, RedrawQuality);
				else if(node.Name == "ImageDir")
					ImageDir = node.InnerText;
				else if(node.Name == "PaletteDir")
					PaletteDir = node.InnerText;
				else if(node.Name == "CurrentLibraryFile")
					CurrentLibraryFile = node.InnerText;
				else if(node.Name == "CustomRes")
				{
					Size s = CustomRes;
					foreach(XmlNode crNode in node.ChildNodes)
					{
						if(crNode.Name == "Width")
							s.Width = readIntElem(crNode, s.Width);
						else if(crNode.Name == "Height")
							s.Height = readIntElem(crNode, s.Height);
					}
					CustomRes = s;
				}
			}

			//configs prior to 0.3 should have the palette dir reset to default
			if(configVersion == null || configVersion == "" || configVersion == "0.2")
			{
				PaletteDir = DefaultPaletteDir;
			}
		}

		//tries to parse an element as an int and return it.
		//On failure, returns defaultValue
		private static int readIntElem(XmlNode node, int defaultValue)
		{
			int newVal;
			if(int.TryParse(node.InnerText, out newVal))
				return newVal;
			else
				return defaultValue;
		}

		//tries to parse an element as a float and return it.
		//On failure, returns defaultValue
		private static float readFloatElem(XmlNode node, float defaultValue)
		{
			float newVal;
			if(float.TryParse(node.InnerText, out newVal))
				return newVal;
			else
				return defaultValue;
		}
	}
}