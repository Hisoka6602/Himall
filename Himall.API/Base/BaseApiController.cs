using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace Himall.API
{
    //[HimallApiActionFilter]
    [Himall.API.ApiExceptionFilter]
    public class BaseApiController<TUser> : HiAPIController<TUser>
    {
        /// <summary>
        /// 检测用户登录信息是否有效
        /// </summary>
        public virtual void CheckUserLogin()
        {
            if (CurrentUser == null)
            {
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "用户已过期，请重新登录");
            }
        }

        /// <summary>
        /// 获取图形验证码
        /// </summary>
        /// <returns></returns>
        public object GetImageCheckCode()
        {
            string code;
            var image = Core.Helper.ImageHelper.GenerateCheckCode(out code);
            var id = Guid.NewGuid().ToString("N");
            Cache.Insert("ImageCheckCode:" + id, code, 600);
            var file = new System.Web.Mvc.FileContentResult(image.ToArray(), "image/png");
            return new
            {
                Id = id,
                file.ContentType,
                file.FileContents,
                file.FileDownloadName
            };
        }

        /// <summary>
        /// 发送手机或邮箱验证码
        /// </summary>
        /// <param name="imageCheckCode"></param>
        /// <param name="contact"></param>
        /// <returns></returns>
        public object GetPhoneOrEmailCheckCode(string contact, string id = null, string imageCheckCode = null, bool checkBind = false)
        {
            if (string.IsNullOrEmpty(imageCheckCode))
                return ErrorResult("请输入图形验证码");

            var key = "ImageCheckCode:" + id;
            var systemCheckCode = Cache.Get<string>(key);
            if (systemCheckCode == null)
                return ErrorResult("图形验证码已过期");

            if (systemCheckCode.ToLower() != imageCheckCode.ToLower())
                return ErrorResult("图形验证码错误");
            else
                Cache.Remove(key);

            string msg;
            var checkResult = this.CheckContact(contact, out msg);
            if (!checkResult)
                return ErrorResult(string.IsNullOrEmpty(msg) ? "手机或邮箱号码不存在" : msg);
            string pluginId = "";

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
                return ErrorResult(contact + "已经绑定过了！");
            }
            MessageApplication.SendMessageCode(contact, pluginId);
            return SuccessResult("验证码发送成功");
        }

        /// <summary>
        /// 验证手机或邮箱验证码
        /// </summary>
        /// <param name="checkCode">验证码</param>
        /// <param name="contact">手机号或邮箱</param>
        /// <returns></returns>
        public object GetCheckPhoneOrEmailCheckCode(string checkCode, string contact)
        {
            if (string.IsNullOrEmpty(checkCode + ""))
                return ErrorResult("请输入验证码");

            PluginInfo pluginInfo;
            var isMobile = Core.Helper.ValidateHelper.IsMobile(contact);
            if (isMobile)
                pluginInfo = PluginsManagement.GetInstalledPluginInfos(Core.Plugins.PluginType.SMS).First();
            else
                pluginInfo = PluginsManagement.GetInstalledPluginInfos(PluginType.Email).First();

           string cacheCode= MessageApplication.GetMessageCacheCode(contact,pluginInfo.PluginId);

            if (cacheCode != null && cacheCode == checkCode)
                return OnCheckCheckCodeSuccess(contact);
            else
                return ErrorResult((isMobile ? "手机" : "邮箱") +"验证码输入错误");
        }

        /// <summary>
        /// 修改用户密码
        /// </summary>
        /// <param name="contact">手机号或邮箱</param>
        /// <param name="password">密码</param>
        /// <param name="repeatPassword">确认密码</param>
        /// <returns></returns>
        [HttpPost]
        public object PostChangePassword(LoginModPwdModel value)
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
        [HttpPost]
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
        protected virtual object ChangePassowrdByCertificate(string certificate, string password)
        {
            return ErrorResult("");
        }

        /// <summary>
        /// 根据旧密码修改密码
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual object ChangePasswordByOldPassword(string oldPassword, string password)
        {
            return ErrorResult("");
        }

        /// <summary>
        /// 根据验证码验证成功后的凭证修改密码
        /// </summary>
        /// <param name="certificate"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual object ChangePayPwdByCertificate(string certificate, string password)
        {
            return ErrorResult("");
        }

        /// <summary>
        /// 根据旧密码修改密码
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        protected virtual object ChangePayPwdByOldPassword(string oldPassword, string password)
        {
            return ErrorResult("");
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

        protected virtual object OnCheckCheckCodeSuccess(string contact)
        {
            var certificate = this.CreateCertificate(contact);
            dynamic result = SuccessResult();
            result.certificate = certificate;
            return result;
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

        protected Result ApiResult(bool success, string msg, int code = 0)
        {
            return new Result
            {
                success = success,
                msg = msg,
                code = code
            };
        }

        protected Result SuccessResult(string msg = null, int code = 0)
        {
            return ApiResult(true, msg, code);
        }

        protected Result ErrorResult(string msg, int code = 0)
        {
            return ApiResult(false, msg, code);
        }

        protected override TUser GetUser()
        {
            throw new NotImplementedException();
        }

        public class Result : System.Dynamic.DynamicObject
        {
            #region 字段
            private Dictionary<string, object> _members;
            #endregion

            #region 构造函数
            public Result()
            {
                _members = new Dictionary<string, object>();
            }
            #endregion

            #region 属性
            public bool success
            {
                get
                {
                    var value = _members["success"];
                    if (value != null)
                        return (bool)value;
                    return false;
                }
                set
                {
                    _members["success"] = value;
                }
            }

            public string msg
            {
                get
                {
                    var value = _members["msg"];
                    if (value != null)
                        return (string)value;
                    return string.Empty;
                }
                set
                {
                    _members["msg"] = value;
                }
            }

            /// <summary>
            /// 状态
            /// <para>1表成功，0表示未处理（默认）</para>
            /// </summary>
            public int code
            {
                get
                {
                    var value = _members["code"];
                    if (value != null)
                        return (int)value;
                    return 0;
                }
                set
                {
                    _members["code"] = value;
                }
            }
            /// <summary>
            /// 数据总量
            /// </summary>
            public int total
            {
                get
                {
                    var value = _members["total"];
                    if (value != null)
                        return (int)value;
                    return 0;
                }
                set
                {
                    _members["total"] = value;
                }
            }
            #endregion

            #region 重写方法
            public override bool TrySetMember(System.Dynamic.SetMemberBinder binder, object value)
            {
                if (!_members.ContainsKey(binder.Name))
                {
                    _members.Add(binder.Name, value);
                    return true;
                }

                return base.TrySetMember(binder, value);
            }

            public override bool TryGetMember(System.Dynamic.GetMemberBinder binder, out object result)
            {
                if (_members.ContainsKey(binder.Name))
                {
                    result = _members[binder.Name];
                    return true;
                }

                return base.TryGetMember(binder, out result);
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                return base.GetDynamicMemberNames().Concat(_members.Keys);
            }
            #endregion
        }
    }

    [WebApiAuth]
    public class BaseApiController : BaseApiController<Entities.MemberInfo>
    {
        public override void CheckUserLogin()
        {
            base.CheckUserLogin();
            if (CurrentUser.Disabled)
            {
                throw new HimallApiException(ApiErrorCode.Invalid_User_Key_Info, "用户已被冻结");
            }
        }
        protected override Entities.MemberInfo GetUser()
        {
            var userId = this.CurrentUserId;
            if (userId > 0)
            {
                var userInfo = Application.MemberApplication.GetMember(this.CurrentUserId);
                var siteInfo = Application.SiteSettingApplication.SiteSettings;
                if (siteInfo != null)
                {
                    if (!(siteInfo.IsOpenPC || siteInfo.IsOpenH5 || siteInfo.IsOpenMallSmallProg || siteInfo.IsOpenApp))//授权模块影响会员折扣功能
                    {
                        userInfo.MemberDiscount = 1M;
                    }

                }
                return userInfo;
            }

            return null;
        }
    }
}
