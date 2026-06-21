
//------------------< r a n d o m _ m w c . h >---------------------



#ifndef __RANDOM_MWC_H__
#define __RANDOM_MWC_H__


#define MWC_A ((UInt64)2111111111)
#define MWC_B ((UInt64)1492)
#define MWC_C ((UInt64)1776)
#define MWC_D ((UInt64)5115)

DEVICE unsigned int MWC_rand(
	LOCAL_PARAM uint4* x,
	LOCAL_PARAM uint*  c
);

DEVICE void MWC_seed(
	LOCAL_PARAM uint4* x,
	LOCAL_PARAM uint*  c,
	            uint seed
){
	int j;
	(*x).x = seed * 29943829 - 1;
	(*x).y = (*x).x  * 29943829 - 1;
	(*x).z = (*x).y  * 29943829 - 1;
	(*x).w = (*x).z  * 29943829 - 1;
	*c     = (*x).w  * 29943829 - 1;
	for(j = 0; j < 19; j++)
		MWC_rand(x, c);
}

DEVICE unsigned int MWC_rand(
	LOCAL_PARAM uint4* x,
	LOCAL_PARAM uint*  c
){
	UInt64 sum =
		MWC_A*(UInt64)(*x).w +
		MWC_B*(UInt64)(*x).z +
		MWC_C*(UInt64)(*x).y +
		MWC_D*(UInt64)(*x).x +
		      (UInt64)(*c);
		      
	(*x).w = (*x).z;
	(*x).z = (*x).y;
	(*x).y = (*x).x;
	(*x).x = (uint)sum;
	*c     = (uint)(sum >> 32);
	return (*x).x;
}

DEVICE float2 MWC_rand_float2(
	LOCAL_PARAM uint4* x,
	LOCAL_PARAM uint*  c
){
	float2 result;
	uint rnd = MWC_rand(x, c);
	result.x = (float)(rnd&0xFFFF) / 32768.0f - 1.0f;
	result.y = (float)(rnd>>16)   / 32768.0f - 1.0f;
	return result;
}


#endif
