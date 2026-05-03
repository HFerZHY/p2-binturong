using System;
using UnityEngine;
using UnityEditor;

namespace Base
{
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance is null)
                    _instance = FindFirstObjectByType<T>();
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance is not null  && _instance != this)
            {
                Debug.LogWarning("Multiple instances of " + typeof(T).Name + " are not allowed.");
                Destroy(this.gameObject);
            }
            else
            {
                _instance = (T)this;
            }
        }
    }
}