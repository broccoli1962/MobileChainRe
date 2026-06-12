#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// 에디터에서 EditorOnly 태그가 붙은 프리뷰 오브젝트를 플레이 모드 진입 시 자동으로 정리합니다.
/// 씬에 미리 배치한 프리뷰 오브젝트가 런타임 동적 생성과 충돌하지 않도록 합니다.
/// </summary>
public static class EditorPreviewUtil
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CleanEditorOnlyObjects()
    {
        var editorOnlyObjects = GameObject.FindGameObjectsWithTag("EditorOnly");
        foreach (var go in editorOnlyObjects)
            Object.DestroyImmediate(go);
    }
}
#endif
