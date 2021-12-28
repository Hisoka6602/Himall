using Himall.CommonModel;
using Himall.DTO;
using System;
using System.Web;

namespace Himall.Web
{
    public class SceneHelper
    {
        private static System.Web.Caching.Cache _cache = HttpRuntime.Cache;
        public SceneModel GetModel(string sceneid)
        {
            var cachkey = CacheKeyCollection.SceneState(sceneid);
            var sceneObj = _cache.Get(cachkey);
            if (sceneObj != null)
            {
                return (SceneModel)sceneObj;
            }
            return null;
        }

        public SceneModel GetModel(int sceneid)
        {
            return GetModel(sceneid.ToString());
        }

        /// <summary>
        /// 设置场景Model
        /// </summary>
        /// <param name="model"></param>
        /// <param name="expireTime"></param>
        /// <returns>场景ID</returns>
        public int SetModel(SceneModel model, int expireTime = 600)
        {
            var sceneid = model.GetHashCode();
            var cachekey = CacheKeyCollection.SceneState(sceneid.ToString());
            //Core.Cache.Insert( cachekey , model , expireTime );
            _cache.Insert(cachekey, model, null, DateTime.MaxValue, TimeSpan.FromSeconds(expireTime), System.Web.Caching.CacheItemPriority.NotRemovable, null);
            var sceneObj = Core.Cache.Get<SceneModel>(cachekey);
            return sceneid;
        }

        /// <summary>
        /// 移除场景Model
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        public void RemoveModel<T>(string key)
        {
            var cachkey = CacheKeyCollection.SceneState(key);
            _cache.Remove(cachkey);
        }
    }
}