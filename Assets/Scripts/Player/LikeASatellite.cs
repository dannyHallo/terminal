using System.Collections.Generic;
using UnityEngine;

public class LikeASatellite : MonoBehaviour
{
    public enum OrbitWay
    {
        Orginal,
        Yrotation,
        Zrotation,
        CustomRotation
    }
    public OrbitWay orbitWay;
    public float orbitHeight = 256.0f;

    [Range(0, 1)] public float orbitPos = 0.0f;

    public float orbitSpeed = 0.0001f;
    // private GameObject OrbitCenter;
    private float Xrotation;
    private Vector3 cameraPosition;

    private void Start()
    {
        // OrbitCenter = transform.parent.gameObject;
    }

    private void FixedUpdate()
    {
        if (orbitWay == OrbitWay.Orginal)
        {
            if (orbitPos > 1.0f) orbitPos = 0;
            if (orbitPos < 0) orbitPos = 1.0f;
            orbitPos += orbitSpeed * Time.fixedDeltaTime;

            transform.position = orbitHeight *
                new Vector3(Mathf.Sin(orbitPos * 2 * Mathf.PI),
                            0,
                            Mathf.Cos(orbitPos * 2 * Mathf.PI));
            transform.LookAt(new Vector3(0, 0, 0), Vector3.up);
        }

        // SetUpXRotation();

        // //CameraSetUp();
        // Transform centerRotation = OrbitCenter.transform;
        // Vector3 centerRotationVector = OrbitCenter.transform.eulerAngles;

        // if (orbitWay == OrbitWay.Yrotation)
        //     centerRotationVector.y += orbitSpeed;

        // if (orbitWay == OrbitWay.Zrotation)
        //     centerRotationVector.z += orbitSpeed;

        // if (orbitWay == OrbitWay.CustomRotation)
        // {

        // }

        // centerRotation.eulerAngles = centerRotationVector;


        // CameraSetUp();
    }

    // public void SetUpXRotation()
    // {
    //     Vector3 centerRotationVector = OrbitCenter.transform.eulerAngles;
    //     centerRotationVector.x = Xrotation;
    //     OrbitCenter.transform.eulerAngles = centerRotationVector;
    //     Xrotation = centerRotationVector.x;
    // }

    /// <summary>
    /// Setup height
    /// </summary>
    public void CameraSetUp()
    {
        cameraPosition = new Vector3(0, 0, orbitHeight);
        transform.localPosition = cameraPosition;
    }
}