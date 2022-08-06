using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera normalFollowingCam;
    public CinemachineVirtualCamera orbitalCam;
    public Transform cameraLookAtItem;

    public float cameraVerticalOffset;
    private bool settingsChanged = false;

    private CinemachineComposer normalFollowingCamComposer;
    private Cinemachine3rdPersonFollow normalFollowingCamFollower;
    private CinemachineOrbitalTransposer orbitalCamTransposer;

    [Space]

    [Header("Following Camera Properties")]
    public float camDstMin;
    public float camDstMax;
    [Range(0, 1)] public float mouseYMin;
    [Range(0, 1)] public float mouseYMax;


    [Header("Orbiting Camera Properties")]
    public float orbitingSpeed;


    [Header("Sensitivity")]
    [Range(0, 0.1f)] public float mouseYSensitivity = 0.03f;
    [Range(0.5f, 1.5f)] public float scrollingSensitivity = 1.0f;



    private bool isFollowingPlayer
    {
        get
        {
            return normalFollowingCam.Priority > orbitalCam.Priority ? true : false;
        }
    }

    private bool isOrbitingPlayer
    {
        get
        {
            return normalFollowingCam.Priority < orbitalCam.Priority ? true : false;
        }
    }

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        // Add camera brain to the main camera if it is not added
        Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out var brain);
        if (brain == null) Camera.main.gameObject.AddComponent<CinemachineBrain>();

        normalFollowingCamComposer = normalFollowingCam.GetCinemachineComponent<CinemachineComposer>();
        normalFollowingCamFollower = normalFollowingCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        orbitalCamTransposer = orbitalCam.GetCinemachineComponent<CinemachineOrbitalTransposer>();

        FollowPlayer();

        // Ask for a update
        settingsChanged = true;
    }

    private void Update()
    {
        // Do nothing when cursor is there
        if (Cursor.lockState == CursorLockMode.None)
            return;

        if (isFollowingPlayer)
        {
            HandleMouseYMovement();
            HandleMouseScrollMovement();
        }

        // Move cam automatically
        else if (isOrbitingPlayer)
        {
            orbitalCamTransposer.m_XAxis.Value += orbitingSpeed * Time.deltaTime;
            if (-100.0f < orbitalCamTransposer.m_XAxis.Value && orbitalCamTransposer.m_XAxis.Value < -80.0f)
            {
                FollowPlayer();
            }
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isFollowingPlayer) OrbitPlayer();
            else FollowPlayer();
        }
    }

    private void HandleMouseYMovement()
    {
        float mouseDelY = Input.GetAxis("Mouse Y");

        normalFollowingCamComposer.m_ScreenY += mouseDelY * mouseYSensitivity;
        normalFollowingCamComposer.m_ScreenY = Mathf.Clamp(normalFollowingCamComposer.m_ScreenY, mouseYMin, mouseYMax);
    }

    private void HandleMouseScrollMovement()
    {
        float mouseScrollDel = -Input.mouseScrollDelta.y;

        normalFollowingCamFollower.CameraDistance += mouseScrollDel * scrollingSensitivity;
        normalFollowingCamFollower.CameraDistance = Mathf.Clamp(normalFollowingCamFollower.CameraDistance, camDstMin, camDstMax);
    }

    public void OrbitPlayer()
    {
        normalFollowingCam.Priority = 15;
        orbitalCam.Priority = 20;

        orbitalCamTransposer.m_XAxis.Value = 80.0f;
    }

    public void FollowPlayer()
    {
        normalFollowingCam.Priority = 20;
        orbitalCam.Priority = 15;
    }
}
