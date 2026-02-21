#if UNITY_EDITOR

using UnityEngine;

namespace Goorm.MMDEyeFix
{
    public class BlendShapeFrame
    {
        public readonly Vector3[] DeltaVertices;
        public readonly Vector3[] DeltaNormals;
        public readonly Vector3[] DeltaTangents;

        public BlendShapeFrame(int vertexCount)
        {
            DeltaVertices = new Vector3[vertexCount];
            DeltaNormals = new Vector3[vertexCount];
            DeltaTangents = new Vector3[vertexCount];
        }

        public BlendShapeFrame(Vector3[] deltaVertices, Vector3[] deltaNormals, Vector3[] deltaTangents)
        {
            DeltaVertices = deltaVertices;
            DeltaNormals = deltaNormals;
            DeltaTangents = deltaTangents;
        }

        private static Vector3[] Add(Vector3[] a, Vector3[] b)
        {
            var result = new Vector3[a.Length];
            for (var i = 0; i < a.Length; i++) result[i] = a[i] + b[i];
            return result;
        }

        public static BlendShapeFrame operator +(BlendShapeFrame a, BlendShapeFrame b)
        {
            return new BlendShapeFrame(
                Add(a.DeltaVertices, b.DeltaVertices),
                Add(a.DeltaNormals, b.DeltaNormals),
                Add(a.DeltaTangents, b.DeltaTangents)
            );
        }

        private static Vector3[] Multiply(Vector3[] a, float b)
        {
            var result = new Vector3[a.Length];
            for (var i = 0; i < a.Length; i++) result[i] = a[i] * b;
            return result;
        }

        public static BlendShapeFrame operator *(BlendShapeFrame a, float b)
        {
            return new BlendShapeFrame(
                Multiply(a.DeltaVertices, b),
                Multiply(a.DeltaNormals, b),
                Multiply(a.DeltaTangents, b)
            );
        }

        public static BlendShapeFrame operator *(float a, BlendShapeFrame b)
        {
            return b * a; // 교환법칙 지원
        }
    }

    public static class BlendShapeFrameUtility
    {
        public static void AddBlendShape(
            this Mesh mesh,
            string name,
            BlendShapeFrame frame
        )
        {
            mesh.AddBlendShapeFrame(name, 100, frame.DeltaVertices, frame.DeltaNormals, frame.DeltaTangents);
        }

        public static BlendShapeFrame GetBlendShape(this Mesh mesh, string blendShape)
        {
            var blendShapeIndex = mesh.GetBlendShapeIndex(blendShape);
            if (blendShapeIndex == -1)
            {
                Debug.LogError($"Blend shape {blendShape} not found.");
                return null;
            }

            var frame = new BlendShapeFrame(mesh.vertexCount);

            for (var i = 0; i < mesh.GetBlendShapeFrameCount(blendShapeIndex); i++)
            {
                var weight = mesh.GetBlendShapeFrameWeight(blendShapeIndex, i);
                frame += mesh.GetBlendShapeFrame(blendShapeIndex, i) * (weight / 100f);
            }

            return frame;
        }

        private static BlendShapeFrame GetBlendShapeFrame(this Mesh mesh, int blendShapeIndex, int frameIndex)
        {
            var deltaVertices = new Vector3[mesh.vertexCount];
            var deltaNormals = new Vector3[mesh.vertexCount];
            var deltaTangents = new Vector3[mesh.vertexCount];
            mesh.GetBlendShapeFrameVertices(blendShapeIndex, frameIndex, deltaVertices, deltaNormals, deltaTangents);
            return new BlendShapeFrame(deltaVertices, deltaNormals, deltaTangents);
        }
    }
}

#endif