using Himall.Core.Plugins.OAuth;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Mvc;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.Application;
using Himall.CommonModel;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class OAuthController : BaseAdminController
    {
        // GET: Admin/OAuth
        public ActionResult Management()
        {
            var paymentPlugins = PluginsManagement.GetPlugins<IOAuthPlugin>();

            var data = paymentPlugins.Select(item =>
            {
                dynamic model = new ExpandoObject();
                model.name = item.PluginInfo.DisplayName;
                model.pluginId = item.PluginInfo.PluginId;
                model.enable = item.PluginInfo.Enable;
                return model;
            }
                );
            return View(data);
        }



        public ActionResult Edit(string pluginId)
        {
            ViewBag.Id = pluginId;

            var oauthPlugin = PluginsManagement.GetPlugin<IOAuthPlugin>(pluginId);
            ViewBag.Name = oauthPlugin.PluginInfo.DisplayName;
            var formData = oauthPlugin.Biz.GetFormData();
            
            return View(formData);
        }

        [HttpPost]
        [UnAuthorize]
        [ValidateInput(false)]
        public JsonResult Save(string pluginId, string values)
        {
            var oauthPlugin = PluginsManagement.GetPlugin<IOAuthPlugin>(pluginId);
            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<IEnumerable<KeyValuePair<string, string>>>(values);
            oauthPlugin.Biz.SetFormValues(items);
            return Json(new { success = true });
        }



        [HttpPost]
        [UnAuthorize]
        public JsonResult Enable(string pluginId, bool enable)
        {
            Result result = new Result();
            PluginsManagement.EnablePlugin(pluginId, enable);
            result.success = true;
            Cache.Remove(CacheKeyCollection.CACHE_OAUTHLIST);//清空前端信任登录列表缓存
            return Json(result);
        }
    }
}