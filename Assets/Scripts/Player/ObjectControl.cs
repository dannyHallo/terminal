using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ObjectControl : MonoBehaviour
{
    public AtmosphereSettings atmosphereSettings;

    [HideInInspector] public Vector3 planetCentre;

    float timeOfDay;
    float sunDistance;
    float sunSpeed;
    bool allowTimeFlow;
    GameObject sun;

    private void Awake()
    {
        sun = GameObject.Find("Test Sun");
        timeOfDay = atmosphereSettings.startTimeOfDay;
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            timeOfDay = atmosphereSettings.startTimeOfDay;
        }
        sunDistance = atmosphereSettings.sunDistance;
        sunSpeed = atmosphereSettings.sunSpeed;
        allowTimeFlow = atmosphereSettings.allowTimeFlow;

        Vector3 atmosOriginDueToPlayer = new Vector3(transform.position.x, 0, transform.position.z);
        Vector3 sunOriginDueToPlayer = transform.position;
        planetCentre = new Vector3(0, -atmosphereSettings.planetRadius, 0) + atmosOriginDueToPlayer;
        sun.transform.position = new Vector3(Mathf.Cos(timeOfDay * 2 * Mathf.PI), Mathf.Sin(timeOfDay * 2 * Mathf.PI), 0) * sunDistance + sunOriginDueToPlayer;
        sun.transform.LookAt(sunOriginDueToPlayer);
    }

    private void FixedUpdate()
    {
        if (Application.isPlaying && allowTimeFlow)
        {
            // Approximately 24 mins per round if the sun speed is 1
            timeOfDay += 0.0007f * Time.fixedDeltaTime * sunSpeed;
            if (timeOfDay >= 1)
            {
                timeOfDay = 0;
            }
        }
    }
}
