namespace NetworkedDemo
{
    using UnityEngine;

    /// <summary>
    /// Base class for components that must behave as singletons - being unique in the scene and having a static instance property for easy access.
    /// </summary>
    /// <typeparam name="T">The type of the deriving component.</typeparam>
    /// <seealso cref="UnityEngine.MonoBehaviour" />
    [DisallowMultipleComponent]
    internal abstract class SingletonMonoBehaviour<T> : MonoBehaviour where T : SingletonMonoBehaviour<T>
    {
        private static T _instance;

        /// <summary>
        /// Gets the singleton instance.
        /// </summary>
        /// <value>
        /// The instance.
        /// </value>
        internal static T instance
        {
            get
            {
                if (ReferenceEquals(_instance, null))
                {
                    _instance = FindObjectOfType(typeof(T)) as T;
                }

                return _instance;
            }
        }

        /// <summary>
        /// Called by Unity when this instance awakes.
        /// </summary>
        private void Awake()
        {
            if (!ReferenceEquals(_instance, null) && _instance != this)
            {
                Debug.LogWarning(this.ToString() + " another instance has already been registered for this scene, destroying this one");
                Destroy(this);
                return;
            }

            _instance = (T)this;
            OnAwake();
        }

        private void OnDestroy()
        {
            if (ReferenceEquals(_instance, this))
            {
                OnDestroying();

                _instance = null;
            }
        }

        protected virtual void OnAwake()
        {
            /* NOOP */
        }

        protected virtual void OnDestroying()
        {
            /* NOOP */
        }
    }
}