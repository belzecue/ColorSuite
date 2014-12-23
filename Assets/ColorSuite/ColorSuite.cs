﻿//
// Copyright (C) 2014 Keijiro Takahashi
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using UnityEngine;
using System.Collections;

[ExecuteInEditMode]
[ImageEffectTransformsToLDR]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/Color Adjustments/Color Suite")]
public class ColorSuite : MonoBehaviour
{
    // Curve objects.
    [SerializeField] AnimationCurve _rCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _gCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _bCurve = AnimationCurve.Linear(0, 0, 1, 1);
    [SerializeField] AnimationCurve _lCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public AnimationCurve redCurve {
        get { return _rCurve; }
        set { _rCurve = value; UpdateCurves(); }
    }
    public AnimationCurve greenCurve {
        get { return _gCurve; }
        set { _gCurve = value; UpdateCurves(); }
    }
    public AnimationCurve blueCurve {
        get { return _bCurve; }
        set { _bCurve = value; UpdateCurves(); }
    }
    public AnimationCurve luminanceCurve {
        get { return _lCurve; }
        set { _lCurve = value; UpdateCurves(); }
    }

    // Adjustment parameters.
    [SerializeField] float _brightness = 0.0f;
    [SerializeField] float _contrast   = 1.0f;
    [SerializeField] float _saturation = 1.0f;

    public float brightness {
        get { return _brightness; }
        set { _brightness = value; UpdateCurves(); }
    }
    public float contrast {
        get { return _contrast; }
        set { _contrast = value; UpdateCurves(); }
    }
    public float saturation {
        get { return _saturation; }
        set { _saturation = value; } // no UpdateCurves
    }

    // Tonemapping parameters.
    [SerializeField] bool _tonemapping = false;
    [SerializeField] float _exposure   = 1.8f;

    public bool tonemapping {
        get { return _tonemapping; }
        set { _tonemapping = value; }
    }
    public float exposure {
        get { return _exposure; }
        set { _exposure = value; }
    }

    // Vignette parameters.
    [SerializeField] float _vignette = 0.0f;

    public float vignette {
        get { return _vignette; }
        set { _vignette = value; }
    }

    // Dithering options.
    public enum DitherMode { Off, Ordered, Triangular  }
    [SerializeField] DitherMode _ditherMode = DitherMode.Off;

    public DitherMode ditherMode {
        get { return _ditherMode; }
        set { _ditherMode = value; }
    }

    // Reference to the shader.
    [SerializeField] Shader shader;

    // Temporary objects.
    Material _material;
    Texture2D _texture;

    Color EncodeRGBM(float r, float g, float b)
    {
        var a = Mathf.Max(Mathf.Max(r, g), Mathf.Max(b, 1e-6f));
        a = Mathf.Ceil(a * 255) / 255;
        return new Color(r / a, g / a, b / a, a);
    }

    void SetUpResources()
    {
        if (_material == null)
        {
            _material = new Material(shader);
            _material.hideFlags = HideFlags.DontSave;
        }

        if (_texture == null)
        {
            _texture = new Texture2D(512, 1, TextureFormat.ARGB32, false, true);
            _texture.hideFlags = HideFlags.DontSave;
            _texture.wrapMode = TextureWrapMode.Clamp;
            UpdateCurves();
        }
    }

    void UpdateCurves()
    {
        // Variables for brightness adjustment.
        var bt = _brightness > 0 ? 1.0f : -1.0f;
        var bp = Mathf.Abs(_brightness);

        for (var x = 0; x < _texture.width; x++)
        {
            var u = 1.0f / (_texture.width - 1) * x;
            var r = Mathf.Lerp(_lCurve.Evaluate((_rCurve.Evaluate(u) - 0.5f) * _contrast + 0.5f), bt, bp);
            var g = Mathf.Lerp(_lCurve.Evaluate((_gCurve.Evaluate(u) - 0.5f) * _contrast + 0.5f), bt, bp);
            var b = Mathf.Lerp(_lCurve.Evaluate((_bCurve.Evaluate(u) - 0.5f) * _contrast + 0.5f), bt, bp);
            _texture.SetPixel(x, 0, EncodeRGBM(r, g, b));
        }

        _texture.Apply();
    }

    void Start()
    {
        SetUpResources();
    }

    void OnValidate()
    {
        SetUpResources();
        UpdateCurves();
    }

    void Reset()
    {
        SetUpResources();
        UpdateCurves();
    }

    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        SetUpResources();

        _material.SetTexture("_Curves", _texture);
        _material.SetFloat("_Saturation", _saturation);

        if (QualitySettings.activeColorSpace == ColorSpace.Linear)
            _material.EnableKeyword("LINEAR_ON");
        else
            _material.DisableKeyword("LINEAR_ON");

        if (_tonemapping)
        {
            _material.EnableKeyword("TONEMAPPING_ON");
            _material.SetFloat("_Exposure", _exposure);
        }
        else
            _material.DisableKeyword("TONEMAPPING_ON");

        if (_vignette > 0.0f)
        {
            _material.EnableKeyword("VIGNETTE_ON");
            _material.SetFloat("_Vignette", _vignette);
        }
        else
            _material.DisableKeyword("VIGNETTE_ON");

        if (_ditherMode == DitherMode.Ordered)
        {
            _material.EnableKeyword("DITHER_ORDERED");
            _material.DisableKeyword("DITHER_TRIANGULAR");
        }
        else if (_ditherMode == DitherMode.Triangular)
        {
            _material.DisableKeyword("DITHER_ORDERED");
            _material.EnableKeyword("DITHER_TRIANGULAR");
        }
        else
        {
            _material.DisableKeyword("DITHER_ORDERED");
            _material.DisableKeyword("DITHER_TRIANGULAR");
        }

        Graphics.Blit(source, destination, _material);
    }
}
