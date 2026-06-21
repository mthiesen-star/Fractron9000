//
//------------------< k e r n e l s . c >---------------------
//

#include "config.h"
#include "data_types.h"
#include "random_mwc.h"
#include "variations.h"

#define AccumPtr(x,y,sub_x,sub_y) (accumBuffer + 4*(((y)*xRes) + (x)) + 2*(sub_y) + (sub_x))
#define AccumRef(x,y,sub_x,sub_y) (*(AccumPtr((x),(y),(sub_x),(sub_y))))

#define OutputPtr(x,y) (outputBuffer + ((y)*xRes) + (x))
#define OutputRef(x,y) (*(OutputPtr((x),(y))))

#ifdef _CUDA_SRC_
CONST_MEM    FractalInfo      fractalInfo[1];
CONST_MEM    BranchInfo       branchInfo[MaxBranches];
CONST_MEM    float            variWeightBuffer[MaxBranches*MaxVariations];

texture<float4, 2, cudaReadModeElementType> paletteTex;
#endif

KERNEL void init_iterators_kernel(
  GLOBAL_MEM IterStatEntry    iterStatBuffer[],
  GLOBAL_MEM GlobalStatEntry* globalStatBuffer,
  GLOBAL_MEM uint4 entropyXBuffer[],
  GLOBAL_MEM uint  entropyCBuffer[],
  GLOBAL_MEM uint  entropySeedBuffer[]
){
  LOCAL_MEM uint4 randXBuffer   [IterGroupSize];
  LOCAL_MEM uint  randCBuffer   [IterGroupSize];
  LOCAL_MEM uint  randSeedBuffer[IterGroupSize];
    
  randXBuffer[LOCAL_ID_X]    = make_uint4(0,0,0,0);
  randCBuffer[LOCAL_ID_X]    = 0;
  randSeedBuffer[LOCAL_ID_X] = entropySeedBuffer[GLOBAL_ID_X];
  
  MWC_seed(randXBuffer+LOCAL_ID_X, randCBuffer+LOCAL_ID_X, randSeedBuffer[LOCAL_ID_X]);
  
  entropyXBuffer[GLOBAL_ID_X] = randXBuffer[LOCAL_ID_X];
  entropyCBuffer[GLOBAL_ID_X] = randCBuffer[LOCAL_ID_X];
  
  iterStatBuffer[GLOBAL_ID_X].dotCount = 0;
  iterStatBuffer[GLOBAL_ID_X].peakDensity = 0.0f;
  
  if(GLOBAL_ID_X == 0) //the first thread will reset the global stats
  {
    globalStatBuffer->iterCount =     (UInt64)0;
    globalStatBuffer->dotCount =      (UInt64)0;
    globalStatBuffer->density  =      0.0f;
    globalStatBuffer->peakDensity =   0.0f;
    globalStatBuffer->scaleConstant = 0.0f;
  }
}

KERNEL void reset_iterators_kernel(
               uint             xRes,
               uint             yRes,
#ifdef _OPENCL_SRC_
  CONST_MEM    FractalInfo*     fractalInfo,
  CONST_MEM    BranchInfo       branchInfo[],
  CONST_MEM    float            variWeightBuffer[],
#endif
  GLOBAL_MEM   float2           iterPosStateBuffer[],
  GLOBAL_MEM   float2           iterColorStateBuffer[],
  GLOBAL_MEM   IterStatEntry    iterStatBuffer[],
  GLOBAL_MEM   GlobalStatEntry* globalStatBuffer,
  GLOBAL_MEM   uint4            entropyXBuffer[],
  GLOBAL_MEM   uint             entropyCBuffer[],
  GLOBAL_MEM   uint             entropySeedBuffer[]
){
  LOCAL_MEM uint4 randXBuffer   [IterGroupSize];
  LOCAL_MEM uint  randCBuffer   [IterGroupSize];
  LOCAL_MEM uint  randSeedBuffer[IterGroupSize];
    
  iterStatBuffer[GLOBAL_ID_X].dotCount = 0;
  iterStatBuffer[GLOBAL_ID_X].peakDensity = 0.0f;
  
  if(GLOBAL_ID_X == 0) //the first thread will reset the global stats
  {
    globalStatBuffer->iterCount =     (UInt64)0;
    globalStatBuffer->dotCount =      (UInt64)0;
    globalStatBuffer->density  =      0.0f;
    globalStatBuffer->peakDensity =   0.0f;
    globalStatBuffer->scaleConstant = 0.0f;
  }
  
  randXBuffer[LOCAL_ID_X]    = make_uint4(0,0,0,0);
  randCBuffer[LOCAL_ID_X]    = 0;
  randSeedBuffer[LOCAL_ID_X] = entropySeedBuffer[GLOBAL_ID_X];
  
  MWC_seed(randXBuffer+LOCAL_ID_X, randCBuffer+LOCAL_ID_X, randSeedBuffer[LOCAL_ID_X]);
  
  float2 pos = MWC_rand_float2(randXBuffer+LOCAL_ID_X, randCBuffer+LOCAL_ID_X);
  float2 color = make_float2(0.5f, 0.5f);
  
  for(int iter = 0; iter < (int)WarmupIterationCount; iter++)
  {
    uint rnd = MWC_rand(randXBuffer+LOCAL_ID_X, randCBuffer+LOCAL_ID_X);
    uint bi = chooseRandomBranch(rnd & 0x0000FFFF, fractalInfo->branchCount, branchInfo);                //the low entropy bits are for branch selection
    iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*MaxVariations), randXBuffer+LOCAL_ID_X, randCBuffer+LOCAL_ID_X); //the extra entropy bits are for variations
  }
  
  iterPosStateBuffer[GLOBAL_ID_X] = pos;
  iterColorStateBuffer[GLOBAL_ID_X] = color;
}

KERNEL void reset_output_kernel(
  int xRes,
  int yRes,
  GLOBAL_MEM float4  accumBuffer[],
  GLOBAL_MEM uint    outputBuffer[]
)
{
  int x = GLOBAL_ID_X;
  int y = GLOBAL_ID_Y;

  if(x < xRes && y < yRes)
  {
    AccumRef(x,y,0,0) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    AccumRef(x,y,0,1) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    AccumRef(x,y,1,0) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    AccumRef(x,y,1,1) = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    OutputRef(x,y) = 0x00000000;
  }
}

#ifdef _OPENCL_SRC_
#ifdef _LOW_PROFILE_
DEVICE float4 samplePaletteBuffer(float2 v, GLOBAL_PARAM uchar4 paletteBuffer[], uint width, uint height)
{
  int x = clamp((int)(v.x * (float)width), 0, (int)width-1);
  int y = clamp((int)(v.y * (float)height), 0, (int)height-1);
  uchar4 pix = paletteBuffer[y*width+x];
  return make_float4((float)pix.x / 255.0f, (float)pix.y / 255.0f, (float)pix.z / 255.0f, (float)pix.w / 255.0f);
}
#endif
#endif


KERNEL void iterate_kernel(
               uint             xRes,
               uint             yRes,
#ifdef _OPENCL_SRC_
  CONST_MEM    FractalInfo*     fractalInfo,
  CONST_MEM    BranchInfo       branchInfo[],
  CONST_MEM    float            variWeightBuffer[],
#endif
  GLOBAL_MEM   float2           iterPosStateBuffer[],
  GLOBAL_MEM   float2           iterColorStateBuffer[],
  GLOBAL_MEM   IterStatEntry    iterStatBuffer[],
  GLOBAL_MEM   GlobalStatEntry* globalStatBuffer,
  GLOBAL_MEM   uint4            entropyXBuffer[],
  GLOBAL_MEM   uint             entropyCBuffer[],
  GLOBAL_MEM   float4           accumBuffer[],
#ifdef _OPENCL_SRC_
#ifdef _LOW_PROFILE_
               uint             paletteWidth,
               uint             paletteHeight,
  GLOBAL_MEM   uchar4           paletteBuffer[],
#else
  __read_only  image2d_t        palette,
               sampler_t        paletteSampler,
#endif
#endif
               uint             iterCount
){    
  LOCAL_MEM uint4 randXBuffer   [IterGroupSize];
  LOCAL_MEM uint  randCBuffer   [IterGroupSize];
  
  float2 pos =        iterPosStateBuffer[GLOBAL_ID_X];
  float2 color =      iterColorStateBuffer[GLOBAL_ID_X];
  UInt64 dotCount =   iterStatBuffer[GLOBAL_ID_X].dotCount;
  float peakDensity = iterStatBuffer[GLOBAL_ID_X].peakDensity;
  randXBuffer[LOCAL_ID_X] = entropyXBuffer[GLOBAL_ID_X];
  randCBuffer[LOCAL_ID_X] = entropyCBuffer[GLOBAL_ID_X];
  int gid = GLOBAL_ID_X;
  int lid = LOCAL_ID_X;

  
  for(int iter = 0; iter < (int)iterCount; iter++)
  {
    uint rnd = MWC_rand(randXBuffer+lid, randCBuffer+lid);
    uint bi = chooseRandomBranch(rnd & 0x0000FFFF, fractalInfo->branchCount, branchInfo);                  //the low entropy bits are for branch selection
    
    iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*MaxVariations), randXBuffer+lid, randCBuffer+lid); //the extra entropy bits are for variations
    
    float2 screenPos = Affine2D_transformVector_cm(&(fractalInfo->vpsTransform), pos);
    
    int xa = (int)(2.0f*screenPos.x);
    int ya = (int)(2.0f*screenPos.y);
    int x  = xa >> 1;
    int y  = ya >> 1;
    
    if(x >= 0 && x < xRes && y >= 0 && y < yRes)
    {
#ifdef _OPENCL_SRC_
#ifdef _LOW_PROFILE_
      float4 sample = samplePaletteBuffer(color, paletteBuffer, paletteWidth, paletteHeight);
#else
      float4 sample = read_imagef(palette, paletteSampler, color);
#endif
#endif
#ifdef _CUDA_SRC_
      float4 sample = tex2D(paletteTex, color.x, color.y);
#endif
      //accumulate the histogram buffer
      //this is not actually thread safe, but hopefully it wont screw up the counts
      //enough to trash the image
      float4 mem = AccumRef(x,y,(xa&1),(ya&1));
      mem.x += sample.x;
      mem.y += sample.y;
      mem.z += sample.z;
      mem.w += 1.0f;
      AccumRef(x,y,(xa&1),(ya&1)) = mem;
      
      dotCount++;
      peakDensity = math_fmax(peakDensity, mem.w);
    }
  }
  
  
  iterPosStateBuffer[gid]   = pos;
  iterColorStateBuffer[gid] = color;
  iterStatBuffer[gid].dotCount = dotCount;
  iterStatBuffer[gid].peakDensity = peakDensity;
  entropyXBuffer[gid] = randXBuffer[lid];
  entropyCBuffer[gid] = randCBuffer[lid];
  
  if(gid == 0)
  {
    globalStatBuffer->iterCount += (UInt64)(GLOBAL_SIZE_X * iterCount);
  }
}

KERNEL void iterate_kernel_hybrid(
               uint             xRes,
               uint             yRes,
#ifdef _OPENCL_SRC_
  CONST_MEM    FractalInfo*     fractalInfo,
  CONST_MEM    BranchInfo       branchInfo[],
  CONST_MEM    float            variWeightBuffer[],
#endif
  GLOBAL_MEM   float2           iterPosStateBuffer[],
  GLOBAL_MEM   float2           iterColorStateBuffer[],
  GLOBAL_MEM   IterStatEntry    iterStatBuffer[],
  GLOBAL_MEM   GlobalStatEntry* globalStatBuffer,
  GLOBAL_MEM   uint4            entropyXBuffer[],
  GLOBAL_MEM   uint             entropyCBuffer[],
  GLOBAL_MEM   float4           dotBuffer[],
               uint             iterCount
){  
  LOCAL_MEM uint4 randXBuffer[IterGroupSize];
  LOCAL_MEM uint  randCBuffer[IterGroupSize];
  
  int gid = GLOBAL_ID_X;
  int lid = LOCAL_ID_X;
  
  float2 pos =       iterPosStateBuffer[gid];
  float2 color =     iterColorStateBuffer[gid];
  UInt64 dotCount =  iterStatBuffer[gid].dotCount;
  randXBuffer[lid] = entropyXBuffer[gid];
  randCBuffer[lid] = entropyCBuffer[gid];
  
  for(int iter = 0; iter < (int)iterCount; iter++)
  {
    uint rnd = MWC_rand(randXBuffer+lid, randCBuffer+lid);
    uint bi = chooseRandomBranch(rnd & 0x0000FFFF, fractalInfo->branchCount, branchInfo);                  //the low entropy bits are for branch selection
    
    iterate(&pos, &color, (rnd>>16), branchInfo+bi, variWeightBuffer+(bi*MaxVariations), randXBuffer+lid, randCBuffer+lid); //the extra entropy bits are for variations
    
    //write the dot into the dot buffer
    dotBuffer[iter * GLOBAL_SIZE_X + gid] = make_float4(pos.x, pos.y, color.x, color.y);
    
    //if the dot is on screen, then increment the visible dot count
    float2 screenPos = Affine2D_transformVector_cm(&(fractalInfo->vpsTransform), pos);
    if(screenPos.x >= 0.0f && screenPos.x < (float)xRes && screenPos.y >= 0.0f && screenPos.y < (float)yRes)
    {
      dotCount++;
    }
  }
  
  iterPosStateBuffer[gid]   = pos;
  iterColorStateBuffer[gid] = color;
  iterStatBuffer[gid].dotCount = dotCount;
  iterStatBuffer[gid].peakDensity = 0.0f;
  entropyXBuffer[gid] = randXBuffer[lid];
  entropyCBuffer[gid] = randCBuffer[lid];
  
  if(gid == 0)
  {
    globalStatBuffer->iterCount += (UInt64)(GLOBAL_SIZE_X * iterCount);
  }
}


KERNEL void update_stats_kernel(
               uint             xRes,
               uint             yRes,
#ifdef _OPENCL_SRC_
  CONST_MEM    FractalInfo*     fractalInfo,
#endif
               uint             iteratorCount,
  GLOBAL_MEM   IterStatEntry    iterStatBuffer[],
  GLOBAL_MEM   GlobalStatEntry* globalStatBuffer 
){
  if(GLOBAL_ID_X == 0)
  {
    UInt64 totalDotCount = 0;
    float peakDensity = 0.0f;
    for(int i = 0; i < (int)iteratorCount; i++)
    {
      totalDotCount += iterStatBuffer[i].dotCount;
      peakDensity = math_fmax(peakDensity, iterStatBuffer[i].peakDensity);
    }
    UInt64 totalIterationCount = globalStatBuffer->iterCount;
    float totalSubPixels = (float)(xRes*yRes*SubPixelCount);
    float density = (float)totalDotCount / totalSubPixels;
    float invPixArea = math_fabs((fractalInfo->vpsTransform.xa.x)*(fractalInfo->vpsTransform.ya.y) - (fractalInfo->vpsTransform.xa.y)*(fractalInfo->vpsTransform.ya.x));
    float scaleConstant = Tone_C2*(invPixArea*(float)SubPixelCount)/(float)totalIterationCount;
    
    //globalStatBuffer->iterCount = 9876543;
    globalStatBuffer->dotCount = totalDotCount;
    globalStatBuffer->density = math_fmax(density, Epsilon);
    globalStatBuffer->peakDensity = math_fmax(peakDensity, Epsilon);
    globalStatBuffer->scaleConstant = math_fmax(scaleConstant, Epsilon);
  }
}

DEVICE float4 tonemap(CONST_PARAM FractalInfo* fractal, float4 rawPix, float scaleConstant)
{
  float4 logPix;
  float4 result;        //the tonemapped pixel
  
  if(rawPix.w <= 0.5f) //bail if alpha is too small to avoid dividing by zero
    return make_float4(0.0f, 0.0f, 0.0f, 0.0f);
  
  logPix.w = Tone_C1 * fractal->brightness * fast_log10(1.0f+rawPix.w*scaleConstant);
  float ka = logPix.w / rawPix.w;
  
  logPix.x = ka * rawPix.x;
  logPix.y = ka * rawPix.y;
  logPix.z = ka * rawPix.z;
  
  float z = fast_pow(logPix.w,fractal->invGamma);
  float gammaFactor = z / logPix.w;
  
  result.x = fast_saturate(lerp(fast_pow(logPix.x,fractal->invGamma), gammaFactor*logPix.x, fractal->vibrancy));
  result.y = fast_saturate(lerp(fast_pow(logPix.y,fractal->invGamma), gammaFactor*logPix.y, fractal->vibrancy));
  result.z = fast_saturate(lerp(fast_pow(logPix.z,fractal->invGamma), gammaFactor*logPix.z, fractal->vibrancy));
  result.w = fast_saturate(z);
  
  return result;
}

KERNEL void update_output_kernel(
             uint             xRes,
             uint             yRes,
#ifdef _OPENCL_SRC_
  CONST_MEM  FractalInfo*     fractalInfo,
#endif
  GLOBAL_MEM GlobalStatEntry* globalStatBuffer,
  GLOBAL_MEM float4           accumBuffer[],
  GLOBAL_MEM uint             outputBuffer[]
){
  uint4 iPix;
  float4 pix;
  
  int x = GLOBAL_ID_X;
  int y = GLOBAL_ID_Y;
  
  float4 acc = make_float4(0.0f, 0.0f, 0.0f, 0.0f);
    
  if(x < xRes && y < yRes)
  {
    float scaleConstant = globalStatBuffer->scaleConstant;
    
    pix = tonemap( fractalInfo, AccumRef(x,y,0,0), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
      
    pix = tonemap( fractalInfo, AccumRef(x,y,0,1), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
    
    pix = tonemap( fractalInfo, AccumRef(x,y,1,0), scaleConstant);
    acc.x += pix.w*pix.x;
    acc.y += pix.w*pix.y;
    acc.z += pix.w*pix.z;
    acc.w += pix.w;
    
    pix = tonemap( fractalInfo, AccumRef(x,y,1,1), scaleConstant);
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
        
    OutputRef(x,y) = iPix.w << 24 | iPix.z << 16 | iPix.y << 8 | iPix.x;
  }
}
/*
 //old tonemapping
DEVICE float4 tonemap(CONST_PARAM FractalInfo* fractal, float4 rawPix, float scaleConstant)
{
  float z, gammaFactor;
  float4 logPix;
  float4 result;        //the tonemapped pixel
  
  float ka = Tone_C1 * fractal->brightness * fast_log10(1.0f+rawPix.w*scaleConstant) / rawPix.w;
    
  logPix.x = rawPix.x*ka;
  logPix.y = rawPix.y*ka;
  logPix.z = rawPix.z*ka;
  logPix.w = rawPix.w*ka;
  
  z = fast_pow(logPix.w,fractal->invGamma);
  gammaFactor = z / logPix.w;
  
  result.x = fast_saturate(lerp(fast_pow(logPix.x,fractal->invGamma), gammaFactor*logPix.x, fractal->vibrancy));
  result.y = fast_saturate(lerp(fast_pow(logPix.y,fractal->invGamma), gammaFactor*logPix.y, fractal->vibrancy));
  result.z = fast_saturate(lerp(fast_pow(logPix.z,fractal->invGamma), gammaFactor*logPix.z, fractal->vibrancy));
  result.w = fast_saturate(z);
  
  return result;
}

KERNEL void update_output_kernel(
             uint             xRes,
             uint             yRes,
#ifdef _OPENCL_SRC_
  CONST_MEM  FractalInfo*     fractalInfo,
#endif
  GLOBAL_MEM GlobalStatEntry* globalStatBuffer,
  GLOBAL_MEM float4           accumBuffer[],
  GLOBAL_MEM uint             outputBuffer[]
){
  uint4 iPix;
  float4 pix,acc;
  float scaleConstant;
  
  int x = GLOBAL_ID_X;
  int y = GLOBAL_ID_Y;
    
  if(x < xRes && y < yRes)
  {
    scaleConstant = globalStatBuffer->scaleConstant;
    
    acc = tonemap( fractalInfo, AccumRef(x,y,0,0), scaleConstant);
      
    pix = tonemap( fractalInfo, AccumRef(x,y,0,1), scaleConstant);
    acc.x += pix.x; acc.y += pix.y; acc.z += pix.z; acc.w += pix.w;
    
    pix = tonemap( fractalInfo, AccumRef(x,y,1,0), scaleConstant);
    acc.x += pix.x; acc.y += pix.y; acc.z += pix.z; acc.w += pix.w;
    
    pix = tonemap( fractalInfo, AccumRef(x,y,1,1), scaleConstant);
    acc.x += pix.x; acc.y += pix.y; acc.z += pix.z; acc.w += pix.w;
    
    acc.x /= 4.0f;
    acc.y /= 4.0f;
    acc.z /= 4.0f;
    acc.w /= 4.0f;
        
    iPix.x = (uint)(255.0f*acc.x);
    iPix.y = (uint)(255.0f*acc.y);
    iPix.z = (uint)(255.0f*acc.z);
    iPix.w = (uint)(255.0f*acc.w);
        
    OutputRef(x,y) = iPix.w << 24 | iPix.z << 16 | iPix.y << 8 | iPix.x;
  }
}

*/

/* Experimental version
DEVICE float4 tonemap(CONST_PARAM FractalInfo* fractal, float4 rawPix, float scaleConstant)
{
  float z;
  float4 logPix;
  float4 result;        //the tonemapped pixel
  
  if(rawPix.w <= Epsilon)
    return make_float4(0.0f, 0.0f, 0.0f, 0.0f);
  
  logPix.w = Tone_C1 * fractal->brightness * fast_log10(1.0f+rawPix.w*scaleConstant);
  
  logPix.x = fractal->brightness * rawPix.x / rawPix.w;
  logPix.y = fractal->brightness * rawPix.y / rawPix.w;
  logPix.z = fractal->brightness * rawPix.z / rawPix.w;
  
  z = fast_pow(logPix.w,fractal->invGamma);
  
  result.x = fast_saturate(lerp(fast_pow(logPix.x,fractal->invGamma), logPix.x, fractal->vibrancy));
  result.y = fast_saturate(lerp(fast_pow(logPix.y,fractal->invGamma), logPix.y, fractal->vibrancy));
  result.z = fast_saturate(lerp(fast_pow(logPix.z,fractal->invGamma), logPix.z, fractal->vibrancy));
  result.w = fast_saturate(z);
  
  return result;
}
*/
