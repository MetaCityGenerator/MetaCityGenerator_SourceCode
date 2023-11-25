using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace UrbanXWrapper
{
    public class Raytracer : IDisposable
    {
        IntPtr scene;
        bool isReady =false;

        void Free()
        {
            if (scene != IntPtr.Zero)
            {
                UnsafeNativeMethods.DeleteScene(scene);
                scene = IntPtr.Zero;
            }
        }

        ~Raytracer() => Free();

        public void Dispose()
        {
            Free();
            GC.SuppressFinalize(this);
        }

        public Raytracer()
        {
            scene=UnsafeNativeMethods.InitScene();
        }

        public void AddMesh(Vector3[] vertices, int numVerts, int[] indices, int numIdx)
        {
            UnsafeNativeMethods.AddTriangleMesh(scene, vertices, numVerts, indices, numIdx);
        }

        public void CommitScene()
        {
            UnsafeNativeMethods.FinalizeScene(scene);
            isReady = true;
        }

        public void Trace(Ray ray, out Hit hit)
        {
            //Debug.Assert(isReady);
            UnsafeNativeMethods.TraceSingle(scene, in ray, out hit);
        }

        public static string TimeCalculation(DateTime beforDT, string topic)
        {
            DateTime afterDT = System.DateTime.Now;
            TimeSpan ts = afterDT.Subtract(beforDT);
            double spanTotalSeconds = double.Parse(ts.TotalMilliseconds.ToString()); //执行时间的总秒数
            return string.Format("{0}模块：计算用时  {1}ms", topic, Math.Round(spanTotalSeconds, 2));
        }
    }
}
