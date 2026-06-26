# Legacy OpenCL Kernel: iterate() Function

**Source File**: `Legacy/Fractron9000/Kernels/kernels.cl`

## Struct Definitions

### BranchInfo
```c
typedef struct __attribute__((packed)) _BranchInfo_struct
{
	uint     normWeight;
	float    colorWeight;
	float2   chroma;
	Affine2D preTransform;
	Affine2D postTransform;
} BranchInfo;
```

### Affine2D
```c
typedef struct __attribute__((packed)) _Affine2D_struct
{
	float2 xa;
	float2 ya;
	float2 ta;
} Affine2D;
```

### FractalInfo
```c
typedef struct __attribute__((packed)) _FractalInfo_struct
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
```

---

## Complete iterate() Function

**Location**: Line 888 in `kernels.cl`

```c
void iterate(
	__private float2*   pos,
	__private float2*   color,
	            uint      entropy,
	__constant BranchInfo* branch,
	__constant float       branchVariWeights[],
	__local uint4*      randX,
	__local uint*       randC
){
	float2 t;
	float2 vn;
	float2 result;
	float theta, rsq, r;
	result.x = 0.0f;
	result.y = 0.0f;
	
	// Color blending
	(*color).x = lerp((*color).x, branch->chroma.x, branch->colorWeight);
	(*color).y = lerp((*color).y, branch->chroma.y, branch->colorWeight);
		
	// Pre-affine transform and polar coordinate pre-computation
	t = Affine2D_transformVector_cm(&(branch->preTransform), *pos);
	theta = atan2(t.x,t.y);
	rsq = t.x*t.x + t.y*t.y;
	r = native_sqrt(rsq);
	
	// Apply all 30 variations with per-branch weights
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
	
	// Post-affine transform
	*pos = Affine2D_transformVector_cm(&(branch->postTransform), result);
}
```

---

## Key Details

### 1. Function Signature
- **Return type**: `void` (modifies position and color by pointer)
- **Parameters**:
  - `__private float2* pos` — Current position in fractal space (modified)
  - `__private float2* color` — Current color (u,v coordinates, modified)
  - `uint entropy` — Random number seed for stochastic variations
  - `__constant BranchInfo* branch` — Reference to the selected branch's transform parameters
  - `__constant float branchVariWeights[]` — Array of 30 per-branch variation weights
  - `__local uint4* randX`, `__local uint* randC` — Random number generator state (MWC algorithm)

### 2. Per-Branch Variation Weights Storage & Access
- **Storage**: `branchVariWeights[]` is a constant memory array passed as parameter
- **Access pattern**: `branchVariWeights[i]` where `i` is the variation index (0–29)
- **Indexing in kernel**: Called with `variWeightBuffer+(bi*48)` where:
  - `bi` = branch index
  - `48` = 48 floats per branch (30 variations × 4 bytes per float = 120 bytes, but scaled by some factor)
  - In memory, each branch gets 48 floats of space (though only first 30 are used)
- **Conditional execution**: Each variation only executes if `branchVariWeights[i] > 0.0f`

### 3. Complete Variation Loop & Accumulation
All 30 variations follow this pattern:
```c
if(branchVariWeights[i] > 0.0f){
    vn = vari_XXXXX(t, branch, theta, r, rsq, entropy, randX, randC);
    result.x += branchVariWeights[i]*vn.x;
    result.y += branchVariWeights[i]*vn.y;
}
```

**The 30 variations in order** (indices 0–29):
0. linear
1. sinusoidal
2. spherical
3. swirl
4. horseshoe
5. polar
6. handkerchief
7. heart
8. disc
9. spiral
10. hyperbolic
11. diamond
12. ex
13. julia
14. bent
15. waves
16. fisheye
17. popcorn
18. exponential
19. power
20. cosine
21. eyefish
22. bubble
23. cylinder
24. noise
25. blur
26. gaussian_blur
27. orb9k
28. ripple9k
29. bulge9k

### 4. Accumulation Logic
- Each variation returns a `float2` (`vn`)
- Result is accumulated as: `result.x += branchVariWeights[i]*vn.x; result.y += branchVariWeights[i]*vn.y;`
- Accumulation is **additive** across all active variations

### 5. Polar Coordinate Pre-computation
**Performed immediately after pre-affine transform** (lines 905–908):
```c
t = Affine2D_transformVector_cm(&(branch->preTransform), *pos);
theta = atan2(t.x, t.y);  // Angle in radians
rsq = t.x*t.x + t.y*t.y;  // Squared radius (avoids sqrt for many variations)
r = native_sqrt(rsq);      // Actual radius (computed only once)
```

These are passed to **every** variation function for use as needed.

---

## Integration in __kernel iterate_kernel()

**Location**: Line 1084 in `kernels.cl`

The kernel calls `iterate()` once per iteration:
```c
iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*48), randXBuffer+lid, randCBuffer+lid);
```

After the call:
- Updated `pos` is transformed to screen space
- Screen position is rasterized to histogram bins
- Histogram accumulation uses floating-point RGBA scatter-write with atomics (not atomic operations on u32 as in the notes — uses float4 accum).

---

## Helper Functions

### Affine2D_transformVector_cm
Applies affine transform to a 2D vector using constant-memory affine parameters:
```c
float2 Affine2D_transformVector_cm(__constant Affine2D* a, float2 v)
{
	return ((float2)((a->xa.x*v.x + a->ya.x*v.y + a->ta.x),
	                  (a->xa.y*v.x + a->ya.y*v.y + a->ta.y)));
}
```

### lerp
Linear interpolation utility:
```c
float lerp(float n1, float n2, float a)
{
	return n1 + a * (n2 - n1);
}
```
