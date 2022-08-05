using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera cinemachineVirtualCamera;
    public Transform cameraLookAtItem;

    public float cameraVerticalOffset;
    private bool settingsChanged = false;
    private CinemachineComposer composer;
    private Cinemachine3rdPersonFollow follower;

    [Space]

    [Header("Scrolling")]
    public float camDstMin;
    public float camDstMax;
    public float camDstSpeed = 1.0f;


    [Header("Mouse Y")]
    [Range(0, 1)] public float mouseYMin;
    [Range(0, 1)] public float mouseYMax;
    [Range(0, 0.1f)] public float mouseYSpeed = 0.03f;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        // Add camera brain to the main camera if it is not added
        Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out var brain);
        if (brain == null) Camera.main.gameObject.AddComponent<CinemachineBrain>();

        composer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineComposer>();
        follower = cinemachineVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        cinemachineVirtualCamera.Priority = 20;

        // Ask for a update
        settingsChanged = true;
    }

    private void Update()
    {
        HandleMouseYMovement();
        HandleMouseScrollMovement();
    }

    private void HandleMouseYMovement()
    {
        float mouseDelY = Input.GetAxis("Mouse Y");

        composer.m_ScreenY += mouseDelY * mouseYSpeed;
        composer.m_ScreenY = Mathf.Clamp(composer.m_ScreenY, mouseYMin, mouseYMax);
    }

    private void HandleMouseScrollMovement()
    {
        float mouseScrollDel = -Input.mouseScrollDelta.y;

        follower.CameraDistance += mouseScrollDel * camDstSpeed;
        follower.CameraDistance = Mathf.Clamp(follower.CameraDistance, camDstMin, camDstMax);
    }
}
