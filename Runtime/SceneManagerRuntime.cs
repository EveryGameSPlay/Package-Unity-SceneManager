using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;
using Original = UnityEngine.SceneManagement.SceneManager;

namespace Egsp.Core.SceneManagement
{
    // TODO: Добавить проверку на провал перед исполнением древа запросов. Если в древе хоть один запрос не исполнится, то лучше не исполнять древо полностью. Добавить также настройку игнора такой ситуации.
    // TODO: Добавить ожидание завершения дел у сцен перед их выгрузкой.
    
    public sealed partial class SceneManager
    {
        /// <summary>
        /// Сцены в стадии загрузки. Этот список нужен для идентификации загружаемых сцен.
        /// </summary>
        private LinkedList<SceneLoadInfo> _loadingScenes = new LinkedList<SceneLoadInfo>();
        
        /// <summary>
        /// Рабочие сцены, которые уже загружены.
        /// </summary>
        private LinkedList<SceneInfo> _workingScenes = new LinkedList<SceneInfo>();

        /// <summary>
        /// Сцены в стадии выгрузки. Этот список нужен для идентификации выгружаемых сцен.
        /// </summary>
        private LinkedList<SceneUnloadRequest> _unloadingScenes = new LinkedList<SceneUnloadRequest>();

        private static LinkedList<SceneInfo> _scenes => Instance._workingScenes;
        
        /// <summary>
        /// Все действующие сцены.
        /// </summary>
        public static IReadOnlyList<SceneInfo> Scenes => Instance._workingScenes.ToList();

        #region Public
        /// <summary>
        /// Загрузка сцены и оформление запроса.
        /// </summary>
        public static SceneRequest LoadScene(string name, LoadSceneMode mode) => Instance?.LoadSceneInstance(name, mode);
        
        private SceneRequest LoadSceneInstance(string name, LoadSceneMode mode)
        {
            var request = new SceneRequest(name, mode, DateTime.Now, RequestApply);
            return request;
        }

        /// <summary>
        /// Создает запрос на выгрузку сцены.
        /// </summary>
        public static SceneUnloadRequest UnloadScene(string name) => Instance?.UnloadSceneInstance(name);
        
        /// <summary>
        /// Создает запрос на выгрузку сцены.
        /// </summary>
        public static SceneUnloadRequest UnloadScene(Scene scene) => Instance?.UnloadSceneInstance(scene);

        public static IReadOnlyList<SceneUnloadRequest> UnloadScene(ICollection<Scene> scenes)
        {
            var requests = new List<SceneUnloadRequest>(scenes.Count);
            foreach (var scene in scenes)
            {
                requests.Add(Instance?.UnloadSceneInstance(scene));
            }

            return requests;
        }

        public static IReadOnlyList<SceneUnloadRequest> UnloadScene(ICollection<string> scenes)
        {
            var requests = new List<SceneUnloadRequest>(scenes.Count);
            foreach (var scene in scenes)
            {
                requests.Add(Instance?.UnloadSceneInstance(scene));
            }

            return requests;
        }

        private SceneUnloadRequest UnloadSceneInstance(string name)
        {
            var request = new SceneUnloadRequest(name, x => UnloadRequestApply(x));
            return request;
        }

        private SceneUnloadRequest UnloadSceneInstance(Scene scene)
        {
            var request = new SceneUnloadRequest(scene, x => UnloadRequestApply(x));
            return request;
        }

        #endregion
        
        #region Events
        private void SubscribeOriginalManager()
        {
            Original.sceneLoaded += OnSceneLoaded;
            Original.sceneUnloaded += OnSceneUnloaded;
        }

        private void UnsubscribeOrginalManager()
        {
            Original.sceneLoaded -= OnSceneLoaded;
            Original.sceneUnloaded -= OnSceneUnloaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            ProceedLoadedScene(scene);
        }
        
        private void OnSceneUnloaded(Scene scene)
        {
            ProceedUnloadedScene(scene);
        }
        #endregion
        
        #region Private
        
        /// <summary>
        /// Вызывается при применении запроса.
        /// </summary>
        private void RequestApply(SceneRequest request)
        {
            ProceedLoadRequest(request);
            Logger.Log($"Запрос на загрузку сцены принят: {request.Name}");
        }

        private void UnloadRequestApply(SceneUnloadRequest request)
        {
            ProceedUnloadRequest(request);
        }
        

        /// <summary>
        /// Обработка запроса на загрузку сцены.
        /// </summary>
        private void ProceedLoadRequest(SceneRequest request)
        {
            if (!SceneExistInBuild(request.Name))
            {
                request.Fail();
                return;
            }
            
            var loadInfo = new SceneLoadInfo(request);
            _loadingScenes.AddLast(loadInfo);
            
            Original.LoadSceneAsync(loadInfo.Request.Name, loadInfo.Request.Mode);
        }
        
        
        private void ProceedUnloadRequest(SceneUnloadRequest request)
        {
            Option<LinkedListNode<SceneInfo>> sceneInfoO;
            if (request.Scene.IsSome)
            { 
                // Поиск по экземпляру.
                sceneInfoO = 
                    _workingScenes.FirstOrNone(x => x.Scene == request.Scene.option);
            }
            else
            {
                // Поиск по названию.
                sceneInfoO = 
                    _workingScenes.FirstOrNone(x => x.Scene.name == request.Name);
            }

            if (sceneInfoO.IsSome)
            {
                var sceneInfo = sceneInfoO.option.Value;
                _workingScenes.Remove(sceneInfoO.option);
                
                Logger.Log($"Сцена {request.Name} - добавлена в очередь на выгрузку по экземпляру.");
                _unloadingScenes.AddLast(request);
                request.Scene = sceneInfo.Scene;
                Original.UnloadSceneAsync(request.Scene.option);
            }
            else
            {
                Logger.LogWarning("Scene", $"Сцена {request.Name} - не найдена в списках рабочих." +
                                           $" Поэтому выгрузки совершено не будет.");
                request.Fail();
            }
        }
        

        private void ProceedLoadedScene(Scene scene)
        {
            if (_workingScenes.FirstOrNone(x => x.Scene == scene).IsSome)
            {
                Logger.LogWarning("Scene", $"Сцена {scene.name} - уже обработана загрузчиком сцен.");
            }
            
            var loadingRegistry = RemoveFromLoading(scene);

            SceneInfo sceneInfo = null;
            // Если запрос на загрузку сцены был оформлен.
            if (loadingRegistry.IsSome)
            { 
                Logger.Log($"Сцена: {scene.name} - была загружена.");
                sceneInfo = new SceneInfo(scene, DateTime.Now, loadingRegistry.option);
                _workingScenes.AddLast(sceneInfo);
                sceneInfo.LoadInfo.option.Request.Complete(sceneInfo);
            }
            // Если запрос на загрузку сцены не был оформлен. UNKNOWN SCENE
            else
            {
                Logger.LogWarning("Scene",
                    $"Сцена: {scene.name} - была загружена извне, без использования Egsp.Core.SceneManagement!" +
                    $" Она будет помечена как неизвестная, но обработана как запрошенная.");
                
                string tag = null;
                if (_workingScenes.Count == 0)
                {
                    tag = "root";
                }
                else
                {
                    tag = "unknown";
                }

                sceneInfo = new SceneInfo(scene, DateTime.Now, tag);
                _workingScenes.AddLast(sceneInfo);
            }
            
            
          
            // Убирает сцену из списка загружаемых, если она была туда занесена.
            Option<SceneLoadInfo> RemoveFromLoading(Scene scene)
            {
                // На момент написания это единственный способ безопасно определить наличие объекта.
                var sceneLoadInfoO = _loadingScenes
                    .FirstOrNone((x) => x.Request.Name == scene.name);
            
                if(sceneLoadInfoO.IsNone)
                    return Option<SceneLoadInfo>.None;
                
                // Extracting value from node and option
                var sceneLoadInfo = sceneLoadInfoO.option.Value;
                // Remove from loading
                _loadingScenes.Remove(sceneLoadInfoO.option);
            
                return sceneLoadInfo;
            }
        }
        
        
        private void ProceedUnloadedScene(Scene scene)
        {
            var unloadingRegistry = RemoveFromUnloading(scene);

            if (unloadingRegistry.IsSome)
            {
                unloadingRegistry.option.Complete();
                Logger.Log($"Сцена: {scene.name} - была выгружена.");
            }
            else
            {
                var working = _workingScenes.FirstOrNone(x => x.Scene == scene);
                if (working.IsSome)
                {
                    _workingScenes.Remove(working.option);
                }
            }
                
            
            // Убирает сцену из списка выгружаемых, если она была туда занесена.
            Option<SceneUnloadRequest> RemoveFromUnloading(Scene scene)
            {
                // На момент написания это единственный способ безопасно определить наличие объекта.
                var sceneInfoO = _unloadingScenes
                    .FirstOrNone((x) => x.Scene.option == scene);
            
                if(sceneInfoO.IsNone)
                    return Option<SceneUnloadRequest>.None;
                
                var sceneInfo = sceneInfoO.option.Value;
                // Remove from loading
                _unloadingScenes.Remove(sceneInfoO.option);
            
                return sceneInfo;
            }
        }
        #endregion
    }
}