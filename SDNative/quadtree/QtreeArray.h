#pragma once
#include <cstdint>
#include "QtreeAllocator.h"

namespace tree
{
    template<class T, int Capacity>
    struct QtreeArray
    {
        int size = 0;
        T* items = nullptr;

        /// <summary>
        /// Regular vector-like push_back
        /// </summary>
        void push_back(QtreeAllocator& allocator, const T& item)
        {
            if (items == nullptr)
            {
                items = allocator.allocArray<T>(Capacity);
            }
            items[size++] = item;
        }

        void clear()
        {
            size = 0;
            items = nullptr;
        }
    };

    template<class T>
    struct QtreeFlatMap
    {
        static constexpr int ChunkSize = 2048;
        static constexpr int InitialChunks = 8;

        // we divide the flatmap into chunks, this allows us to have gaps
        struct Chunk
        {
            T objects[ChunkSize];
        };

        Chunk** Chunks = nullptr;
        int Count = 0; // number of total objects in all active chunks
        int NumChunks = 0; // number of chunks
        QtreeAllocator* Allocator = nullptr;

        void insert(int objectId, const T& item)
        {
            int whichChunk = objectId / ChunkSize;
            int indexInChunk = objectId % ChunkSize;
            Chunk* chunk;

            if (NumChunks <= whichChunk)
            {
                int newCapacity = NumChunks == 0 ? InitialChunks : NumChunks * 2;
                while (newCapacity <= whichChunk)
                    newCapacity *= 2;

                Chunks = Allocator->allocArray(Chunks, NumChunks, newCapacity);
                memset(Chunks+NumChunks, 0, (newCapacity - NumChunks) * sizeof(Chunk*));
                NumChunks = newCapacity;

                chunk = Allocator->allocUninitialized<Chunk>();
                Chunks[whichChunk] = chunk;
            }
            else
            {
                chunk = Chunks[whichChunk];
                if (chunk == nullptr)
                {
                    chunk = Allocator->allocUninitialized<Chunk>();
                    Chunks[whichChunk] = chunk;
                }
            }
            
            chunk->objects[indexInChunk] = item;
            ++Count;
        }

        void reset(QtreeAllocator* allocator)
        {
            Chunks = nullptr;
            NumChunks = 0;
            Count = 0;
            Allocator = allocator;
        }

        const T* operator[](int objectId) const
        {
            int whichChunk = objectId / ChunkSize;
            int indexInChunk = objectId % ChunkSize;

            if (whichChunk < NumChunks)
            {
                if (Chunk* chunk = Chunks[whichChunk])
                {
                    return &chunk->objects[indexInChunk];
                }
            }
            return nullptr;
        }
    };

}
