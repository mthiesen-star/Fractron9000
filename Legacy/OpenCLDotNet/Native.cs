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
	/*
	 * Native Bindings taken from the OpenTK Project
	 */
	internal static class Native
	{
		internal const string Library = "opencl.dll";

		internal static void Call(ErrorCode result){
			if(result != ErrorCode.Success){
				throw new OpenCLCallFailedException(result);
			}
		}

        [SuppressUnmanagedCodeSecurity()]
        [DllImport(Native.Library, EntryPoint = "clBuildProgram", ExactSpelling = true)]
        internal extern static unsafe ErrorCode BuildProgram(IntPtr program, uint num_devices, IntPtr[] device_list, String options, IntPtr pfn_notify, IntPtr user_data);
        
		[SuppressUnmanagedCodeSecurity()]
        [DllImport(Native.Library, EntryPoint = "clCreateBuffer", ExactSpelling = true)]
        internal extern static unsafe IntPtr CreateBuffer(IntPtr context, MemFlags flags, IntPtr size, IntPtr host_ptr, [Out] ErrorCode* errcode_ret);
        
		[SuppressUnmanagedCodeSecurity()]
        [DllImport(Native.Library, EntryPoint = "clCreateCommandQueue", ExactSpelling = true)]
        internal extern static unsafe IntPtr CreateCommandQueue(IntPtr context, IntPtr device, CommandQueueFlags properties, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateContext", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateContext(IntPtr[] properties, uint num_devices, IntPtr[] devices, IntPtr pfn_notify, IntPtr user_data, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateContextFromType", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateContextFromType(ContextProperties* properties, DeviceTypeFlags device_type, IntPtr pfn_notify, IntPtr user_data, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateImage2D", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateImage2D(IntPtr context, MemFlags flags, ImageFormat* image_format, IntPtr image_width, IntPtr image_height, IntPtr image_row_pitch, IntPtr host_ptr, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateImage3D", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateImage3D(IntPtr context, MemFlags flags, ImageFormat* image_format, IntPtr image_width, IntPtr image_height, IntPtr image_depth, IntPtr image_row_pitch, IntPtr image_slice_pitch, IntPtr host_ptr, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateKernel", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateKernel(IntPtr program, String kernel_name, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateKernelsInProgram", ExactSpelling = true)]
		internal extern static unsafe ErrorCode CreateKernelsInProgram(IntPtr program, uint num_kernels, IntPtr[] kernels, [Out] uint* num_kernels_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateProgramWithBinary", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateProgramWithBinary(IntPtr context, uint num_devices, IntPtr[] device_list, IntPtr[] lengths, byte** binaries, int* binary_status, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateProgramWithSource", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateProgramWithSource(IntPtr context, uint count, IntPtr* strings, IntPtr* lengths, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clCreateSampler", ExactSpelling = true)]
		internal extern static unsafe IntPtr CreateSampler(IntPtr context, bool normalized_coords, AddressingMode addressing_mode, FilterMode filter_mode, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueBarrier", ExactSpelling = true)]
		internal extern static ErrorCode EnqueueBarrier(IntPtr command_queue);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueCopyBuffer", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueCopyBuffer(IntPtr command_queue, IntPtr src_buffer, IntPtr dst_buffer, IntPtr src_offset, IntPtr dst_offset, IntPtr cb, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueCopyBufferToImage", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueCopyBufferToImage(IntPtr command_queue, IntPtr src_buffer, IntPtr dst_image, IntPtr src_offset, IntPtr** dst_origin, IntPtr** region, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueCopyImage", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueCopyImage(IntPtr command_queue, IntPtr src_image, IntPtr dst_image, IntPtr* src_origin, IntPtr* dst_origin, IntPtr* region, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueCopyImageToBuffer", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueCopyImageToBuffer(IntPtr command_queue, IntPtr src_image, IntPtr dst_buffer, IntPtr** src_origin, IntPtr** region, IntPtr dst_offset, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueMapBuffer", ExactSpelling = true)]
		internal extern static unsafe System.IntPtr EnqueueMapBuffer(IntPtr command_queue, IntPtr buffer, bool blocking_map, MapFlags map_flags, IntPtr offset, IntPtr cb, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueMapImage", ExactSpelling = true)]
		internal extern static unsafe System.IntPtr EnqueueMapImage(IntPtr command_queue, IntPtr image, bool blocking_map, MapFlags map_flags, Offset3D* origin, Dim3D* region, IntPtr* image_row_pitch, IntPtr* image_slice_pitch, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event, [Out] ErrorCode* errcode_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueMarker", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueMarker(IntPtr command_queue, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueNativeKernel", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueNativeKernel(IntPtr command_queue, IntPtr user_func, IntPtr args, IntPtr cb_args, uint num_mem_objects, IntPtr* mem_list, IntPtr args_mem_loc, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueNDRangeKernel", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueNDRangeKernel(IntPtr command_queue, IntPtr kernel, uint work_dim, IntPtr* global_work_offset, IntPtr* global_work_size, IntPtr* local_work_size, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueReadBuffer", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueReadBuffer(IntPtr command_queue, IntPtr buffer, bool blocking_read, IntPtr offset, IntPtr cb, IntPtr ptr, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueReadImage", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueReadImage(IntPtr command_queue, IntPtr image, bool blocking_read, IntPtr** origin, IntPtr** region, IntPtr row_pitch, IntPtr slice_pitch, IntPtr ptr, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueTask", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueTask(IntPtr command_queue, IntPtr kernel, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueUnmapMemObject", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueUnmapMemObject(IntPtr command_queue, IntPtr memobj, IntPtr mapped_ptr, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueWaitForEvents", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueWaitForEvents(IntPtr command_queue, uint num_events, IntPtr* event_list);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueWriteBuffer", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueWriteBuffer(IntPtr command_queue, IntPtr buffer, bool blocking_write, IntPtr offset, IntPtr cb, IntPtr ptr, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clEnqueueWriteImage", ExactSpelling = true)]
		internal extern static unsafe ErrorCode EnqueueWriteImage(IntPtr command_queue, IntPtr image, bool blocking_write, IntPtr* origin, IntPtr* region, IntPtr input_row_pitch, IntPtr input_slice_pitch, IntPtr ptr, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clFinish", ExactSpelling = true)]
		internal extern static ErrorCode Finish(IntPtr command_queue);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clFlush", ExactSpelling = true)]
		internal extern static ErrorCode Flush(IntPtr command_queue);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetCommandQueueInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetCommandQueueInfo(IntPtr command_queue, CommandQueueInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetContextInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetContextInfo(IntPtr context, ContextInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetDeviceIDs", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetDeviceIDs(IntPtr platform, DeviceTypeFlags device_type, uint num_entries, IntPtr* devices, uint* num_devices);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetDeviceInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetDeviceInfo(IntPtr device, DeviceInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetEventInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetEventInfo(IntPtr @event, EventInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetEventProfilingInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetEventProfilingInfo(IntPtr @event, ProfilingInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetImageInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetImageInfo(IntPtr image, ImageInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetKernelInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetKernelInfo(IntPtr kernel, KernelInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetKernelWorkGroupInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetKernelWorkGroupInfo(IntPtr kernel, IntPtr device, KernelWorkGroupInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetMemObjectInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetMemObjectInfo(IntPtr memobj, MemInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetPlatformIDs", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetPlatformIDs(uint num_entries, IntPtr* platforms, uint* num_platforms);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetPlatformInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetPlatformInfo(IntPtr platform, PlatformInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetProgramBuildInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetProgramBuildInfo(IntPtr program, IntPtr device, ProgramBuildInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetProgramInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetProgramInfo(IntPtr program, ProgramInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetSamplerInfo", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetSamplerInfo(IntPtr sampler, SamplerInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetSupportedImageFormats", ExactSpelling = true)]
		internal extern static unsafe ErrorCode GetSupportedImageFormats(IntPtr context, MemFlags flags, MemObjectType image_type, uint num_entries, ImageFormat* image_formats, uint* num_image_formats);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseCommandQueue", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseCommandQueue(IntPtr command_queue);

		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseContext", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseContext(IntPtr context);

		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseEvent", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseEvent(IntPtr @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseKernel", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseKernel(IntPtr kernel);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseMemObject", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseMemObject(IntPtr memobj);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseProgram", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseProgram(IntPtr program);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clReleaseSampler", ExactSpelling = true)]
		internal extern static ErrorCode ReleaseSampler(IntPtr sampler);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainCommandQueue", ExactSpelling = true)]
		internal extern static ErrorCode RetainCommandQueue(IntPtr command_queue);

		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainContext", ExactSpelling = true)]
		internal extern static ErrorCode RetainContext(IntPtr context);

		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainEvent", ExactSpelling = true)]
		internal extern static ErrorCode RetainEvent(IntPtr @event);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainKernel", ExactSpelling = true)]
		internal extern static ErrorCode RetainKernel(IntPtr kernel);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainMemObject", ExactSpelling = true)]
		internal extern static ErrorCode RetainMemObject(IntPtr memobj);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainProgram", ExactSpelling = true)]
		internal extern static ErrorCode RetainProgram(IntPtr program);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clRetainSampler", ExactSpelling = true)]
		internal extern static ErrorCode RetainSampler(IntPtr sampler);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clSetCommandQueueProperty", ExactSpelling = true)]
		internal extern static unsafe ErrorCode SetCommandQueueProperty(IntPtr command_queue, CommandQueueFlags properties, bool enable, CommandQueueFlags* old_properties);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clSetKernelArg", ExactSpelling = true)]
		internal extern static ErrorCode SetKernelArg(IntPtr kernel, uint arg_index, IntPtr arg_size, IntPtr arg_value);
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clUnloadCompiler", ExactSpelling = true)]
		internal extern static ErrorCode UnloadCompiler();
		
		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clWaitForEvents", ExactSpelling = true)]
		internal extern static unsafe ErrorCode WaitForEvents(uint num_events, IntPtr* event_list);

		[SuppressUnmanagedCodeSecurity()]
		[DllImport(Native.Library, EntryPoint = "clGetExtensionFunctionAddress", ExactSpelling = true)]
		internal extern static unsafe IntPtr GetExtensionFunctionAddress(string funcname);

		internal static class GL
		{
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clCreateFromGLBuffer", ExactSpelling = true)]
			internal extern static unsafe IntPtr CreateFromGLBuffer(IntPtr context, MemFlags flags, uint bufobj, [Out] ErrorCode* errorCode_ret);
			
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clCreateFromGLRenderbuffer", ExactSpelling = true)]
			internal extern static unsafe IntPtr CreateFromGLRenderbuffer(IntPtr context, MemFlags flags, uint renderbuffer, [Out] ErrorCode* errorCode_ret);
			
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clCreateFromGLTexture2D", ExactSpelling = true)]
			internal extern static unsafe IntPtr CreateFromGLTexture2D(IntPtr context, MemFlags flags, OpenTK.Graphics.OpenGL.TextureTarget texture_target, int miplevel, uint texture, [Out] ErrorCode* errorCode_ret);
			
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clCreateFromGLTexture3D", ExactSpelling = true)]
			internal extern static unsafe IntPtr CreateFromGLTexture3D(IntPtr context, MemFlags flags, uint texture_target, int miplevel, uint texture, [Out] ErrorCode* errorCode_ret);
			
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clEnqueueAcquireGLObjects", ExactSpelling = true)]
			internal extern static unsafe ErrorCode EnqueueAcquireGLObjects(IntPtr command_queue, uint num_objects, IntPtr[] mem_objects, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
			
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clEnqueueReleaseGLObjects", ExactSpelling = true)]
			internal extern static unsafe ErrorCode EnqueueReleaseGLObjects(IntPtr command_queue, uint num_objects, IntPtr[] mem_objects, uint num_events_in_wait_list, IntPtr[] event_wait_list, IntPtr* @event);
			
			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clGetGLObjectInfo", ExactSpelling = true)]
			internal extern static unsafe ErrorCode GetGLObjectInfo(IntPtr memobj, GLObjectType* gl_object_type, uint* gl_object_name);

			[SuppressUnmanagedCodeSecurity()]
			[DllImport(Native.Library, EntryPoint = "clGetGLTextureInfo", ExactSpelling = true)]
			internal extern static unsafe ErrorCode GetGLTextureInfo(IntPtr memobj, GLTextureInfo param_name, IntPtr param_value_size, IntPtr param_value, [Out] IntPtr* param_value_size_ret);	
		}
	}
}