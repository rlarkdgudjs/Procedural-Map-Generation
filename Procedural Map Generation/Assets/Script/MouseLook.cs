using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("마우스 감도")]
    public float mouseSensitivity = 100f;

    [Header("플레이어 몸체 Transform (Y축 회전용)")]
    public Transform playerBody;

    float xRotation = 0f;

    void Start()
    {
        // 게임 시작 시 마우스 커서를 화면 중앙에 잠그고 보이지 않게
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1) 마우스 입력
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2) 수직 회전값 누적 및 한계(-90° ~ +90°) 클램프
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 3) 카메라(이 스크립트가 붙은 오브젝트) 수직 회전 적용
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 4) 플레이어 몸체는 수평 회전만
        playerBody.Rotate(Vector3.up * mouseX);
    }
}

