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

namespace OpenCL
{
	/// <summary>
	/// Interface that an OpenCL object can implement to indicate that it can be queried for information.
	/// </summary>
	/// <typeparam name="KeyType">The enum type used as the key for retrieving information.</typeparam>
	public interface IHasInfo<KeyType>
	{
		unsafe ErrorCode GetInfo(KeyType param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret);
	}

	/// <summary>
	/// Helper class for reading information from various OpenCL objects.
	/// </summary>
	public static class Info
	{
		public static IntPtr GetSize<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			IntPtr result = IntPtr.Zero;
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)0, IntPtr.Zero, &result));
			}
			return result;
		}

		public static string Get_string<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			string result = null;

			unsafe{
				IntPtr size = GetSize(obj, param_name);
				IntPtr strBuf = Marshal.AllocHGlobal((int)size + 1); //leave room for a null terminator
				try{
					Native.Call(obj.GetInfo(param_name, size, strBuf, null));
					((byte*)strBuf)[(int)size] = 0; //make sure the string is null terminated
					result = Marshal.PtrToStringAnsi(strBuf);
				}
				finally{
					Marshal.FreeHGlobal(strBuf);
				}
			}
			return result;
		}
		
		public static int[] Get_intArray<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			unsafe{
				IntPtr size = GetSize(obj, param_name);
				uint count = (uint)size / sizeof(int);
				int[] result = new int[count];
				fixed(int* p_result = result){
					Native.Call(obj.GetInfo(param_name, size, (IntPtr)p_result, null));
				}
				return result;
			}
		}

		public static uint[] Get_uintArray<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			unsafe{
				IntPtr size = GetSize(obj, param_name);
				uint count = (uint)size / sizeof(uint);
				uint[] result = new uint[count];
				fixed(uint* p_result = result){
					Native.Call(obj.GetInfo(param_name, size, (IntPtr)p_result, null));
				}
				return result;
			}
		}

		public static IntPtr[] Get_IntPtrArray<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			unsafe{
				IntPtr size = GetSize(obj, param_name);
				uint count = (uint)size / (uint)sizeof(IntPtr);
				IntPtr[] result = new IntPtr[count];
				fixed(IntPtr* p_result = result){
					Native.Call(obj.GetInfo(param_name, size, (IntPtr)p_result, null));
				}
				return result;
			}
		}

		public static IntPtr Get_IntPtr<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			IntPtr result = IntPtr.Zero;
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)sizeof(IntPtr), (IntPtr)(&result), null));
			}
			return result;
		}

		public static bool Get_bool<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			return Get_int(obj, param_name) != 0;
		}

		public static int Get_int<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			int result = 0;
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)sizeof(int), (IntPtr)(&result), null));
			}
			return result;
		}

		public static uint Get_uint<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			uint result = 0;
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)sizeof(uint), (IntPtr)(&result), null));
			}
			return result;
		}

		public static long Get_long<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			long result = 0;
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)sizeof(long), (IntPtr)(&result), null));
			}
			return result;
		}

		public static ulong Get_ulong<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			ulong result = 0;
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)sizeof(ulong), (IntPtr)(&result), null));
			}
			return result;
		}

		public static ImageFormat Get_ImageFormat<KeyType>(IHasInfo<KeyType> obj, KeyType param_name)
		{
			ImageFormat result = new ImageFormat();
			unsafe{
				Native.Call(obj.GetInfo(param_name, (IntPtr)sizeof(ImageFormat), (IntPtr)(&result), null));
			}
			return result;
		}
	}
}