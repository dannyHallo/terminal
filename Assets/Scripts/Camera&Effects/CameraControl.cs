using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraControl : MonoBehaviour
{
    public CinemachineVirtualCamera thirdPersonCam;
    public CinemachineVirtualCamera firstPersonCam;
    public CinemachineVirtualCamera orbitalCam;

    private CinemachineComposer thirdPersonCamComposer;
    private CinemachineComposer firstPersonCamComposer;
    private Cinemachine3rdPersonFollow thirdPersonCamFollower;
    private CinemachineOrbitalTransposer orbitalCamTransposer;

    [Space]

    [Header("Following Camera Properties")]
    public float camDstMin;
    public float camDstMax;
    [Range(-0.5f, 1.5f)] public float mouseYMin;
    [Range(-0.5f, 1.5f)] public float mouseYMax;


    [Header("Orbiting Camera Properties")]
    public float orbitingSpeed;


    [Header("Sensitivity")]
    [Range(0, 0.1f)] public float mouseYSensitivity = 0.03f;
    [Range(0.5f, 1.5f)] public float scrollingSensitivity = 1.0f;
    private float _Countdown;
    private bool _cameraShakeBool;


    public bool isFollowingPlayerThirdPerson
    {
        get
        {
            return thirdPersonCam.Priority > Mathf.Max(orbitalCam.Priority, firstPersonCam.Priority) ? true : false;
        }
    }

    public bool isFollowingPlayerFirstPerson
    {
        get
        {
            return firstPersonCam.Priority > Mathf.Max(orbitalCam.Priority, thirdPersonCam.Priority) ? true : false;
        }
    }

    public bool isOrbitingPlayer
    {
        get
        {
            return orbitalCam.Priority > Mathf.Max(thirdPersonCam.Priority, firstPersonCam.Priority) ? true : false;
        }
    }



    private void Awake()
    {
        // Add camera brain to the main camera if it is not added
        Camera.main.gameObject.TryGetComponent<CinemachineBrain>(out var brain);
        if (brain == null) Camera.main.gameObject.AddComponent<CinemachineBrain>();

        thirdPersonCamComposer = thirdPersonCam.GetCinemachineComponent<CinemachineComposer>();
        thirdPersonCamFollower = thirdPersonCam.GetCinemachineComponent<Cinemachine3rdPersonFollow>();
        firstPersonCamComposer = firstPersonCam.GetCinemachineComponent<CinemachineComposer>();
        orbitalCamTransposer = orbitalCam.GetCinemachineComponent<CinemachineOrbitalTransposer>();

        FollowPlayerThirdPerson();
    }

    private void Update()
    {
        // Do nothing when cursor is there
        if (Cursor.lockState == CursorLockMode.None)
            return;

        HandleMouseScrollMovement();

        if (!isOrbitingPlayer) HandleMouseYMovement();
        else
        {
            orbitalCamTransposer.m_XAxis.Value += orbitingSpeed * Time.deltaTime;
            if (-100.0f < orbitalCamTransposer.m_XAxis.Value && orbitalCamTransposer.m_XAxis.Value < -80.0f)
                FollowPlayerThirdPerson();
        }

        //Can be transform into Shake function
        if (_cameraShakeBool)
        {
            _Countdown -= Time.deltaTime;
            if (_Countdown > 2f)
            {
                if (thirdPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain <= 1.5f)
                {

                    thirdPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain += 1f * Time.deltaTime;
                }
            }
            else
            {
                Debug.Log("!!");
                thirdPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain -= 2.5f * Time.deltaTime;
                if (thirdPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain <= 0)
                {
                    thirdPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain = 0;
                    _cameraShakeBool = false;
                }
            }
        }
    }

    private void HandleMouseYMovement()
    {
        float mouseDelY = Input.GetAxis("Mouse Y");

        if (isFollowingPlayerThirdPerson)
        {
            thirdPersonCamComposer.m_ScreenY += mouseDelY * mouseYSensitivity;
            thirdPersonCamComposer.m_ScreenY = Mathf.Clamp(thirdPersonCamComposer.m_ScreenY, mouseYMin, mouseYMax);
        }

        if (isFollowingPlayerFirstPerson)
        {
            firstPersonCamComposer.m_ScreenY += mouseDelY * mouseYSensitivity;
            firstPersonCamComposer.m_ScreenY = Mathf.Clamp(firstPersonCamComposer.m_ScreenY, mouseYMin, mouseYMax);
        }

    }

    private void HandleMouseScrollMovement()
    {
        float mouseScrollDel = -Input.mouseScrollDelta.y;

        if (isFollowingPlayerThirdPerson)
        {
            thirdPersonCamFollower.CameraDistance += mouseScrollDel * scrollingSensitivity;
            thirdPersonCamFollower.CameraDistance = Mathf.Clamp(thirdPersonCamFollower.CameraDistance, 0, camDstMax);

            if (thirdPersonCamFollower.CameraDistance < camDstMin)
            {
                FollowPlayerFirstPerson();
                thirdPersonCamFollower.CameraDistance = camDstMin;
            }
        }

        if (isFollowingPlayerFirstPerson)
        {
            if (mouseScrollDel > 0) FollowPlayerThirdPerson();
        }
    }

    public void OrbitPlayer()
    {
        thirdPersonCam.Priority = 15;
        firstPersonCam.Priority = 15;
        orbitalCam.Priority = 20;

        orbitalCamTransposer.m_XAxis.Value = 80.0f;
    }

    public void FollowPlayerThirdPerson()
    {
        thirdPersonCam.Priority = 20;
        firstPersonCam.Priority = 15;
        orbitalCam.Priority = 15;
    }

    public void FollowPlayerFirstPerson()
    {
        thirdPersonCam.Priority = 15;
        firstPersonCam.Priority = 20;
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
        thirdPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain -= 2.5f * Time.deltaTime;
        // normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_FrequencyGain = 1;
    }



}
