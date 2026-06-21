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
	abstract public class MemObject : CLResource, IHasInfo<MemInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new MemObject. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged memory object handle.</param>
		public MemObject(IntPtr handle) : base(handle) {}

		protected override void Release()
		{
			Native.Call(Native.ReleaseMemObject(handle));
		}
		
		public void Retain()
		{
			Native.Call(Native.RetainMemObject(this.Handle));
		}
		#endregion

		#region Properies
		public MemObjectType MemObjectType{
			get{ return (MemObjectType)Info.Get_int<MemInfo>(this, MemInfo.Type); }
		}

		public MemFlags Flags{
			get{ return (MemFlags)Info.Get_long<MemInfo>(this, MemInfo.Flags); }
		}

		public IntPtr Size{
			get{ return Info.Get_IntPtr<MemInfo>(this, MemInfo.Size); }
		}

		public uint MapCount{
			get{ return Info.Get_uint<MemInfo>(this, MemInfo.MapCount); }
		}

		public uint ReferenceCount{
			get{ return Info.Get_uint<MemInfo>(this, MemInfo.ReferenceCount); }
		}

		public Context Context{
			get{
				IntPtr ctxHandle = Info.Get_IntPtr<MemInfo>(this, MemInfo.Context);
				Context result = new Context(ctxHandle);
				result.Retain();
				return result;
			}
		}

		public GLObjectType GLObjectType{
			get{
				GLObjectType result;
				unsafe{
					Native.Call(Native.GL.GetGLObjectInfo(this.Handle, &result, null));
				}
				return result;
			}
		}

		public uint GLObjectName{
			get{
				uint result;
				unsafe{
					Native.Call(Native.GL.GetGLObjectInfo(this.Handle, null, &result));
				}
				return result;
			}
		}

		#endregion
		
		#region IHasInfo<MemInfo> Members
		unsafe ErrorCode IHasInfo<MemInfo>.GetInfo(MemInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetMemObjectInfo(handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		#region OpenGL Interop

		public void EnqueueAcquireGL(CommandQueue queue, Event[] waitList, out Event evt)
		{
			unsafe{
				uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
				IntPtr[] wlh = CLObject.GetHandles(waitList);
				IntPtr evtHandle = IntPtr.Zero;
				IntPtr[] memobjHandles = new IntPtr[]{this.Handle};
				Native.Call(Native.GL.EnqueueAcquireGLObjects(queue.Handle, 1, memobjHandles, waitCount, wlh, &evtHandle));
				evt = new Event(evtHandle);
			}
		}

		public void AcquireGL(CommandQueue queue)
		{
			Event evt;
			EnqueueAcquireGL(queue, null, out evt);
			evt.Wait();
			evt.Dispose();
		}

		public static void AcquireGLObjects(CommandQueue queue, CLObject[] objects)
		{
			Event evt;
			EnqueueAcquireGLObjects(queue, objects, null, out evt);
			evt.Wait();
			evt.Dispose();
		}

		public static void EnqueueAcquireGLObjects(CommandQueue queue, CLObject[] objects, Event[] waitList, out Event evt)
		{
			unsafe{
				IntPtr[] objHandles = CLObject.GetHandles(objects);
				uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
				IntPtr[] wlh = CLObject.GetHandles(waitList);
				IntPtr evtHandle = IntPtr.Zero;
				Native.Call(Native.GL.EnqueueAcquireGLObjects(queue.Handle, (uint)objHandles.Length, objHandles, waitCount, wlh, &evtHandle));
				
				evt = new Event(evtHandle);
			}
		}

		public void EnqueueReleaseGL(CommandQueue queue, Event[] waitList, out Event evt)
		{
			unsafe{
				uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
				IntPtr[] wlh = CLObject.GetHandles(waitList);
				IntPtr evtHandle = IntPtr.Zero;
				IntPtr[] memobjHandles = new IntPtr[]{this.Handle};
				fixed(IntPtr* p_wlh = wlh){
					Native.Call(Native.GL.EnqueueReleaseGLObjects(queue.Handle, 1, memobjHandles, waitCount, wlh, &evtHandle));
				}
				evt = new Event(evtHandle);
			}
		}

		public void ReleaseGL(CommandQueue queue)
		{
			Event evt;
			EnqueueReleaseGL(queue, null, out evt);
			evt.Wait();
			evt.Dispose();
		}

		public static void ReleaseGLObjects(CommandQueue queue, CLObject[] objects)
		{
			Event evt;
			EnqueueReleaseGLObjects(queue, objects, null, out evt);
			evt.Wait();
			evt.Dispose();
		}

		public static void EnqueueReleaseGLObjects(CommandQueue queue, CLObject[] objects, Event[] waitList, out Event evt)
		{
			unsafe{
				uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
				IntPtr[] wlh = CLObject.GetHandles(waitList);
				IntPtr evtHandle = IntPtr.Zero;
				IntPtr[] objHandles = CLObject.GetHandles(objects);
				Native.Call(Native.GL.EnqueueReleaseGLObjects(queue.Handle, (uint)objHandles.Length, objHandles, waitCount, wlh, &evtHandle));
				evt = new Event(evtHandle);
			}
		}

		#endregion

	}
}