using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Egsp.Core.SceneManagement
{
    /// <summary>
    /// Запрос на загрузку сцены.
    /// </summary>
    public class SceneRequest
    {
        [Flags]
        private enum CompleteFlags : byte
        {
            None = 0,
            This = 1,
            Subs = 2
        }
        
        public readonly string Name;
        public readonly LoadSceneMode Mode;
        public readonly DateTime RequestTime;
        
        // data
        private List<string> _tags = new List<string>();
        private Option<object> _data;

        // internal data
        private CompleteFlags _completeFlags;

        // actions
        private Action<SceneRequest> _applyAction;
        private Action<SceneRequest> _failAction;

        // settings
        private List<SceneRequest> _subRequests = new List<SceneRequest>();
        private List<SceneInfo> _completedScenes = new List<SceneInfo>();
        private SceneInfo _sceneInfo;
        private bool _parallel;
        private bool _dataForAllScenes;

        // properties
        public IReadOnlyList<string> Tags => _tags;
        public Option<object> StoredData => _data;

        // events
        // Вызывать для внутреннего использования.
        private event Action<SceneInfo> onCompleted = delegate(SceneInfo request) {  };
        private event Action onCompletedThis = delegate {  };
        private event Action onCompletedSubs = delegate {  };
        
        public SceneRequest(string name, LoadSceneMode mode, DateTime requestTime, Action<SceneRequest> applyAction)
        {
            Name = name;
            Mode = mode;
            RequestTime = requestTime;

            _applyAction = applyAction;
        }

        public SceneRequest With(string sceneName)
        {
            _subRequests.Add(new SceneRequest(sceneName, LoadSceneMode.Additive, DateTime.Now, _applyAction));
            return this;
        }
        
        /// <param name="requestAction">Действие, которому будет передан дополнительный запрос.
        /// Применять .Apply() не рекомендуется. Сначала будут загружены дополнительные сцены.</param>
        public SceneRequest With(string sceneName, Action<SceneRequest> requestAction)
        {
            var request = new SceneRequest(sceneName, LoadSceneMode.Additive, DateTime.Now, _applyAction);
            requestAction?.Invoke(request);
            _subRequests.Add(request);
            return this;
        }

        /// <summary>
        /// Добавляет тэг к информации о сцене.
        /// </summary>
        public SceneRequest Tag(params string[] tags)
        {
            for (var i = 0; i < tags.Length; i++)
            {
                if (_tags.Contains(tags[i]))
                    continue;
                
                _tags.Add(tags[i]);
            }
            return this;
        }

        /// <summary>
        /// Сцены будут загружаться параллельно. Однако событие завершения будет вызвано только после загрузки всех сцен.
        /// </summary>
        public SceneRequest Parallel()
        {
            _parallel = true;
            return this;
        }

        /// <summary>
        /// Добавляет данные к запросу. Данные можно перезаписать до вызова .Apply().
        /// </summary>
        /// <param name="dataForAllScenes">Применяет данные ко всем запросам.</param>
        public SceneRequest Data(object data, bool dataForAllScenes = true)
        {
            _data = data;
            _dataForAllScenes = dataForAllScenes;
            return this;
        }

        public SceneRequest OnComplete(Action<SceneInfo> completeAction)
        {
            onCompleted += completeAction;
            return this;
        }

        public SceneRequest OnFail(Action<SceneRequest> failAction)
        {
            _failAction = failAction;
            return this;
        }
        
        /// <summary>
        /// Завершить создание формирование запроса и начать загрузку сцены.
        /// </summary>
        public void Apply()
        {
            // На этой стадии мы определяем порядок загрузки относительно главной сцены и подсцен.
            
            if (_subRequests.Count == 0)
            {
                onCompletedThis += ApplySubs;
                onCompletedSubs += TryCompleteAll;
                ApplyThis();
                return;
            }

            // В одиночном режиме сначала нужно загружать главную сцену.
            // Порядок загрузки подсцен определяется в ApplySubs.
            if (_parallel && Mode == LoadSceneMode.Additive)
            {
                onCompletedThis += TryCompleteAll;
                onCompletedSubs += TryCompleteAll;
                ApplyThis();
                ApplySubs();
                return;
            }
            
            // Standard Single or Additive
            onCompletedThis += ApplySubs;
            onCompletedSubs += TryCompleteAll;
            ApplyThis();
        }

        public void Complete(SceneInfo sceneInfo)
        {
            _sceneInfo = sceneInfo;
            CompleteThis();
        }

        public void Fail()
        {
            _failAction?.Invoke(this);
            _failAction = null;
        }

        private void ApplyThis()
        {
            _applyAction?.Invoke(this);
            _applyAction = null;
        }

        private void ApplySubs()
        {
            if (_subRequests.Count == 0)
            {
                CompleteSubs();
                return;
            }
            
            // data
            if (_dataForAllScenes)
            {
                foreach (var sceneRequest in _subRequests)
                {
                    // Если данные не были установлены до.
                    if (sceneRequest._data.IsNone)
                    {
                        // Добавляем данные к сцене. 
                        // Она в свою очередь добавит их к своим сценам.
                        sceneRequest.Data(_data.option, true);
                    }
                }
            }
            
            // loading
            if (_parallel)
            {
                foreach (var sceneRequest in _subRequests)
                {
                    sceneRequest.onCompleted += SubRequestCompleted;
                    sceneRequest.Apply();
                }   
            }
            else
            {
                for (var i = 0; i < _subRequests.Count-1; i++)
                {
                    // Next request.
                    var index = i+1;
                    var sceneRequest = _subRequests[i];
                    
                    sceneRequest.onCompleted += SubRequestCompleted;
                    sceneRequest.onCompleted += (x) => ApplyIndex(index);
                }

                _subRequests[_subRequests.Count - 1].onCompleted += SubRequestCompleted;
                _subRequests[0].Apply();
            }
        }

        private void ApplyIndex(int index)
        {
            _subRequests[index].Apply();
        }

        private void CompleteThis()
        {
            _completeFlags |= CompleteFlags.This;
            onCompletedThis?.Invoke();
        }

        private void CompleteSubs()
        {
            _completeFlags |= CompleteFlags.Subs;
            onCompletedSubs?.Invoke();
        }

        private void CompleteAll()
        {
            onCompleted(_sceneInfo);
            onCompleted = delegate(SceneInfo request) { };
        }

        private void TryCompleteAll()
        {
            if (_completeFlags.HasFlag(CompleteFlags.This) && _completeFlags.HasFlag(CompleteFlags.Subs))
            {
                CompleteAll();
            }
        }
        
        private void SubRequestCompleted(SceneInfo sceneInfo)
        {
            _completedScenes.Add(sceneInfo);

            if (SubRequestsCompletionCheck())
            {
                // Теперь можно выполнить текущий запрос.
                CompleteSubs();
            }
        }

        private bool SubRequestsCompletionCheck()
        {
            if (_subRequests.Count == _completedScenes.Count)
                return true;

            return false;
        }
        
    }
}