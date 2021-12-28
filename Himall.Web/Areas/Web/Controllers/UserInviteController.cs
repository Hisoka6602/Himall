using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.IO;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class UserInviteController : BaseMemberController
    {
        private MemberInviteService _iMemberInviteService;
        public UserInviteController(MemberInviteService MemberInviteService)
        {
            _iMemberInviteService = MemberInviteService;
        }

        public ActionResult Index()
        {
            var userId = CurrentUser.Id;
            var model = _iMemberInviteService.GetMemberInviteInfo(userId);
            var rule = _iMemberInviteService.GetInviteRule();

            model.InviteLink = String.Format("{0}/Register/index/{1}", Himall.Application.SiteSettingApplication.GetCurDomainUrl(), userId);
            var map = Core.Helper.QRCodeHelper.Create(model.InviteLink);
            MemoryStream ms = new MemoryStream();
            map.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            //  将图片内存流转成base64,图片以DataURI形式显示  
            string strUrl = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray());
            ms.Dispose();
            model.QR = strUrl;
            var  m = new Tuple<UserInviteModel, Entities.InviteRuleInfo, Entities.MemberInfo>(model, rule,CurrentUser);
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(m);
        }
    }
}