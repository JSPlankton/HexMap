using System;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class TimeOfDay : MonoBehaviour
{
    public Material SkyboxMaterial = null;

    private Transform SunTransform = null;
        
    private Transform MoonTransform = null;

    public Transform DirectionalLight = null;

    [Header("Control")]
    public bool UseTimeline = true;
    [Range(0.0f, 24.0f)]
    public float Timeline = 6.0f;
    public float TimelineSpeed = 1f;
    public float Latitude = 0;
    public float Longitude = 0;
    [DisplayOnly]
    [SerializeField]
    private float m_sunElevation = 0f;
    [DisplayOnly]
    [SerializeField]
    private float m_sunElevationPrecent = 0f;
    private float m_moonElevation = 0f;
    private Vector3 m_sunLocalDirection;
    private Vector3 m_moonLocalDirection;
    private Vector3 m_lightLocalDirection;

    [Header("SkyColor")]
    [GradientUsage(true,ColorSpace.Linear)]
    public Gradient HorizonColor = new Gradient();
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient ZenithColor = new Gradient();
    public AnimationCurve HorizonFalloff = AnimationCurve.Linear(-1.0f, 1.0f, 1f, 1.0f);
    public float HorizonTilt = 0.2f;
    public float HorizonOffset = -0.2f;

    [Header("SunMoonColor")]
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient SunColor = new Gradient();
    public AnimationCurve SunIntensity = AnimationCurve.Linear(-1.0f, 0.5f, 1f, 0.5f);
    public float SunRadius = 0.2f;
    public float SunBloom = 4.0f;
    public float SunScattering = 0.3f;
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient MoonColor = new Gradient();
    public AnimationCurve MoonIntensity = AnimationCurve.Linear(-1.0f, 0.5f, 1f, 0.5f);
    [Range(0.1f,1.0f)]
    public float MoonSize = 0.5f;
    public float MoonBloom = 10.0f;
    public float MoonScattering = 0.5f;

    [Header("Static Background")]
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient StarsColor = new Gradient();
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient CloudsBackgroundColor = new Gradient();
    public AnimationCurve CloudsBackgroundBrightness = AnimationCurve.Linear(-1.0f, 0.5f, 1f, 0.5f);

    [Header("Dynamic Cloud")]
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient CloudsBrightColor = new Gradient();
    [GradientUsage(true, ColorSpace.Linear)]
    public Gradient CloudsDarkColor = new Gradient();

    private void Awake()
    {
        if (DirectionalLight == null || SkyboxMaterial == null)
            return;
        if (SunTransform == null)
        {
            GameObject sunGo = new GameObject("SunTransform");
            sunGo.hideFlags = HideFlags.HideAndDontSave;
            SunTransform = sunGo.transform;
        }
        if (MoonTransform == null)
        {
            GameObject moonGo = new GameObject("MoonTransform");
            moonGo.hideFlags = HideFlags.HideAndDontSave;
            MoonTransform = moonGo.transform;
        }

        ComputeSunMoonLoxation();
        EvaluateSunMoonElevation();
        SetDirectionalLightRotation();
    }

    private void Update()
    {
        if (DirectionalLight == null || SkyboxMaterial == null)
            return;
        if (SunTransform == null)
        {
            GameObject sunGo = new GameObject("SunTransform");
            sunGo.hideFlags = HideFlags.HideAndDontSave;
            SunTransform = sunGo.transform;
        }
        if (MoonTransform == null)
        {
            GameObject moonGo = new GameObject("MoonTransform");
            moonGo.hideFlags = HideFlags.HideAndDontSave;
            MoonTransform = moonGo.transform;
        }
        // Only in gameplay
        if (Application.isPlaying)
        {
            Timeline += TimelineSpeed * Time.deltaTime;

            // Change to the next day
            if (Timeline > 24)
            {
                Timeline = 0;
            }
            EvaluateSunMoonElevation();
        }

        // Editor only
        // Computes the celestial coordinates and light rotation in edit mode.
        #if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            ComputeSunMoonLoxation();
            EvaluateSunMoonElevation();
            SetDirectionalLightRotation();
        }
        #endif

        SetSkyboxMaterial();
    }

    private void FixedUpdate()
    {
        ComputeSunMoonLoxation();
        SetDirectionalLightRotation();
    }

    private void ComputeSunMoonLoxation()
    {
        if (UseTimeline)
        {
            SunTransform.localRotation = Quaternion.Euler(0.0f, Longitude, -Latitude) * Quaternion.Euler((Timeline * 360.0f / 24.0f) - 90.0f, 180.0f, 0.0f);
            MoonTransform.localRotation = SunTransform.localRotation * Quaternion.Euler(0, -180, 0);
        }
        else
        {
            SunTransform.localRotation = DirectionalLight.localRotation;
            MoonTransform.localRotation = SunTransform.localRotation * Quaternion.Euler(0, -180, 0);
        }
    }

    private void EvaluateSunMoonElevation()
    {
        m_sunLocalDirection = SunTransform.forward;
        m_moonLocalDirection = MoonTransform.forward;
        m_sunElevation = Vector3.Dot(-m_sunLocalDirection, Vector3.up);
        m_sunElevationPrecent = Mathf.Max(0.0f,m_sunElevation * 0.5f + 0.5f);
        m_moonElevation = Vector3.Dot(-m_moonLocalDirection, Vector3.up);
    }
    private void SetDirectionalLightRotation()
    {
        if (UseTimeline)
        {
            DirectionalLight.localRotation = Quaternion.LookRotation(m_sunElevation >= 0.0f ? m_sunLocalDirection : m_moonLocalDirection);
            float MinLightAltitude = 10.0f;
            // Avoid the directional light to get close to the horizon line
            m_lightLocalDirection = DirectionalLight.localEulerAngles;
            if (m_lightLocalDirection.x <= MinLightAltitude) m_lightLocalDirection.x = MinLightAltitude;
            DirectionalLight.localEulerAngles = m_lightLocalDirection;
        }
    }
    private void SetSkyboxMaterial()
    {
        if (SkyboxMaterial != null)
        {
            SkyboxMaterial.SetVector("_SunDirection", m_sunLocalDirection);
            SkyboxMaterial.SetVector("_MoonDirection", m_moonLocalDirection);
            SkyboxMaterial.SetColor("_HorizonColor", HorizonColor.Evaluate(m_sunElevationPrecent));
            SkyboxMaterial.SetColor("_ZenithColor", ZenithColor.Evaluate(m_sunElevationPrecent));
            SkyboxMaterial.SetFloat("_HorizonFalloff", HorizonFalloff.Evaluate(m_sunElevation));
            SkyboxMaterial.SetFloat("_HorizonTilt",HorizonTilt);
            SkyboxMaterial.SetFloat("_HorizonOffset", HorizonOffset);

            SkyboxMaterial.SetColor("_SunColor", SunColor.Evaluate(m_sunElevationPrecent));
            SkyboxMaterial.SetFloat("_SunIntensity", SunIntensity.Evaluate(m_sunElevation));
            SkyboxMaterial.SetFloat("_SunRadius", SunRadius);
            SkyboxMaterial.SetFloat("_SunBloom", SunBloom);
            SkyboxMaterial.SetFloat("_SunScattering", SunScattering);

            SkyboxMaterial.SetColor("_MoonColor", MoonColor.Evaluate(m_sunElevationPrecent));
            SkyboxMaterial.SetFloat("_MoonIntensity", MoonIntensity.Evaluate(m_sunElevation));
            SkyboxMaterial.SetFloat("_MoonSize", MoonSize);
            SkyboxMaterial.SetFloat("_MoonBloom", MoonBloom);
            SkyboxMaterial.SetFloat("_MoonScattering", MoonScattering);

            SkyboxMaterial.SetColor("_StarsColor", StarsColor.Evaluate(m_sunElevationPrecent));
            SkyboxMaterial.SetColor("_CloudsBackgroundColor", CloudsBackgroundColor.Evaluate(m_sunElevationPrecent));
            SkyboxMaterial.SetFloat("_CloudsBackgroundBrightness", CloudsBackgroundBrightness.Evaluate(m_sunElevation));
            //再传一次参数给Cloud
            Shader.SetGlobalVector("_SunDirection", m_sunLocalDirection);
            Shader.SetGlobalVector("_MoonDirection", m_moonLocalDirection);
            Shader.SetGlobalColor("_HorizonColor", HorizonColor.Evaluate(m_sunElevationPrecent).linear);//该方法有bug,需要手动转Linear
            Shader.SetGlobalColor("_ZenithColor", ZenithColor.Evaluate(m_sunElevationPrecent).linear);
            Shader.SetGlobalFloat("_HorizonFalloff", HorizonFalloff.Evaluate(m_sunElevation));
            Shader.SetGlobalFloat("_HorizonTilt", HorizonTilt);
            Shader.SetGlobalFloat("_HorizonOffset", HorizonOffset);
            Shader.SetGlobalColor("_SunColor", SunColor.Evaluate(m_sunElevationPrecent).linear);
            Shader.SetGlobalFloat("_SunIntensity", SunIntensity.Evaluate(m_sunElevation));
            Shader.SetGlobalFloat("_SunScattering", SunScattering);
            Shader.SetGlobalColor("_MoonColor", MoonColor.Evaluate(m_sunElevationPrecent).linear);
            Shader.SetGlobalFloat("_MoonIntensity", MoonIntensity.Evaluate(m_sunElevation));
            Shader.SetGlobalFloat("_MoonScattering", MoonScattering);
            Shader.SetGlobalColor("_CloudsBrightColor", CloudsBrightColor.Evaluate(m_sunElevationPrecent).linear);
            Shader.SetGlobalColor("_CloudsDarkColor", CloudsDarkColor.Evaluate(m_sunElevationPrecent).linear);
        }
    
    }
}

    public class DisplayOnly : PropertyAttribute
    {

    }
    [CustomPropertyDrawer(typeof(DisplayOnly))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
