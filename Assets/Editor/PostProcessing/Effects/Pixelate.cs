using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/Pixelate", typeof(UniversalRenderPipeline))]
public class Pixelate : VolumeComponent, IPostProcessComponent {
    public ClampedIntParameter resolution = new ClampedIntParameter(0, 0, 720);

    public bool IsActive() {
        return resolution.value > 0;
    }
    
    public bool IsTileCompatible() => true;

    private static Material _material;
    public static Material material {
        private set {
            _material = value;
        }
        get {
            if (_material == null) {
                _material = new Material(Shader.Find("Hidden/Custom/PostProcessing/Pixelate"));
            }
            return _material;
        }
    }
}
