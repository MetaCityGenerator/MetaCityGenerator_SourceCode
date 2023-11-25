using g3;

using System.Collections.Generic;

namespace UrbanX.Application.Intersection
{
    public class Buildings
    {
        public DMesh3 buildingsMesh { get; set; }
        public Dictionary<NetTopologySuite.Geometries.Point, double> ptAreaDic { get; set; }

        //public Buildings(string JsonFilePath, string[] Attribute, int sub)
        //{
        //    buildingsMesh = CreateMesh(JsonFilePath, Attribute, sub);
        //}

        //public static DMesh3 ReadJson(string JsonFilePath, string[] Attribute, int sub)
        //{
        //    return CreateMesh(JsonFilePath, Attribute, sub);
        //}
        //To Do 更改sub，促使每一部分的楼房分割情况一致
        /// <summary>
        /// Create Mesh from boundary srf, extrude srf, remesh srf
        /// </summary>
        /// <param name="jsonFilePath">json path</param>
        /// <param name="attributes"> default value is  "baseHeight", "brepHeight"</param>
        /// <param name="sub"> subdivision value</param>
        /// <returns></returns>
        //private static DMesh3 CreateMesh(string jsonFilePath, string[] attributes, int sub)
        //{
        //    //读取mesh
        //    var inputDataCollection = MeshCreation.ReadJsonData(jsonFilePath, attributes[0], attributes[1], out double[] heightCollection, out double[] baseHeightCollection);

        //    //创建mesh simple，输出中心点与面积
        //    var simpleMeshes = MeshCreation.ExtrudeMeshListFromPtMinusTopBtn(inputDataCollection, heightCollection, baseHeightCollection, out Dictionary<NetTopologySuite.Geometries.Point, double> secPtDic, out DCurve3[][] edges);

        //    //细分
        //    DMesh3 meshCollection = new DMesh3();
        //    for (int i = 0; i < simpleMeshes.Length; i++)
        //    {
        //        var remeshedMesh = MeshCreation.ReMeshHardEdge(simpleMeshes[i], edges[i], sub);
        //        var exportedMesh = remeshedMesh.pMesh2g3Mesh();
        //        MeshEditor.Append(meshCollection, exportedMesh);
        //    }

        //    return meshCollection;
        //}
    }
}
