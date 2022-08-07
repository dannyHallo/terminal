using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FinalRenderingChannel : ColorChannel
{
    public GameObject gameObj;
    public ParticleSystem _particleSystem;
    private Renderer gameObjectRenderer;
    private ParticleSystem.MainModule mainModule;
    private void Start()
    {
        gameObj = this.gameObject;
        gameObjectRenderer = gameObj.GetComponent<Renderer>();
        _particleSystem = gameObj.GetComponent<ParticleSystem>();
        if (_particleSystem)
            mainModule = _particleSystem.main;
    }
    private void Update()
    {
        if (gameObjectRenderer)
            gameObjectRenderer.material.SetColor("_color", mainInputColor);
        if (_particleSystem)
            mainModule.startColor = mainInputColor;
    }
}