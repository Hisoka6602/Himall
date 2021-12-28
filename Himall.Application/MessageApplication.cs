using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Management;

namespace Himall.Application
{
    public class MessageApplication
    {
        private static MessageService _iMessageService = ObjectContainer.Current.Resolve<MessageService>();
        private static OrderService _orderService  = ObjectContainer.Current.Resolve<OrderService>();
        //更新信息=用户表
        public static void UpdateMemberContacts(Entities.MemberContactInfo info)
        {
            _iMessageService.UpdateMemberContacts(info);
        }
        /// <summary>
        /// 获取发送目标
        /// </summary>
        /// <param name="userId">用户ID</param>
        /// <param name="pluginId">插件ID</param>
        /// <param name="type">用户类型</param>
        /// <returns></returns>
        public static string GetDestination(long userId, string pluginId, Entities.MemberContactInfo.UserTypes type)
        {
            return _iMessageService.GetDestination(userId, pluginId, type);
        }

        /// <summary>
        /// 根据插件类型和ID和目标获取信息
        /// </summary>
        /// <param name="pluginId"></param>
        /// <param name="contact"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Entities.MemberContactInfo GetMemberContactsInfo(string pluginId, string contact, Entities.MemberContactInfo.UserTypes type)
        {
            return _iMessageService.GetMemberContactsInfo(pluginId, contact, type);
        }

        /// <summary>
        /// 根据用户ID获取目标信息
        /// </summary>
        /// <param name="UserId">用户ID</param>
        /// <returns></returns>
        public static List<Entities.MemberContactInfo> GetMemberContactsInfo(long UserId)
        {
            return _iMessageService.GetMemberContactsInfo(UserId);
        }

        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageCode(string destination, string pluginId,MessageUserInfo userInfo=null)
        {
            if (string.IsNullOrEmpty(pluginId))
                throw new HimallException("账号不是手机号或邮箱，不能发送!");

            string ip = SiteSettingApplication.getIPAddress();
            if (!pluginId.ToLower().Contains("email"))//短信才需要验证防盗刷验证
            {
                if (!PhoneIPCodeApplication.ValidSendCount(ip, SMSSourceType.Ip))//验证Ip次数
                {
                    throw new HimallException("您今天已经发送" + SiteSettingApplication.SiteSettings.IpSmsCount + "次!");
                }
                if (!PhoneIPCodeApplication.ValidSendCount(destination, SMSSourceType.Phone))//验证手机号次数
                {
                    throw new HimallException("您今天已经发送" + SiteSettingApplication.SiteSettings.PhoneSmsCount + "次!");
                }
            }

            if (isExsitMessageCache(destination, pluginId))
            {
                throw new HimallException("60秒内只允许请求一次，请稍后重试!");
            }
            SaveMessageCacheCode(destination, pluginId, userInfo);
            if (!pluginId.ToLower().Contains("email"))
            {
                PhoneIPCodeApplication.AddPhoneIPCode(ip, SMSSourceType.Ip);//记录同一天Ip发送短信的次数
                PhoneIPCodeApplication.AddPhoneIPCode(destination, SMSSourceType.Phone);//记录同一天手机号发送短信的次数
            }

        }


        public static void SendMessageCodeDirect(string destination, string username, string pluginId)
        {
            if (string.IsNullOrEmpty(pluginId))
                throw new HimallException("账号不是手机号或邮箱，不能发送!");
            string ip = SiteSettingApplication.getIPAddress();
            if (!pluginId.ToLower().Contains("email"))//短信才需要验证防盗刷验证
            {
                if (!PhoneIPCodeApplication.ValidSendCount(ip, SMSSourceType.Ip))//验证Ip次数
                {
                    throw new HimallException("您今天已经发送" + SiteSettingApplication.SiteSettings.IpSmsCount + "次!");
                }
                if (!PhoneIPCodeApplication.ValidSendCount(destination, SMSSourceType.Phone))//验证手机号次数
                {
                    throw new HimallException("您今天已经发送" + SiteSettingApplication.SiteSettings.PhoneSmsCount + "次!");
                }
            }


            if (isExsitMessageCache(destination, pluginId))
            {
                throw new HimallException("60秒内只允许请求一次，请稍后重试!");
            }
            var user = new MessageUserInfo() { UserName = username, SiteName = SiteSettingApplication.SiteSettings.SiteName };

            SaveMessageCacheCode(destination, pluginId, user);
            if (!pluginId.ToLower().Contains("email"))
            {
                PhoneIPCodeApplication.AddPhoneIPCode(ip, SMSSourceType.Ip);//记录同一天Ip发送短信的次数
                PhoneIPCodeApplication.AddPhoneIPCode(destination, SMSSourceType.Phone);//记录同一天手机号发送短信的次数
            }
        }



        /// <summary>
        /// 发送验证码
        /// </summary>
        /// <param name="destination">手机或邮箱</param>
        public static void SendMessageCodeDirect(string destination)
        {
            var pluginId = "";
            if (Core.Helper.ValidateHelper.IsMobile(destination))
            {
                pluginId = "Himall.Plugin.Message.SMS";
            }
            if (!string.IsNullOrEmpty(destination) && Core.Helper.ValidateHelper.IsEmail(destination))
            {
                pluginId = "Himall.Plugin.Message.Email";
            }
            if(string.IsNullOrEmpty(pluginId))
                throw new HimallException("账号不是手机号或邮箱，不能发送!");


            string ip = SiteSettingApplication.getIPAddress();
            if (!pluginId.ToLower().Contains("email"))//短信才需要验证防盗刷验证
            {
                if (!PhoneIPCodeApplication.ValidSendCount(ip, SMSSourceType.Ip))//验证Ip次数
                {
                    throw new HimallException("您今天已经发送" + SiteSettingApplication.SiteSettings.IpSmsCount + "次,请明天再来!");
                }
                if (!PhoneIPCodeApplication.ValidSendCount(destination, SMSSourceType.Phone))//验证手机号次数
                {
                    throw new HimallException("您今天已经发送" + SiteSettingApplication.SiteSettings.PhoneSmsCount + "次,请明天再来!");
                }
            }

            if (isExsitMessageCache(destination, pluginId)) {
                throw new HimallException("60秒内只允许请求一次，请稍后重试!");
            }
         

            SaveMessageCacheCode(destination,pluginId);

            if (!pluginId.ToLower().Contains("email"))
            {
                PhoneIPCodeApplication.AddPhoneIPCode(ip, SMSSourceType.Ip);//记录同一天Ip发送短信的次数
                PhoneIPCodeApplication.AddPhoneIPCode(destination, SMSSourceType.Phone);//记录同一天手机号发送短信的次数
            }
            
        }

        /// <summary>
        /// 是否存在验证码
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="pluginId"></param>
        /// <returns></returns>
        public static bool isExsitMessageCache(string destination, string pluginId)
        {
            bool tag = false;
            var timeout = CacheKeyCollection.MemberPluginCheckTime(destination, pluginId);
            if (Cache.Exists(timeout))
            {
                tag = true;
            }
            return tag;
        }
        public static void SendMsgWaitPay(OrderInfo order)
        {
            var message = new MessageOrderInfo();
            message.OrderId = order.Id.ToString();
            message.ShopId = order.ShopId;
            message.ShopName = order.ShopName;

            var userId = order.UserId;
            var siteName = SiteSettingApplication.SiteSettings.SiteName;
            message.SiteName = siteName;
            message.TotalMoney = order.TotalAmount;
            message.UserName = order.UserName;
            var item = _orderService.GetOrderItemsByOrderId(order.Id);
            message.ProductName = item[0].ProductName;
            message.Quantity = item.Sum(p => p.Quantity);
            message.OrderTime = order.OrderDate;

            if (order.Platform == PlatformType.WeiXinSmallProg)
            {
                message.MsgOrderType = MessageOrderType.Applet;
            }
            _iMessageService.SendMessageOnOrderCreate(userId, message);
        }

        public static void SaveMessageCacheCode(string destination,string pluginId,MessageUserInfo msguser = null) {
            var checkCode = new Random().Next(10000, 99999);
            var cacheTimeout = DateTime.Now.AddMinutes(15);
            if (pluginId.ToLower().Contains("email"))
            {
                cacheTimeout = DateTime.Now.AddMinutes(30);
            }
            Cache.Insert(CacheKeyCollection.MemberPluginCheck(destination, pluginId + destination), checkCode.ToString(), cacheTimeout);
            var user = new MessageUserInfo() { UserName = destination, SiteName = SiteSettingApplication.SiteSettings.SiteName, CheckCode = checkCode.ToString() };
            if (msguser != null)
            {
                if (string.IsNullOrEmpty(msguser.UserName))
                {
                    msguser.UserName = user.UserName;
                }
                if (string.IsNullOrEmpty(msguser.SiteName))
                {
                    msguser.SiteName = user.SiteName;
                }
                if (string.IsNullOrEmpty(msguser.CheckCode))
                {
                    msguser.CheckCode = user.CheckCode;
                }
            }
            else {
                msguser = user;
            }
            _iMessageService.SendMessageCode(destination, pluginId, msguser);
            Core.Cache.Insert(CacheKeyCollection.MemberPluginCheckTime(destination, pluginId), "0", DateTime.Now.AddSeconds(55));

        }

        public static void RemoveMessageCacheCode(string destination, string pluginId)
        {
            Cache.Remove(CacheKeyCollection.MemberPluginCheck(destination, pluginId+ destination));
        }

        public static string GetMessageCacheCode(string destination,string pluginId) {
           string keyname= CacheKeyCollection.MemberPluginCheck(destination, pluginId + destination); 
           return Cache.Get<string>(keyname);
        }

        



        /// <summary>
        /// 找回密码
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnFindPassWord(long userId, MessageUserInfo info)
        {
            _iMessageService.SendMessageOnFindPassWord(userId, info);
        }
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnOrderCreate(long userId, MessageOrderInfo info)
        {
            _iMessageService.SendMessageOnOrderCreate(userId, info);
        }

        /// <summary>
        /// 订单支付
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnOrderPay(long userId, MessageOrderInfo info)
        {
            _iMessageService.SendMessageOnOrderPay(userId, info);
        }
        /// <summary>
        /// 店铺有新订单
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnShopHasNewOrder(long shopId, MessageOrderInfo info)
        {
            _iMessageService.SendMessageOnShopHasNewOrder(shopId, info);
        }
        /// <summary>
        /// 订单退款
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnOrderRefund(long userId, MessageOrderInfo info, long refundid = 0)
        {
            _iMessageService.SendMessageOnOrderRefund(userId, info, refundid);
        }

        public static void SendMessageOnRefundApply(long userId,long orderId, long orderItemId,int refundMode, long refundid = 0) {
            var order = _orderService.GetOrder(orderId);
            var orderItem = _orderService.GetOrderItem(orderItemId);
            //新增微信短信邮件消息推送
            var message = new MessageOrderInfo();
            message.UserName = order.UserName;
            message.OrderId = order.Id.ToString();
            message.ShopId = order.ShopId;
            message.ShopName = order.ShopName;
            message.RefundMoney = order.OrderTotalAmount;

            var siteName = SiteSettingApplication.SiteSettings.SiteName;
            message.SiteName = siteName;
            message.TotalMoney = order.OrderTotalAmount;
            message.ProductName = orderItem.ProductName;
            message.RefundAuditTime = DateTime.Now;
            message.MsgOrderType = MessageOrderType.Normal;

            _iMessageService.SendMessageOnRefundApply(userId, message, refundMode, refundid);
        }
        /// <summary>
        /// 售后发货信息提醒
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnRefundDeliver(long userId, MessageOrderInfo info, long refundid = 0)
        {
            _iMessageService.SendMessageOnRefundDeliver(userId, info, refundid);
        }

        /// <summary>
        /// 订单发货
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnOrderShipping(long userId, MessageOrderInfo info)
        {
            _iMessageService.SendMessageOnOrderShipping(userId, info);
        }
        /// <summary>
        /// 店铺审核
        /// </summary>
        /// <param name="destination"></param>
        /// <param name="info"></param>
        public static void SendMessageOnShopAudited(long userId, MessageShopInfo info)
        {
            _iMessageService.SendMessageOnShopAudited(userId, info);
        }

        /// <summary>
        /// 发送优惠券成功
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="info"></param>
        public static void SendMessageOnCouponSuccess(long userId, MessageCouponInfo info)
        {
            _iMessageService.SendMessageOnCouponSuccess(userId, info);
        }
        /// <summary>
        /// 会员提现失败
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="info"></param>
        public static void SendMessageOnMemberWithDrawFail(long userId, MessageWithDrawInfo info)
        {
            _iMessageService.SendMessageOnMemberWithDrawFail(userId, info);
        }

        #region 分销提现
        /// <summary>
        /// 分销会员提现申请
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="info"></param>
        public static void SendMessageOnDistributionMemberWithDrawApply(long userId, MessageWithDrawInfo info)
        {
            _iMessageService.SendMessageOnDistributionMemberWithDrawApply(userId, info);
        }

        /// <summary>
        /// 分销会员提现成功
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="info"></param>
        public static void SendMessageOnDistributionMemberWithDrawSuccess(long userId, MessageWithDrawInfo info)
        {
            _iMessageService.SendMessageOnDistributionMemberWithDrawSuccess(userId, info);
        }
        #endregion
        #region 分销
        /// <summary>
        /// 分销：申请成为销售员
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        public static void SendMessageOnDistributorApply(long userId, string userName)
        {
            _iMessageService.SendMessageOnDistributorApply(userId, userName, SiteSettingApplication.SiteSettings.SiteName);
        }
        /// <summary>
        /// 分销：申请审核通过
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        public static void SendMessageOnDistributorAuditSuccess(long userId, string userName)
        {
            _iMessageService.SendMessageOnDistributorAuditSuccess(userId, userName, SiteSettingApplication.SiteSettings.SiteName);
        }
        /// <summary>
        /// 分销：申请审核拒绝
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="userName"></param>
        /// <param name="remark"></param>
        /// <param name="applyTime"></param>
        public static void SendMessageOnDistributorAuditFail(long userId, string userName, string remark, DateTime applyTime)
        {
            _iMessageService.SendMessageOnDistributorAuditFail(userId, userName, remark, applyTime, SiteSettingApplication.SiteSettings.SiteName);
        }
        /// <summary>
        /// 分销：会员发展成功
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="subUserName"></param>
        /// <param name="subUserRegTime"></param>
        public static void SendMessageOnDistributorNewJoin(long userId, string subUserName, DateTime subUserRegTime)
        {
            _iMessageService.SendMessageOnDistributorNewJoin(userId, subUserName, subUserRegTime, SiteSettingApplication.SiteSettings.SiteName);
        }
        /// <summary>
        /// 分销：有已结算佣金时
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="amount"></param>
        /// <param name="settlementDate"></param>
        public static void SendMessageOnDistributorCommissionSettled(long userId, decimal amount, DateTime settlementDate)
        {
            _iMessageService.SendMessageOnDistributorCommissionSettled(userId, amount, settlementDate, SiteSettingApplication.SiteSettings.SiteName);
        }
        #endregion
        ///// <summary>
        ///// 店铺成功2.4去除
        ///// </summary>
        ///// <param name="destination"></param>
        ///// <param name="info"></param>
        //void SendMessageOnShopSuccess(long userId, MessageShopInfo info);
        /// <summary>
        /// 添加群发消息记录
        /// </summary>
        /// <param name="model"></param>
        public static void AddSendMessageRecord(dynamic model)
        {
            _iMessageService.AddSendMessageRecord(model);
        }
        /// <summary>
        /// 查询群发消息记录
        /// </summary>
        /// <param name="querymodel"></param>
        /// <returns></returns>
        public static QueryPageModel<object> GetSendMessageRecords(object querymodel)
        {
            return _iMessageService.GetSendMessageRecords(querymodel);
        }

        /// <summary>
        /// 是否强制绑定手机号
        /// </summary>
        /// <returns></returns>
        public static bool IsOpenBindSms(long userId)
        {
            var setting = SiteSettingApplication.SiteSettings;
            var IsBind = true;
            if (setting.IsConBindCellPhone)
            {
                IsBind = !string.IsNullOrEmpty(_iMessageService.GetDestination(userId, "Himall.Plugin.Message.SMS", Entities.MemberContactInfo.UserTypes.General));
            }

            return IsBind;
        }

        public static void SendMessageOnVirtualOrderVerificationSuccess(long userId, MessageVirtualOrderVerificationInfo info)
        {
            _iMessageService.SendMessageOnVirtualOrderVerificationSuccess(userId, info);
        }
    }
}
