using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class RegisterController : BaseMobileTemplatesController
    {
        const string CHECK_CODE_KEY = "checkCode";

        private MemberService _MemberService;
        private MemberInviteService _iMemberInviteService;
        private MemberIntegralService _iMemberIntegralService;
        private BonusService _BonusService;
        //private MessageService _iMessageService;
        private MemberIntegralConversionFactoryService _iMemberIntegralConversionFactoryService;
        public RegisterController(
            MemberService MemberService,
            MemberInviteService MemberInviteService,
            MemberIntegralService MemberIntegralService,
            MemberIntegralConversionFactoryService MemberIntegralConversionFactoryService,
            BonusService BonusService
            //MessageService MessageService
            )
        {
            //_iMessageService = MessageService;
            _MemberService = MemberService;
            _iMemberInviteService = MemberInviteService;
            _iMemberIntegralService = MemberIntegralService;
            _iMemberIntegralConversionFactoryService = MemberIntegralConversionFactoryService;
            _BonusService = BonusService;
        }
        // GET: Mobile/Register
        public ActionResult Index(long id = 0, string openid = "")
        {
            ViewBag.Introducer = id;
            if (id > 0)
            {
                if (string.IsNullOrWhiteSpace(openid))
                {
                    string webRoot = CurrentUrlHelper.CurrentUrlNoPort();
                    string url = webRoot + "/m-" + PlatformType + "/Register/InviteRegist?id=" + id;
                    if (PlatformType == PlatformType.WeiXin)
                        return Redirect("/m-" + PlatformType.ToString() + "/WXApi/WXAuthorize?returnUrl=" + url);
                    else
                        return Redirect(url);
                }
            }
            var setting = SiteSettingApplication.SiteSettings;
            var type = setting.RegisterType;
            ViewBag.EmailVerifOpen = setting.EmailVerifOpen;
            ViewBag.IsTheftBrush = setting.IsTheftBrush;
            ViewBag.SlideValidateAppKey = setting.SlideValidateAppKey;
            if (type == (int)RegisterTypes.Mobile || setting.IsConBindCellPhone)
            {
                return View("MobileReg");
            }
            return View();
        }
        public ActionResult InviteRegist(long id = 0, string openId = "", string unionid = "", string serviceProvider = "")
        {
            ViewBag.Introducer = id;
            var memberInfo = _MemberService.GetMemberByUnionId(unionid);
            var settings = SiteSettingApplication.SiteSettings;
            var inviteRule = _iMemberInviteService.GetInviteRule();
            var model = _iMemberIntegralService.GetIntegralChangeRule();
            var perMoney = model == null ? 0 : model.IntegralPerMoney;
            ViewBag.WXLogo = settings.WXLogo;
            string money;
            if (perMoney > 0)
            {
                money = (Convert.ToDouble(inviteRule.RegIntegral) / perMoney).ToString("f1");
            }
            else
            {
                money = "0.0";
            }


            int isRegist = 0;
            if (memberInfo != null)
            {
                isRegist = 1;
            }
            ViewBag.Money = money;
            ViewBag.IsRegist = isRegist;
            ViewBag.RegisterType = settings.RegisterType;
            return View(inviteRule);
        }
        [HttpPost]
        public JsonResult Index(string serviceProvider, string openId, string username, string password, string checkCode, string mobilecheckCode,
            string headimgurl, long introducer = 0, string unionid = null, string sex = null,
            string city = null, string province = null, string country = null, string nickName = null, string email = "", string emailcheckCode = "")
        {
            var mobilepluginId = "Himall.Plugin.Message.SMS";
            var emailpluginId = "Himall.Plugin.Message.Email";
            var siteset = SiteSettingApplication.SiteSettings;
            //开启了防盗刷且手机注册，防盗刷之前已验证短信发送，这里不需再验证图形验证码
            if (!siteset.IsTheftBrush || !siteset.MobileVerifOpen)
            {
                string systemCheckCode = Session[CHECK_CODE_KEY] as string;
                if (systemCheckCode.ToLower() != checkCode.ToLower())
                    throw new Core.HimallException("验证码错误");
            }

            if (Core.Helper.ValidateHelper.IsMobile(username))
            {
                var cacheCode = MessageApplication.GetMessageCacheCode(username,mobilepluginId);

                if (string.IsNullOrEmpty(mobilecheckCode) || mobilecheckCode.ToLower() != cacheCode.ToLower())
                {
                    throw new Core.HimallException("手机验证码错误");
                }
            }

            if (!string.IsNullOrEmpty(email) && Core.Helper.ValidateHelper.IsMobile(email))
            {
                var cacheCode = MessageApplication.GetMessageCacheCode(username, emailpluginId);

                if (string.IsNullOrEmpty(emailcheckCode) || emailcheckCode.ToLower() != cacheCode.ToLower())
                {
                    throw new Core.HimallException("手机验证码错误");
                }
            }

            headimgurl = System.Web.HttpUtility.UrlDecode(headimgurl);
            nickName = System.Web.HttpUtility.UrlDecode(nickName);
            province = System.Web.HttpUtility.UrlDecode(province);
            city = System.Web.HttpUtility.UrlDecode(city);
            Entities.MemberInfo member;
            var mobile = "";
            if (Core.Helper.ValidateHelper.IsMobile(username))
                mobile = username;
            var platform = PlatformType.GetHashCode();//注册终端来源
            if (!string.IsNullOrWhiteSpace(serviceProvider) && !string.IsNullOrWhiteSpace(openId))
            {
                OAuthUserModel userModel = new OAuthUserModel
                {
                    UserName = username,
                    Password = password,
                    LoginProvider = serviceProvider,
                    OpenId = openId,
                    Headimgurl = headimgurl,
                    Sex = sex,
                    NickName = nickName,
                    Email = email,
                    UnionId = unionid,
                    introducer = introducer,
                    Province = province,
                    City = city,
                    Platform = platform,
                    SpreadId = CurrentSpreadId
                };
                member = _MemberService.Register(userModel);
            }
            else
                member = _MemberService.Register(username, password, platform, mobile, email, introducer, spreadId: CurrentSpreadId);
            if (member != null)
            {
                Session.Remove(CHECK_CODE_KEY);
                MessageHelper helper = new MessageHelper();
                helper.ClearErrorTimes(member.UserName);
                if (!string.IsNullOrEmpty(email))
                {
                    helper.ClearErrorTimes(member.Email);
                }
                ClearDistributionSpreadCookie();
            }
            //TODO:ZJT  在用户注册的时候，检查此用户是否存在OpenId是否存在红包，存在则添加到用户预存款里
            _BonusService.DepositToRegister(member.Id);
            //用户注册的时候，检查是否开启注册领取优惠券活动，存在自动添加到用户预存款里
            int num = CouponApplication.RegisterSendCoupon(member.Id, member.UserName,out List< CouponModel> couponlist);

            base.SetUserLoginCookie(member.Id);
            Application.MemberApplication.UpdateLastLoginDate(member.Id);
            _MemberService.AddIntegel(member); //给用户加积分//执行登录后初始化相关操作
            return Json<dynamic>(success: true, data: new { memberId = member.Id, num = num,coupons=couponlist });
        }

        [HttpPost]
        public JsonResult InviteRegist(string serviceProvider, string openId, string username, string password, string nickName, string headimgurl, long introducer, string sex, string city = null, string province = null, string unionid = null, string mobile = null)
        {

            headimgurl = System.Web.HttpUtility.UrlDecode(headimgurl);
            nickName = System.Web.HttpUtility.UrlDecode(nickName);
            username = System.Web.HttpUtility.UrlDecode(username);
            province = System.Web.HttpUtility.UrlDecode(province);
            city = System.Web.HttpUtility.UrlDecode(city);
            var platform = PlatformType.GetHashCode();//注册终端来源
            Entities.MemberInfo member;
            if (string.IsNullOrWhiteSpace(username))
                username = mobile;
            if (!string.IsNullOrWhiteSpace(serviceProvider) && !string.IsNullOrWhiteSpace(openId))
                member = _MemberService.Register(username, password, serviceProvider, openId, platform, sex, headimgurl, introducer, nickName
                    , city, province, unionid, spreadId: CurrentSpreadId);
            else
                member = _MemberService.Register(username, password, platform, mobile, "", introducer, spreadId: CurrentSpreadId);

            //TODO:ZJT  在用户注册的时候，检查此用户是否存在OpenId是否存在红包，存在则添加到用户预存款里
            _BonusService.DepositToRegister(member.Id);
            //用户注册的时候，检查是否开启注册领取优惠券活动，存在自动添加到用户预存款里
            int num = CouponApplication.RegisterSendCoupon(member.Id, member.UserName, out List<CouponModel> couponlist);

            ClearDistributionSpreadCookie();
            base.SetUserLoginCookie(member.Id);
            Application.MemberApplication.UpdateLastLoginDate(member.Id);
            _MemberService.AddIntegel(member); //给用户加积分//执行登录后初始化相关操作
            return Json<dynamic>(success: true, data: new { memberId = member.Id, num = num,coupons=couponlist});
        }


        [HttpPost]
        public JsonResult Skip(string serviceProvider, string openId, string nickName, string realName, string headimgurl, Entities.MemberOpenIdInfo.AppIdTypeEnum appidtype = Entities.MemberOpenIdInfo.AppIdTypeEnum.Normal, string unionid = null, string sex = null, string city = null, string province = null)
        {
            int num = 0;
            List<CouponModel> couponlist = new List<CouponModel>();
            Entities.MemberInfo memberInfo = _MemberService.GetMemberByUnionIdOpenId(unionid, openId);
            if (memberInfo == null)
                memberInfo = _MemberService.GetMemberByOpenId(serviceProvider, openId);

            if (memberInfo == null)
            {
                var site = SiteSettingApplication.SiteSettings;
                if (site.IsConBindCellPhone)
                {
                    return Json<dynamic>(success: false, data: new { num = num }, code: 0);//开启了强制绑定，未注册会员返回
                }

                string username = DateTime.Now.ToString("yyMMddHHmmssffffff");   //未使用，在方法内会重新生成
                nickName = System.Web.HttpUtility.UrlDecode(nickName);
                realName = System.Web.HttpUtility.UrlDecode(realName);
                headimgurl = System.Web.HttpUtility.UrlDecode(headimgurl);
                province = System.Web.HttpUtility.UrlDecode(province);
                city = System.Web.HttpUtility.UrlDecode(city);

                memberInfo = _MemberService.QuickRegister(username, realName, nickName, serviceProvider, openId, PlatformType.GetHashCode(),
                    unionid, sex, headimgurl, appidtype, null, city, province, spreadId: CurrentSpreadId);
                //TODO:ZJT  在用户注册的时候，检查此用户是否存在OpenId是否存在红包，存在则添加到用户预存款里
                _BonusService.DepositToRegister(memberInfo.Id);
                //用户注册的时候，检查是否开启注册领取优惠券活动，存在自动添加到用户预存款里
                if (memberInfo.IsNewAccount)
                    num = CouponApplication.RegisterSendCoupon(memberInfo.Id, memberInfo.UserName,out couponlist);
                ClearDistributionSpreadCookie();
                _MemberService.AddIntegel(memberInfo); //给用户加积分//执行登录后初始化相关操作
            }
            else
            {

            }
            base.SetUserLoginCookie(memberInfo.Id);
            Application.MemberApplication.UpdateLastLoginDate(memberInfo.Id);
            WebHelper.SetCookie(CookieKeysCollection.HIMALL_ACTIVELOGOUT, "0", DateTime.MaxValue);

            #region 判断是否强制绑定手机号
            MemberApplication.UpdateLastLoginDate(memberInfo.Id);
            var isBind = MessageApplication.IsOpenBindSms(memberInfo.Id);
            if (!isBind)
            {
                return Json<dynamic>(success: false, data: new { num = num }, code: 99);
            }
            #endregion

            return Json<dynamic>(success: true, data: new { num = num,coupons=couponlist});
        }

        [HttpPost]
        public JsonResult CheckCode(string checkCode)
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


        [HttpPost]
        public JsonResult SendMobileCode(string pluginId, string destination, string imagecheckCode, string action)
        {
            if(SiteSettingApplication.SiteSettings.IsTheftBrush)
                return Json(new Result { success = false, msg = "开启了防盗刷验证该接口不能发送短信，刷新页面重新处理!" });

            //验证图形验证码
            var cacheCheckCode = Session[CHECK_CODE_KEY] as string;
            //Session.Remove(CHECK_CODE_KEY);
            if (string.IsNullOrEmpty(action))
            {
                if (cacheCheckCode == null || string.IsNullOrEmpty(imagecheckCode) || imagecheckCode.ToLower() != cacheCheckCode.ToLower())
                {
                    return Json(new Result { success = false, msg = "图形验证码错误" });
                }
            }
            _MemberService.CheckContactInfoHasBeenUsed(pluginId, destination);
            MessageApplication.SendMessageCodeDirect(destination);
            return SuccessResult<dynamic>(msg: "发送成功");
        }

        [HttpPost]
        public JsonResult SendEmailCode(string pluginId, string destination)
        {
            _MemberService.CheckContactInfoHasBeenUsed(pluginId, destination);
            MessageApplication.SendMessageCodeDirect(destination);
            return SuccessResult<dynamic>(msg: "发送成功");
        }


        [HttpPost]
        public JsonResult CheckEmailCode(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination,pluginId);
            if (cacheCode != null && cacheCode == code)
            {
                return SuccessResult<dynamic>(msg: "验证正确");
            }
            else
            {
                return ErrorResult<dynamic>(msg: "邮箱验证码不正确或者已经超时");
            }
        }


        [HttpPost]
        public JsonResult CheckMobileCode(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            if (cacheCode != null && cacheCode == code)
            {
                return SuccessResult<dynamic>(msg: "验证正确");
            }
            else
            {
                return ErrorResult<dynamic>(msg: "手机验证码不正确或者已经超时");
            }
        }

    }
}