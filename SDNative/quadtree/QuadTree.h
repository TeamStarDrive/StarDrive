#pragma once
#include "QtreeNode.h"
#include "QtreeAllocator.h"
#include <vector>
#include <memory>

namespace tree
{
    using CollisionFunc = int (__stdcall*)(int objectA, int objectB);

    class QuadTree
    {
        int Levels;
        float FullSize;
        float QuadToLinearSearchThreshold;
        QtreeNode* Root;

        std::unique_ptr<QtreeAllocator> FrontBuffer = std::make_unique<QtreeAllocator>(10000);
        std::unique_ptr<QtreeAllocator> BackBuffer  = std::make_unique<QtreeAllocator>(20000);

    public:

        explicit QuadTree(float universeSize, float smallestCell);
        ~QuadTree();

        QtreeNode* createRoot();
        void insertAt(QtreeNode* node, int level, const SpatialObj& so);
        void insert(QtreeNode* root, const SpatialObj& so) { insertAt(root, Levels, so); }
        void removeAt(QtreeNode* root, int objectId);

        void collideAll(float timeStep, CollisionFunc onCollide);
        void collideAllRecursive(float timeStep, CollisionFunc onCollide);

        int findNearby(int* outResults, int maxResults,
                       float x, float y, float radius,
                       int typeFilter, int objectToIgnoreId, int loyaltyFilter);

    private:
        void markForRemoval(int objectId, SpatialObj& so);
        void subdivide(QtreeNode& node, int level);
        static QtreeNode* pickSubQuadrant(QtreeNode& node, const SpatialObj& so);
        QtreeNode* findEnclosingNode(QtreeNode* node, const SpatialObj& so);
    };
}
