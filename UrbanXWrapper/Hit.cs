using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MetaCityWrapper
{
    public struct Hit
    {
        public int meshId;
        public int primId;
        public float u, v;
        public float distance;
        //public static implicit operator bool(Hit hit) => hit.Mesh != null;
    }
}
