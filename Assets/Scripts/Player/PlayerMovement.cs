using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public String botName;
    public TerrainMesh terrainMesh;

    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookYLimit = 45.0f;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationY = 0;

    [HideInInspector]
    public bool canMove = true;

    bool ableToDig = true;
    Rigidbody rb;

    [Range(1, 10)]
    public int drawRange = 5;
    public Image screenShotMask;
    //Input

    // Flags
    bool startCoroutineF,
    terraUpdate = false;

    // Others
    public LayerMask playerMask;

    Coroutine c = null;
    public CapsuleCollider capsuleCollider;

    AudioSource audioSource;
    public AudioClip Cam_35mm;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>(); characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = false;
        ableToDig = true;
        // transform.position = new Vector3(0, 1000f, 0);
        // atmosphereSettings.timeOfDay = 0f;

        if (!isLocalPlayer)
            playerCamera.gameObject.SetActive(false);
    }

    // Land on planet initially
    void TryToLand()
    {
        float rayLength = 2000f;

        // actual Ray
        Ray ray = new Ray(transform.position + new Vector3(0, 30f, 0), Vector3.down);

        // debug Ray
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.green);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayLength, playerMask))
        {
            transform.position = hit.point + new Vector3(0, 10f, 0);
        }
    }

    private void FixedUpdate()
    {
        // CheckRay();
    }

    private void Update()
    {
        if (!isLocalPlayer)
            return;
        if (!terrainMesh)
            terrainMesh = GameObject.Find("TerrainMesh").GetComponent<TerrainMesh>();
        // if (!landed)
        //     TryToLand();

        CheckScreenShot();
        Movement();
        // move cam pos to socket
    }

    IEnumerator DiggingCountdown()
    {
        yield return new WaitForEndOfFrame();
        ableToDig = true;
    }

    IEnumerator TakePhoto()
    {
        String desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        String filename = desktopPath + "/" + botName.ToUpper() + "_" + UnityEngine.Random.Range(100, 1000).ToString() + ".png";
        print(filename);
        ScreenCapture.CaptureScreenshot(filename, 2);
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

        // debug Ray
        Debug.DrawRay(ray.origin, ray.direction * rayLength, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, rayLength))
        {

            // Get Left Mouse Button

            // The direct hit is a chunk
            if (hit.collider.tag == "Chunk")
            {
                if (Input.GetMouseButton(0))
                {
                    if (ableToDig)
                    {
                        terrainMesh.DrawOnChunk(hit.point, drawRange, 0);
                        ableToDig = false;
                        startCoroutineF = true;
                    }
                    else if (startCoroutineF)
                    {
                        RestartCoroutine();
                    }
                }

                // Right Mouse Btn
                else if (Input.GetMouseButton(1))
                {
                    if (ableToDig)
                    {
                        terrainMesh.DrawOnChunk(hit.point, drawRange, 1);
                        NotifyTerrainChanged(hit.point, drawRange);
                        ableToDig = false;
                        startCoroutineF = true;
                    }
                    else if (startCoroutineF)
                    {
                        RestartCoroutine();
                    }
                }
            }
        }
    }

    void RestartCoroutine()
    {
        startCoroutineF = false;
        if (c != null)
        {
            StopCoroutine(c);
        }
        c = StartCoroutine(DiggingCountdown());
    }

    private void CheckScreenShot()
    {
        if (Input.GetKeyDown(KeyCode.F))
            StartCoroutine(TakePhoto());

        if (Input.GetMouseButtonUp(0))
        {
            RestartCoroutine();
        }
    }

    private void Movement()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationY += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -lookYLimit, lookYLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationY, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }

    public void NotifyTerrainChanged(Vector3 point, float radius)
    {
        float dstFromCam = (point - transform.position).magnitude;
        if (dstFromCam < radius)
        {
            terraUpdate = true;
        }
    }

    private void LateUpdate()
    {
        if (terraUpdate)
        {
            float heightOffset = 5f;
            // Normalized direction
            Vector3 localUp = transform.up;
            Vector3 a = transform.position - localUp * (capsuleCollider.height / 2 + capsuleCollider.radius - heightOffset);
            Vector3 b = transform.position + localUp * (capsuleCollider.height / 2 + capsuleCollider.radius + heightOffset);
            RaycastHit hitInfo;


            if (Physics.CapsuleCast(a, b, capsuleCollider.radius, -localUp, out hitInfo, heightOffset, playerMask))
            {
                Vector3 hp = hitInfo.point;
                Vector3 newPos = hp;
                float deltaY = Vector3.Dot(transform.up, (newPos - transform.position));
                // if (deltaY > 0.01f)
                {
                    transform.position = newPos;
                    // grounded = true;
                }
            }
            terraUpdate = false;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
    }
}
