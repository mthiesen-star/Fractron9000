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
using System.Runtime.InteropServices;
using System.Text;
using System.Reflection;
using System.IO;

namespace Cuda
{
	public enum StreamQueryResult{ Unfinished, Finished };
	public class Stream
	{
		IntPtr handle = IntPtr.Zero;

		internal IntPtr Handle{
			get{ return handle; }
		}

		public Stream()
		{
			CudaUtil.Call(cuStreamCreate(out handle, 0));
		}

		~Stream()
		{
			if(handle != IntPtr.Zero)
			{
				cuStreamDestroy(handle);
				handle = IntPtr.Zero;
			}
		}

		public StreamQueryResult Query()
		{
			CudaResult result = cuStreamQuery(handle);
			switch(result)
			{
				case CudaResult.NotReady:
					return StreamQueryResult.Unfinished;
				case CudaResult.Success:
					return StreamQueryResult.Finished;
				default:
					throw new CudaException(result);
			}
		}

		public void Synchronize()
		{
			CudaUtil.Call(cuStreamSynchronize(handle));
		}

        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuStreamCreate(out IntPtr hStream, UInt32 flags);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuStreamDestroy(IntPtr hStream);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuStreamQuery(IntPtr hStream);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuStreamSynchronize(IntPtr hStream);
	}
}