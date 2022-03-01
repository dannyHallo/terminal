using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PostProcessing/PlanetEffects")]
public class PlanetEffects : PostProcessingEffect
{
    public Shader atmosphereShader;
    public AtmosphereSettings atmosphereSettings;
    EffectHolder effectHolder;
    List<float> sortDistances;
    List<Material> postProcessingMaterials;

    public override void Render(RenderTexture source, RenderTexture destination)
    {
        PostProcessingCore.RenderMaterials(source, destination, GetMaterial());
    }

    public Material GetMaterial()
    {
        if (effectHolder == null)
            effectHolder = new EffectHolder();

        if (material == null)
            material = new Material(atmosphereShader);

        // Set properties of the atmosphere material
        effectHolder.atmosphereEffect.UpdateSettings(atmosphereShader);
        material = effectHolder.atmosphereEffect.GetMaterial();
        atmosphereSettings.SetProperties(material);

        return material;
    }

    public class EffectHolder
    {
        public AtmosphereEffect atmosphereEffect;

        public EffectHolder()
        {
            atmosphereEffect = new AtmosphereEffect();
        }
    }
}