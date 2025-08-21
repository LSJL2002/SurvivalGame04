using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Range(0f, 1f)] public float time;
    public float fullDayLength = 300f;
    public float startTime = 0.4f;
    private float timeRate;
    public Vector3 noon = new Vector3(90, 0, 0);

    [Header("Sun")]
    public Light sun;
    public Gradient sunColor;
    public AnimationCurve sunIntensity;

    [Header("Moon")]
    public Light moon;
    public Gradient moonColor;
    public AnimationCurve moonIntensity;

    [Header("Other Lighting")]
    public AnimationCurve lightingIntesityMultiplier;

    // ✅ 밤 판정 파라미터
    [Header("Night Detection")]
    [Range(0f, 1f)] public float nightStart = 0.75f;   // 이 시간부터 밤 시작
    [Range(0f, 1f)] public float nightEnd = 0.25f;   // 이 시간까지 밤
    [Range(0f, 5f)] public float sunIntensityNightThreshold = 0.05f; // 태양 밝기 기준(보조)

    public int dayCount = 1;
    public bool isNight = false;

    void Start()
    {
        timeRate = 1f / Mathf.Max(1f, fullDayLength);
        time = Mathf.Clamp01(startTime);
    }

    void Update()
    {
        float prev = time;
        time = (time + timeRate * Time.deltaTime) % 1f;
        if (prev > time) dayCount++;

        UpdateLighting(sun, sunColor, sunIntensity, 0.25f);
        UpdateLighting(moon, moonColor, moonIntensity, 0.75f);

        RenderSettings.ambientIntensity = lightingIntesityMultiplier != null
            ? lightingIntesityMultiplier.Evaluate(time) : 1f;

        // ✅ 밤 판정: 시간 구간 + 태양 밝기 둘 다 사용
        bool nightByTime = (time >= nightStart || time < nightEnd);
        bool nightBySun = sun != null && sun.intensity <= sunIntensityNightThreshold;
        bool newIsNight = nightByTime || nightBySun;

        if (newIsNight != isNight)
        {
            isNight = newIsNight;
            Debug.Log($"[DayNight] isNight -> {isNight}  (time={time:0.000}, sunI={sun?.intensity:0.00})");
        }
    }

    void UpdateLighting(Light lightSource, Gradient gradient, AnimationCurve intensityCurve, float phase)
    {
        if (!lightSource) return;

        float t = time;
        float intensity = intensityCurve != null ? intensityCurve.Evaluate(t) : 1f;

        lightSource.transform.eulerAngles = (t - phase) * noon * 4f;
        if (gradient != null) lightSource.color = gradient.Evaluate(t);
        lightSource.intensity = intensity;

        // 강/약에 따라 GameObject on/off
        GameObject go = lightSource.gameObject;
        if (intensity <= 0f && go.activeSelf) go.SetActive(false);
        else if (intensity > 0f && !go.activeSelf) go.SetActive(true);
    }
}
