using UnityEngine;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Rain")]
    [SerializeField] private GameObject rainSystem;
    [SerializeField] private KeyCode toggleRainKey = KeyCode.R;
    [SerializeField] private bool rainEnabledAtStart = false;

    private bool isRaining;

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
        SetRain(rainEnabledAtStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleRainKey))
        {
            ToggleRain();
        }
    }

    public void ToggleRain()
    {
        SetRain(!isRaining);
    }

    public void SetRain(bool enable)
    {
        isRaining = enable;

        if (rainSystem != null)
        {
            rainSystem.SetActive(enable);
        }

        Debug.Log("Rain is now " + (isRaining ? "ON" : "OFF"));
    }

    public bool IsRaining()
    {
        return isRaining;
    }
}
