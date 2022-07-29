using System.Collections.Generic;
using UnityEngine;

public class LikeASatellite : MonoBehaviour
{
    public enum OrbitWay
    {
        // orginal,
        Yrotation,
        Zrotation,
        CustomRotation
    }
    public OrbitWay orbitWay;
    public float orbitHeight = 256.0f;
    [Range(0, 1)] public float orbitPos = 0.0f;
    public float orbitSpeed = 0.0001f;
    public GameObject OrbitCenter;
    private float Xrotation;
    private Vector3 cameraPosition;
    public void Start()
    {
        SetUpXRotation();

    }
    private void FixedUpdate()
    {
        //if (orbitWay == OrbitWay.orginal)
        //{
        //    if (orbitPos > 1.0f) orbitPos = 0;
        //    if (orbitPos < 0) orbitPos = 1.0f;
        //    orbitPos += orbitSpeed * Time.fixedDeltaTime;

        //    transform.position = orbitHeight * new Vector3(Mathf.Sin(orbitPos * 2 * Mathf.PI), 0, Mathf.Cos(orbitPos * 2 * Mathf.PI));
        //    transform.LookAt(new Vector3(0, 0, 0), Vector3.up);
        //}

        SetUpXRotation();
        //CameraSetUp();

        if (orbitWay == OrbitWay.Yrotation)
        {
            Transform centerRotation = OrbitCenter.transform;
            Vector3 centerRotationVector = OrbitCenter.transform.eulerAngles;
            centerRotationVector.y += orbitSpeed;
            centerRotation.eulerAngles = centerRotationVector;

        }
        if (orbitWay == OrbitWay.Zrotation)
        {
            Transform centerRotation = OrbitCenter.transform;
            Vector3 centerRotationVector = OrbitCenter.transform.eulerAngles;
            centerRotationVector.z += orbitSpeed;
            centerRotation.eulerAngles = centerRotationVector;

        }
        if (orbitWay == OrbitWay.CustomRotation)
        {


        }


        CameraSetUp();
    }

    public void SetUpXRotation()
    {
        Transform centerRotation = OrbitCenter.transform;
        Vector3 centerRotationVector = OrbitCenter.transform.eulerAngles;
        centerRotationVector.x = Xrotation;
        centerRotation.eulerAngles = centerRotationVector;
        Xrotation = centerRotationVector.x;
    }
    /// <summary>
    /// Setupheight
    /// </summary>
    public void CameraSetUp()
    {

        cameraPosition = new Vector3 (0,0,orbitHeight);
        transform.localPosition = cameraPosition;
    }
}