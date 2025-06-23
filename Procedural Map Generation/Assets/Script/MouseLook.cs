using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("���콺 ����")]
    public float mouseSensitivity = 100f;

    [Header("�÷��̾� ��ü Transform (Y�� ȸ����)")]
    public Transform playerBody;

    float xRotation = 0f;

    void Start()
    {
        // ���� ���� �� ���콺 Ŀ���� ȭ�� �߾ӿ� ��װ� ������ �ʰ�
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1) ���콺 �Է�
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2) ���� ȸ���� ���� �� �Ѱ�(-90�� ~ +90��) Ŭ����
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // 3) ī�޶�(�� ��ũ��Ʈ�� ���� ������Ʈ) ���� ȸ�� ����
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 4) �÷��̾� ��ü�� ���� ȸ����
        playerBody.Rotate(Vector3.up * mouseX);
    }
}

