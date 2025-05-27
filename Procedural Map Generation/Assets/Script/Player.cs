using System.Threading;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    /***********************************************************************
   *                               Inspector Fields
   ***********************************************************************/
    #region .
    [SerializeField] World world;

    [Range(1f, 10f)]
    [SerializeField] private float walkSpeed = 5f;

    [Range(-20, 1f)]
    [SerializeField] private float gravity = -9.8f;

    #endregion
    /***********************************************************************
    *                               Private Reference Fields
    ***********************************************************************/
    #region .
    private Transform camTr;

    #endregion
    /***********************************************************************
    *                               Private Fields
    ***********************************************************************/
    #region .
    private float h;
    private float v;
    private float mouseX;
    private float mouseY;
    private float deltaTime;

    private Vector3 velocity;

    private float playerWidth = 0.3f;       // 플레이어의 XZ 반지름
    private float boundsTolerance = 0.3f;
    private float verticalMomentum = 0f;

    private bool isGrounded = false;
    private bool isJumping = false;
    private bool isRunning = false;
    private bool jumpRequested = false;

    #endregion

    private void Awake()
    {
        Init();
        
    }
    private void Start()
    {   
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        deltaTime = Time.deltaTime;
        GetPlayerInputs();
        CalculateVelocity();
        MoveAndRotate();
    }
    private void Init()
    {
        var cam = GetComponentInChildren<Camera>();
        camTr = cam.transform;
    }

    private void GetPlayerInputs()
    {
        h = Input.GetAxisRaw("Horizontal");
        v = Input.GetAxisRaw("Vertical");
        mouseX = Input.GetAxis("Mouse X");
        mouseY = Input.GetAxis("Mouse Y");
    }

    private void CalculateVelocity()
    {
        velocity = ((transform.forward * v) + (transform.right * h)) * deltaTime * walkSpeed;
        velocity += Vector3.up * CalculateDownSpeedAndSetGroundState(gravity * deltaTime); // 중력 적용, 바닥 인식
    }

    private void MoveAndRotate()
    {
        // Rotate
        transform.Rotate(Vector3.up * mouseX);
        camTr.Rotate(Vector3.right * -mouseY);

        // Move
        transform.Translate(velocity, Space.World);
    }
    private float CalculateDownSpeedAndSetGroundState(float yVelocity)
    {
        // playerWidth * 2를 변의 길이로 하는 XZ 평면 정사각형의 네 꼭짓점에서 하단으로 grounded 체크
        // gounded 체크가 플레이어 회전의 영향을 받지 않도록, transform 로컬벡터가 아니라 월드벡터 기준으로 검사
        // 즉, 플레이어가 회전해도 큐브 모양의 콜라이더가 회전하지 않는 효과

        Vector3 pos = transform.position;

        isGrounded =
            world.IsBlockSolid(new Vector3(pos.x - playerWidth, pos.y + yVelocity, pos.z - playerWidth)) ||
            world.IsBlockSolid(new Vector3(pos.x + playerWidth, pos.y + yVelocity, pos.z - playerWidth)) ||
            world.IsBlockSolid(new Vector3(pos.x + playerWidth, pos.y + yVelocity, pos.z + playerWidth)) ||
            world.IsBlockSolid(new Vector3(pos.x - playerWidth, pos.y + yVelocity, pos.z + playerWidth));

        return isGrounded ? 0 : yVelocity;
    }

}
