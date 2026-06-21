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
using System.Drawing;

namespace MTUtil
{
	public static class Util
	{
		public const float HalfPI = 1.5707963267948966192313216916398f;
		public const float PI = 3.1415926535897932384626433832795f;
		public const float _2_PI = 6.283185307179586476925286766559f;
		public const float Root_2_PI = 2.506628274631000502415765284811f;
		public static Random Rand = new Random();

		public static float NormPDF( float val )
		{
			return (float)Math.Exp( -0.5f * (val*val) ) / Root_2_PI;
		}

		public static float NormPDF( float val, float mean, float stdDev )
		{
			float diff = val-mean;
			return (float)Math.Exp( -0.5f * (diff*diff)/(stdDev*stdDev) ) / (stdDev * Root_2_PI);
		}

		public static float NormPDF( float x, float y )
		{
			return (float)Math.Exp( -0.5f * ((x*x)+(y*y))) / _2_PI;
		}

		//returns value, but clamped to the range min..max
		public static int Clamp( int val, int min, int max )
		{
			return ( min <= val ? (val <= max ? val : max ) : min );
		}

		//returns value, but clamped to the range 0..1
		public static float Clamp( float val )
		{
			return ( 0 <= val ? (val < 1 ? val : 1 ) : 0 );
		}

		//returns value, but clamped to the range min..max
		public static float Clamp( float val, float min, float max )
		{
			return ( min <= val ? (val < max ? val : max ) : min );
		}

		//returns value, but clamped to the range 0..1
		public static double Clamp( double val )
		{
			return ( 0 <= val ? (val < 1 ? val : 1 ) : 0 );
		}

		//returns value, but clamped to the range min..max
		public static double Clamp( double val, double min, double max )
		{
			return ( min <= val ? (val < max ? val : max ) : min );
		}

		//returns value, but clamped to the range min..max-1
		public static int IClamp( int val, int min, int max )
		{
			return ( min < val ? (val < max ? val : max-1 ) : min );
		}


		//returns value, but clamped to the range 0..255
		public static int ClampByte(int val)
		{
			return ( 0 <= val ? (val <= 255 ? val : 255 ) : 0 );
		}

		public static float Lerp( float n1, float n2, float a )
		{
			return n1 + a * ( n2 - n1 );
		}

		public static float SCurve( float p )
		{
			return( p*p*(3 - 2*p) );
		}

		public static double SnapToGrid(double n, double gridStep)
		{
			return Math.Round(n/gridStep) * gridStep;
		}
		public static float SnapToGrid(float n, float gridStep)
		{
			return (float)Math.Round(n/gridStep) * gridStep;
		}
		public static Vec2 SnapToGrid(Vec2 n, float gridStep)
		{
			return new Vec2(
				(float)Math.Round(n.X/gridStep) * gridStep,
				(float)Math.Round(n.Y/gridStep) * gridStep );
		}

		//returns random number >= 0 and < max
		public static int RandInt( int max )
		{
			return Rand.Next( max );
		}

		public static int RandInt( int min, int max )
		{
			return Rand.Next( min, max );
		}

		//returns random float >= 0 and < 1.0
		public static float RandFloat()
		{
			return (float)Rand.NextDouble();
		}

		public static float RandFloat( float min, float max )
		{
			return min + (max-min)*(float)Rand.NextDouble();
		}
		
		//returns -1 to 1, with 0 being the most likely
		public static float RandPeak()
		{
			return (float)Rand.NextDouble() + (float)Rand.NextDouble() - 1.0f;
		}

		public delegate T GenerateElementDelegate<T>(int index);

		public static T[] GenerateArray<T>(int length, GenerateElementDelegate<T> elemFunc)
		{
			T[] result = new T[length];
			for(int i = 0; i < result.Length; i++)
				result[i] = elemFunc(i);
			return result;
		}

		public static Vec4 Vec4FromColor(Color c)
		{
			return new Vec4(
				(float)c.R / 255.0f,
				(float)c.G / 255.0f,
				(float)c.B / 255.0f,
				(float)c.A / 255.0f);
		}

		public static Color ColorFromVec4(Vec4 v)
		{
			return Color.FromArgb(
				Util.Clamp((int)(v.W*255.0f), 0, 255),
				Util.Clamp((int)(v.X*255.0f), 0, 255),
				Util.Clamp((int)(v.Y*255.0f), 0, 255),
				Util.Clamp((int)(v.Z*255.0f), 0, 255));
		}

		public static void DisposeAndNullify<T>(ref T obj) where T : class,IDisposable
		{
			if(obj != null)
			{
				obj.Dispose();
				obj = null;
			}
		}

		public static string HumanReadableNumber(double number)
		{
			if(number >= 1000000000000.0)
				return (number / 1000000000000.0).ToString("f2") + " Trillion";
			else if(number >= 1000000000.0)
				return (number / 1000000000.0).ToString("f2") + " Billion";
			else if(number >= 1000000.0)
				return (number / 1000000.0).ToString("f2") + " Million";
			else if(number >= 1000.0)
				return ((int)number).ToString();
			else
				return number.ToString("f4");
		}
	}
}
