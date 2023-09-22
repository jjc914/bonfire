using System;
using System.Diagnostics;
using UnityEngine.Rendering;

[Serializable]
[DebuggerDisplay("{m_Value} ({m_OverrideState})")]
public class EnumParameter<T> : VolumeParameter<T> { }