#if UNITY_EDITOR

using System.Linq;
using nadena.dev.ndmf;
using UnityEditor;
using UnityEngine;

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

                    switch (optimizers.Length)
                    {
                        case 0:
                            return;
                        case > 1:
                            Debug.LogError("Multiple MMDEyeFix components found in the avatar. Only one is allowed.");
                            return;
                        default:
                            var optimizer = optimizers.First();
                            optimizer.RevertOnDisable = false;
                            optimizer.Apply(ctx.AvatarDescriptor);
                            Object.DestroyImmediate(optimizer);
                            break;
                    }
                });
        }
    }
}

#endif