using UnityEngine;

namespace Seb.Vis.Internal
{
    public readonly struct SphereData
    {
        public readonly Vector3 centre;
        public readonly float size;
        public readonly Color col;

        public SphereData(Vector3 centre, float size, Color col)
        {
            this.centre = centre;
            this.size = size;
            this.col = col;
        }
    }
}