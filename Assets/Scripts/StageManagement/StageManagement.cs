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
    public GameObject _space;
    public int stageInt;
    public float _countDown;
    private float rotateTime = 8f;
    public RandomForestGenerator randomForestGenerator;

    public AudioSource greybackgroundmusic;
    public AudioSource instrumentSwitching;
    public AudioSource landSwitching;
    public AudioSource landSwitchingTwo;
    public AudioSource earthquake;
    public AudioSource earthquakeTwo;
    public AudioSource skyChange;
    public AudioSource skyChangeTwo;
    public AudioSource factorySound;
    public AudioSource creatureSound;
    public AudioSource creatureSoundTwo;
    public AudioSource creatureNagativeSound;
    public AudioSource decoInstruSound;
    public AudioSource decoInstruSoundTwo;
    public AudioSource eighty;
    public AudioSource f2b;
    public AudioSource skydown;
    public AudioSource AI;
    public AudioSource grass;
    private void Start()
    {
        _cameraControl = FindObjectOfType<CameraControl>();
        orginalPlane.transform.position = new Vector3(0,20,0);
        _space.transform.localScale = Vector3.zero;
        randomForestGenerator = FindObjectOfType<RandomForestGenerator>();
    }


    public void StageSwitch (int SwitchCount)
    {
        if (SwitchCount == 1)
        {
            instrumentSwitching.Play();
            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = rotateTime;
            stageInt = SwitchCount;

        }
        if (SwitchCount == 2)
        {
            _cameraControl.CameraShake(40);
            earthquake.Play();
            Instantiate(SecondItem, secondPosition, SecondItem.transform.rotation);
            stageInt = SwitchCount;
        }
        if (SwitchCount == 3)
        {

            Destroy(orginalPlane);
            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = rotateTime;
            stageInt = SwitchCount;
        }
        if (SwitchCount == 4)
        {
            Instantiate(ThirdItem, thirdPosition, ThirdItem.transform.rotation);
            stageInt = SwitchCount;
            _countDown = 0;
        }
        if (SwitchCount == 5)
        {
            if (_cameraControl.isFollowingPlayer) _cameraControl.OrbitPlayer();
            _countDown = rotateTime;
            stageInt = SwitchCount;
            Debug.Log("III");
        }
        if (SwitchCount == 6)
        {
            Debug.Log("we");
            randomForestGenerator.SpawnForest();
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
            greybackgroundmusic.volume -= Time.deltaTime;
            
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
                WorldEdge.transform.position += Vector3.up * 4 * Time.deltaTime;
            }
            else
            {
                if (WorldEdge.transform.position.y < 40)
                {
                    WorldEdge.transform.position += Vector3.up * 6 * Time.deltaTime;
                }
                    else
                {
                    earthquake.volume -= Time.deltaTime;
                    //StageSwitch(3);
                    if (_cameraControl.normalFollowingCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>().m_AmplitudeGain >= 0)
                    {
                   _cameraControl.CameraShakeStop();
                    }

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
            _countDown += Time.deltaTime;
            if (_space.transform.localScale.y <= 0.1f)
            {
                _space.transform.localScale = Vector3.one * 0.0000000000001f * _countDown * _countDown * _countDown * _countDown * _countDown *_countDown * _countDown *_countDown * _countDown;
            }


        }
        if (stageInt == 5)
        {
            Debug.Log("we");
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
