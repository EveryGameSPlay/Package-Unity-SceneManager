using System;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Egsp.Core.SceneManagement
{
    public class SceneUnloadRequest
    {
        [Flags]
        private enum CompleteFlags : byte
        {
            None = 0,
            This = 1,
            Subs = 2
        }
        
        // data
        private readonly string _name;

        // internal data
        private CompleteFlags _completeFlags;
        
        // actions
        private Action<SceneUnloadRequest> _applyAction;
        private Action<SceneUnloadRequest> _failAction;

        // settings
        private List<SceneUnloadRequest> _subRequests = new List<SceneUnloadRequest>();
        private List<SceneUnloadRequest> _completedScenes = new List<SceneUnloadRequest>();
        private bool _parallel;
        
        // properties
        public Option<Scene> Scene { get; set; }
        public string Name => Scene.IsSome ? Scene.option.name : _name;
        
        // events
        private event Action<SceneUnloadRequest> onCompleted = delegate(SceneUnloadRequest request) {  };
        private event Action onCompletedThis = delegate {  };
        private event Action onCompletedSubs = delegate {  };

        public SceneUnloadRequest(Scene scene, Action<SceneUnloadRequest> applyAction)
        {
            Scene = scene;
            _applyAction = applyAction;
        }

        public SceneUnloadRequest(string name, Action<SceneUnloadRequest> applyAction)
        {
            _name = name;
            _applyAction = applyAction;
        }

        public SceneUnloadRequest With(Scene scene)
        {
            _subRequests.Add(new SceneUnloadRequest(scene, _applyAction));
            return this;
        }

        public SceneUnloadRequest With(string name)
        {
            _subRequests.Add(new SceneUnloadRequest(name, _applyAction));
            return this;
        }
        
        public SceneUnloadRequest Parallel()
        {
            _parallel = true;
            return this;
        }
        
        public SceneUnloadRequest OnComplete(Action<SceneUnloadRequest> completeAction)
        {
            onCompleted += completeAction;
            return this;
        }

        public SceneUnloadRequest OnFail(Action<SceneUnloadRequest> failAction)
        {
            _failAction = failAction;
            return this;
        }

        public void Apply()
        {
            onCompletedThis += ApplySubs;
            onCompletedSubs += TryCompleteAll;
            ApplyThis();
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

        public void Complete()
        {
            CompleteThis();
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
            onCompleted(this);
            onCompleted = delegate(SceneUnloadRequest obj) { };
        }
        
        private void TryCompleteAll()
        {
            if (_completeFlags.HasFlag(CompleteFlags.This) && _completeFlags.HasFlag(CompleteFlags.Subs))
            {
                CompleteAll();
            }
        }
        
        private void SubRequestCompleted(SceneUnloadRequest sceneInfo)
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

        public void Fail()
        {
            _failAction?.Invoke(this);
            _failAction = null;
        }

    }
}