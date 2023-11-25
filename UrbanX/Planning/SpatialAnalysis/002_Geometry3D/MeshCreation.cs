using g3;

using NetTopologySuite.Features;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Numerics;
using UrbanX.Planning.SpatialAnalysis.Extension;
using UrbanXWrapper;
using NTS = NetTopologySuite;
using Rh = Rhino.Geometry;

namespace UrbanX.Planning.SpatialAnalysis
{
    public enum VisDataType
    {
        TotalVisArea,
        VisRatio,
        normalizedVisRatio
    }

    public enum VisRatioType
    {
        /// <summary>
        /// not normalized
        /// </summary>
        nByOne,

        /// <summary>
        /// normalized by line lenth
        /// </summary>
        nByLength,

        /// <summary>
        /// normalized by pt count in each line
        /// </summary>
        nByCount,

        debug
    }
    public enum BoundaryModes
    {
        FreeBoundaries,
        FixedBoundaries,
        ConstrainedBoundaries
    }
    public class MeshCreation
    {

        #region 000_Basic Function

        public static void InitiateColor(DMesh3 mesh, Colorf color)
        {
            mesh.EnableVertexColors(color);
        }

        public static DMesh3 ApplyColor(DMesh3 mesh, Colorf originColor, Colorf DestnationColor)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            float meshCount = meshIn.VertexCount;

            for (int i = 0; i < meshCount; i++)
            {
                var temp_color = Colorf.Lerp(originColor, DestnationColor, i / meshCount);
                meshIn.SetVertexColor(i, temp_color);
            }
            return meshIn;
        }

        public static DMesh3 ApplyColor(DMesh3 mesh, Colorf originColor, Colorf DestnationColor, float meshCount, Func<float, float> singleCount)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            for (int i = 0; i < meshCount; i++)
            {
                var temp_color = Colorf.Lerp(originColor, DestnationColor, singleCount(i));
                meshIn.SetVertexColor(i, temp_color);
            }
            return meshIn;
        }

        #endregion

        #region 001_Generating Mesh
        public static bool CreateMesh(IEnumerable<Vector3f> vertices, int[] triangles, List<Vector3f> normals, out DMesh3 meshResult)
        {
            DMesh3 mesh = DMesh3Builder.Build(vertices, triangles, normals);
            meshResult = mesh;
            return mesh.CheckValidity();
        }
        public static void CreateMesh(IEnumerable<Vector3d> vertices, int[] triangles, out DMesh3 meshResult)
        {
            List<Vector3d> normals = new List<Vector3d>();
            for (int i = 0; i < vertices.Count(); i++)
                normals.Add(Vector3d.AxisZ);
                
            DMesh3 mesh = DMesh3Builder.Build(vertices, triangles, normals);
            meshResult = mesh;

        }

        public static DMesh3 ExtrudeMeshFromPt(Vector3d[][] OriginalData, double[] height)
        {
            DMesh3 meshCollection = new DMesh3();
            for (int i = 0; i < OriginalData.Length; i++)
            {
                var meshSrf = BoundarySrfFromPts(OriginalData[i]);
                var meshExtruded = ExtrudeMeshFromMesh(meshSrf, height[i]);
                MeshEditor.Append(meshCollection, meshExtruded);
            }
            return meshCollection;
        }

        public static DMesh3 ExtrudeMeshFromPt(Vector3d[][] OriginalData, double[] height, out Dictionary<NTS.Geometries.Point, double> centerPtDic)
        {
            Dictionary<NTS.Geometries.Point, double> temp_centerPtDic = new Dictionary<NTS.Geometries.Point, double>();
            DMesh3 meshCollection = new DMesh3();
            for (int i = 0; i < OriginalData.Length; i++)
            {
                var meshSrf = BoundarySrfFromPts(OriginalData[i], out NTS.Geometries.Point centerPt);
                var meshExtruded = ExtrudeMeshFromMesh(meshSrf, height[i]);
                MeshEditor.Append(meshCollection, meshExtruded);
                var triAreaList = new List<double>(meshExtruded.TriangleCount);
                for (int j = 0; j < meshExtruded.TriangleCount; j++)
                {
                    triAreaList.Add(meshExtruded.GetTriArea(j));
                }
                var tempArea = triAreaList.Sum();

                if (temp_centerPtDic.ContainsKey(centerPt))
                {
                    var tempAreaInDic = temp_centerPtDic[centerPt];
                    temp_centerPtDic[centerPt] = tempArea + tempAreaInDic;
                }
                else
                    temp_centerPtDic.Add(centerPt, tempArea);
            }

            centerPtDic = temp_centerPtDic;
            return meshCollection;
        }

        public static DMesh3 ExtrudeMeshFromPtMinusTopBtn(Vector3d[][] OriginalData, double[] height, out Dictionary<NTS.Geometries.Point, double> centerPtDic)
        {
            Dictionary<NTS.Geometries.Point, double> temp_centerPtDic = new Dictionary<NTS.Geometries.Point, double>();
            DMesh3 meshCollection = new DMesh3();
            for (int i = 0; i < OriginalData.Length; i++)
            {
                var meshSrf = BoundarySrfFromPts(OriginalData[i], out NTS.Geometries.Point centerPt, out double meshArea);
                //var meshExtruded = ExtrudeMeshFromMesh(meshSrf, height[i]); 
                var meshExtruded = ExtrudeMeshEdge(meshSrf, height[i]);

                MeshEditor.Append(meshCollection, meshExtruded);
                var triAreaList = new List<double>(meshExtruded.TriangleCount);
                for (int j = 0; j < meshExtruded.TriangleCount; j++)
                {
                    triAreaList.Add(meshExtruded.GetTriArea(j));
                }
                var tempArea = triAreaList.Sum() - meshArea * 2;

                if (temp_centerPtDic.ContainsKey(centerPt))
                {
                    var tempAreaInDic = temp_centerPtDic[centerPt];
                    temp_centerPtDic[centerPt] = tempArea + tempAreaInDic;
                }
                else
                    temp_centerPtDic.Add(centerPt, tempArea);
            }

            centerPtDic = temp_centerPtDic;
            return meshCollection;
        }


        /// <summary>
        /// Create mesh from a list of points
        /// </summary>
        /// <param name="vectorListInput"></param>
        /// <param name="indicesResult"></param>
        /// <returns></returns>
        public static DMesh3 BoundarySrfFromPts(Vector3d[] vectorListInput)
        {
            // Use the triangulator to get indices for creating triangles
            var vectorList = new Vector3d[vectorListInput.Length - 1];
            for (int i = 0; i < vectorListInput.Length - 1; i++)
                vectorList[i] = vectorListInput[i];

            Triangulator tri = new Triangulator(vectorList);
            int[] indices = tri.Triangulate();
            CreateMesh(vectorList, indices, out DMesh3 meshResult);
            return meshResult;
        }
        public static DMesh3 BoundarySrfFromPts(Vector3d[] vectorListInput, out Vector3d centerPt)
        {
            // Use the triangulator to get indices for creating triangles
            var vectorList = new Vector3d[vectorListInput.Length - 1];
            for (int i = 0; i < vectorListInput.Length - 1; i++)
                vectorList[i] = vectorListInput[i];

            Triangulator tri = new Triangulator(vectorList);
            int[] indices = tri.Triangulate();
            CreateMesh(vectorList, indices, out DMesh3 meshResult);
            centerPt = meshResult.CachedBounds.Center;
            return meshResult;
        }
        public static DMesh3 BoundarySrfFromPts(Vector3d[] vectorListInput, out NTS.Geometries.Point centerPt)
        {
            // Use the triangulator to get indices for creating triangles
            var vectorList = new Vector3d[vectorListInput.Length - 1];
            for (int i = 0; i < vectorListInput.Length - 1; i++)
                vectorList[i] = vectorListInput[i];

            Triangulator tri = new Triangulator(vectorList);
            int[] indices = tri.Triangulate();
            CreateMesh(vectorList, indices, out DMesh3 meshResult);
            centerPt = new NTS.Geometries.Point(meshResult.CachedBounds.Center.x, meshResult.CachedBounds.Center.y);
            return meshResult;
        }
        public static DMesh3 BoundarySrfFromPts(Vector3d[] vectorListInput, out NTS.Geometries.Point centerPt, out double meshArea)
        {
            // Use the triangulator to get indices for creating triangles
            var vectorList = new Vector3d[vectorListInput.Length - 1];
            for (int i = 0; i < vectorListInput.Length - 1; i++)
                vectorList[i] = vectorListInput[i];

            Triangulator tri = new Triangulator(vectorList);
            int[] indices = tri.Triangulate();
            CreateMesh(vectorList, indices, out DMesh3 meshResult);
            centerPt = new NTS.Geometries.Point(meshResult.CachedBounds.Center.x, meshResult.CachedBounds.Center.y);

            var triAreaList = new List<double>(meshResult.TriangleCount);
            for (int j = 0; j < meshResult.TriangleCount; j++)
                triAreaList.Add(meshResult.GetTriArea(j));
            meshArea = triAreaList.Sum();
            return meshResult;
        }
        public static void BoundarySrfFromPts(Vector3d[] vectorListInput, out int[] indicesResult)
        {
            // Use the triangulator to get indices for creating triangles
            var vectorList = new Vector3d[vectorListInput.Length - 1];
            for (int i = 0; i < vectorListInput.Length - 1; i++)
                vectorList[i] = vectorListInput[i];

            Triangulator tri = new Triangulator(vectorList);
            indicesResult = tri.Triangulate();
        }

        /// <summary>
        /// extrude a boundary loop of mesh and connect w/ triangle strip
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static DMesh3 ExtrudeMeshEdge(DMesh3 mesh, double height)
        {
            var meshResult = mesh;
            var removeCount = mesh.TriangleCount;

            var removeIndex = new int[removeCount];
            for (int i = 0; i < removeCount; i++)
                removeIndex[i] = i;

            MeshBoundaryLoops loops = new MeshBoundaryLoops(mesh);
            EdgeLoop eLoop = new EdgeLoop(mesh)
            {
                Edges = loops[0].Edges,
                Vertices = loops[0].Vertices
            };
            new MeshExtrudeLoop(meshResult, eLoop)
            {
                PositionF = (v, n, vid) => v + height * Vector3d.AxisZ
            }.Extrude();
            var debug = meshResult.Triangles();
            //MeshLoopClosure meshClose = new MeshLoopClosure(mesh, eLoop);
            //meshClose.Close_Flat();
            MeshEditor.RemoveTriangles(meshResult, removeIndex);
            return meshResult;
        }

        /// <summary>
        /// Extrude a boundary Faces of mesh and connect w/ triangle strip
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static DMesh3 ExtrudeMeshFaces(DMesh3 mesh, int[] triangles, double height)
        {
            var meshResult = mesh;
            new MeshExtrudeFaces(meshResult, triangles, true)
            {
                ExtrudedPositionF = ((Func<Vector3d, Vector3f, int, Vector3d>)((v, n, vid) => v + height * Vector3d.AxisZ))
            }.Extrude();
            return meshResult;
        }

        /// <summary>
        /// Extrude mesh  with certain height and connect w/ triangle strip
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public static DMesh3 ExtrudeMeshFromMesh(DMesh3 mesh, double height)
        {
            var meshResult = mesh;
            new MeshExtrudeMesh(meshResult)
            {
                ExtrudedPositionF = (v, n, vid) => v + height * Vector3d.AxisZ
            }.Extrude();
            return meshResult;
        }

        #endregion

        #region 002_IO
        #endregion

        #region 003_Intersection
        public static void CalcRaysThroughTriParallel(DMesh3 meshIn, int[] IntrMeshIndices, DMeshAABBTree3 spatial, NTS.Geometries.Point[] ptArray, double viewRange, Dictionary<NTS.Geometries.Point, double> ptAreaDic, 
            out ConcurrentDictionary<int, int> MeshIntrCountDic, out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = IntrMeshIndices.Length;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);

            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            System.Threading.Tasks.Parallel.For(0, count, tempIndex =>
            {
                int meshIndex = IntrMeshIndices[tempIndex];
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = centroid.toNTSPt();
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new Vector3d[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = (secPt.tog3Pt());
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                            //hit_dist = centroid.Distance(ray.PointAt(intr.RayParameter));
                            #endregion

                            //double intrDistance=ray.PointAt(rayT).Distance(viewPtList[viewPtIndex]);
                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];

                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                {
                                    var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                    viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + triArea, tempTriArea);
                                    //viewPtIntrAreaDic[viewPtIndex] += triArea;
                                }
                                else
                                    viewPtIntrAreaDic.TryAdd(tempViewPtIndex, triArea);

                                for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                                {
                                    if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                    {
                                        var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                        meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                        //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                    }
                                    else
                                    {
                                        meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });


            MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewRange, ptAreaDic, viewPtIntrAreaDic,
                out List<double> totalVisArea, out List<double> visRatio, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="meshIn"></param>
        /// <param name="IntrMeshIndices"></param>
        /// <param name="spatial"></param>
        /// <param name="ptArray"></param>
        /// <param name="viewRange"></param>
        /// <param name="ptAreaDic"></param>
        /// <param name="MeshIntrCountDic"></param>
        /// <param name="TotalVisArea"></param>
        /// <param name="VisRatio"></param>
        /// <param name="NormalizedVisArea"></param>
        public static void CalcRaysThroughTriParallelDecay(DMesh3 meshIn, int[] IntrMeshIndices, DMeshAABBTree3 spatial, NTS.Geometries.Point[] ptArray, double viewRange, double beta, Dictionary<NTS.Geometries.Point, double> ptAreaDic,
    out ConcurrentDictionary<int, int> MeshIntrCountDic, out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = IntrMeshIndices.Length;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);

            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            System.Threading.Tasks.Parallel.For(0, count, tempIndex =>
            {
                int meshIndex = IntrMeshIndices[tempIndex];
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = centroid.toNTSPt();
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new Vector3d[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = (secPt.tog3Pt());
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                            //hit_dist = centroid.Distance(ray.PointAt(intr.RayParameter));
                            #endregion

                            //double intrDistance=ray.PointAt(rayT).Distance(viewPtList[viewPtIndex]);
                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var decayArea = TriAreaDecay(triArea, distance, beta);

                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];

                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                {
                                    var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                    viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + decayArea, tempTriArea);
                                    //viewPtIntrAreaDic[viewPtIndex] += triArea;
                                }
                                else
                                {
                                    viewPtIntrAreaDic.TryAdd(tempViewPtIndex, decayArea);
                                }
                                    

                                for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                                {
                                    if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                    {
                                        var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                        meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                        //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                    }
                                    else
                                    {
                                        meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });


            MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewRange, ptAreaDic, viewPtIntrAreaDic,
                out List<double> totalVisArea, out List<double> visRatio, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }

        //TODO: 改成Embree
        public static void CalcRaysThroughTriParallel(Rhino.Geometry.Mesh meshIn, NTS.Geometries.Point[] ptArray, double viewRange, Dictionary<NTS.Geometries.Point, double> ptAreaDic, out ConcurrentDictionary<int, int> MeshIntrCountDic,
           out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            Rhino.Geometry.Mesh mesh = meshIn.DuplicateMesh();
            mesh.RebuildNormals();
            ExtractMeshInfo(in mesh, out Vector3[] vertXyzArray, out int[] faceIndexArray, out int vertCount, out int faceCount);
            var count = faceCount;


            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            var rt = new Raytracer();
            rt.AddMesh(vertXyzArray, vertCount, faceIndexArray, faceCount);
            rt.CommitScene();

            System.Threading.Tasks.Parallel.For(0, count, meshIndex =>
            {
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.FaceNormals[meshIndex];
                RhMeshFaceInfo(mesh, meshIndex, out Rhino.Geometry.Point3f centroid, out double triArea);
                int[] indexList = new int[3] { faceIndexArray[3* meshIndex + 0], faceIndexArray[3 * meshIndex + 1], faceIndexArray[3 * meshIndex + 2] };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = new NTS.Geometries.Point(centroid.X, centroid.Y, centroid.Z);
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new NTS.Geometries.Point[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = secPt;
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex].toRhPt() - centroid;

                    //判定方向，是否同向
                    var angle = Rhino.Geometry.Vector3d.Multiply(trisNormals, direction);

                    //判定距离，是否在视域内
                    var distance = centroid4Tree.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray singleRay = new Ray
                        {
                            Origin = new Vector3((float)viewPtList[viewPtIndex].X, (float)viewPtList[viewPtIndex].Y, (float)viewPtList[viewPtIndex].Z),
                            Direction = new Vector3((float)-direction.X, (float)-direction.Y, (float)-direction.Z),
                            MinDistance = 0f
                        };
                        rt.Trace(singleRay, out Hit hit);

                        if (hit.primId!=-1)
                        {
                            var tempPt = viewPtList[viewPtIndex];
                            var tempViewPtIndex = viewPtIndexDic[tempPt];

                            if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                            {
                                var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + triArea, tempTriArea);
                                //viewPtIntrAreaDic[viewPtIndex] += triArea;
                            }
                            else
                                viewPtIntrAreaDic.TryAdd(tempViewPtIndex, triArea);

                            for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                            {
                                if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                {
                                    var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                    meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                    //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                }
                                else
                                {
                                    meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });


            MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewRange, ptAreaDic, viewPtIntrAreaDic,
                out List<double> totalVisArea, out List<double> visRatio, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }

        public static void CalcRaysThroughTriParallel(Rhino.Geometry.Mesh meshIn, NTS.Geometries.Point[] ptArray, double viewRange, Dictionary<NTS.Geometries.Point, double> ptAreaDic,
           out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            Rhino.Geometry.Mesh mesh = meshIn.DuplicateMesh();
            //mesh.Vertices.UseDoublePrecisionVertices = true;
            //mesh.Faces.ConvertQuadsToTriangles();
            //mesh.Vertices.CombineIdentical(true, true);
            //mesh.Vertices.CullUnused();
            //mesh.Weld(Math.PI);
            //mesh.FillHoles();
            

            ExtractMeshInfo(in mesh, out Vector3[] vertXyzArray, out int[] faceIndexArray, out int vertCount, out int faceCount);
            var count = faceCount;


            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            var rt = new Raytracer();
            rt.AddMesh(vertXyzArray, vertCount, faceIndexArray, faceCount);
            rt.CommitScene();

            //var debug = mesh.FaceNormals;

            System.Threading.Tasks.Parallel.For(0, count, meshIndex =>
            {
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.FaceNormals[meshIndex];
                RhMeshFaceInfo(mesh, meshIndex, out Rhino.Geometry.Point3f centroid, out double triArea);
                int[] indexList = new int[3] { faceIndexArray[3 * meshIndex + 0], faceIndexArray[3 * meshIndex + 1], faceIndexArray[3 * meshIndex + 2] };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = new NTS.Geometries.Point(centroid.X, centroid.Y, centroid.Z);
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new NTS.Geometries.Point[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = secPt;
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex].toRhPt() - centroid;

                    //判定方向，是否同向
                    var angle = Rhino.Geometry.Vector3d.Multiply(trisNormals, direction);

                    //判定距离，是否在视域内
                    var distance = centroid4Tree.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray singleRay = new Ray
                        {
                            Origin = new Vector3((float)viewPtList[viewPtIndex].X, (float)viewPtList[viewPtIndex].Y, (float)viewPtList[viewPtIndex].Z),
                            Direction = new Vector3((float)-direction.X, (float)-direction.Y, (float)-direction.Z),
                            MinDistance = 0f
                        };
                        rt.Trace(singleRay, out Hit hit);

                        if (hit.primId != -1)
                        {
                            var tempPt = viewPtList[viewPtIndex];
                            var tempViewPtIndex = viewPtIndexDic[tempPt];

                            if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                            {
                                var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + triArea, tempTriArea);
                                //viewPtIntrAreaDic[viewPtIndex] += triArea;
                            }
                            else
                                viewPtIntrAreaDic.TryAdd(tempViewPtIndex, triArea);

                            for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                            {
                                if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                {
                                    var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                    meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                    //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                }
                                else
                                {
                                    meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });


            //MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewRange, ptAreaDic, viewPtIntrAreaDic,
                out List<double> totalVisArea, out List<double> visRatio, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }

        public static void CalcRaysThroughTriParallel(DMesh3 meshIn, NTS.Geometries.Point[] ptArray, double viewRange, double beta, Dictionary<NTS.Geometries.Point, double> ptAreaDic, out ConcurrentDictionary<int, int> MeshIntrCountDic,
            out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = mesh.TriangleCount;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            System.Threading.Tasks.Parallel.For(0, count, meshIndex =>
            {
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = centroid.toNTSPt();
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new Vector3d[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = (secPt.tog3Pt());
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                            //hit_dist = centroid.Distance(ray.PointAt(intr.RayParameter));
                            #endregion

                            //double intrDistance=ray.PointAt(rayT).Distance(viewPtList[viewPtIndex]);
                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];
                                var decayArea = TriAreaDecay(triArea, distance, beta);
                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                {
                                    var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                    viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + decayArea, tempTriArea);
                                    //viewPtIntrAreaDic[viewPtIndex] += triArea;
                                }
                                else
                                    viewPtIntrAreaDic.TryAdd(tempViewPtIndex, decayArea);

                                for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                                {
                                    if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                    {
                                        var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                        meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                        //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                    }
                                    else
                                    {
                                        meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });


            MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewRange, ptAreaDic, viewPtIntrAreaDic,
                out List<double> totalVisArea, out List<double> visRatio, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }


        public static void CalcRaysThroughTriParallel(DMesh3 meshIn, NTS.Geometries.Point[] ptArray, double viewRange, out ConcurrentDictionary<int, int> MeshIntrCountDic,
    out List<double> TotalVisArea, out List<double> NormalizedVisArea)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = mesh.TriangleCount;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();
            ConcurrentBag<double> areaBag = new ConcurrentBag<double>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            System.Threading.Tasks.Parallel.For(0, count, meshIndex =>
            {
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);
                areaBag.Add(triArea);

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = centroid.toNTSPt();
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new Vector3d[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = (secPt.tog3Pt());
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                            //hit_dist = centroid.Distance(ray.PointAt(intr.RayParameter));
                            #endregion

                            //double intrDistance=ray.PointAt(rayT).Distance(viewPtList[viewPtIndex]);
                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];

                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                {
                                    var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                    viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + triArea, tempTriArea);
                                    //viewPtIntrAreaDic[viewPtIndex] += triArea;
                                }
                                else
                                    viewPtIntrAreaDic.TryAdd(tempViewPtIndex, triArea);

                                for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                                {
                                    if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                    {
                                        var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                        meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                        //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                    }
                                    else
                                    {
                                        meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });

            var areaWhole = areaBag.Sum();
            MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewPtIntrAreaDic, areaWhole,
                out List<double> totalVisArea, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            NormalizedVisArea = normalizedVisArea;
        }

        public static void CalcRaysThroughTriParallelDecay(DMesh3 meshIn, NTS.Geometries.Point[] ptArray, double viewRange, double beta, out ConcurrentDictionary<int, int> MeshIntrCountDic,
out List<double> TotalVisArea, out List<double> NormalizedVisArea)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = mesh.TriangleCount;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            ConcurrentDictionary<int, int> meshIntrCountDic = new ConcurrentDictionary<int, int>();// meshVertex Index, hit count
            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();
            ConcurrentBag<double> areaBag = new ConcurrentBag<double>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            System.Threading.Tasks.Parallel.For(0, count, meshIndex =>
            {
                //var trisNormals = -mesh.GetTriNormal(meshIndex);
                var trisNormals = mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);
                areaBag.Add(triArea);

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = centroid.toNTSPt();
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new Vector3d[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList[j] = (secPt.tog3Pt());
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 四叉树排除点");

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                            //hit_dist = centroid.Distance(ray.PointAt(intr.RayParameter));
                            #endregion


                            //double intrDistance=ray.PointAt(rayT).Distance(viewPtList[viewPtIndex]);
                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];
                                var decayArea = TriAreaDecay(triArea, distance, beta);

                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                {
                                    var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                    viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + decayArea, tempTriArea);
                                    //viewPtIntrAreaDic[viewPtIndex] += triArea;
                                }
                                else
                                    viewPtIntrAreaDic.TryAdd(tempViewPtIndex, decayArea);

                                for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                                {
                                    if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                    {
                                        var tempCount = meshIntrCountDic[indexList[vertexIndex]];
                                        meshIntrCountDic.TryUpdate(indexList[vertexIndex], tempCount + 1, tempCount);
                                        //meshIntrCountDic[indexList[vertexIndex]] += 1;
                                    }
                                    else
                                    {
                                        meshIntrCountDic.TryAdd(indexList[vertexIndex], 1);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });

            var areaWhole = areaBag.Sum();
            MeshIntrCountDic = meshIntrCountDic;
            CalcVisData(ptArray, viewPtIntrAreaDic, areaWhole,
                out List<double> totalVisArea, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            NormalizedVisArea = normalizedVisArea;
        }

        public static void CalcRaysThroughTriWithoutColorParallelDecay(DMesh3 meshIn, NTS.Geometries.Point[] ptArray, double viewRange, double beta, Dictionary<NTS.Geometries.Point, double> ptAreaDic, 
            out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = mesh.TriangleCount;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            ConcurrentDictionary<int, double> viewPtIntrAreaDic = new ConcurrentDictionary<int, double>();//viewPoint Index, hit mesh area
            ConcurrentDictionary<NTS.Geometries.Point, int> viewPtIndexDic = new ConcurrentDictionary<NTS.Geometries.Point, int>();

            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.TryAdd(tempPt, i);
            }

            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Point>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i]);


            System.Threading.Tasks.Parallel.For(0, count, meshIndex =>
            {
                var trisNormals = mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                //To Do 用NTS进行四叉树索引
                var centroid4Tree = centroid.toNTSPt();
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);
                var viewPtList = new Vector3d[secPtListQuery.Count];
                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    viewPtList[j] = (secPt.tog3Pt());
                }

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Length; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离，并加入viewPt计算列表
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);

                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];
                                var decayArea = TriAreaDecay(triArea, distance, beta);

                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                {
                                    var tempTriArea = viewPtIntrAreaDic[tempViewPtIndex];
                                    viewPtIntrAreaDic.TryUpdate(tempViewPtIndex, tempTriArea + decayArea, tempTriArea);
                                    //viewPtIntrAreaDic[viewPtIndex] += triArea;
                                }
                                else
                                    viewPtIntrAreaDic.TryAdd(tempViewPtIndex, decayArea);
                            }
                            #endregion
                        }
                        #endregion
                    }
                }
                //ToolManagers.TimeCalculation(start, $"{meshIndex} 判断相切");
            });


            CalcVisData(ptArray, viewRange, ptAreaDic, viewPtIntrAreaDic,
                out List<double> totalVisArea, out List<double> visRatio, out List<double> normalizedVisArea);
            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }

        public static List<double> CalcRaysThroughTri(DMesh3 meshIn, NTS.Geometries.Point[] ptArray, double viewRange, Dictionary<NTS.Geometries.Point, double> ptAreaDic, VisDataType visType, out Dictionary<int, int> MeshIntrCountDic)
        {
            DMesh3 mesh = new DMesh3(meshIn);
            var count = mesh.TriangleCount;
            //var viewPtList = NTSPtList2Vector3dList_3d(ptArray);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(mesh);
            spatial.Build();

            Dictionary<int, int> meshIntrCountDic = new Dictionary<int, int>();// meshVertex Index, hit count
            Dictionary<int, double> viewPtIntrAreaDic = new Dictionary<int, double>();//viewPoint Index, hit mesh area

            //Dictionary<int, List<int>> debug_ptMeshIndex = new Dictionary<int, List<int>>();

            Dictionary<NTS.Geometries.Point, int> viewPtIndexDic = new Dictionary<NTS.Geometries.Point, int>();
            for (int i = 0; i < ptArray.Length; i++)
            {
                var tempPt = ptArray[i];
                if (viewPtIndexDic.ContainsKey(tempPt))
                    viewPtIndexDic[tempPt] = i;
                else
                    viewPtIndexDic.Add(tempPt, i);
            }


            NTS.Index.Quadtree.Quadtree<NTS.Geometries.Coordinate> quadTree = new NTS.Index.Quadtree.Quadtree<NTS.Geometries.Coordinate>();
            for (int i = 0; i < ptArray.Length; i++)
                quadTree.Insert(ptArray[i].EnvelopeInternal, ptArray[i].Coordinate);

            for (int meshIndex = 0; meshIndex < count; meshIndex++)
            {
                var trisNormals = -mesh.GetTriNormal(meshIndex);
                var triArea = mesh.GetTriArea(meshIndex);
                //debugNormalList.Add(trisNormals);
                if (trisNormals.z == 1d || trisNormals.z == -1d)
                    continue;

                var centroid = mesh.GetTriCentroid(meshIndex);
                var vertexList = mesh.GetTriangle(meshIndex);
                int[] indexList = new int[3] { vertexList.a, vertexList.b, vertexList.c };

                var centroid4Tree = centroid.toNTSPt();
                //var mainCoor = new NTS.Geometries.Coordinate(centroid4Tree.X, centroid4Tree.Y);
                var tempEnv = Poly2DCreation.CreateEnvelopeFromPt(centroid4Tree, viewRange);
                var secPtListQuery = quadTree.Query(tempEnv);

                List<Vector3d> viewPtList = new List<Vector3d>();

                for (int j = 0; j < secPtListQuery.Count; j++)
                {
                    var secPt = secPtListQuery[j];
                    //NTS.Geometries.Coordinate secCoor = new NTS.Geometries.Coordinate(secPt.X, secPt.Y);
                    //double dis = mainCoor.Distance(secCoor);
                    //if (dis < viewRange)
                    viewPtList.Add(new NTS.Geometries.Point(secPt).tog3Pt());
                }

                for (int viewPtIndex = 0; viewPtIndex < viewPtList.Count; viewPtIndex++)
                {
                    var direction = viewPtList[viewPtIndex] - centroid;

                    //判定方向，是否同向
                    var angle = (trisNormals).Dot(direction);

                    //判定距离，是否在视域内
                    var distance = centroid.Distance(viewPtList[viewPtIndex]);

                    //debugAngleList.Add(angle);
                    //debugDistanceList.Add(distance);

                    if (angle > 0 && distance < viewRange)
                    {
                        #region 计算被击中的次数
                        Ray3d ray = new Ray3d(viewPtList[viewPtIndex], -direction);
                        int hit_tid = spatial.FindNearestHitTriangle(ray);
                        if (hit_tid != DMesh3.InvalidID)
                        {
                            #region 计算射线距离
                            IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                            //hit_dist = centroid.Distance(ray.PointAt(intr.RayParameter));
                            #endregion

                            //double intrDistance=ray.PointAt(rayT).Distance(viewPtList[viewPtIndex]);
                            if (Math.Abs(intr.RayParameter - distance) < 0.0001)
                            {
                                var tempPt = viewPtList[viewPtIndex].toNTSPt();
                                var tempViewPtIndex = viewPtIndexDic[tempPt];

                                //if (debug_ptMeshIndex.ContainsKey(tempViewPtIndex))
                                //    debug_ptMeshIndex[tempViewPtIndex].Add(meshIndex);
                                //else
                                //    debug_ptMeshIndex.Add(tempViewPtIndex, new List<int>() { meshIndex});

                                if (viewPtIntrAreaDic.ContainsKey(tempViewPtIndex))
                                    viewPtIntrAreaDic[tempViewPtIndex] += triArea;
                                else
                                    viewPtIntrAreaDic.Add(tempViewPtIndex, triArea);


                                for (int vertexIndex = 0; vertexIndex < 3; vertexIndex++)
                                {
                                    if (meshIntrCountDic.ContainsKey(indexList[vertexIndex]))
                                    {
                                        meshIntrCountDic[indexList[vertexIndex]] += 1;
                                    }
                                    else
                                    {
                                        meshIntrCountDic.Add(indexList[vertexIndex], 1);
                                    }
                                }
                            }
                        }
                        #endregion

                    }
                }
            }

            var visRatio = new List<double>(ptArray.Length);
            switch (visType)
            {
                case VisDataType.TotalVisArea:
                    for (int i = 0; i < ptArray.Length; i++)
                    {
                        if (!viewPtIntrAreaDic.ContainsKey(i))
                            visRatio.Add(0d);
                        else
                            visRatio.Add(viewPtIntrAreaDic[i]);
                    }
                    break;
                case VisDataType.VisRatio:

                    Dictionary<int, double> areaDic = Poly2DCreation.ContainsAreaInPts(ptArray, ptAreaDic, viewRange);
                    for (int i = 0; i < ptArray.Length; i++)
                    {
                        if (!viewPtIntrAreaDic.ContainsKey(i))
                            visRatio.Add(0d);
                        else
                            visRatio.Add(viewPtIntrAreaDic[i] / areaDic[i]);
                    }
                    break;
                case VisDataType.normalizedVisRatio:
                    var total = viewPtIntrAreaDic.Values.ToList().Sum();
                    for (int i = 0; i < ptArray.Length; i++)
                    {
                        if (!viewPtIntrAreaDic.ContainsKey(i))
                            visRatio.Add(0d);
                        else
                            visRatio.Add(viewPtIntrAreaDic[i] / total);
                    }
                    break;
            }

            MeshIntrCountDic = meshIntrCountDic;
            return visRatio;
        }

        public static Dictionary<int, int> CalcRays(DMesh3 mesh, Vector3d origin, int segmentHeight = 10, int segment = 10, double angle = 360, double radius = 100, double angleHeight = 90)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(meshIn);
            spatial.Build();

            var direction = CreateSphereDirection(origin, segmentHeight, segment, angle, radius, angleHeight);
            //number of hitted vertex
            Dictionary<int, int> hitIndexDic = new Dictionary<int, int>();
            for (int i = 0; i < direction.Length; i++)
            {
                Ray3d ray = new Ray3d(origin, direction[i]);

                #region 计算被击中的次数
                int hit_tid = spatial.FindNearestHitTriangle(ray);
                if (hit_tid != DMesh3.InvalidID)
                {
                    #region 计算射线距离
                    IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                    double hit_dist = origin.Distance(ray.PointAt(intr.RayParameter));
                    #endregion

                    #region 判定是否在距离内，如果是，提取三角形的顶点序号
                    if (hit_dist <= radius)
                    {
                        var tempTri = meshIn.GetTriangle(hit_tid);
                        for (int eachVertex = 0; eachVertex < tempTri.array.Length; eachVertex++)
                        {
                            var hit_vid = tempTri[eachVertex];
                            if (hitIndexDic.ContainsKey(hit_vid))
                            {
                                var temp_amount = hitIndexDic[hit_vid];
                                hitIndexDic[hit_vid] = temp_amount + 1;
                            }
                            else
                            {
                                hitIndexDic.Add(hit_vid, 1);
                            }
                        }
                    }
                    #endregion
                }
                #endregion
            }
            return hitIndexDic;
            //return hitTrianglesDic;
        }

        public static Dictionary<int, int> CalcRays(DMesh3 mesh, IEnumerable<Vector3d> originList, int segmentHeight = 10, int segment = 10, double angle = 360, double radius = 100, double angleHeight = 90)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(meshIn);
            spatial.Build();
            Dictionary<int, int> hitIndexDic = new Dictionary<int, int>();

            for (int ptIndex = 0; ptIndex < originList.Count(); ptIndex++)
            {
                var origin = originList.ElementAt(ptIndex);
                var direction = CreateSphereDirection(origin, segmentHeight, segment, angle, radius, angleHeight);
                //number of hitted vertex
                for (int i = 0; i < direction.Length; i++)
                {
                    Ray3d ray = new Ray3d(origin, direction[i]);

                    #region 计算被击中的次数
                    int hit_tid = spatial.FindNearestHitTriangle(ray);
                    if (hit_tid != DMesh3.InvalidID)
                    {
                        #region 计算射线距离
                        IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                        double hit_dist = origin.Distance(ray.PointAt(intr.RayParameter));
                        #endregion

                        #region 判定是否在距离内，如果是，存入三角形序号
                        if (hit_dist <= radius)
                        {
                            var tempTri = meshIn.GetTriangle(hit_tid);
                            for (int eachVertex = 0; eachVertex < tempTri.array.Length; eachVertex++)
                            {
                                var hit_vid = tempTri[eachVertex];
                                if (hitIndexDic.ContainsKey(hit_vid))
                                {
                                    var temp_amount = hitIndexDic[hit_vid];
                                    hitIndexDic[hit_vid] = temp_amount + 1;
                                }
                                else
                                {
                                    hitIndexDic.Add(hit_vid, 1);
                                }
                            }
                        }
                        #endregion
                    }
                    #endregion
                }
            }
            return hitIndexDic;
            //return hitTrianglesDic;
        }

        public static double[] CalcRaysThroughPt(DMesh3 mesh, IEnumerable<Vector3d> originList, int segmentHeight = 10, int segment = 10, double angle = 360, double radius = 100, double angleHeight = 90)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            DMeshAABBTree3 spatial = new DMeshAABBTree3(meshIn);
            spatial.Build();
            double[] hitTriArea = new double[originList.Count()];

            for (int ptIndex = 0; ptIndex < originList.Count(); ptIndex++)
            {
                var origin = originList.ElementAt(ptIndex);
                var direction = CreateSphereDirection(origin, segmentHeight, segment, angle, radius, angleHeight);
                var rayArea = 0d;
                //number of hitted vertex
                for (int i = 0; i < direction.Length; i++)
                {
                    Ray3d ray = new Ray3d(origin, direction[i]);

                    #region 计算被击中的次数
                    int hit_tid = spatial.FindNearestHitTriangle(ray);
                    if (hit_tid != DMesh3.InvalidID)
                    {
                        #region 计算射线距离
                        IntrRay3Triangle3 intr = MeshQueries.TriangleIntersection(mesh, hit_tid, ray);
                        double hit_dist = origin.Distance(ray.PointAt(intr.RayParameter));
                        #endregion

                        #region 判定是否在距离内，如果是，加上先前面积
                        if (hit_dist <= radius)
                        {
                            var triArea = meshIn.GetTriArea(hit_tid);
                            rayArea += triArea;
                        }
                        #endregion
                    }
                    #endregion
                }

                hitTriArea[ptIndex] = rayArea;
            }
            return hitTriArea;
            //return hitTrianglesDic;
        }
        /// <summary>
        /// based on origin, create a sphere
        /// </summary>
        /// <param name="origin"></param>
        /// <param name="segment">subdivision count</param>
        /// <param name="angle">horizontal visible angle</param>
        /// <param name="radius">how far could we see</param>
        /// <param name="angleHeight">vertical visible angle, <=90</param>
        public static Vector3d[] CreateSphereDirection(Vector3d origin, int segmentHeight, int segment, double angle, double radius, double angleHeight)
        {
            if (angleHeight > 90)
                angleHeight = 90;
            if (angle > 360)
                angle = 360;
            double _angleHeight = Math.PI * angleHeight / 180 / segmentHeight;
            double _angle = Math.PI * angle / 180 / segment;

            Vector3d[] vertices = new Vector3d[(segmentHeight) * (segment)];
            int index = 0;

            for (int y = 0; y < segmentHeight; y++)
            {
                for (int x = 0; x < segment; x++)
                {
                    double _z = Math.Sin(y * _angleHeight) * radius;
                    double _x = Math.Cos(y * _angleHeight) * Math.Cos(x * _angle) * radius;
                    double _y = Math.Cos(y * _angleHeight) * Math.Sin(x * _angle) * radius;
                    vertices[index] = new Vector3d(new Vector3d(origin.x + _x, origin.y + _y, origin.z + _z) - origin);
                    index++;
                }
            }

            return vertices;
        }

        public static DMesh3 ApplyColorsBasedOnRays(DMesh3 mesh, Dictionary<int, int> hitTrianglesDic, Colorf originColor, Colorf DestnationColor)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            try
            {
                var maxNumber = hitTrianglesDic.Values.Max();

                int i = 0;

                foreach (var item in hitTrianglesDic)
                {
                    //var scalarValue = item.Value / (float)maxNumber;
                    var scalarValue = (float)Math.Log(item.Value, maxNumber);
                    var tempColor = Colorf.Lerp(originColor, DestnationColor, scalarValue);
                    meshIn.SetVertexColor(item.Key, tempColor);
                    i++;
                }

            }
            catch
            {
                throw new MyException("there is no intersection between brep and view point");
            }

            return meshIn;
        }

        //TODO: Rhino Mesh Color
        public static Rhino.Geometry.Mesh ApplyColorsBasedOnRays(Rhino.Geometry.Mesh mesh, ConcurrentDictionary<int, int> hitTrianglesDic, Colorf originColor, Colorf DestnationColor)
        {
            var meshIn = mesh.DuplicateMesh();
            var baseColor=Colorf.LightGrey;

            int[] faceIndices = new int[mesh.Faces.Count];
            for (int index = 0; index < mesh.Faces.Count; index++)
            {
                if (!hitTrianglesDic.ContainsKey(index))
                {
                    meshIn.VertexColors.Add((int)baseColor.r, (int)baseColor.g, (int)baseColor.b);
                }
                else
                {
                    var maxNumber = hitTrianglesDic.Values.Max();
                    //var scalarValue = item.Value / (float)maxNumber;
                    var scalarValue = (float)Math.Log(hitTrianglesDic[index], maxNumber);
                    var tempColor = Colorf.Lerp(originColor, DestnationColor, scalarValue);

                    meshIn.VertexColors.Add((int)tempColor.r, (int)tempColor.g, (int)tempColor.b);
                }
                faceIndices[index] = index;
            }
            //catch
            //{
            //    throw new MyException("there is no intersection between brep and view point");
            //}

            return meshIn;
        }
        public static DMesh3 ApplyColorsBasedOnRays(DMesh3 mesh, ConcurrentDictionary<int, int> hitTrianglesDic, Colorf originColor, Colorf DestnationColor)
        {
            DMesh3 meshIn = new DMesh3(mesh);
            try
            {
                var maxNumber = hitTrianglesDic.Values.Max();

                int i = 0;

                foreach (var item in hitTrianglesDic)
                {
                    //var scalarValue = item.Value / (float)maxNumber;
                    var scalarValue = (float)Math.Log(item.Value, maxNumber);
                    var tempColor = Colorf.Lerp(originColor, DestnationColor, scalarValue);
                    meshIn.SetVertexColor(item.Key, tempColor);
                    i++;
                }

            }
            catch
            {
                throw new MyException("there is no intersection between brep and view point");
            }

            return meshIn;
        }

        public static double[] CalcVisibilityPercent(double[] visibleArea, double[] wholeArea)
        {
            double[] result = new double[visibleArea.Length];
            for (int i = 0; i < visibleArea.Length; i++)
            {
                result[i] = visibleArea[i] / wholeArea[i];
            }
            return result;
        }

        private static void CalcVisData(NTS.Geometries.Point[] ptArray, double viewRange, Dictionary<NTS.Geometries.Point, double> ptAreaDic, ConcurrentDictionary<int, double> viewPtIntrAreaDic,
                                        out List<double> TotalVisArea, out List<double> VisRatio, out List<double> NormalizedVisArea)
        {
            var totalVisArea = new List<double>(ptArray.Length);
            var visRatio = new List<double>(ptArray.Length);
            var normalizedVisArea = new List<double>(ptArray.Length);

            var total = viewPtIntrAreaDic.Values.ToList().Sum();
            Dictionary<int, double> areaDic = Poly2DCreation.ContainsAreaInPts(ptArray, ptAreaDic, viewRange);
            for (int i = 0; i < ptArray.Length; i++)
            {
                if (!viewPtIntrAreaDic.ContainsKey(i))
                {
                    totalVisArea.Add(0d);
                    visRatio.Add(0d);
                    normalizedVisArea.Add(0d);
                }
                else
                {
                    totalVisArea.Add(viewPtIntrAreaDic[i]);
                    visRatio.Add(viewPtIntrAreaDic[i] / areaDic[i]);
                    normalizedVisArea.Add(viewPtIntrAreaDic[i] / total);
                }
            }

            TotalVisArea = totalVisArea;
            VisRatio = visRatio;
            NormalizedVisArea = normalizedVisArea;
        }

        private static void CalcVisData(NTS.Geometries.Point[] ptArray, ConcurrentDictionary<int, double> viewPtIntrAreaDic, double wholeArea,
                                        out List<double> TotalVisArea, out List<double> NormalizedVisArea)
        {
            var totalVisArea = new List<double>(ptArray.Length);
            var normalizedVisArea = new List<double>(ptArray.Length);

            //var total = viewPtIntrAreaDic.Values.ToList().Sum();
            for (int i = 0; i < ptArray.Length; i++)
            {
                if (!viewPtIntrAreaDic.ContainsKey(i))
                {
                    totalVisArea.Add(0d);
                    normalizedVisArea.Add(0d);
                }
                else
                {
                    totalVisArea.Add(viewPtIntrAreaDic[i]);
                    normalizedVisArea.Add(viewPtIntrAreaDic[i] / wholeArea);
                }
            }

            TotalVisArea = totalVisArea;
            NormalizedVisArea = normalizedVisArea;
        }

        private static double TriAreaDecay(double triArea, double distance, double beta)
        {
            var tempDistance = distance == 0 ? 1 : distance;
            var decayArea= triArea * Math.Exp(-1 * beta * tempDistance);

            return decayArea;
        }
        #endregion

        #region 004_Generating polyline
        //public static Polygon2d[] CreatePolygon(string jsonFilePath)
        //{
        //    var jsonData = ReadJsonData2D(jsonFilePath);
        //    var polygonList = new Polygon2d[jsonData.Length];
        //    for (int i = 0; i < jsonData.Length; i++)
        //    {
        //        Polygon2d polygon = new Polygon2d(jsonData[i]);
        //        polygonList[i] = polygon;
        //    }
        //    return polygonList;
        //}

        public static Circle2d[] CreateCircle(Vector2d[] origin, double radius)
        {
            var circleList = new Circle2d[origin.Length];
            for (int i = 0; i < origin.Length; i++)
            {
                Circle2d circle = new Circle2d(origin[i], radius);
                circleList[i] = circle;
            }
            return circleList;

        }

        public static Vector2d[] ConvertV3toV2(Vector3d[] origin)
        {
            var result = new Vector2d[origin.Length];
            for (int i = 0; i < origin.Length; i++)
            {
                result[i] = new Vector2d(origin[i].x, origin[i].y);
            }
            return result;
        }

        public static Vector3d[] ConvertV2toV3(Vector2d[] origin, double[] zValue)
        {
            var result = new Vector3d[origin.Length];
            for (int i = 0; i < origin.Length; i++)
            {
                result[i] = new Vector3d(origin[i].x, origin[i].y, zValue[i]);
            }
            return result;
        }
        #endregion

        #region 005_convert
        #region RhinoMesh
        public static Rh.Mesh ConvertFromDMesh3(DMesh3 meshInput)
        {
            Rh.Mesh meshOutput = new Rh.Mesh();

            var tempMeshInputVertices = meshInput.Vertices().ToArray();
            var rhVerticesList = ConvertFromDMeshVector(tempMeshInputVertices);
            var rhFacesList = ConvertFromDMeshTri(meshInput.Triangles().ToArray());

            meshOutput.Vertices.AddVertices(rhVerticesList);
            meshOutput.Faces.AddFaces(rhFacesList);

            for (int i = 0; i < tempMeshInputVertices.Length; i++)
            {
                meshOutput.VertexColors.Add(ConvertFromDMeshColor(meshInput.GetVertexColor(i)));
            }

            meshOutput.Faces.ConvertTrianglesToQuads(0.034907, 0.875);
            return meshOutput;

        }

        public static Rh.Mesh ConvertFromDMesh3NoColor(DMesh3 meshInput)
        {
            Rh.Mesh meshOutput = new Rh.Mesh();

            var tempMeshInputVertices = meshInput.Vertices().ToArray();
            var rhVerticesList = ConvertFromDMeshVector(tempMeshInputVertices);
            var rhFacesList = ConvertFromDMeshTri(meshInput.Triangles().ToArray());

            meshOutput.Vertices.AddVertices(rhVerticesList);
            meshOutput.Faces.AddFaces(rhFacesList);

            return meshOutput;
        }

        private static Rh.Point3d[] ConvertFromDMeshVector(Vector3d[] meshVertices)
        {
            Rh.Point3d[] ptResult = new Rh.Point3d[meshVertices.Length];
            for (int i = 0; i < meshVertices.Length; i++)
            {
                ptResult[i] = new Rh.Point3d(meshVertices[i].x, meshVertices[i].y, meshVertices[i].z);
            }
            return ptResult;
        }

        private static Rh.MeshFace[] ConvertFromDMeshTri(Index3i[] meshTri)
        {
            Rh.MeshFace[] meshResult = new Rh.MeshFace[meshTri.Length];
            for (int i = 0; i < meshTri.Length; i++)
            {
                meshResult[i] = new Rh.MeshFace(meshTri[i].a, meshTri[i].b, meshTri[i].c);
            }
            return meshResult;
        }

        public static void CreateBrepMinusTopBtn(Rh.Brep single, Rh.MeshingParameters mp, out Rh.Mesh resultTopBtn, out Rh.Mesh resultSides, out double size, out Rh.Point3d centPt)
        {
            var faceList = single.Faces;
            var heightList = new List<double>(faceList.Count);
            var indexList = new List<int>(faceList.Count);
            for (int j = 0; j < faceList.Count; j++)
            {
                var singleFace = faceList[j];
                var boundingBox = singleFace.GetBoundingBox(false);
                heightList.Add(boundingBox.Center.Z);
                indexList.Add(j);
            }
            var max = heightList.Max();
            var min = heightList.Min();
            var indexMax = heightList.IndexOf(max);
            var indexMin = heightList.IndexOf(min);
            indexList.Remove(indexMax);
            indexList.Remove(indexMin);

            //输出 四角面top btn
            Rh.Mesh topBtnMesh = new Rh.Mesh();
            var topMesh = single.Faces[indexMax];
            topMesh.ShrinkFace(Rh.BrepFace.ShrinkDisableSide.ShrinkAllSides);
            var btnMesh = single.Faces[indexMin];
            btnMesh.ShrinkFace(Rh.BrepFace.ShrinkDisableSide.ShrinkAllSides);

            topBtnMesh.Append(Rh.Mesh.CreateFromSurface(topMesh, mp));
            topBtnMesh.Append(Rh.Mesh.CreateFromSurface(btnMesh, mp));
            resultTopBtn = topBtnMesh;

            //输出 三角面side
            Rh.Brep result = new Rh.Brep();
            var sumArea = 0d;
            for (int k = 0; k < indexList.Count; k++)
            {
                var tempBrep = single.Faces[indexList[k]].ToBrep();
                sumArea += tempBrep.GetArea();
                result.Append(tempBrep);
            }

            var tempMeshArray = Rh.Mesh.CreateFromBrep(result, mp);
            var sideMesh = new Rh.Mesh();
            sideMesh.Append(tempMeshArray);
            sideMesh.Faces.ConvertQuadsToTriangles();
            resultSides = sideMesh;

            //输出中心点
            centPt = single.GetBoundingBox(false).Center;

            //输出面积
            size = sumArea;
        }

        public static void CreateMeshFromWholeMesh(Rh.Mesh single, out double size, out Rh.Point3d centPt)
        {
            //输出 三角面side
            size=Rh.AreaMassProperties.Compute(single).Area;
            centPt= single.GetBoundingBox(false).Center;
        }

        public static Color ConvertFromDMeshColor(Vector3f vertexColor)
        {
            var tempColor = ConvertColorValueBasedFloat(vertexColor.x, vertexColor.y, vertexColor.z);
            return Color.FromArgb(tempColor[0], tempColor[1], tempColor[2]);
        }

        private static int[] ConvertColorValueBasedFloat(float r, float g, float b)
        {
            int[] colorResult = new int[3];
            colorResult[0] = Math.Max(0, Math.Min(255, (int)Math.Floor(r * 256.0)));
            colorResult[1] = Math.Max(0, Math.Min(255, (int)Math.Floor(g * 256.0)));
            colorResult[2] = Math.Max(0, Math.Min(255, (int)Math.Floor(b * 256.0)));
            return colorResult;
        }
        #endregion

        public static Dictionary<NetTopologySuite.Geometries.Point, double> GenerateDic(double[] areaList, Rh.Point3d[] ptList)
        {
            Dictionary<NetTopologySuite.Geometries.Point, double> ptDic = new Dictionary<NTS.Geometries.Point, double>();
            for (int i = 0; i < areaList.Length; i++)
            {
                var pt2d = ConvertFromRh_Point2D(ptList[i]);
                if (ptDic.ContainsKey(pt2d))
                {
                    ptDic[pt2d] += areaList[i];
                }
                else
                {
                    ptDic.Add(pt2d, areaList[i]);
                }

            }
            return ptDic;
        }

        private static NTS.Geometries.Point ConvertFromRh_Point2D(Rh.Point3d pt)
        {
            return new NTS.Geometries.Point(pt.X, pt.Y, 0);
        }

        public static NTS.Geometries.Point[] ConvertFromRh_Point(IEnumerable<Rh.Point3d> pts)
        {
            var count = pts.Count();
            NTS.Geometries.Point[] ptResult = new NTS.Geometries.Point[count];
            for (int i = 0; i < count; i++)
            {
                ptResult[i] = new NTS.Geometries.Point(pts.ElementAt(i).X, pts.ElementAt(i).Y, pts.ElementAt(i).Z);
            }
            return ptResult;
        }

        private static void ExtractMeshInfo(in Rhino.Geometry.Mesh mesh, out Vector3[] vertXyzArray, out int[] faceIndexArray, out int vertCount, out int faceCount)
        {
            //m.Vertices.UseDoublePrecisionVertices = true;
            //m.Faces.ConvertQuadsToTriangles();
            //m.Vertices.CombineIdentical(true, true);
            //m.Vertices.CullUnused();
            //m.Weld(Math.PI);
            //m.FillHoles();
            //m.RebuildNormals();

            vertXyzArray = new Vector3[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                var tempPt = mesh.Vertices.Point3dAt(i);
                vertXyzArray[i] = new Vector3((float)tempPt.X, (float)tempPt.Y, (float)tempPt.Z);
            }

            vertCount = mesh.Vertices.Count;

            faceIndexArray = mesh.Faces.ToIntArray(true);
            faceCount=mesh.Faces.Count;
        }

        private static void RhMeshFaceInfo(in Rhino.Geometry.Mesh mesh, int faceInx, out Rhino.Geometry.Point3f center, out double area)
        {
            mesh.Faces.GetFaceVertices(faceInx, out var a, out var b, out var c, out _);

            center = RhMeshFaceCenter(a, b, c);
            area=ReMeshFaceArea(a, b, c);
        }

        private static Rhino.Geometry.Point3f RhMeshFaceCenter(Rhino.Geometry.Point3f a, Rhino.Geometry.Point3f b, Rhino.Geometry.Point3f c)
        {
            var x = (a.X + b.X + c.X) / 3f;
            var y = (a.Y + b.Y + c.Y) / 3f;
            var z = (a.Z + b.Z + c.Z) / 3f;
            return new Rh.Point3f(x, y, z);
        }

        private static double ReMeshFaceArea(Rhino.Geometry.Point3f pt1, Rhino.Geometry.Point3f pt2, Rhino.Geometry.Point3f pt3)
        {
            double a = pt1.DistanceTo(pt2);
            double b = pt2.DistanceTo(pt3);
            double c = pt3.DistanceTo(pt1);
            double s = (a + b + c) / 2;
            return Math.Sqrt(s * (s - a) * (s - b) * (s - c));
        }



        #region RhinoPt
        #endregion

        #region PlanktonMesh


        public static NTS.Geometries.Point PMeshVertex2NTSPt(PlanktonVertex ptIn)
        {
            return new NTS.Geometries.Point(ptIn.X, ptIn.Y, ptIn.Z);
        }
        #endregion
        #endregion
        public static DMesh3 UnWeldMesh(DMesh3 input)
        {
            DMesh3 mesh = new DMesh3(input);
            int triCount = mesh.TriangleCount;

            DMesh3 meshResult = new DMesh3();

            for (int i = 0; i < triCount; i++)
            {
                var v0 = new Vector3d();
                var v1 = new Vector3d();
                var v2 = new Vector3d();
                mesh.GetTriVertices(i, ref v0, ref v1, ref v2);
                DMesh3 smallMesh = CreateSingleMesh(v0, v1, v2);
                MeshEditor.Append(meshResult, smallMesh);
            }
            return meshResult;
        }

        public static DMesh3 CreateSingleMesh(Vector3d v0, Vector3d v1, Vector3d v2)
        {
            var triangles = new Index3i[1] { new Index3i(0, 1, 2) };
            var normal = CalculateNormal(v0, v1, v2);
            var normals = new Vector3f[3] { normal, normal, normal };
            return DMesh3Builder.Build(Vertices: new Vector3d[] { v0, v1, v2 }, Triangles: triangles, Normals: normals);
        }

        public static Vector3f CalculateNormal(Vector3d firstPoint, Vector3d secondPoint, Vector3d thirdPoint)
        {
            var u = new Vector3d(firstPoint.x - secondPoint.x,
                firstPoint.y - secondPoint.y,
                firstPoint.z - secondPoint.z);

            var v = new Vector3d(secondPoint.x - thirdPoint.x,
                secondPoint.y - thirdPoint.y,
                secondPoint.z - thirdPoint.z);

            return new Vector3f(u.y * v.z - u.z * v.y, u.z * v.x - u.x * v.z, u.x * v.y - u.y * v.x);
        }
        class MyException : Exception
        {
            public MyException(string message) : base(message)
            {
            }
        }


    }

    public class MeshInfoCollection
    {
        public int Id { get; set; }
        public NTS.Geometries.Point CentPt { get; set; }
        public double Area { get; set; }
    }

    public class VertexData
    {
        public bool IsAnchor;
        public bool IsBoundary;
        public List<int> CreaseIndices = new List<int>();
        public List<int> NeighbourIndices = new List<int>();

        public VertexData(bool SetAnchor, bool SetBoundary)
        {
            IsAnchor = SetAnchor;
            IsBoundary = SetBoundary;
        }
    }

    public class ViewPt
    {
        private readonly double _wholeArea;
        private readonly double _viewArea;

        private int _index;
        public int Index
        {
            get { return _index; }
            set { _index = value; }
        }
        public NTS.Geometries.Point Pt { get; set; }

        private double _viewRatio;
        public double ViewRatio
        {
            get
            {
                return _viewRatio;
            }
            set
            {
                if (_wholeArea != 0)
                    _viewRatio = _viewArea / _wholeArea;
                else
                    _viewRatio = 0;
            }
        }

        public ViewPt(NTS.Geometries.Point point, int index, double viewArea, double wholeArea)
        {
            Pt = point;
            _index = index;
            _wholeArea = wholeArea;
            _viewArea = viewArea;
        }

        public ViewPt(NTS.Geometries.Point point, int index, double wholeArea)
        {
            Pt = point;
            _index = index;
            _wholeArea = wholeArea;
        }

    }

    public class GeneratedMeshClass
    {
        public Rh.Mesh TopBtnList { get; }
        public DMesh3 SideList { get;}
        public DMesh3 WholeMesh { get;}

        public double[] SideAreaList { get;}
        public Rh.Point3d[] CenPtList { get;}

        public DMeshAABBTree3 SpatialTree { get; }

        public GeneratedMeshClass(Rh.Mesh TopBtnList, DMesh3 SideList, double[] SideAreaList, Rh.Point3d[] CenPtList)
        {
            this.TopBtnList = TopBtnList;
            this.SideList = SideList;
            this.SideAreaList = SideAreaList;
            this.CenPtList = CenPtList;

            DMeshAABBTree3 tree = new DMeshAABBTree3(this.SideList);
            tree.Build();
            this.SpatialTree = tree;
        }

        public GeneratedMeshClass(DMesh3 WholeMesh)
        {
            this.WholeMesh = WholeMesh;
        }

        public GeneratedMeshClass(DMesh3 WholeMesh, double[] AreaList, Rh.Point3d[] CenPtList)
        {
            this.WholeMesh = WholeMesh;
            this.SideList = WholeMesh;
            this.SideAreaList = AreaList;
            this.CenPtList = CenPtList;
        }

        public GeneratedMeshClass() { }
    }

    public class Debug_GeneratedMeshClass
    {
        public Rh.Mesh TopBtnList { get; }
        public Rh.Mesh SideList { get; }
        public Rh.Mesh WholeMesh { get; }

        public double[] SideAreaList { get; }
        public Rh.Point3d[] CenPtList { get; }

        public Debug_GeneratedMeshClass(Rh.Mesh TopBtnList, Rh.Mesh SideList, double[] SideAreaList, Rh.Point3d[] CenPtList)
        {
            SideList.RebuildNormals();
            this.TopBtnList = TopBtnList;
            this.SideList = SideList;
            this.SideAreaList = SideAreaList;
            this.CenPtList = CenPtList;
        }

        public Debug_GeneratedMeshClass(Rh.Mesh WholeMesh)
        {
            this.WholeMesh = WholeMesh;
        }

        public Debug_GeneratedMeshClass(Rh.Mesh WholeMesh, double[] AreaList, Rh.Point3d[] CenPtList)
        {
            //this.WholeMesh = WholeMesh;
            WholeMesh.RebuildNormals();
            this.SideList = WholeMesh;
            this.SideAreaList = AreaList;
            this.CenPtList = CenPtList;
        }

        public Debug_GeneratedMeshClass() { }
    }
}