using Himall.Core;
using Himall.Service;

namespace Himall.Application
{
    public class AppBaseApplication
    {
        static AppBaseService _AppBaseService = ObjectContainer.Current.Resolve<AppBaseService>();

        /// <summary>
        /// 通过appkey获取AppSecret
        /// </summary>
        /// <param name="appkey"></param>
        /// <returns></returns>
        public static string GetAppSecret(string appkey)
        {
            return _AppBaseService.GetAppSecret(appkey);
        }
    }
}
