#region License
/*
    MTUtil - Miscellaneous utility classes 
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
using System.Collections.Generic;
using System.Text;

namespace MTUtil.Extensions
{
#if _TARGET_DOTNET_3_5_
	public static class MathExtensions
	{
		/// <summary>
		/// Returns a value clamped to the range [min..max]
		/// </summary>
		public static int Clamp(this int val, int min, int max)
		{
			return ( min <= val ? (val <= max ? val : max ) : min );
		}

		/// <summary>
		/// Returns a value clamped to the range [0..1]
		/// </summary>
		public static float Clamp(this float val)
		{
			return ( 0 <= val ? (val < 1 ? val : 1 ) : 0 );
		}

		/// <summary>
		/// Returns a value clamped to the range [min..max]
		/// </summary>
		public static float Clamp(this float val, float min, float max)
		{
			return ( min <= val ? (val < max ? val : max ) : min );
		}

		/// <summary>
		/// Returns a value clamped to the range [0..1]
		/// </summary>
		public static double Clamp(this double val)
		{
			return ( 0 <= val ? (val < 1 ? val : 1 ) : 0 );
		}

		/// <summary>
		/// Returns a value clamped the range [min..max]
		/// </summary>
		public static double Clamp(this double val, double min, double max)
		{
			return ( min <= val ? (val < max ? val : max ) : min );
		}
	}
#endif
}
