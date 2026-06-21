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
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using MTUtil;

namespace Fractron9000
{
	public class Variation
	{
		private static Variation[] variations = new Variation[]{
			new Variation(0,  "Linear", "linear"),
			new Variation(1,  "Sinusoidal", "sinusoidal"),
			new Variation(2,  "Spherical", "spherical"),
			new Variation(3,  "Swirl", "swirl"),
			new Variation(4,  "Horseshoe", "horseshoe"),
			new Variation(5,  "Polar", "polar"),
			new Variation(6,  "Handkerchief", "handkerchief"),
			new Variation(7,  "Heart", "heart"),
			new Variation(8,  "Disc", "disc"),
			new Variation(9,  "Spiral", "spiral"),
			new Variation(10, "Hyperbolic", "hyperbolic"),
			new Variation(11, "Diamond", "diamond"),
			new Variation(12, "Ex", "ex"),
			new Variation(13, "Julia", "julia"),
			new Variation(14, "Bent", "bent"),
			new Variation(15, "Waves", "waves"),
			new Variation(16, "Fisheye", "fisheye"),
			new Variation(17, "Popcorn", "popcorn"),
			new Variation(18, "Exponential", "exponential"),
			new Variation(19, "Power", "power"),
			new Variation(20, "Cosine", "cosine"),
			new Variation(21, "Eyefish", "eyefish"),
			new Variation(22, "Bubble", "bubble"),
			new Variation(23, "Cylinder", "cylinder"),
			new Variation(24, "Noise", "noise"),
			new Variation(25, "Blur", "blur"),
			new Variation(26, "Gaussian Blur", "gaussian_blur"),
			new Variation(27, "Orb 9000", "f9k_orb"),
			new Variation(28, "Ripple 9000", "f9k_ripple"),
			new Variation(29, "Bulge 9000", "f9k_bulge")
		};

		public static Variation[] Variations{
			get{ return variations; }
		}

		public static int Count{
			get{ return variations.Length; }
		}

		public static Variation GetByAttrName(string attrName)
		{
			for(int i = 0; i < variations.Length; i++)
				if(variations[i].AttrName == attrName)
					return variations[i];
			return null;
		}

		private int index;
		public int Index{
			get{ return index; }
		}
		private string name;
		public string Name{
			get{ return name; }
		}

		private string attrName;
		public string AttrName{
			get{ return attrName; }
		}

		private Variation(int index, string name, string attrName)
		{
			this.index = index;
			this.name = name;
			this.attrName = attrName;
		}

		public override string ToString()
		{
			return name;
		}
	}
}