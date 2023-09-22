using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenuForRenderPipeline("Custom/DebugView", typeof(UniversalRenderPipeline))]
public class DebugView : VolumeComponent, IPostProcessComponent {
    public enum DebugMode { None = 0, Depth = 1, Normals = 2 }

    public EnumParameter<DebugMode> mode = new EnumParameter<DebugMode>();

    public bool IsActive() {
        return mode.value != DebugMode.None;
    }
    
    public bool IsTileCompatible() => true;

    private static Material _material;
    public static Material material {
        private set {
            _material = value;
        }
        get {
            if (_material == null) {
                _material = new Material(Shader.Find("Hidden/Custom/PostProcessing/DebugView"));
            }
            return _material;
        }
    }
}
