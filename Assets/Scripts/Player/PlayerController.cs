using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Profiling;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public String botName;

    private TerrainMesh terrainMesh
    {
        get
        {
            return GameObject.Find("TerrainGen").GetComponent<TerrainMesh>();
        }
    }

    private ColourGenerator2D colourGenerator2D
    {
        get
        {
            return GameObject.Find("TerrainGen").GetComponent<ColourGenerator2D>();
        }
    }

    public Image screenShotMask;

    [Header("Movement")]
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float flySpeed = 0.1f;
    public float gravity = 20.0f;
    public float lookSpeed = 2.0f;
    public float lookYLimit = 45.0f;

    [HideInInspector] public bool canMove = true;


    [Header("Terrain Editing")]
    [Range(1, 20)] public float terrainEditingHardRange = 5.0f;
    [Range(1, 20)] public float terrainEditingSoftRange = 10.0f;
    public float digStrength = 1.0f;

    public AudioClip screenShotSound;
    AudioSource audioSource;
    PlayerInputActions playerInputActions;
    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationY = 0;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        characterController = GetComponent<CharacterController>();

        LockCursor();
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();
    }

    private void Update()
    {
        CheckRay();

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
            CheckScreenShot();
        }
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
        audioSource.PlayOneShot(screenShotSound);
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
            if (hit.collider.tag != "Chunk") return;

            // Profiler.BeginSample("PF_DrawOnChunk");

            // if (Input.GetMouseButton(0))
            // {
            //     terrainMesh.DrawOnChunk(hit.point, terrainEditingRange, digStrength, 0);
            //     colourGenerator2D.DrawTextureOnWorldPos(hit.point, terrainEditingRange, ColourGenerator2D.DrawType.Clear);
            // }
            // else if (Input.GetMouseButton(1))
            // {
            //     terrainMesh.DrawOnChunk(hit.point, terrainEditingRange, digStrength, 1);
            //     colourGenerator2D.DrawTextureOnWorldPos(hit.point, terrainEditingRange, ColourGenerator2D.DrawType.Clear);
            // }

            // Profiler.EndSample();

            if (hit.collider.tag != "Chunk") return;

            if (Input.GetMouseButton(0))
            {
                terrainMesh.DrawOnChunk(hit.point, terrainEditingSoftRange, 0.0f, 1);
                colourGenerator2D.DrawTextureOnWorldPos(hit.point, terrainEditingHardRange, terrainEditingSoftRange, ColourGenerator2D.DrawType.Grass);
            }
            else if (Input.GetMouseButton(1))
            {
                terrainMesh.DrawOnChunk(hit.point, terrainEditingSoftRange, 0.0f, 1);
                colourGenerator2D.DrawTextureOnWorldPos(hit.point, terrainEditingHardRange, terrainEditingSoftRange, ColourGenerator2D.DrawType.Metal);
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
            Camera.main.transform.localRotation = Quaternion.Euler(rotationY, 0, 0);
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
