using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using ServiceStack.Messaging;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class RegisterController : BaseController
    {
        private MemberService _MemberService;
        private BonusService _BonusService;
       // private MessageService _iMessageService;
        private ManagerService _ManagerService;
        private SystemAgreementService _iSystemAgreementService;
        public RegisterController(
            MemberService MemberService,
            BonusService BonusService,
           // MessageService MessageService,
            SystemAgreementService SystemAgreementService,
            ManagerService ManagerService
            )
        {
            _MemberService = MemberService;
            _BonusService = BonusService;
           // _iMessageService = MessageService;
            _iSystemAgreementService = SystemAgreementService;
            _ManagerService = ManagerService;
        }

        // GET: Web/Register
        public ActionResult Index(long id = 0)
        {
            ViewBag.SiteName = SiteSettings.SiteName;
            ViewBag.Logo = SiteSettings.Logo;
            ViewBag.IsTheftBrush = SiteSettings.IsTheftBrush;
            ViewBag.SlideValidateAppKey = SiteSettings.SlideValidateAppKey;
            RegisterIndexPageModel model = new RegisterIndexPageModel();
            model.MobileVerifOpen = SiteSettings.MobileVerifOpen;
            model.EmailVerifOpen = SiteSettings.EmailVerifOpen;
            model.RegisterEmailRequired = SiteSettings.RegisterEmailRequired;
            model.RegisterType = (RegisterTypes)SiteSettings.RegisterType;
            model.Introducer = id;
            return View(model);
        }

        const string CHECK_CODE_KEY = "regist_CheckCode";

        [HttpPost]
        public JsonResult RegisterUser(string username, string password, string mobile, string email, string checkCode, long introducer = 0)
        {
            var siteset = SiteSettings;
            if (siteset.RegisterEmailRequired)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return Json(new { success = false, msg = "错误的电子邮箱地址" });
                }
            }
            if (siteset.MobileVerifOpen)
            {
                if (string.IsNullOrWhiteSpace(mobile))
                {
                    return Json(new { success = false, msg = "错误的手机号码" });
                }
            }

            if (StringHelper.GetStringLength(username) > CommonModel.CommonConst.MEMBERNAME_LENGTH)
            {
                var unicodeChar = CommonModel.CommonConst.MEMBERNAME_LENGTH / 2;

                return Json(new { success = false, msg = "用户名最大长度为" + CommonModel.CommonConst.MEMBERNAME_LENGTH + "位," + unicodeChar + "个中文字符" });
            }

            //开启了防盗刷且手机注册，防盗刷之前已验证短信发送，这里不需再验证图形验证码
            if (!siteset.IsTheftBrush || !siteset.MobileVerifOpen)
            {
                var cacheCheckCode = Session[CHECK_CODE_KEY] as string;
                if (cacheCheckCode == null || string.IsNullOrEmpty(checkCode) || checkCode.ToLower() != cacheCheckCode.ToLower())
                {
                    return Json(new { success = false, msg = "验证码错误" });
                }
            }

            var member = _MemberService.Register(username, password, (int)PlatformType.PC, mobile, email, introducer);
            if (member != null)
            {
                //自动登录
                _MemberService.Login(username, password);

                base.SetUserLoginCookie(member.Id);

                Session.Remove(CHECK_CODE_KEY);
                if (!string.IsNullOrEmpty(mobile))
                    MessageApplication.RemoveMessageCacheCode(mobile, "Himall.Plugin.Message.SMS");
            }
            //TODO:ZJT  在用户注册的时候，检查此用户是否存在OpenId是否存在红包，存在则添加到用户预存款里
            _BonusService.DepositToRegister(member.Id);
            //用户注册的时候，检查是否开启注册领取优惠券活动，存在自动添加到用户预存款里
            int num = CouponApplication.RegisterSendCoupon(member.Id, member.UserName, out List<CouponModel> couponlist);

            return Json(new { success = true, memberId = member.Id, num = num,coupons= couponlist });
        }

        [ValidateInput(false)]
        public ActionResult GetCheckCode()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            Session[CHECK_CODE_KEY] = code;
            return File(image.ToArray(), "image/png");
        }

        [HttpPost]
        public JsonResult CheckCheckCode(string checkCode)
        {
            var cache = Session[CHECK_CODE_KEY] as string;
            bool result = cache != null && checkCode.ToLower() == cache.ToLower();
            return Json(new { success = true, result = result });
        }

        [HttpPost]
        public JsonResult CheckUserName(string username)
        {
            bool result = _MemberService.CheckMemberExist(username);
            return Json(new { success = true, result = result });
        }

        [HttpPost]
        public JsonResult CheckMobile(string mobile)
        {
            bool result = _MemberService.CheckMobileExist(mobile);
            return Json(new { success = true, result = result });
        }

        [HttpPost]
        public JsonResult CheckEmail(string email)
        {
            bool result = _MemberService.CheckEmailExist(email);
            return Json(new { success = true, result = result });
        }

        [HttpPost]
        public JsonResult SendCode(string pluginId, string destination, string imagecheckCode)
        {
            //验证图形验证码
            var cacheCheckCode = Session[CHECK_CODE_KEY] as string;
            //Session.Remove(CHECK_CODE_KEY);
            if (cacheCheckCode == null || string.IsNullOrEmpty(imagecheckCode) || imagecheckCode.ToLower() != cacheCheckCode.ToLower())
            {
                return Json(new Result { success = false, msg = "验证码错误" });
            }
            if (SiteSettingApplication.SiteSettings.IsTheftBrush)
                return Json(new Result { success = false, msg = "开启了防盗刷验证该接口不能发送短信，刷新页面重新处理!" });//特意做日志把该判断移到后面来，不然应可以放最前面
            MessageApplication.SendMessageCodeDirect(destination);         
            return Json(new Result() { success = true, msg = "发送成功" });
        }

     

        [HttpPost]
        public JsonResult CheckCode(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            if (cacheCode != null && cacheCode == code)
            {
                return Json(new Result() { success = true, msg = "验证正确" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确或者已经超时" });
            }
        }

        [HttpPost]
        public JsonResult CheckManagerUser(string username)
        {
            bool result = _ManagerService.CheckUserNameExist(username);
            return Json(new { success = true, result });
        }

        [HttpGet]
        public ActionResult RegBusiness()
        {
            ViewBag.Logo = SiteSettings.Logo;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }
        public ActionResult RegisterAgreement()
        {
            ViewBag.Logo = SiteSettings.Logo;
            var model = _iSystemAgreementService.GetAgreement(0);

            ViewBag.Keyword = SiteSettings.Keyword;
            return View(model);
        }
    }
}