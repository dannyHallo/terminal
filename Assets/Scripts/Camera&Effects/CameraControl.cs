using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera normalFollowingCam;
    public CinemachineVirtualCamera orbitalCam;

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
    private float _Countdown;
    private bool _cameraShakeBool;


    public bool isFollowingPlayer
    {
        get
        {
            return normalFollowingCam.Priority > orbitalCam.Priority ? true : false;
        }
    }

    public bool isOrbitingPlayer
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
        normalFollowingCam.TryGetComponent<CinemachineBasicMultiChannelPerlin>(out var perlin);
        if (!perlin)
        {
            normalFollowingCam.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        }
        normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
        normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 1;
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

        if (Input.GetKey(KeyCode.T))
        {
            if (isFollowingPlayer) CameraShakeStop();
        }

        //Can be transform into Shake function
        if (_cameraShakeBool)
        {
            _Countdown -= Time.deltaTime;
            if (_Countdown > 2f)
            {
                normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain += 1f * Time.deltaTime;
            }
            else
            {
                normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain -= 2.5f * Time.deltaTime;
                if (normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain <= 0)
                {
                    normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
                    _cameraShakeBool = false;
                }
            }


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
    public void CameraShake(float time)
    {

        _cameraShakeBool = true;
        _Countdown = time - 2;
        //normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 1;
    }

    public void CameraShakeStop()
    {
        normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain -= 2.5f * Time.deltaTime;
        // normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 1;
    }



}
