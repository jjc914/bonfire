using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/Outline", typeof(UniversalRenderPipeline))]
public class Outline : VolumeComponent, IPostProcessComponent {
    [Serializable]
    public struct SobelParameters : IStructParameter {
        public bool hardCutoff;
        [Range(0, 4)] public float depthThreshold;
        [Range(0, 4)] public float normalsThreshold;
        [Range(0, 4)] public float textureThreshold;
    }

    [Serializable]
    public struct RobertsCrossParameters : IStructParameter {
        public bool hardCutoff;
        [Range(0, 4)] public float depthThreshold;
        [Range(0, 4)] public float normalsThreshold;
        [Range(0, 4)] public float textureThreshold;
    }

    [Serializable]
    public struct GaussianParameters : IStructParameter {
        [Min(1)] public int kernelSize;
    }

    public enum OutlineAlgorithms { None = 0, RobertsCross = 1, Sobel = 2, Gaussian = 3, JumpFlood = 4 }

    public EnumParameter<OutlineAlgorithms> algorithm = new EnumParameter<OutlineAlgorithms>();
    public ColorParameter outlineColor = new ColorParameter(Color.blue);
    public ClampedFloatParameter maxDepth = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

    public StructParameter<RobertsCrossParameters> robertCrossParameters = new StructParameter<RobertsCrossParameters>();
    public StructParameter<SobelParameters> sobelParameters = new StructParameter<SobelParameters>();
    public StructParameter<GaussianParameters> gaussianParameters = new StructParameter<GaussianParameters>();

    public bool IsActive() {
        return algorithm.value != OutlineAlgorithms.None;
    }
    
    public bool IsTileCompatible() => true;

    private static Material _material;
    public static Material material {
        private set {
            _material = value;
        }
        get {
            if (_material == null) {
                _material = new Material(Shader.Find("Hidden/Custom/PostProcessing/Outline"));
            }
            return _material;
        }
    }
}