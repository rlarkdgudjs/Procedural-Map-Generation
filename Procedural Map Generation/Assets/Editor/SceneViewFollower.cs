using UnityEditor;
using UnityEngine;

// �����Ͱ� �ε�� �� �ڵ� ����
[InitializeOnLoad]
public static class SceneViewFollower
{
    // ���� ���� Ÿ��
    private static Transform followTarget;

    static SceneViewFollower()
    {
        // ������ ������Ʈ���� ȣ��
        EditorApplication.update += OnEditorUpdate;
        // ������ ����� ������ Ÿ�� ����
        Selection.selectionChanged += OnSelectionChanged;
    }

    // ���õ� Transform�� ������ Ÿ������ ����
    private static void OnSelectionChanged()
    {
        if (Selection.activeTransform != null)
            followTarget = Selection.activeTransform;
    }

    // ������ ������Ʈ ����
    private static void OnEditorUpdate()
    {
        if (followTarget == null)
            return;

        // ������ Ȱ��ȭ�� SceneView ��������
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
            return;

        // ī�޶� ��ġ(pivot)�� Ÿ�� ��ġ�� ������Ʈ
        sceneView.pivot = followTarget.position;
        sceneView.Repaint();
    }
}
