using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PointLightManager : MonoBehaviour
{
    [SerializeField] public Light[] pointLights;  // Array of point lights

    void Start()
    {
    }

    void Update()
    {
        pointLights = FindObjectsOfType<Light>().Where(light => light.type == LightType.Point).ToArray();
        //Debug.Log($"Number of point lights: {pointLights.Length}");
        Shader.SetGlobalInteger("_PointLightCount", pointLights.Length);
        if (pointLights.Length > 0) {
            Shader.SetGlobalVectorArray("_PointLightPosition", pointLights.Select(light => {
                Vector3 p = light.transform.position;
                return new Vector4(p.x, p.y, p.z, 0.0f);
            }).ToList());
            Shader.SetGlobalVectorArray("_PointLightColor", pointLights.Select(light => {
                Color p = light.color;
                float i = light.intensity;
                return new Vector4(p.r, p.g, p.b, 0.0f) * i;
            }).ToList());
        }
    }
}