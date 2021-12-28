using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class UserInfoController : BaseMemberController
    {
        private MessageService _iMessageService;
        private MemberService _MemberService;

        public UserInfoController(MessageService MessageService, MemberService MemberService)
        {
            _MemberService = MemberService;
            _iMessageService = MessageService;
        }
        // GET: Web/UserInfo
        public ActionResult Index()
        {
            var model = MemberApplication.GetMembers(CurrentUser.Id);
            var messagePlugins = PluginsManagement.GetPlugins<IMessagePlugin>();
            var sms = PluginsManagement.GetPlugins<ISMSPlugin>();
            var smsInfo = sms.Select(item => new PluginsInfo
            {
                ShortName = item.Biz.ShortName,
                PluginId = item.PluginInfo.PluginId,
                Enable = item.PluginInfo.Enable,
                IsSettingsValid = item.Biz.IsSettingsValid,
                IsBind = !string.IsNullOrEmpty(_iMessageService.GetDestination(CurrentUser.Id, item.PluginInfo.PluginId, Entities.MemberContactInfo.UserTypes.General))
            }).FirstOrDefault();
            var email = PluginsManagement.GetPlugins<IEmailPlugin>();
            var emailInfo = email.Select(item => new PluginsInfo
            {
                ShortName = item.Biz.ShortName,
                PluginId = item.PluginInfo.PluginId,
                Enable = item.PluginInfo.Enable,
                IsSettingsValid = item.Biz.IsSettingsValid,
                IsBind = !string.IsNullOrEmpty(_iMessageService.GetDestination(CurrentUser.Id, item.PluginInfo.PluginId, Entities.MemberContactInfo.UserTypes.General))
            }).FirstOrDefault();


            ViewBag.BindSMS = smsInfo;
            ViewBag.BindEmail = emailInfo;
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(model);
        }

        [HttpPost]
        public JsonResult GetCurrentUserInfo()
        {
            var memberInfo = CurrentUser;
            string name = string.IsNullOrWhiteSpace(memberInfo.Nick) ? memberInfo.UserName : memberInfo.Nick;
            return Json(new { success = true, name = name });
        }

        public JsonResult UpdateUserInfo(MemberUpdate model)
        {
            if (!model.BirthDay.HasValue && !CurrentUser.BirthDay.HasValue)
            {
                return Json(new Result() { success = false, msg = "生日必须填写" });
            }
            //if (string.IsNullOrWhiteSpace(model.CellPhone) || string.IsNullOrWhiteSpace(CurrentUser.CellPhone))
            //{
            //    return Json(new Result() { success = false, msg = "请先绑定手机号码" });
            //}
            if (string.IsNullOrWhiteSpace(model.RealName))
            {
                return Json(new Result() { success = false, msg = "用户姓名必须填写" });
            }
            model.Id = CurrentUser.Id;
            MemberApplication.UpdateMemberInfo(model);
            return Json(new Result() { success = true, msg = "修改成功" });
        }

        public ActionResult ReBind(string pluginId)
        {
            var messagePlugin = PluginsManagement.GetPlugin<IMessagePlugin>(pluginId);
            ViewBag.ShortName = messagePlugin.Biz.ShortName;
            ViewBag.id = pluginId;
            ViewBag.ContactInfo = _iMessageService.GetDestination(CurrentUser.Id, pluginId, Entities.MemberContactInfo.UserTypes.General);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }

        [HttpPost]
        public ActionResult CheckCode(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination,pluginId);
            if (string.IsNullOrEmpty(cacheCode))
                return Json(new Result { success = false, msg = "验证码已经超时" });

            var member = CurrentUser;
            Log.Info("cacheCode:"+cacheCode+"---code:"+code);
            if (cacheCode == code)
            {
                MessageApplication.RemoveMessageCacheCode(destination,pluginId);
                Core.Cache.Insert("Rebind" + member.Id, "step2", DateTime.Now.AddMinutes(30));
                return Json(new { success = true, msg = "验证正确", key = member.Id });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确" });
            }
        }


        [HttpPost]  //验证第二步需要修改信息了
        public ActionResult CheckCodeStep2(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination,pluginId);
            var member = CurrentUser;

            if (cacheCode != null && cacheCode == code)
            {
                var service = _iMessageService;
                if (service.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
                {
                    return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
                }
                if (pluginId.ToLower().Contains("email"))
                {
                    member.Email = destination;
                }
                else if (pluginId.ToLower().Contains("sms"))
                {
                    member.CellPhone = destination;
                }
                _MemberService.UpdateMember(member);
                service.UpdateMemberContacts(new Entities.MemberContactInfo()
                {
                    Contact = destination,
                    ServiceProvider = pluginId,
                    UserId = CurrentUser.Id,
                    UserType = Entities.MemberContactInfo.UserTypes.General
                });
                MessageApplication.RemoveMessageCacheCode(destination, pluginId);
                return Json(new Result() { success = true, msg = "验证正确" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确或者已经超时" });
            }
        }

        [HttpPost]
        public ActionResult SendCode(string pluginId, string destination, bool checkBind = false)
        {
            if (checkBind && _iMessageService.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
            {
                return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
            }
            MessageApplication.SendMessageCodeDirect(destination,CurrentUser.UserName,pluginId);
            return Json(new Result() { success = true, msg = "发送成功" });
        }

        [HttpPost]
        public ActionResult SendCodeStep2(string pluginId, string destination, bool checkBind = false)
        {
            if (checkBind && _iMessageService.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
            {
                return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
            }
            MessageApplication.SendMessageCodeDirect(destination,CurrentUser.UserName, pluginId);
         
            return Json(new Result() { success = true, msg = "发送成功" });
        }

        public ActionResult ReBindStep2(string pluginId, string key)
        {
            if (Core.Cache.Get<string>("Rebind" + key) != "step2")
            {
                RedirectToAction("ReBind", new { pluginId = pluginId });
            }
            var messagePlugin = PluginsManagement.GetPlugin<IMessagePlugin>(pluginId);
            ViewBag.ShortName = messagePlugin.Biz.ShortName;
            ViewBag.id = pluginId;
            ViewBag.ContactInfo = _iMessageService.GetDestination(CurrentUser.Id, pluginId, Entities.MemberContactInfo.UserTypes.General);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }

        public ActionResult ReBindStep3(string name)
        {
            ViewBag.ShortName = name;
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }

        public ActionResult ChangePassword()
        {
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View();
        }

        [HttpPost]
        public JsonResult ChangePassword(string oldpassword, string password)
        {
            if (string.IsNullOrWhiteSpace(oldpassword) || string.IsNullOrWhiteSpace(password))
            {
                return Json(new Result() { success = false, msg = "密码不能为空！" });
            }
            var model = CurrentUser;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(oldpassword) + model.PasswordSalt);
            if (pwd == model.Password)
            {
                _MemberService.ChangePassword(model.Id, password);
                return Json(new Result() { success = true, msg = "修改成功" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "旧密码错误" });
            }
        }

        public JsonResult CheckOldPassWord(string password)
        {
            var model = CurrentUser;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(password) + model.PasswordSalt);
            if (model.Password == pwd)
            {
                return Json(new Result() { success = true });
            }
            return Json(new Result() { success = false });
        }

        /// <summary>
        /// 获取用户标识
        /// </summary>
        /// <returns></returns>
        public JsonResult UserIdentity()
        {
            if (CurrentUser == null)
                return Json(0, JsonRequestBehavior.AllowGet);

            var identity = (CurrentUser.Id + CurrentUser.CreateDate.ToString("yyyyMMddHHmmss")).GetHashCode();
            return Json(identity, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult BindSms(string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            var member = CurrentUser;
            if (cacheCode != null && cacheCode == code)
            {
                var service = _iMessageService;
                if (service.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
                {
                    return Json(new Result() { success = false, msg = destination + "已经绑定过了！" });
                }
                if (pluginId.ToLower().Contains("email"))
                {
                    member.Email = destination;
                }
                else if (pluginId.ToLower().Contains("sms"))
                {
                    member.CellPhone = destination;
                }
                _MemberService.UpdateMember(member);
                service.UpdateMemberContacts(new Entities.MemberContactInfo()
                {
                    Contact = destination,
                    ServiceProvider = pluginId,
                    UserId = CurrentUser.Id,
                    UserType = Entities.MemberContactInfo.UserTypes.General
                });
                MessageApplication.RemoveMessageCacheCode(destination, pluginId);
                return Json(new Result() { success = true, msg = "绑定成功" });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确或者已经超时" });
            }
        }

        [HttpPost]
        public JsonResult IsConBindSms()
        {
            return Json<dynamic>(success: MessageApplication.IsOpenBindSms(CurrentUser.Id));
        }
    }
}