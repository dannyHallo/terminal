using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode, ImageEffectAllowedInSceneView]
public class PostProcessingCore : MonoBehaviour
{

    public PostProcessingEffect[] effects;
    Shader defaultShader;
    Material defaultMat;
    List<RenderTexture> temporaryTextures = new List<RenderTexture>();  // Temp textures to release
    public bool debugOceanMask;

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

        temporaryTextures.Clear();

        RenderTexture currentSource = initSource;
        RenderTexture currentDest = null;

        if (effects != null)
        {
            for (int i = 0; i < effects.Length; i++)
            {
                PostProcessingEffect effect = effects[i];
                if (effect != null)
                {
                    // Final effect, so render into final destination texture
                    if (i == effects.Length - 1)
                    {
                        currentDest = finalDest;
                    }
                    // Get temporary texture to render this effect into
                    else
                    {
                        currentDest = TemporaryRenderTexture(finalDest);		// Create a blank RenderTexture with the same property of the last one
                        temporaryTextures.Add(currentDest);                     // Temp textures are going to be released
                    }

                    effect.Render(currentSource, currentDest);              	// render the effect
                    currentSource = currentDest;                                // output texture of this effect becomes input for next effect
                }
            }
        }else{
			print("The effect list is null!");
		}

        // Release temporary textures
        for (int i = 0; i < temporaryTextures.Count; i++)
        {
            RenderTexture.ReleaseTemporary(temporaryTextures[i]);
        }

        // Trigger post processing complete event
        if (onPostProcessingComplete != null)
        {
            onPostProcessingComplete(finalDest);
        }

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

    // Helper function for blitting single material
    public static void RenderMaterials(RenderTexture source, RenderTexture destination, Material material)
    {
        RenderTexture currentSource = source;
        RenderTexture currentDestination = null;

        currentDestination = destination;
        Graphics.Blit(currentSource, currentDestination, material);
    }

    public static RenderTexture TemporaryRenderTexture(RenderTexture template)
    {
        return RenderTexture.GetTemporary(template.descriptor);
    }
}