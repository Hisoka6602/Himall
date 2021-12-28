using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Himall.API
{
    public class RegisterController : BaseApiController
    {
        private static string _encryptKey = Guid.NewGuid().ToString("N");
        [System.Web.Mvc.HttpPost]
        public object PostRegisterUser(RegisterUserModel user)
        {
            dynamic result = new Result();
            try
            {
                var email = "";
                //普通注册
                if (user.userName != null && user.password != null && user.userName != "" && user.password != "")
                {
                    string userName = user.userName;
                    string password = user.password;
                    email = user.email;
                    string code = user.code;

                    var pluginId = "";

                    if (!string.IsNullOrEmpty(email) && Core.Helper.ValidateHelper.IsEmail(email))
                    {
                        pluginId = "Himall.Plugin.Message.Email";

                        string cacheCode = MessageApplication.GetMessageCacheCode(email, pluginId);
                        if (cacheCode == null || cacheCode != code)
                        {
                            return new { success = false, ErrorMsg = "验证码输入错误或者已经超时" };
                        }
                    }


                    Regex reg = new Regex("^[a-zA-Z0-9_\u4e00-\u9fa5]+$");
                    if (!reg.IsMatch(userName) || userName.Length < 4 || userName.Length > 20)
                    {
                        throw new HimallException("用户名由4-20个中文英文数字字母下划线组成");
                    }

                    var member = ServiceProvider.Instance<MemberService>.Create.Register(userName, password,(int)PlatformType.Android, string.Empty, email, 0);

                    if (member == null)
                    {
                        result = ErrorResult("注册失败", 104);
                    }
                    else
                    {
                        //手机注册直接绑定手机
                        if (Core.Helper.ValidateHelper.IsMobile(userName))
                        {
                            pluginId = "Himall.Plugin.Message.SMS";
                            member.CellPhone = userName;
                            ServiceProvider.Instance<MemberService>.Create.UpdateMember(member);
                            ServiceProvider.Instance<MessageService>.Create.UpdateMemberContacts(new Entities.MemberContactInfo()
                            {
                                Contact = userName,
                                ServiceProvider = pluginId,
                                UserId = member.Id,
                                UserType = Entities.MemberContactInfo.UserTypes.General
                            });
                        }

                        //注册赠送优惠券
                        int num = this.RegisterSendCoupon(member.Id, member.UserName, out List<CouponModel> couponlist);

                        //注册送积分
                        MemberApplication.AddIntegel(member); //给用户加积分//执行登录后初始化相关操作

                        result.success = true;
                        result.UserId = member.Id.ToString();
                        result.CouponNum = num;
                        result.CopuonInfo = couponlist;
                        string memberId = UserCookieEncryptHelper.Encrypt(member.Id, "Mobile");
                        //WebHelper.SetCookie(CookieKeysCollection.HIMALL_USER_KEY(platformType), memberId, DateTime.MaxValue);
                    }
                }
                //信任登录并且不绑定，后台给一个快速注册
                else
                {
                    string username = DateTime.Now.ToString("yyMMddHHmmssffffff");
                    var member = ServiceProvider.Instance<MemberService>.Create.QuickRegister(username, string.Empty, user.oauthNickName, user.oauthType, user.oauthOpenId, (int)PlatformType.Android, user.unionid, user.sex, user.headimgurl, Entities.MemberOpenIdInfo.AppIdTypeEnum.Normal);

                    //注册赠送优惠券
                    List<CouponModel> couponlist = new List<CouponModel>();
                    int num = 0;
                    if(member.IsNewAccount)
                        num = this.RegisterSendCoupon(member.Id, member.UserName, out couponlist);

                    //注册送积分
                    MemberApplication.AddIntegel(member); //给用户加积分//执行登录后初始化相关操作

                    string memberId = UserCookieEncryptHelper.Encrypt(member.Id, "Mobile");
                    //WebHelper.SetCookie(CookieKeysCollection.HIMALL_USER_KEY(platformType), memberId);
                    result.success = true;
                    result.UserId = member.Id.ToString();
                    result.CouponNum = num;
                    result.CouponModel = couponlist;
                }


            }
            catch (Exception ex)
            {
                result = ErrorResult(ex.Message, 104);
            }
            return result;
        }
        #region 重写方法
        protected override bool CheckContact(string contact, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!string.IsNullOrWhiteSpace(contact))
            {
                var userMenberInfo = Application.MemberApplication.GetMemberByContactInfo(contact);
                if (userMenberInfo != null)
                {
                    errorMessage = "手机或邮箱号码已经存在";
                    Cache.Insert(_encryptKey + contact, string.Format("{0}:{1:yyyyMMddHHmmss}", userMenberInfo.Id, userMenberInfo.CreateDate), DateTime.Now.AddHours(1));
                    return false;
                }
                else
                {//不存在，可以注册
                    return true;
                }
            }
            return false;
        }
        #endregion
        /// <summary>
        /// 注册赠送优惠券
        /// </summary>
        /// <returns></returns>
        private int RegisterSendCoupon(long Id, string UserName, out List<CouponModel> couponlist)
        {
            Log.Info("注册赠送优惠券方法进入");
            var CouponSendByRegisterService = ServiceProvider.Instance<CouponSendByRegisterService>.Create;
            var CouponService = ServiceProvider.Instance<CouponService>.Create;
            return CouponApplication.RegisterSendCoupon(Id, UserName,out couponlist);
        }

        public object GetRegisterType()
        {
            var siteSetting = SiteSettingApplication.SiteSettings;
            return new { success = true, RegisterType = siteSetting.RegisterType, MobileVerifOpen = siteSetting.MobileVerifOpen, EmailVerifOpen = siteSetting.EmailVerifOpen, RegisterEmailRequired = siteSetting.RegisterEmailRequired };
        }

        /// <summary>
        /// 获取会员注册协议
        /// </summary>
        /// <returns></returns>
        public object GetRegisterAgreement()
        {
            var iAgreement = SystemAgreementApplication.GetAgreement(Entities.AgreementInfo.AgreementTypes.Buyers);

            return new { success = true, AgreementContent = (iAgreement == null ? string.Empty : iAgreement.AgreementContent) };
        }
    }
}
