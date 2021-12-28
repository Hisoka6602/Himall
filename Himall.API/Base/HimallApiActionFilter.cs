using Himall.Web.Framework;
using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Himall.API
{
    /// <summary>
    /// Action过滤器-过滤基础参数与签名
    /// </summary>
    public class HimallApiActionFilter: ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            if (String.IsNullOrEmpty(ApiHelper.HostUrl))
            {
                //装载当前URI
                ApiHelper.HostUrl = Application.SiteSettingApplication.GetCurDomainUrl();
            }

            var data = ApiHelper.GetSortedParams(actionContext.Request);
            ApiHelper.CheckBaseParamsAndSign(data);
            base.OnActionExecuting(actionContext);
        }
    }
}
