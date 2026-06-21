/*
    cuda_dll - Recompiles the CUDA libs to a .dll file, and adds a few utility functions
		This file and associated project are obsolete as of Fractron 9000 version 0.4
		
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

#include <windows.h>
#include <GL/gl.h>
#include <cuda.h>
#include <cudaGL.h>

//used to test how the program reacts to crashes in unmanaged code
int crashNow(int* ptr)
{
	return *ptr;
}

CUresult loadModuleHelper(CUmodule* mod, const char* image,
	char* logBuffer, int logBufferSize, char* errorBuffer, int errorBufferSize)
{
	CUjit_option options[10];
	void* values[10];
	int ct = 0;
	CUresult result = 0;

	if(logBuffer != 0 && logBufferSize > 0)
	{
		options[ct] = CU_JIT_INFO_LOG_BUFFER;
		values[ct] = (void*)logBuffer;
		ct++;
		options[ct] = CU_JIT_INFO_LOG_BUFFER_SIZE_BYTES;
		values[ct] = (void*)logBufferSize;
		ct++;
	}

	if(errorBuffer != 0 && errorBufferSize > 0)
	{
		options[ct] = CU_JIT_ERROR_LOG_BUFFER;
		values[ct] = (void*)errorBuffer;
		ct++;
		options[ct] = CU_JIT_ERROR_LOG_BUFFER_SIZE_BYTES;
		values[ct] = (void*)errorBufferSize;
		ct++;
	}

	/*
	if(maxRegisters > 0)
	{
		options[ct] = CU_JIT_MAX_REGISTERS;
		values[ct] = (void*)maxRegisters;
		ct++;
	}

	if(theadsPerBlock > 0)
	{
		options[ct] = CU_JIT_THREADS_PER_BLOCK;
		values[ct] = (void*)threadsPerBlock;
		ct++;
	}*/

	result = cuModuleLoadDataEx(mod, image, ct, options, values);
	return result;
}

void fillMemcpyArgs(
	CUDA_MEMCPY2D* args, 
	UINT32 srcType, UINT32 srcXInBytes, UINT32 srcY, void* srcPtr, UINT32 srcPitch,
	UINT32 dstType, UINT32 dstXInBytes, UINT32 dstY, void* dstPtr, UINT32 dstPitch,
	UINT32 widthInBytes, UINT32 height
){
	args->srcXInBytes = srcXInBytes;
	args->srcY = srcY;
	args->srcMemoryType = srcType;
	if(srcType == CU_MEMORYTYPE_HOST)
		args->srcHost = srcPtr;
	else if(srcType == CU_MEMORYTYPE_DEVICE)
		args->srcDevice = (CUdeviceptr)srcPtr;
	else if(srcType == CU_MEMORYTYPE_ARRAY)
		args->srcArray = (CUarray)srcPtr;
	args->srcPitch = srcPitch;

	args->dstXInBytes = dstXInBytes;
	args->dstY = dstY;
	args->dstMemoryType = dstType;
	if(dstType == CU_MEMORYTYPE_HOST)
		args->dstHost = dstPtr;
	else if(dstType == CU_MEMORYTYPE_DEVICE)
		args->dstDevice = (CUdeviceptr)dstPtr;
	else if(dstType == CU_MEMORYTYPE_ARRAY)
		args->dstArray = (CUarray)dstPtr;
	args->dstPitch = dstPitch;

	args->WidthInBytes = widthInBytes;
	args->Height = height;
}

int memcpy2DHelper(
	UINT32 srcType, UINT32 srcXInBytes, UINT32 srcY, void* srcPtr, UINT32 srcPitch,
	UINT32 dstType, UINT32 dstXInBytes, UINT32 dstY, void* dstPtr, UINT32 dstPitch,
	UINT32 widthInBytes, UINT32 height
){
	CUDA_MEMCPY2D args;
	
	fillMemcpyArgs(&args,
		srcType, srcXInBytes, srcY, srcPtr, srcPitch,
		dstType, dstXInBytes, dstY, dstPtr, dstPitch,
		widthInBytes, height);

	return cuMemcpy2D(&args);
}

int memcpy2DAsyncHelper(
	UINT32 srcType, UINT32 srcXInBytes, UINT32 srcY, void* srcPtr, UINT32 srcPitch,
	UINT32 dstType, UINT32 dstXInBytes, UINT32 dstY, void* dstPtr, UINT32 dstPitch,
	UINT32 widthInBytes, UINT32 height,
	CUstream stream
){
	CUDA_MEMCPY2D args;
	
	fillMemcpyArgs(&args,
		srcType, srcXInBytes, srcY, srcPtr, srcPitch,
		dstType, dstXInBytes, dstY, dstPtr, dstPitch,
		widthInBytes, height);

	return cuMemcpy2DAsync(&args, stream);
}

int memcpy2DUnalignedHelper(
	UINT32 srcType, UINT32 srcXInBytes, UINT32 srcY, void* srcPtr, UINT32 srcPitch,
	UINT32 dstType, UINT32 dstXInBytes, UINT32 dstY, void* dstPtr, UINT32 dstPitch,
	UINT32 widthInBytes, UINT32 height
){
	CUDA_MEMCPY2D args;
	
	fillMemcpyArgs(&args,
		srcType, srcXInBytes, srcY, srcPtr, srcPitch,
		dstType, dstXInBytes, dstY, dstPtr, dstPitch,
		widthInBytes, height);

	return cuMemcpy2DUnaligned(&args);
}

