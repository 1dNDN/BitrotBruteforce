
#include "cuda_runtime.h"
#include "device_launch_parameters.h"

#include <stdio.h>
#include <string>
#include <stdexcept>
#include <iostream>
#include <fstream>

#include "sha1.cuh"

extern "C" {
    void __declspec(dllexport) bruteforceBits(unsigned char* pieceData, unsigned char* pieceHash, size_t pieceSize, unsigned int* result)
    {
        unsigned char* dev_pieceData = 0;
        unsigned char* dev_pieceHash = 0;
        SHA1_CTX* dev_midstates = 0;
        unsigned int* dev_result = 0;
        cudaError_t cudaStatus;

        size_t midstatesLength = pieceSize / CHUNK_SIZE;
        SHA1_CTX* midstates = new SHA1_CTX[midstatesLength + 1];

        SHA1_CTX ctx;
        sha1_init(&ctx);

        midstates[0] = ctx;
        for (int i = 0; i < midstatesLength; i++) {
            sha1_update(&ctx, &pieceData[i * CHUNK_SIZE], CHUNK_SIZE);
            midstates[i + 1] = ctx;
        }

        cudaStatus = cudaSetDevice(0);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaSetDevice failed!");
            goto Error;
        }

        // alloc
        cudaStatus = cudaMalloc((void**)&dev_pieceData, pieceSize);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMalloc failed!");
            goto Error;
        }

        cudaStatus = cudaMalloc((void**)&dev_pieceHash, 20);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMalloc failed!");
            goto Error;
        }

        cudaStatus = cudaMalloc((void**)&dev_midstates, ((pieceSize / CHUNK_SIZE) + 1) * sizeof(SHA1_CTX));
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMalloc failed!");
            goto Error;
        }

        cudaStatus = cudaMalloc((void**)&dev_result, sizeof(unsigned int));
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMalloc failed!");
            goto Error;
        }


        // copy
        cudaStatus = cudaMemcpy(dev_pieceData, pieceData, pieceSize, cudaMemcpyHostToDevice);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMemcpy failed!");
            goto Error;
        }

        cudaStatus = cudaMemcpy(dev_pieceHash, pieceHash, 20, cudaMemcpyHostToDevice);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMemcpy failed!");
            goto Error;
        }

        cudaStatus = cudaMemcpy(dev_midstates, midstates, ((pieceSize / CHUNK_SIZE) + 1) * sizeof(SHA1_CTX), cudaMemcpyHostToDevice);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMemcpy failed!");
            goto Error;
        }

        cudaStatus = cudaMemcpy(dev_result, result, sizeof(unsigned int), cudaMemcpyHostToDevice);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMemcpy failed!");
            goto Error;
        }

        int threadsPerBlock = 1024;
        int blocksPerGrid = ((pieceSize * 8) + threadsPerBlock - 1) / threadsPerBlock;
        bitFlipKernel << <blocksPerGrid, threadsPerBlock >> > (dev_pieceData, dev_pieceHash, dev_midstates, pieceSize, dev_result);

        cudaStatus = cudaGetLastError();
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "kernel launch failed: %s\n", cudaGetErrorString(cudaStatus));
            goto Error;
        }

        cudaStatus = cudaDeviceSynchronize();
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaDeviceSynchronize returned error code %d after launching kernel!\n", cudaStatus);
            goto Error;
        }

        cudaStatus = cudaMemcpy(result, dev_result, sizeof(unsigned int), cudaMemcpyDeviceToHost);
        if (cudaStatus != cudaSuccess) {
            fprintf(stderr, "cudaMemcpy failed!");
            goto Error;
        }

    Error:
        cudaFree(dev_pieceData);
        cudaFree(dev_pieceHash);
        cudaFree(dev_midstates);
        cudaFree(dev_result);

        delete[] midstates;

        return;
    }
}

unsigned char* hexStringToBytes(const char* hexStr, size_t& byteArrayLength) {
    size_t hexStrLength = std::strlen(hexStr);

    if (hexStrLength % 2 != 0) {
        return nullptr;
    }

    byteArrayLength = hexStrLength / 2;

    unsigned char* byteArray = new unsigned char[byteArrayLength];

    for (size_t i = 0; i < byteArrayLength; ++i) {
        char byteString[3] = { hexStr[2 * i], hexStr[2 * i + 1], '\0' };
        byteArray[i] = static_cast<unsigned char>(std::strtoul(byteString, nullptr, 16));
    }

    return byteArray;
}

int main(int argc, char** argv)
{
    if (argc != 3) {
        std::cerr << "Error: Not enough arguments supplied! Usage: " << argv[0] << " <piece path> " << "<expected hash>" << std::endl;
        return 1;
    }

    size_t byteArrayLength = 0;
    auto pieceHash = hexStringToBytes(argv[2], byteArrayLength);

    if (byteArrayLength != 20) {
        std::cerr << "Error: Incorrect expected hash length";
        return 1;
    }

    if (argc < 2) {
        std::cerr << "Usage: " << argv[0] << " <file_path>" << std::endl;
        return 1;
    }

    std::string piecePath = argv[1];

    std::ifstream file(piecePath, std::ios::binary | std::ios::ate);
    if (!file) {
        std::cerr << "Error: File '" << piecePath << "' does not exist or cannot be opened." << std::endl;
        return 1;
    }

    std::streamsize fileSize = file.tellg();
    file.seekg(0, std::ios::beg);

    unsigned char* fileData = new unsigned char[fileSize];

    if (!file.read(reinterpret_cast<char*>(fileData), fileSize)) {
        std::cerr << "Error: Failed to read the file." << std::endl;
        delete[] fileData;
        return 1;
    }

    file.close();

    // Output the size of the file and the first few bytes (for demonstration)
    std::cout << "File size: " << fileSize << " bytes" << std::endl;
    std::cout << "First few bytes: ";
    for (size_t i = 0; i < std::min(fileSize, static_cast<std::streamsize>(64)); ++i) {
        std::cout << std::hex << static_cast<int>(fileData[i]) << " ";
    }
    std::cout << std::dec << std::endl;

    // Add vectors in parallel.
    unsigned int result = -1;
    bruteforceBits(fileData, pieceHash, fileSize, &result);

    std::cout << "Result: " << result << std::endl;

    cudaError_t cudaStatus = cudaDeviceReset();
    if (cudaStatus != cudaSuccess) {
        fprintf(stderr, "cudaDeviceReset failed!");
        return 1;
    }

    delete[] pieceHash;
    delete[] fileData;

    return 0;
}
