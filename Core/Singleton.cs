using UnityEngine;

namespace SHIN
{
    public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new object();
        private static bool _applicationQuitting;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _applicationQuitting = false;
        }

        public static T Instance
        {
            get
            {
                lock (_lock)
                {
                    if (_instance != null)
                        return _instance;

                    // 종료 중에는 파괴된 싱글톤을 다시 만들지 않는다.
                    if (_applicationQuitting)
                        return null;

                    _instance = FindFirstObjectByType<T>();
                    if (_instance != null)
                        return _instance;

                    var go = new GameObject(typeof(T).Name);
                    _instance = go.AddComponent<T>();
                    return _instance;
                }
            }
        }

        protected virtual void Awake()
        {
            _applicationQuitting = false;

            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnApplicationQuit()
        {
            _applicationQuitting = true;
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
