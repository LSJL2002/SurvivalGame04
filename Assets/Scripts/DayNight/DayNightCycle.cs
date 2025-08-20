using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Range(0.0f, 1.0f)]
    public float time;
    public float fullDayLength;
    public float startTime = 0.4f;
    private float timeRate;
    public Vector3 noon; // In this case its Vector 90,0,0

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
    public AnimationCurve reflectionIntesityMultiplier;

    // Day counter and night flag
    public int dayCount = 1;
    public bool isNight = false;

    void Start()
    {
        timeRate = 1.0f / fullDayLength;
        time = startTime;
    }

    void Update()
    {
        float previousTime = time;
        time = (time + timeRate * Time.deltaTime) % 1.0f;

        // Increment day counter when a new day starts
        if (previousTime > time)
        {
            dayCount++;
        }

        // Night detection (example: night is between 0.75 and 0.25)
        isNight = (time >= 0.75f || time < 0.25f);

        UpdateLighting(sun, sunColor, sunIntensity);
        UpdateLighting(moon, moonColor, moonIntensity);

        RenderSettings.ambientIntensity = lightingIntesityMultiplier.Evaluate(time);
    }

    void UpdateLighting(Light lightSource, Gradient gradient, AnimationCurve intensityCurve)
    {
        float intensity = intensityCurve.Evaluate(time);

        lightSource.transform.eulerAngles = (time - (lightSource == sun ? 0.25f : 0.75f)) * noon * 4f;
        lightSource.color = gradient.Evaluate(time);
        lightSource.intensity = intensity;

        GameObject go = lightSource.gameObject;
        if (lightSource.intensity == 0 && go.activeInHierarchy)
        {
            go.SetActive(false);
        }
        else if (lightSource.intensity > 0 && !go.activeInHierarchy)
        {
            go.SetActive(true);
        }
    }
}