using UnityEngine;
using UnityEngine.Scripting;

namespace PaiSho.Presentation
{
    /// <summary>
    /// Keeps Physics collider types alive under WebGL + stripEngineCode.
    /// <see cref="GameObject.CreatePrimitive"/> needs CapsuleCollider for cylinders
    /// and SphereCollider/MeshCollider for spheres/planes; IL2CPP otherwise drops them
    /// when nothing AddComponent&lt;T&gt;s those types directly.
    /// </summary>
    [Preserve]
    public static class WebGlPhysicsPreserve
    {
        [Preserve]
        static WebGlPhysicsPreserve()
        {
            EnsureCollidersPreserved();
        }

        /// <summary>
        /// Call before <see cref="GameObject.CreatePrimitive"/> so IL2CPP keeps CapsuleCollider
        /// (Cylinder) / SphereCollider / BoxCollider under stripEngineCode.
        /// </summary>
        [Preserve]
        public static void EnsureCollidersPreserved()
        {
            _ = typeof(BoxCollider);
            _ = typeof(CapsuleCollider);
            _ = typeof(SphereCollider);
            _ = typeof(MeshCollider);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        [Preserve]
        static void Touch()
        {
            EnsureCollidersPreserved();
        }
    }
}
