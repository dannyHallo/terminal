// Some stupid rigidbody based movement by Dani

using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(AudioSource))]
public class PlayerMovement : MonoBehaviour
{
    public String botName;
    public float gravity = 10f;
    public Scrollbar thrustIndicator;
    public MeshGenerator meshGenerator;
    public Transform playerCamera;
    public AtmosphereSettings atmosphereSettings;
    bool ableToDig = true;
    //Others
    Rigidbody rb;

    //Rotation and look
    float verticalRotation;
    float mouseSensitivity = 50f;

    //Movement
    public float moveSpeed = 100;
    public float maxSpeed = 4;

    float distToGnd = 0.8f;
    public bool grounded = false;
    //Crouch & Slide
    Vector3 playerScale;

    [Range(1, 10)]
    public int drawRange = 5;
    public float thrustForce = 150;
    public Image screenShotMask;
    //Input
    float x, y;

    // Flags
    bool jumping,
    startCoroutineF,
    sprinting,
    crouching,
    readyToJump = true,
    terraUpdate = false,
    landed = false;

    // Others
    float oriMoveSpeed;
    float oriMaxSpeed;
    public LayerMask playerMask;
    float multiplier = 1f;
    private float horiRotation;

    Coroutine c = null;
    public CapsuleCollider capsuleCollider;

    AudioSource audioSource;
    public AudioClip Cam_35mm;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = false;
        ableToDig = true;
        transform.position = new Vector3(0, 1000f, 0);
        // atmosphereSettings.timeOfDay = 0f;

        oriMoveSpeed = moveSpeed;
        oriMaxSpeed = maxSpeed;
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
            transform.position = hit.point + new Vector3(0, 3f, 0);
            landed = true;
        }
    }

    private void FixedUpdate()
    {
        CheckRay();
    }

    private void Update()
    {
        if (!landed)
            TryToLand();
        rb.AddForce(Vector3.down * gravity);
        GroundCheck();
        GetInput();
        Movement();
        Look();
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
        float rayLength = 1000f;

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
                        meshGenerator.DrawOnChunk(hit.point, drawRange, 0);
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
                        meshGenerator.DrawOnChunk(hit.point, drawRange, 1);
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

    private void GetInput()
    {
        if (Input.GetKeyDown(KeyCode.F))
            StartCoroutine(TakePhoto());

        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        crouching = Input.GetKey(KeyCode.LeftControl);
        if (Input.GetMouseButtonUp(0))
        {
            RestartCoroutine();
        }

        //Crouching
        if (Input.GetKeyDown(KeyCode.LeftControl))
            StartCrouch();
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();

        //Sprinting
        if (Input.GetKeyDown(KeyCode.LeftShift))
            StartSprint();
        if (Input.GetKeyUp(KeyCode.LeftShift))
            StopSprint();
    }

    private void StartCrouch() { }

    private void StopCrouch() { }

    private void StartSprint()
    {
        moveSpeed *= 2f;
        maxSpeed *= 2f;
    }

    private void StopSprint()
    {
        moveSpeed = oriMoveSpeed;
        maxSpeed = oriMaxSpeed;
    }

    private void Movement()
    {
        //Extra gravity
        //rb.AddForce(Vector3.down * Time.deltaTime * garvity * 4f);

        //Find actual velocity relative to where player is looking
        float speedMag = Vector2.SqrMagnitude(new Vector2(rb.velocity.x, rb.velocity.z));
        // print(speedMag);
        //Counteract sliding and sloppy movement
        //CounterMovement(x, y, mag);

        //If holding jump && ready to jump, then jump
        if (jumping)
            Jump();

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump)
        {
            rb.AddForce(-transform.up * Time.deltaTime * 3000);
            return;
        }

        // If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        // if (x != 0 || y != 0)
        // {
        //     if (speedMag > maxSpeed)
        //     {
        //         x = 0;
        //         y = 0;
        //     }
        // }
        // No input
        if (grounded)
        {
            float yVelocity = rb.velocity.y;
            rb.velocity = new Vector3(rb.velocity.x / 4, yVelocity, rb.velocity.z / 4);
        }

        //Apply forces to move player
        rb.AddForce(transform.forward * y * moveSpeed * Time.deltaTime * 100 * multiplier);
        rb.AddForce(transform.right * x * moveSpeed * Time.deltaTime * 100 * multiplier);
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

    private void Jump()
    {
        rb.AddForce(transform.up * thrustForce * 1f);
    }

    private void Look()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.fixedDeltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.fixedDeltaTime;

        //Find current look rotation
        Vector3 rot = playerCamera.localRotation.eulerAngles;
        //horiRotation = rot.y + mouseX;
        horiRotation = mouseX;

        //Rotate, and also make sure we dont over- or under-rotate.
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -90f, 90f);

        //Perform the rotations
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, horiRotation, 0);
        // playerBody.localRotation = Quaternion.Euler(0, horiRotation, 0);
        // transform.RotateAround(transform.position, transform.up, Time.deltaTime * 90f);
        transform.Rotate(0, horiRotation, 0, Space.Self);
    }

    void GroundCheck()
    {
        if (Physics.Raycast(transform.position, -transform.up, distToGnd))
            grounded = true;
        else
            grounded = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
    }
}
