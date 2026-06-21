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
	public class Sampler : CLResource, IHasInfo<SamplerInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new Sampler. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged sampler handle.</param>
		public Sampler(IntPtr handle) : base(handle) {}

		public static Sampler Create(Context context, bool normalizedCoords, AddressingMode addressingMode, FilterMode filterMode)
		{
			IntPtr handle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;

			unsafe{
				handle = Native.CreateSampler(context.Handle, normalizedCoords, addressingMode, filterMode, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			return new Sampler(handle);
		}

		protected override void Release()
		{
			Native.Call(Native.ReleaseSampler(this.Handle));
		}
		public void Retain()
		{
			Native.Call(Native.RetainSampler(this.Handle));
		}
		#endregion

		#region Properies
		public uint ReferenceCount{
			get{ return Info.Get_uint(this, SamplerInfo.ReferenceCount); }
		}

		/// <summary>
		/// Gets and retains the context object for this sampler.
		/// </summary>
		public Context Context{
			get{
				IntPtr ctxHandle = Info.Get_IntPtr(this, SamplerInfo.Context);
				Context result = new Context(ctxHandle);
				result.Retain();
				return result;
			}
		}

		public AddressingMode AddressingMode{
			get{ return (AddressingMode)Info.Get_int(this, SamplerInfo.AddressingMode); }
		}

		public FilterMode FilterMode{
			get{ return (FilterMode)Info.Get_int(this, SamplerInfo.FilterMode); }
		}

		public bool NormalizedCoords{
			get{ return Info.Get_bool(this, SamplerInfo.NormalizedCoords); }
		}
		#endregion

		#region IHasInfo<SamplerInfo> Members
		unsafe ErrorCode IHasInfo<SamplerInfo>.GetInfo(SamplerInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetSamplerInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion
	}
}