using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class UnlitPrePass : ScriptableRenderPass {
    private RenderTargetHandle _unlitHandle;

    private FilteringSettings _filteringSettings;

    private Dictionary<int, Material> materialCache = new Dictionary<int, Material>();
    private Dictionary<int, Material> materialStore = new Dictionary<int, Material>();

    public UnlitPrePass() {
        renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;

        _filteringSettings = new FilteringSettings(RenderQueueRange.opaque, -1);
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        _unlitHandle.Init("_CameraUnlitTexture");

        RenderTextureDescriptor unlitDescriptor = renderingData.cameraData.cameraTargetDescriptor;
        unlitDescriptor.colorFormat = RenderTextureFormat.ARGB32;

        cmd.GetTemporaryRT(_unlitHandle.id, unlitDescriptor, FilterMode.Point);
        ConfigureTarget(_unlitHandle.Identifier());

        ConfigureClear(ClearFlag.All, Color.black);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        CommandBuffer cmd = CommandBufferPool.Get("Unlit PrePass");
        cmd.Clear();

        SortingCriteria sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
        // TODO: investigate DepthNormalsOnly: does URP already render it? is there extra work being done by adding another depthnormalpreepass? can i just add the unlit depthnormalsonly pass to custom shaders for it to work?
        DrawingSettings drawSettings = CreateDrawingSettings(new ShaderTagId("Unlit"), ref renderingData, sortFlags);
        drawSettings.perObjectData = PerObjectData.None;

        Camera camera = renderingData.cameraData.camera;
        if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassMultiView)
            context.StartMultiEye(camera);

        Renderer[] renderers = (Renderer[])Object.FindObjectsOfType(typeof(Renderer));
        foreach (Renderer r in renderers) {
            int id = r.GetInstanceID();
            materialStore[id] = r.sharedMaterial;

            // if material not created yet, init
            if (!materialCache.ContainsKey(id)) {
                materialCache[id] = CoreUtils.CreateEngineMaterial("Universal Render Pipeline/Unlit");
            }
            materialCache[id].color = r.sharedMaterial.color;
            materialCache[id].mainTexture = r.sharedMaterial.mainTexture;
            r.sharedMaterial = materialCache[id];
        }

        context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref _filteringSettings);
        cmd.SetGlobalTexture("_CameraUnlitTexture", _unlitHandle.id);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);

        //Renderer[] renderers = (Renderer[])Object.FindObjectsOfType(typeof(Renderer));
        foreach (Renderer r in renderers) {
            r.sharedMaterial = materialStore[r.GetInstanceID()];
        }
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        cmd.ReleaseTemporaryRT(_unlitHandle.id);
    }
}