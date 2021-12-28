using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Web.Models;
using Himall.Web.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class FindPassWordController : BaseWebController
    {
        private MemberService _MemberService;
        private MemberCapitalService _MemberCapitalService;
        private MessageService _iMessageService;
        public FindPassWordController(MessageService MessageService, MemberService MemberService, MemberCapitalService MemberCapitalService)
        {
            _iMessageService = MessageService;
            _MemberService = MemberService;
            _MemberCapitalService = MemberCapitalService;
        }
        // GET: Web/FindPassWord
        public ActionResult Index(int id)
        {
            SetTitle(id);

            string FindPayContact = "";
            if (id == 2 && CurrentUser != null)
            {
                FindPayContact = string.IsNullOrEmpty(CurrentUser.CellPhone) ? CurrentUser.Email : CurrentUser.CellPhone;
            }
            ViewBag.Contact = FindPayContact;
            ViewBag.IsTheftBrush = SiteSettingApplication.SiteSettings.IsTheftBrush;
            return View();
        }

        //找回密码第二步
        public ActionResult Step2(int id, string key)
        {
            SetTitle(id);
            var s = Core.Cache.Get<MemberInfo>(key);
            if (s == null)
            {
                return RedirectToAction("Error", "FindPassWord");
            }
            var messagePlugins = PluginsManagement.GetPlugins<IMessagePlugin>();
            var data = messagePlugins.Select(item => new PluginsInfo
            {
                ShortName = item.Biz.ShortName,
                PluginId = item.PluginInfo.PluginId,
                Enable = item.PluginInfo.Enable,
                IsSettingsValid = item.Biz.IsSettingsValid,
                IsBind = !string.IsNullOrEmpty(_iMessageService.GetDestination(s.Id, item.PluginInfo.PluginId, Entities.MemberContactInfo.UserTypes.General))
            });
            ViewBag.BindContactInfo = data;
            ViewBag.Key = key;
            ViewBag.Keyword = SiteSettings.Keyword;
            ViewBag.IsTheftBrush = SiteSettings.IsTheftBrush;
            ViewBag.SlideValidateAppKey = SiteSettings.SlideValidateAppKey;
            return View(s);
        }

        public ActionResult Error()
        {
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }

        public ActionResult Step3(int id, string key)
        {
            SetTitle(id);
            var s = Core.Cache.Get<MemberInfo>(key + "0");
            if (s == null)
            {
                return RedirectToAction("Error", "FindPassWord");
            }
            ViewBag.Key = key;
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }

        public ActionResult ChangePassWord(int id, string passWord, string key)
        {
            var member = Core.Cache.Get<MemberInfo>(key + "0");
            if (member == null)
            {
                return Json(new { success = false, flag = -1, msg = "验证超时" });
            }
            var userId = member.Id;
            if (id == 1)
            {
                _MemberService.ChangePassword(userId, passWord);
            }
            else
            {
                _MemberCapitalService.SetPayPwd(UserId, passWord);
            }
            MessageUserInfo info = new MessageUserInfo();
            info.SiteName = SiteSettings.SiteName;
            info.UserName = member.UserName;
            Task.Factory.StartNew(() => _iMessageService.SendMessageOnFindPassWord(userId, info));
            return Json(new { success = true, flag = 1, msg = "成功找回密码" });
        }

        public ActionResult Step4(int id)
        {
            SetTitle(id);
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }

        public ActionResult GetCheckCode()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            Session["FindPassWordcheckCode"] = code;
            return File(image.ToArray(), "image/png");
        }
        ///短信或者邮件验证码对比
        [HttpPost]
        public ActionResult CheckPluginCode(string pluginId, string code, string key)
        {
            var member = Core.Cache.Get<MemberInfo>(key);
            string destination = _iMessageService.GetDestination(member.Id, pluginId, Entities.MemberContactInfo.UserTypes.General);

            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            if (!string.IsNullOrEmpty(cacheCode) && cacheCode == code)
            {
                MessageApplication.RemoveMessageCacheCode(destination,pluginId);
                Core.Cache.Insert(key + "0", member, DateTime.Now.AddMinutes(15));
                return Json(new { success = true, msg = "验证正确", key = key });
            }
            else
            {
                return Json(new Result() { success = false, msg = "验证码不正确或者已经超时" });
            }
        }
        void VaildCode(string checkCode)
        {
            if (string.IsNullOrWhiteSpace(checkCode))
            {
                throw new HimallException("验证码不能为空");
            }
            else
            {
                string systemCheckCode = Session["FindPassWordcheckCode"] as string;
                if (string.IsNullOrEmpty(systemCheckCode))
                {
                    throw new HimallException("验证码超时，请刷新");
                }
                if (systemCheckCode.ToLower() != checkCode.ToLower())
                {
                    throw new HimallException("验证码不正确");
                }
            }
            Session["FindPassWordcheckCode"] = Guid.NewGuid().ToString();
        }


        //发送短信邮件验证码
        [HttpPost]
        public ActionResult SendCode(string pluginId, string key)
        {
            if (SiteSettingApplication.SiteSettings.IsTheftBrush)
                return Json(new Result { success = false, msg = "开启了防盗刷验证该接口不能发送短信，刷新页面重新处理!" });

            var s = Core.Cache.Get<MemberInfo>(key);
            if (s == null)
                return Json(new { success = false, flag = -1, msg = "验证已超时！" });
            string destination = _iMessageService.GetDestination(s.Id, pluginId, Entities.MemberContactInfo.UserTypes.General);
            MessageApplication.SendMessageCodeDirect(destination);
            return Json(new { success = true, flag = 1, msg = "发送成功" });
        }

        ///第一步，检查用户邮箱手机是否存在对应的用户
        [HttpPost]
        public ActionResult CheckUser(string userName, string checkCode)
        {
            VaildCode(checkCode);//检测验证码
            return CheckUserCommon(userName);
        }

        /// <summary>
        /// 第二步中，用户重置key
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CheckUserNoCode(string userName)
        {
            return CheckUserCommon(userName);
        }

        /// <summary>
        /// 检查用户邮箱手机是否存在对应的用户
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        private ActionResult CheckUserCommon(string userName)
        {
            var key = Guid.NewGuid().ToString().Replace("-", "");
            var member = _MemberService.GetMemberByContactInfo(userName);

            if (member == null)
                return Json(new { success = false, tag = "username", msg = "您输入的账户名不存在或者没有绑定邮箱和手机，请核对后重新输入" });
            else
            {
                Core.Cache.Insert<Entities.MemberInfo>(key, member, DateTime.Now.AddMinutes(15));
                return Json(new { success = true, key, memberID = member.Id });
            }
        }

        private void SetTitle(int id)
        {
            ViewBag.OpType = id;
            if (id == 1)
            {
                ViewBag.Title = "找回密码";
            }
            else
            {
                ViewBag.Title = "找回支付密码";
            }
        }
    }
}