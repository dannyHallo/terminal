using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightEmitter : MonoBehaviour
{
    [Header("Emitter Basic Settings")]
    public GameObject emitter;
    public int emitterNum;
    public float emitterScale;


    [Header("Emitter Movement")]
    public float radius;
    public float height;
    [Range(0, 0.02f)] public float emitterRotatingSpeed = 0.01f;
    public float emitterFloatingSpeed = 0.3f;
    public float emitterFloatingBound = 2.0f;


    private float[] angles;
    private GameObject[] instancedEmitters;

    private void Start()
    {
        angles = new float[emitterNum];

        float a = 0;

        instancedEmitters = new GameObject[emitterNum];

        for (int i = 0; i < emitterNum; i++)
        {
            GameObject thisEmitter = GameObject.Instantiate(
                emitter,
                new Vector3(radius * Mathf.Cos(a * Mathf.PI * 2), height, radius * Mathf.Sin(a * Mathf.PI * 2)),
                Quaternion.identity);

            thisEmitter.transform.localScale = emitterScale * Vector3.one;
            angles[i] = a;

            thisEmitter.transform.parent = this.gameObject.transform;
            instancedEmitters[i] = thisEmitter;

            a += 1.0f / emitterNum;
        }
    }

    private void Update()
    {
        // Calc rot angle
        for (int i = 0; i < emitterNum; i++)
        {
            angles[i] += emitterRotatingSpeed * Time.deltaTime;
            angles[i] = angles[i] % 1.0f;
        }

        // Rot
        for (int i = 0; i < emitterNum; i++)
        {
            GameObject thisEmitter = instancedEmitters[i];
            float a = angles[i];

            thisEmitter.transform.localScale = emitterScale * Vector3.one;

            thisEmitter.transform.localPosition =
                new Vector3(
                    radius * Mathf.Cos(a * Mathf.PI * 2),
                    height + Mathf.Cos(a * Mathf.PI * 2 + Time.time * emitterFloatingSpeed * Mathf.PI * 2) * emitterFloatingBound,
                    radius * Mathf.Sin(a * Mathf.PI * 2));
        }


    }
}
