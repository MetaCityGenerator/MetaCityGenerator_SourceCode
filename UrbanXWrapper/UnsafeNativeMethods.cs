using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace UrbanXWrapper
{
    internal class UnsafeNativeMethods
    {
        private const string DLL_NAME = "UrbanXTools.Native.dll";
        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr InitScene();

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FinalizeScene(IntPtr scene);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int AddTriangleMesh(IntPtr scene, Vector3[] vertices, int numVerts, int[] indices, int numIdx);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void TraceSingle(IntPtr scene, in Ray ray, out Hit hit);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.I1)]
        public static extern bool IsOccluded(IntPtr scene, in Ray ray, float maxDistance);

        [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
        public static extern void DeleteScene(IntPtr scene);

    }
}
