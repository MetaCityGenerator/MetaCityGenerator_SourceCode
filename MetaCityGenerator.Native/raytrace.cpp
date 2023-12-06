#pragma once

#include "pch.h"
#include "raytrace.h"
#include "scene.h"

void* InitScene()
{
	auto scn = new MetaCitynative::Scene();
	scn->Init();
	return (void*)scn;
}

int AddTriangleMesh(void* scene, const float* vertices, int numVerts, const int* indices, int numIdx)
{
	auto scn = (MetaCitynative::Scene*)scene;
	return scn->AddMesh(vertices, indices, numVerts,  numIdx/3);
}

void FinalizeScene(void* scene)
{
	auto scn = (MetaCitynative::Scene*)scene;
	scn->Finalize();
}

void TraceSingle(void* scene, const Ray* ray, Hit* hit)
{
	auto scn = (MetaCitynative::Scene*)scene;
	*hit = scn->Intersect(*ray);
}

bool IsOccluded(void* scene, const Ray* ray, float maxDistance)
{
	auto scn = (MetaCitynative::Scene*)scene;
	return scn->IsOccluded(*ray, maxDistance);
}

void DeleteScene(void* scene, const Ray* ray, float maxDistance)
{
	auto scn = (MetaCitynative::Scene*)scene;
}

void RayTracing(const float* vertArray, int vertCount,
	const int* faceArray, int faceCount,
	const Ray* rayOrigin, int rayCount,
	int*& hitId)
{
	RTCDevice device = rtcNewDevice(NULL);
	RTCScene scene = rtcNewScene(device);
	RTCGeometry geom = rtcNewGeometry(device, RTC_GEOMETRY_TYPE_TRIANGLE);

	//declare the surface mesh
	float* vb = (float*)rtcSetNewGeometryBuffer(geom,
		RTC_BUFFER_TYPE_VERTEX, 0, RTC_FORMAT_FLOAT3, 3 * sizeof(float), vertCount);


	unsigned* ib = (unsigned*)rtcSetNewGeometryBuffer(geom,
		RTC_BUFFER_TYPE_INDEX, 0, RTC_FORMAT_UINT3, 3 * sizeof(unsigned), faceCount);

	std::copy(vertArray, vertArray + vertCount * 3, vb);
	std::copy(faceArray, faceArray + faceCount * 3, ib);

	//initiate scene

	rtcCommitGeometry(geom);
	rtcAttachGeometry(scene, geom);
	rtcReleaseGeometry(geom);

	rtcCommitScene(scene);

	//TODO: change into multi
	//initiate ray and intersect
	hitId = new int[rayCount];
#pragma omp parallel for schedule(dynamic)
	for (int i = 0; i < rayCount; i++)
	{
		struct RTCIntersectContext context;
		rtcInitIntersectContext(&context);

		RTCRayHit rayhit;
		rayhit.ray.org_x = rayOrigin[i].origin.x;
		rayhit.ray.org_y = rayOrigin[i].origin.y;
		rayhit.ray.org_z = rayOrigin[i].origin.z;

		rayhit.ray.dir_x = rayOrigin[i].direction.x;
		rayhit.ray.dir_y = rayOrigin[i].direction.y;
		rayhit.ray.dir_z = rayOrigin[i].direction.z;

		rayhit.ray.tnear = 0.f;
		rayhit.ray.tfar = std::numeric_limits<float>::infinity();
		rayhit.ray.mask = 0;
		rayhit.ray.flags = 0;
		rayhit.hit.geomID = RTC_INVALID_GEOMETRY_ID;
		rayhit.hit.instID[0] = RTC_INVALID_GEOMETRY_ID;

		rtcIntersect1(scene, &context, &rayhit);
		hitId[i] = rayhit.hit.primID;

		//if (rayhit.hit.geomID != RTC_INVALID_GEOMETRY_ID)
		//{
		//	hitId[i] = rayhit.hit.primID;
		//}
		//else {
		//	hitId[i] = -1;
		//}
	}

	return;
}

void ReleaseDoubleArray(double* arr)
{
	delete[] arr;
}

void ReleaseIntArray(int* arr)
{
	delete[] arr;
}
