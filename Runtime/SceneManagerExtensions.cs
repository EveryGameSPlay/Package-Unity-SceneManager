using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Egsp.Core.SceneManagement
{
    public sealed partial class SceneManager
    {
        public static IReadOnlyList<SceneUnloadRequest> UnloadByTag(string tag)
        {
            var list = UnloadScene(GetScenesByTag(tag).Select(x => x.Scene).ToList());
            for (var i = 0; i < list.Count; i++)
            {
                list[i].Apply();
            }

            return list;
        }

        /// <summary>
        /// Возвращает данные переданные сцене.
        /// </summary>
        public static Option<T> GetData<T>(Scene scene)
        {
            var coincidence = _scenes.FirstOrNone(x => x.Scene == scene);

            if (coincidence)
            {
                var sceneDataO = coincidence.option.Value.Data;
                
                if(sceneDataO)
                    if (sceneDataO.option is T data)
                        return data;

            }
            
            return Option<T>.None;
        }
    }
}