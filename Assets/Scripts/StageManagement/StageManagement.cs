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

    public List<GameObject> SkyObject;
    public CameraControl _cameraControl;
    public GameObject SecondItem;
    public Vector3 secondPosition;
    public GameObject ThirdItem;
    public Vector3 thirdPosition;
    public GameObject FourthItem;
    public Vector3 fourthPosition;
    public int stageInt;
    public float _countDown;
    private float rotateTime = 8f;



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
            _countDown = rotateTime;
            stageInt = SwitchCount;
        }
        if (SwitchCount == 2)
        {
            _cameraControl.CameraShake(40);
            
            Instantiate(SecondItem, secondPosition, SecondItem.transform.rotation);
            stageInt = SwitchCount;
        }
        if (SwitchCount == 3)
        {

            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = rotateTime;
            stageInt = SwitchCount;
        }
        if (SwitchCount == 4)
        {
            Instantiate(ThirdItem, thirdPosition, ThirdItem.transform.rotation);
            stageInt = SwitchCount;
        }
        if (SwitchCount == 5)
        {
            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = rotateTime;
            stageInt = SwitchCount;
        }
        if (SwitchCount == 6)
        {
            Instantiate(FourthItem, fourthPosition, FourthItem.transform.rotation);
            stageInt = SwitchCount;
        }
        if (SwitchCount == 7)
        {
            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = rotateTime;
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

            if (orginalPlane.transform.position.y > -5)
            {
                orginalPlane.transform.position += Vector3.down * 2 * Time.deltaTime;
                WorldEdge.transform.position += Vector3.up * .5f * Time.deltaTime;
            }
            else
            {
                if (WorldEdge.transform.position.y < 40)
                {
                    WorldEdge.transform.position += Vector3.up * 6 * Time.deltaTime;
                }
                    else
                {
                    
                    StageSwitch(3);
                }
            }


            
        }
        if (stageInt == 3)
        {

            _countDown -= Time.deltaTime;
            if (_countDown <= 0)
            {
                StageSwitch(4);
            }


        }
        if (stageInt == 4)
        {


        }
        if (stageInt == 5)
        {
            _countDown -= Time.deltaTime;
            if (_countDown <= 0)
            {
                StageSwitch(6);
            }
        }
        if (stageInt == 6)
        {



        }
        if (stageInt == 7)
        {
            _countDown -= Time.deltaTime;
            if (_countDown <= 0)
            {
                StageSwitch(8);
            }

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
