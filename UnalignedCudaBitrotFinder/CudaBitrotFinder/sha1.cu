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

__host__ __device__ __forceinline__ void sha1_transform(SHA1_CTX* ctx, const BYTE data[])
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

	if (ctx->datalen < 56) {
		ctx->data[i++] = 0x80;
		while (i < 56)
			ctx->data[i++] = 0x00;
	}
	else {
		ctx->data[i++] = 0x80;
		while (i < 64)
			ctx->data[i++] = 0x00;
		sha1_transform(ctx, ctx->data);
		memset(ctx->data, 0, 56);
	}

	ctx->bitlen += ctx->datalen * 8;
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
	 unsigned int bitIdx = blockIdx.x * blockDim.x + threadIdx.x;
	 if (bitIdx < fileSize * 8 && *result == -1) {
		 unsigned int byteIdx = bitIdx >> 3;
		 unsigned char workingChunk[CHUNK_SIZE];
		 unsigned char hash[20];
		 unsigned int dataOffset = (byteIdx / CHUNK_SIZE) * CHUNK_SIZE;
		 unsigned int currentChunkSize;

		 if (dataOffset < fileSize - CHUNK_SIZE)
			currentChunkSize = CHUNK_SIZE;
		 else
			currentChunkSize = fileSize - dataOffset;

		 memcpy(workingChunk, &pieceData[dataOffset], currentChunkSize);

		 SHA1_CTX ctx = midstates[byteIdx / CHUNK_SIZE];

		 workingChunk[byteIdx % CHUNK_SIZE] ^= (1 << (bitIdx % 8));

		 sha1_update(&ctx, workingChunk, currentChunkSize);

		 if (fileSize - dataOffset > CHUNK_SIZE) {
			 sha1_update(&ctx, &pieceData[dataOffset + CHUNK_SIZE], fileSize - (dataOffset + CHUNK_SIZE));
		 }

		 sha1_final(&ctx, hash);

		 if (cuda_bytecmp(hash, pieceHash)) {
			 // printf("Found result: %d\n", bitIdx);
			 *result = bitIdx;
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