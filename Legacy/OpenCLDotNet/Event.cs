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
	public class Event : CLResource, IHasInfo<EventInfo>, IHasInfo<ProfilingInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new Event. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged event handle.</param>
		public Event(IntPtr handle) : base(handle) {}

		protected override void Release()
		{
			Native.Call(Native.ReleaseEvent(this.Handle));
		}

		public void Retain()
		{
			Native.Call(Native.RetainEvent(this.Handle));
		}
		#endregion
	
		#region Properties
		public CommandType CommandType{
			get{ return (CommandType)Info.Get_int<EventInfo>(this, EventInfo.CommandType); }
		}

		public CommandExecutionStatus ExecutionStatus
		{
			get{
				int status = Info.Get_int<EventInfo>(this, EventInfo.CommandExecutionStatus);
				if(status < 0)
					return CommandExecutionStatus.Error;
				else
					return (CommandExecutionStatus)status;
			}
		}

		public bool Complete{
			get{ return ExecutionStatus == CommandExecutionStatus.Complete; }
		}

		
		public uint ReferenceCount{
			get{ return Info.Get_uint<EventInfo>(this, EventInfo.ReferenceCount); }
		}

		/* //broken :P
		public CommandQueue Queue{
			get{
				IntPtr queueHandle = Info.Get_IntPtr(this, EventInfo.CommandQueue);
				CommandQueue result = new CommandQueue(queueHandle);
				//result.Retain();
				return result;
			}
		}*/

		/// <summary>
		/// Gets the device time in nano-seconds when this event's command was enqueued by the host. The queue must have the ProfilingEnable flag set.
		/// </summary>
		public ulong ProfilingCommandQueued{
			get{ return Info.Get_ulong<ProfilingInfo>(this, ProfilingInfo.CommandQueued); }
		}
		/// <summary>
		/// Gets the device time in nano-seconds when this event's command was submitted to the device. The queue must have the ProfilingEnable flag set.
		/// </summary>
		public ulong ProfilingCommandSubmit{
			get{ return Info.Get_ulong<ProfilingInfo>(this, ProfilingInfo.CommandSubmit); }
		}
		/// <summary>
		/// Gets the device time in nano-seconds when this event's command started. The queue must have the ProfilingEnable flag set.
		/// </summary>
		public ulong ProfilingCommandStart{
			get{ return Info.Get_ulong<ProfilingInfo>(this, ProfilingInfo.CommandStart); }
		}
		/// <summary>
		/// Gets the device time in nano-seconds when this event's command finished. The queue must have the ProfilingEnable flag set.
		/// </summary>
		public ulong ProfilingCommandEnd{
			get{ return Info.Get_ulong<ProfilingInfo>(this, ProfilingInfo.CommandEnd); }
		}


		#endregion

		#region IHasInfo<EventInfo> Members
		unsafe ErrorCode IHasInfo<EventInfo>.GetInfo(EventInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetEventInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		#region IHasInfo<ProfilingInfo> Members
		unsafe ErrorCode IHasInfo<ProfilingInfo>.GetInfo(ProfilingInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetEventProfilingInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		/// <summary>
		/// Blocks execution until the command identified by this event is complete.
		/// </summary>
		public void Wait()
		{
			unsafe{
				IntPtr tempHandle = this.Handle;
				Native.Call(Native.WaitForEvents(1, &tempHandle));
			}
		}


	}
}