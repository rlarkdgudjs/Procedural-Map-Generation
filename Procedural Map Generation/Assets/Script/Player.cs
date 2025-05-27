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

    private float playerWidth = 0.3f;       // �÷��̾��� XZ ������
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
        velocity += Vector3.up * CalculateDownSpeedAndSetGroundState(gravity * deltaTime); // �߷� ����, �ٴ� �ν�
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
        // playerWidth * 2�� ���� ���̷� �ϴ� XZ ��� ���簢���� �� ���������� �ϴ����� grounded üũ
        // gounded üũ�� �÷��̾� ȸ���� ������ ���� �ʵ���, transform ���ú��Ͱ� �ƴ϶� ���庤�� �������� �˻�
        // ��, �÷��̾ ȸ���ص� ť�� ����� �ݶ��̴��� ȸ������ �ʴ� ȿ��

        Vector3 pos = transform.position;

        isGrounded =
            world.IsBlockSolid(new Vector3(pos.x - playerWidth, pos.y + yVelocity, pos.z - playerWidth)) ||
            world.IsBlockSolid(new Vector3(pos.x + playerWidth, pos.y + yVelocity, pos.z - playerWidth)) ||
            world.IsBlockSolid(new Vector3(pos.x + playerWidth, pos.y + yVelocity, pos.z + playerWidth)) ||
            world.IsBlockSolid(new Vector3(pos.x - playerWidth, pos.y + yVelocity, pos.z + playerWidth));

        return isGrounded ? 0 : yVelocity;
    }

}
