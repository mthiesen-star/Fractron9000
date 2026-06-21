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
	public enum EventQueryResult{ Unfinished, Finished, NotStarted };
	public class Event
	{
		IntPtr handle = IntPtr.Zero;

		internal IntPtr Handle{
			get{ return handle; }
		}

		public Event()
		{
			CudaUtil.Call(cuEventCreate(out handle, 0));
		}

		~Event()
		{
			if(handle != IntPtr.Zero)
			{
				cuEventDestroy(handle);
				handle = IntPtr.Zero;
			}
		}

		public void Record()
		{
			CudaUtil.Call(cuEventRecord(handle, IntPtr.Zero));
		}

		public void Record(Stream stream)
		{
			CudaUtil.Call(cuEventRecord(handle, stream.Handle));
		}

		public EventQueryResult Query()
		{
			CudaResult result = cuEventQuery(handle);
			switch(result)
			{
				case CudaResult.NotReady:
					return EventQueryResult.Unfinished;
				case CudaResult.Success:
					return EventQueryResult.Finished;
				case CudaResult.InvalidValue:
					return EventQueryResult.NotStarted;
				default:
					throw new CudaException(result);
			}
		}

		public void Synchronize()
		{
			CudaResult result = cuEventSynchronize(handle);
			switch(result)
			{
				case CudaResult.Success:
					return;
				case CudaResult.InvalidValue:
					return;
				default:
					throw new CudaException(result);
			}
		}

		public float TimeSince(Event prev)
		{
			return ElapsedTime(prev, this);
		}

		/// <summary>
		/// Computes the elapsed time in seconds between two events
		/// </summary>
		/// <param name="start">The starting event</param>
		/// <param name="stop">The stoping event</param>
		/// <returns>The number of seconds between the two events, or NaN if either event has not been recored.</returns>
		public static float ElapsedTime(Event start, Event stop)
		{
			float time_ms;
			CudaResult result = cuEventElapsedTime(out time_ms, start.handle, stop.handle);
			switch(result)
			{
				case CudaResult.Success:
					return time_ms / 1000.0f;
				case CudaResult.InvalidValue:
					return float.NaN;
				default:
					throw new CudaException(result);
			}
		}


        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuEventCreate(out IntPtr evt, UInt32 flags);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuEventDestroy(IntPtr evt);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuEventElapsedTime(out float time, IntPtr start, IntPtr end);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuEventQuery(IntPtr evt);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuEventRecord(IntPtr evt, IntPtr stream);
        [DllImport("nvcuda.dll")]
		private static extern CudaResult cuEventSynchronize(IntPtr evt); 
	}
}
