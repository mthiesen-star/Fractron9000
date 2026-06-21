







 













































typedef char                   sbyte;
typedef unsigned char          byte;
typedef          long long int Int64;
typedef unsigned long long int UInt64;



typedef unsigned short         ushort;
typedef unsigned int           uint;





















 









































extern "C" __device__ float lerp(float n1, float n2, float a)
{
	return n1 + a * (n2 - n1);
}

typedef struct  _Affine2D_struct
{
	float2 xa;
	float2 ya;
	float2 ta;
} Affine2D;

extern "C" __device__ float2 Affine2D_transformVector(const Affine2D* a, float2 v)
{
	return make_float2(a->xa.x*v.x + a->ya.x*v.y + a->ta.x,
	                   a->xa.y*v.x + a->ya.y*v.y + a->ta.y);
}
	
extern "C" __device__ float2 Affine2D_transformVector_cm( Affine2D* a, float2 v)
{
	return make_float2(a->xa.x*v.x + a->ya.x*v.y + a->ta.x,
	                   a->xa.y*v.x + a->ya.y*v.y + a->ta.y);
}
	
extern "C" __device__ void Affine2D_transformPoint(const Affine2D* a, float* x_out, float* y_out, float x, float y)
{
	*x_out = a->xa.x*x + a->ya.x*y + a->ta.x;
	*y_out = a->xa.y*x + a->ya.y*y + a->ta.y;
}

extern "C" __device__ void Affine2D_getInverse(const Affine2D* a, Affine2D* out)
{
	float det = a->xa.x * a->ya.y - a->xa.y * a->ya.x;

	out->xa.x =  a->ya.y / det;
	out->xa.y = -a->xa.y / det;
	out->ya.x = -a->ya.x / det;
	out->ya.y =  a->xa.x / det;
	out->ta.x = (a->ta.y * a->ya.x - a->ta.x * a->ya.y) / det;
	out->ta.y = (a->ta.x * a->xa.y - a->ta.y * a->xa.x) / det;
}


typedef struct  _FractalInfo_struct
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


typedef struct  _BranchInfo_struct
{
	uint     normWeight;
	float    colorWeight;
	float2   chroma;
	Affine2D preTransform;
	Affine2D postTransform;
} BranchInfo;


typedef struct  _IterStatEntry_struct
{
	UInt64 dotCount;
	float  peakDensity;
	float  reserved0;
} IterStatEntry;


typedef struct  _GlobalStatEntry_struct
{
	UInt64 iterCount;
	UInt64 dotCount;
	float  density;
	float  peakDensity;
	float  scaleConstant;
  float  reserved0;
} GlobalStatEntry;


typedef struct  _Dot_struct
{
	float2 pos;
	float2 chroma;
} Dot;


















extern "C" __device__ unsigned int MWC_rand(
	 uint4* x,
	 uint*  c
);

extern "C" __device__ void MWC_seed(
	 uint4* x,
	 uint*  c,
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

extern "C" __device__ unsigned int MWC_rand(
	 uint4* x,
	 uint*  c
){
	UInt64 sum =
		((UInt64)2111111111)*(UInt64)(*x).w +
		((UInt64)1492)*(UInt64)(*x).z +
		((UInt64)1776)*(UInt64)(*x).y +
		((UInt64)5115)*(UInt64)(*x).x +
		      (UInt64)(*c);
		      
	(*x).w = (*x).z;
	(*x).z = (*x).y;
	(*x).y = (*x).x;
	(*x).x = (uint)sum;
	*c     = (uint)(sum >> 32);
	return (*x).x;
}

extern "C" __device__ float2 MWC_rand_float2(
	 uint4* x,
	 uint*  c
){
	float2 result;
	uint rnd = MWC_rand(x, c);
	result.x = (float)(rnd&0xFFFF) / 32768.0f - 1.0f;
	result.y = (float)(rnd>>16)   / 32768.0f - 1.0f;
	return result;
}


















































































































































































































extern "C" __device__ float2 vari_linear(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return v;
}

extern "C" __device__ float2 vari_sinusoidal(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		__sinf(v.x),
		__sinf(v.y)
	);
}

extern "C" __device__ float2 vari_spherical(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		v.x / (rsq+(4.7019774E-38f)),
		v.y / (rsq+(4.7019774E-38f))
	);
}

extern "C" __device__ float2 vari_swirl(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float j = __sinf(rsq);
	float k = __cosf(rsq);
	return make_float2(
		j*v.x - k*v.y,
		k*v.x + j*v.y
	);
}
	
extern "C" __device__ float2 vari_horseshoe(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		(v.x*v.x - v.y*v.y)/(r+(4.7019774E-38f)),
		(2.0f*v.x*v.y)/(r+(4.7019774E-38f))
	);
}
	
extern "C" __device__ float2 vari_polar(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		theta / 3.14159265358f,
		r - 1.0f
	);
}
	
extern "C" __device__ float2 vari_handkerchief(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		r * __sinf(theta+r),
		r * __cosf(theta-r)
	);
}
	
extern "C" __device__ float2 vari_heart(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		 r*__sinf(theta*r),
		-r*__cosf(theta*r)
	);
}

extern "C" __device__ float2 vari_disc(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		theta*__sinf(3.14159265358f*r)/3.14159265358f,
		theta*__cosf(3.14159265358f*r)/3.14159265358f
	);
}

extern "C" __device__ float2 vari_spiral(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		(__cosf(theta) + __sinf(r))/(r+(4.7019774E-38f)),
		(__sinf(theta) - __cosf(r))/(r+(4.7019774E-38f))
	);
}

extern "C" __device__ float2 vari_hyperbolic(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		v.y/(rsq+(4.7019774E-38f)),
		v.x
	);
}

extern "C" __device__ float2 vari_diamond(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		v.y / (r+(4.7019774E-38f)) * __cosf(r),
		v.x / (r+(4.7019774E-38f)) * __sinf(r)
	);
}

extern "C" __device__ float2 vari_ex(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float j,k;
	j = __cosf(r)*v.y + __sinf(r)*v.x;
	k = __sinf(r)*v.y + __cosf(r)*v.x;
	return make_float2( j*j*j/(rsq+(4.7019774E-38f)), k*k*k/(rsq+(4.7019774E-38f)) );
}

extern "C" __device__ float2 vari_julia(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = 0.5f*theta + 3.14159265358f*(float)(entropy&0x0001);
	return make_float2( __fsqrt_rn(r)*__cosf(k), __fsqrt_rn(r)*__sinf(k) );
}

extern "C" __device__ float2 vari_bent(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2( v.x >= 0 ? v.x : 2.0f*v.x, v.y >= 0 ? v.y : v.y*0.5f);
}

extern "C" __device__ float2 vari_waves(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		v.x + branch->preTransform.ya.x * __sinf(v.y/(branch->preTransform.ta.x*branch->preTransform.ta.x + (4.7019774E-38f))),
		v.y + branch->preTransform.ya.y * __sinf(v.x/(branch->preTransform.ta.y*branch->preTransform.ta.y + (4.7019774E-38f)))
	);
}

extern "C" __device__ float2 vari_fisheye(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = 2.0f/(r+1.0f);
	return make_float2(k*v.y, k*v.x);
}

extern "C" __device__ float2 vari_popcorn(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2(
		v.x + branch->preTransform.ta.x*__sinf(__tanf(3.0f*v.y)),
		v.y + branch->preTransform.ta.y*__sinf(__tanf(3.0f*v.x))
	);
}

extern "C" __device__ float2 vari_exponential(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = __expf(v.x - 1.0f);
	return make_float2( __sinf(3.14159265358f*v.y)*k, __cosf(3.14159265358f*v.y)*k );
}

extern "C" __device__ float2 vari_power(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k;
	float nx = v.x/(r+(4.7019774E-38f));
	float ny = v.y/(r+(4.7019774E-38f));
	k = __powf(r,ny);
	return make_float2(nx * k, ny * k);
}

extern "C" __device__ float2 vari_cosine(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float cosh_ty = 0.5f*(__expf(v.y) - __expf(-v.y));
	float sinh_ty = 0.5f*(__expf(v.y) + __expf(-v.y));
	return make_float2( __cosf(3.14159265358f*v.x)*cosh_ty, -1.0f*__sinf(3.14159265358f*v.x)*sinh_ty );
}

extern "C" __device__ float2 vari_eyefish(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = 2.0f/(r+1.0f);
	return make_float2( k*v.x, k*v.y );
}

extern "C" __device__ float2 vari_bubble(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = 4.0f / (rsq + 4.0f);
	return make_float2( k*v.x, k*v.y );
}

extern "C" __device__ float2 vari_cylinder(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	return make_float2( __sinf(v.x), v.y );
}

extern "C" __device__ float2 vari_noise(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float p1,p2;
	uint rnd = MWC_rand(randX, randC);
	p1 = (float)(rnd>>16) / 65536.0f;
	p2 = 2.0f*3.14159265358f*(float)(rnd&0x0000FFFF) / 65536.0f;
	
	return make_float2( p1*__cosf(p2)*v.x, p1*__sinf(p2)*v.y );
}

extern "C" __device__ float2 vari_blur(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float br,bt;	
	uint rnd = MWC_rand(randX, randC);
	br = (float)(rnd>>16) / 65536.0f;
	bt = 2.0f * 3.14159265358f * (float)(rnd&0x0000FFFF) / 65536.0f;
	
	return make_float2( __cosf(bt)*br, __sinf(bt)*br );
}

extern "C" __device__ float2 vari_gaussian_blur(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float br,bt;
	uint rnd;
	uint sum = 0;
	
	rnd = MWC_rand(randX, randC);
	sum += (rnd&0x0000FFFF) + (rnd>>16);
	rnd = MWC_rand(randX, randC);
	sum += (rnd&0x0000FFFF) + (rnd>>16);
	br = (float)sum / 65536.0f - 2.0f;
	bt = 2.0f * 3.14159265358f * (float)(entropy&0x0000FFFF) / 65536.0f;
	
	return make_float2( __cosf(bt)*br, __sinf(bt)*br );
}

extern "C" __device__ float2 vari_orb9k(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = 2.0f/(rsq+1.0f);
	return make_float2( k*v.x, k*v.y );
}

extern "C" __device__ float2 vari_ripple9k(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = __sinf(r*1.57079632679f);
	return make_float2( k*v.x, k*v.y );
}

extern "C" __device__ float2 vari_bulge9k(
	float2 v,
	 BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	 uint4*      randX,
	 uint*       randC
){
	float k = (r+1.0f)/(r+(4.7019774E-38f));
	return make_float2( k*v.x, k*v.y );
}



extern "C" __device__ uint chooseRandomBranch(
	uint rnd,
	uint branchCount,
	 BranchInfo branches[]
){
	uint i;

	for(i = 0; i < 16; i++)
		if(rnd < branches[i].normWeight)
			return i;
	
	return i;
}








extern "C" __device__ void iterate(
	 float2*   pos,
	 float2*   color,
	            uint      entropy,
	 BranchInfo* branch,
	 float       branchVariWeights[],
	 uint4*      randX,
	 uint*       randC
){
	float2 t;
	float2 vn;
	float2 result;
	float theta, rsq, r;
	result.x = 0.0f;
	result.y = 0.0f;
	
	(*color).x = lerp((*color).x, branch->chroma.x, branch->colorWeight);
	(*color).y = lerp((*color).y, branch->chroma.y, branch->colorWeight);
		
	t = Affine2D_transformVector_cm(&(branch->preTransform), *pos);
	theta = atan2f(t.x,t.y);
	rsq = t.x*t.x + t.y*t.y;
	r = __fsqrt_rn(rsq);
	
	if(branchVariWeights[0] > 0.0f){ vn = vari_linear(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[0]*vn.x; result.y += branchVariWeights[0]*vn.y; }
	if(branchVariWeights[1] > 0.0f){ vn = vari_sinusoidal(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[1]*vn.x; result.y += branchVariWeights[1]*vn.y; }
	if(branchVariWeights[2] > 0.0f){ vn = vari_spherical(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[2]*vn.x; result.y += branchVariWeights[2]*vn.y; }
	if(branchVariWeights[3] > 0.0f){ vn = vari_swirl(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[3]*vn.x; result.y += branchVariWeights[3]*vn.y; }
	if(branchVariWeights[4] > 0.0f){ vn = vari_horseshoe(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[4]*vn.x; result.y += branchVariWeights[4]*vn.y; }
	if(branchVariWeights[5] > 0.0f){ vn = vari_polar(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[5]*vn.x; result.y += branchVariWeights[5]*vn.y; }
	if(branchVariWeights[6] > 0.0f){ vn = vari_handkerchief(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[6]*vn.x; result.y += branchVariWeights[6]*vn.y; }
	if(branchVariWeights[7] > 0.0f){ vn = vari_heart(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[7]*vn.x; result.y += branchVariWeights[7]*vn.y; }
	if(branchVariWeights[8] > 0.0f){ vn = vari_disc(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[8]*vn.x; result.y += branchVariWeights[8]*vn.y; }
	if(branchVariWeights[9] > 0.0f){ vn = vari_spiral(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[9]*vn.x; result.y += branchVariWeights[9]*vn.y; }
	if(branchVariWeights[10] > 0.0f){ vn = vari_hyperbolic(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[10]*vn.x; result.y += branchVariWeights[10]*vn.y; }
	if(branchVariWeights[11] > 0.0f){ vn = vari_diamond(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[11]*vn.x; result.y += branchVariWeights[11]*vn.y; }
	if(branchVariWeights[12] > 0.0f){ vn = vari_ex(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[12]*vn.x; result.y += branchVariWeights[12]*vn.y; }
	if(branchVariWeights[13] > 0.0f){ vn = vari_julia(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[13]*vn.x; result.y += branchVariWeights[13]*vn.y; }
	if(branchVariWeights[14] > 0.0f){ vn = vari_bent(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[14]*vn.x; result.y += branchVariWeights[14]*vn.y; }
	if(branchVariWeights[15] > 0.0f){ vn = vari_waves(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[15]*vn.x; result.y += branchVariWeights[15]*vn.y; }
	if(branchVariWeights[16] > 0.0f){ vn = vari_fisheye(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[16]*vn.x; result.y += branchVariWeights[16]*vn.y; }
	if(branchVariWeights[17] > 0.0f){ vn = vari_popcorn(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[17]*vn.x; result.y += branchVariWeights[17]*vn.y; }
	if(branchVariWeights[18] > 0.0f){ vn = vari_exponential(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[18]*vn.x; result.y += branchVariWeights[18]*vn.y; }
	if(branchVariWeights[19] > 0.0f){ vn = vari_power(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[19]*vn.x; result.y += branchVariWeights[19]*vn.y; }
	if(branchVariWeights[20] > 0.0f){ vn = vari_cosine(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[20]*vn.x; result.y += branchVariWeights[20]*vn.y; }
	if(branchVariWeights[21] > 0.0f){ vn = vari_eyefish(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[21]*vn.x; result.y += branchVariWeights[21]*vn.y; }
	if(branchVariWeights[22] > 0.0f){ vn = vari_bubble(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[22]*vn.x; result.y += branchVariWeights[22]*vn.y; }
	if(branchVariWeights[23] > 0.0f){ vn = vari_cylinder(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[23]*vn.x; result.y += branchVariWeights[23]*vn.y; }
	if(branchVariWeights[24] > 0.0f){ vn = vari_noise(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[24]*vn.x; result.y += branchVariWeights[24]*vn.y; }
	if(branchVariWeights[25] > 0.0f){ vn = vari_blur(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[25]*vn.x; result.y += branchVariWeights[25]*vn.y; }
	if(branchVariWeights[26] > 0.0f){ vn = vari_gaussian_blur(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[26]*vn.x; result.y += branchVariWeights[26]*vn.y; }
	if(branchVariWeights[27] > 0.0f){ vn = vari_orb9k(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[27]*vn.x; result.y += branchVariWeights[27]*vn.y; }
	if(branchVariWeights[28] > 0.0f){ vn = vari_ripple9k(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[28]*vn.x; result.y += branchVariWeights[28]*vn.y; }
	if(branchVariWeights[29] > 0.0f){ vn = vari_bulge9k(t, branch, theta, r, rsq, entropy, randX, randC); result.x += branchVariWeights[29]*vn.x; result.y += branchVariWeights[29]*vn.y; }
	
	*pos = Affine2D_transformVector_cm(&(branch->postTransform), result);
}













__constant__    FractalInfo      fractalInfo[1];
__constant__    BranchInfo       branchInfo[16];
__constant__    float            variWeightBuffer[16*48];

texture<float4, 2, cudaReadModeElementType> paletteTex;


extern "C" __global__ void init_iterators_kernel(
   IterStatEntry    iterStatBuffer[],
   GlobalStatEntry* globalStatBuffer,
   uint4 entropyXBuffer[],
   uint  entropyCBuffer[],
   uint  entropySeedBuffer[]
){
  __shared__ uint4 randXBuffer   [64];
  __shared__ uint  randCBuffer   [64];
  __shared__ uint  randSeedBuffer[64];
    
  randXBuffer[threadIdx.x]    = make_uint4(0,0,0,0);
  randCBuffer[threadIdx.x]    = 0;
  randSeedBuffer[threadIdx.x] = entropySeedBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  
  MWC_seed(randXBuffer+threadIdx.x, randCBuffer+threadIdx.x, randSeedBuffer[threadIdx.x]);
  
  entropyXBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = randXBuffer[threadIdx.x];
  entropyCBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = randCBuffer[threadIdx.x];
  
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].dotCount = 0;
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].peakDensity = 0.0f;
  
  if((blockIdx.x*blockDim.x + threadIdx.x) == 0) 
  {
    globalStatBuffer->iterCount =     (UInt64)0;
    globalStatBuffer->dotCount =      (UInt64)0;
    globalStatBuffer->density  =      0.0f;
    globalStatBuffer->peakDensity =   0.0f;
    globalStatBuffer->scaleConstant = 0.0f;
  }
}

extern "C" __global__ void reset_iterators_kernel(
               uint             xRes,
               uint             yRes,





     float2           iterPosStateBuffer[],
     float2           iterColorStateBuffer[],
     IterStatEntry    iterStatBuffer[],
     GlobalStatEntry* globalStatBuffer,
     uint4            entropyXBuffer[],
     uint             entropyCBuffer[],
     uint             entropySeedBuffer[]
){
  __shared__ uint4 randXBuffer   [64];
  __shared__ uint  randCBuffer   [64];
  __shared__ uint  randSeedBuffer[64];
    
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].dotCount = 0;
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].peakDensity = 0.0f;
  
  if((blockIdx.x*blockDim.x + threadIdx.x) == 0) 
  {
    globalStatBuffer->iterCount =     (UInt64)0;
    globalStatBuffer->dotCount =      (UInt64)0;
    globalStatBuffer->density  =      0.0f;
    globalStatBuffer->peakDensity =   0.0f;
    globalStatBuffer->scaleConstant = 0.0f;
  }
  
  randXBuffer[threadIdx.x]    = make_uint4(0,0,0,0);
  randCBuffer[threadIdx.x]    = 0;
  randSeedBuffer[threadIdx.x] = entropySeedBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  
  MWC_seed(randXBuffer+threadIdx.x, randCBuffer+threadIdx.x, randSeedBuffer[threadIdx.x]);
  
  float2 pos = MWC_rand_float2(randXBuffer+threadIdx.x, randCBuffer+threadIdx.x);
  float2 color = make_float2(0.5f, 0.5f);
  
  for(int iter = 0; iter < (int)32; iter++)
  {
    uint rnd = MWC_rand(randXBuffer+threadIdx.x, randCBuffer+threadIdx.x);
    uint bi = chooseRandomBranch(rnd & 0x0000FFFF, fractalInfo->branchCount, branchInfo);                
    iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*48), randXBuffer+threadIdx.x, randCBuffer+threadIdx.x); 
  }
  
  iterPosStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = pos;
  iterColorStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = color;
}

extern "C" __global__ void reset_output_kernel(
  int xRes,
  int yRes,
   float4  accumBuffer[],
   uint    outputBuffer[]
)
{
  int x = (blockIdx.x*blockDim.x + threadIdx.x);
  int y = (blockIdx.y*blockDim.y + threadIdx.y);

  if(x < xRes && y < yRes)
  {
    (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((0)) + ((0))))) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((1)) + ((0))))) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((0)) + ((1))))) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((1)) + ((1))))) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    (*((outputBuffer + (((y))*xRes) + ((x))))) = 0x00000000;
  }
}














extern "C" __global__ void iterate_kernel(
               uint             xRes,
               uint             yRes,





     float2           iterPosStateBuffer[],
     float2           iterColorStateBuffer[],
     IterStatEntry    iterStatBuffer[],
     GlobalStatEntry* globalStatBuffer,
     uint4            entropyXBuffer[],
     uint             entropyCBuffer[],
     float4           accumBuffer[],










               uint             iterCount
){    
  __shared__ uint4 randXBuffer   [64];
  __shared__ uint  randCBuffer   [64];
  
  float2 pos =        iterPosStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  float2 color =      iterColorStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  UInt64 dotCount =   iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].dotCount;
  float peakDensity = iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].peakDensity;
  randXBuffer[threadIdx.x] = entropyXBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  randCBuffer[threadIdx.x] = entropyCBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  
  
  for(int iter = 0; iter < (int)iterCount; iter++)
  {
    uint rnd = MWC_rand(randXBuffer+threadIdx.x, randCBuffer+threadIdx.x);
    uint bi = chooseRandomBranch(rnd & 0x0000FFFF, fractalInfo->branchCount, branchInfo);                  
    
    iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*48), randXBuffer+threadIdx.x, randCBuffer+threadIdx.x); 
    
    float2 screenPos = Affine2D_transformVector_cm(&(fractalInfo->vpsTransform), pos);
    
    int xa = (int)(2.0f*screenPos.x);
    int ya = (int)(2.0f*screenPos.y);
    int x  = xa >> 1;
    int y  = ya >> 1;
    
    if(x >= 0 && x < xRes && y >= 0 && y < yRes)
    {








      float4 sample = tex2D(paletteTex, color.x, color.y);

      
      
      
      float4 mem = (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*(((ya&1))) + (((xa&1))))));
      mem.x += sample.x;
      mem.y += sample.y;
      mem.z += sample.z;
      mem.w += 1.0f;
      (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*(((ya&1))) + (((xa&1)))))) = mem;
      
      dotCount++;
      peakDensity = fmaxf(peakDensity,mem.w);
    }
  }
  
  
  iterPosStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)]   = pos;
  iterColorStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = color;
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].dotCount = dotCount;
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].peakDensity = peakDensity;
  entropyXBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = randXBuffer[threadIdx.x];
  entropyCBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = randCBuffer[threadIdx.x];
  
  if((blockIdx.x*blockDim.x + threadIdx.x) == 0)
  {
    globalStatBuffer->iterCount += (UInt64)((gridDim.x*blockDim.x) * iterCount);
  }
}

extern "C" __global__ void iterate_kernel_hybrid(
               uint             xRes,
               uint             yRes,





     float2           iterPosStateBuffer[],
     float2           iterColorStateBuffer[],
     IterStatEntry    iterStatBuffer[],
     GlobalStatEntry* globalStatBuffer,
     uint4            entropyXBuffer[],
     uint             entropyCBuffer[],
     float4           dotBuffer[],
               uint             iterCount
){  
  __shared__ uint4 randXBuffer[64];
  __shared__ uint  randCBuffer[64];
  
  float2 pos =       iterPosStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  float2 color =     iterColorStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  UInt64 dotCount =  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].dotCount;
  randXBuffer[threadIdx.x] = entropyXBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  randCBuffer[threadIdx.x] = entropyCBuffer[(blockIdx.x*blockDim.x + threadIdx.x)];
  
  for(int iter = 0; iter < (int)iterCount; iter++)
  {
    uint rnd = MWC_rand(randXBuffer+threadIdx.x, randCBuffer+threadIdx.x);
    uint bi = chooseRandomBranch(rnd & 0x0000FFFF, fractalInfo->branchCount, branchInfo);                  
    
    iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*48), randXBuffer+threadIdx.x, randCBuffer+threadIdx.x); 
    
    
    dotBuffer[iter * (gridDim.x*blockDim.x) + (blockIdx.x*blockDim.x + threadIdx.x)] = make_float4(pos.x, pos.y, color.x, color.y);
    
    
    float2 screenPos = Affine2D_transformVector_cm(&(fractalInfo->vpsTransform), pos);
    if(screenPos.x >= 0.0f && screenPos.x < (float)xRes && screenPos.y >= 0.0f && screenPos.y < (float)yRes)
    {
      dotCount++;
    }
  }
  
  iterPosStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)]   = pos;
  iterColorStateBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = color;
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].dotCount = dotCount;
  iterStatBuffer[(blockIdx.x*blockDim.x + threadIdx.x)].peakDensity = 0.0f;
  entropyXBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = randXBuffer[threadIdx.x];
  entropyCBuffer[(blockIdx.x*blockDim.x + threadIdx.x)] = randCBuffer[threadIdx.x];
  
  if((blockIdx.x*blockDim.x + threadIdx.x) == 0)
  {
    globalStatBuffer->iterCount += (UInt64)((gridDim.x*blockDim.x) * iterCount);
  }
}


extern "C" __global__ void update_stats_kernel(
               uint             xRes,
               uint             yRes,



               uint             iteratorCount,
     IterStatEntry    iterStatBuffer[],
     GlobalStatEntry* globalStatBuffer 
){
  if((blockIdx.x*blockDim.x + threadIdx.x) == 0)
  {
    UInt64 totalDotCount = 0;
    float peakDensity = 0.0f;
    for(int i = 0; i < (int)iteratorCount; i++)
    {
      totalDotCount += iterStatBuffer[i].dotCount;
      peakDensity = fmaxf(peakDensity,iterStatBuffer[i].peakDensity);
    }
    UInt64 totalIterationCount = globalStatBuffer->iterCount;
    float totalSubPixels = (float)(xRes*yRes*(2*2));
    float density = (float)totalDotCount / totalSubPixels;
    float invPixArea = fabsf((fractalInfo->vpsTransform.xa.x)*(fractalInfo->vpsTransform.ya.y) - (fractalInfo->vpsTransform.xa.y)*(fractalInfo->vpsTransform.ya.x));
    float scaleConstant = (64.0f/1.0f)*(invPixArea*(float)(2*2))/(float)totalIterationCount;
    
    
    globalStatBuffer->dotCount = totalDotCount;
    globalStatBuffer->density = fmaxf(density,(4.7019774E-38f));
    globalStatBuffer->peakDensity = fmaxf(peakDensity,(4.7019774E-38f));
    globalStatBuffer->scaleConstant = fmaxf(scaleConstant,(4.7019774E-38f));
  }
}

extern "C" __device__ float4 tonemap( FractalInfo* fractal, float4 rawPix, float scaleConstant)
{
  float4 logPix;
  float4 result;        
  
  if(rawPix.w <= 0.5f) 
    return make_float4(0.0f, 0.0f, 0.0f, 0.0f);
  
  logPix.w = (1.0f/2.0f) * fractal->brightness * __log10f(1.0f+rawPix.w*scaleConstant);
  float ka = logPix.w / rawPix.w;
  
  logPix.x = ka * rawPix.x;
  logPix.y = ka * rawPix.y;
  logPix.z = ka * rawPix.z;
  
  float z = __powf(logPix.w,fractal->invGamma);
  float gammaFactor = z / logPix.w;
  
  result.x = __saturatef(lerp(__powf(logPix.x,fractal->invGamma), gammaFactor*logPix.x, fractal->vibrancy));
  result.y = __saturatef(lerp(__powf(logPix.y,fractal->invGamma), gammaFactor*logPix.y, fractal->vibrancy));
  result.z = __saturatef(lerp(__powf(logPix.z,fractal->invGamma), gammaFactor*logPix.z, fractal->vibrancy));
  result.w = __saturatef(z);
  
  return result;
}

extern "C" __global__ void update_output_kernel(
             uint             xRes,
             uint             yRes,



   GlobalStatEntry* globalStatBuffer,
   float4           accumBuffer[],
   uint             outputBuffer[]
){
  uint4 iPix;
  float4 pix;
  
  int x = (blockIdx.x*blockDim.x + threadIdx.x);
  int y = (blockIdx.y*blockDim.y + threadIdx.y);
  
  float4 acc = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    
  if(x < xRes && y < yRes)
  {
    float scaleConstant = globalStatBuffer->scaleConstant;
    
    pix = tonemap( fractalInfo, (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((0)) + ((0))))), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
      
    pix = tonemap( fractalInfo, (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((1)) + ((0))))), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
    
    pix = tonemap( fractalInfo, (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((0)) + ((1))))), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
    
    pix = tonemap( fractalInfo, (*((accumBuffer + 4*((((y))*xRes) + ((x))) + 2*((1)) + ((1))))), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
    
    if(acc.w < (1.0f/256.0f))
    {
      iPix = make_uint4(0,0,0,0);
    }
    else
    {
      acc.x /= acc.w;
      acc.y /= acc.w;
      acc.z /= acc.w;
      acc.w *= 0.25f;
          
      iPix.x = (uint)(255.0f*acc.x) & 0xFF;
      iPix.y = (uint)(255.0f*acc.y) & 0xFF;
      iPix.z = (uint)(255.0f*acc.z) & 0xFF;
      iPix.w = (uint)(255.0f*acc.w) & 0xFF;
    }
        
    (*((outputBuffer + (((y))*xRes) + ((x))))) = iPix.w << 24 | iPix.z << 16 | iPix.y << 8 | iPix.x;
  }
}




































































































