using UnityEditor;
using UnityEngine;

// 에디터가 로드될 때 자동 실행
[InitializeOnLoad]
public static class SceneViewFollower
{
    private static Transform followTarget;
    private static bool isFollowing = true;  // 따라가기 모드 on/off

    static SceneViewFollower()
    {
        EditorApplication.update += OnEditorUpdate;
        Selection.selectionChanged += OnSelectionChanged;
    }

    // 메뉴에서 토글 가능
    [MenuItem("Tools/SceneView Follower/Follow Selected %#f")]
    private static void ToggleFollow()
    {
        isFollowing = !isFollowing;
        if (!isFollowing)
            followTarget = null;    // 해제 시 타깃 클리어
        // 메뉴에 체크 표시 업데이트
        Menu.SetChecked("Tools/SceneView Follower/Follow Selected", isFollowing);
    }

    // 메뉴 활성화 상태 표시
    [MenuItem("Tools/SceneView Follower/Follow Selected %#f", true)]
    private static bool ToggleFollowValidate()
    {
        Menu.SetChecked("Tools/SceneView Follower/Follow Selected", isFollowing);
        return true;
    }

    // 선택한 오브젝트가 있을 때만 타깃으로 지정
    private static void OnSelectionChanged()
    {
        if (!isFollowing) return;
        if (Selection.activeTransform != null)
            followTarget = Selection.activeTransform;
    }

    // 에디터 업데이트 루프
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