using g3;

using System.Collections.Generic;
using System.Linq;

using Rh = Rhino.Geometry;

namespace UrbanX.Planning.SpatialAnalysis
{
    public class RhinoToolManager
    {
        public static double GetMaxBounds(Rh.Brep brep)
        {
            var ptMax = brep.GetBoundingBox(false).Max;
            var ptMin = brep.GetBoundingBox(false).Min;
            return ptMax.DistanceTo(ptMin);
        }

        public static Rh.Mesh JoinedMesh(IEnumerable<Rh.Mesh> meshInput)
        {
            var tempMesh = new Rh.Mesh();
            var count = meshInput.Count();
            var vertexCount = 0;

            for (int i = 0; i < count; i++)
            {
                var temp = meshInput.ElementAt(i);
                tempMesh.Append(temp);
                vertexCount += temp.Vertices.Count;
            }

            IEnumerable<System.Drawing.Color> colorList = Enumerable.Repeat(System.Drawing.Color.LightGray, vertexCount);

            tempMesh.VertexColors.AppendColors(colorList.ToArray());
            return tempMesh;
        }

        public static void JoinedMeshParallel(Rh.Mesh origin, Rh.Mesh meshInput)
        {
            origin.Append(meshInput);
        }
        public static DMesh3 ConvertFromRhMesh(IEnumerable<Rh.Mesh> meshInput)
        {
            var tempMesh = new Rh.Mesh();
            for (int i = 0; i < meshInput.Count(); i++)
            {
                tempMesh.Append(meshInput.ElementAt(i));
            }

            var meshVertices = ConvertFromRhPt(tempMesh.Vertices.ToPoint3fArray());
            var meshNormals = ConvertFromRhPt_float(tempMesh.Normals.ToFloatArray());
            var meshFaces = ConvertFromRhinoMeshFace(tempMesh.Faces);
            DMesh3 meshOut = DMesh3Builder.Build(meshVertices, meshFaces, meshNormals);
            return meshOut;
        }

        public static void ConvertFromRhMeshParallel(DMesh3 origin, Rh.Mesh meshInput)
        {
            Vector3d[] meshVertices = ConvertFromRhPt(meshInput.Vertices.ToPoint3dArray());
            Vector3d[] meshNormals = ConvertFromRhPt_double(meshInput.Normals.ToFloatArray());
            var meshFaces = ConvertFromRhinoMeshFace(meshInput.Faces);
            DMesh3 meshOut = DMesh3Builder.Build(meshVertices, meshFaces, meshNormals);
            MeshEditor.Append(origin, meshOut);
        }

        private static Vector3f[] ConvertFromRhPt(Rh.Point3f[] ptArray)
        {
            var vectorResult = new Vector3f[ptArray.Length];
            for (int i = 0; i < ptArray.Length; i++)
            {
                vectorResult[i] = new Vector3f(ptArray[i].X, ptArray[i].Y, ptArray[i].Z);
            }
            return vectorResult;
        }

        private static Vector3f[] ConvertFromRhPt_float(float[] floatArray)
        {
            var vectorResult = new Vector3f[floatArray.Count() / 3];
            for (int i = 0; i < vectorResult.Length; i++)
            {
                vectorResult[i].x = floatArray[i * 3];
                vectorResult[i].y = floatArray[i * 3 + 1];
                vectorResult[i].z = floatArray[i * 3 + 2];
            }
            return vectorResult;
        }

        private static Vector3d[] ConvertFromRhPt(Rh.Point3d[] ptArray)
        {
            var vectorResult = new Vector3d[ptArray.Length];
            for (int i = 0; i < ptArray.Length; i++)
            {
                vectorResult[i] = new Vector3d(ptArray[i].X, ptArray[i].Y, ptArray[i].Z);
            }
            return vectorResult;
        }

        private static Vector3d[] ConvertFromRhPt_double(float[] floatArray)
        {
            var vectorResult = new Vector3d[floatArray.Count() / 3];
            for (int i = 0; i < vectorResult.Length; i++)
            {
                vectorResult[i].x = floatArray[i * 3];
                vectorResult[i].y = floatArray[i * 3 + 1];
                vectorResult[i].z = floatArray[i * 3 + 2];
            }
            return vectorResult;
        }

        private static Index3i[] ConvertFromRhinoMeshFace(Rh.Collections.MeshFaceList meshFaceList)
        {
            Index3i[] triangles = new Index3i[meshFaceList.Count];
            for (int i = 0; i < meshFaceList.Count; i++)
            {
                var meshFace = meshFaceList[i];
                triangles[i] = new Index3i(meshFace.A, meshFace.B, meshFace.C);
            }
            return triangles;
        }



    }
}
