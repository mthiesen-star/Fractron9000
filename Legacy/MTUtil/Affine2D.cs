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
using System.Runtime.InteropServices;
using System.Xml.Serialization;

namespace MTUtil
{
	[StructLayout(LayoutKind.Sequential)]
	public struct Affine2D
	{
		public Vec2 XAxis; //X Axis
		public Vec2 YAxis; //Y Axis
		public Vec2 Translation; //Translation
		
		#region Accessors
		//these are for compatability with the common a,b,c,d,e,f representation of an affine transformation, expressed as the following matrix:
		// [ A  B  C ]
		// [ D  E  F ]
		[XmlIgnore]
		public float A{
			get{ return XAxis.X; }
			set{ XAxis.X = value; }
		}
		[XmlIgnore]
		public float B{
			get{ return YAxis.X; }
			set{ YAxis.X = value; }
		}
		[XmlIgnore]
		public float C{
			get{ return Translation.X; }
			set{ Translation.X = value; }
		}
		[XmlIgnore]
		public float D{
			get{ return XAxis.Y; }
			set{ XAxis.Y = value; }
		}
		[XmlIgnore]
		public float E{
			get{ return YAxis.Y; }
			set{ YAxis.Y = value; }
		}
		[XmlIgnore]
		public float F{
			get{ return Translation.Y; }
			set{ Translation.Y = value; }
		}
		#endregion

		#region Constructors
		public Affine2D(float xx, float xy, float yx, float yy, float tx, float ty)
		{
			XAxis.X = xx;
			XAxis.Y = xy;
			YAxis.X = yx;
			YAxis.Y = yy;
			Translation.X = tx;
			Translation.Y = ty;
		}
		
		public Affine2D(Vec2 xAxis, Vec2 yAxis, Vec2 translation)
		{
			XAxis = xAxis;
			YAxis = yAxis;
			Translation = translation;
		}
		#endregion

		public override string ToString()
		{
			return String.Format(
				"[({0:0.00},{1:0.00}), ({2:0.00},{3:0.00}), ({4:0.00},{5:0.00})]",
				XAxis.X, XAxis.Y, YAxis.X, YAxis.Y, Translation.X, Translation.Y );
		}

		public static Affine2D Identity{
			get{ return new Affine2D(new Vec2(1.0f,0.0f), new Vec2(0.0f,1.0f), new Vec2(0.0f,0.0f)); }
		}

		[XmlIgnore]
		public Affine2D Inverse{
			get{
				float det = XAxis.X * YAxis.Y - XAxis.Y * YAxis.X;
				return new Affine2D(
					 YAxis.Y / det, -XAxis.Y / det,
					-YAxis.X / det,  XAxis.X / det,
					 (Translation.Y * YAxis.X - Translation.X * YAxis.Y) / det, (Translation.X * XAxis.Y - Translation.Y * XAxis.X) / det);
			}
		}

		public void Translate(Vec2 v)
		{
			Translation += v;
		}

		public void RotateScaleXTo(Vec2 v)
		{
			//float a,c;
			///calcRSAC(out a, out c, X, v);

			//Gaaaaccch!!
			//Work you stupid math!!!!!!!!!!!!
			//X.X = a*X.X - c*X.Y;
			//X.Y = c*X.X + a*X.Y;
			//Y.X = a*Y.X - c*Y.Y;
			//Y.Y = c*Y.X + a*Y.Y;


			//oh well, using the slow way instead....
			Affine2D rs = CalcRotateScale(XAxis, v);

			XAxis = rs.TransformNormal(XAxis);
			YAxis = rs.TransformNormal(YAxis);
		}

		public void RotateScaleYTo(Vec2 v)
		{
			Affine2D rs = CalcRotateScale(YAxis, v);

			XAxis = rs.TransformNormal(XAxis);
			YAxis = rs.TransformNormal(YAxis);
		}

		public static Affine2D CalcRotateScale(Vec2 from, Vec2 to)
		{
			float a,c;
			calcRSAC(out a, out c, from, to);
			return new Affine2D( a, c, -c, a, 0.0f, 0.0f );
		}

		private static void calcRSAC(out float a, out float c, Vec2 from, Vec2 to)
		{
			float lsq = from.LengthSq;
			a = (from.Y*to.Y + from.X*to.X)/lsq;
			c = (from.X*to.Y - from.Y*to.X)/lsq;
		}

		
		public Vec2 TransformVector(Vec2 v)
		{
			return new Vec2(XAxis.X*v.X + YAxis.X*v.Y + Translation.X,
			                XAxis.Y*v.X + YAxis.Y*v.Y + Translation.Y);
		}
		public Vec2 TransformNormal(Vec2 v)
		{
			return new Vec2(XAxis.X*v.X + YAxis.X*v.Y,
			                XAxis.Y*v.X + YAxis.Y*v.Y);
		}

		//returns true if all values differ from a by less than or equal to epsilon
		public bool Equals(Affine2D a, float epsilon)
		{
			return Math.Abs(XAxis.X - a.XAxis.X) <= epsilon &&
				Math.Abs(XAxis.Y - a.XAxis.Y) <= epsilon &&
				Math.Abs(YAxis.X - a.YAxis.X) <= epsilon &&
				Math.Abs(YAxis.Y - a.YAxis.Y) <= epsilon &&
				Math.Abs(Translation.X - a.Translation.X) <= epsilon &&
				Math.Abs(Translation.Y - a.Translation.Y) <= epsilon;
		}

		public static Affine2D Lerp(Affine2D l, Affine2D r, float alpha)
		{
			return new Affine2D(
				Vec2.Lerp(l.XAxis, r.XAxis, alpha),
				Vec2.Lerp(l.YAxis, r.YAxis, alpha),
				Vec2.Lerp(l.Translation, r.Translation, alpha));
		}

		#region Operators
		public static Affine2D operator *(Affine2D l, Affine2D r)
		{
			Affine2D result = new Affine2D();
			result.XAxis = l.TransformNormal(r.XAxis);
			result.YAxis = l.TransformNormal(r.YAxis);
			result.Translation = l.TransformVector(r.Translation);
			return result;
		}
		
		//Multiply all elements by a scalar
		public static Affine2D operator *(Affine2D l, float v)
		{
			Affine2D result = new Affine2D();
			result.XAxis = l.XAxis * v;
			result.YAxis = l.YAxis * v;
			result.Translation = l.Translation * v;
			return result;
		}
		
		public static Vec2 operator *(Affine2D l, Vec2 r)
		{
			return l.TransformVector(r);
		}
		#endregion

	}
}
