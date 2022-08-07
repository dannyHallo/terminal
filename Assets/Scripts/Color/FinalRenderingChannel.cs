using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalRenderingChannel : ColorChannel
{
    public GameObject gameObj;
    public ParticleSystem particleSystem;
    private Renderer gameObjectRenderer;
    private ParticleSystem.MainModule mainModule;
    private void Start()
    {
        gameObj = this.gameObject;
        gameObjectRenderer = gameObj.GetComponent<Renderer>();
        particleSystem = gameObj.GetComponent<ParticleSystem>();
        if (particleSystem)
            mainModule = particleSystem.main;
    }
    private void Update()
    {
        if (gameObjectRenderer)
            gameObjectRenderer.material.color = mainInputColor;
        if (particleSystem)
            mainModule.startColor = mainInputColor;
    }
}