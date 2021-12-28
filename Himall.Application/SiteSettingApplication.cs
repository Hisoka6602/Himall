using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Web;

namespace Himall.Application
{
    //TODO:FG 转移Servic对Application的引用
    public class SiteSettingApplication : BaseApplicaion<SiteSettingService>
    {
        public static SiteSettings SiteSettings
        {
            get
            {
                var settings = (SiteSettings)CallContext.GetData(CacheKeyCollection.SiteSettings);
                if (settings == null)
                {
                    settings = Service.SiteSettings;
                    CallContext.SetData(CacheKeyCollection.SiteSettings, settings);
                }
                return settings;
            }
        }

        /// <summary>
        /// 保存对配置的修改
        /// </summary>
        public static void SaveChanges()
        {
            var current = SiteSettings;
            var data = Service.GetSiteSettings();

            var changes = new Dictionary<string, string>();
            var properties = typeof(SiteSettings).GetProperties();

            foreach (var property in properties)
            {
                var key = property.Name;
                var oldValue = data.FirstOrDefault(p => p.Key == key)?.Value ?? string.Empty;
                var newValue = property.GetValue(current)?.ToString() ?? string.Empty;

                if (oldValue != newValue) changes.Add(key, newValue);
            }

            if (changes.Count > 0)
            {
                Service.SaveSettings(changes);
                Cache.Remove(CacheKeyCollection.SiteSettings);//清空配置缓存
            }
        }

        /// <summary>
        /// 判断地址是否有https或http，如没有根据请求地址返回前面加上https或http返回
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetUrlHttpOrHttps(string url)
        {
            if (!string.IsNullOrEmpty(url) && url.IndexOf("https://") == -1 && url.IndexOf("http://") == -1)
            {
                if (HttpContext.Current == null || HttpContext.Current.Request == null)
                    return "http://" + url;

                return HttpContext.Current.Request.Url.ToString().IndexOf("https://") != -1 ? "https://" : "http://" + url;
            }
            return url;
        }

        /// <summary>
        /// 获取域名地址(优先获取当前请求的域名地址，如没有再获取后台配置的站点域名地址)
        /// </summary>
        /// <returns></returns>
        public static string GetCurDomainUrl()
        {
            string url = "";
            if (HttpContext.Current != null && HttpContext.Current.Request != null)
            {
                var scheme = WebHelper.GetScheme().ToLower();

                var port = WebHelper.GetPort();
                if (port == "80" && scheme == "http") { port = ""; }
                if (port == "443" && scheme == "https") { port = ""; }
                port = string.IsNullOrEmpty(port) ? string.Empty : ":" + port;
                url = scheme + "://" + WebHelper.GetHost() + port;

            }

            if (string.IsNullOrEmpty(url))
                url = GetUrlHttpOrHttps(SiteSettings.SiteUrl);//站点域名


            if (!string.IsNullOrEmpty(url) && (url.Length - 1) == url.LastIndexOf('/'))
                url = url.Substring(0, url.Length - 1);//如果最后一位“/”结束，去掉“/”;            

            return url;
        }

        /// <summary>
        /// 获取mapkey的js字符串
        /// </summary>
        /// <returns></returns>
        public static string GetJsMapQQKey()
        {
            return "<script charset=\"utf-8\" src=\"https://map.qq.com/api/js?v=2.exp&libraries=convertor&key=" + SiteSettings.QQMapAPIKey + "\"></script>";
        }

        /// <summary>
        /// 获取IP地址
        /// </summary>
        /// <returns></returns>
        public static string getIPAddress()
        {
            return WebHelper.GetIP();
        }
    }
}
