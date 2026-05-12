using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Rain")]
    [SerializeField] private GameObject rainSystem;
    [SerializeField] private KeyCode toggleRainKey = KeyCode.R;
    [SerializeField] private bool rainEnabledAtStart = false;

    [Header("Rain Audio")]
    [SerializeField] private AudioSource rainAudioSource;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private AudioSource birdAudioSource;

    [Header("Lighting")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private float sunnyLightIntensity = 1.2f;
    [SerializeField] private float rainyLightIntensity = 0.35f;
    [SerializeField] private Color sunnyLightColor = Color.white;
    [SerializeField] private Color rainyLightColor = new Color(0.55f, 0.6f, 0.75f);

    [Header("Ambient Light")]
    [SerializeField] private Color sunnyAmbientColor = new Color(0.75f, 0.85f, 1f);
    [SerializeField] private Color rainyAmbientColor = new Color(0.25f, 0.3f, 0.38f);

    [Header("Fog")]
    [SerializeField] private bool useFogWhenRaining = true;
    [SerializeField] private Color rainyFogColor = new Color(0.38f, 0.42f, 0.48f);
    [SerializeField] private float rainyFogDensity = 0.015f;

    [Header("Transition")]
    [SerializeField] private float transitionSpeed = 2f;

    private bool isRaining;

    private float targetLightIntensity;
    private Color targetLightColor;
    private Color targetAmbientColor;
    private float targetFogDensity;
    private Color targetFogColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (directionalLight == null)
        {
            directionalLight = FindFirstObjectByType<Light>();
        }

        SetRain(rainEnabledAtStart, true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleRainKey))
        {
            ToggleRain();
        }

        UpdateEnvironmentTransition();
    }

    public void ToggleRain()
    {
        SetRain(!isRaining, false);
    }

    /*public void SetRain(bool enable)
    {
        //SetRain(enable, false);

        isRaining = enable;

        if (rainSystem != null)
        {
            rainSystem.SetActive(enable);
        }

        // AUDIO TRANSITIONS
        if (enable)
        {
            StartCoroutine(FadeOutBirds());
            StartCoroutine(FadeInRain());
        }
        else
        {
            StartCoroutine(FadeOutRain());
            StartCoroutine(FadeInBirds());
        }

        Debug.Log("Rain is now " + (isRaining ? "ON" : "OFF"));

    }*/

    private void SetRain(bool enable, bool instant)
    {
        isRaining = enable;

        if (rainSystem != null)
        {
            rainSystem.SetActive(enable);
        }

        if (rainAudioSource != null)
        {
            StopAllCoroutines();

            if (enable)
            {
                StartCoroutine(FadeOutBirds());
                StartCoroutine(FadeInRain());
            }
            else
            {
                StartCoroutine(FadeOutRain());
                StartCoroutine(FadeInBirds());
            }
        }

        if (isRaining)
        {
            targetLightIntensity = rainyLightIntensity;
            targetLightColor = rainyLightColor;
            targetAmbientColor = rainyAmbientColor;
            targetFogDensity = rainyFogDensity;
            targetFogColor = rainyFogColor;

            if (useFogWhenRaining)
            {
                RenderSettings.fog = true;
            }
        }
        else
        {
            targetLightIntensity = sunnyLightIntensity;
            targetLightColor = sunnyLightColor;
            targetAmbientColor = sunnyAmbientColor;
            targetFogDensity = 0f;
            targetFogColor = rainyFogColor;
        }

        if (instant)
        {
            ApplyEnvironmentInstant();
        }

        Debug.Log("Rain is now " + (isRaining ? "ON" : "OFF"));
    }

    private void UpdateEnvironmentTransition()
    {
        if (directionalLight != null)
        {
            directionalLight.intensity = Mathf.Lerp(
                directionalLight.intensity,
                targetLightIntensity,
                Time.deltaTime * transitionSpeed
            );

            directionalLight.color = Color.Lerp(
                directionalLight.color,
                targetLightColor,
                Time.deltaTime * transitionSpeed
            );
        }

        RenderSettings.ambientLight = Color.Lerp(
            RenderSettings.ambientLight,
            targetAmbientColor,
            Time.deltaTime * transitionSpeed
        );

        RenderSettings.fogDensity = Mathf.Lerp(
            RenderSettings.fogDensity,
            targetFogDensity,
            Time.deltaTime * transitionSpeed
        );

        RenderSettings.fogColor = Color.Lerp(
            RenderSettings.fogColor,
            targetFogColor,
            Time.deltaTime * transitionSpeed
        );

        if (!isRaining && RenderSettings.fogDensity < 0.001f)
        {
            RenderSettings.fog = false;
        }
    }

    private void ApplyEnvironmentInstant()
    {
        if (directionalLight != null)
        {
            directionalLight.intensity = targetLightIntensity;
            directionalLight.color = targetLightColor;
        }

        RenderSettings.ambientLight = targetAmbientColor;
        RenderSettings.fogDensity = targetFogDensity;
        RenderSettings.fogColor = targetFogColor;
        RenderSettings.fog = isRaining && useFogWhenRaining;
    }

    public bool IsRaining()
    {
        return isRaining;
    }

    private System.Collections.IEnumerator FadeInRain()
    {
        rainAudioSource.volume = 0f;

        if (!rainAudioSource.isPlaying)
        {
            rainAudioSource.Play();
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            rainAudioSource.volume = Mathf.Lerp(0f, 0.6f, timer / fadeDuration);
            yield return null;
        }

        rainAudioSource.volume = 0.6f;
    }

    private System.Collections.IEnumerator FadeOutRain()
    {
        float startVolume = rainAudioSource.volume;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            rainAudioSource.volume = Mathf.Lerp(startVolume, 0f, timer / fadeDuration);
            yield return null;
        }

        rainAudioSource.volume = 0f;
        rainAudioSource.Stop();
    }

    private System.Collections.IEnumerator FadeOutBirds()
    {
        float startVolume = birdAudioSource.volume;

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            birdAudioSource.volume =
                Mathf.Lerp(startVolume, 0f, timer / fadeDuration);

            yield return null;
        }

        birdAudioSource.volume = 0f;
        birdAudioSource.Stop();
    }

    private System.Collections.IEnumerator FadeInBirds()
    {
        float timer = 0f;

        birdAudioSource.volume = 0f;

        if (!birdAudioSource.isPlaying)
        {
            birdAudioSource.Play();
        }

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            birdAudioSource.volume =
                Mathf.Lerp(0f, 0.5f, timer / fadeDuration);

            yield return null;
        }

        birdAudioSource.volume = 0.5f;
    }
}