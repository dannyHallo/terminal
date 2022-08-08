using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public String botName;
    private UIManager uiManager
    {
        get
        {
            return GameObject.Find("Canvas").GetComponent<UIManager>();
        }
    }

    private TerrainMesh terrainMesh;
    private ColourGenerator2D colourGenerator2D;
    public GameObject butterflyPrefab;
    public GameObject metalSpiritPrefab;

    public Image screenShotMask;

    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float flySpeed = 0.1f;
    public float gravity = 20.0f;
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
    public float digStrength = 1.0f;

    AudioSource audioSource;
    public AudioClip Cam_35mm;
    PlayerInputActions playerInputActions;
    //Instrument
    public enum InstrumentTypes
    {
        Guitar,
        Violin,
        Sax,
        Dudelsa,
        Mic,
        None
    };

    public List<enumToInstrument> instruments;

    [Serializable]
    public struct enumToInstrument
    {
        public InstrumentTypes e;
        public GameObject i;
        public GameObject s;
        public bool have;
    }

    public GameObject mainSocket;
    public GameObject tmpSocket;
    private InstrumentTypes mainSocketCurrentStoringInstrument = InstrumentTypes.None;

    public float socketMoveFreq, socketMoveBoundary;
    public float instrumentChaseSocketSpeed;
    private float floatingHeight;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        // audioListener = GetComponent<AudioListener>();
        characterController = GetComponent<CharacterController>();

        LockCursor();
        ableToDig = true;

        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
    }

    public void EquipInstrument(InstrumentTypes instrumentType)
    {
        for (int i = 0; i < instruments.Count; i++)
        {
            enumToInstrument enumToInstrument = instruments[i];

            if (enumToInstrument.e == instrumentType)
            {
                if (enumToInstrument.have == true) return;

                GameObject genInstrument = GameObject.Instantiate(
                    enumToInstrument.i, enumToInstrument.s.transform.position, Quaternion.identity);

                enumToInstrument.i = genInstrument;
                enumToInstrument.have = true;

                instruments[i] = enumToInstrument;
                return;
            }
        }
    }

    public void EquipInstrument(int i)
    {
        if (i >= instruments.Count) return;

        if (instruments[i].have == true) return;

        GameObject genInstrument = GameObject.Instantiate(
            instruments[i].i, instruments[i].s.transform.position, Quaternion.identity);

        // Change equip status to true, and change prefab to scene object
        enumToInstrument tmpInstrument;
        tmpInstrument.e = instruments[i].e;
        tmpInstrument.s = instruments[i].s;
        tmpInstrument.i = genInstrument;
        tmpInstrument.have = true;

        instruments[i] = tmpInstrument;
    }

    private void MoveSockets()
    {
        if (instruments.Count <= 0)
            return;

        if (floatingHeight == 0) floatingHeight = instruments[0].s.transform.localPosition.y;

        for (int i = 0; i < instruments.Count; i++)
        {
            if (!instruments[i].have)
                continue;

            Vector3 pos = instruments[i].s.transform.localPosition;
            pos.y = floatingHeight + Mathf.Sin(
                (2 * Mathf.PI * ((float)i / (2 * instruments.Count))) +
                (2 * Mathf.PI * Time.time * socketMoveFreq)) * socketMoveBoundary;

            instruments[i].s.transform.localPosition = pos;
            // instruments[i].i.transform.localPosition = pos;
            instruments[i].i.transform.position = Vector3.Lerp(instruments[i].i.transform.position,
                instruments[i].s.transform.position, Time.deltaTime * instrumentChaseSocketSpeed);
        }
    }


    private void ChangeWeaponCheck()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            TryToggleInstrument(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            TryToggleInstrument(1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            TryToggleInstrument(2);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            TryToggleInstrument(3);
        }
        if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            TryToggleInstrument(4);
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

    private void TryToggleInstrument(int i)
    {
        InstrumentUIColor(i);
        if (mainSocketCurrentStoringInstrument == instruments[i].e)
        {
            TryUseInstrument(InstrumentTypes.None);
            uiManager.InstrumentsUI[i].GetComponent<Image>().color = Color.gray;
        }
        else
        {
            TryUseInstrument(instruments[i].e);
        }
    }

    public void TryUseInstrument(InstrumentTypes instrument)
    {
        // No Instrument
        if (instrument == InstrumentTypes.None)
        {
            // Place the former instrument back - IDENTICAL
            if (mainSocketCurrentStoringInstrument != InstrumentTypes.None)
            {
                for (int i = 0; i < instruments.Count; i++)
                {

                    if (instruments[i].e == mainSocketCurrentStoringInstrument)
                    {
                        enumToInstrument enumToInstrument = instruments[i];
                        enumToInstrument.s = tmpSocket;
                        instruments[i] = enumToInstrument;
                    }
                }
            }
            uiManager.hintText.text = "";

            // clear sockets
            mainSocketCurrentStoringInstrument = InstrumentTypes.None;
            tmpSocket = null;
            return;
        }

        // Valid instrument
        for (int instru = 0; instru < instruments.Count; instru++)
        {
            enumToInstrument queriedInstrument = instruments[instru];

            if (queriedInstrument.e == instrument)
            {
                if (!queriedInstrument.have)
                {
                    print("Unusable because you don't have this instrument!");
                    return;
                }

                print(instru);

                switch (instru)
                {
                    case 0:
                        uiManager.hintText.text = "1 - Remove dirt (RMB)";
                        break;

                    case 1:
                        uiManager.hintText.text = "2 - Add dirt (LMB)";
                        break;

                    case 2:
                        uiManager.hintText.text = "3 - Add butterfly (LMB), Add metal spirit (RMB)";
                        break;

                    case 3:
                        uiManager.hintText.text = "4 - Not implemented...";
                        break;

                    case 4:
                        uiManager.hintText.text = "5 - Not implemented...";
                        break;
                }


                // Place the former instrument back
                if (mainSocketCurrentStoringInstrument != InstrumentTypes.None)
                {
                    for (int i = 0; i < instruments.Count; i++)
                    {
                        if (instruments[i].e == mainSocketCurrentStoringInstrument)
                        {
                            enumToInstrument enumToInstrument = instruments[i];
                            enumToInstrument.s = tmpSocket;
                            instruments[i] = enumToInstrument;
                        }
                    }
                }

                // Storing the current socket for future use
                mainSocketCurrentStoringInstrument = queriedInstrument.e;
                tmpSocket = queriedInstrument.s;
                // Change socket
                queriedInstrument.s = mainSocket;

                // Save
                instruments[instru] = queriedInstrument;
            }
        }
    }

    private void Awake()
    {
        // for (int i = 0; i < instruments.Count; i++)
        // {
        //     EquipInstrument(i);
        // }
    }

    private void Update()
    {
        CheckRay();
        ChangeWeaponCheck();
        MoveSockets();

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
            if (mainSocketCurrentStoringInstrument == InstrumentTypes.Sax)
            {
                if (hit.collider.tag == "Chunk")
                {

                    if (Input.GetMouseButton(0) && ableToDig)
                    {
                        terrainMesh.DrawOnChunk(hit.point, drawRange, digStrength, 0);
                    }

                }
            }
            else if (mainSocketCurrentStoringInstrument == InstrumentTypes.Dudelsa)
            {
                if (Input.GetMouseButton(0) && ableToDig)
                {
                    NotifyTerrainChanged(hit.point, drawRange);
                    terrainMesh.DrawOnChunk(hit.point, drawRange, digStrength, 1);
                }
            }
            else if (mainSocketCurrentStoringInstrument == InstrumentTypes.Guitar)
            {
                // Create
                if (hit.collider.tag == "Chunk")
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        GameObject bufferflyCreated =
                        GameObject.Instantiate(
                            butterflyPrefab,
                            new Vector3(hit.point.x, hit.point.y + 5.0f, hit.point.z),
                            Quaternion.identity);
                        bufferflyCreated.GetComponent<FlyingBehaviour>().seed = Time.time;
                    }
                    else if (Input.GetMouseButtonDown(1))
                    {
                        GameObject metalSpititCreated =
                        GameObject.Instantiate(
                            metalSpiritPrefab,
                            new Vector3(hit.point.x, hit.point.y + 5.0f, hit.point.z),
                            Quaternion.identity);
                        metalSpititCreated.GetComponent<FlyingBehaviour>().seed = Time.time;
                    }
                }
                // Destroy
                else if (hit.collider.tag == "Creature")
                {
                    Destroy(hit.collider.gameObject);
                }
            }
            // Creaste / Destroy Creature
            else if (mainSocketCurrentStoringInstrument == InstrumentTypes.Mic)
            {

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

}
