using UnityEngine;

namespace AetherNexus.FoundationPlatform.Extensions
{
public static class ColliderExtensions
{
    public static Vector3 GetRandomPointInsideCollider(this BoxCollider boxCollider)
    {
        Vector3 extents = boxCollider.size / 2f;
        Vector3 point = new Vector3(
            UnityEngine.Random.Range(-extents.x, extents.x),
            UnityEngine.Random.Range(-extents.y, extents.y),
            UnityEngine.Random.Range(-extents.z, extents.z)
        ) + boxCollider.center;
        return boxCollider.transform.TransformPoint(point);
    }

    public static Vector2 GetRandomPointInsideCollider(this BoxCollider2D boxCollider)
    {
        Vector2 extents = boxCollider.size / 2f;
        Vector2 point = new Vector2(
            UnityEngine.Random.Range(-extents.x, extents.x),
            UnityEngine.Random.Range(-extents.y, extents.y)
        ) + boxCollider.offset;
        return boxCollider.transform.TransformPoint(point);
    }

    public static bool IsColliderInside(this Collider insideCollider, Collider outsideCollider)
    {
        Bounds insideBounds = insideCollider.bounds;
        Bounds outsideBounds = outsideCollider.bounds;

        // Check if the insideBounds is completely inside the outsideBounds
        if (outsideBounds.Contains(insideBounds.min) && outsideBounds.Contains(insideBounds.max))
        {
            return true;
        }

        return false;
    }
}}
