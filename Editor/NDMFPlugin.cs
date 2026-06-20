#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using nadena.dev.ndmf;
using UnityEngine;
using VRC.SDK3.Avatars.Components;

[assembly: ExportsPlugin(typeof(Goorm.MMDEyeFix.NDMFPlugin))]

namespace Goorm.MMDEyeFix
{
    public class NDMFPlugin : Plugin<NDMFPlugin>
    {
        public override string DisplayName => "MMDEyeFix";

        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming)
                .BeforePlugin("com.anatawa12.avatar-optimizer")
                .BeforePlugin("ShellProtectorNDMFPlugin")
                .Run("Transforming MMD BlendShapes", ctx =>
                {
                    var optimizers = ctx.AvatarRootObject.GetComponentsInChildren<MMDEyeFix>(true);

                    if (optimizers.Length == 0)
                    {
                        return;
                    }

                    WarnConflictingTargets(optimizers, ctx.AvatarDescriptor);

                    foreach (var optimizer in optimizers)
                    {
                        optimizer.RevertOnDisable = false;
                        optimizer.Apply(ctx.AvatarDescriptor);
                        Object.DestroyImmediate(optimizer);
                    }
                });
        }

        private static void WarnConflictingTargets(MMDEyeFix[] optimizers, VRCAvatarDescriptor avatar)
        {
            var faceRenderers = new Dictionary<MMDEyeFix, SkinnedMeshRenderer>();
            foreach (var optimizer in optimizers)
            {
                var faceRenderer = optimizer._faceRenderer.Get(optimizer)?.GetComponent<SkinnedMeshRenderer>();
                if (faceRenderer == null) continue;

                if (faceRenderers.ContainsValue(faceRenderer))
                {
                    Debug.LogWarning($"Multiple MMDEyeFix components target the same renderer: {faceRenderer.name}");
                }

                faceRenderers[optimizer] = faceRenderer;
            }

            foreach (var entry in faceRenderers)
            {
                var optimizer = entry.Key;
                var faceRenderer = entry.Value;
                if (optimizer._applyTargetMode != ApplyTargetMode.RenderersSharingMesh) continue;

                var targetRenderers = optimizer
                    .GetTargetRenderers(avatar, faceRenderer, faceRenderer.sharedMesh)
                    .ToList();

                foreach (var otherEntry in faceRenderers)
                {
                    var otherOptimizer = otherEntry.Key;
                    var otherFaceRenderer = otherEntry.Value;
                    if (otherOptimizer == optimizer || !targetRenderers.Contains(otherFaceRenderer)) continue;

                    Debug.LogWarning(
                        $"MMDEyeFix on {optimizer.name} uses shared mesh mode and also affects " +
                        $"{otherFaceRenderer.name}, which is targeted by another MMDEyeFix component."
                    );
                }
            }
        }
    }
}

#endif
