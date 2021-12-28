using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.SmallProgAPI.Model.ParamsModel;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;

namespace Himall.SmallProgAPI
{
    public class UserCenterController : BaseApiController
    {
        private MemberService _MemberService;
        private MessageService _iMessageService;
        private static string _encryptKey = Guid.NewGuid().ToString("N");
        /// <summary>
        /// 获取组合购详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetSendPhoneOrEmailCode()
        {

            return JsonResult<dynamic>(new
            {
                success = true
            });
        }
        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="pluginId"></param>
        /// <param name="destination"></param>
        /// <param name="checkBind"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetSendCode(string openId, string pluginId, string destination = null, bool checkBind = false)
        {
            CheckUserLogin();

            long id = CurrentUser.Id;
            var member = MemberApplication.GetMember(id);
            if (string.IsNullOrEmpty(destination))
                destination = CurrentUser.CellPhone;

            if (string.IsNullOrEmpty(destination))
                return JsonResult<dynamic>(new
                {
                    success = false,
                    msg = pluginId.ToLower() == "himall.plugin.message.email" ? "请输入邮箱" : "请输入手机号码"
                });

            if (checkBind && _iMessageService.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
            {
                return JsonResult<dynamic>(new
                {
                    success = false,
                    msg = destination + "已经绑定过了！"
                });
            }
            MemberApplication.CheckContactInfoHasBeenUsed(pluginId, destination);
            var user = new Core.Plugins.Message.MessageUserInfo() { UserName = CurrentUser.UserName};
            MessageApplication.SendMessageCode(destination, pluginId, user);
            return JsonResult<dynamic>(new
            {
                success = true,
                msg = destination + "发送成功!"
            });
        }
        /// <summary>
        /// 验证验证码
        /// </summary>
        /// <param name="pluginId"></param>
        /// <param name="code"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetCheckCode(string openId, string pluginId, string code, string destination)
        {
            var cacheCode = MessageApplication.GetMessageCacheCode(destination, pluginId);
            var member = CurrentUser;
            var mark = "";
            if (cacheCode != null && cacheCode == code)
            {
                var service = _iMessageService;
                if (service.GetMemberContactsInfo(pluginId, destination, Entities.MemberContactInfo.UserTypes.General) != null)
                {
                    return JsonResult<dynamic>(new
                    {
                        success = false,
                        msg = destination + "已经绑定过了！"
                    });
                }
                if (pluginId.ToLower().Contains("email"))
                {
                    member.Email = destination;
                    mark = "邮箱";
                }
                else if (pluginId.ToLower().Contains("sms"))
                {
                    member.CellPhone = destination;
                    mark = "手机";
                }

                MemberApplication.UpdateMember(member);
                MessageApplication.UpdateMemberContacts(new Entities.MemberContactInfo()
                {
                    Contact = destination,
                    ServiceProvider = pluginId,
                    UserId = CurrentUser.Id,
                    UserType = Entities.MemberContactInfo.UserTypes.General
                });
                MessageApplication.RemoveMessageCacheCode(destination, pluginId);

                Himall.Entities.MemberIntegralRecordInfo info = new Himall.Entities.MemberIntegralRecordInfo();
                info.UserName = member.UserName;
                info.MemberId = member.Id;
                info.RecordDate = DateTime.Now;
                info.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Reg;
                info.ReMark = "绑定" + mark;
                var memberIntegral = ObjectContainer.Current.Resolve<MemberIntegralConversionFactoryService>().Create(Himall.Entities.MemberIntegralInfo.IntegralType.Reg);
                ObjectContainer.Current.Resolve<MemberIntegralService>().AddMemberIntegral(info, memberIntegral);

                //去掉会员推广
                /*
                if (member.InviteUserId > 0)
                {
                    var inviteMember = _MemberService.GetMember(member.InviteUserId);
                    if (inviteMember != null)
                        ObjectContainer.Current.Resolve<MemberInviteService>().AddInviteIntegel(member, inviteMember, true);
                }
                */

                return JsonResult<dynamic>(new
                {
                    success = true,
                    msg = "绑定成功!"
                });
            }
            else
            {
                return JsonResult<dynamic>(new
                {
                    success = false,
                    msg = "绑定失败!"
                });
            }
        }

        private string MoveImages(string image, long userId)
        {
            if (!string.IsNullOrWhiteSpace(image))
            {
                var ext = Path.GetFileName(image);
                string ImageDir = string.Empty;
                //转移图片
                ;
                string relativeDir = "/Storage/Member/" + userId + "/";
                string fileName = "headImage.jpg";
                if (image.Replace("\\", "/").Contains("/temp/"))//只有在临时目录中的图片才需要复制
                {
                    var de = image.Substring(image.LastIndexOf("/temp/")).Split('?')[0];
                    Core.HimallIO.CopyFile(de, relativeDir + fileName, true);
                    return relativeDir + fileName;
                }  //目标地址
                else if (image.Contains("/Storage/"))
                {
                    return image.Substring(image.LastIndexOf("/Storage/"));
                }
                else if (image.ToLower().StartsWith("http://") || image.ToLower().StartsWith("https://"))
                {
                    return image;
                }
                else
                {
                    return "";
                }
            }
            else
            {
                return "";
            }
        }
        /// <summary>
        /// 保存用户信息
        /// </summary>
        /// <param name="openId"></param>
        /// <param name="photoUrl"></param>
        /// <param name="sex"></param>
        /// <param name="birthday"></param>
        /// <param name="qq"></param>
        /// <param name="nickname"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetChangeUserInfo(string openId, string photo = "", string realName = "", int sex = 0, string birthday = "", string qq = "", string nickname = "")
        {
            CheckUserLogin();
            dynamic d = new System.Dynamic.ExpandoObject();
            long id = CurrentUser.Id;
            var member = MemberApplication.GetMember(id);
            MemberUpdate model = new MemberUpdate();
            model.Photo = photo;
            model.RealName = realName;
            model.Sex = (SexType)sex;
            DateTime dBirthday = DateTime.Now;
            if (DateTime.TryParse(birthday, out dBirthday))
            {
                model.BirthDay = dBirthday;
            }
            else
            {
                model.BirthDay = null;
            }
            model.QQ = qq;
            model.Nick = nickname;
            if (string.IsNullOrWhiteSpace(model.RealName))
            {
                throw new HimallException("真实姓名必须填写");
            }
            if (!string.IsNullOrWhiteSpace(model.Photo))
            {
                model.Photo = MoveImages(model.Photo, id);
            }
            else
            {
                model.Photo = member.Photo;
            }
            model.RealName = realName;
            model.Sex = Enum.IsDefined(typeof(SexType), sex) ? (SexType)sex : member.Sex;
            model.QQ = qq;
            model.Nick = nickname;
            model.Id = CurrentUser.Id;
            MemberApplication.UpdateMemberInfo(model);
            //重新获取会员数据，返回新的会员数据
            member = MemberApplication.GetMember(id);
            d.UserName = member.UserName;//用户名
            d.RealName = member.RealName;//真实姓名
            d.Nick = member.Nick;//昵称 
            d.UserId = member.Id.ToString();
            d.CellPhone = member.CellPhone;//绑定的手机号码
            d.Photo = String.IsNullOrEmpty(member.Photo) ? "" : HimallIO.GetRomoteImagePath(member.Photo);//头像
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(member.Id);
            var statistic = StatisticApplication.GetMemberOrderStatistic(id, true);
            d.AllOrders = statistic.OrderCount;
            d.WaitingForPay = statistic.WaitingForPay;
            d.WaitingForRecieve = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(id);
            d.WaitingForDelivery = statistic.WaitingForDelivery;
            d.WaitingForComments = statistic.WaitingForComments;
            d.RefundOrders = statistic.RefundCount;
            d.Email = member.Email;
            d.FavoriteShop = ShopApplication.GetUserConcernShopsCount(member.Id); //收藏的店铺数
            d.FavoriteProduct = FavoriteApplication.GetFavoriteCountByUser(member.Id); //收藏的商品数

            d.Counpon = MemberApplication.GetAvailableCouponCount(id);

            d.Integral = MemberIntegralApplication.GetAvailableIntegral(member.Id);//我的积分
            d.Balance = MemberCapitalApplication.GetBalanceByUserId(member.Id);//我的资产
            d.IsOpenRechargePresent = SiteSettingApplication.SiteSettings.IsOpenRechargePresent;
            var phone = SiteSettingApplication.SiteSettings.SitePhone;
            d.ServicePhone = string.IsNullOrEmpty(phone) ? "" : phone;
            d.IsDistributor = (currentDistributor != null && currentDistributor.DistributionStatus == (int)DistributorStatus.Audited);
            d.CanRecharge = SiteSettingApplication.SiteSettings.IsOpenRecharge;//是否开启了充值
            d.RealName = member.RealName;
            d.Sex = member.Sex.GetHashCode();
            d.BirthDay = member.BirthDay.HasValue ? member.BirthDay.Value.ToString("yyyy-MM-dd") : "";
            d.QQ = member.QQ;
            d.Nick = member.Nick;
            return JsonResult<dynamic>(d);

        }
        /// <summary>
        /// 个人中心主页
        /// </summary>
        /// <returns></returns>
        public new JsonResult<Result<dynamic>> GetUser()
        {
            CheckUserLogin();
            dynamic d = new System.Dynamic.ExpandoObject();
            long id = CurrentUser.Id;
            var member = MemberApplication.GetMember(id);
            DistributorInfo currentDistributor = DistributionApplication.GetDistributor(member.Id);
            d.IsSignIn=MemberApplication.CheckSignInByToday(id);
            d.UserName = member.UserName;//用户名
            d.RealName = member.RealName;//真实姓名
            d.Nick = member.Nick;//昵称 
            d.UserId = member.Id.ToString();
            d.CellPhone = member.CellPhone;//绑定的手机号码
            d.Photo = String.IsNullOrEmpty(member.Photo) ? "" : HimallIO.GetRomoteImagePath(member.Photo);//头像

            var statistic = StatisticApplication.GetMemberOrderStatistic(id, true);
            d.AllOrders = statistic.OrderCount;
            d.WaitingForPay = statistic.WaitingForPay;
            d.WaitingForRecieve = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(id);
            d.WaitingForDelivery = statistic.WaitingForDelivery;
            d.WaitingForComments = statistic.WaitingForComments;
            d.RefundOrders = statistic.RefundCount;

            d.FavoriteShop = ShopApplication.GetUserConcernShopsCount(member.Id); //收藏的店铺数
            d.FavoriteProduct = FavoriteApplication.GetFavoriteCountByUser(member.Id); //收藏的商品数

            d.Counpon = MemberApplication.GetAvailableCouponCount(id);
            d.Email = member.Email;
            d.Integral = MemberIntegralApplication.GetAvailableIntegral(member.Id);//我的积分
            d.Balance = MemberCapitalApplication.GetBalanceByUserId(member.Id);//我的资产
            d.IsOpenRechargePresent = SiteSettingApplication.SiteSettings.IsOpenRechargePresent;
            var phone = SiteSettingApplication.SiteSettings.SitePhone;
            d.ServicePhone = string.IsNullOrEmpty(phone) ? "" : phone;
            d.IsDistributor = (currentDistributor != null && currentDistributor.DistributionStatus == (int)DistributorStatus.Audited);
            d.CanRecharge = SiteSettingApplication.SiteSettings.IsOpenRecharge;//是否开启了充值
            d.RealName = member.RealName;
            d.Sex = member.Sex.GetHashCode();
            d.BirthDay = member.BirthDay.HasValue ? member.BirthDay.Value.ToString("yyyy-MM-dd") : "";
            d.QQ = member.QQ;
            var defaultgrade = MemberGradeApplication.GetGradeByMember(member.Id).GradeName;
            d.GradeName= defaultgrade.Equals("vip0")?"": defaultgrade;
            d.GroupTotal = FightGroupApplication.GetJoinGroupNumber(id); //用户参与的团数量
            return JsonResult<dynamic>(d);
        }

        public JsonResult<Result<dynamic>> GetIntegralRecordList(string openId, int pageNo = 1, int pageSize = 10)
        {
            CheckUserLogin();
            IntegralRecordQuery query = new IntegralRecordQuery
            {
                UserId = CurrentUserId,
                PageNo = pageNo,
                PageSize = pageSize
            };
            var list = MemberIntegralApplication.GetIntegralRecordList(query);
            if (list.Models != null)
            {
                var recordlist = list.Models.Select(a =>
                {
                    var actions = ServiceProvider.Instance<MemberIntegralService>.Create.GetIntegralRecordAction(a.Id);
                    return new
                    {
                        Id = a.Id,
                        MemberId = a.MemberId,
                        UserName = a.UserName,
                        TypeName = (a.TypeId == MemberIntegralInfo.IntegralType.WeiActivity) ? a.ReMark : a.TypeId.ToDescription(),
                        Integral = a.Integral,
                        RecordDate = ((DateTime)a.RecordDate).ToString("yyyy-MM-dd HH:mm:ss"),
                        ReMark = GetRemarkFromIntegralType(a.TypeId, actions, a.ReMark)
                    };
                });
                return JsonResult<dynamic>(recordlist);
            }

            return JsonResult<dynamic>(new int[0]);
        }

        private string GetRemarkFromIntegralType(MemberIntegralInfo.IntegralType type, ICollection<MemberIntegralRecordActionInfo> recordAction, string remark = "")
        {
            if (recordAction == null || recordAction.Count == 0)
                return remark;
            switch (type)
            {

                case MemberIntegralInfo.IntegralType.Consumption:
                    var orderIds = "";
                    foreach (var item in recordAction)
                    {
                        orderIds += item.VirtualItemId + ",";
                    }
                    remark = "订单号：" + orderIds.TrimEnd(',');
                    break;
                default:
                    return remark;
            }
            return remark;
        }

        protected override bool CheckContact(string contact, out string errorMessage)
        {
            errorMessage = string.Empty;
            if (!string.IsNullOrWhiteSpace(contact))
            {
                var userMenberInfo = Application.MemberApplication.GetMemberByContactInfo(contact);
                if (userMenberInfo != null)
                    Cache.Insert(_encryptKey + contact, string.Format("{0}:{1:yyyyMMddHHmmss}", userMenberInfo.Id, userMenberInfo.CreateDate), DateTime.Now.AddHours(1));
                return userMenberInfo != null;
            }

            return false;
        }



        protected override JsonResult<Result<int>> ChangePayPwdByOldPassword(string oldPassword, string password)
        {
            CheckUserLogin();

            var _MemberCapitalService = ServiceProvider.Instance<MemberCapitalService>.Create;

            var hasPayPwd = MemberApplication.HasPayPassword(CurrentUser.Id);

            if (hasPayPwd && string.IsNullOrEmpty(oldPassword))

                return Json(ErrorResult<int>("请输入旧支付密码"));

            if (string.IsNullOrWhiteSpace(password))
                return Json(ErrorResult<int>("请输入新支付密码"));

            if (hasPayPwd)
            {
                var success = MemberApplication.VerificationPayPwd(CurrentUser.Id, oldPassword);
                if (!success)
                    return Json(ErrorResult<int>("旧支付密码错误"));
            }

            _MemberCapitalService.SetPayPwd(CurrentUser.Id, password);

            return Json(SuccessResult<int>(msg: "修改密码成功"));
        }
        protected override string CreateCertificate(string contact)
        {
            //Cache.Remove(_encryptKey + contact);
            var identity = Cache.Get<string>(_encryptKey + contact);
            if (string.IsNullOrWhiteSpace(identity))
            {
                identity = contact;


                identity = SecureHelper.AESEncrypt(identity, _encryptKey);
                if (!string.IsNullOrWhiteSpace(identity))
                {
                    Cache.Insert<string>(_encryptKey + contact, identity);
                }
            }
            return identity;
        }
        protected override JsonResult<Result<int>> ChangePayPwdByCertificate(string certificate, string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return Json(ErrorResult<int>("密码不能为空"));
            if (certificate.Trim().Contains(" "))
            {
                certificate = certificate.Replace(" ", "+");
            }
            certificate = SecureHelper.AESDecrypt(certificate, _encryptKey);
            string contact = certificate.Split(':')[0];
            MemberInfo member = MemberApplication.GetMemberByContactInfo(contact);
            if (member == null)
                throw new HimallException("数据异常");

            var _MemberCapitalService = ServiceProvider.Instance<MemberCapitalService>.Create;

            _MemberCapitalService.SetPayPwd(member.Id, password);
            return JsonResult<int>(msg: "支付密码修改成功");
        }

        /// <summary>
        /// 用户收藏的商品
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public object GetUserCollectionProduct(int pageNo = 1, int pageSize = 16)
        {
            CheckUserLogin();
            if (CurrentUser != null)
            {
                var model = ServiceProvider.Instance<ProductService>.Create.GetUserConcernProducts(CurrentUser.Id, pageNo, pageSize);
                var result = model.Models.ToArray().Select(item =>
                {
                    var pro = ProductManagerApplication.GetProduct(item.ProductId);
                    return new
                    {
                        Id = item.ProductId,
                        Image = HimallIO.GetRomoteProductSizeImage(pro.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_220),
                        ProductName = pro.ProductName,
                        SalePrice = pro.MinSalePrice.ToString("F2"),
                        Evaluation = CommentApplication.GetCommentCountByProduct(pro.Id),
                        Status = ProductManagerApplication.GetProductShowStatus(pro)
                    };
                });
                return new { success = true, data = result, total = model.Total };
            }
            else
            {
                return new Result { success = false, msg = "未登录" };
            }
        }
        /// <summary>
        /// 删除关注商品
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public object GetCancelConcernProduct(long productId)
        {
            CheckUserLogin();
            if (CurrentUser != null)
            {
                if (productId < 1)
                {
                    throw new HimallException("错误的参数");
                }
                ServiceProvider.Instance<ProductService>.Create.DeleteFavorite(productId, CurrentUser.Id);

                return new { success = true };
            }
            else
            {
                return new Result { success = false, msg = "未登录" };
            }
        }

        /// <summary>
        /// 用户收藏的店铺
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public object GetUserCollectionShop(int pageNo = 1, int pageSize = 8)
        {
            CheckUserLogin();
            if (CurrentUser != null)
            {
                var model = ServiceProvider.Instance<ShopService>.Create.GetUserConcernShops(CurrentUser.Id, pageNo, pageSize);

                var result = model.Models.Select(item =>
                {
                    var shop = ShopApplication.GetShop(item.ShopId);
                    var vShop = VshopApplication.GetVShopByShopId(item.ShopId);
                    return new
                    {
                        //Id = item.Id,
                        Id = vShop?.Id ?? 0,
                        ShopId = item.ShopId,
                        Logo = vShop == null ? HimallIO.GetRomoteImagePath(shop.Logo) : HimallIO.GetRomoteImagePath(vShop.Logo),
                        Name = shop.ShopName,
                        Status = shop.ShopStatus,
                        ConcernTime = item.Date,
                        ConcernTimeStr = item.Date.ToString("yyyy-MM-dd"),
                        ConcernCount = FavoriteApplication.GetFavoriteShopCountByShop(item.ShopId)
                    };
                });
                return new { success = true, data = result };
            }
            else
            {
                return new Result { success = false, msg = "未登录" };
            }
        }

        /// <summary>
        /// 取消店铺关注
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public object GetCancelConcernShop(long shopId)
        {
            CheckUserLogin();
            if (CurrentUser != null)
            {
                ServiceProvider.Instance<ShopService>.Create.CancelConcernShops(shopId, CurrentUser.Id);
                return new Result() { success = true, msg = "取消成功！" };
            }
            else
            {
                return new Result { success = false, msg = "未登录" };
            }
        }

        /// <summary>
        /// 用户是否没设密码
        /// </summary>
        /// <returns></returns>
        public object GetIsNotSetPwd()
        {
            CheckUserLogin();

            bool isLoginPwdNotModify = false;//信任登录后密码是否没修改
            if (!string.IsNullOrEmpty(CurrentUser.PasswordSalt))
                isLoginPwdNotModify = CurrentUser.PasswordSalt.StartsWith("o");//如信任登录加密盐第一个字符串是“o”

            var result = new
            {
                IsLoginPwdNotModify = isLoginPwdNotModify,
            };
            return new { success = true, data = result };
        }

        /// <summary>
        /// 根据旧密码修改密码
        /// </summary>
        /// <param name="oldPassword"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public object GetChangePasswordByOld(string oldPassword, string password)
        {
            CheckUserLogin();

            if (string.IsNullOrWhiteSpace(password))
            {
                return new { success = false, msg = "密码不能为空！" };
            }
            var model = CurrentUser;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(oldPassword + "") + model.PasswordSalt);
            bool CanChange = false;
            if (pwd == model.Password)
            {
                CanChange = true;
            }
            if (!string.IsNullOrEmpty(model.PasswordSalt) && model.PasswordSalt.StartsWith("o"))
            {
                CanChange = true;
            }

            if (CanChange)
            {
                Application.MemberApplication.ChangePassword(model.Id, password);
                return new { success = true, msg = "密码修改成功！" };
            }
            else
                return new { success = false, msg = "旧密码错误！" };
        }

        /// <summary>
        /// 获取小程序解密信息
        /// </summary>
        /// <param name="sessionKey">key</param>
        /// <param name="iv">向量128</param>
        /// <param name="encryptedData">要解密的值</param>
        /// <returns>解密后的字符串</returns>
        public object GetAESDecrypt(string session_key, string iv, string encryptedData)
        {
            CheckUserLogin();
            if (CurrentUser != null)
            {
                var result = ApiHelper.AESDecrypt(session_key, iv, encryptedData);
                if (string.IsNullOrEmpty(result))
                    return new { success = false, msg = "解密失败" };
                else
                {
                    WxAppletUserPhoneInfo userPhoneInfo = ApiHelper.GetAppletUserPhoneInfo(result);
                    if (userPhoneInfo != null && !string.IsNullOrEmpty(userPhoneInfo.phoneNumber))
                    {
                        string pluginId = PluginsManagement.GetInstalledPluginInfos(Core.Plugins.PluginType.SMS).First().PluginId;//Himall.Plugin.Message.SMS
                        var contact = Application.MessageApplication.GetMemberContactsInfo(pluginId, userPhoneInfo.phoneNumber, MemberContactInfo.UserTypes.General);
                        bool isnotexistbind = contact == null || string.IsNullOrEmpty(contact.Contact);
                        if (!isnotexistbind)
                        {
                            return new { success = true, data = result, msg = "用户此手机号'" + userPhoneInfo.phoneNumber + "'已被绑定" };
                        }

                        var member = CurrentUser;
                        if (string.IsNullOrEmpty(member.CellPhone))
                        {
                            member.CellPhone = userPhoneInfo.phoneNumber;

                            var _iMemberIntegralConversionFactoryService = ServiceProvider.Instance<MemberIntegralConversionFactoryService>.Create;
                            var _iMemberIntegralService = ServiceProvider.Instance<MemberIntegralService>.Create;
                            var _iMemberInviteService = ServiceProvider.Instance<MemberInviteService>.Create;

                            MemberApplication.UpdateMember(member.Map<DTO.Members>());
                            MessageApplication.UpdateMemberContacts(new MemberContactInfo()
                            {
                                Contact = userPhoneInfo.phoneNumber,
                                ServiceProvider = pluginId,
                                UserId = CurrentUser.Id,
                                UserType = MemberContactInfo.UserTypes.General
                            });
                            MessageApplication.RemoveMessageCacheCode(userPhoneInfo.phoneNumber, pluginId);

                            //如没绑定过送积分
                            if (isnotexistbind)
                            {
                                var info = new MemberIntegralRecordInfo();
                                info.UserName = member.UserName;
                                info.MemberId = member.Id;
                                info.RecordDate = DateTime.Now;
                                info.TypeId = MemberIntegralInfo.IntegralType.Reg;
                                info.ReMark = "绑定手机";
                                var memberIntegral = _iMemberIntegralConversionFactoryService.Create(Himall.Entities.MemberIntegralInfo.IntegralType.Reg);
                                _iMemberIntegralService.AddMemberIntegral(info, memberIntegral);

                                MemberInfo inviteMember = MemberApplication.GetMember(member.InviteUserId);
                                if (inviteMember != null)
                                    _iMemberInviteService.AddInviteIntegel(member, inviteMember, true);
                            }
                        }
                    }
                }

                return new { success = true, data = result };
            }
            else
            {
                return new { success = false, msg = "未登录" };
            }
        }

    }
}
