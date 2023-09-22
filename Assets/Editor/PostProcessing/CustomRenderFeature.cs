using UnityEngine.Rendering.Universal;

public class CustomRenderFeature : ScriptableRendererFeature {
    private DepthNormalsPrePass _depthNormalsPass;
    private MaskPrePass _maskPass;
    private UnlitPrePass _unlitPass;

    private CustomPostPass _postPass;

    public override void Create() {
        _depthNormalsPass = new DepthNormalsPrePass();
        _maskPass = new MaskPrePass();
        //_unlitPass = new UnlitPrePass();
        _postPass = new CustomPostPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
        renderer.EnqueuePass(_depthNormalsPass);
        renderer.EnqueuePass(_maskPass);
        //renderer.EnqueuePass(_unlitPass);
        renderer.EnqueuePass(_postPass);
    }
}