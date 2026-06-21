/*
    Fractron 9000
    Copyright (C) 2009 Michael J. Thiesen
	http://fractron9000.sourceforge.net
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


/*
 * Some types and structs to aid in interoperating with the .NET framework
 */

#ifndef __CUDA_INTEROP_CUH__
#define __CUDA_INTEROP_CUH__

//create typedefs that match the .NET types
typedef char                   sbyte;
typedef unsigned char          byte;
typedef unsigned short         ushort;
typedef unsigned int           uint;
typedef long long int          Int64;
typedef unsigned long long int UInt64;

//matches the binary format of the DevicePtr struct
template<class T>
struct DevicePtr2D{
	void* ptr;
	uint pitch;
	
	__device__ T* operator[](int index){
		return (T*)((char*)ptr + index*pitch);
	}
};

//matches the binary format of the DeviceBuffer struct
template<class T>
struct DeviceBuffer2D{
	DevicePtr2D<T> ptr;
	uint widthInBytes;
	uint height;
	
	__device__ T* operator[](int index){
		return ptr[index];
	}
};

//get a pointer to an element in a buffer
#define BufferPtr(type,buffer,i) ((type*)((buffer).ptr.ptr) + (i))
//reference an element in a buffer
#define BufferRef(type,buffer,i) (*(BufferPtr(type,buffer,i)))

//get element pointer in a 2D buffer
#define BufferPtr2D(type,buffer,row,col) ((type*)((char*)buffer.ptr.ptr + (row)*buffer.ptr.pitch) + (col))
//reference an element in a buffer
#define BufferRef2D(type,buffer,row,col) (*(BufferPtr2D(type,buffer,row,col)))



#endif