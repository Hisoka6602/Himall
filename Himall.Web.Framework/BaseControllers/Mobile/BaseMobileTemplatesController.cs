using Himall.Core.Helper;
using System.Web.Mvc;
using Himall.Application;
using Himall.CommonModel;
using System.Text.RegularExpressions;
using System;
using Himall.Core;

namespace Himall.Web.Framework
{
    /// <summary>
    /// 移动端控制器基类(带模板)
    /// </summary>
    public abstract class BaseMobileTemplatesController : BaseMobileController
    {
        /// <summary>
        /// 微信小程序信任登录服务标识
        /// </summary>
        public const string SmallProgServiceProvider = "WeiXinSmallProg";
        /// <summary>
        /// 当前销售员
        /// </summary>
        protected long? CurrentSpreadId { get; set; }
        /// <summary>
        /// 是否需要处理分销微信分享
        /// </summary>
        protected bool NeedDistributionWeiXinShare { get; set; }
        /// <summary>
        /// 前置处理
        /// </summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (Request != null && WebHelper.GetUrl().ToLower().Contains("register/getcheckcode"))
                return;//验证码的不要后面处理

            base.OnActionExecuting(filterContext);

            AppletLogin();//小程序登录验证

            string _tmp = string.Empty;

            _tmp = WebHelper.GetCookie(CookieKeysCollection.HIMALL_NEED_CLEAR_DISTRIBUTION_SPREAD_ID_COOKIE_NAME);
            int needclear = 0;
            if (!int.TryParse(_tmp, out needclear))
            {
                needclear = 0;
            }
            if (needclear == 1)
            {
                //WebHelper.SetCookie(CookieKeysCollection.HIMALL_DISTRIBUTION_SPREAD_ID_COOKIE_NAME, "0");
                WebHelper.SetCookie(CookieKeysCollection.HIMALL_NEED_CLEAR_DISTRIBUTION_SPREAD_ID_COOKIE_NAME, "0");
            }

            //处理销售员引流
            _tmp = Request[DISTRIBUTION_SPREAD_ID_PARAMETER_NAME];
            long SpreadId = 0;
            if (!long.TryParse(_tmp, out SpreadId))
            {
                SpreadId = 0;
            }

            if (SpreadId > 0)
            {
                //写入销售员信息
                WebHelper.SetCookie(CookieKeysCollection.HIMALL_DISTRIBUTION_SPREAD_ID_COOKIE_NAME, SpreadId.ToString());
            }
            if (SpreadId == 0)
            {
                //获取cookie里的销售员id
                _tmp = WebHelper.GetCookie(CookieKeysCollection.HIMALL_DISTRIBUTION_SPREAD_ID_COOKIE_NAME);
                if (!long.TryParse(_tmp, out SpreadId))
                {
                    SpreadId = 0;
                }
            }
            CurrentSpreadId = SpreadId;

            //处理销售员自身推广
            if (WebHelper.IsGet() && !WebHelper.IsAjax() && CurrentUser != null && SiteSettingApplication.SiteSettings.DistributionIsEnable)
            {
                //if (PlatformType == Core.PlatformType.WeiXin)
                {
                    var disobj = DistributionApplication.GetDistributor(CurrentUser.Id);
                    if (disobj != null && disobj.IsNormalDistributor)
                    {
                        NeedDistributionWeiXinShare = true;

                        if (string.IsNullOrWhiteSpace(Request[DISTRIBUTION_SPREAD_ID_PARAMETER_NAME]) || CurrentSpreadId != CurrentUser.Id)
                        {
                            var siteUrl = SiteSettingApplication.SiteSettings.SiteUrl;
                            
                            CurrentSpreadId = CurrentUser.Id;
                            string url = WebHelper.GetUrl();
                            //如果站点配置的授权域名是包含https则强制替换http为https
                            if (siteUrl.ToLower().StartsWith("https://"))
                            {
                                url = url.Replace("http://", "https://");
                            }
                            string jumpurl = "";
                            string regstr = @"([\?&])" + DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=[^&]*(&?)";
                            jumpurl = Regex.Replace(url, regstr, "$1", RegexOptions.IgnoreCase);
                            if ("?&".IndexOf(jumpurl.Substring(jumpurl.Length - 1)) > -1) { jumpurl = jumpurl.Substring(0, jumpurl.Length - 1); }
                            if (jumpurl.IndexOf("?") > -1)
                            {
                                jumpurl += "&";
                            }
                            else
                            {
                                jumpurl += "?";
                            }
                            jumpurl += DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=" + CurrentSpreadId;

                            //Response.Clear();
                            //Response.BufferOutput = true;
                            //Response.Redirect(jumpurl);
                            filterContext.Result = Redirect(jumpurl);
                        }
                    }
                }
            }
            //关闭分销后处理地址
            if (!SiteSettingApplication.SiteSettings.DistributionIsEnable && !string.IsNullOrWhiteSpace(Request[DISTRIBUTION_SPREAD_ID_PARAMETER_NAME]))
            {
                string url = WebHelper.GetUrl();
                if (url.ToLower().Contains(DISTRIBUTION_SPREAD_ID_PARAMETER_NAME.ToLower()))
                {
                    url = Regex.Replace(url, @"([\?&])" + DISTRIBUTION_SPREAD_ID_PARAMETER_NAME + "=[^&]*(&?)", "$1", RegexOptions.IgnoreCase);
                    url = Regex.Replace(url, @"&$", "", RegexOptions.IgnoreCase);
                    //Response.Clear();
                    //Response.BufferOutput = true;
                    //Response.Redirect(url);
                    filterContext.Result = Redirect(url);
                }
            }
        }

        private void AppletLogin()
        {
            //验证登录来自小程序端
            var openId = Request["oid"];
            var source = Request["source"];
            if (!string.IsNullOrEmpty(source) && source == "applet")
            {
                var cacheKey = WebHelper.GetCookie(CookieKeysCollection.HIMALL_USER);
                if (!string.IsNullOrWhiteSpace(cacheKey))
                {

                    //_MemberService.DeleteMemberOpenId(userid, string.Empty);
                    WebHelper.DeleteCookie(CookieKeysCollection.HIMALL_USER);


                    //记录主动退出符号
                    WebHelper.SetCookie(CookieKeysCollection.HIMALL_ACTIVELOGOUT, "1", DateTime.MaxValue);

                    ClearDistributionSpreadCookie();
                }
                if (!string.IsNullOrEmpty(openId))
                {
                    openId = openId.Substring(0, openId.Length - 6);//获取实际的openId值，后六位为随机生成

                    var member = MemberApplication.GetMemberOpenIdInfoByOpenIdOrUnionId(openId);
                    if (member != null)
                    {
                        base.SetUserLoginCookie(member.UserId);
                        WebHelper.SetCookie(CookieKeysCollection.HIMALL_ACTIVELOGOUT, "0", DateTime.MaxValue);
                        base.SetSellerAdminLoginCookie(member.UserId, DateTime.Now.AddDays(7));
                    }
                }
            }

        }

        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {

            var viewResult = filterContext.Result as ViewResult;
            if (viewResult != null)
            {
                var currentUserTemplate = "Default";
                if (PlatformType == Core.PlatformType.IOS || PlatformType == Core.PlatformType.Android)
                    currentUserTemplate = "APP";
                var template = string.IsNullOrEmpty(currentUserTemplate) ? "" : currentUserTemplate;
                var controller = filterContext.RequestContext.RouteData.Values["Controller"].ToString();
                var action = filterContext.RequestContext.RouteData.Values["Action"].ToString();
                if (string.IsNullOrWhiteSpace(viewResult.ViewName))
                {
                    viewResult.ViewName = string.Format(
                        "~/Areas/Mobile/Templates/{0}/Views/{1}/{2}.cshtml",
                        template,
                        controller,
                        action);
                    return;
                }
                else if (!viewResult.ViewName.EndsWith(".cshtml"))
                {
                    viewResult.ViewName = string.Format(
                         "~/Areas/Mobile/Templates/{0}/Views/{1}/{2}.cshtml",
                         template,
                         controller,
                         viewResult.ViewName);
                    return;
                }
            }
            base.OnResultExecuting(filterContext);
        }
    }
}
