using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(AudioListener))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public String botName;
    public TerrainMesh terrainMesh;
    public ColourGenerator2D colourGenerator2D;

    public Image screenShotMask;

    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float flySpeed = 0.1f;
    public float gravity = 20.0f;
    // public Camera playerCamera;
    public AudioListener audioListener;
    public float lookSpeed = 2.0f;
    public float lookYLimit = 45.0f;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationY = 0;

    [HideInInspector]
    public bool canMove = true;

    bool ableToDig = true;

    [Range(1, 10)]
    public int drawRange = 5;

    // Others
    public LayerMask playerMask;

    AudioSource audioSource;
    public AudioClip Cam_35mm;
    PlayerInputActions playerInputActions;
    //Instrument
    public enum InstrumentTypes
    {
        Guitar,
        Violin
    };
    public List<enumToInstrument> instruments;
    private GameObject activeInstrument;

    [Serializable]
    public struct enumToInstrument
    {
        public InstrumentTypes e;
        public GameObject g;
        public bool have;
    }
    public UIManager uiManager;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // audioListener = GetComponent<AudioListener>();
        characterController = GetComponent<CharacterController>();

        LockCursor();
        ableToDig = true;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
        uiManager = FindObjectOfType<UIManager>();
    }

    // Land on planet initially


    private void Update()
    {
        CheckRay();
        ChangeWeaponCheck();
        if (!terrainMesh)
            terrainMesh = GameObject.Find("TerrainGen").GetComponent<TerrainMesh>();
        if (!colourGenerator2D)
            colourGenerator2D = GameObject.Find("TerrainGen").GetComponent<ColourGenerator2D>();

        if (Cursor.lockState == CursorLockMode.None)
        {
            if (PlayerWantsToLockCursor())
                LockCursor();
            Movement(false);
        }
        else if (Cursor.lockState == CursorLockMode.Locked)
        {
            if (PlayerWantsToUnlockCursor())
                UnlockCursor();
            Movement(true);
            // CheckRay();
            CheckScreenShot();
        }
    }

    IEnumerator DiggingCountdown()
    {
        yield return new WaitForEndOfFrame();
        ableToDig = true;
    }

    IEnumerator TakePhoto()
    {
        String desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        String filename =
            desktopPath
            + "/"
            + botName.ToUpper()
            + "_"
            + UnityEngine.Random.Range(100, 1000).ToString()
            + ".png";
        ScreenCapture.CaptureScreenshot(filename, 1);
        yield return new WaitForSeconds(0.05f);
        audioSource.PlayOneShot(Cam_35mm);
        yield return new WaitForSeconds(0.05f);
        Color tempColForMask = screenShotMask.color;
        tempColForMask.a = 1;
        screenShotMask.color = tempColForMask;
        yield return new WaitForSeconds(0.008f);
        while (tempColForMask.a > 0)
        {
            tempColForMask.a -= 0.02f;
            screenShotMask.color = tempColForMask;
            yield return new WaitForSeconds(0.008f);
        }
    }

    private void CheckRay()
    {
        Vector3 rayOrigin = new Vector3(0.5f, 0.5f, 0f); // center of the screen
        float rayLength = 3000f;

        // actual Ray
        Ray ray = Camera.main.ViewportPointToRay(rayOrigin);
        LayerMask IgnoreMe = LayerMask.GetMask("Player");

        // debug Ray
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayLength, ~IgnoreMe))
        {
            if (hit.collider.tag == "Chunk")
            {
                if (Input.GetMouseButton(0) && ableToDig)
                {
                    terrainMesh.DrawOnChunk(hit.point, drawRange, 0);
                    colourGenerator2D.DrawTextureOnWorldPos(colourGenerator2D.userTex, hit.point, drawRange);

                }
                else if (Input.GetMouseButton(1) && ableToDig)
                {
                    terrainMesh.DrawOnChunk(hit.point, drawRange, 1);
                    NotifyTerrainChanged(hit.point, drawRange);
                }
            }
        }
    }

    private void CheckScreenShot()
    {
        if (Input.GetKeyDown(KeyCode.F))
            StartCoroutine(TakePhoto());
    }

    private bool PlayerWantsToLockCursor()
    {
        return (
            (playerInputActions.Player.Return.ReadValue<float>() == 1)
            || (playerInputActions.Player.Movement.ReadValue<Vector2>() != new Vector2(0, 0))
        )
            ? true
            : false;
    }

    public bool PlayerWantsToUnlockCursor()
    {
        return (playerInputActions.Player.Exit.ReadValue<float>() == 1) ? true : false;
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Movement(bool takeControl)
    {
        Vector2 inputVector = new Vector2(0, 0);
        float xDrift = 0;
        float yDrift = 0;
        bool jumpPressed = false;
        bool sprintPressed = false;

        if (takeControl)
        {
            inputVector = playerInputActions.Player.Movement.ReadValue<Vector2>();
            xDrift += Input.GetAxis("Mouse X");
            xDrift += playerInputActions.Player.Rotation.ReadValue<Vector2>().x * 0.2f;
            yDrift += -Input.GetAxis("Mouse Y");
            sprintPressed = Input.GetKey(KeyCode.LeftShift);
            jumpPressed = (playerInputActions.Player.Jump.ReadValue<float>() == 1) ? true : false;
        }

        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        float curSpeedX = canMove
            ? (sprintPressed ? runningSpeed : walkingSpeed) * inputVector.y
            : 0;
        float curSpeedY = canMove
            ? (sprintPressed ? runningSpeed : walkingSpeed) * inputVector.x
            : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (!characterController.isGrounded)
        {
            moveDirection.y = movementDirectionY;
            moveDirection.y -= gravity * Time.deltaTime;
        }

        if (jumpPressed && canMove)
            moveDirection.y += flySpeed * Time.deltaTime;

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationY += yDrift * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -lookYLimit, lookYLimit);
            // playerCamera.transform.localRotation = Quaternion.Euler(rotationY, 0, 0);
            transform.rotation *= Quaternion.Euler(0, xDrift * lookSpeed, 0);
        }
    }

    public void NotifyTerrainChanged(Vector3 point, float radius)
    {
        float dstFromCam = (point - transform.position).magnitude;
        if (dstFromCam < radius)
        {
            // terraUpdate = true;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
    }
    private void ChangeWeaponCheck()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryUseInstrument(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryUseInstrument(1);
        }
    }

    public void InstrumentUIColor(int num)
    {
        for (int i = 0; i < uiManager.InstrumentsUI.Count; i++)
        {

            if (i != num)
            {
                uiManager.InstrumentsUI[i].GetComponent<Image>().color = Color.gray;
            }
            else
            {
                uiManager.InstrumentsUI[i].GetComponent<Image>().color = Color.white;
            }
        }
    }



    private void TryUseInstrument(int i)
    {

        var instrument = instruments[i].e;
        InstrumentUIColor(i);
        if (activeInstrument == instruments[i].g)
        {
            if (activeInstrument.activeInHierarchy)
            {
                activeInstrument.SetActive(false);
                uiManager.InstrumentsUI[i].GetComponent<Image>().color = Color.gray;
            }
            else
            {
                Debug.Log("!");
                activeInstrument.SetActive(true);
            }
        }
        else
        {
            UseInstrument(instruments[i].e);

        }

    }
    public void UseInstrument(InstrumentTypes instrument)
    {
        if (activeInstrument)
            activeInstrument.SetActive(false);

        foreach (enumToInstrument queriedInstrument in instruments)
        {
            if (queriedInstrument.e == instrument && queriedInstrument.have == true)
            {
                activeInstrument = queriedInstrument.g;
                activeInstrument.SetActive(true);
            }
        }
    }
}
