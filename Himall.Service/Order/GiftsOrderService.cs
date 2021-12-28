using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Service
{
    public partial class GiftsOrderService : ServiceBase
    {
        /// <summary>
        /// 创建订单
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public GiftOrderInfo CreateOrder(GiftOrderModel model)
        {
            if (model.CurrentUser == null)
            {
                throw new HimallException("错误的用户信息");
            }
            if (model.ReceiveAddress == null)
            {
                throw new HimallException("错误的收货人信息");
            }
            GiftOrderInfo result = new GiftOrderInfo()
            {
                Id = GenerateOrderNumber(),
                UserId = model.CurrentUser.Id,
                RegionId = model.ReceiveAddress.RegionId,
                ShipTo = model.ReceiveAddress.ShipTo,
                Address = model.ReceiveAddress.Address + " " + model.ReceiveAddress.AddressDetail,
                RegionFullName = model.ReceiveAddress.RegionFullName,
                CellPhone = model.ReceiveAddress.Phone,
                TopRegionId = int.Parse(model.ReceiveAddress.RegionIdPath.Split(',')[0]),
                UserRemark = model.UserRemark,

            };
            var giftOrderItemInfo = new List<GiftOrderItemInfo>();
            DbFactory.Default
                 .InTransaction(() =>
                 {
                     //礼品信息处理，库存判断并减库存
                     foreach (var item in model.Gifts)
                     {
                         if (item.Counts < 1)
                         {
                             throw new HimallException("错误的兑换数量！");
                         }
                         GiftInfo giftdata = DbFactory.Default.Get<GiftInfo>().Where(d => d.Id == item.GiftId).FirstOrDefault();
                         if (giftdata != null && giftdata.GetSalesStatus == GiftInfo.GiftSalesStatus.Normal)
                         {
                             if (giftdata.StockQuantity >= item.Counts)
                             {
                                 giftdata.StockQuantity = giftdata.StockQuantity - item.Counts;   //先减库存
                                 giftdata.RealSales += item.Counts;    //加销量

                                 GiftOrderItemInfo gorditem = new GiftOrderItemInfo()
                                 {
                                     GiftId = giftdata.Id,
                                     GiftName = giftdata.GiftName,
                                     GiftValue = giftdata.GiftValue,
                                     ImagePath = giftdata.ImagePath,
                                     OrderId = result.Id,
                                     Quantity = item.Counts,
                                     SaleIntegral = giftdata.NeedIntegral
                                 };
                                 giftOrderItemInfo.Add(gorditem);
                                 DbFactory.Default.Update(giftdata);

                             }
                             else
                             {
                                 throw new HimallException("礼品库存不足！");
                             }
                         }
                         else
                         {
                             throw new HimallException("礼品不存在或已失效！");
                         }
                     }
                     //建立订单
                     result.TotalIntegral = giftOrderItemInfo.Sum(d => d.Quantity * d.SaleIntegral);
                     result.OrderStatus = GiftOrderInfo.GiftOrderStatus.WaitDelivery;
                     result.OrderDate = DateTime.Now;
                     DbFactory.Default.Add(result);
                     DbFactory.Default.AddRange(giftOrderItemInfo);
                     //减少积分
                     var userdata = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == model.CurrentUser.Id).FirstOrDefault();
                     DeductionIntegral(userdata, result.Id, (int)result.TotalIntegral);
                 });
            return result;
        }
        /// <summary>
        /// 积分抵扣
        /// </summary>
        /// <param name="member"></param>
        /// <param name="Id"></param>
        /// <param name="integral"></param>
        private void DeductionIntegral(MemberInfo member, long Id, int integral)
        {
            if (integral == 0)
            {
                return;
            }
            var record = new MemberIntegralRecordInfo();
            record.UserName = member.UserName;
            record.MemberId = member.Id;
            record.RecordDate = DateTime.Now;
            var remark = "礼品订单号:";
            record.TypeId = MemberIntegralInfo.IntegralType.Exchange;
            remark += Id.ToString();
            var action = new MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = MemberIntegralInfo.VirtualItemType.Exchange;
            action.VirtualItemId = Id;
            record.MemberIntegralRecordActionInfo = new List<MemberIntegralRecordActionInfo>();
            record.MemberIntegralRecordActionInfo.Add(action);
            record.ReMark = remark;
            var memberIntegral = ServiceProvider.Instance<MemberIntegralConversionFactoryService>.Create.Create(Himall.Entities.MemberIntegralInfo.IntegralType.Exchange, integral);
            ServiceProvider.Instance<MemberIntegralService>.Create.AddMemberIntegral(record, memberIntegral);
        }

        /// <summary>
        /// 获取订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public GiftOrderInfo GetOrder(long orderId)
        {
            GiftOrderInfo result = DbFactory.Default.Get<GiftOrderInfo>().Where(d => d.Id == orderId).FirstOrDefault();
            return result;
        }
        /// <summary>
        /// 获取订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public GiftOrderInfo GetOrder(long orderId, long userId)
        {
            GiftOrderInfo result = DbFactory.Default.Get<GiftOrderInfo>().Where(d => d.Id == orderId && d.UserId == userId).FirstOrDefault();
            return result;
        }
        /// <summary>
        /// 查询订单
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<GiftOrderInfo> GetOrders(GiftsOrderQuery query)
        {
            var db = WhereBuilder(query);
            //排序
            switch (query.Sort.ToLower())
            {
                case "firstgiftbuyquantity":
                    db.LeftJoin<GiftOrderItemInfo>((o, oi) => o.Id == oi.OrderId).Select().Select<GiftOrderItemInfo>(p => p.Quantity);
                    if (query.IsAsc) db.OrderBy(p => "Quantity");
                    else db.OrderByDescending(p => "Quantity");
                    break;
                case "orderdate":
                    if (query.IsAsc) db.OrderBy(p => p.OrderDate);
                    else db.OrderByDescending(p => p.OrderDate);
                    break;
                case "totalintegral":
                    if (query.IsAsc) db.OrderBy(p => p.TotalIntegral);
                    else db.OrderByDescending(p => p.TotalIntegral);
                    break;

                default:
                    db.OrderByDescending(d => d.OrderDate).OrderByDescending(d => d.Id);
                    break;
            }
            var result = db.ToPagedList(query.PageNo, query.PageSize);
            return new QueryPageModel<GiftOrderInfo>
            {
                Models = result,
                Total = result.TotalRecordCount
            };
        }

        public int GetOrderCount(GiftsOrderQuery query)
        {
            var db = WhereBuilder(query);
            return db.Count();
        }
        public GetBuilder<GiftOrderInfo> WhereBuilder(GiftsOrderQuery query)
        {
            var db = DbFactory.Default.Get<GiftOrderInfo>();
            if (!string.IsNullOrWhiteSpace(query.Skey))
            {
                long sOrderId;
                long.TryParse(query.Skey, out sOrderId);
                var sett = DbFactory.Default
                    .Get<GiftOrderItemInfo>()
                    .Where<GiftOrderInfo>((csi, ci) => csi.OrderId == ci.Id && csi.GiftName.Contains(query.Skey));

                db.Where(d => d.ExExists(sett) || d.ShipTo.Contains(query.Skey) || d.Id == sOrderId);
            }

            if (query.OrderId.HasValue)
                db.Where(d => d.Id == query.OrderId.Value);

            if (query.Status.HasValue)
                db.Where(d => d.OrderStatus == query.Status.Value);

            if (query.UserId.HasValue)
                db.Where(d => d.UserId == query.UserId.Value);
            return db;
        }

        /// <summary>
        /// 获取订单
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<GiftOrderInfo> GetOrders(IEnumerable<long> ids)
        {
            return DbFactory.Default.Get<GiftOrderInfo>()
                .Where(d => d.Id.ExIn(ids))
                .OrderByDescending(d => d.Id)
                .ToList();
        }
        /// <summary>
        /// 获取订单计数
        /// </summary>
        /// <param name="status"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public int GetOrderCount(GiftOrderInfo.GiftOrderStatus? status, long userId = 0)
        {
            var sql = DbFactory.Default.Get<GiftOrderInfo>();
            if (status.HasValue)
            {
                sql.Where(d => d.OrderStatus == status.Value);
            }
            if (userId != 0)
            {
                sql.Where(d => d.UserId == userId);
            }
            return sql.Count();
        }
        /// <summary>
        /// 补充用户数据
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public IEnumerable<GiftOrderInfo> OrderAddUserInfo(IEnumerable<GiftOrderInfo> orders)
        {
            if (orders.Count() > 0)
            {
                List<long> uids = orders.Select(d => d.UserId).ToList();
                if (uids.Count > 0)
                {
                    List<MemberInfo> userlist = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id.ExIn(uids)).ToList();
                    if (userlist.Count > 0)
                    {
                        foreach (var item in orders)
                        {
                            MemberInfo umdata = userlist.FirstOrDefault(d => d.Id == item.UserId);
                            item.UserName = (umdata == null ? "" : umdata.UserName);
                        }
                    }
                }
            }
            return orders;
        }


        public List<GiftOrderItemInfo> GetOrderItemByOrder(long id)
        {
            return DbFactory.Default.Get<GiftOrderItemInfo>().Where(p => p.OrderId == id).ToList();
        }
        public List<GiftOrderItemInfo> GetOrderItemByOrder(List<long> orders)
        {
            return DbFactory.Default.Get<GiftOrderItemInfo>().Where(p => p.OrderId.ExIn(orders)).ToList();
        }

        /// <summary>
        /// 发货
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shipCompanyName"></param>
        /// <param name="shipOrderNumber"></param>
        public void SendGood(long id, string shipCompanyName, string shipOrderNumber)
        {
            GiftOrderInfo orderdata = GetOrder(id);
            if (string.IsNullOrWhiteSpace(shipCompanyName) || string.IsNullOrWhiteSpace(shipOrderNumber))
            {
                throw new HimallException("请填写快递公司与快递单号");
            }
            if (orderdata == null)
            {
                throw new HimallException("错误的订单编号");
            }
            if (orderdata.OrderStatus != GiftOrderInfo.GiftOrderStatus.WaitDelivery)
            {
                throw new HimallException("订单状态有误，不可重复发货");
            }
            orderdata.ExpressCompanyName = shipCompanyName;
            orderdata.ShipOrderNumber = shipOrderNumber;
            orderdata.ShippingDate = DateTime.Now;
            orderdata.OrderStatus = GiftOrderInfo.GiftOrderStatus.WaitReceiving;
            DbFactory.Default.Update(orderdata);
        }
        /// <summary>
        /// 确认订单到货
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        public void ConfirmOrder(long id, long userId)
        {
            var order = DbFactory.Default.Get<GiftOrderInfo>().Where(a => a.UserId == userId && a.Id == id && a.OrderStatus == GiftOrderInfo.GiftOrderStatus.WaitReceiving).FirstOrDefault();
            if (order == null)
            {
                throw new HimallException("错误的订单编号，或订单状态不对！");
            }
            order.OrderStatus = GiftOrderInfo.GiftOrderStatus.Finish;
            order.FinishDate = DateTime.Now;

            DbFactory.Default.Update(order);
        }
        /// <summary>
        /// 过期自动确认订单到货
        /// </summary>
        public void AutoConfirmOrder()
        {
            try
            {
                var siteSetting = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
                //退换货间隔天数
                int intIntervalDay = siteSetting == null ? 7 : (siteSetting.NoReceivingTimeout == 0 ? 7 : siteSetting.NoReceivingTimeout);
                DateTime waitReceivingDate = DateTime.Now.AddDays(-intIntervalDay);
                DbFactory.Default
                    .Set<GiftOrderInfo>()
                    .Where(a => a.ShippingDate < waitReceivingDate && a.OrderStatus == GiftOrderInfo.GiftOrderStatus.WaitReceiving)
                    .Set(n => n.OrderStatus, GiftOrderInfo.GiftOrderStatus.Finish)
                    .Set(n => n.CloseReason, "完成过期未确认收货的礼品订单")
                    .Set(n => n.FinishDate, DateTime.Now)
                    .Succeed();
            }
            catch (Exception ex)
            {
                Log.Error("AutoConfirmGiftOrder:" + ex.Message + "/r/n" + ex.StackTrace);
            }
        }
        /// <summary>
        /// 已购买数量
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="giftid"></param>
        /// <returns></returns>
        public int GetOwnBuyQuantity(long userid, long giftid)
        {
            return DbFactory.Default.Get<GiftOrderItemInfo>().InnerJoin<GiftOrderInfo>((item, o) => item.OrderId == o.Id && o.UserId == userid && item.GiftId == giftid).Sum<int>(d => d.Quantity);
        }

        #region 生成订单号
        private static object obj = new object();
        /// <summary>
        ///  生成订单号
        ///  <para>所有礼品订单号以1开头</para>
        /// </summary>
        public long GenerateOrderNumber()
        {
            lock (obj)
            {
                int rand;
                char code;
                string orderId = string.Empty;
                Random random = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                for (int i = 0; i < 4; i++)
                {
                    rand = random.Next();
                    code = (char)('0' + (char)(rand % 10));
                    orderId += code.ToString();
                }
                return long.Parse("1" + DateTime.Now.ToString("yyyyMMddfff") + orderId);
            }
        }
        #endregion
    }
}
