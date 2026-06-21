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
using System.Xml.Serialization;
using System.Runtime.InteropServices;

namespace MTUtil
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Vec2
	{
		[XmlAttribute]
		public float X;
		[XmlAttribute]
		public float Y;
		
		public Vec2(float x, float y)
		{
			X = x;
			Y = y;
		}
		
		public bool IsZero(){ return X==0.0f && Y==0.0f; }
	
		public bool IsFinite()
		{
			return 
				!float.IsInfinity(X) && !float.IsNaN(X) &&
				!float.IsInfinity(Y) && !float.IsNaN(Y);
		}
		
		public float LengthSq{
			get{ return X*X + Y*Y; }
		}
		public float Length{
			get{ return (float)Math.Sqrt(X*X + Y*Y); }
		}
		public void Normalize(){ this /= Length; }
		public Vec2 GetNormal(){ return this / Length;}
	
		public static float Dot (Vec2 a, Vec2 b){ return a.X*b.X + a.Y*b.Y; }
		public static float Dist(Vec2 a, Vec2 b){ return (a-b).Length; }
		public static Vec2 Lerp(Vec2 a, Vec2 b, float alpha)
		{
			return new Vec2( a.X + (alpha*(b.X-a.X)),
			                 a.Y + (alpha*(b.Y-a.Y)));
		}
		
		public static Vec2 operator-(Vec2 a){ return new Vec2(-a.X, -a.Y); }
		
		public static Vec2 operator+(Vec2 a  , Vec2 b  ){ return new Vec2(a.X+b.X, a.Y+b.Y); }
		public static Vec2 operator-(Vec2 a  , Vec2 b  ){ return new Vec2(a.X-b.X, a.Y-b.Y); }
		public static Vec2 operator*(Vec2 a  , float b ){ return new Vec2(a.X*b, a.Y*b); }
		public static Vec2 operator*(float a , Vec2 b  ){ return new Vec2(a*b.X, a*b.Y); }
		public static Vec2 operator*(Vec2 a  , Vec2 b  ){ return new Vec2(a.X*b.X, a.Y*b.Y); }
		public static Vec2 operator/(Vec2 a  , float b ){ return new Vec2(a.X/b, a.Y/b); }

		//I wish I knew a faster way to do this
		public void SnapToNormalizedAngle(int divisions)
		{
			Vec2 best = new Vec2(1.0f, 0.0f);
			Vec2 c;
			float bestRsq = float.PositiveInfinity;
			float rsq, theta;

			for(int i = 0; i < divisions; i++)
			{
				theta = 2.0f * Util.PI * (float)i / (float)divisions;
				c.X = (float)Math.Cos(theta);
				c.Y = (float)Math.Sin(theta);
				rsq = (this-c).LengthSq;
				if(rsq < bestRsq)
				{
					best = c;
					bestRsq = rsq;
				}
			}
			this = best;
		}

	}
}
