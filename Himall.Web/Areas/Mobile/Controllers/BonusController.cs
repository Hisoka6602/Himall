using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Areas.Admin.Models.Market;
using Himall.Web.Framework;
using Senparc.Weixin.Exceptions;
using Senparc.Weixin.MP.AdvancedAPIs;
using Senparc.Weixin.MP.AdvancedAPIs.OAuth;
using Senparc.Weixin.MP.CommonAPIs;
using Senparc.Weixin.MP.Containers;
using System;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class BonusController : BaseMobileTemplatesController
    {
        private BonusService _BonusService;
        public BonusController(BonusService BonusService)
        {
            _BonusService = BonusService;
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrWhiteSpace(siteSetting.WeixinAppId) || string.IsNullOrWhiteSpace(siteSetting.WeixinAppSecret))
            {
                throw new HimallException("未配置公众号参数");
            }
        }


        /// <param name="id">红包id</param>
        public ActionResult Index(long id)
        {
            if (this.PlatformType != Core.PlatformType.WeiXin)
            {
                return Content("只能在微信端访问");
            }

            var bonus = this._BonusService.Get(id);
            if (bonus == null)
            {
                return Redirect("/m-weixin/Bonus/Invalidtwo");
            }

            BonusModel model = new BonusModel(bonus);

            if (model.Type == BonusInfo.BonusType.Attention)
            {
                throw new Exception("红包异常"); //这里只能领取活动红包，不能领取关注红包
            }
            string code = HttpContext.Request["code"];
            OAuthAccessTokenResult wxOpenInfo = null;

            var settings = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrEmpty(code))
            {
                string selfAddress = WebHelper.GetAbsoluteUri();
                #region 有时可能cdn原因，访问的域名虽然是https，它可能是域名解析跳转则直接获取网址域名它还是http，所以下面判断当配置的是https则让它返回https
                if (selfAddress.IndexOf("https://") == -1 && settings.SiteUrl.IndexOf("https://") != -1)
                    selfAddress = selfAddress.Replace("http://", "https://");
                #endregion

                //string url = OAuthApi.GetAuthorizeUrl(settings.WeixinAppId.Trim() , selfAddress , "123321#wechat_redirect" , Senparc.Weixin.MP.OAuthScope.snsapi_base , "code" );
                string url = string.Format("https://open.weixin.qq.com/connect/oauth2/authorize?appid={0}&redirect_uri={1}&response_type=code&scope=snsapi_userinfo&state=STATE#wechat_redirect"
                                , settings.WeixinAppId.Trim(), System.Web.HttpUtility.UrlEncode(selfAddress));
                return Redirect(url);
            }
            else
            {
                try
                {
                    wxOpenInfo = OAuthApi.GetAccessToken(settings.WeixinAppId.Trim(), settings.WeixinAppSecret.Trim(), code, "authorization_code");
                }
                catch (ErrorJsonResultException wxe)
                {
                    Log.Error("1--id:" + id + "--" + wxe.JsonResult.errcode + "--" + wxe.JsonResult.errmsg);
                    if (wxe.JsonResult.errcode == Senparc.Weixin.ReturnCode.不合法的oauth_code)
                    {
                        return Redirect("/m-weixin/Bonus/Index/" + id);
                    }
                    else if (wxe.JsonResult.errcode.GetHashCode() == 40163)
                    {
                        //微信：40163，说明code被使用过一次了，code只能用一次,errmsg:code been used,hints:[req_id:GsQPaa0384th48]
                        //(这种情况是手机上退回加载时上面code是原来的<页面它是先跳转获取openid则退回时还是这个页面>，则它让它退回原页面)
                        return Content("<script>history.back(-1);</script>");
                    }
                    return Content(wxe.JsonResult.errmsg);
                }
                catch (Exception e)
                {
                    Exception innerEx = e.InnerException == null ? e : e.InnerException;
                    Log.Error("2--id:" + id + "--" + innerEx.Message);
                    return Content(innerEx.Message);
                }
            }
            OAuthUserInfo wxUserInfo = OAuthApi.GetUserInfo(wxOpenInfo.access_token, wxOpenInfo.openid);
            var openId = wxOpenInfo.openid;
            var unionId = "";
            if (!string.IsNullOrEmpty(wxUserInfo.unionid))
            {
                unionId = wxUserInfo.unionid;
            }
            if (model.EndTime <= DateTime.Now || model.IsInvalid)
            {
                return Redirect("/m-weixin/Bonus/Invalid/" + model.Id);
            }
            else if (model.StartTime > DateTime.Now)
            {
                return Redirect("/m-weixin/Bonus/NotStart/" + model.Id + "?openId=" + openId + "&unionId=" + unionId);
            }
            //（3.3版本因为要兼容小程序领取现金红包，BonusReceiveInfo表中的OpenId改为存储Unionid）
            ViewBag.OpenId = openId;
            ViewBag.UnionId = unionId;
            model.ImagePath = HimallIO.GetFilePath(model.ImagePath);
            return View(model);
        }

        //完成
        public ActionResult Completed(long id, string openId = "", decimal price = 0)
        {
            ViewBag.Price = price;
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            model.ImagePath = HimallIO.GetFilePath(model.ImagePath);
            ViewBag.OpenId = openId;
            return View(model);
        }

        public ActionResult CanReceiveNotAttention(long id, string openId = "", decimal price = 0)
        {
            ViewBag.Price = price;
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            //TODO:改成统一的方式取 Token
            var settings = SiteSettingApplication.SiteSettings;
            var token = WXApiApplication.TryGetToken(settings.WeixinAppId, settings.WeixinAppSecret);

            SceneHelper helper = new SceneHelper();
            var qrTicket = QrCodeApi.Create(token, 86400, 123456789, Senparc.Weixin.MP.QrCode_ActionName.QR_SCENE).ticket;
            ViewBag.ticket = WXApiApplication.GetTicket(settings.WeixinAppId, settings.WeixinAppSecret);
            model.ImagePath = HimallIO.GetFilePath(model.ImagePath);
            ViewBag.OpenId = openId;
            ViewBag.QRTicket = qrTicket;
            return View(model);
        }

        //失效
        public ActionResult Invalid(long id)
        {
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            model.ImagePath = HimallIO.GetFilePath(model.ImagePath);
            return View(model);
        }

        public ActionResult Invalidtwo()
        {
            return View();
        }

        //未开始
        public ActionResult NotStart(long id, string openId = "")
        {
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            model.ImagePath = HimallIO.GetFilePath(model.ImagePath);
            ViewBag.OpenId = openId;
            return View(model);
        }

        //已领取过
        public ActionResult HasReceive(long id)
        {
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            model.ImagePath = HimallIO.GetFilePath(model.ImagePath);
            return View(model);
        }

        //红包已被领取完
        public ActionResult HaveNot(long id)
        {
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            return View(model);
        }

        //未关注
        public ActionResult NotAttention(long id)
        {
            BonusModel model = new BonusModel(this._BonusService.Get(id));
            var settings = SiteSettingApplication.SiteSettings;
            //TODO:改成统一的方式取 Token
            var token = WXApiApplication.TryGetToken(settings.WeixinAppId, settings.WeixinAppSecret);

            SceneHelper helper = new SceneHelper();
            SceneModel scene = new SceneModel(QR_SCENE_Type.Bonus, model);
            int sceneId = helper.SetModel(scene);
            var ticket = QrCodeApi.Create(token, 86400, sceneId, Senparc.Weixin.MP.QrCode_ActionName.QR_SCENE).ticket;
            ViewBag.ticket = ticket;
            return View("~/Areas/Mobile/Templates/Default/Views/Bonus/NotAttention.cshtml", model);
        }

        /// <summary>
        /// 设置为分享状态
        /// </summary>
        [HttpPost]
        public ActionResult SetShare(long id, string openId = "")
        {
            this._BonusService.SetShare(id, openId);
            return Json<dynamic>(true);
        }

        /// <summary>
        /// 拆红包
        /// </summary>
        /// <param name="id">红包id</param>
        /// <param name="openId">微信id</param>
        /// <param name="isShare">是否分享，1：已分享，0：没分享</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Receive(long id, string openId = "", string unionId = "")
        {
            //（3.3版本因为要兼容小程序领取现金红包，BonusReceiveInfo表中的OpenId改为存储Unionid）
            ReceiveModel obj = (ReceiveModel)this._BonusService.Receive(id, openId, unionId);
            var bonus = this._BonusService.Get(id);
            BonusModel model = new BonusModel(bonus);
            if (obj.State == ReceiveStatus.CanReceive)
            {
                return Redirect("/m-weixin/Bonus/Completed/" + model.Id + "?openId=" + unionId + "&price=" + obj.Price);
            }
            if (obj.State == ReceiveStatus.CanReceiveNotAttention)
            {
                return Redirect("/m-weixin/Bonus/CanReceiveNotAttention/" + model.Id + "?openId=" + unionId + "&price=" + obj.Price);
            }
            else if (obj.State == ReceiveStatus.Receive)
            {
                return Redirect("/m-weixin/Bonus/HasReceive/" + model.Id);
            }
            else if (obj.State == ReceiveStatus.HaveNot)
            {
                return Redirect("/m-weixin/Bonus/HaveNot/" + model.Id);
            }
            else if (obj.State == ReceiveStatus.NotAttention)
            {
                return Redirect("/m-weixin/Bonus/NotAttention/" + model.Id);
            }
            else if (obj.State == ReceiveStatus.Invalid)
            {
                return Redirect("/m-weixin/Bonus/Invalid/" + model.Id);
            }
            else
            {
                throw new Exception("领取发生异常");
            }
        }

    }
}