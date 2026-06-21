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

namespace MTUtil
{
	//keeps a running average of a changing value
	public class MovingAverage
	{
		private double[] pool;
		private double sum;
		private int count;

		public MovingAverage(int size)
		{
			pool = new double[size];
			Reset();
		}

		public void AddValue(double value)
		{
			int i = count%pool.Length;
			sum -= pool[i];
			pool[i] = value;
			sum += value;
			count++;
		}

		public void Reset()
		{
			for(int i = 0; i < pool.Length; i++)
				pool[i] = 0.0;
			sum = 0.0;
			count = 0;
		}

		public int Count{
			get{ return count; }
		}

		public double Average{
			get{
				if(count == 0) return 0.0;
				if(count < pool.Length) return sum / (double)count;
				return sum / (double)pool.Length;
			}
		}

	}
}
