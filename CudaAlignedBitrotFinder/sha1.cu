#include "sha1.cuh"

#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include <stdio.h>

#ifndef ROTLEFT
#define ROTLEFT(a,b) (((a) << (b)) | ((a) >> (32-(b))))
#endif

constexpr auto K0 = 0x5a827999;
constexpr auto K1 = 0x6ed9eba1;
constexpr auto K2 = 0x8f1bbcdc;
constexpr auto K3 = 0xca62c1d6;

__host__ __device__ void sha1_transform(SHA1_CTX* ctx, const BYTE data[])
{
	WORD a, b, c, d, e, i, j, t, m[80];

	for (i = 0, j = 0; i < 16; ++i, j += 4)
		m[i] = (data[j] << 24) + (data[j + 1] << 16) + (data[j + 2] << 8) + (data[j + 3]);

	for (; i < 80; ++i) {
		m[i] = (m[i - 3] ^ m[i - 8] ^ m[i - 14] ^ m[i - 16]);
		m[i] = (m[i] << 1) | (m[i] >> 31);
	}

	a = ctx->state[0];
	b = ctx->state[1];
	c = ctx->state[2];
	d = ctx->state[3];
	e = ctx->state[4];

	for (i = 0; i < 20; ++i) {
		t = ROTLEFT(a, 5) + ((b & c) ^ (~b & d)) + e + K0 + m[i];
		e = d;
		d = c;
		c = ROTLEFT(b, 30);
		b = a;
		a = t;
	}

	for (; i < 40; ++i) {
		t = ROTLEFT(a, 5) + (b ^ c ^ d) + e + K1 + m[i];
		e = d;
		d = c;
		c = ROTLEFT(b, 30);
		b = a;
		a = t;
	}

	for (; i < 60; ++i) {
		t = ROTLEFT(a, 5) + ((b & c) ^ (b & d) ^ (c & d)) + e + K2 + m[i];
		e = d;
		d = c;
		c = ROTLEFT(b, 30);
		b = a;
		a = t;
	}

	for (; i < 80; ++i) {
		t = ROTLEFT(a, 5) + (b ^ c ^ d) + e + K3 + m[i];
		e = d;
		d = c;
		c = ROTLEFT(b, 30);
		b = a;
		a = t;
	}

	ctx->state[0] += a;
	ctx->state[1] += b;
	ctx->state[2] += c;
	ctx->state[3] += d;
	ctx->state[4] += e;
}

__host__ __device__ void sha1_init(SHA1_CTX* ctx)
{
	ctx->datalen = 0;
	ctx->bitlen = 0;
	ctx->state[0] = 0x67452301;
	ctx->state[1] = 0xEFCDAB89;
	ctx->state[2] = 0x98BADCFE;
	ctx->state[3] = 0x10325476;
	ctx->state[4] = 0xc3d2e1f0;
}

__host__ __device__ void sha1_update(SHA1_CTX* ctx, const BYTE data[], size_t len)
{
	size_t i;

	for (i = 0; i < len; ++i) {
		ctx->data[ctx->datalen] = data[i];
		ctx->datalen++;
		if (ctx->datalen == 64) {
			sha1_transform(ctx, ctx->data);
			ctx->bitlen += 512;
			ctx->datalen = 0;
		}
	}
}

__host__ __device__ void sha1_final(SHA1_CTX* ctx, BYTE hash[])
{
	WORD i;

	i = ctx->datalen;

	ctx->data[0] = 0x80;
	memset(&ctx->data[1], 0x00, 55);

	ctx->data[63] = ctx->bitlen;
	ctx->data[62] = ctx->bitlen >> 8;
	ctx->data[61] = ctx->bitlen >> 16;
	ctx->data[60] = ctx->bitlen >> 24;
	ctx->data[59] = ctx->bitlen >> 32;
	ctx->data[58] = ctx->bitlen >> 40;
	ctx->data[57] = ctx->bitlen >> 48;
	ctx->data[56] = ctx->bitlen >> 56;
	sha1_transform(ctx, ctx->data);

	for (i = 0; i < 4; ++i) {
		hash[i] = (ctx->state[0] >> (24 - i * 8)) & 0x000000ff;
		hash[i + 4] = (ctx->state[1] >> (24 - i * 8)) & 0x000000ff;
		hash[i + 8] = (ctx->state[2] >> (24 - i * 8)) & 0x000000ff;
		hash[i + 12] = (ctx->state[3] >> (24 - i * 8)) & 0x000000ff;
		hash[i + 16] = (ctx->state[4] >> (24 - i * 8)) & 0x000000ff;
	}
}

__global__ void bitFlipKernel(unsigned char* pieceData, unsigned char* pieceHash, SHA1_CTX* midstates, size_t fileSize, unsigned int* result)
{
	unsigned int batchIdx = blockIdx.x * blockDim.x + threadIdx.x;

	if (batchIdx < fileSize / BATCH_SIZE && *result == -1) {
		unsigned int startingBit = batchIdx * BATCH_SIZE * 8;
		unsigned int endingBit;

		if (startingBit + BATCH_SIZE * 8 < fileSize * 8)
			endingBit = startingBit + BATCH_SIZE * 8;
		else
			endingBit = fileSize * 8;

		unsigned char workingChunk[CHUNK_SIZE];
		unsigned char hash[20];

		unsigned int dataOffset = ((startingBit >> 3) / CHUNK_SIZE) * CHUNK_SIZE;
		unsigned int currentChunkSize;

		if (dataOffset < fileSize - CHUNK_SIZE)
			currentChunkSize = CHUNK_SIZE;
		else
			currentChunkSize = fileSize - dataOffset;

		memcpy(workingChunk, &pieceData[dataOffset], currentChunkSize);

		SHA1_CTX cachedCtx = midstates[(startingBit >> 3) / CHUNK_SIZE];

		for (unsigned int bitIdx = startingBit; bitIdx < endingBit; bitIdx++) {
			if (*result != -1) 
				return;

			SHA1_CTX ctx = cachedCtx;

			if (bitIdx != startingBit) {
				workingChunk[((bitIdx - 1) >> 3) % CHUNK_SIZE] ^= (1 << ((bitIdx - 1) % 8));
			}

			workingChunk[(bitIdx >> 3) % CHUNK_SIZE] ^= (1 << (bitIdx % 8));

			memcpy(ctx.data, workingChunk, CHUNK_SIZE);
			sha1_transform(&ctx, workingChunk);
			ctx.bitlen += 512;

			if (endingBit != fileSize * 8) {
				for (unsigned int chunkOffset = dataOffset + CHUNK_SIZE; chunkOffset < fileSize; chunkOffset += CHUNK_SIZE) {
					sha1_transform(&ctx, &pieceData[chunkOffset]);
					ctx.bitlen += 512;
				}
				memcpy(ctx.data, &pieceData[fileSize - CHUNK_SIZE], CHUNK_SIZE);
			}

			ctx.datalen = 0;

			sha1_final(&ctx, hash);

			if (cuda_bytecmp(hash, pieceHash)) {
				// printf("Result: %d\n", bitIdx);
				*result = bitIdx;
			}
		}
	}		
}

__device__ __forceinline__ int cuda_bytecmp(register const unsigned char* s1, register const unsigned char* s2) {
	register unsigned char n = 15;
	do {
		if (*s1 != *s2++)
			return 0;
		if (*s1++ == 0)
			break;
	} while (--n != 0);
	return 1;
}