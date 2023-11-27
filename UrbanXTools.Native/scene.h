#pragma once

#include "pch.h"
#include "common.h"
#include "raytrace.h"

namespace MetaCitynative {

class Scene {
public:
	~Scene() {
		if (isInit)
		{
			rtcReleaseScene(embreeScene);
			rtcReleaseDevice(embreeDevice);
		}
	}

	void Init();
	int AddMesh(const float* vertexData, const int* indexData, int numVerts, int numTriangles);
	void Finalize();
	Hit Intersect(const Ray& ray);
	bool IsOccluded(const Ray& ray, float maxDistance);

private:
	bool isInit = false;
	bool isFinal = false;

	RTCDevice embreeDevice;
	RTCScene embreeScene;

};

}