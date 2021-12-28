using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Himall.Core;
using System.Net.Http;

namespace Himall.API
{
    /// <summary>
    /// APP端授权
    /// </summary>
    public class WebApiAuthAttribute : AuthorizationFilterAttribute
    {
        public override void OnAuthorization(HttpActionContext actionContext)
        {
            var siteSettingsInfo = Application.SiteSettingApplication.SiteSettings;
            if (siteSettingsInfo != null && !siteSettingsInfo.IsOpenApp)
            {
                string reurl = (actionContext.ActionDescriptor.ControllerDescriptor.ControllerName + "/" + actionContext.ActionDescriptor.ActionName).ToLower();
                bool ischeck = true;
                if (siteSettingsInfo.IsOpenShopApp)
                {
                    if(reurl == "productcontroller/getproductdetail" 
                        || reurl == "logincontroller/getimagecheckcode")//获取图形验证码
                    {
                        ischeck = false;//如当前是产品详细接口，且开启了门店则表示验证app通过（详细页接口可以访问）
                    }
                }

                if (ischeck)
                {
                    HttpResponseMessage result = new HttpResponseMessage();
                    string jsonstr = "{\"IsOpenApp\":\"{0}\"}";
                    jsonstr = jsonstr.Replace("\"{0}\"", "false");
                    result.Content = new StringContent(jsonstr, Encoding.GetEncoding("UTF-8"), "application/json");
                    actionContext.Response = result;
                }
            }
        }
    }
}
