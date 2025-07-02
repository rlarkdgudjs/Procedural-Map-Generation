using UnityEditor;
using UnityEngine;

// 에디터가 로드될 때 자동 실행
[InitializeOnLoad]
public static class SceneViewFollower
{
    // 현재 따라갈 타깃
    private static Transform followTarget;

    static SceneViewFollower()
    {
        // 에디터 업데이트마다 호출
        EditorApplication.update += OnEditorUpdate;
        // 선택이 변경될 때마다 타깃 설정
        Selection.selectionChanged += OnSelectionChanged;
    }

    // 선택된 Transform이 있으면 타깃으로 지정
    private static void OnSelectionChanged()
    {
        if (Selection.activeTransform != null)
            followTarget = Selection.activeTransform;
    }

    // 에디터 업데이트 루프
    private static void OnEditorUpdate()
    {
        if (followTarget == null)
            return;

        // 마지막 활성화된 SceneView 가져오기
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView == null)
            return;

        // 카메라 위치(pivot)를 타깃 위치로 업데이트
        sceneView.pivot = followTarget.position;
        sceneView.Repaint();
    }
}
