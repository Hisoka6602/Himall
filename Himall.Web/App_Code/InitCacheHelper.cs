using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Entities;
using Himall.Service;

namespace Himall.Web.App_Code.Common
{
    /// <summary>
    /// 加载缓存处理类
    /// </summary>
    public class InitCacheHelper
    {
        /// <summary>
        /// 初始化缓存
        /// </summary>
        public static void InitCache()
        {
            //加载移动端当前首页模版
            var curr = ObjectContainer.Current.Resolve<TemplateSettingsService>().GetCurrentTemplate(0);
            Core.Cache.Insert<TemplateVisualizationSettingInfo>(CacheKeyCollection.MobileHomeTemplate("0"), curr);
        }
    }
}