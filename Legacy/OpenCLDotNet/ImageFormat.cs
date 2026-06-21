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
using System.Security;

namespace OpenCL
{
	[StructLayout(LayoutKind.Sequential)]
	public struct ImageFormat
	{
		public ImageFormat(ChannelOrder order, ChannelType type)
		{
			this.ChannelOrder = order;
			this.ChannelType = type;
		}

		public ChannelOrder ChannelOrder;
		public ChannelType ChannelType;

		public static bool operator ==(ImageFormat a, ImageFormat b)
		{
			return a.ChannelOrder == b.ChannelOrder && a.ChannelType == b.ChannelType;
		}
		public static bool operator !=(ImageFormat a, ImageFormat b)
		{
			return a.ChannelOrder != b.ChannelOrder || a.ChannelType != b.ChannelType;
		}

		public override bool Equals(object obj)
		{
			return (obj is ImageFormat) && ((ImageFormat)obj == this);
		}

		public override int GetHashCode()
		{
			return ChannelOrder.GetHashCode() ^ ChannelType.GetHashCode();
		}

		public bool IsSupported(Context context, MemFlags flags, MemObjectType type)
		{
			ImageFormat[] supportedFormats = GetSupportedImageFormats(context, flags, type);

			foreach(ImageFormat supported in supportedFormats)
				if(supported == this)
					return true;
			return false;
		}

		public static ImageFormat[] GetSupportedImageFormats(Context context, MemFlags flags, MemObjectType type)
		{
			uint count = 0;
			unsafe{
				Native.Call(Native.GetSupportedImageFormats(context.Handle, flags, type, 0, null, &count));
			}

			ImageFormat[] result = new ImageFormat[count];

			unsafe{
				fixed(ImageFormat* p_result = result)
				{
					Native.Call(Native.GetSupportedImageFormats(context.Handle, flags, type, count, p_result, null));
				}
			}
			return result;
		}
	}
}