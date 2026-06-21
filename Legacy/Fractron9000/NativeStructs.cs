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
using System.Diagnostics;
using System.Runtime.InteropServices;

using MTUtil;

namespace Fractron9000
{
	/// <summary>
	/// Device compatable fractal description
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct NativeFractal
	{
		public const int MaxBranches = 16;
		public const int MaxVariations = 48;

		public uint     BranchCount;
		public float    Brightness;
		public float    InvGamma;
		public float    Vibrancy;
		public Vec4     BgColor;
		public Affine2D VpsTransform;
		public float    Reserved0;
		public float    Reserved1;
	}

	/// <summary>
	/// Device compatable branch description
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct NativeBranch
	{
		public uint     NormWeight;
		public float    ColorWeight;
		public Vec2     Chroma;
		public Affine2D PreTransform;
		public Affine2D PostTransform;
	}

	/// <summary>
	/// Device compatable entry for per-iterator statistics
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct NativeIterStatEntry
	{
		public UInt64 DotCount;
		public float  PeakDensity;
		public float  Reserved0;
	}

	/// <summary>
	/// Device compatable entry for global statistics
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct NativeGlobalStatEntry
	{
		public UInt64 IterCount;
		public UInt64 DotCount;
		public float  Density;
		public float  PeakDensity;
		public float  ScaleConstant;
		public float  Reserved0;
	}

	/// <summary>
	/// Device compatable representation of a dot to be drawn to the accumulation buffer
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Dot
	{
		public Dot(Vec2 pos, Vec2 chroma){
			this.Pos = pos;
			this.Chroma = chroma;
		}
		public Vec2 Pos;
		public Vec2 Chroma;
	}
}
