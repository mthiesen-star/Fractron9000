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
	internal static class FractronFileIO
	{
		public class FractronFormatException : System.Exception
		{
			public FractronFormatException()
				: base() {}
			public FractronFormatException(string message)
				: base(message) {}
		}

		internal static Fractal ReadFractronFile(string filename)
		{
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

			XmlNode fractalNode = null;
			foreach(XmlNode node in doc.ChildNodes)
				if(node.Name == "Fractal")
					fractalNode = node;

			if(fractalNode == null)
				throw new FractronFormatException("Invalid fractron file.");

			Fractal fractal = new Fractal();

			foreach(XmlNode node in fractalNode.ChildNodes)
			{
				if(node.Name == "CameraTransform")
					readAffine2D(node, ref fractal.CameraTransform);
				else if(node.Name == "Brightness")
					readFloatElem(node, ref fractal.Brightness);
				else if(node.Name == "Gamma")
					readFloatElem(node, ref fractal.Gamma);
				else if(node.Name == "Branches")
				{
					foreach(XmlNode branchNode in node.ChildNodes)
					{
						Branch branch = readBranchNode(branchNode);
						fractal.Branches.Add(branch);
					}
				}
			}

			try{
				fractal.Name = Path.GetFileNameWithoutExtension(filename);
			}catch(ArgumentException){
				fractal.Name = "unnamed";
			}

			return fractal;
		}

		private static Branch readBranchNode(XmlNode branchNode)
		{
			float w = 0.0f;
			Branch branch = new Branch();
			branch.Variations.Clear();

			foreach(XmlNode node in branchNode.ChildNodes)
			{
				if(node.Name == "Transform")
					readAffine2D(node, ref branch.Transform);
				else if(node.Name == "Chroma")
					readVec2Elem(node, ref branch.Chroma);
				else if(node.Name == "Weight")
					readFloatElem(node, ref branch.Weight);
				else if(node.Name == "ColorWeight")
					readFloatElem(node, ref branch.ColorWeight);
				else if(node.Name == "LinearFactor")
				{
					w = 0.0f;
					readFloatElem(node, ref w);
					if(w != 0.0f)
					{
						Variation vari = Variation.GetByAttrName("linear");
						if(vari != null)
							branch.Variations.Add(new Branch.VariEntry(vari.Index, w));
					}
				}
				else if(node.Name == "SphericalFactor")
				{
					w = 0.0f;
					readFloatElem(node, ref w);
					if(w != 0.0f)
					{
						Variation vari = Variation.GetByAttrName("spherical");
						if(vari != null)
						{
							branch.Variations.Add(new Branch.VariEntry(vari.Index, w));
							branch.Localized = true;
						}
					}
				}
				else if(node.Name == "HorseshoeFactor")
				{
					w = 0.0f;
					readFloatElem(node, ref w);
					if(w != 0.0f)
					{
						Variation vari = Variation.GetByAttrName("horseshoe");
						if(vari != null)
						{
							branch.Variations.Add(new Branch.VariEntry(vari.Index, w));
							branch.Localized = true;
						}
					}
				}
				else if(node.Name == "BubbleFactor")
				{
					w = 0.0f;
					readFloatElem(node, ref w);
					if(w != 0.0f)
					{
						Variation vari = Variation.GetByAttrName("f9k_orb");
						if(vari != null)
						{
							branch.Variations.Add(new Branch.VariEntry(vari.Index, w));
							branch.Localized = true;
						}
					}
				}
				else if(node.Name == "SinusoidalFactor")
				{
					w = 0.0f;
					readFloatElem(node, ref w);
					if(w != 0.0f)
					{
						Variation vari = Variation.GetByAttrName("f9k_ripple");
						if(vari != null)
						{
							branch.Variations.Add(new Branch.VariEntry(vari.Index, w));
							branch.Localized = true;
						}
					}
				}
			}

			if(branch.Variations.Count == 0)
				branch.Variations.Add(new Branch.VariEntry(0,1.0f));
			return branch;
		}


		//tries to parse an element as a float and write it to value.
		//On failure, value remains unchanged
		private static void readFloatElem(XmlNode node, ref float value)
		{
			float newVal;
			if(float.TryParse(node.InnerText, out newVal))
				value = newVal;
		}

		private static void readAffine2D(XmlNode node, ref Affine2D value)
		{
			foreach(XmlNode child in node.ChildNodes)
			{
				if(child.Name == "XAxis")
					readVec2Elem(child, ref value.XAxis);
				else if(child.Name == "YAxis")
					readVec2Elem(child, ref value.YAxis);
				else if(child.Name == "Translation")
					readVec2Elem(child, ref value.Translation);
			}
		}

		private static void readVec2Elem(XmlNode node, ref Vec2 value)
		{
			readFloatAttr(node, "X", ref value.X);
			readFloatAttr(node, "Y", ref value.Y);
		}

		private static void readFloatAttr(XmlNode node, string attrName, ref float value)
		{
			XmlAttribute attr = node.Attributes[attrName];
			if(attr != null)
				readFloatAttr(attr, ref value);
		}
		//tries to parse an attribute as a float and write it to value.
		//On failure, value remains unchanged
		private static void readFloatAttr(XmlAttribute attr, ref float value)
		{
			float newVal;
			if(float.TryParse(attr.Value, out newVal))
				value = newVal;
		}

	}
}




