using UnityEngine;

namespace Egsp.Core.SceneManagement
{
    public sealed partial class SceneManager : SingletonRaw<SceneManager>
    {
        private static ILogger Logger => Debug.unityLogger;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InitializeSingleton()
        {
            Logger.Log("Инициализация менеджера сцен.");
            // Создает экземпляр перед полной загрузкой сцены.
            if (!Exist)
                CreateInstance();
        }
        
        protected override bool CanBeDestroyedOutside => false;

        public SceneManager() : base()
        {
            SubscribeOriginalManager();
        }

        protected override void Dispose()
        {
            UnsubscribeOrginalManager();
        }
    }
}