using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins.OAuth;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Himall.Application;
using Himall.DTO;

namespace Himall.Web.Areas.Web.Controllers
{
    public class LoginController : BaseController
    {
        private MemberService _MemberService;
        private ManagerService _ManagerService;
        public LoginController(MemberService MemberService, ManagerService ManagerService)
        {
            _MemberService = MemberService;
            _ManagerService = ManagerService;
        }

        /// <summary>
        /// 同一用户名无需验证的的尝试登录次数
        /// </summary>
        const int TIMES_WITHOUT_CHECKCODE = 3;

        // GET: SellerAdmin/Login
        public ActionResult Index()
        {
            ViewBag.SiteName = SiteSettings.SiteName;
            ViewBag.Logo = SiteSettings.Logo;
            ViewBag.IsOpenPC = SiteSettings.IsOpenPC ? 1 : 0;
            ViewBag.IsOpenCopyRight = SiteSettings.IsOpenCopyRight ? 1 : 0;
            return View();
        }

        public JsonResult GetOAuthList()
        {
            if (Cache.Exists(CacheKeyCollection.CACHE_OAUTHLIST))
                return Cache.Get<JsonResult>(CacheKeyCollection.CACHE_OAUTHLIST);

            List<OAuthInfo> result = new List<OAuthInfo>();
            var oauthPlugins = Core.PluginsManagement.GetPlugins<IOAuthPlugin>(true);
            string siteDomain = CurrentUrlHelper.CurrentUrlNoPort();//信任登录不能有端口
            string rootDir = IOHelper.GetMapPath("/");

            JsonResult rlt = null;
            ///京东云插件里SDK调用了插件初始调用System.Threading.Tasks的Result时有会卡死，则加了System.Threading.Tasks.Task.Factory.StartNew处理



            //临时注释
            System.Threading.Tasks.Task.Factory.StartNew(() =>
            {
                result = oauthPlugins.Select(item =>
                {
                    return new OAuthInfo()
                    {
                        Name = item.Biz.ShortName,
                        Url = item.Biz.GetOpenLoginUrl(siteDomain + "/Login/OauthCallBack?oauthId=" + item.PluginInfo.PluginId),
                        LogoDefault = item.Biz.Icon_Default.Replace(rootDir, "/").Replace("\\", "/"),
                        LogoHover = item.Biz.Icon_Hover.Replace(rootDir, "/").Replace("\\", "/")
                    };
                }).ToList();

                if (result != null && result.Count > 0)
                {
                    rlt = Json(result, JsonRequestBehavior.AllowGet);
                    Cache.Insert(CacheKeyCollection.CACHE_OAUTHLIST, rlt, 600);
                }
            });

            return rlt;
        }

        public ActionResult OauthCallBack(string oauthId)
        {
            try
            {
                var oauthPlugin = Core.PluginsManagement.GetPlugin<IOAuthPlugin>(oauthId);
                var oauthInfo = oauthPlugin.Biz.GetUserInfo(Request.QueryString);
                Entities.MemberInfo member = null;
                if (oauthId.Equals("Himall.Plugin.OAuth.Weibo") || oauthId.Equals("Himall.Plugin.OAuth.JDClound"))
                {
                    if (!string.IsNullOrEmpty(oauthInfo.OpenId))
                    {
                        //微博查询是否该OpenId对应的用户已经存在
                        member = _MemberService.GetMemberByOpenId(oauthId, oauthInfo.OpenId);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(oauthInfo.UnionId))//检查是否正确返回OpenId
                    {
                        //查询是否该UnionId对应的用户已经存在
                        member = _MemberService.GetMemberByUnionId(oauthId, oauthInfo.UnionId);
                    }
                }
                if (member != null)
                {//存在，则直接登录
                    SellerLoginIn(member.UserName, member.Password);

                    base.SetUserLoginCookie(member.Id);
                    Application.MemberApplication.UpdateLastLoginDate(member.Id);

                    BizAfterLogin.Run(member.Id);//执行登录后初始化相关操作 
                    return Redirect("/");
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(oauthInfo.OpenId))
                    {//扫码登录
                        string url = string.Format("/Login/BindUser?oauthId={0}&openId={1}&name={2}&unionid={3}&unionopenid={4}", oauthId, oauthInfo.OpenId, oauthInfo.NickName, oauthInfo.UnionId, oauthInfo.OpenId);
                        return Redirect(url);
                    }
                }
                ViewBag.Keyword = SiteSettings.Keyword;
                return View();
            }
            catch (Exception ex)//出异常(包括取消后回调，直接返回到登录页)
            {
                Log.Error(Request.Url);
                Log.Error(ex);

                return Content(string.Format("<script type=\"text/javascript\">window.location.href = '{0}'; window.close();</script>", "/login"));
            }
        }

        [HttpPost]
        public JsonResult Skip(string oauthId, string openId, string nickName, string unionid = null, string unionopenid = null)
        {
            string username = DateTime.Now.ToString("yyMMddHHmmssffffff");
            var memberInfo = _MemberService.QuickRegister(username, string.Empty, nickName, oauthId, openId, (int)PlatformType.PC, unionid, unionopenid: unionopenid);
            int num = 0;
            List<CouponModel> couponlist = new List<CouponModel>();
            if (memberInfo != null)
            {
                //TODO:ZJT  在用户注册的时候，检查此用户是否存在OpenId是否存在红包，存在则添加到用户预存款里
                BonusApplication.DepositToRegister(memberInfo.Id);
                //用户注册的时候，检查是否开启注册领取优惠券活动，存在自动添加到用户预存款里
                if (memberInfo.IsNewAccount)
                    num = CouponApplication.RegisterSendCoupon(memberInfo.Id, memberInfo.UserName, out couponlist);
            }
            base.SetUserLoginCookie(memberInfo.Id);
            Application.MemberApplication.UpdateLastLoginDate(memberInfo.Id);

            return Json(new { success = true, num = num, coupons = couponlist });
        }

        [HttpPost]
        public JsonResult BindUser(string username, string password, string oauthId, string openId, string unionid = null, string unionopenid = null)
        {

            var service = _MemberService;
            var member = service.Login(username, password);
            if (member == null)
                throw new Himall.Core.HimallException("用户名和密码不匹配");

            service.BindMember(member.Id, oauthId, openId, unionid: unionid, unionopenid: unionopenid);

            base.SetUserLoginCookie(member.Id);

            return Json(new { success = true });
        }


        public ActionResult BindUser(string oauthId, string openId, string name, string unionid = null, string unionopenid = null)
        {
            ViewBag.Logo = SiteSettings.Logo;
            ViewBag.OauthId = oauthId;
            ViewBag.NickName = name;
            ViewBag.OpenId = openId;
            ViewBag.unionid = unionid == null ? string.Empty : unionid;
            ViewBag.unionopenid = unionopenid == null ? string.Empty : unionopenid;

            var oauthPlugin = Core.PluginsManagement.GetPlugin<IOAuthPlugin>(oauthId).Biz;
            ViewBag.ServiceProvider = oauthPlugin.ShortName;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }


        private Entities.ManagerInfo SellerLoginIn(string username, string password, bool keep = false)
        {
            var seller = _ManagerService.Login(username, password);
            if (seller == null)
            {
                Log.Info("获取店铺帐号信号为空，UserName:" + username + ",Password:" + password);
                return null;
            }
            if (keep)
            {
                base.SetSellerAdminLoginCookie(seller.Id, DateTime.Now.AddDays(7));
            }
            else
            {
                base.SetSellerAdminLoginCookie(seller.Id);
            }
            return seller;
        }

        private Entities.MemberInfo UserLoginIn(string username, string password, bool keep = false)
        {
            var member = _MemberService.Login(username, password);
            if (member == null)
            {
                throw new LoginException("用户名和密码不匹配", LoginException.ErrorTypes.PasswordError);
            }
            BizAfterLogin.Run(member.Id);

            if (keep)
            {
                base.SetUserLoginCookie(member.Id, DateTime.Now.AddDays(7));
            }
            else
            {
                base.SetUserLoginCookie(member.Id);
            }

            return member;
        }

        [HttpPost]
        public JsonResult Login(string username, string password, string checkCode, bool keep = false, string returnUrl = "")
        {
            try
            {
                //检查输入合法性
                CheckInput(username, password);
                //检查验证码
                CheckCheckCode(username, checkCode);
                if (username.IndexOf(':') > 0 || !MemberApplication.CheckLoginExist(username))
                {
                    var seller = SellerLoginIn(username, password, keep);
                    if (seller == null)
                    {

                        throw new LoginException("用户名和密码不匹配", LoginException.ErrorTypes.PasswordError);
                    }
                    ClearErrorTimes(username);//清除输入错误记录次数
                    return Json(new { success = true, isChildSeller = true });
                }
                else
                {
                    var isMemberDisabled = false;
                    var seller = SellerLoginIn(username, password, keep);
                    try
                    {
                        var member = UserLoginIn(username, password, keep);
                    }
                    catch (MessageException ex)
                    {
                        if (ex.MessageStatus == ExceptionMessages.MemberDisabled && seller != null)
                        {
                            isMemberDisabled = true;
                            //门店账户 忽略会员冻结状态
                        }
                        else
                            throw ex;
                    }
                    ClearErrorTimes(username);//清除输入错误记录次数
                    return Json(new { success = true, isMemberDisabled = isMemberDisabled });
                }
            }
            catch (LoginException ex)
            {
                int errorTimes = SetErrorTimes(username);
                return Json(new { success = false, msg = ex.Message, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE, errorType = (int)ex.ErrorType });
            }
            catch (HimallException ex)
            {
                int errorTimes = SetErrorTimes(username);
                return Json(new { success = false, msg = ex.Message, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
            catch (Exception ex)
            {
                int errorTimes = SetErrorTimes(username);
                Exception innerEx = GerInnerException(ex);
                string showerrmsg = "未知错误";
                if (innerEx is Himall.Core.HimallException)
                {
                    showerrmsg = innerEx.Message;
                }
                else
                {
                    Core.Log.Error("用户" + username + "登录时发生异常", ex);
                }
                return Json(new { success = false, msg = showerrmsg, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
        }

        [HttpPost]
        public JsonResult GetErrorLoginTimes(string username)
        {
            var errorTimes = GetErrorTimes(username);
            return Json(new { errorTimes = errorTimes });
        }


        [HttpPost]
        public JsonResult CheckCode(string checkCode)
        {
            try
            {
                string systemCheckCode = Session["checkCode"] as string;
                bool result = systemCheckCode.ToLower() == checkCode.ToLower();
                return Json(new { success = result });
            }
            catch (Himall.Core.HimallException ex)
            {
                return Json(new { success = false, msg = ex.Message });
            }
            catch (Exception ex)
            {
                Core.Log.Error("检验验证码时发生异常", ex);
                return Json(new { success = false, msg = "未知错误" });
            }
        }

        void CheckInput(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new LoginException("请填写用户名", LoginException.ErrorTypes.UsernameError);

            if (string.IsNullOrWhiteSpace(password))
                throw new LoginException("请填写密码", LoginException.ErrorTypes.PasswordError);

        }

        void CheckCheckCode(string username, string checkCode)
        {
            var errorTimes = GetErrorTimes(username);
            if (errorTimes >= TIMES_WITHOUT_CHECKCODE)
            {
                if (string.IsNullOrWhiteSpace(checkCode))
                    throw new LoginException("30分钟内登录错误3次以上需要提供验证码", LoginException.ErrorTypes.CheckCodeError);

                string systemCheckCode = Session["checkCode"] as string;
                if (systemCheckCode.ToLower() != checkCode.ToLower())
                    throw new LoginException("验证码错误", LoginException.ErrorTypes.CheckCodeError);

                //生成随机验证码，强制使验证码过期（一次提交必须更改验证码）
                Session["checkCode"] = Guid.NewGuid().ToString();
            }
        }


        public ActionResult GetCheckCode()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            Session["checkCode"] = code;
            return File(image.ToArray(), "image/png");
        }


        /// <summary>
        /// 获取指定用户名在30分钟内的错误登录次数
        /// </summary>
        /// <param name="username"></param>
        /// <returns></returns>
        int GetErrorTimes(string username)
        {
            var timesObject = Core.Cache.Get<int>(CacheKeyCollection.MemberLoginError(username));
            //var times = timesObject == null ? 0 : int.Parse(timesObject.ToString());
            return timesObject;
        }

        void ClearErrorTimes(string username)
        {
            Core.Cache.Remove(CacheKeyCollection.MemberLoginError(username));
        }

        /// <summary>
        /// 设置错误登录次数
        /// </summary>
        /// <param name="username"></param>
        /// <returns>返回最新的错误登录次数</returns>
        int SetErrorTimes(string username)
        {
            var times = GetErrorTimes(username) + 1;
            Core.Cache.Insert(CacheKeyCollection.MemberLoginError(username), times, DateTime.Now.AddMinutes(30.0));//写入缓存
            return times;
        }

    }
}