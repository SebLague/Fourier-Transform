#include <UnityShaderVariables.cginc>

// ---- Version 0.6 [05/Nov/2025] ----

// ---- Sections ----
// #ConstantsAndStructures
// #Misc
// #IntersectionFunctions
// #SignedDistanceFunctions
// #ShapeContainsFunctions
// #Random
// #Colour
// #Rotation
// #Easing

// ---- #ConstantsAndStructures ----
static const float PI = 3.1415926;
static const float TAU = PI * 2;

struct HitInfo
{
    bool didHit;
    bool isInside;
    float dst;
    float3 hitPoint;
    float3 normal;
};

// ---- #Misc ----

uint CoordToFlatIndex(uint2 coord, uint resolutionX)
{
    return coord.y * resolutionX + coord.x;
}

uint2 FlatIndexToCoord(uint flatIndex, uint resolutionX)
{
    uint y = flatIndex / resolutionX;
    uint x = flatIndex % resolutionX;
    return uint2(x, y);
}

// Convert 2D coordinate (on xz plane) to a 3D point with given height (y value)
float3 CoordinateToWorldPos(uint2 coord, uint2 resolution, float2 worldSize, float height)
{
    float u = resolution.x <= 1 ? 0.5f : coord.x / (resolution.x - 1.0);
    float v = resolution.y <= 1 ? 0.5f : coord.y / (resolution.y - 1.0);
    float2 uv = float2(u, v);
    float2 worldPos2D = (uv - 0.5) * worldSize;
    return float3(worldPos2D.x, height, worldPos2D.y);
}

// Convert world point to 2D coordinate on xz plane (world.y is ignored)
int2 WorldPosToCoordinate(float3 worldPos, uint2 resolution, float2 worldSize)
{
    float2 worldPos2D = float2(worldPos.x, worldPos.z);
    float2 coord = (worldPos2D / worldSize + 0.5f) * (resolution - 1.0);
    return int2((int)(coord.x + 0.5f), (int)(coord.y + 0.5));
}


// Get point on the line segment (a1, a2) that's closest to the given point (p)
float3 ClosestPointOnLineSegment(float3 p, float3 a1, float3 a2)
{
    float3 lineDelta = a2 - a1;
    float3 pointDelta = p - a1;
    float sqrLineLength = dot(lineDelta, lineDelta);

    if (sqrLineLength == 0)
        return a1;

    float t = saturate(dot(pointDelta, lineDelta) / sqrLineLength);
    return a1 + lineDelta * t;
}

// Calculates smallest distance from given point to the line segment (a1, a2)
float DistanceToLineSegment(float3 p, float3 a1, float3 a2)
{
    float3 closestPoint = ClosestPointOnLineSegment(p, a1, a2);
    return length(p - closestPoint);
}

float3 WorldViewDir(float2 uv)
{
    float3 viewVector = mul(unity_CameraInvProjection, float4(uv * 2 - 1, 0, -1));
    return normalize(mul(unity_CameraToWorld, viewVector));
}

// ---- #IntersectionFunctions ----

// Test intersection of ray with unit box centered at origin
HitInfo RayUnitBox(float3 pos, float3 dir)
{
    const float3 boxMin = -1;
    const float3 boxMax = 1;
    float3 invDir = 1 / dir;

    // Thanks to https://tavianator.com/2011/ray_box.html
    float3 tMin = (boxMin - pos) * invDir;
    float3 tMax = (boxMax - pos) * invDir;
    float3 t1 = min(tMin, tMax);
    float3 t2 = max(tMin, tMax);
    float tNear = max(max(t1.x, t1.y), t1.z);
    float tFar = min(min(t2.x, t2.y), t2.z);

    // Set hit info
    HitInfo hitInfo = (HitInfo)0;
    hitInfo.dst = 1.#INF;
    hitInfo.didHit = tFar >= tNear && tFar > 0;
    hitInfo.isInside = tFar > tNear && tNear <= 0;

    if (hitInfo.didHit)
    {
        float hitDst = hitInfo.isInside ? tFar : tNear;
        float3 hitPos = pos + dir * hitDst;

        hitInfo.dst = hitDst;
        hitInfo.hitPoint = hitPos;

        // Calculate normal
        float3 o = (1 - abs(hitPos));
        float3 absNormal = (o.x < o.y && o.x < o.z) ? float3(1, 0, 0) : (o.y < o.z) ? float3(0, 1, 0) : float3(0, 0, 1);
        hitInfo.normal = absNormal * sign(hitPos) * (hitInfo.isInside ? -1 : 1);
    }

    return hitInfo;
}

// Calculate the intersection of a ray with a unit sphere centered at the origin
HitInfo RayUnitSphere(float3 rayPos, float3 rayDir)
{
    const float3 sphereCentre = 0;
    const float sphereRadius = 1;

    HitInfo hitInfo = (HitInfo)0;
    hitInfo.dst = 1.#INF;

    float3 offsetRayOrigin = rayPos - sphereCentre;
    // From the equation: sqrLength(rayOrigin + rayDir * dst) = radius^2
    // Solving for dst results in a quadratic equation with coefficients:
    float a = dot(rayDir, rayDir); // a = 1 (assuming unit vector)
    float b = 2 * dot(offsetRayOrigin, rayDir);
    float c = dot(offsetRayOrigin, offsetRayOrigin) - sphereRadius * sphereRadius;
    // Quadratic discriminant
    float discriminant = b * b - 4 * a * c;

    // No solution when d < 0 (ray misses sphere)
    if (discriminant >= 0)
    {
        float s = sqrt(discriminant);
        // Distance to nearest intersection point (from quadratic formula)
        float dstNear = max(0, (-b - s) / (2 * a));
        float dstFar = (-b + s) / (2 * a);

        // Ignore intersections that occur behind the ray
        if (dstFar >= 0)
        {
            hitInfo.didHit = true;
            hitInfo.isInside = dstNear == 0;
            hitInfo.dst = hitInfo.isInside ? dstFar : dstNear;
            hitInfo.hitPoint = rayPos + rayDir * hitInfo.dst;
            hitInfo.normal = normalize(hitInfo.hitPoint - sphereCentre) * (hitInfo.isInside ? -1 : 1);
        }
    }

    return hitInfo;
}

void TransformRayToLocalSpace(inout float3 pos, inout float3 dir, float4x4 worldToLocalMatrix)
{
    pos = mul(worldToLocalMatrix, float4(pos, 1));
    dir = mul(worldToLocalMatrix, float4(dir, 0));
}

// #SignedDistanceFunctions ----

float SphereDistance(float3 p, float3 centre, float radius)
{
    return length(p - centre) - radius;
}

float BoxDistance(float3 p, float3 centre, float3 size)
{
    float3 d = abs(p - centre) - size;
    return max(d.x, max(d.y, d.z));
}

// Thanks to https://iquilezles.org/articles/distfunctions/
float RoundedBoxDistance(float3 p, float3 centre, float3 size, float r)
{
    p -= centre;
    float3 q = abs(p) - size + r;
    return length(max(q, 0.0)) + min(max(q.x, max(q.y, q.z)), 0.0) - r;
}

float TorusDistance(float3 p, float3 centre, float r1, float r2)
{
    float2 q = float2(length((p - centre).xz) - r1, p.y - centre.y);
    return length(q) - r2;
}

// #ShapeContainsFunctions ----

bool BoxContainsPoint(float3 boxCentre, float3 boxSize, float3 p)
{
    float3 o = abs(p - boxCentre) * 2;
    return (o.x <= boxSize.x && o.y <= boxSize.y && o.z <= boxSize.z);
}

// ---- #Random ----


uint RngStateFromFloat4(float4 input)
{
    const uint4 primes = uint4(19349669, 83492837, 73856131, 4785773);
    return dot(asuint(input), primes);
}

uint RngStateFromFloat3(float3 input)
{
    return RngStateFromFloat4(float4(input, 1));
}

uint RngStateFromFloat2(float2 input)
{
    return RngStateFromFloat4(float4(input, 1, 1));
}

// PCG (permuted congruential generator). Thanks to:
// www.pcg-random.org and www.shadertoy.com/view/XlGcRh
uint NextRandomUint(inout uint state)
{
    state = state * 747796405 + 2891336453;
    uint result = ((state >> ((state >> 28) + 4)) ^ state) * 277803737;
    result = (result >> 22) ^ result;
    return result;
}

float RandomUNorm(inout uint state)
{
    return NextRandomUint(state) / 4294967295.0; // 2^32 - 1
}

// Random value in normal distribution (with mean=0 and sd=1)
float RandomValueNormalDistribution(inout uint state)
{
    const float PI = 3.1415926;
    // Thanks to https://stackoverflow.com/a/6178290
    float theta = 2 * PI * RandomUNorm(state);
    float rho = sqrt(-2 * log(RandomUNorm(state)));
    return rho * cos(theta);
}

// Calculate a random direction
float3 RandomDirection(inout uint state)
{
    // Thanks to https://math.stackexchange.com/a/1585996
    float x = RandomValueNormalDistribution(state);
    float y = RandomValueNormalDistribution(state);
    float z = RandomValueNormalDistribution(state);
    return normalize(float3(x, y, z));
}

float2 RandomPointInCircle(inout uint rngState)
{
    const float PI = 3.1415926;
    float angle = RandomUNorm(rngState) * 2 * PI;
    float2 pointOnCircle = float2(cos(angle), sin(angle));
    return pointOnCircle * sqrt(RandomUNorm(rngState));
}

// ---- #Colour ----

float3 RGBToHSV(float3 rgb)
{
    // Thanks to http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = rgb.g < rgb.b ? float4(rgb.bg, K.wz) : float4(rgb.gb, K.xy);
    float4 q = rgb.r < p.x ? float4(p.xyw, rgb.r) : float4(rgb.r, p.yzx);

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float3 HSVToRGB(float3 hsv)
{
    // Thanks to http://lolengine.net/blog/2013/07/27/rgb-to-hsv-in-glsl
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(hsv.xxx + K.xyz) * 6.0 - K.www);
    return hsv.z * lerp(K.xxx, saturate(p - K.xxx), hsv.y);
}

float3 TweakCol(float3 colRGB, float hueShift, float satShift, float valShift)
{
    float3 hsv = RGBToHSV(colRGB);
    return HSVToRGB(hsv + float3(hueShift, satShift, valShift));
}

// ---- #Rotation ----
float3 RotateAroundAxis(float3 p, float3 axis, float sinAngle, float cosAngle)
{
    // Rodrigues' rotation formula: https://en.wikipedia.org/wiki/Rodrigues%27_rotation_formula
    return p * cosAngle + cross(axis, p) * sinAngle + axis * dot(axis, p) * (1 - cosAngle);
}

// Rotates given vector by the rotation that aligns startDir with endDir
float3 RotateBetweenDirections(float3 vec, float3 startDir, float3 endDir)
{
    float3 rotationAxis = cross(startDir, endDir);
    float sinAngle = length(rotationAxis);
    float cosAngle = dot(startDir, endDir);

    return RotateAroundAxis(vec, normalize(rotationAxis), sinAngle, cosAngle);
}

// ---- #Easing ----
float EaseQuadInOut(float t)
{
    t = saturate(t);
    return 3 * t * t - 2 * t * t * t;
}

float EaseCubeInOut(float t)
{
    t = saturate(t);
    int r = (int)round(t);
    float t3 = t * t * t;
    float oneMinusT3 = (1 - t) * (1 - t) * (1 - t);
    return 4 * t3 * (1 - r) + (1 - 4 * oneMinusT3) * r;
}
