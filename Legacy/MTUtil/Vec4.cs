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
	public struct Vec4
	{
		[XmlAttribute]
		public float X;
		[XmlAttribute]
		public float Y;
		[XmlAttribute]
		public float Z;
		[XmlAttribute]
		public float W;
		
		public Vec4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
		
		public bool IsZero(){ return X==0.0f && Y==0.0f && Z==0.0f && W==0.0f; }
	
		public bool IsFinite()
		{
			return 
				!float.IsInfinity(X) && !float.IsNaN(X) &&
				!float.IsInfinity(Y) && !float.IsNaN(Y) &&
				!float.IsInfinity(Z) && !float.IsNaN(Z) &&
				!float.IsInfinity(W) && !float.IsNaN(W);
		}
		
		public float LengthSq{
			get{ return X*X + Y*Y + Z*Z + W*W; }
		}
		public float Length{
			get{ return (float)Math.Sqrt(X*X + Y*Y + Z*Z + W*W); }
		}
		public void Normalize(){ this /= Length; }
		public Vec4 GetNormal(){ return this / Length;}
	
		public static float Dot (Vec4 a, Vec4 b){ return a.X*b.X + a.Y*b.Y + a.Z*b.Z + a.W*b.W; }
		public static float Dist(Vec4 a, Vec4 b){ return (a-b).Length; }
		public static Vec4 Lerp(Vec4 a, Vec4 b, float alpha)
		{
			return new Vec4(
				a.X + (alpha*(b.X-a.X)),
				a.Y + (alpha*(b.Y-a.Y)),
				a.Z + (alpha*(b.Z-a.Z)),
				a.W + (alpha*(b.W-a.W)));
		}
		
		public static Vec4 operator-(Vec4 a){ return new Vec4(-a.X, -a.Y, -a.Z, -a.W); }
		
		public static Vec4 operator+(Vec4 a  , Vec4 b  ){ return new Vec4(a.X+b.X, a.Y+b.Y, a.Z+b.Z, a.W+b.W); }
		public static Vec4 operator-(Vec4 a  , Vec4 b  ){ return new Vec4(a.X-b.X, a.Y-b.Y, a.Z-b.Z, a.W-b.W); }
		public static Vec4 operator*(Vec4 a  , float b ){ return new Vec4(a.X*b, a.Y*b, a.Z*b, a.W*b); }
		public static Vec4 operator*(float a , Vec4 b  ){ return new Vec4(a*b.X, a*b.Y, a*b.Z, a*b.W); }
		public static Vec4 operator*(Vec4 a  , Vec4 b  ){ return new Vec4(a.X*b.X, a.Y*b.Y, a.Y*b.Z, a.Y*b.W); }
		public static Vec4 operator/(Vec4 a  , float b ){ return new Vec4(a.X/b, a.Y/b, a.Z/b, a.W/b); }
	}
}
