//
//------------------< d a t a _ t y p e s . h >---------------------
//

#ifndef __DATA_TYPES_H__
#define __DATA_TYPES_H__

DEVICE float lerp(float n1, float n2, float a)
{
	return n1 + a * (n2 - n1);
}

typedef struct PACKED_ATTR _Affine2D_struct
{
	float2 xa;
	float2 ya;
	float2 ta;
} Affine2D;

DEVICE float2 Affine2D_transformVector(const Affine2D* a, float2 v)
{
	return make_float2(a->xa.x*v.x + a->ya.x*v.y + a->ta.x,
	                   a->xa.y*v.x + a->ya.y*v.y + a->ta.y);
}
	
DEVICE float2 Affine2D_transformVector_cm(CONST_PARAM Affine2D* a, float2 v)
{
	return make_float2(a->xa.x*v.x + a->ya.x*v.y + a->ta.x,
	                   a->xa.y*v.x + a->ya.y*v.y + a->ta.y);
}
	
DEVICE void Affine2D_transformPoint(const Affine2D* a, float* x_out, float* y_out, float x, float y)
{
	*x_out = a->xa.x*x + a->ya.x*y + a->ta.x;
	*y_out = a->xa.y*x + a->ya.y*y + a->ta.y;
}

DEVICE void Affine2D_getInverse(const Affine2D* a, Affine2D* out)
{
	float det = a->xa.x * a->ya.y - a->xa.y * a->ya.x;

	out->xa.x =  a->ya.y / det;
	out->xa.y = -a->xa.y / det;
	out->ya.x = -a->ya.x / det;
	out->ya.y =  a->xa.x / det;
	out->ta.x = (a->ta.y * a->ya.x - a->ta.x * a->ya.y) / det;
	out->ta.y = (a->ta.x * a->xa.y - a->ta.y * a->xa.x) / det;
}

// Device compatable fractal description
typedef struct PACKED_ATTR _FractalInfo_struct
{
	uint     branchCount;
	float    brightness;
	float    invGamma;
	float    vibrancy;
	float4   bgColor;
	Affine2D vpsTransform;
	float    reserved0;
	float    reserved1;
} FractalInfo;

// Device compatable branch description
typedef struct PACKED_ATTR _BranchInfo_struct
{
	uint     normWeight;
	float    colorWeight;
	float2   chroma;
	Affine2D preTransform;
	Affine2D postTransform;
} BranchInfo;

// Device compatable entry for per-iterator statistics
typedef struct PACKED_ATTR _IterStatEntry_struct
{
	UInt64 dotCount;
	float  peakDensity;
	float  reserved0;
} IterStatEntry;

// Device compatable entry for global statistics
typedef struct PACKED_ATTR _GlobalStatEntry_struct
{
	UInt64 iterCount;
	UInt64 dotCount;
	float  density;
	float  peakDensity;
	float  scaleConstant;
  float  reserved0;
} GlobalStatEntry;

// Device compatable entry for global statistics
typedef struct PACKED_ATTR _Dot_struct
{
	float2 pos;
	float2 chroma;
} Dot;

#endif
