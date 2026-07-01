using Godot;
using MegaCrit.Sts2.Core.Helpers;

namespace WhatIfRelics.WhatIfRelicsCode.Ui;

internal sealed partial class WhatIfFogSpireMapPointFog : Control
{
    private const string FogNodeName = "WhatIfFogSpirePointFog";

    private readonly List<FogLayerState> _layers = [];
    private readonly Vector2 _basePosition;
    private readonly float _phase;
    private readonly float _driftAmplitudeX;
    private readonly float _driftAmplitudeY;
    private readonly float _driftSpeedX;
    private readonly float _driftSpeedY;
    private readonly float _globalPulseSpeed;
    private readonly float _globalPulseAmount;

    private WhatIfFogSpireMapPointFog(Vector2 size, Vector2 positionOffset, float phase)
    {
        Name = FogNodeName;
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 1;
        Position = positionOffset;
        Size = size;
        PivotOffset = size * 0.5f;
        ClipContents = true;
        _basePosition = positionOffset;
        _phase = phase;

        float variation = Fract(phase * 0.173f + 0.27f);
        _driftAmplitudeX = Mathf.Lerp(1.8f, 4.6f, variation);
        _driftAmplitudeY = Mathf.Lerp(1.4f, 3.7f, Fract(phase * 0.211f + 0.49f));
        _driftSpeedX = Mathf.Lerp(0.34f, 0.58f, Fract(phase * 0.193f + 0.17f));
        _driftSpeedY = Mathf.Lerp(0.28f, 0.51f, Fract(phase * 0.287f + 0.61f));
        _globalPulseSpeed = Mathf.Lerp(0.26f, 0.44f, Fract(phase * 0.101f + 0.73f));
        _globalPulseAmount = Mathf.Lerp(0.01f, 0.035f, Fract(phase * 0.149f + 0.33f));

        foreach (FogLayerSpec spec in BuildLayerSpecs(size, variation, phase))
        {
            TextureRect layer = new()
            {
                Name = spec.Name,
                MouseFilter = MouseFilterEnum.Ignore,
                Texture = spec.Texture,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                Position = spec.Offset,
                Size = spec.LayerSize,
                PivotOffset = spec.LayerSize * 0.5f,
                Rotation = spec.BaseRotation,
                Modulate = spec.Modulate
            };

            this.AddChildSafely(layer);
            _layers.Add(new FogLayerState(layer, spec));
        }
    }

    public static WhatIfFogSpireMapPointFog GetOrCreate(Control point, Vector2 size, Vector2 positionOffset, float phase)
    {
        point.GetNodeOrNull<WhatIfFogSpireMapPointFog>(FogNodeName)?.QueueFreeSafely();

        WhatIfFogSpireMapPointFog fog = new(size, positionOffset, phase);
        point.AddChildSafely(fog);
        return fog;
    }

    public static void Remove(Control point)
    {
        point.GetNodeOrNull<WhatIfFogSpireMapPointFog>(FogNodeName)?.QueueFreeSafely();
    }

    public override void _Process(double delta)
    {
        float time = Time.GetTicksMsec() * 0.001f;
        float driftX = Mathf.Sin(time * _driftSpeedX + _phase) * _driftAmplitudeX;
        float driftY = Mathf.Cos(time * _driftSpeedY + _phase * 1.31f) * _driftAmplitudeY;
        float pulse = 1f + Mathf.Sin(time * _globalPulseSpeed + _phase * 0.79f) * _globalPulseAmount;

        Position = _basePosition + new Vector2(driftX, driftY);
        Scale = new Vector2(pulse, pulse);

        foreach (FogLayerState layer in _layers)
        {
            FogLayerSpec spec = layer.Spec;
            float alphaWave = Mathf.Sin(time * spec.AlphaSpeed + _phase * spec.AlphaPhase);
            float driftWaveX = Mathf.Cos(time * spec.DriftSpeedX + _phase * spec.DriftPhaseX);
            float driftWaveY = Mathf.Sin(time * spec.DriftSpeedY + _phase * spec.DriftPhaseY);
            float scaleWave = Mathf.Sin(time * spec.ScaleSpeed + _phase * spec.ScalePhase);
            float rotationWave = Mathf.Sin(time * spec.RotationSpeed + _phase * spec.RotationPhase);

            layer.Visual.Position = spec.Offset + new Vector2(
                driftWaveX * spec.DriftAmountX,
                driftWaveY * spec.DriftAmountY);
            layer.Visual.Scale = Vector2.One * (1f + scaleWave * spec.ScaleAmount);
            layer.Visual.Rotation = spec.BaseRotation + rotationWave * spec.RotationAmount;

            Color modulate = spec.Modulate;
            modulate.A = spec.Modulate.A + alphaWave * spec.AlphaAmount;
            layer.Visual.Modulate = modulate;
        }
    }

    private static IEnumerable<FogLayerSpec> BuildLayerSpecs(Vector2 size, float variation, float phase)
    {
        Vector2 outerSize = size * Mathf.Lerp(1.12f, 1.2f, variation);
        Vector2 midSize = size * Mathf.Lerp(0.9f, 1.02f, variation);
        Vector2 innerSize = size * Mathf.Lerp(0.64f, 0.78f, variation);

        yield return new FogLayerSpec(
            "OuterFog",
            CreateFogTexture(110, 110, phase + 0.11f, 6, 0.58f, 0.82f),
            new Color(0.33f, 0.34f, 0.35f, 0.42f),
            CenterOffset(size, outerSize, -8f, -6f),
            outerSize,
            Mathf.DegToRad(Mathf.Lerp(-7f, 5f, variation)),
            1.9f,
            1.5f,
            0.31f,
            0.23f,
            0.7f,
            0.48f,
            0.08f,
            0.03f,
            0.02f,
            0.025f,
            0.76f,
            0.93f,
            0.58f,
            1.07f,
            0.41f);

        yield return new FogLayerSpec(
            "BodyFog",
            CreateFogTexture(96, 96, phase + 1.37f, 5, 0.74f, 1f),
            new Color(0.41f, 0.42f, 0.43f, 0.56f),
            CenterOffset(size, midSize, -2f, -2f),
            midSize,
            Mathf.DegToRad(Mathf.Lerp(-10f, 12f, Fract(variation * 1.43f))),
            1.6f,
            1.1f,
            0.37f,
            0.29f,
            0.9f,
            0.63f,
            0.07f,
            0.03f,
            0.04f,
            0.08f,
            1.12f,
            0.86f,
            1.09f,
            0.72f,
            0.67f);

        yield return new FogLayerSpec(
            "InnerFog",
            CreateFogTexture(78, 78, phase + 2.71f, 4, 0.88f, 1.14f),
            new Color(0.50f, 0.51f, 0.52f, 0.44f),
            CenterOffset(size, innerSize, 6f, 4f),
            innerSize,
            Mathf.DegToRad(Mathf.Lerp(-15f, 9f, Fract(variation * 2.19f))),
            1.3f,
            1.6f,
            0.44f,
            0.34f,
            0.56f,
            0.74f,
            0.06f,
            0.05f,
            0.035f,
            0.1f,
            0.63f,
            1.28f,
            0.79f,
            1.16f,
            1.21f);
    }

    private static ImageTexture CreateFogTexture(
        int width,
        int height,
        float seed,
        int blobCount,
        float alphaScale,
        float stretchY)
    {
        Image image = Image.CreateEmpty(width, height, false, Image.Format.Rgba8);
        image.Fill(Colors.Transparent);

        Span<Vector2> centers = blobCount <= 8 ? stackalloc Vector2[blobCount] : new Vector2[blobCount];
        Span<float> radii = blobCount <= 8 ? stackalloc float[blobCount] : new float[blobCount];
        Span<float> weights = blobCount <= 8 ? stackalloc float[blobCount] : new float[blobCount];

        for (int i = 0; i < blobCount; i++)
        {
            float px = Mathf.Lerp(0.24f, 0.76f, Noise01(seed, i * 3 + 1));
            float py = Mathf.Lerp(0.22f, 0.78f, Noise01(seed, i * 3 + 2));
            float radius = Mathf.Lerp(0.18f, 0.34f, Noise01(seed, i * 3 + 3));
            float weight = Mathf.Lerp(0.75f, 1.2f, Noise01(seed, i * 3 + 4));

            centers[i] = new Vector2(px, py);
            radii[i] = radius;
            weights[i] = weight;
        }

        for (int y = 0; y < height; y++)
        {
            float v = height <= 1 ? 0.5f : (float)y / (height - 1);
            for (int x = 0; x < width; x++)
            {
                float u = width <= 1 ? 0.5f : (float)x / (width - 1);

                float density = 0f;
                for (int i = 0; i < blobCount; i++)
                {
                    Vector2 delta = new(
                        u - centers[i].X,
                        (v - centers[i].Y) * stretchY);
                    float dist = delta.Length();
                    float blob = Mathf.Clamp(1f - dist / radii[i], 0f, 1f);
                    density += blob * blob * weights[i];
                }

                float edgeFade = Smooth01(Mathf.Clamp(1f - DistanceToCenter(u, v) * 1.38f, 0f, 1f));
                float noise = 0.84f + Noise01(seed * 1.71f + u * 3.7f + v * 1.9f, x + y * width) * 0.16f;
                float alpha = Mathf.Clamp((density - 0.34f) * 0.78f, 0f, 1f) * edgeFade * alphaScale * noise;
                if (alpha < 0.01f)
                {
                    continue;
                }

                image.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        return ImageTexture.CreateFromImage(image);
    }

    private static float DistanceToCenter(float u, float v)
    {
        float dx = (u - 0.5f) * 2f;
        float dy = (v - 0.5f) * 2f;
        return Mathf.Sqrt(dx * dx + dy * dy);
    }

    private static float Smooth01(float value)
    {
        return value * value * (3f - 2f * value);
    }

    private static float Noise01(float seed, int offset)
    {
        return Fract(Mathf.Sin(seed * 12.9898f + offset * 78.233f) * 43758.5453f);
    }

    private static float Fract(float value)
    {
        return value - Mathf.Floor(value);
    }

    private static Vector2 CenterOffset(Vector2 containerSize, Vector2 layerSize, float extraX, float extraY)
    {
        return (containerSize - layerSize) * 0.5f + new Vector2(extraX, extraY);
    }

    private readonly record struct FogLayerState(TextureRect Visual, FogLayerSpec Spec);

    private readonly record struct FogLayerSpec(
        string Name,
        Texture2D Texture,
        Color Modulate,
        Vector2 Offset,
        Vector2 LayerSize,
        float BaseRotation,
        float DriftAmountX,
        float DriftAmountY,
        float DriftSpeedX,
        float DriftSpeedY,
        float AlphaSpeed,
        float ScaleSpeed,
        float RotationSpeed,
        float AlphaAmount,
        float ScaleAmount,
        float RotationAmount,
        float AlphaPhase,
        float DriftPhaseX,
        float DriftPhaseY,
        float ScalePhase,
        float RotationPhase);
}
