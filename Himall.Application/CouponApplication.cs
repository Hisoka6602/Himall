using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Application
{
    /// <summary>
    /// 优惠券业务实现
    /// </summary>
    public class CouponApplication:BaseApplicaion<CouponService>
    {
        /// <summary>
        /// 优惠券设置
        /// </summary>
        /// <param name="mCouponRegister"></param>
        public static void SetCouponSendByRegister(CouponSendByRegisterModel model)
        {
            var detail = new List<CouponSendByRegisterDetailedInfo>();
            foreach (var item in model.CouponIds)
            {
                detail.Add(new CouponSendByRegisterDetailedInfo() { CouponId = item.Id });
            }
            var info = new CouponSendByRegisterInfo
            {
                Id = model.Id,
                Link = model.Link,
                Status = model.Status,
            };
            var service = GetService<CouponSendByRegisterService>();
            if (model.Id <= 0)
                service.AddCouponSendByRegister(info, detail);
            else
                service.UpdateCouponSendByRegister(info, detail);
        }

        /// <summary>
        /// 获取优惠券设置
        /// </summary>
        /// <returns></returns>
        public static CouponSendByRegisterModel GetCouponSendByRegister()
        {
            var vModel = new CouponSendByRegisterModel();
            var model = GetService<CouponSendByRegisterService>().GetCouponSendByRegister();
            if (model != null)
            {
                vModel.Id = model.Id;
                vModel.Link = model.Link;
                vModel.Status = model.Status;

                int total = 0;
                decimal price = 0;
                var lmCoupon = new List<CouponModel>();
                var details = Service.GetCouponSendByRegisterDetailedInfo(model.Id);
                var coupons = Service.GetCoupons(details.Select(p => p.CouponId).ToList());
                var records = Service.GetRecordCountByCoupon(coupons.Select(p => p.Id).ToList());
                foreach (var item in details)
                {
                    var coupon = coupons.FirstOrDefault(p => p.Id == item.CouponId);
                    var record = records.ContainsKey(coupon.Id) ? records[coupon.Id] : 0;
                    int inventory = coupon.Num - record;//优惠券剩余量
                    if (inventory > 0 && coupon.EndTime > DateTime.Now)
                    {
                        total += inventory;
                        price += coupon.Price;
                        lmCoupon.Add(new CouponModel
                        {
                            Id = item.CouponId,
                            CouponName = coupon.CouponName,
                            inventory = inventory,
                            Num = coupon.Num,
                            useNum = record,
                            Price = coupon.Price,
                            ShopId = coupon.ShopId,
                            ShopName = coupon.ShopName,
                            EndTime = coupon.EndTime,
                            StartTime = coupon.StartTime,
                            OrderAmount = coupon.OrderAmount == 0 ? "不限制" : "满" + coupon.OrderAmount
                        });
                    }
                }
                vModel.CouponIds = lmCoupon;
                vModel.total = total;
                vModel.price = price;
                if (vModel.CouponIds.Count.Equals(0))
                    vModel.Status = CouponSendByRegisterStatus.Shut;
            }
            return vModel;
        }

        public static List<CouponData> GetAvailable(long shopId) =>
            Service.GetAvailable(shopId);

       

        /// <summary>
        /// 注册成功赠送优惠券
        /// </summary>
        /// <param name="id">会员ID</param>
        /// <param name="userName">会员登录名</param>
        /// <returns>返回赠送张数</returns>
        public static int RegisterSendCoupon(long id, string userName,out List<CouponModel> couponlist)
        {
            List<CouponModel> couponresult = new List<CouponModel>();
            int result = 0;
            var model = GetCouponSendByRegister();
            if (model != null && model.Status.Equals(Himall.CommonModel.CouponSendByRegisterStatus.Open) && model.total > 0)//如果活动开启，且优惠券有剩余
            {
                foreach (var item in model.CouponIds)
                {
                    if (item.inventory > 0)
                    {

                        CouponRecordInfo info = new CouponRecordInfo();
                        info.UserId = id;
                        info.UserName = userName;
                        info.ShopId = item.ShopId;
                        info.CouponId = item.Id;
                        Service.AddCouponRecord(info);
                        couponresult.Add(item);
                        result++;
                    }
                }
            }
            couponlist = couponresult;
            return result;
        }
        /// <summary>
        /// 发送优惠券
        /// </summary>
        /// <param name="labelids">发送标签</param>
        /// <param name="labelinfos">标签名称</param>
        /// <param name="couponIds">优惠券名称</param>
        /// <returns>-1:优惠券不足;-2:请选择发送的优惠券;-3:标签中用户数为0</returns>
        public static string SendCouponMsg(string labelids, string labelinfos, string couponIds, string url)
        {

            var messageEmali = PluginsManagement.GetPlugin<IMessagePlugin>("Himall.Plugin.Message.Email");
            var messageSMS = PluginsManagement.GetPlugin<IMessagePlugin>("Himall.Plugin.Message.SMS");
            string result = "";
            if (!couponIds.TrimEnd(',').Equals(""))
            {
                //取出标签对应的会员信息
                long[] lids = string.IsNullOrWhiteSpace(labelids) ? null : labelids.Split(',').Select(s => long.Parse(s)).ToArray();
                int pageNo = 1, pageSize = 100;
                var pageMode = MemberApplication.GetMembers(new MemberQuery
                {
                    Labels = lids,
                    PageNo = pageNo,
                    PageSize = pageSize
                });
                if (pageMode.Total > 0)
                {
                    List<Himall.Entities.MemberInfo> mUserMember = new List<Himall.Entities.MemberInfo>();
                    while (pageMode.Models.Count() > 0)//循环批量获取用户信息
                    {
                        string[] dests = pageMode.Models.Select(e => e.Email).ToArray();
                        foreach (var item in pageMode.Models)
                        {
                            mUserMember.Add(item);
                        }
                        pageNo += 1;
                        pageMode = MemberApplication.GetMembers(new MemberQuery
                        {
                            Labels = lids,
                            PageNo = pageNo,
                            PageSize = pageSize
                        });
                    }

                    string[] arrStr = couponIds.TrimEnd(',').Split(',');
                    long[] arrcouponIds = arrStr.Select(a => long.Parse(a)).ToArray();

                    var model = Service.GetCouponInfo(arrcouponIds);//获取所选优惠券集合

                    //查询优惠券领取状况
                    var mCouponRecord = Service.GetCouponRecordTotal(arrcouponIds);

                    decimal price = 0;
                    List<SendmessagerecordCouponInfo> lsendInfo = new List<SendmessagerecordCouponInfo>();
                    List<SendmessagerecordCouponSNInfo> lsendSN = new List<SendmessagerecordCouponSNInfo>();
                    var records = Service.GetRecordCountByCoupon(model.Select(p => p.Id).ToList());
                    //验证优惠券是否充足
                    foreach (var item in model)
                    {
                        price += item.Price;
                        lsendInfo.Add(new SendmessagerecordCouponInfo() { CouponId = item.Id });
                        var record = records.ContainsKey(item.Id) ? records[item.Id] : 0;
                        if (item.Num - record < mUserMember.Count)
                        {
                            result = item.CouponName + "优惠券的数量不足，无法赠送";
                            break;
                        }
                    }
                    var siteName = SiteSettingApplication.SiteSettings.SiteName;
                    if (result == "")
                    {
                        //发送优惠券
                        bool alTotal = false;
                        for (int i = 0; i < mUserMember.Count; i++)
                        {
                            bool suTotal = false;//会员发送优惠券成功数
                            foreach (var item in model)
                            {
                                //判断会员领取限制，是否可领取此优惠券
                                bool isf = true;
                                if (item.PerMax > 0)
                                {
                                    int total = mCouponRecord.Where(p => p.UserId == mUserMember[i].Id && p.CouponId == item.Id).ToList().Count;
                                    if (item.PerMax <= total)
                                    {
                                        isf = false;
                                    }
                                }

                                if (isf)
                                {
                                    suTotal = true;
                                    alTotal = true;

                                    CouponRecordInfo info = new CouponRecordInfo();
                                    info.UserId = mUserMember[i].Id;
                                    info.UserName = mUserMember[i].UserName;
                                    info.ShopId = item.ShopId;
                                    info.CouponId = item.Id;
                                    var couponRecord = Service.AddCouponRecord(info);
                                    lsendSN.Add(new SendmessagerecordCouponSNInfo() { CouponSN = couponRecord.CounponSN });
                                }
                            }

                            if (suTotal)
                            {
                                MessageCouponInfo info = new MessageCouponInfo();
                                info.Money = price;
                                info.SiteName = siteName;
                                info.UserName = mUserMember[i].UserName;
                                MessageApplication.SendMessageOnCouponSuccess(mUserMember[i].Id, info);
                            }
                        }

                        Log.Debug("sendCoupon:" + alTotal);
                        //查看成功发送会员数
                        if (alTotal)
                        {
                            //记录发送历史
                            var sendRecord = new SendMessageRecordInfo
                            {
                                ContentType = WXMsgType.wxcard,
                                MessageType = MsgType.Coupon,
                                SendContent = "",
                                SendState = 1,
                                SendTime = DateTime.Now,
                                ToUserLabel = labelinfos ?? "",
                            };
                            WXMsgTemplateApplication.AddSendRecord(sendRecord, lsendInfo);
                            foreach (var item in lsendSN)
                            {
                                item.MessageId = sendRecord.Id;
                            }
                            Service.AddSendmessagerecordCouponSN(lsendSN);
                        }
                        else
                        {
                            result = "此标签下无符合领取此优惠券的会员";
                        }
                    }
                }
                else
                    result = "该标签下无任何会员";
            }
            else
                result = "请选择发送的优惠券";
            return result;
        }

        /// <summary>
        /// 发送优惠券，根据会员ID
        /// </summary>
        /// <param name="userIds">发送对象</param>
        /// <param name="couponIds">优惠券名称</param>
        public static void SendCouponByUserIds(List<long> userIds, IEnumerable<long> couponIds)
        {
            var model = Service.GetCouponInfo(couponIds.ToArray());
            var siteName = SiteSettingApplication.SiteSettings.SiteName;
            var mCouponRecord = Service.GetCouponRecordTotal(couponIds.ToArray());
            var mUserMember = MemberApplication.GetMembers(userIds);
            decimal price = 0;
            string result = "";
            List<SendmessagerecordCouponInfo> lsendInfo = new List<SendmessagerecordCouponInfo>();
            var records = Service.GetRecordCountByCoupon(model.Select(p => p.Id).ToList());
            //验证优惠券是否充足
            foreach (var item in model)
            {
                price += item.Price;
                lsendInfo.Add(new SendmessagerecordCouponInfo() { CouponId = item.Id });
                var record = records.ContainsKey(item.Id) ? records[item.Id] : 0;
                if (item.Num - record < mUserMember.Count)
                {
                    result = item.CouponName + "优惠券的数量不足，无法赠送";
                    break;
                }
            }
            if (result == "")
            {
                //发送优惠券
                bool alTotal = false;
                for (int i = 0; i < mUserMember.Count; i++)
                {
                    bool suTotal = false;//会员发送优惠券成功数
                    foreach (var item in model)
                    {
                        //判断会员领取限制，是否可领取此优惠券
                        bool isf = true;
                        if (item.PerMax > 0)
                        {
                            int total = mCouponRecord.Where(p => p.UserId == mUserMember[i].Id && p.CouponId == item.Id).ToList().Count;
                            if (item.PerMax <= total)
                            {
                                isf = false;
                            }
                        }

                        if (isf)
                        {
                            suTotal = true;
                            alTotal = true;

                            CouponRecordInfo info = new CouponRecordInfo();
                            info.UserId = mUserMember[i].Id;
                            info.UserName = mUserMember[i].UserName;
                            info.ShopId = item.ShopId;
                            info.CouponId = item.Id;
                            Service.AddCouponRecord(info);
                        }
                    }

                    if (suTotal)
                    {
                        MessageCouponInfo info = new MessageCouponInfo();
                        info.Money = price;
                        info.SiteName = siteName;
                        info.UserName = mUserMember[i].UserName;
                        MessageApplication.SendMessageOnCouponSuccess(mUserMember[i].Id, info);
                    }
                }

                Log.Debug("sendCoupon:" + alTotal);
                //查看成功发送会员数
                if (alTotal)
                {
                    //记录发送历史
                    var sendRecord = new SendMessageRecordInfo
                    {
                        ContentType = WXMsgType.wxcard,
                        MessageType = MsgType.Coupon,
                        SendContent = "",
                        SendState = 1,
                        SendTime = DateTime.Now,
                        ToUserLabel = "",
                    };
                    WXMsgTemplateApplication.AddSendRecord(sendRecord, lsendInfo);
                }
                else
                {
                    result = "无符合领取此优惠券的会员";
                }
            }
           
            if (!string.IsNullOrWhiteSpace(result))
            {
                throw new HimallException(result);
            }
        }

        /// <summary>
        /// 发送优惠券，根据搜索条件
        /// </summary>
        /// <param name="query"></param>
        /// <param name="couponIds"></param>
        public static void SendCoupon(MemberPowerQuery query, IEnumerable<long> couponIds, MemberQuery memberQuery = null, string labelinfos = "")
        {
            var siteName = SiteSettingApplication.SiteSettings.SiteName;
            decimal price = 0;
            string result = "";
            //会员领取优惠券记录ID
            //   List<long> memberCouponIds = new List<long>();
            // dictResult = new Dictionary<string, int>();  

            var isMember = memberQuery != null;//是否为会员管理发送
            var mUserMember = new List<MemberPurchasingPower>();
            var mUserMemberList = new List<Members>();
            if (isMember)
            {
                memberQuery.PageSize = 500;
                memberQuery.PageNo = 1;
                var pageMode = MemberApplication.GetMemberList(memberQuery);
                if (pageMode.Total > 0)
                {
                    while (pageMode.Models.Count() > 0)//循环批量获取用户信息
                    {
                        //   string[] dests = pageMode.Models.Select(e => e.).ToArray();
                        foreach (var item in pageMode.Models)
                        {
                            mUserMemberList.Add(item);
                        }
                        memberQuery.PageNo += 1;
                        pageMode = MemberApplication.GetMemberList(memberQuery);
                    }
                }
            }
            else
            {
                query.PageSize = 500;
                query.PageNo = 1;
                var pageMode = MemberApplication.GetPurchasingPowerMember(query);
                if (pageMode.Total > 0)
                {
                    while (pageMode.Models.Count() > 0)//循环批量获取用户信息
                    {
                        //   string[] dests = pageMode.Models.Select(e => e.).ToArray();
                        foreach (var item in pageMode.Models)
                        {
                            mUserMember.Add(item);
                        }
                        query.PageNo += 1;
                        pageMode = MemberApplication.GetPurchasingPowerMember(query);
                    }
                }
            }

            var isTrue = isMember ? mUserMemberList.Any() : mUserMember.Any();
            if (isTrue)
            {
                var model = Service.GetCouponInfo(couponIds.ToArray());//获取所选优惠券集合

                //查询优惠券领取状况
                var mCouponRecord = Service.GetCouponRecordTotal(couponIds.ToArray());

                List<SendmessagerecordCouponInfo> lsendInfo = new List<SendmessagerecordCouponInfo>();
                var records = Service.GetRecordCountByCoupon(model.Select(p => p.Id).ToList());
                //验证优惠券是否充足
                foreach (var item in model)
                {
                    price += item.Price;
                    lsendInfo.Add(new SendmessagerecordCouponInfo() { CouponId = item.Id });
                    var record = records.ContainsKey(item.Id) ? records[item.Id] : 0;
                    var count = isMember ? mUserMemberList.Count : mUserMember.Count;
                    if (item.Num - record < count)
                    {
                        result = item.CouponName + "优惠券的数量不足，无法赠送";
                        break;
                    }
                }
                if (result == "")
                {
                    //发送优惠券
                    bool alTotal = false;
                    var count = isMember ? mUserMemberList.Count : mUserMember.Count;
                    for (int i = 0; i < count; i++)
                    {
                        bool suTotal = false;//会员发送优惠券成功数
                        foreach (var item in model)
                        {
                            //判断会员领取限制，是否可领取此优惠券
                            bool isf = true;
                            if (item.PerMax > 0)
                            {
                                int total = mCouponRecord.Where(p => p.UserId == (isMember ? mUserMemberList[i].Id : mUserMember[i].Id) && p.CouponId == item.Id).ToList().Count;
                                if (item.PerMax <= total)
                                {
                                    isf = false;
                                }
                            }

                            if (isf)
                            {
                                suTotal = true;
                                alTotal = true;

                                CouponRecordInfo info = new CouponRecordInfo();
                                info.UserId = isMember ? mUserMemberList[i].Id : mUserMember[i].Id;
                                info.UserName = isMember ? mUserMemberList[i].UserName : mUserMember[i].UserName;
                                info.ShopId = item.ShopId;
                                info.CouponId = item.Id;
                                Service.AddCouponRecord(info);
                            }
                        }

                        if (suTotal)
                        {
                            MessageCouponInfo info = new MessageCouponInfo();
                            info.Money = price;
                            info.SiteName = siteName;
                            info.UserName = isMember ? mUserMemberList[i].UserName : mUserMember[i].UserName;
                            MessageApplication.SendMessageOnCouponSuccess((isMember ? mUserMemberList[i].Id : mUserMember[i].Id), info);
                        }
                    }

                    Log.Debug("sendCoupon:" + alTotal);
                    //查看成功发送会员数
                    if (alTotal)
                    {
                        //记录发送历史
                        var sendRecord = new SendMessageRecordInfo
                        {
                            ContentType = WXMsgType.wxcard,
                            MessageType = MsgType.Coupon,
                            SendContent = "",
                            SendState = 1,
                            SendTime = DateTime.Now,
                            ToUserLabel = labelinfos ?? "",
                        };
                        WXMsgTemplateApplication.AddSendRecord(sendRecord, lsendInfo);
                    }
                    else
                    {
                        result = "此标签下无符合领取此优惠券的会员";
                    }
                }
            }
            else
                result = "该标签下无任何会员";            

            if (!string.IsNullOrWhiteSpace(result))
            {
                throw new HimallException(result);
            }
        }

        public static QueryPageModel<CouponModel> GetCouponByName(string text, DateTime endDate, int ReceiveType, int page, int 
pageSize,List<long> couponsId=null)
        {
            var couponList = Service.GetCouponByName(text, endDate, ReceiveType, page, pageSize, couponsId);
            var pageModel = new QueryPageModel<CouponModel>();

            var lmCoupon = new List<CouponModel>();
            var records = Service.GetRecordCountByCoupon(couponList.Models.Select(p => p.Id).ToList());
            foreach (var item in couponList.Models)
            {
                if (item.IsSyncWeiXin == 0 || (item.IsSyncWeiXin == 1 && item.WXAuditStatus == (int)WXCardLogInfo.AuditStatusEnum.Audited))
                {
                    var record = records.ContainsKey(item.Id) ? records[item.Id] : 0;
                    CouponModel couponModel = new CouponModel();
                    couponModel.CouponName = item.CouponName;
                    couponModel.Id = item.Id;
                    couponModel.Num = item.Num;
                    couponModel.useNum = record;
                    couponModel.inventory = item.Num - record;
                    couponModel.OrderAmount = item.OrderAmount == 0 ? "不限制" : "满" + item.OrderAmount;
                    couponModel.Price = item.Price;
                    couponModel.ShopName = item.ShopName;
                    couponModel.EndTime = item.EndTime;
                    couponModel.StartTime = item.StartTime;
                    couponModel.perMax = item.PerMax;
                    couponModel.UseArea = item.UseArea;
                    couponModel.Remark = item.Remark;
                    lmCoupon.Add(couponModel);
                }
            }
            pageModel.Models = lmCoupon;
            pageModel.Total = couponList.Total;
            return pageModel;
        }
        public static CouponModel Get(long id)
        {
            var couponList = Service.GetCouponInfo(id);
            if (couponList == null)
                return null;
            var record = Service.GetRecordCountByCoupon(couponList.Id);
            var lmCoupon = new Himall.DTO.CouponModel();
            lmCoupon.CouponName = couponList.CouponName;
            lmCoupon.Id = couponList.Id;
            lmCoupon.Num = couponList.Num;
            lmCoupon.useNum = record;
            lmCoupon.inventory = couponList.Num - record;
            lmCoupon.OrderAmount = couponList.OrderAmount == 0 ? "不限制" : "满" + couponList.OrderAmount;
            lmCoupon.Price = couponList.Id;
            lmCoupon.ShopName = couponList.ShopName;
            lmCoupon.EndTime = couponList.EndTime;
            lmCoupon.StartTime = couponList.StartTime;
            lmCoupon.perMax = couponList.PerMax;
            lmCoupon.UseArea = couponList.UseArea;
            lmCoupon.Remark = couponList.Remark;
            return lmCoupon;
        }

        /// <summary>
        /// 领取一个优惠券
        /// </summary>
        /// <param name="info"></param>
        public static void AddCouponRecord(CouponRecordInfo info)
        {
            Service.AddCouponRecord(info);
        }

     
        /// <summary>
        /// 获取商家添加的优惠券列表
        /// </summary>
        /// <returns></returns>
        public static QueryPageModel<CouponInfo> GetCouponList(CouponQuery query)
        {
            return Service.GetCouponList(query);
        }


        /// <summary>
        /// 获取商家添加的优惠券列表(排除失效)
        /// </summary>
        /// <param name="shopid"></param>
        /// <returns></returns>
        public static List<CouponInfo> GetCouponLists(long shopid)
        {
            return Service.GetCouponLists(shopid);
        }

        /// <summary>
        /// 获取领取的优惠券列表
        /// </summary>
        /// <returns></returns>
        public static QueryPageModel<CouponRecordInfo> GetCouponRecordList(CouponRecordQuery query)
        {
            return Service.GetCouponRecordList(query);
        }

        /// <summary>
        /// 获取优惠券信息（couponid）
        /// </summary>
        /// <param name="couponId"></param>
        /// <returns></returns>
        public static CouponInfo GetCouponInfo(long couponId)
        {
            return Service.GetCouponInfo(couponId);
        }

        /// <summary>
        /// 批量获取优惠券信息（couponIds）
        /// </summary>
        /// <param name="couponIds">优惠券数组</param>
        /// <returns></returns>
        public static List<CouponInfo> GetCouponInfo(IEnumerable<long> couponIds)
        {
            return Service.GetCouponInfo(couponIds.ToArray());
        }

        /// <summary>
        /// 获取一个用户在某个店铺的可用优惠券
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="userId"></param>
        /// <param name="totalPrice">总金额</param>
        /// <returns></returns>
        public static List<CouponRecordInfo> GetUserCoupon(long shopId, long userId, decimal totalPrice)
        {
            return Service.GetUserCoupon(shopId, userId, totalPrice);
        }
        
        /// <summary>
        /// 取用户领取的所有优惠卷信息
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static IEnumerable<UserCouponInfo> GetUserCouponList(long userid)
        {
            return Service.GetUserCouponList(userid);
        }
        /// <summary>
        /// 获取用户未使用的所有优惠券
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public static List<UserCouponInfo> GetAllUserCoupon(long userid)
        {
            return Service.GetAllUserCoupon(userid);
        }
        /// <summary>
        /// 取积分优惠券列表
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public static QueryPageModel<CouponInfo> GetIntegralCoupons(int page, int pageSize)
        {
            return Service.GetIntegralCoupons(page, pageSize);
        }

        public static List<CouponProductInfo> GetCouponProductsByCouponId(long couponId)
        {
            return Service.GetCouponProductsByCouponId(couponId);
        }
        public static List<CouponShopInfo> GetCouponShopsByCouponId(long couponId)
        {
            return Service.GetCouponShopsByCouponId(couponId);
        }
        public static List<CouponShopInfo> GetCouponShopsByCouponIds(List<long> couponId)
        {
            return Service.GetCouponShopsByCouponIds(couponId);
        }

        /// <summary>
        /// 将前端传入参数转换成适合操作的格式
        /// </summary>
        public static IEnumerable<string[]> ConvertUsedCoupon(string couponIds)
        {
            //couponIds格式  "id_type,id_type,id_type"//部分更新为 id_type_shopId
            IEnumerable<string> couponArr = null;
            if (!string.IsNullOrEmpty(couponIds))
            {
                couponArr = couponIds.Split(',');
            }

            //返回格式  string[0] = id , string[1] = type //部分更新 string[2] = shopId
            return couponArr == null ? null : couponArr.Select(p => p.Split('_'));
        }

        /// <summary>
        /// 获取平台优惠券（包含指定商家券）
        /// </summary>
        /// <param name="shopId">指定商家ShopId</param>
        /// <returns></returns>
        public static List<CouponInfo> GetPaltCouponList(long shopId)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            //获取在移动端显示的平台优惠券Ids
            var couponSetList = ServiceProvider.Instance<VShopService>.Create.GetVShopCouponSetting(0).Where(n => n.PlatForm == PlatformType.Wap).Select(item => item.CouponID);
            //全平台
            var platAll = service.GetCouponList(0).Where(item => item.UseArea == 0 && couponSetList.Contains(item.Id)).ToList();
            //指定商家
            var platPart = service.GetPlatCouponList(shopId).Distinct(new CouponDuplicateDefine()).Where(item => couponSetList.Contains(item.Id)).ToList();
            return platAll.Concat(platPart).OrderByDescending(t => t.Price).ToList();
        }
        /// <summary>
        /// 获取店铺优惠券数量
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static int GetCouponCount(long shopId)
        {
            var service = ServiceProvider.Instance<CouponService>.Create;
            var result = service.GetCouponList(shopId);
            var platCoupon = GetPaltCouponList(shopId);
            var couponSetList = ServiceProvider.Instance<VShopService>.Create.GetVShopCouponSetting(shopId).Where(d => d.PlatForm == PlatformType.Wap).Select(item => item.CouponID);
            if (result.Count() > 0 && couponSetList.Count() > 0)
            {
                var couponList = result.ToArray().Where(item => couponSetList.Contains(item.Id));
                return couponList.Count() + platCoupon.Count();
            }
            else
            {
                return platCoupon.Count();
            }
        }
    }

    /// <summary>
    /// 优惠券比较器
    /// </summary>
    public class CouponDuplicateDefine : IEqualityComparer<CouponInfo>
    {
        public bool Equals(CouponInfo x, CouponInfo y)
        {
            return x.Id == y.Id;
        }

        public int GetHashCode(CouponInfo obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
    /// <summary>
    /// 优惠券比较器
    /// </summary>
    public class UserCouponDuplicateDefine : IEqualityComparer<UserCouponInfo>
    {
        public bool Equals(UserCouponInfo x, UserCouponInfo y)
        {
            return x.CouponId == y.CouponId;
        }

        public int GetHashCode(UserCouponInfo obj)
        {
            return obj.ToString().GetHashCode();
        }
    }

    /// <summary>
    /// 优惠券比较器
    /// </summary>
    public class BaseCouponDuplicateDefine : IEqualityComparer<BaseCoupon>
    {
        public bool Equals(BaseCoupon x, BaseCoupon y)
        {
            return x.BaseId == y.BaseId;
        }

        public int GetHashCode(BaseCoupon obj)
        {
            return obj.ToString().GetHashCode();
        }
    }
}