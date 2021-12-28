﻿using Himall.API.Model;
using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using Himall.DTO;
using System.IO;
using System.Web.Http;

namespace Himall.API
{
    public class UserCenterController : BaseApiController
    {
        private static string _encryptKey = Guid.NewGuid().ToString("N");

        public object GetUser(string UserKey)
        {
            CheckUserLogin();
            dynamic data = SuccessResult();
            long userId = CurrentUser.Id;

            var member = MemberApplication.GetMember(userId);
            data.UserId = member.Id;
            data.UserName = member.UserName;
            data.Photo = string.IsNullOrEmpty(member.Photo) ? "" : HimallIO.GetRomoteImagePath(member.Photo);
            data.CellPhone = member.CellPhone;

            data.QQ = member.QQ;
            data.BirthDay = member.BirthDay;
            data.Nick = member.Nick;
            data.RealName = member.RealName;
            data.Sex = (int)member.Sex;


            var orderStatic = StatisticApplication.GetMemberOrderStatistic(userId,true);
            //用户所有订单
            data.AllOrders = orderStatic.OrderCount;
            //待付款订单
            data.WaitingForPay = orderStatic.WaitingForPay;
            //待发货订单
            data.WaitingForDelivery = orderStatic.WaitingForDelivery;//获取待发货订单数
            //未评论订单
            data.WaitingForComments = orderStatic.WaitingForComments;
            //待收货订单/待消费
            data.WaitingForRecieve = orderStatic.WaitingForRecieve + orderStatic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(userId);
            //待处理的售后
            data.RefundOrders = orderStatic.RefundCount;
            data.FavoriteShop = ShopApplication.GetUserConcernShopsCount(userId);
            data.FavoriteProduct = FavoriteApplication.GetFavoriteCountByUser(userId);
            data.Coupon = MemberApplication.GetAvailableCouponCount(userId);
            data.Integral = MemberIntegralApplication.GetAvailableIntegral(userId);
            data.Balance = MemberCapitalApplication.GetBalanceByUserId(userId);

            data.GroupTotal = FightGroupApplication.GetJoinAllGroupNumber(userId); //用户参与的团数量
            data.MyGroup = FightGroupApplication.GetJoinGroupNumber(userId);
            data.IsOpenRechargePresent = SiteSettingApplication.SiteSettings.IsOpenRechargePresent;
            data.GradeName = MemberGradeApplication.GetGradeByMember(member.Id).GradeName;
            MemberApplication.AddIntegel(member);//给用户加积分//执行登录后初始化相关操作
            return data;
        }

        public object GetUserCollectionProduct(int pageNo, int pageSize = 16)
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
                        Image = Core.HimallIO.GetRomoteProductSizeImage(pro.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_220),
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
        [HttpGet]
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

        public object GetUserCollectionShop(int pageNo, int pageSize = 8)
        {
            CheckUserLogin();
            if (CurrentUser != null)
            {
                var model = ServiceProvider.Instance<ShopService>.Create.GetUserConcernShops(CurrentUser.Id, pageNo, pageSize);

                var result = model.Models.Select(item => {
                    var shop = ShopApplication.GetShop(item.ShopId);
                    var vShop = VshopApplication.GetVShopByShopId(item.ShopId);
                    return new
                    {
                        //Id = item.Id,
                        Id = vShop?.Id ?? 0,
                        ShopId = item.ShopId,
                        Logo = vShop == null ? Core.HimallIO.GetRomoteImagePath(shop.Logo) : Core.HimallIO.GetRomoteImagePath(vShop.Logo),
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

      

        [HttpGet]
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

        protected override bool CheckContact(string contact, out string errorMessage)
        {
            CheckUserLogin();

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// 获取用户积分明细
        /// </summary>
        /// <param name="id">用户编号</param>
        /// <param name="type"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public object GetIntegralRecord(Himall.Entities.MemberIntegralInfo.IntegralType? type = null, int page = 1, int pagesize = 10)
        {
            CheckUserLogin();
            //处理当前用户与id的判断
            var _iMemberIntegralService = ObjectContainer.Current.Resolve<MemberIntegralService>();

            var query = new IntegralRecordQuery() { IntegralType = type, UserId = CurrentUser.Id, PageNo = page, PageSize = pagesize };
            var result = _iMemberIntegralService.GetIntegralRecordListForWeb(query);
            var list = result.Models.Select(item => {
                var actions = _iMemberIntegralService.GetIntegralRecordAction(item.Id);
                return new UserCenterGetIntegralRecordModel
                {
                    Id = item.Id,
                    RecordDate = item.RecordDate,
                    Integral = item.Integral,
                    TypeId = item.TypeId,
                    ShowType = (item.TypeId == MemberIntegralInfo.IntegralType.WeiActivity) ? item.ReMark : item.TypeId.ToDescription(),
                    ReMark = GetRemarkFromIntegralType(item.TypeId, actions, item.ReMark)
                };
            });
            dynamic pageresult = SuccessResult();
            pageresult.total = result.Total;
            pageresult.data = list.ToList();
            return pageresult;
        }



        private string GetRemarkFromIntegralType(MemberIntegralInfo.IntegralType type, ICollection<MemberIntegralRecordActionInfo> recordAction, string remark = "")
        {
            if (recordAction == null || recordAction.Count == 0)
                return remark;
            switch (type)
            {
                case Himall.Entities.MemberIntegralInfo.IntegralType.Consumption:
                    var orderIds = "";
                    foreach (var item in recordAction)
                    {
                        orderIds += item.VirtualItemId + ",";
                    }
                    remark = "使用订单号(" + orderIds.TrimEnd(',') + ")";
                    break;
              
                default:
                    return remark;
            }
            return remark;
        }

        protected override object OnCheckCheckCodeSuccess(string contact)
        {
            CheckUserLogin();

            string pluginId = PluginsManagement.GetInstalledPluginInfos(Core.Plugins.PluginType.SMS).First().PluginId;

            var _iMemberIntegralConversionFactoryService = ServiceProvider.Instance<MemberIntegralConversionFactoryService>.Create;
            var _iMemberIntegralService = ServiceProvider.Instance<MemberIntegralService>.Create;
            var _iMemberInviteService = ServiceProvider.Instance<MemberInviteService>.Create;

            var member = CurrentUser;
            if (Application.MessageApplication.GetMemberContactsInfo(pluginId, contact, Entities.MemberContactInfo.UserTypes.General) != null)
            {
                return new { success = false, msg = contact + "已经绑定过了！" };
            }
            member.CellPhone = contact;
            MemberApplication.UpdateMember(member.Map<DTO.Members>());
            Application.MessageApplication.UpdateMemberContacts(new Entities.MemberContactInfo()
            {
                Contact = contact,
                ServiceProvider = pluginId,
                UserId = CurrentUser.Id,
                UserType = Entities.MemberContactInfo.UserTypes.General
            });
            MessageApplication.RemoveMessageCacheCode(contact,pluginId);

            Entities.MemberInfo inviteMember = MemberApplication.GetMember(member.InviteUserId);


            var info = new MemberIntegralRecordInfo();
            info.UserName = member.UserName;
            info.MemberId = member.Id;
            info.RecordDate = DateTime.Now;
            info.TypeId = MemberIntegralInfo.IntegralType.Reg;
            info.ReMark = "绑定手机";
            var memberIntegral = _iMemberIntegralConversionFactoryService.Create(Himall.Entities.MemberIntegralInfo.IntegralType.Reg);
            _iMemberIntegralService.AddMemberIntegral(info, memberIntegral);
            if (inviteMember != null)
                _iMemberInviteService.AddInviteIntegel(member, inviteMember, true);

            return base.OnCheckCheckCodeSuccess(contact);
        }

        protected override object ChangePasswordByOldPassword(string oldPassword, string password)
        {
            CheckUserLogin();

            var _MemberService = ServiceProvider.Instance<MemberService>.Create;

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(password))
            {
                return new { success = false, msg = "密码不能为空！" };
            }
            var model = CurrentUser;
            var pwd = SecureHelper.MD5(SecureHelper.MD5(oldPassword) + model.PasswordSalt);
            bool CanChange = false;
            if (pwd == model.Password)
            {
                CanChange = true;
            }
            if (model.PasswordSalt.StartsWith("o"))
            {
                CanChange = true;
            }
            if (CanChange)
            {
                Application.MemberApplication.ChangePassword(model.Id, password);
                return SuccessResult("密码修改成功");
            }
            else
                return ErrorResult("旧密码错误");
        }

        protected override object ChangePayPwdByOldPassword(string oldPassword, string password)
        {
            CheckUserLogin();

            var _MemberCapitalService = ServiceProvider.Instance<MemberCapitalService>.Create;

            var hasPayPwd = MemberApplication.HasPayPassword(CurrentUser.Id);

            if (hasPayPwd && string.IsNullOrEmpty(oldPassword))
                return Json(new { success = false, msg = "请输入旧支付密码" });

            if (string.IsNullOrWhiteSpace(password))
                return Json(new { success = false, msg = "请输入新支付密码" });

            if (hasPayPwd)
            {
                var success = MemberApplication.VerificationPayPwd(CurrentUser.Id, oldPassword);
                if (!success)
                    return Json(new { success = false, msg = "旧支付密码错误" });
            }

            _MemberCapitalService.SetPayPwd(CurrentUser.Id, password);

            return Json(new { success = true, msg = "设置成功" });
        }

        public object GetUserContact()
        {
            CheckUserLogin();
            string cellPhone = CurrentUser.CellPhone;
            string email = CurrentUser.Email;
            if (string.IsNullOrEmpty(cellPhone) && string.IsNullOrEmpty(email))
                return Json(new { success = false, msg = "没有绑定手机或邮箱" });

            string Contact = string.IsNullOrEmpty(CurrentUser.CellPhone) ? CurrentUser.Email : CurrentUser.CellPhone;

            return Json(new { success = true, msg = "", contact = Contact });
        }

        /// <summary>
        /// 保存个人信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public object SaveUserInfo(MemberUpdate model)
        {
            CheckUserLogin();
            if (string.IsNullOrWhiteSpace(model.RealName))
            {
                return ErrorResult("真实姓名必须填写");
            }
            if (!string.IsNullOrWhiteSpace(model.Photo))
            {
                model.Photo = UploadPhoto(model.Photo);
            }
            model.Id = CurrentUser.Id;
            MemberApplication.UpdateMemberInfo(model);
            return Json(new { success = true, msg = "修改成功" });
        }

        private string UploadPhoto(string strPhoto)
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

        /// <summary>
        /// 是否强制绑定手机号
        /// </summary>
        /// <returns></returns>
        public object IsConBindSms()
        {
            CheckUserLogin();
            return Json(new { success = MessageApplication.IsOpenBindSms(CurrentUser.Id) });
        }
    }
}
