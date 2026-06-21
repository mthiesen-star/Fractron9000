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
using System.Diagnostics;

namespace OpenCL
{
	abstract public class CLObject
	{
		protected IntPtr handle;
		public IntPtr Handle{
			get{ return handle; }
		}

		public CLObject(IntPtr handle)
		{
			this.handle = handle;
		}

		public static IntPtr[] GetHandles(CLObject[] objs)
		{
			if(objs == null)
				return null;
			IntPtr[] result = new IntPtr[objs.Length];
			for(int i = 0; i < result.Length; i++)
				result[i] = objs[i].Handle;
			return result;
		}
	}

	/// <summary>
	/// Represents an OpenCL resource that must be disposed of when no longer needed.
	/// </summary>
	abstract public class CLResource : CLObject, IDisposable
	{
		public CLResource(IntPtr handle) : base(handle){}
		~CLResource()
		{
			try{
				Release();
			}
			catch(Exception ex){
				string message = String.Format("{0}.Release() failed with {1}:\n{2}", this.GetType().Name, ex.GetType().Name, ex.Message);
				Trace.WriteLine(message, "warning");
			}
			this.handle = IntPtr.Zero;
		}

		public bool Disposed{
			get{ return handle == IntPtr.Zero; }
		}

		public void Dispose()
		{
			Release();
			GC.SuppressFinalize(this);
			this.handle = IntPtr.Zero;
		}

		abstract protected void Release();
	}
}
