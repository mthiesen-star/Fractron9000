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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace OpenCL
{
	public class CommandQueue : CLResource
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new CommandQueue. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged queue handle.</param>
		public CommandQueue(IntPtr handle) : base(handle) {}

		public static CommandQueue Create(Context context, Device device, CommandQueueFlags flags)
		{
			IntPtr handle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;
			unsafe{
				handle = Native.CreateCommandQueue(context.Handle, device.Handle, flags, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
			return new CommandQueue(handle);
		}

		protected override void Release()
		{
			Native.Call(Native.ReleaseCommandQueue(this.Handle));
		}
		
		public void Retain()
		{
			Native.Call(Native.RetainCommandQueue(this.Handle));
		}
		#endregion

		#region Properies
		public CommandQueueFlags Flags{
			get{
				CommandQueueFlags result;
				unsafe{
					Native.Call(Native.SetCommandQueueProperty(this.Handle, (CommandQueueFlags)0, true, &result));
				}
				return result;
			}
		}
		#endregion

		public void EnqueueMarker(out Event evt)
		{
			IntPtr evtHandle = IntPtr.Zero;
			unsafe{
				Native.Call(Native.EnqueueMarker(this.Handle, &evtHandle));
			}
			evt = new Event(evtHandle);
		}

		public void EnqueueWaitForEvents(Event[] waitList)
		{
			unsafe{
				uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
				IntPtr[] wlh = CLObject.GetHandles(waitList);
				fixed(IntPtr* p_wlh = wlh)
				{
					Native.Call(Native.EnqueueWaitForEvents(this.Handle, waitCount, p_wlh));
				}
			}
		}

		public void EnqueueBarrier()
		{
			Native.Call(Native.EnqueueBarrier(this.Handle));
		}

		public void Flush()
		{
			Native.Call(Native.Flush(this.Handle));
		}

		public void Finish()
		{
			Native.Call(Native.Finish(this.Handle));
		}

		public void SetProperty(CommandQueueFlags flags, bool enable)
		{
			unsafe{
				Native.Call(Native.SetCommandQueueProperty(this.Handle, flags, enable, null));
			}
		}
	}
}