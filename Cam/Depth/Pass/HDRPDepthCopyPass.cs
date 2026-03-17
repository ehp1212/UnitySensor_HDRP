using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Sensor.Cam.Depth.Pass
{
    internal class HDRPDepthCopyPass : CustomPass
    {
        public RenderTexture targetDepth;
        public Material depthMaterial;

        protected override void Execute(CustomPassContext ctx)
        {
            if (targetDepth == null || depthMaterial == null)
                return;

            CoreUtils.SetRenderTarget(ctx.cmd, targetDepth);
            CoreUtils.DrawFullScreen(ctx.cmd, depthMaterial);
        }
    }
}