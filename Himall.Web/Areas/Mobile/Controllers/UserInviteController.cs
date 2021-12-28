using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.IO;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class UserInviteController : BaseMobileMemberController
    {
        private MemberInviteService _iMemberInviteService;
        private MemberIntegralService _iMemberIntegralService;
        public UserInviteController(MemberInviteService MemberInviteService,MemberIntegralService MemberIntegralService)
        {
            _iMemberInviteService = MemberInviteService;
            _iMemberIntegralService = MemberIntegralService;
        }
        public ActionResult Index()
        {
            var userId = CurrentUser.Id;
            var model = _iMemberInviteService.GetMemberInviteInfo(userId);
            var rule = _iMemberInviteService.GetInviteRule();
            var Integral = _iMemberIntegralService.GetIntegralChangeRule() ;
            if (Integral != null && Integral.IntegralPerMoney > 0)
            {
                ViewBag.IntergralMoney = (rule.InviteIntegral /Integral.IntegralPerMoney).ToString("f2");
            }
            string host = CurrentUrlHelper.CurrentUrlNoPort();
            model.InviteLink = String.Format("{0}/Register/index/{1}", host, userId);
            //rule.ShareIcon = string.Format("http://{0}{1}", host, rule.ShareIcon);
            rule.ShareIcon = !string.IsNullOrWhiteSpace(rule.ShareIcon) ? HimallIO.GetRomoteImagePath(rule.ShareIcon) : "";
            var map = Core.Helper.QRCodeHelper.Create(model.InviteLink);
            MemoryStream ms = new MemoryStream();
            map.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            //  将图片内存流转成base64,图片以DataURI形式显示  
            string strUrl = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray());
            ms.Dispose();
            model.QR = strUrl;
            ViewBag.WeiXin = PlatformType == PlatformType.WeiXin;
            var m = new Tuple<UserInviteModel, Entities.InviteRuleInfo, Entities.MemberInfo>(model, rule, CurrentUser);
            return View(m);
        }
    }
}