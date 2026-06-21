#region License
/*
    CudaDotNet - Compatability layer between NVidia CUDA and the .NET framework
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

namespace Cuda
{
	public class CudaException : System.Exception
	{
		private CudaResult result;
		
		public CudaResult Result{
			get{ return result; }
		}

		public CudaException(CudaResult result)
			: base("Cuda Error: " + Enum.GetName(typeof(CudaResult), (object)result))
		{
			this.result = result;
		}

		public CudaException(string message)
			: base(message)
		{
			this.result = CudaResult.Unknown;
		}
	}

	/// <summary>
	/// This exception is thrown when attempting to use a null device pointer or a null host pointer.
	/// </summary>
	public class NullPointerException : System.Exception
	{
		public NullPointerException(): base(){}
	}
}