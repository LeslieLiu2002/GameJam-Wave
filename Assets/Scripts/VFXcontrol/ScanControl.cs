using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class ScannerController : MonoBehaviour
{
    [Header("Radius Control")]
    [Tooltip("目标最大半径 (Shader内部计算用)")]
    public float targetRadius = 10f;
    
    [Tooltip("展开动画耗时")]
    public float openDuration = 0.5f;
    
    [Tooltip("关闭/锐减动画耗时")]
    public float closeDuration = 0.2f;
    
    private Renderer _renderer;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _currentRoutine;
    
    private static readonly int _OverrideRadiusID = Shader.PropertyToID("_OverrideRadius");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();

        // 初始状态：设置半径为 0 并关闭渲染
        UpdateMPB(0f);
        if (_renderer) _renderer.enabled = false;
    }

    // --- 公开方法 ---

    [ContextMenu("Test Open")] // 允许右键组件菜单调用
    public void Open()
    {
        if (_renderer) _renderer.enabled = true;
        
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        _currentRoutine = StartCoroutine(AnimateRadius(0f, targetRadius, openDuration));
    }

    [ContextMenu("Test Close")] // 允许右键组件菜单调用
    public void Close()
    {
        if (_currentRoutine != null) StopCoroutine(_currentRoutine);
        
        _currentRoutine = StartCoroutine(AnimateRadius(targetRadius, 0f, closeDuration, () => 
        {
            if (_renderer) _renderer.enabled = false;
        }));
    }

    // --- 内部逻辑 ---

    private IEnumerator AnimateRadius(float start, float end, float duration, System.Action onComplete = null)
    {
        float timer = 0f;
        if (duration <= 0.001f) duration = 0.001f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            float currentR = Mathf.Lerp(start, end, smoothT);
            
            UpdateMPB(currentR);
            yield return null;
        }

        UpdateMPB(end);
        onComplete?.Invoke();
    }

    private void UpdateMPB(float radius)
    {
        if (_renderer == null) return;

        _renderer.GetPropertyBlock(_propBlock);
        
        _propBlock.SetFloat(_OverrideRadiusID, radius);
        _renderer.SetPropertyBlock(_propBlock);
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ScannerController))]
public class ScannerControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); 

        ScannerController script = (ScannerController)target;

        GUILayout.Space(10);
        GUILayout.Label("Debug Controls (Play Mode Only)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open Effect", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                script.Open();
            }
            else
            {
                Debug.LogWarning("动画测试需要在 Play Mode 下运行。");
            }
        }

        if (GUILayout.Button("Close Effect", GUILayout.Height(30)))
        {
            if (Application.isPlaying)
            {
                script.Close();
            }
            else
            {
                Debug.LogWarning("动画测试需要在 Play Mode 下运行。");
            }
        }
        GUILayout.EndHorizontal();
    }
}
#endif