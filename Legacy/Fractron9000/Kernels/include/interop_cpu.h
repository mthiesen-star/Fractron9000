#ifndef __INTEROP_CPU_H__
#define __INTEROP_CPU_H__

#include <math.h>

#define _CPU_SRC_

//Begin CUDA qualifiers
#define KERNEL
#define DEVICE

#define GLOBAL_MEM
#define LOCAL_MEM
#define CONST_MEM
#define PRIVATE_MEM

#define GLOBAL_PARAM
#define LOCAL_PARAM
#define CONST_PARAM
#define PRIVATE_PARAM

//Begin CUDA attributes
#define PACKED_ATTR

//byte types that match the .NET types
typedef char                   sbyte;
typedef unsigned char          byte;
typedef          long long int Int64;
typedef unsigned long long int UInt64;


//make abbreviated unsigned types available to CUDA
typedef unsigned short         ushort;
typedef unsigned int           uint;

//Make vector types available in C
typedef struct PACKED_ATTR _uint2_struct{
	uint x, y;
} uint2;
typedef struct PACKED_ATTR _uint4_struct{
	uint x, y, z, w;
} uint4;

typedef struct PACKED_ATTR _float2_struct{
	float x, y;
} float2;
typedef struct PACKED_ATTR _float4_struct{
	float x, y, z, w;
} float4;

//various math functions
float math_fmin(float x, float y){ return x < y ? x : y; }
float math_fmax(float x, float y){ return x > y ? x : y; }

#define math_fabs(x)       ((float)fabsf(x))
#define math_atan(x)       ((float)atanf(x))
#define math_atan2(y,x)    ((float)atan2f(y,x))

float fast_saturate(float x){ return x < 0.0f ? 0.0f : (x > 1.0f ? 1.0f : x); }
#define fast_sqrt(x)       ((float)sqrtf(x))
#define fast_exp(x)        ((float)expf(x))
#define fast_log(x)        ((float)logf(x))
#define fast_log10(x)      ((float)log10f(x))
#define fast_pow(x,y)      ((float)powf(x,y))
#define fast_sin(x)        ((float)sinf(x))
#define fast_cos(x)        ((float)cosf(x))
#define fast_tan(x)        ((float)tanf(x))


//These make CUDA style constructors available in C

uint2 make_uint2(uint x, uint y){ uint2 v = {x, y}; return v; }
uint4 make_uint4(uint x, uint y, uint z, uint w){ uint4 v = {x, y, z, w}; return v; }

float2 make_float2(float x, float y){ float2 v = {x, y}; return v; }
float4 make_float4(float x, float y, float z, float w){ float4 v = {x, y, z, w}; return v; }


#endif
