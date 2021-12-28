using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.Core.Plugins;
using Himall.SmallProgAPI.Model.ParamsModel;
using Himall.Web.Framework;
using Himall.Application;
using System;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Results;
using System.IO;
using Himall.Core.Plugins.Message;
using Himall.DTO.CacheData;
using System.Web;

namespace Himall.SmallProgAPI
{
    [ApiExceptionFilter]
    public class BaseApiController<TUser> : HiAPIController<TUser>
    {
        /// <summary>
        /// 检测用户登录信息是否有效
        /// </summary>
        public virtual void CheckUserLogin()
        {
            if (CurrentUser == null)
            {
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "NOUser");
            }
        }


        /// <summary>
        /// 获取图形验证码
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetImageCheckCode()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            var id = Guid.NewGuid().ToString("N");
            Cache.Insert("ImageCheckCode:" + id, code, 600);
            var file = new System.Web.Mvc.FileContentResult(image.ToArray(), "image/png");
            return JsonResult<dynamic>(new
            {
                Id = id,
                file.ContentType,
                file.FileContents,
                file.FileDownloadName
            });
        }
        /// <summary>
        /// 获取图形验证码
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetImageCheckCodGetMemberByOpenIde()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            var id = Guid.NewGuid().ToString("N");
            Cache.Insert("ImageCheckCode:" + id, code, 600);
            var file = new System.Web.Mvc.FileContentResult(image.ToArray(), "image/png");
            return JsonResult<dynamic>(new
            {
                Id = id,
                file.ContentType,
                file.FileContents,
                file.FileDownloadName
            });
        }

        /// <summary>
        /// 发送手机或邮箱验证码
        /// </summary>
        /// <param name="imageCheckCode"></param>
        /// <param name="contact"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetPhoneOrEmailCheckCode(string contact, string id = null, string imageCheckCode = null, bool checkBind = false)
        {
            if (CurrentUser == null)
            {
                if (string.IsNullOrEmpty(imageCheckCode))
                    return Json(ErrorResult<int>("请输入验证码"));

                var key = "ImageCheckCode:" + id;
                var systemCheckCode = Cache.Get<string>(key);
                if (systemCheckCode == null)
                    return Json(ErrorResult<int>("验证码已过期"));

                if (systemCheckCode.ToLower() != imageCheckCode.ToLower())
                    return Json(ErrorResult<int>("验证码错误"));
                else
                    Cache.Remove(key);
            }

            string pluginId="";

            if (Core.Helper.ValidateHelper.IsMobile(contact))
            {
                pluginId = "Himall.Plugin.Message.SMS";
            }
            if (!string.IsNullOrEmpty(contact) && Core.Helper.ValidateHelper.IsEmail(contact))
            {
                pluginId = "Himall.Plugin.Message.Email";
            }
            var isexistcontant = MessageApplication.GetMemberContactsInfo(pluginId, contact, Entities.MemberContactInfo.UserTypes.General);
            if (checkBind && isexistcontant != null)
            {
                return Json(ErrorResult<int>(contact + "已经绑定过了！"));
            }
            MessageApplication.SendMessageCode(contact, pluginId);
            return JsonResult<int>(msg: "验证码发送成功");
        }

        /// <summary>
        /// 验证手机或邮箱验证码
        /// </summary>
        /// <param name="checkCode">验证码</param>
        /// <param name="contact">手机号或邮箱</param>
        /// <returns></returns>
        public JsonResult<Result<string>> GetCheckPhoneOrEmailCheckCode(string checkCode, string contact)
        {
            if (string.IsNullOrEmpty(checkCode))
                return Json(ErrorResult<string>("请输入验证码"));

            string pluginId = "";

            if (Core.Helper.ValidateHelper.IsMobile(contact))
            {
                pluginId = "Himall.Plugin.Message.SMS";
            }
            if (!string.IsNullOrEmpty(contact) && Core.Helper.ValidateHelper.IsEmail(contact))
            {
                pluginId = "Himall.Plugin.Message.Email";
            }
            var cacheCode = MessageApplication.GetMessageCacheCode(contact, pluginId);

            if (cacheCode != null && cacheCode == checkCode)
                return OnCheckCheckCodeSuccess(contact);
            else
                return Json(ErrorResult<string>("验证码输入错误"));
        }

        /// <summary>
        /// 是否强制绑定手机号
        /// </summary>
        /// <returns></returns>
        public JsonResult<Result<string>> IsConBindSms()
        {
            if (CurrentUser == null)
                return Json(ErrorResult<string>("未登录"));

            var isBind = MessageApplication.IsOpenBindSms(CurrentUserId);
            return Json(ApiResult(isBind, "", ""));
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="contact">手机号或邮箱</param>
        /// <param name="password">密码</param>
        /// <param name="repeatPassword">确认密码</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult<Result<int>> PostChangePassword(LoginModPwdModel value)
        {
            if (!string.IsNullOrEmpty(value.Certificate))
                return ChangePassowrdByCertificate(value.Certificate, value.Password);

            return ChangePasswordByOldPassword(value.OldPassword, value.Password);
        }

        /// <summary>
        /// 修改支付密码
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        [HttpPost,HttpGet]
        public object PostChangePayPwd(LoginModPwdModel value)
        {
            if (!string.IsNullOrEmpty(value.Certificate))
            {
                return ChangePayPwdByCertificate(value.Certificate, value.Password);
            }
            return ChangePayPwdByOldPassword(value.OldPassword, value.Password);
        }

        /// <summary>
        /// 根据验证码验证成功后的凭证修改密码
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual JsonResult<Result<int>> ChangePayPwdByCertificate(string certificate, string password)
        {
            return Json(ErrorResult<int>(""));
        }

        /// <summary>
        /// 根据旧密码修改密码
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual JsonResult<Result<int>> ChangePayPwdByOldPassword(string oldPassword, string password)
        {
            return Json(ErrorResult<int>(""));
        }

        /// <summary>
        /// 根据验证码验证成功后的凭证修改密码
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual JsonResult<Result<int>> ChangePassowrdByCertificate(string certificate, string password)
        {
            return Json(ErrorResult<int>(""));
        }

        /// <summary>
        /// 根据旧密码修改密码
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual JsonResult<Result<int>> ChangePasswordByOldPassword(string oldPassword, string password)
        {
            return Json(ErrorResult<int>(""));
        }

        /// <summary>
        /// 发送验证码之前检查联系方式是否存在
        /// </summary>
        /// <param name="contact"></param>
        /// <param name="errorMessage">手机号不存在时的提示</param>
        /// <returns></returns>
        protected virtual bool CheckContact(string contact, out string errorMessage)
        {
            errorMessage = string.Empty;
            return false;
        }

        protected virtual JsonResult<Result<string>> OnCheckCheckCodeSuccess(string contact)
        {
            var certificate = this.CreateCertificate(contact);
            return JsonResult(certificate);
        }

        /// <summary>
        /// 短信或邮箱验证成功后创建用户凭证用于后续操作
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        protected virtual string CreateCertificate(string contact)
        {
            return string.Empty;
        }

        protected override TUser GetUser()
        {
            throw new NotImplementedException();
        }
    }

    public class BaseApiController : BaseApiController<Entities.MemberInfo>
    {
        /// <summary>
        /// 微信小程序信任登录服务标识
        /// </summary>
        public const string SmallProgServiceProvider = "WeiXinSmallProg";
        public override void CheckUserLogin()
        {
            base.CheckUserLogin();
            if (CurrentUser.Disabled)
            {
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "openId");
            }
        }


        public string UploadPhoto(string strPhoto)
        {
            string url = string.Empty;
            string fullPath = "/Storage/Member/" + CurrentUser.Id + "/headImage.jpg";
            try
            {
                byte[] bytes = Convert.FromBase64String(strPhoto.Replace("data:image/jpeg;base64,", ""));
                MemoryStream memStream = new MemoryStream(bytes);
                Core.HimallIO.CreateFile(fullPath, memStream, FileCreateType.Create);
                url = fullPath;
            }
            catch (Exception ex)
            {
                Core.Log.Error("头像上传异常：" + ex);
            }
            return url;
        }
        protected override Entities.MemberInfo GetUser()
        {
            var openId = this.CurrentUserOpenId;
            if (!string.IsNullOrWhiteSpace(openId))
            {
                var userInfo = Application.MemberApplication.GetMemberByOpenId(SmallProgServiceProvider, openId);
                return userInfo;
            }

            return null;
        }
        /// <summary>
        /// 小程序使用openId标识用户
        /// </summary>
        public string CurrentUserOpenId
        {
            get
            {
                string userkey = "";
                userkey = WebHelper.GetQueryString("openId");
                if (string.IsNullOrWhiteSpace(userkey))
                {
                    userkey = WebHelper.GetFormString("openId");
                }
                return userkey;
            }
        }
        public new long CurrentUserId
        {
            get
            {
                if (CurrentUser != null)
                {
                    return CurrentUser.Id;
                }
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "openId");
            }
        }

        /// <summary>
        /// 检测是否已开启门店功能
        /// </summary>

        protected void CheckOpenStore()
        {
            bool isOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            if (!isOpenStore)
                throw new Core.HimallException("门店未授权！");
        }
    }

    /// <summary>
    /// 小程序
    /// </summary>
    public class SmallProgAPIController : HiAPIController<MemberData>
    {
        protected override MemberData GetUser()
        {
            var openId = WebHelper.GetQueryString("openId");

            if (string.IsNullOrEmpty(openId)) {
                openId = HttpContext.Current.Request.Form["openId"];
            }
            if(string.IsNullOrEmpty(openId))
                return null;
            return MemberApplication.GetMemberData("WeiXinSmallProg", openId);
        }

        protected void CheckOpenStore()
        {
            bool isOpenStore = SiteSettingApplication.SiteSettings != null && SiteSettingApplication.SiteSettings.IsOpenStore;
            if (!isOpenStore)
                throw new Core.HimallException("门店未授权！");
        }
        public  void CheckUserLogin()
        {
            if (CurrentUser.Disabled)
            {
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "openId");
            }
        }

        /// <summary>
        /// 当前用户Id
        /// </summary>
        public new long CurrentUserId
        {
            get
            {
                if (CurrentUser != null)
                {
                    return CurrentUser.Id;
                }
                return 0;
            }
        }
    }
}
