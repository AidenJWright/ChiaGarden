using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Procedural geometry-shader hair on any mesh (desktop only).
/// Call Grow() from any script or UnityEvent to animate from short to full length.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class HairGrowth : MonoBehaviour
{
    [Header("Hair")]
    public Material hairMaterial;
    public Color rootColor                          = new Color(0.1f, 0.4f, 0.1f);
    public Color tipColor                           = new Color(0.3f, 0.8f, 0.3f);
    [Range(0.001f, 0.1f)] public float strandWidth  = 0.01f;
    [Range(0f, 1f)] public float gravityStrength    = 0.35f;
    [Range(0f, 0.5f)] public float colorJitter      = 0.15f;

    [Header("Wind")]
    [Range(0f, 1f)] public float windInfluence      = 1f;
    public Vector3 windDirection                    = Vector3.right;
    [Min(0f)] public float windStrength             = 0.05f;
    [Min(0f)] public float windFrequency            = 1f;

    [Header("Growth")]
    [Min(0f)] public float shortLength              = 0.001f;
    [Min(0f)] public float maxLength                = 0.15f;
    [Min(0.01f)] public float growDuration          = 2f;
    public AnimationCurve growCurve                 = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("If true, hair starts at shortLength in Play mode and waits for Grow().")]
    public bool startShort                          = true;

    // -------------------------------------------------------------------------

    MaterialPropertyBlock _mpb;
    MeshRenderer          _hairRenderer;
    readonly List<GameObject> _children = new();

    float     _currentLength;
    Coroutine _growRoutine;

    // -------------------------------------------------------------------------
    // Lifecycle
    // -------------------------------------------------------------------------

    void OnEnable()
    {
        _mpb ??= new MaterialPropertyBlock();
        Rebuild();
    }

    void OnDisable() => Cleanup();
    void OnDestroy() => Cleanup();

#if UNITY_EDITOR
    void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled) Rebuild();
        };
    }
#endif

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Animate hair from its current length to maxLength.
    /// Safe to call mid-growth — resumes from the current length.
    /// </summary>
    public void Grow()
    {
        if (!Application.isPlaying) return;
        if (_growRoutine != null) StopCoroutine(_growRoutine);
        _growRoutine = StartCoroutine(GrowRoutine(_currentLength, maxLength));
    }

    /// <summary>Snap hair back to shortLength immediately.</summary>
    public void ResetToShort()
    {
        if (_growRoutine != null) { StopCoroutine(_growRoutine); _growRoutine = null; }
        ApplyLength(shortLength);
    }

    /// <summary>
    /// Recreates the hair renderer. Call after changing style/material at runtime.
    /// For live length changes use Grow() or ResetToShort() instead.
    /// </summary>
    public void Rebuild()
    {
        Cleanup();
        _mpb ??= new MaterialPropertyBlock();

        if (hairMaterial == null) return;
        var mesh = GetComponent<MeshFilter>().sharedMesh;
        if (mesh == null) return;

        //GetComponent<MeshRenderer>().enabled = false;

        _currentLength = (Application.isPlaying && startShort) ? shortLength : maxLength;

        var go = new GameObject("HairStrands");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.transform.SetParent(transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;

        _hairRenderer = go.AddComponent<MeshRenderer>();
        _hairRenderer.sharedMaterial    = hairMaterial;
        _hairRenderer.shadowCastingMode = ShadowCastingMode.Off;
        _hairRenderer.receiveShadows    = false;
        _children.Add(go);

        PushProperties();
    }

    // -------------------------------------------------------------------------

    void ApplyLength(float len)
    {
        _currentLength = len;
        if (_hairRenderer != null)
        {
            _hairRenderer.GetPropertyBlock(_mpb);
            _mpb.SetFloat("_HairLength", len);
            _hairRenderer.SetPropertyBlock(_mpb);
        }
    }

    void PushProperties()
    {
        if (_hairRenderer == null) return;
        _mpb.Clear();
        _mpb.SetFloat("_HairLength",      _currentLength);
        _mpb.SetColor("_RootColor",       rootColor);
        _mpb.SetColor("_TipColor",        tipColor);
        _mpb.SetFloat("_StrandWidth",     strandWidth);
        _mpb.SetFloat("_GravityStrength", gravityStrength);
        _mpb.SetFloat("_ColorJitter",     colorJitter);
        _mpb.SetVector("_WindDirection",  windDirection.normalized);
        _mpb.SetFloat("_WindStrength",    windStrength * windInfluence);
        _mpb.SetFloat("_WindFrequency",   windFrequency);
        _hairRenderer.SetPropertyBlock(_mpb);
    }

    IEnumerator GrowRoutine(float from, float to)
    {
        float elapsed = 0f;
        while (elapsed < growDuration)
        {
            elapsed += Time.deltaTime;
            ApplyLength(Mathf.Lerp(from, to, growCurve.Evaluate(Mathf.Clamp01(elapsed / growDuration))));
            yield return null;
        }
        ApplyLength(to);
        _growRoutine = null;
    }

    void Cleanup()
    {
        if (_growRoutine != null) { StopCoroutine(_growRoutine); _growRoutine = null; }

        foreach (var go in _children)
            if (go != null) DestroyImmediate(go);
        _children.Clear();
        _hairRenderer = null;

        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = true;
    }
}
