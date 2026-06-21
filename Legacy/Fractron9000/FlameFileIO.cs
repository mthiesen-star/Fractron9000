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
using System.IO;
using System.Collections.Generic;
using System.Xml;
using System.Text;
using System.Drawing;

using MTUtil;

namespace Fractron9000
{
	public class FlameFormatException : System.Exception
	{
		public FlameFormatException()
			: base() {}
		public FlameFormatException(string message)
			: base(message) {}
	}

	internal static class FlameFileIO
	{
		public static string IOVersion{
			get{ return string.Format("Fractron9000 {0}", FractronConfig.VersionString); }
		}

		//makes a copy of a file with .bak00n appended to the name
		//returns the name of the backup file, or null if something goes wrong.
		internal static string BackupFlameFile(string fullFileName)
		{
			string bakFileName = "";
			for(int i = 0; i < 100; i++)
			{
				bakFileName = fullFileName + ".bak" + i.ToString();
				if(!File.Exists(bakFileName))
					break;
			}
			try{
				File.Copy(fullFileName, bakFileName, false);
			}
			catch(Exception)
			{
				return null;
			}
			return bakFileName;
		}

		internal static void WriteFlameFile(string fileName, FractalList fractals)
		{
			string name = null;
			try{
				name = Path.GetFileNameWithoutExtension(fileName);
			}catch(Exception){}

			if(name == null || name == "")
				name = "unnamed";

			FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
			try{
				XmlWriterSettings settings = new XmlWriterSettings();
				settings.Indent = true;
				settings.IndentChars = "  ";
				settings.Encoding = System.Text.Encoding.UTF8;
				settings.OmitXmlDeclaration = true;
				settings.ConformanceLevel = ConformanceLevel.Fragment;

				XmlWriter output = XmlWriter.Create(fs, settings);
				WriteFlames(output, name, fractals);

				output.Flush();
			}
			finally{
				fs.Close();
			}			
		}

		internal static void WriteFlames(XmlWriter xw, string name, FractalList fractals)
		{
			xw.WriteStartElement("flames");
			xw.WriteAttributeString("name", name);
			foreach(Fractal fractal in fractals)
			{
				WriteFlame(xw, fractal);
			}

			xw.WriteEndElement();
		}

		internal static void WriteFlame(XmlWriter xw, Fractal fractal)
		{
			int xSize;
			int ySize;
			float xCenter;
			float yCenter;
			float scale;
			float zoom;
			float rotate;
			fractal.GetFlameFromCamera(out xSize, out ySize, out xCenter, out yCenter, out scale, out zoom, out rotate);


			xw.WriteStartElement("flame");
			xw.WriteAttributeString("name", fractal.Name);
			xw.WriteAttributeString("version", IOVersion);
			xw.WriteAttributeString("size", string.Format("{0} {1}", xSize, ySize));
			xw.WriteAttributeString("center", string.Format("{0:0.####} {1:0.####}", xCenter, yCenter));
			xw.WriteAttributeString("scale", string.Format("{0:0.####}", scale));
			xw.WriteAttributeString("zoom", string.Format("{0:0.####}", zoom));
			xw.WriteAttributeString("rotate", string.Format("{0:0.#}", rotate));

			xw.WriteAttributeString("background", string.Format("{0:0.########} {1:0.########} {2:0.########}",
				fractal.BackgroundColor.X, fractal.BackgroundColor.Y, fractal.BackgroundColor.Z));
			xw.WriteAttributeString("brightness", string.Format("{0:0.########}", fractal.Brightness));
			xw.WriteAttributeString("gamma", string.Format("{0:0.########}", fractal.Gamma));
			xw.WriteAttributeString("vibrancy", string.Format("{0:0.########}", fractal.Vibrancy));

			bool use2DColor = (fractal.Palette == null || fractal.Palette.Type != Palette.PaletteType.OneDimensional);
			foreach(Branch branch in fractal.Branches)
				writeBranch(xw, branch, use2DColor);

			writePalette(xw, fractal.Palette);

			xw.WriteEndElement();			
		}

		private static void writeBranch(XmlWriter xw, Branch branch, bool use2DColor)
		{
			xw.WriteStartElement("xform");
			xw.WriteAttributeString("weight", string.Format("{0:0.########}", branch.Weight));
			xw.WriteAttributeString("color", string.Format("{0:0.########}", branch.Chroma.X));
			if(use2DColor)
				xw.WriteAttributeString("f9k_color2", string.Format("{0:0.########}", branch.Chroma.Y));

			if(branch.ColorWeight != 0.5f)
				xw.WriteAttributeString("f9k_colorweight", string.Format("{0:0.########}", branch.ColorWeight));

			foreach(Branch.VariEntry ve in branch.Variations)
				if(ve.Weight != 0.0f && ve.Index >= 0 && ve.Index < Variation.Count)
					xw.WriteAttributeString(Variation.Variations[ve.Index].AttrName, string.Format("{0:0.########}", ve.Weight));
			
			xw.WriteAttributeString("coefs", string.Format(
				"{0:0.########} {1:0.########} {2:0.########} {3:0.########} {4:0.########} {5:0.########}",
				branch.PreTransform.A, branch.PreTransform.D, branch.PreTransform.B,
				branch.PreTransform.E, branch.PreTransform.C, branch.PreTransform.F ));

			if(branch.Localized)
				xw.WriteAttributeString("post", string.Format(
					"{0:0.########} {1:0.########} {2:0.########} {3:0.########} {4:0.########} {5:0.########}",
					branch.PostTransform.A, branch.PostTransform.D, branch.PostTransform.B,
					branch.PostTransform.E, branch.PostTransform.C, branch.PostTransform.F ));

			xw.WriteEndElement();
		}

		private static void writePalette(XmlWriter xw, Palette palette)
		{
			if(palette == null)
				return;
			if(palette.Type == Palette.PaletteType.OneDimensional)
				write1DPalette(xw, palette);
			else if(palette.Type == Palette.PaletteType.FromFile)
				writeFilePalette(xw, palette);
		}

		private static void writeFilePalette(XmlWriter xw, Palette palette)
		{
			xw.WriteStartElement("palette");
			xw.WriteAttributeString("src", palette.Name);
			xw.WriteEndElement();
		}

		private static void write1DPalette(XmlWriter xw, Palette palette)
		{
			xw.WriteStartElement("palette");
			xw.WriteAttributeString("count", palette.Width.ToString());
			xw.WriteAttributeString("format", "RGB");
			xw.WriteWhitespace("\r\n");

			Color c = Color.Black;
			for(int x = 0; x < palette.Width; x++)
			{
				if(x%8 == 0)
					xw.WriteWhitespace("      ");

				c = palette.GetPixel(x, 0);
				xw.WriteString(string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B));

				if(x%8 == 7 || x == palette.Width-1)
					xw.WriteWhitespace("\r\n");
			}
			xw.WriteWhitespace("    ");
			xw.WriteEndElement();
		}

		internal static FractalList ReadFlameFile(string filename, FractronConfig conf)
		{
			FractalList newFractals = new FractalList();
			Fractal newFractal;

			XmlReaderSettings settings = new XmlReaderSettings();
			settings.CheckCharacters = false;
			settings.CloseInput = true;
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			settings.IgnoreComments = false;
			settings.IgnoreWhitespace = false;
			settings.ValidationType = ValidationType.None;
			
			XmlReader reader = XmlReader.Create(filename, settings);

			XmlDocument doc = new XmlDocument();
			doc.Load(reader);
			reader.Close();

			XmlNode flamesNode = null;

			foreach(XmlNode node in doc.ChildNodes)
				if(node.NodeType == XmlNodeType.Element)
					flamesNode = node;

			if(flamesNode == null)
				return newFractals;

			foreach(XmlNode node in flamesNode)
			{
				if(node.Name == "flame")
				{
					newFractal = readFlameNode(node, conf);
					if(newFractal != null)
						newFractals.Add(newFractal);
				}
			}

			return newFractals;
		}

		private static Fractal readFlameNode(XmlNode flameNode, FractronConfig conf)
		{
			Fractal newFractal = new Fractal();
			Palette apoPalette = null;
			float xSize = 800.0f;
			float ySize = 600.0f;
			float xCenter = 0.0f;
			float yCenter = 0.0f;
			float scale = 100.0f;
			float zoom = 1.0f;
			float rotate = 0.0f;

			if(flameNode == null)
				return null;

			foreach(XmlAttribute attr in flameNode.Attributes)
			{
				if(attr.Name=="name")
					newFractal.Name = attr.Value;
				else if(attr.Name=="version")
				{
					newFractal.Version = attr.Value;
				}
				else if(attr.Name=="size")
				{
					try{
						string[] xyStr = attr.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						xSize =  float.Parse(xyStr[0]);
						ySize =  float.Parse(xyStr[1]);
					}catch(Exception){}
				}
				else if(attr.Name=="center")
				{
					try{
						string[] xyStr = attr.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						xCenter =  float.Parse(xyStr[0]);
						yCenter =  float.Parse(xyStr[1]);
					}catch(Exception){}
				}
				else if(attr.Name=="scale")
					readFloatAttr(attr, ref scale);
				else if(attr.Name=="zoom")
					readFloatAttr(attr, ref zoom);
				else if(attr.Name=="rotate")
					readFloatAttr(attr, ref rotate);
				else if(attr.Name=="background")
				{
					try{
						string[] rgbStr = attr.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						newFractal.BackgroundColor.X = float.Parse(rgbStr[0]);
						newFractal.BackgroundColor.Y = float.Parse(rgbStr[1]);
						newFractal.BackgroundColor.Z = float.Parse(rgbStr[2]);
						newFractal.BackgroundColor.W = 1.0f;
					}catch(Exception){}
				}
				else if(attr.Name=="brightness")
					readFloatAttr(attr, ref newFractal.Brightness);
				else if(attr.Name=="gamma")
					readFloatAttr(attr, ref newFractal.Gamma);
				else if(attr.Name=="vibrancy")
					readFloatAttr(attr, ref newFractal.Vibrancy);
			}

			newFractal.SetCameraFromFlame(xSize, ySize, xCenter, yCenter, scale, zoom, rotate);

			foreach(XmlNode node in flameNode.ChildNodes)
			{
				if(node.Name == "xform")
				{
					newFractal.Branches.Add(readBranchNode(node));
				}
				else if(node.Name == "palette")
				{
					apoPalette = readPaletteNode(node, conf);
				}
				else if(node.Name == "color") //handle the old style palettes
				{
					if(apoPalette == null)
					{
						apoPalette =  new Palette(256,1,Palette.PaletteType.OneDimensional);
						for(int i = 0; i < 256; i++)
							apoPalette.SetPixel(i, 0, Color.White);
					}

					try{
						int index = int.Parse(node.Attributes["index"].Value);
						string[] rgbStr = node.Attributes["rgb"].Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						Color c = Color.FromArgb(
							int.Parse(rgbStr[0]),
							int.Parse(rgbStr[1]),
							int.Parse(rgbStr[2]));
						apoPalette.SetPixel(index, 0, c);
					}
					catch(Exception){}
				}

			}

			if(apoPalette != null)
				newFractal.Palette = apoPalette;

			return newFractal;
		}

		private static Branch readBranchNode(XmlNode node)
		{
			Branch branch = new Branch();
			branch.Variations.Clear();

			bool foundPost = false;
			bool foundLocalized = false;

			foreach(XmlAttribute attr in node.Attributes)
			{
				if(attr.Name=="weight")
					readFloatAttr(attr, ref branch.Weight);
				else if(attr.Name=="coefs")
				{
					try{
						string[] coefArr = attr.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						branch.PreTransform.XAxis.X =  float.Parse(coefArr[0]);
						branch.PreTransform.XAxis.Y =  float.Parse(coefArr[1]);
						branch.PreTransform.YAxis.X =  float.Parse(coefArr[2]);
						branch.PreTransform.YAxis.Y =  float.Parse(coefArr[3]);
						branch.PreTransform.Translation.X =  float.Parse(coefArr[4]);
						branch.PreTransform.Translation.Y =  float.Parse(coefArr[5]);
					}
					catch(Exception){
						branch.PreTransform = Affine2D.Identity;
					}
				}
				else if(attr.Name=="post")
				{
					try{
						string[] coefArr = attr.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						branch.PostTransform.XAxis.X =  float.Parse(coefArr[0]);
						branch.PostTransform.XAxis.Y =  float.Parse(coefArr[1]);
						branch.PostTransform.YAxis.X =  float.Parse(coefArr[2]);
						branch.PostTransform.YAxis.Y =  float.Parse(coefArr[3]);
						branch.PostTransform.Translation.X =  float.Parse(coefArr[4]);
						branch.PostTransform.Translation.Y =  float.Parse(coefArr[5]);
						foundPost = true;
					}
					catch(Exception){
						branch.PostTransform = Affine2D.Identity;
					}
				}
				else if(attr.Name=="vars") //found old style variations
				{
					try{
						string[] varArr = attr.Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						for(int i = 0; i < varArr.Length && i < 14; i++)
						{
							float weight = float.Parse(varArr[i]);
							if(weight != 0.0f)
								branch.Variations.Add(new Branch.VariEntry(i, weight));
						}
					}
					catch(Exception){
					}
				}
				else if(attr.Name=="color")
				{
					try{
						string[] colorStr = node.Attributes["color"].Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
						if(colorStr.Length >= 1)
							branch.Chroma.X = float.Parse(colorStr[0]);
						if(colorStr.Length >= 2)
							branch.Chroma.Y = float.Parse(colorStr[1]);
					}catch(Exception){}
				}
				else if(attr.Name=="f9k_color2")
				{
					readFloatAttr(attr, ref branch.Chroma.Y);
				}
				else if(attr.Name=="f9k_colorweight")
				{
					readFloatAttr(attr, ref branch.ColorWeight);
				}
				else if(attr.Name=="f9k_localized")
				{
					int iLocalized = 0;
					readIntAttr(attr, ref iLocalized);
					foundLocalized = (iLocalized == 1);
				}
				else
				{
					//see if the attribute is a known variation name
					int idx = -1;
					for(int i = 0; i < Variation.Count; i++)
						if(Variation.Variations[i].AttrName == attr.Name)
							idx = i;

					//yep, its a known variation
					if(idx >= 0 && idx < Variation.Count){
						float weight = 0;
						readFloatAttr(attr, ref weight);
						branch.Variations.Add(new Branch.VariEntry(idx,weight));
					}
				}
			}

			//figure out whether or not to treat this as a localized branch
			if(foundLocalized)
			{   //the branch is tagged as localized, so this is easy
				branch.Localized = true;
			}
			else if(foundPost)
			{
				//If the post transform is roughly the inverse of the pre transform, then this is probably supposted to be localized
				branch.Localized = branch.PreTransform.Equals(branch.PostTransform.Inverse, 0.001f);
			}
			else
			{   //just a plain old non-localized branch.
				branch.Localized = false;
			}
			
			//if no variations at all could be found, assume linear
			if(branch.Variations.Count == 0)
				branch.Variations.Add(new Branch.VariEntry(0,1.0f));

			return branch;
		}
		private static void branchVariHelper(Branch branch, int index, float value)
		{
			if(value != 0.0f)
				branch.Variations.Add(new Branch.VariEntry(index, value));
		}

		private static Palette readPaletteNode(XmlNode node, FractronConfig conf)
		{
			Palette result = null;
			if(node.Attributes["src"] != null)
			{
				string fullFileName = Path.Combine(conf.PaletteDir, node.Attributes["src"].Value);
				try{
					result = new Palette(fullFileName);
				}catch(Exception){
					result = null;
				}
			}
			else if(node.Attributes["format"] != null &&
				node.Attributes["format"].Value == "RGB" )
			{
				try{
					result = read1DPaletteNode(node);
				}catch(Exception){
					result = null;
				}
			}
			return result;
		}

		private static Palette read1DPaletteNode(XmlNode node)
		{
			int count = 256;
			if(node.Attributes["count"] != null){
				try{
					count = int.Parse(node.Attributes["count"].Value);
				}catch(Exception){}
			}

			if(node.Attributes["format"] == null || node.Attributes["format"].Value != "RGB")
				return null;
			 
			Palette result = new Palette(count,1,Palette.PaletteType.OneDimensional);
			string content = node.InnerText;
			byte[] data = new byte[count*3];
			int nibble = 0;
			int ni = 0;  //nibble index
			
			foreach(char ch in content)
			{
				nibble = hexCharVal(ch);
				if(nibble >= 0 && (ni/2) < data.Length)
				{
					if(ni%2 == 0)
						data[ni/2] |= (byte)(nibble << 4);
					else
						data[ni/2] |= (byte)nibble;
					ni++;
				}
			}

			for(int i = 0; i < count; i++)
			{
				result.SetPixel(i, 0,
					(float)data[i*3+0] / 255.0f,
					(float)data[i*3+1] / 255.0f,
					(float)data[i*3+2] / 255.0f);
			}

			return result;
		}

		private static int hexCharVal(char ch)
		{
			if(ch >= '0' && ch <= '9')
				return (int)ch - (int)'0';
			else if(ch >= 'A' && ch <= 'F')
				return (int)ch - (int)'A' + 10;
			else if(ch >= 'a' && ch <= 'f')
				return (int)ch - (int)'a' + 10;
			else
				return -1;
		}

		//tries to parse an attribute as a float and write it to value.
		//On failure, value remains unchanged
		private static void readFloatAttr(XmlAttribute attr, ref float value)
		{
			float newVal;
			if(float.TryParse(attr.Value, out newVal))
				value = newVal;
		}

		//tries to parse an attribute as an int and write it to value.
		//On failure, value remains unchanged
		private static void readIntAttr(XmlAttribute attr, ref int value)
		{
			int newVal;
			if(int.TryParse(attr.Value, out newVal))
				value = newVal;
		}
	}
}
