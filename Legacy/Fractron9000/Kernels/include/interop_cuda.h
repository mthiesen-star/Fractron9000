//
//------------------< i n t e r o p _ c u d a . h >---------------------
//

/*
 * Some macros and types to help CUDA, OpenCL, and .NET all play nicely together
 */
 
#ifndef __INTEROP_CUDA_H__
#define __INTEROP_CUDA_H__

#define _CUDA_SRC_

//Begin CUDA qualifiers
#define KERNEL extern "C" __global__
#define DEVICE extern "C" __device__

#define GLOBAL_MEM
#define LOCAL_MEM    __shared__
#define CONST_MEM    __constant__
#define PRIVATE_MEM

#define GLOBAL_PARAM
#define LOCAL_PARAM
#define CONST_PARAM
#define PRIVATE_PARAM

//Begin CUDA attributes
#define PACKED_ATTR

//Begin CUDA thread and group IDs
#define GLOBAL_SIZE_X    (gridDim.x*blockDim.x)
#define GLOBAL_SIZE_Y    (gridDim.y*blockDim.y)
#define GLOBAL_SIZE_Z    (gridDim.z*blockDim.z)
#define GLOBAL_ID_X      (blockIdx.x*blockDim.x + threadIdx.x)
#define GLOBAL_ID_Y      (blockIdx.y*blockDim.y + threadIdx.y)
#define GLOBAL_ID_Z      (blockIdx.z*blockDim.z + threadIdx.z)

#define LOCAL_SIZE_X     blockDim.x
#define LOCAL_SIZE_Y     blockDim.y
#define LOCAL_SIZE_Z     blockDim.z
#define LOCAL_ID_X       threadIdx.x
#define LOCAL_ID_Y       threadIdx.y
#define LOCAL_ID_Z       threadIdx.z

#define GROUP_COUNT_X    gridDim.x
#define GROUP_COUNT_Y    gridDim.y
#define GROUP_COUNT_Z    gridDim.z
#define GROUP_ID_X       blockIdx.x
#define GROUP_ID_Y       blockIdx.y
#define GROUP_ID_Z       blockIdx.z

//byte types that match the .NET types
typedef char                   sbyte;
typedef unsigned char          byte;
typedef          long long int Int64;
typedef unsigned long long int UInt64;


//make abbreviated unsigned types available to CUDA
typedef unsigned short         ushort;
typedef unsigned int           uint;

//Begin CUDA math
#define math_fmin(x,y)     fminf(x,y)
#define math_fmax(x,y)     fmaxf(x,y)
#define math_fabs(x)       fabsf(x) 
#define math_atan(x)       atanf(x)
#define math_atan2(y,x)    atan2f(y,x)

#define fast_saturate(x)   __saturatef(x)
#define fast_sqrt(x)       __fsqrt_rn(x)
#define fast_exp(x)        __expf(x)
#define fast_log(x)        __logf(x)
#define fast_log2(x)       __log2f(x)
#define fast_log10(x)      __log10f(x)
#define fast_pow(x,y)      __powf(x,y)
#define fast_exp(x)        __expf(x)
#define fast_sin(x)        __sinf(x)
#define fast_cos(x)        __cosf(x)
#define fast_tan(x)        __tanf(x)

#endif
 