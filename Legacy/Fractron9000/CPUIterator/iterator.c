#include <stdlib.h>
#include <malloc.h>
#include <math.h>
#include <float.h>

#include "interop_cpu.h"
#include "config.h"
#include "data_types.h"
#include "random_mwc.h"
#include "variations.h"


typedef struct _iterator_state_struct
{
	uint id;
	float2 pos;
	float2 chroma;
	IterStatEntry stats;

	uint4 rand_x;
	uint  rand_c;

	FractalInfo* fractal;
	BranchInfo* branches;
	float* variWeights;
	int dot_count;
	Dot* output;

} iterator_state;

iterator_state* create_iterator(int id, uint seed)
{
	iterator_state* iter = malloc(sizeof(iterator_state));
	iter->id = id;
	iter->pos = make_float2(0.0f, 0.0f);
	iter->chroma = make_float2(0.0f, 0.0f);
	iter->stats.dotCount = 0;
	iter->stats.peakDensity = 0.0f;
	MWC_seed(&iter->rand_x, &iter->rand_c, seed);
	iter->fractal = NULL;
	iter->branches = NULL;
	iter->variWeights = NULL;
	return iter;
}

void destroy_iterator(iterator_state* iter)
{
	free(iter);
}

void set_iterator_fractal(iterator_state* iter, FractalInfo* fractal, BranchInfo* branches, float* variWeights)
{
	iter->fractal = fractal;
	iter->branches = branches;
	iter->variWeights = variWeights;
}

void set_iterator_output(iterator_state* iter, int dot_count, Dot* output)
{
	iter->dot_count = dot_count;
	iter->output = output;
}

void get_iterator_stats(iterator_state* iter, IterStatEntry* stats)
{
	stats->dotCount = iter->stats.dotCount;
	stats->peakDensity = iter->stats.peakDensity;
}

void reset_iterator(iterator_state* iter)
{
	uint rnd;
	uint bi;
	int j;
	iter->pos = MWC_rand_float2(&iter->rand_x, &iter->rand_c);
	iter->chroma = make_float2(0.5f, 0.5f);
	iter->stats.dotCount = 0;
	iter->stats.peakDensity = 0.0f;
	for(j = 0; j < WarmupIterationCount; j++)
	{
		rnd = MWC_rand(&iter->rand_x, &iter->rand_c);
		bi = chooseRandomBranch(rnd&0x0000FFFF, iter->fractal->branchCount, iter->branches);
		iterate(&iter->pos, &iter->chroma, (rnd>>16), iter->branches + bi, iter->variWeights+(bi*MaxVariations), &iter->rand_x, &iter->rand_c);
		if(!(iter->pos.x < FLT_MAX && iter->pos.x > -FLT_MAX && iter->pos.y < FLT_MAX && iter->pos.y > -FLT_MAX))
		{
			iter->pos = MWC_rand_float2(&iter->rand_x, &iter->rand_c);
		}
	}
}

void iterate_batch(iterator_state* iter)
{
	uint rnd;
	uint bi;
	int j;

	for(j = 0; j < iter->dot_count; j++)
	{
		rnd = MWC_rand(&iter->rand_x, &iter->rand_c);
		bi = chooseRandomBranch(rnd&0x0000FFFF, iter->fractal->branchCount, iter->branches);		
		iterate(&iter->pos, &iter->chroma, (rnd>>16), iter->branches + bi, iter->variWeights+(bi*MaxVariations), &iter->rand_x, &iter->rand_c);
		
		if(!(iter->pos.x < FLT_MAX && iter->pos.x > -FLT_MAX && iter->pos.y < FLT_MAX && iter->pos.y > -FLT_MAX))
		{
			iter->pos = MWC_rand_float2(&iter->rand_x, &iter->rand_c);
			iter->pos.x *= 256.0f;
			iter->pos.y *= 256.0f;
		}
		
		iter->output[j].pos = iter->pos;
		iter->output[j].chroma = iter->chroma;
		iter->stats.dotCount++;
	}
}

