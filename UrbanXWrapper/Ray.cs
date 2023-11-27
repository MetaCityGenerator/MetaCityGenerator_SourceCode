using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace MetaCityWrapper
{
    public struct Ray
    {
        public Vector3 Origin;
        public Vector3 Direction;
        public float MinDistance;
    }
}
