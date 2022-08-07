using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class StageManagement : MonoBehaviour
{
    public CinemachineVirtualCamera followingCam;
    public CinemachineVirtualCamera orbitCam;
    public GameObject WorldEdge;
    public GameObject orginalPlane;
    public CameraControl _cameraControl;



    public void StageSwitch(int SwitchCount)
    {
        if (SwitchCount == 1)
        {

            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            StageSwitch(2);

        }
        if (SwitchCount == 2)
        {

        }
        if (SwitchCount == 3)
        {

        }
        if (SwitchCount == 4)
        {

        }
        if (SwitchCount == 5)
        {

        }
        if (SwitchCount == 6)
        {

        }
        if (SwitchCount == 7)
        {

        }
        if (SwitchCount == 8)
        {

        }
        ;
    }
}
