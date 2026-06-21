//
//------------------< v a r i a t i o n s . h >---------------------
//

#ifndef __ITERATE_H__
#define __ITERATE_H__

#include "config.h"
#include "data_types.h"
#include "random_mwc.h"

DEVICE float2 vari_linear(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return v;
}

DEVICE float2 vari_sinusoidal(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		fast_sin(v.x),
		fast_sin(v.y)
	);
}

DEVICE float2 vari_spherical(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		v.x / (rsq+Epsilon),
		v.y / (rsq+Epsilon)
	);
}

DEVICE float2 vari_swirl(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float j = fast_sin(rsq);
	float k = fast_cos(rsq);
	return make_float2(
		j*v.x - k*v.y,
		k*v.x + j*v.y
	);
}
	
DEVICE float2 vari_horseshoe(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		(v.x*v.x - v.y*v.y)/(r+Epsilon),
		(2.0f*v.x*v.y)/(r+Epsilon)
	);
}
	
DEVICE float2 vari_polar(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		theta / PIf,
		r - 1.0f
	);
}
	
DEVICE float2 vari_handkerchief(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		r * fast_sin(theta+r),
		r * fast_cos(theta-r)
	);
}
	
DEVICE float2 vari_heart(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		 r*fast_sin(theta*r),
		-r*fast_cos(theta*r)
	);
}

DEVICE float2 vari_disc(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		theta*fast_sin(PIf*r)/PIf,
		theta*fast_cos(PIf*r)/PIf
	);
}

DEVICE float2 vari_spiral(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		(fast_cos(theta) + fast_sin(r))/(r+Epsilon),
		(fast_sin(theta) - fast_cos(r))/(r+Epsilon)
	);
}

DEVICE float2 vari_hyperbolic(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		v.y/(rsq+Epsilon),
		v.x
	);
}

DEVICE float2 vari_diamond(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		v.y / (r+Epsilon) * fast_cos(r),
		v.x / (r+Epsilon) * fast_sin(r)
	);
}

DEVICE float2 vari_ex(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float j,k;
	j = fast_cos(r)*v.y + fast_sin(r)*v.x;
	k = fast_sin(r)*v.y + fast_cos(r)*v.x;
	return make_float2( j*j*j/(rsq+Epsilon), k*k*k/(rsq+Epsilon) );
}

DEVICE float2 vari_julia(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = 0.5f*theta + PIf*(float)(entropy&0x0001);
	return make_float2( fast_sqrt(r)*fast_cos(k), fast_sqrt(r)*fast_sin(k) );
}

DEVICE float2 vari_bent(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2( v.x >= 0 ? v.x : 2.0f*v.x, v.y >= 0 ? v.y : v.y*0.5f);
}

DEVICE float2 vari_waves(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		v.x + branch->preTransform.ya.x * fast_sin(v.y/(branch->preTransform.ta.x*branch->preTransform.ta.x + Epsilon)),
		v.y + branch->preTransform.ya.y * fast_sin(v.x/(branch->preTransform.ta.y*branch->preTransform.ta.y + Epsilon))
	);
}

DEVICE float2 vari_fisheye(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = 2.0f/(r+1.0f);
	return make_float2(k*v.y, k*v.x);
}

DEVICE float2 vari_popcorn(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2(
		v.x + branch->preTransform.ta.x*fast_sin(fast_tan(3.0f*v.y)),
		v.y + branch->preTransform.ta.y*fast_sin(fast_tan(3.0f*v.x))
	);
}

DEVICE float2 vari_exponential(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = fast_exp(v.x - 1.0f);
	return make_float2( fast_sin(PIf*v.y)*k, fast_cos(PIf*v.y)*k );
}

DEVICE float2 vari_power(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k;
	float nx = v.x/(r+Epsilon);
	float ny = v.y/(r+Epsilon);
	k = fast_pow(r, ny);
	return make_float2(nx * k, ny * k);
}

DEVICE float2 vari_cosine(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float cosh_ty = 0.5f*(fast_exp(v.y) - fast_exp(-v.y));
	float sinh_ty = 0.5f*(fast_exp(v.y) + fast_exp(-v.y));
	return make_float2( fast_cos(PIf*v.x)*cosh_ty, -1.0f*fast_sin(PIf*v.x)*sinh_ty );
}

DEVICE float2 vari_eyefish(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = 2.0f/(r+1.0f);
	return make_float2( k*v.x, k*v.y );
}

DEVICE float2 vari_bubble(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = 4.0f / (rsq + 4.0f);
	return make_float2( k*v.x, k*v.y );
}

DEVICE float2 vari_cylinder(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	return make_float2( fast_sin(v.x), v.y );
}

DEVICE float2 vari_noise(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float p1,p2;
	uint rnd = MWC_rand(randX, randC);
	p1 = (float)(rnd>>16) / 65536.0f;
	p2 = 2.0f*PIf*(float)(rnd&0x0000FFFF) / 65536.0f;
	
	return make_float2( p1*fast_cos(p2)*v.x, p1*fast_sin(p2)*v.y );
}

DEVICE float2 vari_blur(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float br,bt;	
	uint rnd = MWC_rand(randX, randC);
	br = (float)(rnd>>16) / 65536.0f;
	bt = 2.0f * PIf * (float)(rnd&0x0000FFFF) / 65536.0f;
	
	return make_float2( fast_cos(bt)*br, fast_sin(bt)*br );
}

DEVICE float2 vari_gaussian_blur(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float br,bt;
	uint rnd;
	uint sum = 0;
	
	rnd = MWC_rand(randX, randC);
	sum += (rnd&0x0000FFFF) + (rnd>>16);
	rnd = MWC_rand(randX, randC);
	sum += (rnd&0x0000FFFF) + (rnd>>16);
	br = (float)sum / 65536.0f - 2.0f;
	bt = 2.0f * PIf * (float)(entropy&0x0000FFFF) / 65536.0f;
	
	return make_float2( fast_cos(bt)*br, fast_sin(bt)*br );
}

DEVICE float2 vari_orb9k(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = 2.0f/(rsq+1.0f);
	return make_float2( k*v.x, k*v.y );
}

DEVICE float2 vari_ripple9k(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = fast_sin(r*PIo2f);
	return make_float2( k*v.x, k*v.y );
}

DEVICE float2 vari_bulge9k(
	float2 v,
	CONST_PARAM BranchInfo* branch,
	float theta, float r, float rsq, uint entropy,
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
){
	float k = (r+1.0f)/(r+Epsilon);
	return make_float2( k*v.x, k*v.y );
}


//chooses a branch index randomly based on the branch weights
DEVICE uint chooseRandomBranch(
	uint rnd,
	uint branchCount,
	CONST_PARAM BranchInfo branches[]
){
	uint i;

	for(i = 0; i < MaxBranches; i++)
		if(rnd < branches[i].normWeight)
			return i;
	
	return i;
}


#define CALL_VARIATION(idx,fn) if(branchVariWeights[idx] > 0.0f){\
		vn = fn(t, branch, theta, r, rsq, entropy, randX, randC);\
		result.x += branchVariWeights[idx]*vn.x;\
		result.y += branchVariWeights[idx]*vn.y;\
	}

DEVICE void iterate(
	PRIVATE_PARAM float2*   pos,
	PRIVATE_PARAM float2*   color,
	            uint      entropy,
	CONST_PARAM BranchInfo* branch,
	CONST_PARAM float       branchVariWeights[],
	LOCAL_PARAM uint4*      randX,
	LOCAL_PARAM uint*       randC
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
	theta = math_atan2(t.x,t.y);
	rsq = t.x*t.x + t.y*t.y;
	r = fast_sqrt(rsq);
	
	CALL_VARIATION( 0, vari_linear)
	CALL_VARIATION( 1, vari_sinusoidal)
	CALL_VARIATION( 2, vari_spherical)
	CALL_VARIATION( 3, vari_swirl)
	CALL_VARIATION( 4, vari_horseshoe)
	CALL_VARIATION( 5, vari_polar)
	CALL_VARIATION( 6, vari_handkerchief)
	CALL_VARIATION( 7, vari_heart)
	CALL_VARIATION( 8, vari_disc)
	CALL_VARIATION( 9, vari_spiral)
	CALL_VARIATION(10, vari_hyperbolic)
	CALL_VARIATION(11, vari_diamond)
	CALL_VARIATION(12, vari_ex)
	CALL_VARIATION(13, vari_julia)
	CALL_VARIATION(14, vari_bent)
	CALL_VARIATION(15, vari_waves)
	CALL_VARIATION(16, vari_fisheye)
	CALL_VARIATION(17, vari_popcorn)
	CALL_VARIATION(18, vari_exponential)
	CALL_VARIATION(19, vari_power)
	CALL_VARIATION(20, vari_cosine)
	CALL_VARIATION(21, vari_eyefish)
	CALL_VARIATION(22, vari_bubble)
	CALL_VARIATION(23, vari_cylinder)
	CALL_VARIATION(24, vari_noise)
	CALL_VARIATION(25, vari_blur)
	CALL_VARIATION(26, vari_gaussian_blur)
	CALL_VARIATION(27, vari_orb9k)
	CALL_VARIATION(28, vari_ripple9k)
	CALL_VARIATION(29, vari_bulge9k)
	
	*pos = Affine2D_transformVector_cm(&(branch->postTransform), result);
}


#endif

