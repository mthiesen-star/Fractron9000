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

#include "cuda_interop.cuh"
#include "RandomMWC.cuh"
#include "Affine2D.cuh"

#define IterBlockSize 128
#define IteratorCount (IterBlockSize*iterBlockCount)
#define AALevel 2
#define SubPixelCount (AALevel*AALevel)
#define RasterBlockSize 8
#define MaxBranches 16
#define MaxFactors 48

#define WarmupIterationCount 32

#define Tone_C1 (1.0f/2.0f)
#define Tone_C2 (64.0f/1.0f)
#define PIf 3.14159265358f
#define PIo2f 1.57079632679f
#define InvPIo2f 0.636619772368f
//#define Epsilon 0.00000095367431640625f
#define Epsilon (4.7019774E-38f)


__constant__ int xRes;
__constant__ int yRes;
__constant__ int iterBlockCount;

__constant__ DeviceBuffer2D<float2> iterPosStateBuffer;
__constant__ DeviceBuffer2D<float4> iterColorStateBuffer;
__constant__ DeviceBuffer2D<uint4>  entropyXBuffer;
__constant__ DeviceBuffer2D<uint>   entropyCBuffer;
__constant__ DeviceBuffer2D<uint>   entropySeedBuffer;
__constant__ DeviceBuffer2D<UInt64> dotCountBuffer;
__constant__ DeviceBuffer2D<float>  peakDensityBuffer;

__constant__ UInt64* totalIterCountMem;
__constant__ UInt64* totalDotCountMem;
__constant__ float* densityMem;
__constant__ float* peakDensityMem;
__constant__ float* scaleConstantMem;

__constant__ DeviceBuffer2D<float4> accumBuffer;

__constant__ Affine2D vpsTransform; //transforms a point from world space to screen space

__constant__ float brightness;
__constant__ float invGamma;
__constant__ float vibrancy;
__constant__ float4 bgColor;

__constant__ uint     branchCount;
__constant__ uint     branchNormWeights[MaxBranches];
__constant__ float    branchColorWeights[MaxBranches];
__constant__ Affine2D branchPreTransforms[MaxBranches];
__constant__ Affine2D branchPostTransforms[MaxBranches];
__constant__ float    branchLumas[MaxBranches];
__constant__ float2   branchChromas[MaxBranches];
__constant__ float    branchFactors[MaxBranches*MaxFactors];

texture<float4, 2, cudaReadModeElementType> paletteTex;

__device__ float lerp( float n1, float n2, float a )
{
	return n1 + a * ( n2 - n1 );
}


__device__ void iterate(float2* pos, float4* color, uint branch, uint entropy, uint4* randXBuffer, uint* randCBuffer)
{
	#define AccumResult(i) result.x += factors[i]*nx; result.y += factors[i]*ny
	float tx,ty;
	float nx,ny;
	float* factors = branchFactors + branch*MaxFactors;
	
	float2 result;
	result.x = 0.0f;
	result.y = 0.0f;
	
	color->x = lerp(color->x, branchChromas[branch].x, branchColorWeights[branch]);
	color->y = lerp(color->y, branchChromas[branch].y, branchColorWeights[branch]);
	color->z = lerp(color->z, branchLumas[branch]    , branchColorWeights[branch]);
	
	Affine2D pre;
	Affine2D post;
	
	pre = branchPreTransforms[branch];
	post = branchPostTransforms[branch];
	
	pre.transformPoint(&tx, &ty, pos->x, pos->y);
	
	float theta = atan2f(tx,ty); 
	float rsq = tx*tx + ty*ty;
	float r = __fsqrt_ru(rsq);
	
	//Linear
	if(factors[0] != 0.0f)
	{
		nx = tx;
		ny = ty;
		AccumResult(0);
	}
	
	//Apophysis Sinusoidal
	if(factors[1] != 0.0f)
	{
		nx = __sinf(tx);
		ny = __sinf(ty);
		AccumResult(1);
	}
	
	//Apophysis Spherical
	if(factors[2] != 0.0f)
	{
		nx = tx / (rsq+Epsilon);
		ny = ty / (rsq+Epsilon);
		AccumResult(2);
	}

	//Apophysis Swirl
	if(factors[3] != 0.0f)
	{
		float j,k;
		__sincosf(rsq, &j, &k);
		nx = j*tx - k*ty;
		ny = k*tx + j*ty;
		AccumResult(3);
	}
	
	//Apophysis Horseshoe
	if(factors[4] != 0.0f)
	{
		nx = (tx*tx - ty*ty)/(r+Epsilon);
		ny = (2.0f*tx*ty)/(r+Epsilon);
		AccumResult(4);
	}
	
	//Apophysis Polar
	if(factors[5] != 0.0f)
	{
		nx = theta / PIf;
		ny = r - 1.0f;
		AccumResult(5);
	}
	
	//Apophysis Handkerchief
	if(factors[6] != 0.0f)
	{
		nx = r * __sinf(theta+r);
		ny = r * __cosf(theta-r);
		AccumResult(6);
	}
	
	//Apophysis Heart
	if(factors[7] != 0.0f)
	{
		nx =  r*__sinf(theta*r);
		ny = -r*__cosf(theta*r);
		AccumResult(7);
	}
	
	//Apophysis Disc
	if(factors[8] != 0.0f)
	{
		nx =  theta*__sinf(PIf*r)/PIf;
		ny =  theta*__cosf(PIf*r)/PIf;
		AccumResult(8);
	}
	
	//Apophysis Spiral
	if(factors[9] != 0.0f)
	{
		nx =  (__cosf(theta) + __sinf(r))/(r+Epsilon);
		ny =  (__sinf(theta) - __cosf(r))/(r+Epsilon);
		AccumResult(9);
	}
	
	//Apophysis Hyperbolic
	if(factors[10] != 0.0f)
	{
		nx =  ty/(rsq+Epsilon);
		ny =  tx;
		AccumResult(10);
	}
	
	//Apophysis Diamond
	if(factors[11] != 0.0f)
	{
		nx =  ty / (r+Epsilon) * __cosf(r);
		ny =  tx / (r+Epsilon) * __sinf(r);
		AccumResult(11);
	}
	
	//Apophysis Ex
	if(factors[12] != 0.0f)
	{
		float j,k;
		j = __cosf(r)*ty + __sinf(r)*tx;
		nx =  j*j*j/(rsq+Epsilon);
		k = __sinf(r)*ty + __cosf(r)*tx;
		ny =  k*k*k/(rsq+Epsilon);
		AccumResult(12);
	}
	
	//Apophysis Julia
	if(factors[13] != 0.0f)
	{
		float j,k;
		__sincosf(0.5f*theta + PIf*(float)(entropy&0x0001), &j, &k);
		nx = sqrtf(r) * k;
		ny = sqrtf(r) * j;
		AccumResult(13);
	}
	
	//Apophysis Bent
	if(factors[14] != 0.0f)
	{
		nx = tx >= 0 ? tx : 2*tx;
		ny = ty >= 0 ? ty : ty*0.5f;
		AccumResult(14);
	}
	
	//Apophysis Waves
	if(factors[15] != 0.0f)
	{
		nx = tx + pre.ya.x * __sinf(ty/(pre.ta.x*pre.ta.x + Epsilon));
		ny = ty + pre.ya.y * __sinf(tx/(pre.ta.y*pre.ta.y + Epsilon));
		AccumResult(15);
	}
	
	//Apophysis Fisheye
	if(factors[16] != 0.0f)
	{
		float k;
		k = 2.0f/(r+1.0f);
		nx = k*ty;
		ny = k*tx;
		AccumResult(16);
	}
	
	//Apophysis Popcorn
	if(factors[17] != 0.0f)
	{
		nx = tx + pre.ta.x*__sinf(__tanf(3.0f*ty));
		ny = ty + pre.ta.y*__sinf(__tanf(3.0f*tx));
		AccumResult(17);
	}
	
	//Apophysis Exponential
	if(factors[18] != 0.0f)
	{
		float k;
		k = __expf(tx - 1.0f);
		__sincosf(PIf*ty, &nx, &ny);
		nx *= k;
		ny *= k;
		AccumResult(18);
	}
	
	//Apophysis Power
	if(factors[19] != 0.0f)
	{
		float k;
		nx = tx/(r+Epsilon);
		ny = ty/(r+Epsilon);
		k = __powf(r, ny);
		nx *= k;
		ny *= k;
		AccumResult(19);
	}
	
	//Apophysis Cosine
	if(factors[20] != 0.0f)
	{
		float sinh_ty, cosh_ty;
		cosh_ty = 0.5f*(__expf(ty) - __expf(-ty));
		sinh_ty = 0.5f*(__expf(ty) + __expf(-ty));
		nx =       __cosf(PIf*tx)*cosh_ty;
		ny = -1.0f*__sinf(PIf*tx)*sinh_ty;
		AccumResult(20);
	}
	
	//Apophysis Eyefish
	if(factors[21] != 0.0f)
	{
		float k;
		k = 2.0f/(r+1.0f);
		nx = k*tx;
		ny = k*ty;
		AccumResult(21);
	}
	
	//Apophysis Bubble
	if(factors[22] != 0.0f)
	{
		float k;
		k = 4.0f / (rsq + 4.0f);
		nx = k*tx;
		ny = k*ty;
		AccumResult(22);
	}
	
	//Apophysis Cylinder
	if(factors[23] != 0.0f)
	{
		nx = __sinf(tx);
		ny = ty;
		AccumResult(23);
	}
	
	//Apophysis Noise
	if(factors[24] != 0.0f)
	{
		float p1,p2;
		
		uint rnd = MWC_rand(randXBuffer, randCBuffer, threadIdx.x);
		p1 = (float)(rnd>>16) / 65536.0f;
		p2 = 2.0f*PIf*(float)(rnd&0x0000FFFF) / 65536.0f;
		
		__sincosf(p2, &ny, &nx);
		nx *= p1*tx;
		ny *= p1*ty;

		AccumResult(24);
	}
	
	//Apophysis Blur
	if(factors[25] != 0.0f)
	{
		float br,bt;
		
		uint rnd = MWC_rand(randXBuffer, randCBuffer, threadIdx.x);
		br = (float)(rnd>>16) / 65536.0f;
		bt = 2.0f * PIf * (float)(rnd&0x0000FFFF) / 65536.0f;
		
		__sincosf(bt, &ny, &nx);
		nx *= br;
		ny *= br;

		AccumResult(25);
	}
	
	//Apophysis Gaussian Blur
	if(factors[26] != 0.0f)
	{
		float br,bt;
		uint rnd;
		uint sum = 0;
		
		rnd = MWC_rand(randXBuffer, randCBuffer, threadIdx.x);
		sum += (rnd&0x0000FFFF) + (rnd>>16);
		rnd = MWC_rand(randXBuffer, randCBuffer, threadIdx.x);
		sum += (rnd&0x0000FFFF) + (rnd>>16);
		br = (float)sum / 65536.0f - 2.0f;
		bt = 2.0f * PIf * (float)(entropy&0x0000FFFF) / 65536.0f;
		
		__sincosf(bt, &ny, &nx);
		nx *= br;
		ny *= br;

		AccumResult(26);
	}
	
	//Fractron Orb
	if(factors[27] != 0.0f)
	{
		float k;
		k = 2.0f/(rsq+1.0f);
		nx = k*tx;
		ny = k*ty;
		AccumResult(27);
	}
	
	//Fractron Ripple
	if(factors[28] != 0.0f)
	{
		float k;
		k = __sinf(r*PIo2f);
		nx = k*tx;
		ny = k*ty;
		AccumResult(28);
	}
	
	//Fractron Bulge
	if(factors[29] != 0.0f)
	{
		float k;
		k = (r+1.0f)/(r+Epsilon);
		
		nx = k*tx;
		ny = k*ty;
		AccumResult(29);
	}
	
	post.transformPoint(&(result.x), &(result.y), result.x, result.y);
	
	#undef AccumResult
	
	*pos = result;
}

//chooses a branch index randomly based on the branch weights
__device__ uint chooseRandomBranch(uint rnd)
{
	uint branch = 0;

	#pragma unroll
	for(int i = 0; i < MaxBranches; i++)
		if(rnd >= branchNormWeights[i])
			branch++;
	
	//while(branch < branchCount-1 && rnd >= branchNormWeights[branch])
	//	branch++;
	return branch;
}

extern "C"
__global__ void init_iterators_kernel()
{
	#define IterIdx (blockDim.x*blockIdx.x + threadIdx.x)
	
	__shared__ uint4 randXBuffer   [IterBlockSize];
	__shared__ uint  randCBuffer   [IterBlockSize];
	__shared__ uint  randSeedBuffer[IterBlockSize];
	
	randXBuffer   [threadIdx.x] = make_uint4(0,0,0,0);
	randCBuffer   [threadIdx.x] = 0;
	randSeedBuffer[threadIdx.x] = entropySeedBuffer[blockIdx.x][threadIdx.x];
	
	MWC_seed(randXBuffer, randCBuffer, threadIdx.x, randSeedBuffer);
	
	entropyXBuffer[blockIdx.x][threadIdx.x] = randXBuffer[threadIdx.x];
	entropyCBuffer[blockIdx.x][threadIdx.x] = randCBuffer[threadIdx.x];
}

extern "C"
__global__ void reset_iterators_kernel(DeviceBuffer2D<uint> output)
{
	int iter;
	uint branch;
	float rndf;
	float2 pos;
	float4 color;
	
	__shared__ uint4 randXBuffer   [IterBlockSize];
	__shared__ uint  randCBuffer   [IterBlockSize];
	__shared__ uint  randSeedBuffer[IterBlockSize];
	randXBuffer   [threadIdx.x] = entropyXBuffer   [blockIdx.x][threadIdx.x];
	randCBuffer   [threadIdx.x] = entropyCBuffer   [blockIdx.x][threadIdx.x];
	randSeedBuffer[threadIdx.x] = entropySeedBuffer[blockIdx.x][threadIdx.x];
	
	MWC_seed(randXBuffer, randCBuffer, threadIdx.x, randSeedBuffer);


	rndf = (float)(MWC_rand(randXBuffer, randCBuffer, threadIdx.x) & 0x000FFFFF) / 1048575.0f;
	pos.x = 2.0f*rndf - 1.0f;
	rndf = (float)(MWC_rand(randXBuffer, randCBuffer, threadIdx.x) & 0x000FFFFF) / 1048575.0f;
	pos.y = 2.0f*rndf - 1.0f;
	
	color.x = 0.5f;
	color.y = 0.5f;
	color.z = 0.5f;
	color.w = 0.0f;
	
	for(iter = 0; iter < WarmupIterationCount; iter++)
	{
		uint rnd = MWC_rand(randXBuffer, randCBuffer, threadIdx.x);
		
		branch = chooseRandomBranch(rnd & 0x0000FFFF); //the low entropy bits are for branch selection
		
		iterate(&pos, &color, branch, (rnd>>16), randXBuffer, randCBuffer);  //the extra entropy bits are for variations
	}

	iterPosStateBuffer  [blockIdx.x][threadIdx.x] = pos;
	iterColorStateBuffer[blockIdx.x][threadIdx.x] = color;
	entropyXBuffer   [blockIdx.x][threadIdx.x] = randXBuffer[threadIdx.x];
	entropyCBuffer   [blockIdx.x][threadIdx.x] = randCBuffer[threadIdx.x];
	dotCountBuffer   [blockIdx.x][threadIdx.x] = 0;
	peakDensityBuffer[blockIdx.x][threadIdx.x] = 0.0f;
	
	if(threadIdx.x == 0 && blockIdx.x == 0) //the first thread will update the total iteration count
	{
		*totalIterCountMem = 0;
	}
}

extern "C"
__global__ void reset_output_kernel(DeviceBuffer2D<uint> glOutputBuffer)
{
	int x = blockDim.x*blockIdx.x + threadIdx.x;
	int y = blockDim.y*blockIdx.y + threadIdx.y;
	if(x >= xRes || y >= yRes) return;
	
	accumBuffer[y][4*x+0] = make_float4(0.0f,0.0f,0.0f,0.0f);
	accumBuffer[y][4*x+1] = make_float4(0.0f,0.0f,0.0f,0.0f);
	accumBuffer[y][4*x+2] = make_float4(0.0f,0.0f,0.0f,0.0f);
	accumBuffer[y][4*x+3] = make_float4(0.0f,0.0f,0.0f,0.0f);
	
	glOutputBuffer[y][x] = 0xFF000000;
}

extern "C"
__global__ void iterate_kernel(uint iterCount)
{

	//float r;
	float2 pos;
	float4 color;
	float4 sample;
	
	float4 mem;
	float2 screenPos;
	uint branch;
	UInt64 dotCount;
	float peakCount;
	int xa,ya;
	int x;
	int y;
	int si;
	uint iter;
	
	dotCount = dotCountBuffer[blockIdx.x][threadIdx.x];
	peakCount = peakDensityBuffer[blockIdx.x][threadIdx.x];
	
	__shared__ uint4 randXBuffer[IterBlockSize];
	__shared__ uint  randCBuffer[IterBlockSize];
	randXBuffer[threadIdx.x] = entropyXBuffer[blockIdx.x][threadIdx.x];
	randCBuffer[threadIdx.x] = entropyCBuffer[blockIdx.x][threadIdx.x];
	//MWC_seed(randXBuffer, randCBuffer, threadIdx.x, seed + idx);

	pos =   iterPosStateBuffer[blockIdx.x][threadIdx.x];
	color = iterColorStateBuffer[blockIdx.x][threadIdx.x];

	for(iter = 0; iter < iterCount; iter++)
	{
		uint rnd = MWC_rand(randXBuffer, randCBuffer, threadIdx.x);

		branch = chooseRandomBranch(rnd & 0x0000FFFF); //the low entropy bits are for branch selection

		iterate(&pos, &color, branch, (rnd>>16), randXBuffer, randCBuffer); //the extra entropy is for variations
		
		vpsTransform.transformPoint(&screenPos.x, &screenPos.y, pos.x, pos.y);
		xa = 2.0f*screenPos.x;
		ya = 2.0f*screenPos.y;
		x = xa >> 1;
		y = ya >> 1;
		
		if(x >= 0 && x < xRes && y >= 0 && y < yRes)
		{
			si = ((ya&1)<<1) | (xa&1); //calc subpixel index
			
			//sample the color from the palette
			sample = tex2D(paletteTex, color.x, color.y);
			
			//accumulate the histogram buffer
			//this is not actually thread safe, but hopefully it wont screw up the counts
			//enough to trash the image
			mem = accumBuffer[y][4*x+si];
			mem.x += sample.x * color.z;
			mem.y += sample.y * color.z;
			mem.z += sample.z * color.z;
			mem.w += 1.0f;
			accumBuffer[y][4*x+si] = mem;
						
			dotCount++;
			peakCount = fmaxf(peakCount, mem.w);
		}
	}

	iterPosStateBuffer  [blockIdx.x][threadIdx.x] = pos;
	iterColorStateBuffer[blockIdx.x][threadIdx.x] = color;
	entropyXBuffer   [blockIdx.x][threadIdx.x] = randXBuffer[threadIdx.x];
	entropyCBuffer   [blockIdx.x][threadIdx.x] = randCBuffer[threadIdx.x];
	dotCountBuffer   [blockIdx.x][threadIdx.x] = dotCount;
	peakDensityBuffer[blockIdx.x][threadIdx.x] = peakCount;
	
	if(threadIdx.x == 0 && blockIdx.x == 0) //the first thread will update the total iteration count
	{
		*totalIterCountMem += (UInt64)(IterBlockSize*iterBlockCount*iterCount);
	}
	
}

extern "C"
__global__ void update_stats_kernel()
{
	if(threadIdx.x == 0 && threadIdx.y == 0)
	{
		UInt64 totalIterationCount = *totalIterCountMem;
		
		UInt64 totalDotCount = 0;
		float peakDensity = 0;
		for(int row = 0; row < iterBlockCount; row++)
		{
			for(int col = 0; col < IterBlockSize; col++)
			{
				totalDotCount += dotCountBuffer[row][col];
				peakDensity = fmax(peakDensity, peakDensityBuffer[row][col]);
			}
		}
		float totalSubPixels = (float)(xRes*yRes*SubPixelCount);
		float density = (float)totalDotCount / totalSubPixels;
		
		float invPixArea = fabsf(vpsTransform.xa.x*vpsTransform.ya.y - vpsTransform.xa.y*vpsTransform.ya.x);
		//float scaleConstant = totalSubPixels / (float)totalDotCount;
		float scaleConstant = Tone_C2*(invPixArea*(float)SubPixelCount)/(float)totalIterationCount;
		
		*totalDotCountMem = totalDotCount;
		*densityMem = fmax(density,Epsilon);
		*peakDensityMem = fmax(peakDensity,Epsilon);
		*scaleConstantMem = fmax(scaleConstant,Epsilon);
	}
}

extern "C"
__device__ float4 tonemap(float4 rawPix, float scaleConstant)
{
	float z, gammaFactor;
	float4 logPix;
	float4 result;        //the tonemapped pixel
	
	float ka = Tone_C1 * brightness * __log10f(1.0f+rawPix.w*scaleConstant) / rawPix.w;
		
	logPix.x = rawPix.x*ka;
	logPix.y = rawPix.y*ka;
	logPix.z = rawPix.z*ka;
	logPix.w = rawPix.w*ka;
	
	z = __powf(logPix.w,invGamma);
	gammaFactor = z / logPix.w;
	
	result.x = __saturatef(lerp(__powf(logPix.x,invGamma), gammaFactor*logPix.x, vibrancy));
	result.y = __saturatef(lerp(__powf(logPix.y,invGamma), gammaFactor*logPix.y, vibrancy));
	result.z = __saturatef(lerp(__powf(logPix.z,invGamma), gammaFactor*logPix.z, vibrancy));
	result.w = __saturatef(z);
	
	return result;
}

extern "C"
__global__ void update_output_kernel(DeviceBuffer2D<uint> glOutputBuffer)
{
	//__shared__ float density;
	//__shared__ float peak_density;
	__shared__ float scaleConstant;
	
	int x = blockDim.x*blockIdx.x + threadIdx.x;
	int y = blockDim.y*blockIdx.y + threadIdx.y;
	if(x >= xRes || y >= yRes) return;
	
	
	if(threadIdx.x == 0 && threadIdx.y == 0)
	{
		//density = *densityMem;
		//peak_density = *peakDensityMem;
		scaleConstant = *scaleConstantMem;
	}
	__syncthreads();
	
	uint4 iPix;
	float4 pix;
	float4 acc;
	//float4 result;
	acc = tonemap( accumBuffer[y][4*x+0], scaleConstant);
		
	pix = tonemap( accumBuffer[y][4*x+1], scaleConstant);
	acc.x += pix.x; acc.y += pix.y; acc.z += pix.z; acc.w += pix.w;
	
	pix = tonemap( accumBuffer[y][4*x+2], scaleConstant);
	acc.x += pix.x; acc.y += pix.y; acc.z += pix.z; acc.w += pix.w;
	
	pix = tonemap( accumBuffer[y][4*x+3], scaleConstant);
	acc.x += pix.x; acc.y += pix.y; acc.z += pix.z; acc.w += pix.w;
	
	acc.x /= 4.0f;
	acc.y /= 4.0f;
	acc.z /= 4.0f;
	acc.w /= 4.0f;
	
	//result.x = lerp(bgColor.x, acc.x, acc.w);
	//result.y = lerp(bgColor.y, acc.y, acc.w);
	//result.z = lerp(bgColor.z, acc.z, acc.w);
	//result.w = 1.0f;
	
	iPix.x = (uint)(255.0f*acc.x);
	iPix.y = (uint)(255.0f*acc.y);
	iPix.z = (uint)(255.0f*acc.z);
	iPix.w = (uint)(255.0f*acc.w);
	
	glOutputBuffer[y][x] = iPix.w << 24 | iPix.z << 16 | iPix.y << 8 | iPix.x;
}
