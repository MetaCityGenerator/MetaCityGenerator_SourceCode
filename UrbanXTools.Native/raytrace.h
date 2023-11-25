#pragma once

#include "pch.h"

struct Vector3 {
	float x, y, z;
};

struct Ray {
	Vector3 origin;
	Vector3 direction;
	float minDistance;
};

#define INVALID_MESH_ID ((unsigned int) -1)

struct Hit {
	unsigned int meshId;
	unsigned int primId;
	float u, v;
	float distance;
};

URBANXNATIVE_C_FUNCTION
void* InitScene();

URBANXNATIVE_C_FUNCTION
int AddTriangleMesh(void* scene, const float* vertices, int numVerts,
					const int* indices, int numIdx);

URBANXNATIVE_C_FUNCTION
void FinalizeScene(void* scene);

URBANXNATIVE_C_FUNCTION
void TraceSingle(void* scene, const Ray* ray, Hit* hit);

URBANXNATIVE_C_FUNCTION
bool IsOccluded(void* scene, const Ray* ray, float maxDistance);

URBANXNATIVE_C_FUNCTION
void DeleteScene(void* scene, const Ray* ray, float maxDistance);

URBANXNATIVE_C_FUNCTION
void RayTracing(
	const float* vertArray, int vertCount,
	const int* faceArray, int faceCount,
	const Ray* rayOrigin, int rayCount,
	int*& hitId
);

URBANXNATIVE_C_FUNCTION
void ReleaseDoubleArray(double* arr);

URBANXNATIVE_C_FUNCTION
void ReleaseIntArray(int* arr);