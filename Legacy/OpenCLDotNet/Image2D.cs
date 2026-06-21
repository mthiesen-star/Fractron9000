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
	public class Image2D : MemObject, IDisposable, IHasInfo<ImageInfo>, IHasInfo<GLTextureInfo>
	{
		#region Creation and Destruction
		/// <summary>
		/// Creates a new Image2D. Does NOT increment the reference count.
		/// </summary>
		/// <param name="handle">The unmanaged Image2D handle.</param>
		public Image2D(IntPtr handle) : base(handle) {}

		public static Image2D Create(Context context, MemFlags flags, ImageFormat format, int width, int height)
		{
			IntPtr handle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;

			unsafe{
				handle = Native.CreateImage2D(context.Handle, flags, &format, (IntPtr)width, (IntPtr)height, (IntPtr)0, IntPtr.Zero, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			return new Image2D(handle);
		}

		public static Image2D CreateFromGLTexture2D(Context context, MemFlags flags, OpenTK.Graphics.OpenGL.TextureTarget texture_target, int miplevel, uint texture)
		{
			IntPtr handle = IntPtr.Zero;
			ErrorCode errorCode = ErrorCode.Success;

			unsafe{
				handle = Native.GL.CreateFromGLTexture2D(context.Handle, flags, texture_target, miplevel, texture, &errorCode);
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);

			return new Image2D(handle);
		}
		#endregion

		#region Properies
		public ImageFormat ImageFormat{
			get{ return Info.Get_ImageFormat<ImageInfo>(this, ImageInfo.Format); }
		}

		public IntPtr ElementSize{
			get{ return Info.Get_IntPtr<ImageInfo>(this, ImageInfo.ElementSize); }
		}

		public IntPtr RowPitch{
			get{ return Info.Get_IntPtr<ImageInfo>(this, ImageInfo.RowPitch); }
		}

		public int Width{
			get{ return (int)Info.Get_IntPtr<ImageInfo>(this, ImageInfo.Width); }
		}

		public int Height{
			get{ return (int)Info.Get_IntPtr<ImageInfo>(this, ImageInfo.Height); }
		}

		public Dim2D Dimensions{
			get{
				return new Dim2D(
					Info.Get_IntPtr<ImageInfo>(this, ImageInfo.Width),
					Info.Get_IntPtr<ImageInfo>(this, ImageInfo.Height) );
			}
		}

		public OpenTK.Graphics.OpenGL.TextureTarget GLTextureTarget{
			get{ return (OpenTK.Graphics.OpenGL.TextureTarget)Info.Get_uint<GLTextureInfo>(this, GLTextureInfo.TextureTarget); }
		}

		public int GLMipmapLevel{
			get{ return Info.Get_int<GLTextureInfo>(this, GLTextureInfo.MipmapLevel); }
		}
		#endregion

		#region IHasInfo<ImageInfo> Members
		unsafe ErrorCode IHasInfo<ImageInfo>.GetInfo(ImageInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GetImageInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		#region IHasInfo<GLTextureInfo> Members
		unsafe ErrorCode IHasInfo<GLTextureInfo>.GetInfo(GLTextureInfo param_name, IntPtr param_value_size, IntPtr param_value, IntPtr* param_value_size_ret)
		{
			return Native.GL.GetGLTextureInfo(this.Handle, param_name, param_value_size, param_value, param_value_size_ret);
		}
		#endregion

		#region Mapping
		public IntPtr Map(CommandQueue queue, MapFlags flags, Offset2D origin, Dim2D region, out IntPtr pitch)
		{
			Offset3D origin_buf = new Offset3D(origin.X, origin.Y, (IntPtr)0);
			Dim3D    region_buf =    new Dim3D(region.X, region.Y, (IntPtr)1);
			IntPtr tempPitch = IntPtr.Zero;		
			ErrorCode errorCode;
			IntPtr result = IntPtr.Zero;
			unsafe{
				result = Native.EnqueueMapImage(queue.Handle, this.Handle, true, flags, &origin_buf, &region_buf, &tempPitch, null, 0, null, null, &errorCode);			
				pitch = tempPitch;
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
			return result;
		}

		public IntPtr EnqueueMap(CommandQueue queue, MapFlags flags, Offset2D origin, Dim2D region, out IntPtr pitch, Event[] waitList, out Event evt)
		{
			Offset3D origin_buf = new Offset3D(origin.X, origin.Y, (IntPtr)0);
			Dim3D    region_buf =    new Dim3D(region.X, region.Y, (IntPtr)1);
			IntPtr tempPitch = IntPtr.Zero;
			uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
			IntPtr[] wlh = CLObject.GetHandles(waitList);
			IntPtr evtHandle = IntPtr.Zero;
			ErrorCode errorCode;
			IntPtr result = IntPtr.Zero;
			unsafe{
				result = Native.EnqueueMapImage(queue.Handle, this.Handle, true, flags, &origin_buf, &region_buf, &tempPitch, null, waitCount, wlh, &evtHandle, &errorCode);		
			}
			if(errorCode != ErrorCode.Success)
				throw new OpenCLCallFailedException(errorCode);
			pitch = tempPitch;
			evt = new Event(evtHandle);
			return result;
		}

		public unsafe void Unmap(CommandQueue queue, ref IntPtr mappedPtr)
		{
			Event evt;
			this.EnqueueUnmap(queue, ref mappedPtr, null, out evt);
			evt.Wait();
			evt.Dispose();
		}

		public unsafe void EnqueueUnmap(CommandQueue queue, ref IntPtr mappedPtr, Event[] waitList, out Event evt)
		{
			uint waitCount = (waitList == null ? 0 : (uint)waitList.Length);
			IntPtr[] wlh = CLObject.GetHandles(waitList);
			IntPtr evtHandle = IntPtr.Zero;
			Native.Call(Native.EnqueueUnmapMemObject(queue.Handle, this.Handle, mappedPtr, waitCount, wlh, &evtHandle));
			mappedPtr = IntPtr.Zero;
			evt = new Event(evtHandle);		
		}
		#endregion

		#region Reading
		public void Read<T>(CommandQueue queue, T[,] values) where T : struct
		{
			if(values == null)
				throw new ArgumentException("values array is null", "values");
			if(values.GetLength(1) != this.Width)
				throw new OpenCLBufferSizeException("Array width and image width do not match.");
			if(values.GetLength(0) != this.Height)
				throw new OpenCLBufferSizeException("Array height and image height do not match.");
			int elemSize = Marshal.SizeOf(typeof(T));
			if((IntPtr)elemSize != this.ElementSize)
				throw new OpenCLBufferSizeException("Array element and image element sizes do not match.");

			IntPtr pitch = IntPtr.Zero;
			IntPtr buf = this.Map(queue, MapFlags.Read, Offset2D.Zero, this.Dimensions, out pitch);
			try{
				copyPtrToArray(buf, pitch, values);
			}
			finally{
				this.Unmap(queue, ref buf);
			}
		}
		
		private static void copyPtrToArray<T>(IntPtr ptr, IntPtr pitch, T[,] arr)
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint widthInBytes = elemSize * (uint)arr.GetLength(1);
			uint height = (uint)arr.GetLength(0);

			unsafe{
				byte* rowPtr = (byte*)ptr;
				byte* elemPtr = rowPtr;
				for(int r = 0; r < arr.GetLength(0); r++)
				{
					elemPtr = rowPtr;
					for(int c = 0; c < arr.GetLength(1); c++)
					{
						arr[r,c] = (T)Marshal.PtrToStructure((IntPtr)elemPtr, typeof(T));
						elemPtr += elemSize;
					}
					rowPtr += (ulong)pitch;
				}
			}
		}
		#endregion

		#region Writing

		/*
		public void EnqueueWriteRaw(CommandQueue queue, int xOrigin, int yOrigin, int width, int height, IntPtr data, IntPtr rowPitch, out Event evt)
		{
			IntPtr evtHandle = IntPtr.Zero;

			unsafe{
				IntPtr* origin = stackalloc IntPtr[3];
				IntPtr* region = stackalloc IntPtr[3];

				origin[0] = (IntPtr)xOrigin; origin[1] = (IntPtr)yOrigin; origin[2] = (IntPtr)0;
				region[0] = (IntPtr)width;   region[1] = (IntPtr)height;  region[2] = (IntPtr)1;
				Native.Call(Native.EnqueueWriteImage(queue.Handle, this.Handle, false, origin, region, rowPitch, (IntPtr)0, data, 0, null, &evtHandle));
			}
			evt = new Event(evtHandle);
		}
		*/

		public void Write<T>(CommandQueue queue, T[,] values) where T : struct
		{
			if(values == null)
				throw new ArgumentException("values array is null", "values");
			if(values.GetLength(1) != this.Width)
				throw new OpenCLBufferSizeException("Array width and image width do not match.");
			if(values.GetLength(0) != this.Height)
				throw new OpenCLBufferSizeException("Array height and image height do not match.");
			int elemSize = Marshal.SizeOf(typeof(T));
			if((IntPtr)elemSize != this.ElementSize)
				throw new OpenCLBufferSizeException("Array element and image element sizes do not match.");

			IntPtr pitch = IntPtr.Zero;
			IntPtr buf = this.Map(queue, MapFlags.Write, Offset2D.Zero, this.Dimensions, out pitch);
			try{
				copyArrayToPtr(values, buf, pitch);
			}
			finally{
				this.Unmap(queue, ref buf);
			}
		}

		private static void copyArrayToPtr<T>(T[,] arr, IntPtr ptr, IntPtr pitch)
		{
			uint elemSize = (uint)Marshal.SizeOf(typeof(T));
			uint widthInBytes = elemSize * (uint)arr.GetLength(1);
			uint height = (uint)arr.GetLength(0);

			unsafe{
				byte* rowPtr = (byte*)ptr;
				byte* elemPtr = rowPtr;
				for(int r = 0; r < arr.GetLength(0); r++)
				{
					elemPtr = rowPtr;
					for(int c = 0; c < arr.GetLength(1); c++)
					{
						Marshal.StructureToPtr(arr[r,c], (IntPtr)elemPtr, false);
						elemPtr += elemSize;
					}
					rowPtr += (ulong)pitch;
				}
			}
		}
		#endregion

		#region Copying

		public void CopyTo(Image2D dstImage, CommandQueue queue,
			int srcOriginX, int srcOriginY, int dstOriginX, int dstOriginY,
			int width, int height)
		{
			IntPtr evtHandle = IntPtr.Zero;
			IntPtr[] srcOrg = new IntPtr[]{(IntPtr)srcOriginX, (IntPtr)srcOriginY, (IntPtr)0};
			IntPtr[] dstOrg = new IntPtr[]{(IntPtr)dstOriginX, (IntPtr)dstOriginY, (IntPtr)0};
			IntPtr[] region = new IntPtr[]{(IntPtr)width, (IntPtr)height, (IntPtr)1};

			unsafe{
				fixed(IntPtr* p_srcOrg = srcOrg, p_dstOrg = dstOrg, p_region = region)
				{
					Native.Call(Native.EnqueueCopyImage(
						queue.Handle, this.Handle, dstImage.Handle,
						p_srcOrg, p_dstOrg, p_region, 0, null, &evtHandle));
				}
			}
			Event evt = new Event(evtHandle);
			evt.Wait();
			evt.Dispose();
		}

		#endregion
	}
}