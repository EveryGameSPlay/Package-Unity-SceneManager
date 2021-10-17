using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Original = UnityEngine.SceneManagement.SceneManager;
namespace Egsp.Core.SceneManagement
{
    public sealed partial class SceneManager
    {
        /// <summary>
        /// Существует ли сцена в билде. (Build Settings)
        /// </summary>
        private static SceneExistInBuildResult SceneExistInBuildInternal(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return SceneExistInBuildResult.IncorrectName;
            
            for (int i = 0; i < Original.sceneCountInBuildSettings; i++)
            {
                var scenePath = SceneUtility.GetScenePathByBuildIndex(i);
                var lastSlash = scenePath.LastIndexOf("/", StringComparison.Ordinal);
                var sceneName =
                    scenePath.Substring(lastSlash + 1,
                        scenePath.LastIndexOf(".", StringComparison.Ordinal) - lastSlash - 1);

                if (String.Compare(name, sceneName, StringComparison.OrdinalIgnoreCase) == 0)
                    return SceneExistInBuildResult.Exist;
            }

            return SceneExistInBuildResult.NotExist;
        }

        public static bool SceneExistInBuild(string name)
        {
            var logger = SceneManager.Exist ? SceneManager.Logger : Debug.unityLogger;
            
            switch (SceneExistInBuildInternal(name))
            {
                case SceneExistInBuildResult.Exist:
                    return true;
                case SceneExistInBuildResult.NotExist:
                    logger.LogWarning("Scene",$"Scene: {name} doesnt exist");
                    return false;
                case SceneExistInBuildResult.IncorrectName:
                    logger.LogWarning("Scene",$"Incorrect scene name");
                    return false;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Option<SceneInfo> GetSceneInfo(Scene scene) =>
            Scenes.FirstOrNone(x => x.Scene == scene);
        
        public static IReadOnlyList<SceneInfo> GetScenesByTag(string tag) =>
            Scenes.Where(x => x.Tags.Contains(tag)).ToList();
    }
}