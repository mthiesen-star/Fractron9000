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

namespace MTUtil
{
	public class FastRand
	{
		private UInt32 low;
		private UInt32 high;
		
		public FastRand() : this(0xDEADBEEF)
		{
		}
		
		public FastRand(UInt32 seed)
		{
			high = seed;
			low = high ^ 0x49616E42;
		}
		
		public UInt32 NextUInt()
		{
			unchecked
			{
				high = (high<<16) | (high>>16);
				high += low;
				low += high;
			}
			return high;
		}
		
		public int NextInt(int uppperBound)
		{
			return (int)(NextUInt() % (UInt32)uppperBound);
		}
		
		public double NextDouble()
		{
			return (double)NextUInt() / 0x100000000;
		}
		
		public float NextFloat()
		{
			return (float)(NextUInt() & 0x007FFFFF) / (float)0x00800000;
		}
	}
}