//
//------------------< i n t e r o p _ c l . h >---------------------
//

/*
 * Some macros and types to help CUDA, OpenCL, and .NET all play nicely together
 */
 
#ifndef __INTEROP_CL_H__
#define __INTEROP_CL_H__

#define _OPENCL_SRC_

//Begin OpenCL qualifiers
#define KERNEL __kernel
#define DEVICE
#define GLOBAL_MEM   __global
#define LOCAL_MEM    __local
#define CONST_MEM    __constant
#define PRIVATE_MEM  __private

#define GLOBAL_PARAM  __global
#define LOCAL_PARAM   __local
#define CONST_PARAM   __constant
#define PRIVATE_PARAM __private


//Begin OpenCL attributes
#define PACKED_ATTR       __attribute__((packed))


//Begin OpenCL thread and group IDs
#define GLOBAL_SIZE_X    get_global_size(0)
#define GLOBAL_SIZE_Y    get_global_size(1)
#define GLOBAL_SIZE_Z    get_global_size(2)
#define GLOBAL_ID_X      get_global_id(0)
#define GLOBAL_ID_Y      get_global_id(1)
#define GLOBAL_ID_Z      get_global_id(2)

#define LOCAL_SIZE_X     get_local_size(0)
#define LOCAL_SIZE_Y     get_local_size(1)
#define LOCAL_SIZE_Z     get_local_size(2)
#define LOCAL_ID_X       get_local_id(0)
#define LOCAL_ID_Y       get_local_id(1)
#define LOCAL_ID_Z       get_local_id(2)

#define GROUP_COUNT_X    get_num_groups(0)
#define GROUP_COUNT_Y    get_num_groups(1)
#define GROUP_COUNT_Z    get_num_groups(2)
#define GROUP_ID_X       get_group_id(0)
#define GROUP_ID_Y       get_group_id(1)
#define GROUP_ID_Z       get_group_id(2)

//byte types that match the .NET types
typedef char                   sbyte;
typedef unsigned char          byte;
typedef          long int Int64;
typedef unsigned long int UInt64;

//Begin OpenCL math
#define math_fmin(x,y)     fmin(x,y)
#define math_fmax(x,y)     fmax(x,y)
#define math_fabs(x)       fabs(x)
#define math_atan(x)       atan(x)
#define math_atan2(y,x)    atan2(y,x)

#define fast_saturate(x)   clamp((x),0.0f,1.0f)
#define fast_sqrt(x)       native_sqrt(x)
#define fast_exp(x)        native_exp(x)
#define fast_log(x)        native_log(x)
#define fast_log2(x)       native_log2(x)
#define fast_log10(x)      native_log10(x)
#define fast_pow(x,y)      native_powr(x,y)
#define fast_exp(x)        native_exp(x)
#define fast_sin(x)        native_sin(x)
#define fast_cos(x)        native_cos(x)
#define fast_tan(x)        native_tan(x)

//These make CUDA style constructors available in OpenCL
#define make_char2(x,y) ((char2)((x),(y)))
#define make_char3(x,y,z) ((char3)((x),(y),(z)))
#define make_char4(x,y,z,w) ((char4)((x),(y),(z),(w)))

#define make_uchar2(x,y) ((uchar2)((x),(y)))
#define make_uchar3(x,y,z) ((uchar3)((x),(y),(z)))
#define make_uchar4(x,y,z,w) ((uchar4)((x),(y),(z),(w)))

#define make_short2(x,y) ((short2)((x),(y)))
#define make_short3(x,y,z) ((short3)((x),(y),(z)))
#define make_short4(x,y,z,w) ((short4)((x),(y),(z),(w)))

#define make_ushort2(x,y) ((ushort2)((x),(y)))
#define make_ushort3(x,y,z) ((ushort3)((x),(y),(z)))
#define make_ushort4(x,y,z,w) ((ushort4)((x),(y),(z),(w)))

#define make_int2(x,y) ((int2)((x),(y)))
#define make_int3(x,y,z) ((int3)((x),(y),(z)))
#define make_int4(x,y,z,w) ((int4)((x),(y),(z),(w)))

#define make_uint2(x,y) ((uint2)((x),(y)))
#define make_uint3(x,y,z) ((uint3)((x),(y),(z)))
#define make_uint4(x,y,z,w) ((uint4)((x),(y),(z),(w)))

#define make_long2(x,y) ((long2)((x),(y)))
#define make_long3(x,y,z) ((long3)((x),(y),(z)))
#define make_long4(x,y,z,w) ((long4)((x),(y),(z),(w)))

#define make_ulong2(x,y) ((ulong2)((x),(y)))
#define make_ulong3(x,y,z) ((ulong3)((x),(y),(z)))
#define make_ulong4(x,y,z,w) ((ulong4)((x),(y),(z),(w)))

#define make_float2(x,y) ((float2)((x),(y)))
#define make_float3(x,y,z) ((float3)((x),(y),(z)))
#define make_float4(x,y,z,w) ((float4)((x),(y),(z),(w)))

#endif
 