//
//------------------< c o n f i g . h >---------------------
//

#ifndef __CONFIG_H__
#define __CONFIG_H__

#define IterGroupSize 128
#define IteratorCount (IterBlockSize*iterBlockCount)
#define AALevel 2
#define SubPixelCount (AALevel*AALevel)
#define RasterGroupWidth 8
#define RasterGroupHeight 8
#define MaxBranches 16
#define MaxVariations 48

#define WarmupIterationCount 32

#define Tone_C1 (1.0f/2.0f)
#define Tone_C2 (64.0f/1.0f)
#define PIf 3.14159265358f
#define PIo2f 1.57079632679f
#define InvPIo2f 0.636619772368f
#define Epsilon (4.7019774E-38f)

#endif