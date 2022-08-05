using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class PostProcessingCore : MonoBehaviour
{
    Material defaultMat;
    public PostProcessingEffect[] effects;
    List<RenderTexture> temporaryTextures = new List<RenderTexture>();  // Temp textures to release

    public event System.Action<RenderTexture> onPostProcessingComplete;
    public event System.Action<RenderTexture> onPostProcessingBegin;

    // Before rendering transparent stuff
    [ImageEffectOpaque]
    void OnRenderImage(RenderTexture initSource, RenderTexture finalDest)
    {
        if (onPostProcessingBegin != null)
        {
            onPostProcessingBegin(finalDest);
        }

        if (!defaultMat)
        {
            defaultMat = new Material(Shader.Find("Unlit/Texture"));
        }

        temporaryTextures.Clear();

        RenderTexture currentSource = initSource;
        RenderTexture currentDest = null;

        if (effects != null && effects.Length != 0)
        {
            foreach (var effect in effects)
            {
                // Final effect is rendered into final destination texture
                if (effect == effects[effects.Length - 1])
                {
                    currentDest = finalDest;
                }

                // Get temporary texture to render this effect into
                else
                {
                    currentDest = TemporaryRenderTexture(finalDest);        // Create a blank RenderTexture with the same property of the last one
                    temporaryTextures.Add(currentDest);                     // Temp textures are going to be released
                }

                effect.Render(currentSource, currentDest);                  // render the effect
                currentSource = currentDest;                                // output texture of this effect becomes input for next effect
            }
        }
        else
        {
            RenderMaterials(initSource, finalDest, defaultMat);
            // return;
        }

        // Release temporary textures
        foreach (var texture in temporaryTextures)
        {
            RenderTexture.ReleaseTemporary(texture);
        }

        // Trigger post processing complete event
        if (onPostProcessingComplete != null)
        {
            onPostProcessingComplete(finalDest);
        }

    }

    // Helper function for blitting single material
    public static void RenderMaterials(RenderTexture source, RenderTexture destination, Material material)
    {
        RenderTexture currentSource = source;
        RenderTexture currentDestination = null;

        currentDestination = destination;
        Graphics.Blit(currentSource, currentDestination, material);
    }

    // Helper function for blitting a list of materials
    public static void RenderMaterials(RenderTexture source, RenderTexture destination, List<Material> materials)
    {
        List<RenderTexture> temporaryTextures = new List<RenderTexture>();

        RenderTexture currentSource = source;
        RenderTexture currentDestination = null;

        if (materials != null)
        {
            for (int i = 0; i < materials.Count; i++)
            {
                Material material = materials[i];
                if (material != null)
                {

                    if (i == materials.Count - 1)
                    { // last material
                        currentDestination = destination;
                    }
                    else
                    {
                        // get temporary texture to render this effect into
                        currentDestination = TemporaryRenderTexture(destination);
                        temporaryTextures.Add(currentDestination);
                    }
                    Graphics.Blit(currentSource, currentDestination, material);
                    currentSource = currentDestination;
                }
            }
        }

        // Release temporary textures
        for (int i = 0; i < temporaryTextures.Count; i++)
        {
            RenderTexture.ReleaseTemporary(temporaryTextures[i]);
        }
    }



    public static RenderTexture TemporaryRenderTexture(RenderTexture template)
    {
        return RenderTexture.GetTemporary(template.descriptor);
    }
}