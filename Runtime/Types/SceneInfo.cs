using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Egsp.Core.SceneManagement
{
    public class SceneInfo
    {
        /// <summary>
        /// Экземпляр загруженной сцены.
        /// </summary>
        public readonly Scene Scene;
        /// <summary>
        /// Время со старта сцены.
        /// </summary>
        public readonly DateTime StartTime;
        /// <summary>
        /// Информация о загрузке сцены.
        /// </summary>
        public readonly Option<SceneLoadInfo> LoadInfo;
        
        // data
        public readonly IReadOnlyList<string> Tags;
        public readonly Option<object> Data;

        public SceneInfo(Scene scene, DateTime startTime, SceneLoadInfo loadInfo)
        {
            Scene = scene;
            StartTime = startTime;
            LoadInfo = loadInfo;

            Tags = loadInfo.Request.Tags;
            Data = loadInfo.Request.StoredData;
        }

        /// <summary>
        /// Сцена будет помечена как неизвестная, если не указать информацию о загрузке.
        /// </summary>
        public SceneInfo(Scene scene, DateTime startTime, params string[] tags)
        {
            Scene = scene;
            StartTime = startTime;
            
            LoadInfo = Option<SceneLoadInfo>.None;

            Tags = tags.ToList();
            Data = Option<object>.None;
        }
    }
}