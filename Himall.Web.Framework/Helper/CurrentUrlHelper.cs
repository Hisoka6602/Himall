using Himall.Core.Helper;

namespace Himall.Web.Framework
{
    public class CurrentUrlHelper
    {
        /// <summary>
        /// 取站点域名：Scheme://Host
        /// </summary>
        /// <returns></returns>
        public static string CurrentUrlNoPort()
        {
            return GetScheme() + "://" + WebHelper.GetHost();
        }
        /// <summary>
        /// 取站点域名，带端口：Scheme://Host:Port
        /// </summary>
        /// <returns></returns>
        public static string CurrentUrl()
        {
            var scheme = GetScheme().ToLower();
            var port = WebHelper.GetPort();
            if (port == "80" && (scheme == "http" || scheme == "https")) { port = ""; }//因为前面判断https则这里也加上或https
            if (port == "443" && scheme == "https") { port = ""; }
            port = string.IsNullOrEmpty(port) ? string.Empty : ":" + port;
            return scheme + "://" + WebHelper.GetHost() + port;
        }

        /// <summary>
        /// 取站点Scheme
        /// </summary>
        /// <returns></returns>
        public static string GetScheme()
        {
            var websecheme = WebHelper.GetScheme();

            #region 有时可能cdn原因，访问的域名虽然是https，它可能是域名解析跳转则直接获取网址域名它还是http，所以下面判断当配置的是https则让它返回https
            if (!string.IsNullOrEmpty(websecheme) && websecheme == "http")
            {
                var curdomainUrl = Application.SiteSettingApplication.SiteSettings.SiteUrl;
                if (!string.IsNullOrEmpty(curdomainUrl) && curdomainUrl.IndexOf("https://") != -1)
                    websecheme = "https";//域名配置的是https，而直接获取的域名头部是http，则这里回调还是取用https返回
            }
            #endregion

            return websecheme;
        }

    }
}