using UnityEditor;
using UnityEngine;

// �����Ͱ� �ε�� �� �ڵ� ����
[InitializeOnLoad]
public static class SceneViewFollower
{
    private static Transform followTarget;
    private static bool isFollowing = true;  // ���󰡱� ��� on/off

    static SceneViewFollower()
    {
        EditorApplication.update += OnEditorUpdate;
        Selection.selectionChanged += OnSelectionChanged;
    }

    // �޴����� ��� ����
    [MenuItem("Tools/SceneView Follower/Follow Selected %#f")]
    private static void ToggleFollow()
    {
        isFollowing = !isFollowing;
        if (!isFollowing)
            followTarget = null;    // ���� �� Ÿ�� Ŭ����
        // �޴��� üũ ǥ�� ������Ʈ
        Menu.SetChecked("Tools/SceneView Follower/Follow Selected", isFollowing);
    }

    // �޴� Ȱ��ȭ ���� ǥ��
    [MenuItem("Tools/SceneView Follower/Follow Selected %#f", true)]
    private static bool ToggleFollowValidate()
    {
        Menu.SetChecked("Tools/SceneView Follower/Follow Selected", isFollowing);
        return true;
    }

    // ������ ������Ʈ�� ���� ���� Ÿ������ ����
    private static void OnSelectionChanged()
    {
        if (!isFollowing) return;
        if (Selection.activeTransform != null)
            followTarget = Selection.activeTransform;
    }

    // ������ ������Ʈ ����
    private static void OnEditorUpdate()
    {
        if (!isFollowing || followTarget == null)
            return;

        var sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null) return;

        sceneView.pivot = followTarget.position;
        sceneView.Repaint();
    }
}