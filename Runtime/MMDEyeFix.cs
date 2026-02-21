#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using nadena.dev.modular_avatar.core;
using UnityEditor;
using UnityEngine;
using VRC.Core;
using VRC.SDK3.Avatars.Components;
using VRC.SDKBase;
using VRC.Utility;

namespace Goorm.MMDEyeFix
{
    [ExecuteInEditMode]
    public class MMDEyeFix : MonoBehaviour, IEditorOnly
    {
        public AvatarObjectReference _faceRenderer = new();

        public List<string> _leftEyeBlendShapes = new();
        public float _leftEyeWeight = 1f;

        public List<string> _rightEyeBlendShapes = new();
        public float _rightEyeWeight = 1f;

        public List<string> _mouthBlendShapes = new();
        public float _mouthWeight = 1f;

        public bool _applyToCustomBlendShapes = false;
        public List<string> _customLeftEyeBlendShapes = new();
        public List<string> _customRightEyeBlendShapes = new();
        public List<string> _customMouthBlendShapes = new();

        private const string AssetFolderGuid = "97412d17d4e68404bb146c8e755f89f8";

        [SerializeField] private Mesh _originalMesh;
        [SerializeField] private Mesh _fixedMesh;

        [NonSerialized] public bool RevertOnDisable = true;

        private string GetCacheKey(SkinnedMeshRenderer faceRenderer)
        {
            var key = "";

            key += string.Join(",", _leftEyeBlendShapes) + _leftEyeWeight + "\n" +
                   string.Join(",", _rightEyeBlendShapes) + _rightEyeWeight + "\n" +
                   string.Join(",", _mouthBlendShapes) + _mouthWeight;

            key += GlobalObjectId.GetGlobalObjectIdSlow(faceRenderer) + "\n";

            var blendShapeNames = new HashSet<string>();
            foreach (var blendShape in _leftEyeBlendShapes) blendShapeNames.Add(blendShape);
            foreach (var blendShape in _rightEyeBlendShapes) blendShapeNames.Add(blendShape);
            foreach (var blendShape in _mouthBlendShapes) blendShapeNames.Add(blendShape);

            foreach (var blendShape in blendShapeNames)
            {
                key += blendShape + faceRenderer.GetBlendShapeWeight(blendShape) + "\n";
            }

            if (_applyToCustomBlendShapes)
            {
                foreach (var blendShape in _customLeftEyeBlendShapes) key += blendShape + "\n";
                foreach (var blendShape in _customRightEyeBlendShapes) key += blendShape + "\n";
                foreach (var blendShape in _customMouthBlendShapes) key += blendShape + "\n";
            }

            var mesh = faceRenderer.sharedMesh;
            key += AssetDatabase.GetAssetDependencyHash(AssetDatabase.GetAssetPath(mesh)) + "\n";
            return key;
        }

        [SerializeField] private string _cacheKey;
        [SerializeField] private Hash128 _cacheHash;

        public bool IsApplied => _originalMesh != null || _fixedMesh != null;

        private static void ApplyMesh(GameObject avatarRootObject, Mesh from, Mesh to)
        {
            var renderers = avatarRootObject
                .GetComponentsInChildren<SkinnedMeshRenderer>(true)
                .Where(r => r.sharedMesh == from);

            foreach (var otherRenderer in renderers) otherRenderer.sharedMesh = to;
        }

        private static string GetMeshAssetPath(VRCAvatarDescriptor avatar, SkinnedMeshRenderer renderer)
        {
            var folder = AssetDatabase.GUIDToAssetPath(AssetFolderGuid);
            if (string.IsNullOrEmpty(folder)) folder = "Assets/MMDEyeFix/Generated";

            var blueprintId = avatar.GetComponent<PipelineManager>()?.blueprintId;
            var avatarId = blueprintId ?? avatar.gameObject.name;
            folder = $"{folder}/{avatarId}";

            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
            return $"{folder}/{renderer.sharedMesh.name}.asset";
        }

        public void Apply(VRCAvatarDescriptor avatar = null)
        {
            if (avatar == null)
            {
                avatar = transform.FindComponentInParent<VRCAvatarDescriptor>();
                if (avatar == null)
                {
                    Debug.LogError("Avatar descriptor not found.");
                    return;
                }
            }

            // Get renderer
            var faceGameObject = _faceRenderer.Get(this);
            if (faceGameObject == null)
            {
                Debug.LogError("Face renderer not found.");
                return;
            }

            var faceRenderer = faceGameObject.GetComponent<SkinnedMeshRenderer>();
            if (faceRenderer == null)
            {
                Debug.LogError("Face renderer not found.");
                return;
            }

            // 이미 적용된 경우 되돌리기
            Revert(avatar);

            Mesh fixedMesh;
            var path = GetMeshAssetPath(avatar, faceRenderer);

            // 캐싱
            var cacheKey = GetCacheKey(faceRenderer);
            if (_cacheKey == cacheKey)
            {
                fixedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (fixedMesh != null)
                {
                    var hash = AssetDatabase.GetAssetDependencyHash(path);
                    if (hash.Equals(_cacheHash))
                    {
                        _originalMesh = faceRenderer.sharedMesh;
                        _fixedMesh = fixedMesh;
                        ApplyMesh(avatar.gameObject, faceRenderer.sharedMesh, fixedMesh);
                        return;
                    }
                }
            }

            // Fix mesh
            var originalMesh = faceRenderer.sharedMesh;
            fixedMesh = GetFixedMesh(faceRenderer, path);

            _cacheKey = cacheKey;
            _cacheHash = AssetDatabase.GetAssetDependencyHash(path);

            _originalMesh = originalMesh;
            _fixedMesh = fixedMesh;

            // Apply fixed mesh
            ApplyMesh(avatar.gameObject, originalMesh, fixedMesh);
        }

        public void Revert(VRCAvatarDescriptor avatar = null)
        {
            if (_originalMesh == null || _fixedMesh == null) return;

            if (avatar == null)
            {
                avatar = transform.FindComponentInParent<VRCAvatarDescriptor>();
                if (avatar == null)
                {
                    Debug.LogError("Avatar descriptor not found.");
                    return;
                }
            }

            // Revert fixed mesh
            ApplyMesh(avatar.gameObject, _fixedMesh, _originalMesh);

            _originalMesh = null;
            _fixedMesh = null;
        }

        private void OnEnable()
        {
            if (IsApplied) Apply();
        }

        private void OnDisable()
        {
            if (RevertOnDisable) Revert();
        }

        private void Reset()
        {
            Revert();
        }

        private Mesh GetFixedMesh(SkinnedMeshRenderer sourceRenderer, string path)
        {
            var sourceMesh = sourceRenderer.sharedMesh;

            var newMesh = Instantiate(sourceMesh);
            newMesh.ClearBlendShapes();

            foreach (var blendShape in sourceMesh.GetBlendShapeNames())
            {
                var frame = sourceMesh.GetBlendShape(blendShape);

                var mmdBlendShape = MmdBlendShapes.Get(blendShape);

                var leftEyeMMD = mmdBlendShape != null && mmdBlendShape.Category.HasFlag(MmdBlendShapeCategory.LeftEye);
                var rightEyeMMD = mmdBlendShape != null && mmdBlendShape.Category.HasFlag(MmdBlendShapeCategory.RightEye);
                var mouthMMD = mmdBlendShape != null && mmdBlendShape.Category.HasFlag(MmdBlendShapeCategory.Mouth);
                var leftEyeCustom = _applyToCustomBlendShapes && _customLeftEyeBlendShapes.Contains(blendShape);
                var rightEyeCustom = _applyToCustomBlendShapes && _customRightEyeBlendShapes.Contains(blendShape);
                var mouthCustom = _applyToCustomBlendShapes && _customMouthBlendShapes.Contains(blendShape);

                var leftEye = leftEyeMMD || leftEyeCustom;
                var rightEye = rightEyeMMD || rightEyeCustom;
                var mouth = mouthMMD || mouthCustom;

                if (leftEye) frame += GetFixFrame(sourceRenderer, _leftEyeBlendShapes) * _leftEyeWeight;
                if (rightEye) frame += GetFixFrame(sourceRenderer, _rightEyeBlendShapes) * _rightEyeWeight;
                if (mouth) frame += GetFixFrame(sourceRenderer, _mouthBlendShapes) * _mouthWeight;

                newMesh.AddBlendShape(blendShape, frame);
            }

            // Save into asset
            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return newMesh;
        }

        private static BlendShapeFrame GetFixFrame(
            SkinnedMeshRenderer renderer,
            IEnumerable<string> blendShapes
        )
        {
            var frame = new BlendShapeFrame(renderer.sharedMesh.vertexCount);
            foreach (var blendShape in blendShapes)
            {
                var weight = renderer.GetBlendShapeWeight(renderer.sharedMesh.GetBlendShapeIndex(blendShape));
                frame += renderer.sharedMesh.GetBlendShape(blendShape) * (-weight / 100f);
            }

            return frame;
        }
    }
}
#endif