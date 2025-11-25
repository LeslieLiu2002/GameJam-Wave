using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ObjectDetector))]
public class ObjectDetecterEditor : Editor
{
    void OnSceneGUI()
    {
        ObjectDetector od = (ObjectDetector)target;
        Handles.color = Color.white;
        Handles.DrawWireArc(od.transform.position, Vector3.up, Vector3.forward, 360, od.detectRadius);

        Handles.color = Color.red; // 设置手柄颜色为红色
        // 绘制到所有可见目标的连线
        foreach (Transform accessTarget in od.targets)
        {
            Handles.DrawLine(od.transform.position, accessTarget.position);
        }
    }
}
