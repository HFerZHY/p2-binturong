using UnityEngine;

namespace Base
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;
        private static bool _isQuitting;

        protected virtual bool Persistent => false;

        public static T Instance
        {
            get
            {
                if (_isQuitting) return null;
                if (_instance is null)
                    Debug.LogError($"No instance of {typeof(T).Name} found in scene.");
                return _instance;
            }
        }
        
        // In MonoSingleton<T>
        protected virtual void DestroyDuplicate(GameObject go) => Destroy(go);

        protected virtual void Awake()
        {
            if (_instance is not null && _instance != this)
            {
                Debug.LogWarning($"Duplicate {typeof(T).Name} destroyed.");
                DestroyDuplicate(gameObject);
                return;
            }

            _instance = (T)this;
            if (Persistent) DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        private void OnApplicationQuit() => _isQuitting = true;
    }
}