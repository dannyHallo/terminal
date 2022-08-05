using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightEmitter : MonoBehaviour
{
    public GameObject emitter;
    public int emitterNum;
    public float radius;
    public float height;

    private GameObject[] instancedEmitters;

    private void Start()
    {
        float a = 0;

        instancedEmitters = new GameObject[emitterNum];

        for (int i = 0; i < emitterNum; i++)
        {
            GameObject thisEmitter = GameObject.Instantiate(
                emitter,
                new Vector3(radius * Mathf.Cos(a * Mathf.PI * 2), height, radius * Mathf.Sin(a * Mathf.PI * 2)),
                Quaternion.identity);

            thisEmitter.transform.parent = this.gameObject.transform;
            instancedEmitters[i] = thisEmitter;

            a += 1.0f / emitterNum;
        }
    }

    private void Update()
    {

    }
}
