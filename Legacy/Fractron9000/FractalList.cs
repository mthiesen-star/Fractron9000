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
	public class FractalList : List<Fractal>
	{
		public FractalList() : base() {}

		public void AddByName(Fractal fractal)
		{
			if(fractal.Name == null || fractal.Name == "")
				throw new ArgumentException("The fractal must have a name");

			int idx = GetIndexByName(fractal.Name);
			if(idx < 0 || idx >= this.Count)
				this.Add(fractal);
			else
				this[idx] = fractal;
		}

		//gets the first fractal with the given name
		public Fractal FindByName(string name)
		{
			int idx = GetIndexByName(name);
			if(idx >= 0)
				return this[idx];
			else
				return null;
		}

		public int GetIndexByName(string name)
		{
			if(name == null || name == "")
				return -1;

			for(int i = 0; i < Count; i++)
				if(this[i].Name == name)
					return i;
			return -1;
		}

		public bool ContainsNonFractronFractals()
		{
			for(int i = 0; i < Count; i++)
				if(!this[i].IsFromFractron())
					return true;
			return false;
		}

		public void ConvertNonFractronFlames()
		{
			for(int i = 0; i < Count; i++)
				if(!this[i].IsFromFractron())
					this[i].MarkVersionAsFractron();
		}

	}
}
