using UnityEngine.Rendering;

[System.Serializable]
[VolumeComponentMenu("Custom/Gaussian Blur")]
public class GaussianBlurVolume : VolumeComponent, IPostProcessComponent
{
    public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);
    public ClampedIntParameter radius = new ClampedIntParameter(3, 0, 16);

    // 1 = full res, 2 = half, 4 = quarter
    public ClampedIntParameter downsample = new ClampedIntParameter(2, 1, 4);

    public bool IsActive() => intensity.value > 0f && radius.value > 0;
    public bool IsTileCompatible() => true;
}
