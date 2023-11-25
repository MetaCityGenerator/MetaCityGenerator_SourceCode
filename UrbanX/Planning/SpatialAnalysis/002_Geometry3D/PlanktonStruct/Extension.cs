using g3;

using System;
using System.Collections.Generic;
using System.Linq;

using NTS = NetTopologySuite;
using Rh = Rhino.Geometry;

namespace UrbanX.Planning.SpatialAnalysis.Extension
{
    public static class Extension
    {
        public static Vector3d Tog3Pt(this PlanktonVertex ptIn)
        {
            return new Vector3d(ptIn.X, ptIn.Y, ptIn.Z);
        }

        public static Vector3d ClosestPoint(this DCurve3 curve, Vector3d pt, out int crvIndex, out double crvIntr)
        {
            int segCount = curve.SegmentCount;
            Vector3d closestPt = new Vector3d();

            double minCrvDis = double.MaxValue;
            double minCrvIntr = -1;
            int minCrvIndex = -1;

            //Segment3d minSeg = new Segment3d();
            //double tempCrvLength = 0;

            List<double> allLength = new List<double>(segCount);

            for (int i = 0; i < segCount; i++)
            {
                var segment = curve.GetSegment(i);

                var tempPt = segment.ClosestPoint(pt, out double tempCrvIntr, out double distanceSquared3D);
                //allLength.Add(distanceSquared2D);

                if (distanceSquared3D <= minCrvDis)
                {
                    closestPt = tempPt;
                    minCrvDis = distanceSquared3D;
                    minCrvIntr = tempCrvIntr;
                    minCrvIndex = i;
                    //minSeg = segment;
                }
            }

            //for (int i = 0; i < minCrvIndex; i++)
            //    tempCrvLength += allLength[i];
            //crvIntr = (minCrvIntr * minSeg.Length+ tempCrvLength) / allLength.Sum();

            crvIndex = minCrvIndex;
            crvIntr = minCrvIntr;

            return closestPt;
        }

        private static Vector3d ClosestPoint(this Segment3d curve, Vector3d pt, out double crvIntr, out double distance)
        {
            Vector3d p1 = curve.P0;
            Vector3d p2 = curve.P1;

            double dx = p2.x - p1.x;
            double dy = p2.y - p1.y;
            double dz = p2.z - p1.z;

            Vector3d intrPt = new Vector3d();
            if ((dx == 0) && (dy == 0) && (dz == 0))
            {
                // It's a point not a line segment.
                intrPt = p1;
                crvIntr = 0;
                //distanceSquared2D = 0;
                distance = 0;
                return intrPt;
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.x - p1.x) * dx + (pt.y - p1.y) * dy + (pt.z - p1.z) * dz) /
              (dx * dx + dy * dy + dz * dz);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                intrPt = new Vector3d(p1.x, p1.y, p1.z);
                crvIntr = 0;
            }
            else if (t > 1)
            {
                intrPt = new Vector3d(p2.x, p2.y, p2.z);
                crvIntr = 1;
            }
            else
            {
                intrPt = new Vector3d(p1.x + t * dx, p1.y + t * dy, p1.z + t * dz);
                crvIntr = t;
            }

            //distanceSquared2D = new Vector2d(p1.x, p1.y).Distance(new Vector2d(p2.x, p2.y));
            distance = intrPt.Distance(pt);
            return intrPt;
        }

        public static bool SetVertex(this PlanktonVertexList vertices, int v, Vector3d pt)
        {
            return vertices.SetVertex(v, pt.x, pt.y, pt.z);
        }

        public static int Add(this PlanktonVertexList vertices, Vector3d vec)
        {
            return vertices.Add(vec.x, vec.y, vec.z);
        }

        public static Vector3d Tog3Pt(this PlanktonXYZ ptIn)
        {
            return new Vector3d(ptIn.X, ptIn.Y, ptIn.Z);
        }

        public static void ExportMeshAsObj(this DMesh3 mesh, string path, bool color = false)
        {
            WriteOptions writeOption = new WriteOptions()
            {
                bWriteBinary = false,
                bPerVertexNormals = false,
                bPerVertexColors = color,
                bWriteGroups = false,
                bPerVertexUVs = false,
                bCombineMeshes = false,
                bWriteMaterials = false,
                ProgressFunc = null,
                //MaterialFilePath = @"E:\114_temp\008_代码集\002_extras\smallCharpTool\Application\data\geometryTest\exportColor2.mtl",
                RealPrecisionDigits = 15       // double
                                               //RealPrecisionDigits = 7        // float
            };
            IOWriteResult result = StandardMeshWriter.WriteFile(path, new List<WriteMesh>() { new WriteMesh(mesh) }, writeOption);
        }

        /// <summary>
        /// arrange ptList into clockwise
        /// </summary>
        /// <param name="source"></param>
        /// <param name="vIndex"></param>
        /// <returns></returns>
        public static int[] ConnectedVertices(this DMesh3 source, int vIndex)
        {
            var vertexNeighbours = source.VtxVerticesItr(vIndex);
            //return edgesNeighbours.ToArray();

            var count = vertexNeighbours.Count();
            var centroid = source.GetVertex(vIndex);
            List<SortedEdgeClass> vecList = new List<SortedEdgeClass>(count);
            foreach (var item in vertexNeighbours)
            {
                var tempVertex = source.GetVertex(item);
                SortedEdgeClass temp = new SortedEdgeClass(item, tempVertex.x, tempVertex.y, tempVertex.z);
                vecList.Add(temp);
            }
            //var sortedVecList = vecList.OrderBy(x => Math.Atan2(x.y - centroid.y,x.x - centroid.x)).ToList();
            var sortedVecList = vecList.OrderBy(x => Math.Atan2(x.x - centroid.x, x.y - centroid.y)).ToList();

            int[] result = new int[count];
            for (int i = 0; i < sortedVecList.Count; i++)
                result[i] = sortedVecList[i].index;
            return result;
        }

        public static int[] ConnectedEdges(this DMesh3 source, int vIndex)
        {
            var debugEdge = source.Edges();
            var vertexNeighbours = source.VtxEdgesItr(vIndex);
            //return edgesNeighbours.ToArray();
            var debug_bool = new List<bool>();
            foreach (var item in vertexNeighbours)
            {
                debug_bool.Add(source.IsBoundaryEdge(item));
            }

            var count = vertexNeighbours.Count();
            var centroid = source.GetVertex(vIndex);
            List<SortedEdgeClass> vecList = new List<SortedEdgeClass>(count);
            foreach (var item in vertexNeighbours)
            {
                var tempVertex = source.GetVertex(item);
                SortedEdgeClass temp = new SortedEdgeClass(item, tempVertex.x, tempVertex.y, tempVertex.z);
                vecList.Add(temp);
            }
            //var sortedVecList = vecList.OrderBy(x => Math.Atan2(x.y - centroid.y,x.x - centroid.x)).ToList();
            var sortedVecList = vecList.OrderBy(x => Math.Atan2(x.x - centroid.x, x.y - centroid.y)).ToList();

            int[] result = new int[count];
            for (int i = 0; i < sortedVecList.Count; i++)
                result[i] = sortedVecList[i].index;
            return result;
        }

        public static int[] ConnectedVertices_1(this DMesh3 source, int vIndex)
        {
            var edgesNeighbours = source.VtxVerticesItr(vIndex);
            return edgesNeighbours.ToArray();
        }

        public static int[] ConnectedVertices_2(this DMesh3 source, int vIndex)
        {
            var edgesNeighbours = source.VtxEdgesItr(vIndex);
            return edgesNeighbours.ToArray();
        }

        public static int FindSortedEdgeIndex(this List<Index4i> edgeList, int vA, int vB)
        {
            int result = -1;
            for (int i = 0; i < edgeList.Count; i++)
            {
                var tempEdge = edgeList[i];
                if (tempEdge.a == vA)
                {
                    if (tempEdge.b == vB)
                    {
                        result = i;
                    }
                }
                else if (tempEdge.b == vA)
                {
                    if (tempEdge.a == vB)
                    {
                        result = i;
                    }
                }

            }
            return result;
        }

        public static PlanktonMesh g3Mesh2pMesh(this DMesh3 source)
        {
            PlanktonMesh pMesh = new PlanktonMesh();

            var rhMesh = MeshCreation.ConvertFromDMesh3NoColor(source);
            var rhTopVertices = MeshCreation.ConvertFromDMesh3NoColor(source).TopologyVertices; ;

            rhMesh.Vertices.CombineIdentical(true, true);
            rhMesh.Vertices.CullUnused();
            //rhMesh.UnifyNormals();
            //rhMesh.Weld(Math.PI);

            var vertices = source.Vertices();
            foreach (Vector3d v in vertices)
            {
                pMesh.Vertices.Add(v);
            }

            for (int i = 0; i < source.TriangleCount; i++)
            {
                pMesh.Faces.Add(new PlanktonFace());
            }

            var sortedEdge = source.Edges().ToList();
            sortedEdge.Sort(new EdgesComparer());

            for (int i = 0; i < sortedEdge.Count; i++)
            {
                PlanktonHalfedge HalfA = new PlanktonHalfedge();
                var edgeFromSource = sortedEdge[i];

                //To Do 检查合理性
                HalfA.StartVertex = edgeFromSource.a;

                if (pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge == -1)
                    pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count;

                PlanktonHalfedge HalfB = new PlanktonHalfedge();

                HalfB.StartVertex = edgeFromSource.b;
                if (pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge == -1)
                    pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count + 1;

                List<int> tempConnectedFaces = new List<int>() { edgeFromSource.c, edgeFromSource.d };
                if (tempConnectedFaces.Contains(-1))
                    tempConnectedFaces.Remove(-1);
                int[] ConnectedFaces = tempConnectedFaces.ToArray();
                bool[] Match = new bool[] { source.tri_has_neighbour_t(i, edgeFromSource.c), source.tri_has_neighbour_t(i, edgeFromSource.d) };

                Match[0] = false;
                if (Match.Length > 1) Match[1] = true;

                int VertA = source.GetTriangle(ConnectedFaces[0]).a;
                int VertB = source.GetTriangle(ConnectedFaces[0]).b;
                int VertC = source.GetTriangle(ConnectedFaces[0]).c;
                int VertD = source.GetTriangle(ConnectedFaces[0]).c;

                if ((VertA == HalfA.StartVertex) && (VertB == HalfB.StartVertex)) { Match[0] = true; }
                if ((VertB == HalfA.StartVertex) && (VertC == HalfB.StartVertex)) { Match[0] = true; }
                if ((VertC == HalfA.StartVertex) && (VertD == HalfB.StartVertex)) { Match[0] = true; }
                if ((VertD == HalfA.StartVertex) && (VertA == HalfB.StartVertex)) { Match[0] = true; }
                if ((VertC == HalfA.StartVertex) && (VertA == HalfB.StartVertex)) { Match[0] = true; }
                if ((VertB == HalfA.StartVertex) && (VertD == HalfB.StartVertex)) { Match[0] = true; }

                if (Match[0] == true)
                {
                    HalfA.AdjacentFace = ConnectedFaces[0];
                    if (pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge == -1)
                    {
                        pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count;
                    }
                    if (ConnectedFaces.Length > 1)
                    {
                        HalfB.AdjacentFace = ConnectedFaces[1];
                        if (pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge == -1)
                        {
                            pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count + 1;
                        }
                    }
                    else
                    {
                        HalfB.AdjacentFace = -1;
                        pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count + 1;
                    }
                }
                else
                {
                    HalfB.AdjacentFace = ConnectedFaces[0];

                    if (pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge == -1)
                    {
                        pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count + 1;
                    }

                    if (ConnectedFaces.Length > 1)
                    {
                        HalfA.AdjacentFace = ConnectedFaces[1];

                        if (pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge == -1)
                        {
                            pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count;
                        }
                    }
                    else
                    {
                        HalfA.AdjacentFace = -1;
                        pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count;
                    }
                }
                pMesh.Halfedges.Add(HalfA);
                pMesh.Halfedges.Add(HalfB);
            }

            #region 检查
            //Dictionary<int, List<int>> debugDic = new Dictionary<int, List<int>>();
            //for (int i = 0; i < pMesh.Halfedges.Count; i++)
            //{
            //    var temp = source.ConnectedVertices(pMesh.Halfedges[i].StartVertex);
            //    if (debugDic.ContainsKey(pMesh.Halfedges[i].StartVertex))
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        debugDic.Add(pMesh.Halfedges[i].StartVertex,temp.ToList()) ;
            //    }
            //}

            //Dictionary<int, List<int>> debugDic2 = new Dictionary<int, List<int>>();
            //for (int i = 0; i < pMesh.Halfedges.Count; i++)
            //{
            //    var temp = source.ConnectedVertices_1(pMesh.Halfedges[i].StartVertex);
            //    if (debugDic2.ContainsKey(pMesh.Halfedges[i].StartVertex))
            //    {
            //        continue;
            //    }
            //    else
            //    {
            //        debugDic2.Add(pMesh.Halfedges[i].StartVertex, temp.ToList());
            //    }
            //}
            #endregion

            for (int i = 0; i < pMesh.Halfedges.Count; i += 2)
            {
                int[] EndNeighbours = rhTopVertices.ConnectedTopologyVertices(pMesh.Halfedges[i + 1].StartVertex, true);
                //int[] EndNeighbours = source.ConnectedVertices(pMesh.Halfedges[i + 1].StartVertex);

                //int[] EndNeighbours = new int[] { 0,3,2,6,5 };
                for (int j = 0; j < EndNeighbours.Length; j++)
                {
                    if (EndNeighbours[j] == pMesh.Halfedges[i].StartVertex)
                    {
                        int EndOfNextHalfedge = EndNeighbours[(j - 1 + EndNeighbours.Length) % EndNeighbours.Length];
                        int StartOfPrevOfPairHalfedge = EndNeighbours[(j + 1) % EndNeighbours.Length];

                        int NextEdge = sortedEdge.FindSortedEdgeIndex(pMesh.Halfedges[i + 1].StartVertex, EndOfNextHalfedge);
                        int PrevPairEdge = sortedEdge.FindSortedEdgeIndex(pMesh.Halfedges[i + 1].StartVertex, StartOfPrevOfPairHalfedge);

                        if (sortedEdge[NextEdge].a == pMesh.Halfedges[i + 1].StartVertex)
                        {
                            pMesh.Halfedges[i].NextHalfedge = NextEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i].NextHalfedge = NextEdge * 2 + 1;
                        }

                        if (sortedEdge[PrevPairEdge].b == pMesh.Halfedges[i + 1].StartVertex)
                        {
                            pMesh.Halfedges[i + 1].PrevHalfedge = PrevPairEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i + 1].PrevHalfedge = PrevPairEdge * 2 + 1;
                        }
                        break;
                    }
                }

                int[] StartNeighbours = rhTopVertices.ConnectedTopologyVertices(pMesh.Halfedges[i].StartVertex, true);
                //int[] StartNeighbours = source.ConnectedVertices(pMesh.Halfedges[i].StartVertex);
                //int[] StartNeighbours = new int[] { 5,4,3,1};
                for (int j = 0; j < StartNeighbours.Length; j++)
                {
                    if (StartNeighbours[j] == pMesh.Halfedges[i + 1].StartVertex)
                    {
                        int EndOfNextOfPairHalfedge = StartNeighbours[(j - 1 + StartNeighbours.Length) % StartNeighbours.Length];
                        int StartOfPrevHalfedge = StartNeighbours[(j + 1) % StartNeighbours.Length];

                        int NextPairEdge = sortedEdge.FindSortedEdgeIndex(pMesh.Halfedges[i].StartVertex, EndOfNextOfPairHalfedge);
                        int PrevEdge = sortedEdge.FindSortedEdgeIndex(pMesh.Halfedges[i].StartVertex, StartOfPrevHalfedge);

                        if (sortedEdge[NextPairEdge].a == pMesh.Halfedges[i].StartVertex)
                        {
                            pMesh.Halfedges[i + 1].NextHalfedge = NextPairEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i + 1].NextHalfedge = NextPairEdge * 2 + 1;
                        }


                        if (sortedEdge[PrevEdge].b == pMesh.Halfedges[i].StartVertex)
                        {
                            pMesh.Halfedges[i].PrevHalfedge = PrevEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i].PrevHalfedge = PrevEdge * 2 + 1;
                        }
                        break;
                    }
                }
            }
            return pMesh;

        }
        public static PlanktonMesh ToPlanktonMesh(this DMesh3 meshInput, DateTime start)
        {
            PlanktonMesh pMesh = new PlanktonMesh();

            Rh.Mesh source = MeshCreation.ConvertFromDMesh3NoColor(meshInput);

            //source.Vertices.CombineIdentical(true, true);
            //source.Vertices.CullUnused();
            //source.UnifyNormals();
            //source.Weld(Math.PI);

            foreach (Rh.Point3f v in source.TopologyVertices)
            {
                pMesh.Vertices.Add(v.X, v.Y, v.Z);
            }

            for (int i = 0; i < source.Faces.Count; i++)
            {
                pMesh.Faces.Add(new PlanktonFace());
            }

            for (int i = 0; i < source.TopologyEdges.Count; i++)
            {
                PlanktonHalfedge HalfA = new PlanktonHalfedge();

                HalfA.StartVertex = source.TopologyEdges.GetTopologyVertices(i).I;

                if (pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge == -1)
                {
                    pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count;
                }

                PlanktonHalfedge HalfB = new PlanktonHalfedge();

                HalfB.StartVertex = source.TopologyEdges.GetTopologyVertices(i).J;

                if (pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge == -1)
                {
                    pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count + 1;
                }

                bool[] Match;
                int[] ConnectedFaces = source.TopologyEdges.GetConnectedFaces(i, out Match);

                //Note for Steve Baer : This Match bool doesn't seem to work on triangulated meshes - it often returns true
                //for both faces, even for a properly oriented manifold mesh, which can't be right
                //So - making our own check for matching:
                //(I suspect the problem is related to C being the same as D for triangles, so best to
                //deal with them separately just to make sure)
                //loop through the vertices of the face until finding the one which is the same as the start of the edge
                //iff the next vertex around the face is the end of the edge then it matches.

                Match[0] = false;
                if (Match.Length > 1)
                { Match[1] = true; }

                int VertA = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].A);
                int VertB = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].B);
                int VertC = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].C);
                int VertD = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].D);

                if ((VertA == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertB == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertB == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertC == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertC == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertD == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertD == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertA == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                //I don't think these next 2 should ever be needed, but just in case:
                if ((VertC == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertA == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertB == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertD == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }

                if (Match[0] == true)
                {
                    HalfA.AdjacentFace = ConnectedFaces[0];
                    if (pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge == -1)
                    {
                        pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count;
                    }
                    if (ConnectedFaces.Length > 1)
                    {
                        HalfB.AdjacentFace = ConnectedFaces[1];
                        if (pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge == -1)
                        {
                            pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count + 1;
                        }
                    }
                    else
                    {
                        HalfB.AdjacentFace = -1;
                        pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count + 1;
                    }
                }
                else
                {
                    HalfB.AdjacentFace = ConnectedFaces[0];

                    if (pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge == -1)
                    {
                        pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count + 1;
                    }

                    if (ConnectedFaces.Length > 1)
                    {
                        HalfA.AdjacentFace = ConnectedFaces[1];

                        if (pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge == -1)
                        {
                            pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count;
                        }
                    }
                    else
                    {
                        HalfA.AdjacentFace = -1;
                        pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count;
                    }
                }
                pMesh.Halfedges.Add(HalfA);
                pMesh.Halfedges.Add(HalfB);
            }

            for (int i = 0; i < (pMesh.Halfedges.Count); i += 2)
            {
                int[] EndNeighbours = source.TopologyVertices.ConnectedTopologyVertices(pMesh.Halfedges[i + 1].StartVertex, true);
                for (int j = 0; j < EndNeighbours.Length; j++)
                {
                    if (EndNeighbours[j] == pMesh.Halfedges[i].StartVertex)
                    {
                        int EndOfNextHalfedge = EndNeighbours[(j - 1 + EndNeighbours.Length) % EndNeighbours.Length];
                        int StartOfPrevOfPairHalfedge = EndNeighbours[(j + 1) % EndNeighbours.Length];

                        int NextEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i + 1].StartVertex, EndOfNextHalfedge);
                        int PrevPairEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i + 1].StartVertex, StartOfPrevOfPairHalfedge);

                        if (source.TopologyEdges.GetTopologyVertices(NextEdge).I == pMesh.Halfedges[i + 1].StartVertex)
                        {
                            pMesh.Halfedges[i].NextHalfedge = NextEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i].NextHalfedge = NextEdge * 2 + 1;
                        }

                        if (source.TopologyEdges.GetTopologyVertices(PrevPairEdge).J == pMesh.Halfedges[i + 1].StartVertex)
                        {
                            pMesh.Halfedges[i + 1].PrevHalfedge = PrevPairEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i + 1].PrevHalfedge = PrevPairEdge * 2 + 1;
                        }
                        break;
                    }
                }

                int[] StartNeighbours = source.TopologyVertices.ConnectedTopologyVertices(pMesh.Halfedges[i].StartVertex, true);
                for (int j = 0; j < StartNeighbours.Length; j++)
                {
                    if (StartNeighbours[j] == pMesh.Halfedges[i + 1].StartVertex)
                    {
                        int EndOfNextOfPairHalfedge = StartNeighbours[(j - 1 + StartNeighbours.Length) % StartNeighbours.Length];
                        int StartOfPrevHalfedge = StartNeighbours[(j + 1) % StartNeighbours.Length];

                        int NextPairEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i].StartVertex, EndOfNextOfPairHalfedge);
                        int PrevEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i].StartVertex, StartOfPrevHalfedge);

                        if (source.TopologyEdges.GetTopologyVertices(NextPairEdge).I == pMesh.Halfedges[i].StartVertex)
                        {
                            pMesh.Halfedges[i + 1].NextHalfedge = NextPairEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i + 1].NextHalfedge = NextPairEdge * 2 + 1;
                        }

                        if (source.TopologyEdges.GetTopologyVertices(PrevEdge).J == pMesh.Halfedges[i].StartVertex)
                        {
                            pMesh.Halfedges[i].PrevHalfedge = PrevEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i].PrevHalfedge = PrevEdge * 2 + 1;
                        }
                        break;
                    }
                }
            }

            return pMesh;
        }
        public static PlanktonMesh ToPlanktonMesh(this Rh.Mesh source, DateTime start)
        {
            PlanktonMesh pMesh = new PlanktonMesh();

            source.Vertices.CombineIdentical(true, true);
            source.Vertices.CullUnused();
            //source.UnifyNormals();
            //source.Weld(Math.PI);

            foreach (Rh.Point3f v in source.TopologyVertices)
            {
                pMesh.Vertices.Add(v.X, v.Y, v.Z);
            }

            for (int i = 0; i < source.Faces.Count; i++)
            {
                pMesh.Faces.Add(new PlanktonFace());
            }

            for (int i = 0; i < source.TopologyEdges.Count; i++)
            {
                PlanktonHalfedge HalfA = new PlanktonHalfedge();

                HalfA.StartVertex = source.TopologyEdges.GetTopologyVertices(i).I;

                if (pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge == -1)
                {
                    pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count;
                }

                PlanktonHalfedge HalfB = new PlanktonHalfedge();

                HalfB.StartVertex = source.TopologyEdges.GetTopologyVertices(i).J;

                if (pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge == -1)
                {
                    pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count + 1;
                }

                bool[] Match;
                int[] ConnectedFaces = source.TopologyEdges.GetConnectedFaces(i, out Match);

                //Note for Steve Baer : This Match bool doesn't seem to work on triangulated meshes - it often returns true
                //for both faces, even for a properly oriented manifold mesh, which can't be right
                //So - making our own check for matching:
                //(I suspect the problem is related to C being the same as D for triangles, so best to
                //deal with them separately just to make sure)
                //loop through the vertices of the face until finding the one which is the same as the start of the edge
                //iff the next vertex around the face is the end of the edge then it matches.

                Match[0] = false;
                if (Match.Length > 1)
                { Match[1] = true; }

                int VertA = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].A);
                int VertB = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].B);
                int VertC = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].C);
                int VertD = source.TopologyVertices.TopologyVertexIndex(source.Faces[ConnectedFaces[0]].D);

                if ((VertA == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertB == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertB == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertC == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertC == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertD == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertD == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertA == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                //I don't think these next 2 should ever be needed, but just in case:
                if ((VertC == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertA == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }
                if ((VertB == source.TopologyEdges.GetTopologyVertices(i).I)
                    && (VertD == source.TopologyEdges.GetTopologyVertices(i).J))
                {
                    Match[0] = true;
                }

                if (Match[0] == true)
                {
                    HalfA.AdjacentFace = ConnectedFaces[0];
                    if (pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge == -1)
                    {
                        pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count;
                    }
                    if (ConnectedFaces.Length > 1)
                    {
                        HalfB.AdjacentFace = ConnectedFaces[1];
                        if (pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge == -1)
                        {
                            pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count + 1;
                        }
                    }
                    else
                    {
                        HalfB.AdjacentFace = -1;
                        pMesh.Vertices[HalfB.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count + 1;
                    }
                }
                else
                {
                    HalfB.AdjacentFace = ConnectedFaces[0];

                    if (pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge == -1)
                    {
                        pMesh.Faces[HalfB.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count + 1;
                    }

                    if (ConnectedFaces.Length > 1)
                    {
                        HalfA.AdjacentFace = ConnectedFaces[1];

                        if (pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge == -1)
                        {
                            pMesh.Faces[HalfA.AdjacentFace].FirstHalfedge = pMesh.Halfedges.Count;
                        }
                    }
                    else
                    {
                        HalfA.AdjacentFace = -1;
                        pMesh.Vertices[HalfA.StartVertex].OutgoingHalfedge = pMesh.Halfedges.Count;
                    }
                }
                pMesh.Halfedges.Add(HalfA);
                pMesh.Halfedges.Add(HalfB);
            }

            for (int i = 0; i < (pMesh.Halfedges.Count); i += 2)
            {
                int[] EndNeighbours = source.TopologyVertices.ConnectedTopologyVertices(pMesh.Halfedges[i + 1].StartVertex, true);
                for (int j = 0; j < EndNeighbours.Length; j++)
                {
                    if (EndNeighbours[j] == pMesh.Halfedges[i].StartVertex)
                    {
                        int EndOfNextHalfedge = EndNeighbours[(j - 1 + EndNeighbours.Length) % EndNeighbours.Length];
                        int StartOfPrevOfPairHalfedge = EndNeighbours[(j + 1) % EndNeighbours.Length];

                        int NextEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i + 1].StartVertex, EndOfNextHalfedge);
                        int PrevPairEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i + 1].StartVertex, StartOfPrevOfPairHalfedge);

                        if (source.TopologyEdges.GetTopologyVertices(NextEdge).I == pMesh.Halfedges[i + 1].StartVertex)
                        {
                            pMesh.Halfedges[i].NextHalfedge = NextEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i].NextHalfedge = NextEdge * 2 + 1;
                        }

                        if (source.TopologyEdges.GetTopologyVertices(PrevPairEdge).J == pMesh.Halfedges[i + 1].StartVertex)
                        {
                            pMesh.Halfedges[i + 1].PrevHalfedge = PrevPairEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i + 1].PrevHalfedge = PrevPairEdge * 2 + 1;
                        }
                        break;
                    }
                }

                int[] StartNeighbours = source.TopologyVertices.ConnectedTopologyVertices(pMesh.Halfedges[i].StartVertex, true);
                for (int j = 0; j < StartNeighbours.Length; j++)
                {
                    if (StartNeighbours[j] == pMesh.Halfedges[i + 1].StartVertex)
                    {
                        int EndOfNextOfPairHalfedge = StartNeighbours[(j - 1 + StartNeighbours.Length) % StartNeighbours.Length];
                        int StartOfPrevHalfedge = StartNeighbours[(j + 1) % StartNeighbours.Length];

                        int NextPairEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i].StartVertex, EndOfNextOfPairHalfedge);
                        int PrevEdge = source.TopologyEdges.GetEdgeIndex(pMesh.Halfedges[i].StartVertex, StartOfPrevHalfedge);

                        if (source.TopologyEdges.GetTopologyVertices(NextPairEdge).I == pMesh.Halfedges[i].StartVertex)
                        {
                            pMesh.Halfedges[i + 1].NextHalfedge = NextPairEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i + 1].NextHalfedge = NextPairEdge * 2 + 1;
                        }

                        if (source.TopologyEdges.GetTopologyVertices(PrevEdge).J == pMesh.Halfedges[i].StartVertex)
                        {
                            pMesh.Halfedges[i].PrevHalfedge = PrevEdge * 2;
                        }
                        else
                        {
                            pMesh.Halfedges[i].PrevHalfedge = PrevEdge * 2 + 1;
                        }
                        break;
                    }
                }
            }

            return pMesh;
        }
        public static DMesh3 pMesh2g3Mesh(this PlanktonMesh meshIn)
        {
            DMesh3 meshOut = new DMesh3();
            foreach (PlanktonVertex vertex in meshIn.Vertices)
            {
                meshOut.AppendVertex(vertex.Tog3Pt());
            }
            for (int i = 0; i < meshIn.Faces.Count; i++)
            {
                //var debug=meshIn.Faces.GetFaceCenter(i);
                int[] fvs = meshIn.Faces.GetFaceVertices(i);
                if (fvs.Length == 3)
                {
                    meshOut.AppendTriangle(fvs[0], fvs[1], fvs[2]);
                }
                #region 大于三面，目前不可能出现
                else if (fvs.Length == 4)
                {
                    meshOut.AppendTriangle(fvs[0], fvs[1], fvs[2], fvs[3]);
                }
                else if (fvs.Length > 4)
                {
                    PlanktonXYZ faceCenter = meshIn.Faces.GetFaceCenter(i);
                    meshOut.AppendVertex(new Vector3d(faceCenter.X, faceCenter.Y, faceCenter.Z));
                    for (int j = 0; j < fvs.Length; j++)
                    {
                        meshOut.AppendTriangle(fvs[j], fvs[(j + 1) % fvs.Length], meshOut.VertexCount - 1);
                    }
                }
                #endregion
            }
            return new DMesh3(meshOut, true);
        }

        internal class EdgesComparer : IComparer<Index4i>
        {
            public int Compare(Index4i x, Index4i y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;
                {
                    if (x.a > y.a) return 1;
                    else if (x.a < y.a) return -1;
                    if (x.b > y.b) return 1;
                    if (x.b < y.b) return -1;
                }
                return 0;
            }
        }

        internal struct SortedEdgeClass
        {
            public int index { get; set; }
            public double x { get; set; }
            public double y { get; set; }
            public double z { get; set; }

            public SortedEdgeClass(int Index, double X, double Y, double Z)
            {
                index = Index;
                x = X;
                y = Y;
                z = Z;
            }
        }

        public static NTS.Geometries.Point toNTSPt(NTS.Geometries.Coordinate data)
        {
            return new NTS.Geometries.Point(data);
        }

        public static NTS.Geometries.Point toNTSPt(this g3.Vector3d data)
        {
            return new NTS.Geometries.Point(data.x, data.y, data.z);
        }

        public static Vector3d tog3Pt(this NTS.Geometries.Point pt)
        {
            return new Vector3d(pt.X, pt.Y, pt.Z);
        }

        public static Rh.Point3d toRhPt(this NTS.Geometries.Point pt)
        {
            return new Rh.Point3d(pt.X, pt.Y, pt.Z);
        }
    }
}
