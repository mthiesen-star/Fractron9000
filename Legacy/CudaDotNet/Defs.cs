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

namespace Cuda
{
	public enum CudaResult {
		Success              = 0,
		InvalidValue        = 1,
		OutOfMemory        = 2,
		NotInitialized      = 3,
		Deinitialized        = 4,
		
		NoDevice            = 100,
		InvalidDevice       = 101,
		
		InvalidImage        = 200,
		InvalidContext      = 201,
		ContextAlreadyCurrent = 202,
		MapFailed           = 205,
		UnmapFailed         = 206,
		ArrayIsMapped      = 207,
		AlreadyMapped       = 208,
		NoBinaryForGpu    = 209,
		AlreadyAcquired     = 210,
		NotMapped           = 211,
		
		InvalidSource       = 300,
		FileNotFound       = 301,
		
		InvalidHandle       = 400,
		
		NotFound            = 500,
		
		NotReady            = 600,
		
		LaunchFailed        = 700,
		LaunchOutOfResources = 701,
		LaunchTimeout       = 702,
		LaunchIncompatibleTexturing = 703,
		
		Unknown              = 999
	};
	
	internal enum DeviceAttribute{
		MAX_THREADS_PER_BLOCK = 1,
		MAX_BLOCK_DIM_X = 2,
		MAX_BLOCK_DIM_Y = 3,
		MAX_BLOCK_DIM_Z = 4,
		MAX_GRID_DIM_X = 5,
		MAX_GRID_DIM_Y = 6,
		MAX_GRID_DIM_Z = 7,
		MAX_SHARED_MEMORY_PER_BLOCK = 8,
		SHARED_MEMORY_PER_BLOCK = 8,      // Deprecated, use MAX_SHARED_MEMORY_PER_BLOCK
		TOTAL_CONSTANT_MEMORY = 9,
		WARP_SIZE = 10,
		MAX_PITCH = 11,
		MAX_REGISTERS_PER_BLOCK = 12,
		REGISTERS_PER_BLOCK = 12,         // Deprecated, use MAX_REGISTERS_PER_BLOCK
		CLOCK_RATE = 13,
		TEXTURE_ALIGNMENT = 14,
		GPU_OVERLAP = 15,
		MULTIPROCESSOR_COUNT = 16,
		KERNEL_EXEC_TIMEOUT = 17
	};


	internal static class CudaUtil
	{
		internal static void Call(CudaResult result){
			if(result != CudaResult.Success){
				throw new Cuda.CudaException(result);
			}
		}
	}
}