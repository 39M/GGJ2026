using UnityEngine;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    private static T _instance;
    private static readonly object _lock = new object();
    protected virtual bool DestroyOnLoad => true;

    public static T Instance
    {
        get
        {
            if (applicationIsQuitting)
            {
                Debug.LogWarning($"实例 [{typeof(T)}] 已被销毁，应用程序正在退出。");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<T>();
                    
                    if (_instance == null)
                    {
                        GameObject singleton = new GameObject($"[{typeof(T).Name}]");
                        _instance = singleton.AddComponent<T>();
                        
                        if (Application.isPlaying && !_instance.DestroyOnLoad)
                            DontDestroyOnLoad(singleton);
                    }
                }
                return _instance;
            }
        }
    }

    private static bool applicationIsQuitting = false;

    protected virtual void Awake()
    {
        if (_instance == null)
        {
            _instance = this as T;
            if (Application.isPlaying && !_instance.DestroyOnLoad)
                DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"检测到重复单例对象：{name}，正在销毁。");
            Destroy(gameObject);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        applicationIsQuitting = false;
    }

    protected virtual void OnApplicationQuit()
    {
        applicationIsQuitting = true;
        _instance = null;
    }
}