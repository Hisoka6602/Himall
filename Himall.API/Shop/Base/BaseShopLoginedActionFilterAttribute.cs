using Himall.Core.Helper;
using Himall.Web.Framework;
using System;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Himall.Application;

namespace Himall.API
{
    /// <summary>
    /// Action过滤器-过滤冻结
    /// </summary>
    public class BaseShopLoginedActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            string userkey = "";
            userkey = WebHelper.GetQueryString("userkey");
            if (string.IsNullOrWhiteSpace(userkey))
            {
                userkey = WebHelper.GetFormString("userkey");
            }
            long shopuid= UserCookieEncryptHelper.Decrypt(userkey, CookieKeysCollection.USERROLE_SELLERADMIN);
            var shopm = ManagerApplication.GetSellerManager(shopuid);
            if (shopm == null)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "商家信息错误");
            }
            var shop = ShopApplication.GetShop(shopm.ShopId);
            if (shop == null)
            {
                throw new HimallApiException(ApiErrorCode.Parameter_Error, "商家信息错误");
            }
            if (shop.ShopStatus== Entities.ShopInfo.ShopAuditStatus.Freeze)
            {
                throw new HimallApiException(ApiErrorCode.User_Freeze, "商家已冻结");
            }
            base.OnActionExecuting(actionContext);
        }
    }
}
