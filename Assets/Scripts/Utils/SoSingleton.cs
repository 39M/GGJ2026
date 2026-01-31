using Sirenix.OdinInspector;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GGJ
{


    public abstract class ResourceSingletonSO<T> : SerializedScriptableObject where T : ScriptableObject
    {
        private static T _instance;

        private const string _resourcesPath = "Assets/Resources";
        private static string SingletonName => $"{typeof(T).Name}";

        public static T Instance
        {
            get
            {
                if (_instance == null)
                { 
                    _instance = Resources.Load<T>(SingletonName);

#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        // 在编辑器中自动创建资源
                        _instance = CreateInstance<T>();
                        string path = $"{_resourcesPath}/{typeof(T).Name}.asset";

                        // 确保文件夹存在
                        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Application.dataPath + "/../" + path) ?? string.Empty);

                        AssetDatabase.CreateAsset(_instance, path);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();

                        Debug.LogWarning($"创建了新的ScriptableObject单例: {path}");
                    }
#endif

                    if (_instance == null)
                    {
                        Debug.LogError($"找不到ScriptableObject单例资源: {SingletonName}.asset");
                    }
                }

                return _instance;
            }
        }
    }
}