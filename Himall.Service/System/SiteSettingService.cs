using Himall.CommonModel;
using Himall.Entities;
using NetRube.Data;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Collections.Generic;
using Himall.DTO;
using System.Runtime.Remoting.Messaging;
using Himall.Core;
using System.Web;
using Himall.Core.Helper;

namespace Himall.Service
{
    public class SiteSettingService : ServiceBase<SiteSettingInfo>
    {
        public List<SiteSettingInfo> GetSiteSettings()
        {
            return GetList(d => true);
        }

        public void SaveSettings(Dictionary<string, string> settings)
        {
            var keys = settings.Keys.ToList();
            var models = DbFactory.Default.Get<SiteSettingInfo>().Where(p => p.Key.ExIn(keys)).ToList();
            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in settings)
                {
                    var model = models.FirstOrDefault(p => p.Key == item.Key);
                    if (model != null)
                    {
                        model.Value = item.Value;
                        DbFactory.Default.Update(model);
                    }
                    else
                    {
                        DbFactory.Default.Add(new SiteSettingInfo
                        {
                            Key = item.Key,
                            Value = item.Value
                        });
                    }
                }
            });
        }

        private SiteSettings InitSettings()
        {
            var settings = new SiteSettings();
            var properties = typeof(SiteSettings).GetProperties();

            var data = GetSiteSettings();
            foreach (var property in properties)
            {
                var temp = data.FirstOrDefault(item => item.Key == property.Name);
                if (temp != null)
                    property.SetValue(settings, Convert.ChangeType(temp.Value, property.PropertyType));
            }
            return settings;
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
        public string GetCurDomainUrl()
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

        public SiteSettings SiteSettings
        {
            get
            {
                SiteSettings settings = Cache.Get<SiteSettings>(CacheKeyCollection.SiteSettings);//缓存中获取
                if (settings == null)
                {
                    settings = InitSettings();//数据库中加载
                    Cache.Insert(CacheKeyCollection.SiteSettings, settings);
                }
                return settings;
            }
        }
    }
}
