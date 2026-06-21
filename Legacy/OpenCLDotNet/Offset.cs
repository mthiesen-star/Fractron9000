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
	/// Represents 2D Offsets
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Offset2D
	{
		public Offset2D(IntPtr x, IntPtr y)
		{
			this.X = x;
			this.Y = y;
		}
		public Offset2D(int x, int y)
		{
			this.X = (IntPtr)x;
			this.Y = (IntPtr)y;
		}
		public IntPtr X;
		public IntPtr Y;

		public static Offset2D Zero{
			get{ return new Offset2D(IntPtr.Zero,IntPtr.Zero); }
		}
	}

	/// <summary>
	/// Represents 3D Offsets
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct Offset3D
	{
		public Offset3D(IntPtr x, IntPtr y, IntPtr z)
		{
			this.X = x;
			this.Y = y;
			this.Z = z;
		}
		public Offset3D(int x, int y, int z)
		{
			this.X = (IntPtr)x;
			this.Y = (IntPtr)y;
			this.Z = (IntPtr)z;
		}
		public IntPtr X;
		public IntPtr Y;
		public IntPtr Z;

		public static Offset3D Zero{
			get{ return new Offset3D(IntPtr.Zero,IntPtr.Zero, IntPtr.Zero); }
		}
	}
}
