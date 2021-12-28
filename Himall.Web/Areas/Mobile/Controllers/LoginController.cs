using Aliyun.Acs.afs.Model.V20180112;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Areas.Web;
using Himall.Web.Framework;
using System;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class LoginController : BaseMobileTemplatesController
    {
        const string CHECK_CODE_KEY = "checkCode";

        /// <summary>
        /// 同一用户名无需验证的的尝试登录次数
        /// </summary>
        const int TIMES_WITHOUT_CHECKCODE = 3;

        MemberService _MemberService;
        MessageService _iMessageService;
        ManagerService _ManagerService;
        private SystemAgreementService _iSystemAgreementService;

        public LoginController(MemberService MemberService, MessageService MessageService, ManagerService ManagerService,SystemAgreementService SystemAgreementService)
        {

            _MemberService = MemberService;
            _iMessageService = MessageService;
            _ManagerService = ManagerService;
            _iSystemAgreementService = SystemAgreementService;
        }
        // GET: Mobile/Login
        public ActionResult Entrance(string returnUrl, string openId, string serviceProvider, string nickName, string headimgurl, string realName, string unionid = null)
        {
            return View(SiteSettingApplication.SiteSettings);
        }
        public ActionResult ForgotPassword()
        {

            return View(SiteSettingApplication.SiteSettings);
        }

        [HttpPost]
        public JsonResult BindUser(string username, string password, string headimgurl, string serviceProvider, string openId, Entities.MemberOpenIdInfo.AppIdTypeEnum appidtype = Entities.MemberOpenIdInfo.AppIdTypeEnum.Normal, string unionid = null, string sex = null, string city = null, string province = null, string country = null, string nickName = null, string checkCode = null)
        {
            try
            {
                CheckCheckCode(username, checkCode);//检查验证码

                var service = _MemberService;
                var member = service.Login(username, password);
                if (member == null)
                    throw new Himall.Core.HimallException("用户名和密码不匹配");

                #region //检测当前用户微信端是否已绑定openid，如已绑定不绑定，没绑定新绑定；
                var memberopen = service.GetMemberOpenIdInfoByuserIdAndType(member.Id, serviceProvider);
                if (memberopen == null || string.IsNullOrEmpty(memberopen.OpenId))
                {
                    headimgurl = System.Web.HttpUtility.UrlDecode(headimgurl);
                    nickName = System.Web.HttpUtility.UrlDecode(nickName);
                    city = System.Web.HttpUtility.UrlDecode(city);
                    province = System.Web.HttpUtility.UrlDecode(province);
                    OAuthUserModel model = new OAuthUserModel()
                    {
                        AppIdType = appidtype,
                        UserId = member.Id,
                        LoginProvider = serviceProvider,
                        OpenId = openId,
                        Headimgurl = headimgurl,
                        UnionId = unionid,
                        Sex = sex,
                        NickName = nickName,
                        City = city,
                        Province = province
                    };
                    service.BindMember(model);
                }
                else if (!string.IsNullOrEmpty(unionid) && unionid != memberopen.UnionId)
                {
                    memberopen.UnionId = unionid;
                    MemberApplication.UpdateOpenIdBindMember(memberopen);//之前它unionid不同，则修改unionid
                }
                #endregion

                base.SetUserLoginCookie(member.Id);
                WebHelper.SetCookie(CookieKeysCollection.HIMALL_ACTIVELOGOUT, "0", DateTime.MaxValue);
                SellerLoginIn(username, password);
                BizAfterLogin.Run(member.Id);//执行登录后初始化相关操作 

                ClearErrorTimes(username);//清除输入错误记录次数

                return Json(new { success = true });
            }
            catch (LoginException ex)
            {
                int errorTimes = SetErrorTimes(username);
                return Json(new { success = false, msg = ex.Message, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
            catch (HimallException ex)
            {
                int errorTimes = SetErrorTimes(username);
                return Json(new { success = false, msg = ex.Message, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
            catch (Exception ex)
            {
                int errorTimes = SetErrorTimes(username);
                Core.Log.Error("用户" + username + "登录时发生异常", ex);
                return Json(new { success = false, msg = "未知错误", errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
        }



        [HttpPost]
        public JsonResult Index(string username, string password, string checkCode = null)
        {
            try
            {
                CheckInput(username, password);//检查输入合法性
                CheckCheckCode(username, checkCode);//检查验证码

                var member = _MemberService.Login(username, password);
                if (member == null)
                {
                    throw new LoginException("用户名和密码不匹配", LoginException.ErrorTypes.PasswordError);
                }

                if (PlatformType == Core.PlatformType.WeiXin)
                    base.SetUserLoginCookie(member.Id);
                else
                    base.SetUserLoginCookie(member.Id, DateTime.MaxValue);

                WebHelper.SetCookie(CookieKeysCollection.HIMALL_ACTIVELOGOUT, "0", DateTime.MaxValue);
                SellerLoginIn(username, password);
                BizAfterLogin.Run(member.Id);//执行登录后初始化相关操作 

                ClearErrorTimes(username);//清除输入错误记录次数

                return Json(new { success = true, memberId = member.Id });
            }
            catch (LoginException ex)
            {
                int errorTimes = SetErrorTimes(username);
                return Json(new { success = false, msg = ex.Message, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
            catch (HimallException ex)
            {
                int errorTimes = SetErrorTimes(username);
                return Json(new { success = false, msg = ex.Message, errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
            catch (Exception ex)
            {
                int errorTimes = SetErrorTimes(username);
                Core.Log.Error("用户" + username + "登录时发生异常", ex);
                return Json(new { success = false, msg = "未知错误", errorTimes = errorTimes, minTimesWithoutCheckCode = TIMES_WITHOUT_CHECKCODE });
            }
        }

        void CheckInput(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                throw new LoginException("请填写用户名", LoginException.ErrorTypes.UsernameError);

            if (string.IsNullOrWhiteSpace(password))
                throw new LoginException("请填写密码", LoginException.ErrorTypes.PasswordError);

        }
        [HttpGet]
        public JsonResult CheckLogin()
        {
            var userId = base.UserId;
            if (userId != 0)
            {
                //_MemberService.DeleteMemberOpenId(userid, string.Empty);
                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            return Json(new { success = false }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="checkCode"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CheckUserName(string contact, string checkCode)
        {
            var service = _MemberService;
            string systemCheckCode = Session[CHECK_CODE_KEY] as string;
            if (systemCheckCode.ToLower() != checkCode.ToLower())
                throw new Core.HimallException("验证码错误");
            var userMenberInfo = service.GetMemberByContactInfo(contact);
            if (userMenberInfo == null)
            {
                throw new Core.HimallException("该手机号或邮箱未绑定账号");
            }

            MessageApplication.SendMessageCodeDirect(contact);

            Session.Remove(CHECK_CODE_KEY);//前面验证码已用完了清空下验证码

            return Json(new { success = true, data = new { contact = contact, url = "FillCode" } });
        }

        /// <summary>
        /// 检查会员账号是否存在
        /// </summary>
        /// <param name="contact">手机或邮箱</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult CheckUserNameOnly(string contact)
        {
            var service = _MemberService;
            var userMenberInfo = service.GetMemberByContactInfo(contact);
            if (userMenberInfo == null)
            {
                throw new Core.HimallException("该手机号或邮箱未绑定账号");
            }

            return Json(new { success = true, data = new { contact = contact, url = "FillCode" } });
        }

        //public ActionResult FillCode(string contact)
        //{
        //    ViewBag.Contact = contact;

        //    return View();
        //}

        /// <summary>
        /// 重新获取验证码
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public JsonResult SendCode(string contact)
        {
    
            if (SiteSettingApplication.SiteSettings.IsTheftBrush)
                return Json(new Result { success = false, msg = "开启了防盗刷验证该接口不能发送短信，刷新页面重新处理!" });

            MessageApplication.SendMessageCodeDirect(contact);
            return Json(new { success = true });
        }

        /// <summary>
        /// 验证验证码
        /// </summary>
        /// <param name="code"></param>
        /// <param name="contact"></param>
        /// <returns></returns>
        public JsonResult CheckCode(string code, string contact)
        {
            var pluginId = "";
            if (Core.Helper.ValidateHelper.IsMobile(contact))
            {
                pluginId = "Himall.Plugin.Message.SMS";
            }
            if (!string.IsNullOrEmpty(contact) && Core.Helper.ValidateHelper.IsEmail(contact))
            {
                pluginId = "Himall.Plugin.Message.Email";
            }
            ViewBag.Contact = contact;
            var cacheCode = MessageApplication.GetMessageCacheCode(contact, pluginId);
            if (cacheCode != null && cacheCode == code)
            {
                var FdCache = CacheKeyCollection.MemberFindPwd(contact);
                Core.Cache.Insert(FdCache, contact, DateTime.Now.AddMinutes(10));

                return Json(new { success = true, msg = "验证正确", data = new { url = "ResetPwd" } });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码输入错误或者已经超时" });
            }
        }

        /// <summary>
        /// 修改密码页
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public ActionResult ResetPwd(string contact)
        {
            //判断是否通过验证
            var FdCache = CacheKeyCollection.MemberFindPwd(contact);
            if (!Core.Cache.Exists(FdCache))
            {
                Response.Redirect("ForgotPassword");
            }
            ViewBag.Contact = contact;
            return View();
        }
        public ActionResult GoResetResult()
        {
            return View();
        }
        public JsonResult ModPwd(string contact, string password, string repeatPassword)
        {
            var userMenberInfo = _MemberService.GetMemberByContactInfo(contact);
            if (userMenberInfo == null)
            {
                throw new Core.HimallException("该手机号或邮箱未绑定账号");
            }
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(repeatPassword))
            {
                return Json(new Result() { success = false, msg = "密码不能为空！" });
            }
            if (!password.Trim().Equals(repeatPassword.Trim()))
            {
                return Json(new Result() { success = false, msg = "两次密码不一致！" });
            }
            _MemberService.ChangePassword(userMenberInfo.Id, password);

            return Json(new { success = true, msg = "密码修改成功！", data = new { url = "GoResetResult" } });
        }
        private Entities.ManagerInfo SellerLoginIn(string username, string password, bool keep = false)
        {
            var seller = _ManagerService.Login(username, password);
            if (seller == null)
            {
                return null;
            }

            if (keep)
            {
                base.SetSellerAdminLoginCookie(seller.Id, DateTime.Now.AddDays(7));
            }
            else
            {
                base.SetSellerAdminLoginCookie(seller.Id, DateTime.MaxValue);
            }
            return seller;
        }


        [HttpPost]
        public JsonResult CheckPicCode(string checkCode)
        {
            try
            {
                string systemCheckCode = Session[CHECK_CODE_KEY] as string;
                bool result = systemCheckCode.ToLower() == checkCode.ToLower();
                return Json<dynamic>(success: result);
            }
            catch (Himall.Core.HimallException ex)
            {
                return ErrorResult<dynamic>(msg: ex.Message);
            }
            catch (Exception ex)
            {
                Core.Log.Error("检验验证码时发生异常", ex);
                return ErrorResult<dynamic>(msg: "未知错误");
            }
        }

        public ActionResult GetCheckCode()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            Session[CHECK_CODE_KEY] = code;
            return File(image.ToArray(), "image/png");
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
                Session[CHECK_CODE_KEY] = Guid.NewGuid().ToString();
            }
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

        /// <summary>
        /// 滑动验证
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AuthenticateSig(string sessionId, string sig, string scene, string token, string appkey)
        {
            if (string.IsNullOrEmpty(SiteSettingApplication.SiteSettings.AccessKeyID) || string.IsNullOrEmpty(SiteSettingApplication.SiteSettings.AccessKeySecret))
            {
                return Json(new { success = true });
            }
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", SiteSettingApplication.SiteSettings.AccessKeyID, SiteSettingApplication.SiteSettings.AccessKeySecret);
            var client = new DefaultAcsClient(profile);
            DefaultProfile.AddEndpoint("cn-hangzhou", "cn-hangzhou", "afs", "afs.aliyuncs.com");

            AuthenticateSigRequest request = new AuthenticateSigRequest();
            request.SessionId = sessionId;// 必填参数，从前端获取，不可更改，android和ios只传这个参数即可
            request.Sig = sig;// 必填参数，从前端获取，不可更改
            request.Token = token;// 必填参数，从前端获取，不可更改
            request.Scene = scene;// 必填参数，从前端获取，不可更改
            request.AppKey = appkey;// 必填参数，后端填写
            request.RemoteIp = Request.ServerVariables.Get("Remote_Addr").ToString();// 必填参数，后端填写

            try
            {
                AuthenticateSigResponse response = client.GetAcsResponse(request);// 返回code 100表示验签通过，900表示验签失败
                if (response.Code == 100)
                {
                    return Json(new { success = true });
                }
                else
                {
                    Log.Info("验签失败:" + response.Msg);
                    return Json(new { success = false, msg = response.Msg });
                }
                // TODO
            }
            catch (Exception e)
            {
                Log.Info("验签失败:" + e.Message);
                return Json(new { success = false, msg = e.Message });
            }
        }

        public ActionResult RegisterAgreement()
        {
            ViewBag.Logo = SiteSettings.Logo;
            var model = _iSystemAgreementService.GetAgreement(Entities.AgreementInfo.AgreementTypes.Buyers);

            ViewBag.Keyword = SiteSettings.Keyword;
            return View(model);
        }

        /// <summary>
        /// 隐私政策
        /// </summary>
        /// <returns></returns>
        public ActionResult PrivacyPolicy()
        {
            ViewBag.Logo = SiteSettings.Logo;
            ViewBag.Keyword = SiteSettings.Keyword;

            var model = _iSystemAgreementService.GetAgreement(Entities.AgreementInfo.AgreementTypes.PrivacyPolicy);
            return View(model);
        }
    }
}