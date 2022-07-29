using System.Collections.Generic;
using UnityEngine;

public class LikeASatellite : MonoBehaviour
{
    public float orbitHeight = 256.0f;
    [Range(0, 1)] public float orbitPos = 0.0f;
    public float orbitSpeed = 0.0001f;

    private void FixedUpdate()
    {
        if (orbitPos > 1.0f) orbitPos = 0;
        if (orbitPos < 0) orbitPos = 1.0f;
        orbitPos += orbitSpeed * Time.fixedDeltaTime;

        transform.position = orbitHeight * new Vector3(Mathf.Sin(orbitPos * 2 * Mathf.PI), 0, Mathf.Cos(orbitPos * 2 * Mathf.PI));
        transform.LookAt(new Vector3(0, 0, 0), Vector3.up);
    }
}