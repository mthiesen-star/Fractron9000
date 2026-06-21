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

#ifndef __RANDOMMWC_CUH__
#define __RANDOMMWC_CUH__

typedef unsigned long long int MWC_ulong;

#define MWC_a ((MWC_ulong)2111111111)
#define MWC_b ((MWC_ulong)1492)
#define MWC_c ((MWC_ulong)1776)
#define MWC_d ((MWC_ulong)5115)

__device__ unsigned int MWC_rand(uint4* xBuffer, unsigned int* cBuffer, unsigned int idx);

__device__ void MWC_seed(uint4* xBuffer, unsigned int* cBuffer, unsigned int idx, unsigned int* seedBuffer)
{
	int j;
	xBuffer[idx].x = seedBuffer[idx] * 29943829 - 1;
	xBuffer[idx].y = xBuffer[idx].x  * 29943829 - 1;
	xBuffer[idx].z = xBuffer[idx].y  * 29943829 - 1;
	xBuffer[idx].w = xBuffer[idx].z  * 29943829 - 1;
	cBuffer[idx]   = xBuffer[idx].w  * 29943829 - 1;
	for(j = 0; j < 19; j++)
		MWC_rand(xBuffer, cBuffer, idx);
}

__device__ unsigned int MWC_rand(uint4* xBuffer, unsigned int* cBuffer, unsigned int idx)
{
	UInt64 sum =
		MWC_a*(MWC_ulong)xBuffer[idx].w +
		MWC_b*(MWC_ulong)xBuffer[idx].z +
		MWC_c*(MWC_ulong)xBuffer[idx].y +
		MWC_d*(MWC_ulong)xBuffer[idx].x +
		      (MWC_ulong)cBuffer[idx];
		      
	xBuffer[idx].w = xBuffer[idx].z;
	xBuffer[idx].z = xBuffer[idx].y;
	xBuffer[idx].y = xBuffer[idx].x;
	xBuffer[idx].x = (unsigned int)sum;
	cBuffer[idx]   = (unsigned int)(sum >> 32);
	return xBuffer[idx].x;
}


#endif
