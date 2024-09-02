#ifndef SHA1_H
#define SHA1_H

#include "cuda_runtime.h"
#include "device_launch_parameters.h"
#include <stdlib.h>

#define SHA1_BLOCK_SIZE 20
#define CHUNK_SIZE 64

typedef unsigned char BYTE;
typedef unsigned int WORD;
typedef unsigned long long LONG;

typedef struct {
    BYTE data[64];
    WORD datalen;
    unsigned long long bitlen;
    WORD state[5];
} SHA1_CTX;

__host__ __device__ void sha1_init(SHA1_CTX* ctx);
__host__ __device__ void sha1_update(SHA1_CTX* ctx, const BYTE data[], size_t len);
__host__ __device__ __forceinline__ void sha1_transform(SHA1_CTX* ctx, const BYTE data[]);
__host__ __device__ void sha1_final(SHA1_CTX* ctx, BYTE hash[]);
__global__ void bitFlipKernel(unsigned char* pieceData, unsigned char* pieceHash, SHA1_CTX* midstates, size_t fileSize, unsigned int* result);
__device__ __forceinline__ int cuda_bytecmp(register const unsigned char* s1, register const unsigned char* s2);

#endif // SHA1_H