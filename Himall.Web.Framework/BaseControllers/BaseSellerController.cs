using Himall.Application;
using Himall.Core;
using Himall.DTO;
using System.Configuration;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 商城后台基础控制器类
    /// </summary>
    public abstract class BaseSellerController : BaseWebController
    {
        #region 字段
        private Shop _shopInfo;
        #endregion

        #region 属性

        public Shop CurrentShop
        {
            get
            {
                if (_shopInfo == null)
                    _shopInfo = ShopApplication.GetShop(this.CurrentSellerManager.ShopId, true);

                return _shopInfo;
            }
        }
        #endregion


        protected override void OnAuthorization(AuthorizationContext filterContext)
        {
            base.OnAuthorization(filterContext);
            var t = ConfigurationManager.AppSettings["IsInstalled"];
            if (!(null == t || bool.Parse(t)))
            {
                return;
            }
            //不能应用在子方法上
            if (filterContext.IsChildAction)
                return;

            base.OnAuthorization(filterContext);

            //TODO:FG 验证逻辑查询待优化
            if (CheckLoginStatus(filterContext) //检查登录状态
                && CheckAuthorization(filterContext) //检查授权情况
                && CheckRegisterInfo(filterContext)//检查当前商家注册情况 //跳转到第几部
                && CheckShopIsExpired(filterContext)) //检查店铺是否过期
                return;
        }

        /// <summary>
        /// 检查授权情况
        /// </summary>
        /// <param name="filterContext"></param>
        bool CheckAuthorization(AuthorizationContext filterContext)
        {
            var flag = true;
            object[] actionFilter = filterContext.ActionDescriptor.GetCustomAttributes(typeof(UnAuthorize), false);
            if (actionFilter.Length == 1)
                return true;

            string controllerName = filterContext.RouteData.Values["controller"].ToString();
            string actionName = filterContext.RouteData.Values["action"].ToString();

            if (CurrentSellerManager.SellerPrivileges == null || CurrentSellerManager.SellerPrivileges.Count == 0 || !SellerPermission.CheckPermissions(CurrentSellerManager.SellerPrivileges, controllerName, actionName))
            {
                if (Core.Helper.WebHelper.IsAjax())
                {
                    Result result = new Result();
                    result.msg = "你没有访问的权限！";
                    result.success = false;
                    filterContext.Result = Json(result);
                    flag = false;
                }
                else
                {
                    //跳转到错误页
                    var result = new ViewResult()
                    {
                        ViewName = "NoAccess"
                    };
                    result.TempData.Add("Message", "你没有权限访问此页面");
                    result.TempData.Add("Title", "你没有权限访问此页面！");
                    filterContext.Result = result;
                    flag = false;
                }
            }
            return flag;
        }

        /// <summary>
        /// 检查登录状态
        /// </summary>
        /// <param name="filterContext"></param>
        bool CheckLoginStatus(AuthorizationContext filterContext)
        {
            var flag = true;
            if (CurrentSellerManager == null && CurrentUser == null)
            {
                if (Core.Helper.WebHelper.IsAjax())
                {
                    Result result = new Result();
                    result.msg = "登录超时,请重新登录！";
                    result.success = false;
                    filterContext.Result = Json(result);
                    flag = false;
                }
                else
                {
                    HttpRequestBase bases = (HttpRequestBase)filterContext.HttpContext.Request;
                    string url = bases.RawUrl.ToString();
                    string returnurl = System.Web.HttpUtility.HtmlEncode(url);
                    var result = RedirectToAction("", "Login", new
                    {
                        area = "web",
                        returnUrl = filterContext.HttpContext.Request.RawUrl.ToString()
                    });//不带跳转了
                    filterContext.Result = result;
                    flag = false;
                    //跳转到登录页
                }
            }
            else if (CurrentUser != null && CurrentSellerManager == null)
            {
                var result = RedirectToAction("EditProfile0", "ShopProfile", new
                {
                    area = "SellerAdmin"
                });
                filterContext.Result = result;
                flag = false;
            }
            return flag;
        }

        bool CheckShopIsExpired(AuthorizationContext filterContext)
        {
            var flag = true;
            if (ShopApplication.IsExpiredShop(CurrentSellerManager.ShopId))
            {
                string controllerName = filterContext.RouteData.Values["controller"].ToString().ToLower();
                string actionName = filterContext.RouteData.Values["action"].ToString().ToLower();
                string areaName = filterContext.RouteData.DataTokens["area"].ToString().ToLower();

                if ((controllerName == "shop" || controllerName == "accountsettings") && areaName == "selleradmin")
                    return true;//店铺和申请调用控制器返回验证通过

                var result = new ViewResult()
                {
                    ViewName = "IsExpired"
                };
                result.TempData.Add("Message", "你的店铺已过期;");
                result.TempData.Add("Title", "你的店铺已过期！");
                filterContext.Result = result;
                flag = false;
            }
            if (ShopApplication.IsFreezeShop(CurrentSellerManager.ShopId))
            {
                var result = new ViewResult()
                {
                    ViewName = "IsFreeze"
                };
                result.TempData.Add("Message", "抱歉，你的店铺已冻结，请与平台管理员联系…");
                result.TempData.Add("Title", "你的店铺已冻结！");
                filterContext.Result = result;
                flag = false;
            }
            return flag;
        }

        /// <summary>
        /// 检查当前商家注册情况
        /// </summary>
        /// <param name="filterContext"></param>
        bool CheckRegisterInfo(AuthorizationContext filterContext)
        {
            var flag = true;
            if (filterContext.IsChildAction || Core.Helper.WebHelper.IsAjax())
                return flag;
            string controllerName = filterContext.RouteData.Values["controller"].ToString().ToLower();
            string actionName = filterContext.RouteData.Values["action"].ToString().ToLower();
            string areaName = filterContext.RouteData.DataTokens["area"].ToString().ToLower();
            var shop = ShopApplication.GetShop(CurrentSellerManager.ShopId);
            int stage = (int)shop.Stage;
            if ((shop.Stage != Entities.ShopInfo.ShopStage.Finish || shop.ShopStatus == Entities.ShopInfo.ShopAuditStatus.WaitConfirm) && filterContext.RequestContext.HttpContext.Request.HttpMethod.ToUpper() != "POST")
            {
                if (actionName.IndexOf("step") != 0)
                {
                    //如果当前action是已经是对应的值则不要跳转，否则将进入死循环
                    if (actionName != ("EditProfile" + stage).ToLower())
                    {
                        var result = RedirectToAction("EditProfile" + stage, "ShopProfile", new
                        {
                            area = "SellerAdmin"
                        });
                        filterContext.Result = result;
                        flag = false;
                    }
                }
            }
            return flag;
        }
    }
}
