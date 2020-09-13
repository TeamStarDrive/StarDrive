#include "QuadTree.h"

#define DLLEXPORT extern "C" __declspec(dllexport)
using tree::QuadTree;
using tree::QtreeNode;
using tree::SpatialObj;

DLLEXPORT QuadTree* __stdcall QtreeCreate(float universeSize, float smallestCell)
{
    return new QuadTree(universeSize, smallestCell);
}

DLLEXPORT void __stdcall QtreeDestroy(QuadTree* tree)
{
    delete tree;
}

DLLEXPORT QtreeNode* __stdcall QtreeCreateRoot(QuadTree* tree)
{
    return tree->createRoot();
}

DLLEXPORT void __stdcall QtreeInsert(QuadTree* tree, QtreeNode* root, const SpatialObj& so)
{
    tree->insert(root, so);
}

DLLEXPORT void __stdcall QtreeRemoveAt(QuadTree* tree, QtreeNode* node, int objectId)
{
    tree->removeAt(node, objectId);
}

DLLEXPORT void __stdcall QtreeCollideAll(QuadTree* tree, float timeStep, 
                                         tree::CollisionFunc onCollide)
{
    tree->collideAll(timeStep, onCollide);
}

DLLEXPORT int __stdcall QtreeFindNearby(QuadTree* tree, int* outResults,
                                        const tree::SearchOptions& opt)
{
    return tree->findNearby(outResults, opt);
}

