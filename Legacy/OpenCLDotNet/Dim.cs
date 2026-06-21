#region License
/*
    OpenCLDotNet - Compatability layer between OpenCL and the .NET framework
    Copyright (C) 2010 Michael J. Thiesen
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
using System.Text;

namespace OpenCL
{
	/// <summary>
	/// Represents 2D Dimensions
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Dim2D
	{
		public Dim2D(IntPtr x, IntPtr y)
		{
			this.X = x;
			this.Y = y;
		}
		public Dim2D(int x, int y)
		{
			this.X = (IntPtr)x;
			this.Y = (IntPtr)y;
		}
		public IntPtr X;
		public IntPtr Y;
	}

	/// <summary>
	/// Represents 3D Dimensions
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Dim3D
	{
		public Dim3D(IntPtr x, IntPtr y, IntPtr z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}
		public Dim3D(int x, int y, int z)
		{
			this.X = (IntPtr)x;
			this.Y = (IntPtr)y;
			this.Z = (IntPtr)z;
		}
		public IntPtr X;
		public IntPtr Y;
		public IntPtr Z;
	}
}
