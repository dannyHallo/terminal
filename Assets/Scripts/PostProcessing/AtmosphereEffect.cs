using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereEffect
{

    Light light;
    GameObject player;
    protected Material material;

    public void UpdateSettings(Shader atmosphereShader)
    {
        if (material == null || material.shader != atmosphereShader)
            material = new Material(atmosphereShader);

        if (light == null)
            light = GameObject.FindObjectOfType<SunShadowCaster>()?.GetComponent<Light>();

        if (player == null)
            player = GameObject.FindObjectOfType<PlayerMovement>()?.gameObject;

        if (light)
        {
            Vector3 dirFromPlanetToSun = (light.transform.position - player.transform.position).normalized;
            material.SetVector("dirToSun", dirFromPlanetToSun);
        }
        else
        {
            material.SetVector("dirToSun", Vector3.up);
            Debug.Log("No SunShadowCaster found");
        }
    }

    public Material GetMaterial()
    {
        return material;
    }
}