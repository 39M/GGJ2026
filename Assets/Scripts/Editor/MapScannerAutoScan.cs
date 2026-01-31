#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GGJ.Editor
{
    /// <summary>
    /// 编辑器下加载场景时自动扫描地图
    /// </summary>
    [InitializeOnLoad]
    public static class MapScannerAutoScan
    {
        static MapScannerAutoScan()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
        }

        private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
        {
            // 延迟执行，确保场景完全加载
            EditorApplication.delayCall += () =>
            {
                AutoScanMap();
            };
        }

        /// <summary>
        /// 自动扫描地图
        /// </summary>
        private static void AutoScanMap()
        {
            var mapScanner = Object.FindFirstObjectByType<MapScanner>();
            if (mapScanner != null)
            {
                mapScanner.ScanMap();
                Debug.Log($"[MapScannerAutoScan] 场景加载完成，自动扫描地图");
                
                // 标记场景已修改（如果需要保存扫描结果）
                EditorSceneManager.MarkSceneDirty(mapScanner.gameObject.scene);
            }
        }
    }
}
#endif