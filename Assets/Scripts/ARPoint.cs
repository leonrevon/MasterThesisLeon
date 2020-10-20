using System.Linq;

namespace UnityEngine.XR.ARFoundation
{
    public class ARPoint
    {
        public float x;
        public float y;
        public float z;

        public ARPoint(Vector3 pos)
        {
            x = pos.x;
            y = pos.y;
            z = pos.z;
        }

    }

}
