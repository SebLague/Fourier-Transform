using UnityEngine;

namespace Seb.Types
{
    public readonly struct Bounds2D
    {
        public readonly Vector2 Min;
        public readonly Vector2 Max;

        public readonly Vector2 Size => Max - Min;
        public readonly Vector2 Centre => (Min + Max) / 2;

        public readonly float Width => Size.x;
        public readonly float Height => Size.y;

        public readonly Vector2 BottomLeft => Min;
        public readonly Vector2 TopRight => Max;
        public readonly Vector2 BottomRight => new(Max.x, Min.y);
        public readonly Vector2 TopLeft => new(Min.x, Max.y);
        public readonly Vector2 CentreLeft => new(Min.x, (Min.y + Max.y) / 2);
        public readonly Vector2 CentreRight => new(Max.x, (Min.y + Max.y) / 2);
        public readonly Vector2 CentreTop => new((Min.x + Max.x) / 2, Max.y);
        public readonly Vector2 CentreBottom => new((Min.x + Max.x) / 2, Min.y);

        public readonly float Left => Min.x;
        public readonly float Right => Max.x;
        public readonly float Top => Max.y;
        public readonly float Bottom => Min.y;

        public Bounds2D(Vector2 min, Vector2 max)
        {
            Min = min;
            Max = max;
        }

        public static Bounds2D CreateFromCentreAndSize(Vector2 centre, Vector2 size)
        {
            return new Bounds2D(centre - size / 2, centre + size / 2);
        }

        public static Bounds2D CreateEmpty()
        {
            return new Bounds2D(Vector2.one * float.MaxValue, Vector2.one * float.MinValue);
        }

        public static Bounds2D Combine(Bounds2D a, Bounds2D b)
        {
            return new Bounds2D(Vector2.Min(a.Min, b.Min), Vector2.Max(a.Max, b.Max));
        }

        public bool PointInBounds(Vector2 p)
        {
            return p.x >= Min.x && p.x <= Max.x && p.y >= Min.y && p.y <= Max.y;
        }
    }
}