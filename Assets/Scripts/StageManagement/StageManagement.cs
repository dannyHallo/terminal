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
    public int stageInt;
    public float _countDown;





    private void Start()
    {
        _cameraControl = FindObjectOfType<CameraControl>();
        orginalPlane.transform.position = new Vector3(0,20,0);
    }



    public void StageSwitch (int SwitchCount)
    {
        if (SwitchCount == 1)
        {
            
            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = 10f;
            stageInt = SwitchCount;
        }
        if (SwitchCount == 2)
        {
            _cameraControl.CameraShake(20);
            stageInt = SwitchCount;
        }
        if (SwitchCount == 3)
        {
            stageInt = SwitchCount;
        }
        if (SwitchCount == 4)
        {
            stageInt = SwitchCount;
        }
        if (SwitchCount == 5)
        {
            stageInt = SwitchCount;
        }
        if (SwitchCount == 6)
        {
            stageInt = SwitchCount;
        }
        if (SwitchCount == 7)
        {
            stageInt = SwitchCount;
        }
        if (SwitchCount == 8)
        {
            stageInt = SwitchCount;
        }
        
    }

    public void StageAnimation()
    {
        if (stageInt == 1)
        {
            _countDown -= Time.deltaTime;
            if (_countDown <= 0)
            {
                StageSwitch(2);
            }

        }
        if (stageInt == 2)
        {

            if (orginalPlane.transform.position.y > -20)
            {
                orginalPlane.transform.position += Vector3.down * 2 * Time.deltaTime;

            }
            else
            {
                if (WorldEdge.transform.position.y > 40)
                {
                    WorldEdge.transform.position += Vector3.down * 2 * Time.deltaTime;
                }
            }


            StageSwitch(3);
        }
        if (stageInt == 3)
        {

        }
        if (stageInt == 4)
        {

        }
        if (stageInt == 5)
        {

        }
        if (stageInt == 6)
        {

        }
        if (stageInt == 7)
        {

        }
        if (stageInt == 8)
        {

        }
    }

    private void Update()
    {
        //if (Time.frameCount == 30)
        //{
        //    StageSwitch(1);
        //}
        StageAnimation();
    }



}
