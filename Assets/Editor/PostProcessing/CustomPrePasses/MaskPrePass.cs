using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class MaskPrePass : ScriptableRenderPass {
    private RenderTargetHandle _maskHandle;
    private Material _maskMaterial;

    private FilteringSettings _filteringSettings;

    public MaskPrePass() {
        renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;

        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
        _maskMaterial = CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Unlit");
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        _maskHandle.Init("_CameraMaskTexture");

        RenderTextureDescriptor maskDescriptor = renderingData.cameraData.cameraTargetDescriptor;

        cmd.GetTemporaryRT(_maskHandle.id, maskDescriptor, FilterMode.Point);
        ConfigureTarget(_maskHandle.Identifier());

        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get("Mask PrePass");
        cmd.Clear();

        SortingCriteria sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
        DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("DepthOnly"), ref renderingData, sortFlags);
        drawSettings.perObjectData = PerObjectData.None;

        Camera camera = renderingData.cameraData.camera;
        if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassMultiView)
            context.StartMultiEye(camera);

        drawSettings.overrideMaterial = _maskMaterial;
        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
        cmd.SetGlobalTexture("_CameraMaskTexture", _maskHandle.id);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) { 
        cmd.ReleaseTemporaryRT(_maskHandle.id);
    }
}