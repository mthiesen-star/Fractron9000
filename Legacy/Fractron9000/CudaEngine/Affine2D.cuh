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

#ifndef __AFFINE_2D_CUH__
#define __AFFINE_2D_CUH__

struct Affine2D
{
	float2 xa;
	float2 ya;
	float2 ta;
	
	__device__ float2 transformVector(float2 v)
	{
		return make_float2(xa.x*v.x + ya.x*v.y + ta.x,
		                   xa.y*v.x + ya.y*v.y + ta.y);
	}
	
	__device__ void transformPoint(float* x_out, float* y_out, float x, float y)
	{
		*x_out = xa.x*x + ya.x*y + ta.x;
		*y_out = xa.y*x + ya.y*y + ta.y;
	}
	
	__device__ Affine2D getInverse()
	{
		float det = xa.x * ya.y - xa.y * ya.x;
		Affine2D r;
		r.xa.x =  ya.y / det;
		r.xa.y = -xa.y / det;
		r.ya.x = -ya.x / det;
		r.ya.y =  xa.x / det;
		r.ta.x = (ta.y * ya.x - ta.x * ya.y) / det;
		r.ta.y = (ta.x * xa.y - ta.y * xa.x) / det;
		return r;
	}
};

#endif