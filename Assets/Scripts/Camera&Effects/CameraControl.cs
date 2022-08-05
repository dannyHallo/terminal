using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[ExecuteInEditMode]
public class CameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    public Transform cameraLookAtItem;

    public float cameraVerticalOffset;
    private bool settingsChanged = false;

    private void Start()
    {
        // Add camera brain to the main camera if it is not added
        Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out var brain);
        if (brain == null) Camera.main.gameObject.AddComponent<CinemachineBrain>();

        cinemachineVirtualCamera.Priority = 20;

        // Ask for a update
        settingsChanged = true;
    }

    private void OnValidate()
    {
        settingsChanged = true;
    }

    private void Update()
    {
        if (settingsChanged)
        {
            ChangeCameraVerticalOffset(cameraVerticalOffset);
            settingsChanged = false;
        }
    }

    private void ChangeCameraVerticalOffset(in float height)
    {
        Vector3 position = cameraLookAtItem.localPosition;
        position.y = height;
        cameraLookAtItem.localPosition = position;
    }
}
