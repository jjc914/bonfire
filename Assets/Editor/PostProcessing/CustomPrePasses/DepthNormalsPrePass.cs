using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class DepthNormalsPrePass : ScriptableRenderPass {
    private RenderTargetHandle _depthNormalsHandle;
    private Material _depthNormalsMaterial;

    private FilteringSettings _filteringSettings;

    public DepthNormalsPrePass() {
        renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;

        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
        _depthNormalsMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        _depthNormalsHandle.Init("_CameraDepthNormalsTexture");

        RenderTextureDescriptor depthNormalsDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        depthNormalsDescriptor.colorFormat = RenderTextureFormat.ARGB32;
        depthNormalsDescriptor.depthBufferBits = 32;

        cmd.GetTemporaryRT(_depthNormalsHandle.id, depthNormalsDescriptor, FilterMode.Point);
        ConfigureTarget(_depthNormalsHandle.Identifier());

        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get("Custom PrePass");
        cmd.Clear();

        SortingCriteria sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
        DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("DepthOnly"), ref renderingData, sortFlags);
        drawSettings.perObjectData = PerObjectData.None;

        Camera camera = renderingData.cameraData.camera;
        if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassMultiView)
            context.StartMultiEye(camera);

        drawSettings.overrideMaterial = _depthNormalsMaterial;
        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
        cmd.SetGlobalTexture("_CameraDepthNormalsTexture", _depthNormalsHandle.id);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) { 
        cmd.ReleaseTemporaryRT(_depthNormalsHandle.id);
    }
}