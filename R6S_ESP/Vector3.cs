using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R6S_ESP
{
    class Vector3
    {
        public float x, y, z;

        public float Dot(Vector3 vec)
        {
            return x * vec.x + y * vec.y + z * vec.z;
        }

        public float Distance(Vector3 vec)
        {
            return (float)Math.Sqrt((x - vec.x) * (x - vec.x) + (y - vec.y) * (y - vec.y) + (z - vec.z) * (z - vec.z));
        }
    }
}
