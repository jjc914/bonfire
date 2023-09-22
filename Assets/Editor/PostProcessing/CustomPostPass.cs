using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomPostPass : ScriptableRenderPass {
    private RenderTargetIdentifier _sourceIdentifier;

    private bool _onDestinationA = false;
    private RenderTargetIdentifier _originIdentifier;
    private RenderTargetHandle _destinationAHandle;
    private RenderTargetHandle _destinationBHandle;

    public CustomPostPass() {
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
        _sourceIdentifier = renderingData.cameraData.renderer.cameraColorTarget;

        RenderTextureDescriptor descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;

        cmd.GetTemporaryRT(_destinationAHandle.id, descriptor, FilterMode.Bilinear);
        ConfigureTarget(_destinationAHandle.Identifier());

        cmd.GetTemporaryRT(_destinationBHandle.id, descriptor, FilterMode.Bilinear);
        ConfigureTarget(_destinationBHandle.Identifier());
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
        if (renderingData.cameraData.isSceneViewCamera)
            return;
        if (renderingData.cameraData.isPreviewCamera)
            return;

        Camera camera = renderingData.cameraData.camera;
        if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassMultiView)
            context.StartMultiEye(camera);

        CommandBuffer cmd = CommandBufferPool.Get("Custom PostPass");
        cmd.Clear();

        // get current volumes information
        VolumeStack stack = VolumeManager.instance.stack;

        // swap render textures back and forth
        void BlitTo(Material mat, int pass = 0) {
            RenderTargetIdentifier origin;
            RenderTargetIdentifier destination;
            if (_onDestinationA) {
                origin = _originIdentifier;
                destination = _destinationBHandle.Identifier();
            }
            else {
                origin = _originIdentifier;
                destination = _destinationAHandle.Identifier();
            }
            Blit(cmd, origin, destination, mat, pass);

            _originIdentifier = destination;
            _onDestinationA = !_onDestinationA;
        }
        _originIdentifier = _sourceIdentifier;

        // pixelate pass
        Material pixelateMaterial = Pixelate.material;
        if (pixelateMaterial == null) {
            Debug.LogError("Custom Post Processing Materials instance is null");
            return;
        }

        Pixelate pixelate = stack.GetComponent<Pixelate>();
        if (pixelate.IsActive()) {
            // TODO: optimize by caching the property ID somewhere else
            pixelateMaterial.SetInteger(Shader.PropertyToID("_resolution"), pixelate.resolution.value);

            BlitTo(pixelateMaterial);
        }

        // outline pass
        Material outlineMaterial = Outline.material;
        if (outlineMaterial == null) {
            Debug.LogError("Custom Post Processing Materials instance is null");
            return;
        }

        Outline outline = stack.GetComponent<Outline>();
        if (outline.IsActive()) {
            // TODO: optimize by caching the property ID somewhere else
            outlineMaterial.SetInteger(Shader.PropertyToID("_algorithm"), (int)outline.algorithm.value);
            outlineMaterial.SetColor(Shader.PropertyToID("_outlineColor"), outline.outlineColor.value);
            outlineMaterial.SetFloat(Shader.PropertyToID("_maxDepth"), outline.maxDepth.value);
            switch (outline.algorithm.value) {
                case Outline.OutlineAlgorithms.RobertsCross:
                    outlineMaterial.SetInt(Shader.PropertyToID("_hardCutoff"), outline.robertCrossParameters.value.hardCutoff ? 1 : 0);
                    outlineMaterial.SetFloat(Shader.PropertyToID("_depthThreshold"), outline.robertCrossParameters.value.depthThreshold);
                    outlineMaterial.SetFloat(Shader.PropertyToID("_normalsThreshold"), outline.robertCrossParameters.value.normalsThreshold);
                    outlineMaterial.SetFloat(Shader.PropertyToID("_textureThreshold"), outline.robertCrossParameters.value.textureThreshold);
                    break;
                case Outline.OutlineAlgorithms.Sobel:
                    outlineMaterial.SetInt(Shader.PropertyToID("_hardCutoff"), outline.sobelParameters.value.hardCutoff ? 1 : 0);
                    outlineMaterial.SetFloat(Shader.PropertyToID("_depthThreshold"), outline.sobelParameters.value.depthThreshold);
                    outlineMaterial.SetFloat(Shader.PropertyToID("_normalsThreshold"), outline.sobelParameters.value.normalsThreshold);
                    outlineMaterial.SetFloat(Shader.PropertyToID("_textureThreshold"), outline.sobelParameters.value.textureThreshold);
                    break;
                case Outline.OutlineAlgorithms.Gaussian:
                    outlineMaterial.SetInt(Shader.PropertyToID("_kernelSize"), outline.gaussianParameters.value.kernelSize);
                    break;
                case Outline.OutlineAlgorithms.JumpFlood:
                    break;
                default:
                    break;
            }

            // initial pass
            BlitTo(outlineMaterial, 0);

            // secondary passes
            switch (outline.algorithm.value) {
                case Outline.OutlineAlgorithms.RobertsCross:
                    break;
                case Outline.OutlineAlgorithms.Sobel:
                    break;
                case Outline.OutlineAlgorithms.Gaussian:
                    BlitTo(outlineMaterial, 1);
                    break;
                case Outline.OutlineAlgorithms.JumpFlood:
                    break;
                default:
                    break;
            }
        }

        // debugview pass
        Material debugViewMaterial = DebugView.material;
        if (debugViewMaterial == null) {
            Debug.LogError("Custom Post Processing Materials instance is null");
            return;
        }

        DebugView debugView = stack.GetComponent<DebugView>();
        if (debugView.IsActive()) {
            // TODO: optimize by caching the property ID somewhere else
            debugViewMaterial.SetInteger(Shader.PropertyToID("_debugMode"), (int)debugView.mode.value);

            BlitTo(debugViewMaterial);
        }

        Blit(cmd, _originIdentifier, _sourceIdentifier);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd) {
        // clean render textures
        cmd.ReleaseTemporaryRT(_destinationAHandle.id);
        cmd.ReleaseTemporaryRT(_destinationBHandle.id);
    }
}
