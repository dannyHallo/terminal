using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AtmosphereEffect {

	Light light;
	protected Material material;
	public void UpdateSettings (Shader atmosphereShader) {
		if (material == null || material.shader != atmosphereShader)
			material = new Material (atmosphereShader);

		if (light == null)
			light = GameObject.FindObjectOfType<SunShadowCaster> ()?.GetComponent<Light> ();
	
		if (light) {
			Vector3 dirFromPlanetToSun = light.transform.position.normalized;
			//Debug.Log(dirFromPlanetToSun);
			material.SetVector ("dirToSun", dirFromPlanetToSun);
		} else {
			material.SetVector ("dirToSun", Vector3.up);
			Debug.Log ("No SunShadowCaster found");
		}
	}

	public Material GetMaterial () {
		return material;
	}
}