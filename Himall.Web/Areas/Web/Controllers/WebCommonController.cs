using Aliyun.Acs.afs.Model.V20180112;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Web.Framework;
using System;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class WebCommonController : Controller
    {
        [HttpGet]
        public ActionResult JsMapQQKey()
        {
            return Content(SiteSettingApplication.GetJsMapQQKey());//
        }
        /// <summary>
        /// 滑动验证
        /// </summary>
        /// <param name="issendcode">滑动验证通过是否发短信，true:验证通过发送，false:不发送</param>
        /// <param name="contact">手机号或邮箱</param>
        /// <param name="checkcontacttype">验证联系方式类型:0不验证，1验证手机，2验证邮箱,3手机邮箱都验证</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult AuthenticateSig(string sessionId,string sig,string scene,string token,string appkey,bool issendcode=false,string contact=null,int checkcontacttype = 0)
        {
            //if (string.IsNullOrEmpty(SiteSettingApplication.SiteSettings.AccessKeyID) || string.IsNullOrEmpty(SiteSettingApplication.SiteSettings.AccessKeySecret)) {
            //    return Json(new { success = true });
            //}
            var site = SiteSettingApplication.SiteSettings;
            if(!site.IsTheftBrush)
                return Json(new { success = false, msg = "未开启盗刷验证！" });
            if (string.IsNullOrEmpty(site.AccessKeyID) || string.IsNullOrEmpty(site.AccessKeySecret))
                return Json(new { success = false, msg = "盗刷验证配置错误！" });

            if (issendcode)
            {
                if (checkcontacttype == 1)
                {
                    //验证手机号
                    if (!Core.Helper.ValidateHelper.IsMobile(contact))
                        return Json(new { success = false, msg = "手机号码格式有误，请输入正确的手机号！" });
                }
                else if (checkcontacttype == 2)
                {
                    //验证邮箱号
                    if (string.IsNullOrEmpty(contact) || !Core.Helper.ValidateHelper.IsEmail(contact))
                        return Json(new { success = false, msg = "邮箱格式有误，请输入正确的邮箱！" });
                }
                else if (checkcontacttype == 3)
                {
                    if (string.IsNullOrEmpty(contact) && !Core.Helper.ValidateHelper.IsEmail(contact) || !Core.Helper.ValidateHelper.IsMobile(contact))
                        return Json(new { success = false, msg = "手机号或邮箱格式有误，请输入正确的手机号或邮箱！" });
                }
            }

            Log.Error("AuthenticateSig1");
            IClientProfile profile = DefaultProfile.GetProfile("cn-hangzhou", SiteSettingApplication.SiteSettings.AccessKeyID, SiteSettingApplication.SiteSettings.AccessKeySecret);
            var client = new DefaultAcsClient(profile);
            DefaultProfile.AddEndpoint("cn-hangzhou", "cn-hangzhou", "afs", "afs.aliyuncs.com");

            Log.Error("AuthenticateSig2AppKey:" + appkey + "--token:" + token + "--sig:" + sig + "--sessionId:" + sessionId + "--scene:" + scene);
            AuthenticateSigRequest request = new AuthenticateSigRequest();
            request.SessionId = sessionId;// 必填参数，从前端获取，不可更改，android和ios只传这个参数即可
            request.Sig = sig;// 必填参数，从前端获取，不可更改
            request.Token = token;// 必填参数，从前端获取，不可更改
            request.Scene = scene;// 必填参数，从前端获取，不可更改
            request.AppKey = appkey;// 必填参数，后端填写
            request.RemoteIp = Himall.Core.Helper.WebHelper.GetIP();// 必填参数，后端填写

            Log.Error("AuthenticateSig3：" + Request.ServerVariables.Get("Remote_Addr").ToString() + "--getIP：" + Himall.Core.Helper.WebHelper.GetIP());
            try
            {
                AuthenticateSigResponse response = client.GetAcsResponse(request);// 返回code 100表示验签通过，900表示验签失败
                Log.Error("AuthenticateSig4：" + response.Code);
                if (response.Code == 100)
                {
                    Log.Error("AuthenticateSig5：" + issendcode);
                    if (issendcode)
                        MessageApplication.SendMessageCodeDirect(contact);//发送短信
                    return Json(new { success = true });
                }
                else {
                    Log.Error("AuthenticateSig6：" + response.Code);
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
    }
}