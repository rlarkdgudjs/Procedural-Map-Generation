using UnityEngine;

public class TestMove : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    private float xRotation = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // 좌우 회전은 플레이어 오브젝트가 아니라 카메라 부모 (또는 직접 카메라) 기준
        transform.Rotate(Vector3.up * mouseX);

        // 상하 회전은 카메라만 회전
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        // 카메라의 전방/우측 방향을 기준으로 이동 (Y축 제거 후 정규화)
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;



        Vector3 move = camRight * moveX + camForward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;
    }
}
