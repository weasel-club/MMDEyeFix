#if UNITY_EDITOR

using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Goorm.MMDEyeFix
{
    public static class DictionaryUtility
    {
        public delegate TValue MergeDelegate<TValue>(TValue existing, TValue value);

        public static void AddRange<TKey, TValue>(
            this Dictionary<TKey, TValue> a, Dictionary<TKey, TValue> b, [CanBeNull] MergeDelegate<TValue> merge = null
        )
        {
            foreach (var (k, v) in b)
            {
                if (a.TryGetValue(k, out var existing)) a[k] = merge != null ? merge(existing, v) : v;
                else a[k] = v;
            }
        }
    }

    public static class MeshUtility
    {
        public static List<string> GetBlendShapeNames(this Mesh mesh)
        {
            var names = new List<string>();
            for (var i = 0; i < mesh.blendShapeCount; i++) names.Add(mesh.GetBlendShapeName(i));
            return names;
        }
    }

    public static class SkinnedMeshRendererUtility
    {
        public static float GetBlendShapeWeight(this SkinnedMeshRenderer renderer, string name)
        {
            return renderer.GetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(name));
        }
    }
}

#endif