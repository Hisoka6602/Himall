using Himall.CommonModel;
using Himall.CommonModel.Delegates;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.Core.Plugins.Payment;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static Himall.Entities.OrderRefundInfo;

namespace Himall.Service
{
    public class RefundService : ServiceBase
    {
        #region 属性
        /// <summary>
        /// 退款成功
        /// </summary>
        public event RefundSuccessed OnRefundSuccessed;
        #endregion

        #region 方法
        public QueryPageModel<OrderRefundInfo> GetOrderRefunds(RefundQuery refundQuery)
        {
            var refunds = WhereBuilder(refundQuery);

            var rets = refunds.OrderByDescending(o => o.Id).ToPagedList(refundQuery.PageNo, refundQuery.PageSize);
            var ordidlst = rets.Select(r => r.OrderId).ToList();
            var orders = DbFactory.Default.Get<OrderInfo>(d => d.Id.ExIn(ordidlst)).ToList();
            var itemIds = rets.Select(p => p.OrderItemId).ToList();
            var orderitems = DbFactory.Default.Get<OrderItemInfo>(p => p.Id.ExIn(itemIds)).ToList();
            var ordser = ServiceProvider.Instance<OrderService>.Create;
            foreach (var item in rets)
            {
                var orderitem = orderitems.FirstOrDefault(p => p.Id == item.OrderItemId);
                if (item.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                {
                    var order = orders.FirstOrDefault(d => d.Id == item.OrderId);
                    if (order != null)
                    {
                        item.EnabledRefundAmount = order.ProductTotalAmount + order.Freight - order.DiscountAmount - order.FullDiscount;
                        item.IsVirtual = order.OrderType == OrderInfo.OrderTypes.Virtual;
                    }
                }
                else
                {
                    item.EnabledRefundAmount = (orderitem.EnabledRefundAmount == null ? 0 : orderitem.EnabledRefundAmount.Value);
                }
                //处理订单售后期
                item.IsOrderRefundTimeOut = ordser.IsRefundTimeOut(item.OrderId);
                TypeInfo typeInfo = DbFactory.Default.Get<TypeInfo>().InnerJoin<ProductInfo>((ti, pi) => ti.Id == pi.TypeId && pi.Id == orderitem.ProductId).FirstOrDefault();
                ProductInfo prodata = DbFactory.Default.Get<ProductInfo>().Where(pi => pi.Id == orderitem.ProductId).FirstOrDefault();
                orderitem.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                orderitem.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                orderitem.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                if (prodata != null)
                {
                    orderitem.ColorAlias = !string.IsNullOrWhiteSpace(prodata.ColorAlias) ? prodata.ColorAlias : orderitem.ColorAlias;
                    orderitem.SizeAlias = !string.IsNullOrWhiteSpace(prodata.SizeAlias) ? prodata.SizeAlias : orderitem.SizeAlias;
                    orderitem.VersionAlias = !string.IsNullOrWhiteSpace(prodata.VersionAlias) ? prodata.VersionAlias : orderitem.VersionAlias;
                }
            }
            var pageModel = new QueryPageModel<OrderRefundInfo>() { Models = rets, Total = rets.TotalRecordCount };
            return pageModel;
        }

        public List<RefundResultItem> AutoVirtualRefund()
        {
            var result = new List<RefundResultItem>();
            try
            {
                var codes = DbFactory.Default.Get<OrderVerificationCodeInfo>()
                    .LeftJoin<OrderItemInfo>((ov, oii) => ov.OrderItemId == oii.Id)
                    .LeftJoin<VirtualProductInfo, OrderItemInfo>((vp, oii) => oii.ProductId == vp.ProductId)
                    .Where(a => a.Status == OrderInfo.VerificationCodeStatus.Expired)
                    .Where<VirtualProductInfo>(vp => vp.SupportRefundType == 2).ToList();
                var codeOrderids = codes.Select(c => c.OrderId).Distinct();
                var orderInfos = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id.ExIn(codeOrderids)).ToList();
                foreach (var order in orderInfos)
                {
                    var orderItem = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == order.Id).FirstOrDefault();
                    var ordercodes = codes.Where(p => p.OrderId == order.Id);
                    OrderRefundInfo refund = new OrderRefundInfo();
                    refund.IsReturn = false;
                    refund.ShopId = order.ShopId;
                    refund.ShopName = order.ShopName;
                    refund.ReturnQuantity = ordercodes.Count();
                    decimal refundGoodsPrice = 0;
                    if (orderItem.EnabledRefundAmount.HasValue)
                    {
                        refundGoodsPrice = Math.Round(orderItem.EnabledRefundAmount.Value / orderItem.Quantity, 2);
                    }
                    refund.Amount = refund.ReturnQuantity * refundGoodsPrice;
                    var enableRefundPrice = orderItem.EnabledRefundAmount.Value - orderItem.RefundPrice;
                    if (refund.Amount > enableRefundPrice) refund.Amount = enableRefundPrice;
                    refund.UserId = order.UserId;
                    refund.ApplyDate = DateTime.Now;
                    refund.Reason = "过期可退，系统自动发起退款申请";
                    refund.RefundMode = OrderRefundInfo.OrderRefundMode.OrderRefund;

                    refund.RefundPayType = OrderRefundInfo.OrderRefundPayType.BackCapital;
                    if (CanBackOut(order))
                    {
                        refund.RefundPayType = OrderRefundInfo.OrderRefundPayType.BackOut;
                    }
                    refund.SellerAuditDate = DateTime.Now;
                    refund.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.Audited;
                    refund.ManagerConfirmDate = DateTime.Now;
                    refund.ManagerConfirmStatus = OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm;
                    refund.OrderItemId = orderItem.Id;
                    refund.OrderId = order.Id;
                    refund.VerificationCodeIds = string.Join(",", ordercodes.Select(p => p.VerificationCode));
                    refund.ApplyNumber = 1;
                    var user = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == order.UserId).FirstOrDefault();
                    if (user != null)
                    {
                        refund.ContactPerson = string.IsNullOrEmpty(user.RealName) ? user.UserName : user.RealName;
                        refund.Applicant = user.UserName;
                        refund.ContactCellPhone = user.CellPhone;
                    }
                    //虚拟订单退还佣金计算
                    var returnPlatCommission = Math.Round(refund.Amount * orderItem.CommisRate, 2, MidpointRounding.AwayFromZero);
                    refund.ReturnPlatCommission = returnPlatCommission;
                    DbFactory.Default.Add(refund);
                    UpdateOrderVerificationCodeStatusByCodes(ordercodes.Select(p => p.VerificationCode).ToList(), order.Id, OrderInfo.VerificationCodeStatus.Refund);
                    ConfirmRefund(refund.Id, "虚拟订单申请售后自动退款", "系统JOB", string.Empty);
                    var reason = refund.Reason;
                    if (!string.IsNullOrEmpty(refund.ReasonDetail))
                        reason += ":" + refund.ReasonDetail;
                    AddRefundLog(refund.Id, refund.ApplyNumber, OrderRefundStep.UnConfirm, refund.RefundStatus, refund.ContactPerson, reason);
                    result.Add(new RefundResultItem
                    {
                        OrderId = order.Id,
                        UserId = order.UserId,
                        OrderItemId = orderItem.Id,
                        RefundId = refund.Id
                    });
                }
            }
            catch (Exception ex)
            {
                Log.Error("虚拟订单核销码过期自动退：" + ex.ToString());
            }
            return result;
        }

        public bool UpdateOrderVerificationCodeStatusByCodes(List<string> verficationCodes, long orderId, OrderInfo.VerificationCodeStatus status)
        {
            bool result = false;
            result = DbFactory.Default.Set<OrderVerificationCodeInfo>().Set(p => p.Status, status).Where(p => p.VerificationCode.ExIn(verficationCodes)).Succeed();

            OrderInfo.OrderOperateStatus orderStatus = 0;
            var orderVerificationCodes = DbFactory.Default.Get<OrderVerificationCodeInfo>().Where(a => a.OrderId == orderId).ToList();
            //int count1 = orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification || a.Status == OrderInfo.VerificationCodeStatus.Refund).Count();
            int count1 = DbFactory.Default.Get<OrderVerificationCodeInfo>()
                .LeftJoin<OrderItemInfo>((ov, oii) => ov.OrderItemId == oii.Id)
                .LeftJoin<VirtualProductInfo, OrderItemInfo>((vp, oii) => oii.ProductId == vp.ProductId)
                .Where<OrderVerificationCodeInfo, VirtualProductInfo>((a, v) => (a.Status == OrderInfo.VerificationCodeStatus.Expired && v.SupportRefundType == 2) || a.Status == OrderInfo.VerificationCodeStatus.WaitVerification || a.Status == OrderInfo.VerificationCodeStatus.Refund)
                .Where(a => a.OrderId == orderId).Count();
            int count2 = 0, count3 = 0;
            if (count1 > 0)
            {
                orderStatus = OrderInfo.OrderOperateStatus.WaitVerification;
            }
            else
            {
                count3 = orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.Expired || a.Status == OrderInfo.VerificationCodeStatus.RefundComplete).Count();
                if (count3 == orderVerificationCodes.Count())
                {
                    orderStatus = OrderInfo.OrderOperateStatus.Close;
                }
                else
                {
                    var alreadyVerificationInfo = orderVerificationCodes.FirstOrDefault(a => a.Status == OrderInfo.VerificationCodeStatus.AlreadyVerification);
                    if (alreadyVerificationInfo != null)
                    {
                        var other = orderVerificationCodes.Where(a => a.Id != alreadyVerificationInfo.Id);//排除已核销
                        count2 = other.Where(a => a.Status != OrderInfo.VerificationCodeStatus.WaitVerification && a.Status != OrderInfo.VerificationCodeStatus.Refund).Count();
                        if (count2 == other.Count())
                        {
                            orderStatus = OrderInfo.OrderOperateStatus.Finish;
                        }
                    }
                }
            }
            if (orderStatus != 0)
            {
                if (orderStatus == OrderInfo.OrderOperateStatus.Finish || orderStatus == OrderInfo.OrderOperateStatus.Close)
                {
                    var closeReason = "";
                    if (orderStatus == OrderInfo.OrderOperateStatus.Close)
                    {
                        closeReason = "核销码已过期，自动关闭";
                        if (orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.RefundComplete).Count() == orderVerificationCodes.Count)
                        {
                            closeReason = "核销码已退款，自动关闭";
                        }
                    }
                    DbFactory.Default.Set<OrderInfo>().Set(p => p.OrderStatus, orderStatus).Set(p => p.FinishDate, DateTime.Now).Set(p => p.CloseReason, closeReason).Where(a => a.Id == orderId).Succeed();
                    //会员确认收货后，不会马上给积分，得需要过了售后维权期才给积分(虚拟商品除外)
                    var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
                    var member = DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == order.UserId).FirstOrDefault();
                    AddIntegral(member, order.Id, order.TotalAmount - order.RefundTotalAmount);//增加积分
                    //更新待结算订单完成时间
                    UpdatePendingSettlnmentFinishDate(orderId, DateTime.Now);
                }
                else
                {
                    DbFactory.Default.Set<OrderInfo>().Set(p => p.OrderStatus, orderStatus).Where(a => a.Id == orderId).Succeed();
                }
            }
            return result;
        }
        /// <summary>
        /// 更新待结算订单完成时间
        /// </summary>
        /// <param name="order"></param>
        private void UpdatePendingSettlnmentFinishDate(long orderid, DateTime dt)
        {
            DbFactory.Default.Set<PendingSettlementOrderInfo>().Set(e => e.OrderFinshTime, dt).Where(e => e.OrderId == orderid).Succeed();
        }
        public void AddIntegral(MemberInfo member, long orderId, decimal orderTotal)
        {
            var IntegralExchange = DbFactory.Default.Get<MemberIntegralExchangeRuleInfo>().FirstOrDefault();
            if (IntegralExchange == null)
            {
                return; //没设置兑换规则直接返回
            }
            var MoneyPerIntegral = IntegralExchange.MoneyPerIntegral;
            if (MoneyPerIntegral == 0)
            {
                return;
            }
            var integral = Convert.ToInt32(Math.Floor(orderTotal / MoneyPerIntegral));
            MemberIntegralRecordInfo record = new MemberIntegralRecordInfo();
            record.UserName = member.UserName;
            record.MemberId = member.Id;
            record.RecordDate = DateTime.Now;
            record.TypeId = MemberIntegralInfo.IntegralType.Consumption;
            record.Integral = integral;
            DbFactory.Default.Add(record);
            MemberIntegralRecordActionInfo action = new MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = MemberIntegralInfo.VirtualItemType.Consumption;
            action.VirtualItemId = orderId;
            action.IntegralRecordId = record.Id;
            DbFactory.Default.Add(action);

            AddMemberIntegral(record);
        }
        public static void AddMemberIntegral(MemberIntegralRecordInfo model)
        {
            if (null == model) { throw new NullReferenceException("添加会员积分记录时，会员积分Model为空."); }
            if (0 == model.MemberId) { throw new NullReferenceException("添加会员积分记录时，会员Id为空."); }
            var userCount = DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == model.MemberId && a.UserName == model.UserName).Count();
            if (userCount <= 0)
            {
                throw new Himall.Core.HimallException("不存在此会员");
            }
            if (model.Integral == 0)
            {
                return;
            }
            var userIntegral = DbFactory.Default.Get<MemberIntegralInfo>().Where(a => a.MemberId == model.MemberId).FirstOrDefault();

            if (userIntegral == null)
            {
                userIntegral = new MemberIntegralInfo();
                userIntegral.MemberId = model.MemberId;
                userIntegral.UserName = model.UserName;
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Add(userIntegral);
            }
            else
            {
                if (model.Integral > 0)
                {
                    userIntegral.HistoryIntegrals += model.Integral;
                }
                else
                {
                    if (userIntegral.AvailableIntegrals < Math.Abs(model.Integral))
                        throw new Himall.Core.HimallException("用户积分不足以扣减该积分！");
                }
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Update(userIntegral);
            }
        }
        /// <summary>
        /// 是否可原路返回
        /// </summary>
        /// <returns></returns>
        private static bool CanBackOut(OrderInfo order)
        {
            bool result = false;
            if (!string.IsNullOrWhiteSpace(order.PaymentTypeGateway))
            {
                if (order.CapitalAmount <= 0 && (order.PaymentTypeGateway.ToLower().Contains("weixin") || order.PaymentTypeGateway.ToLower().Contains("alipay")))
                {
                    result = true;
                }
            }
            return result;
        }


        public List<long> GetConfirmTimeOut(DateTime exprieTime) =>
            DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving && p.BuyerDeliverDate < exprieTime).Select(p => p.Id).ToList<long>();

        public List<long> GetDeliverTimeOut(DateTime exprieTime) =>
            DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery && p.SellerAuditDate < exprieTime).Select(p => p.Id).ToList<long>();

        public List<long> GetAuditTimeout(DateTime exprieTime) =>
            DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit && p.ApplyDate < exprieTime).Select(p => p.Id).ToList<long>();

        public int GetOrderRefundCount(RefundQuery query)
        {
            var db = WhereBuilder(query);
            return db.Count();
        }
        private GetBuilder<OrderRefundInfo> WhereBuilder(RefundQuery query)
        {
            var db = DbFactory.Default
                .Get<OrderRefundInfo>()
                .InnerJoin<OrderItemInfo>((ori, oii) => ori.OrderItemId == oii.Id)
                .InnerJoin<OrderInfo>((ori, oi) => ori.OrderId == oi.Id);

            if (query.StartDate.HasValue)
                db.Where(item => item.ApplyDate >= query.StartDate);

            if (query.OrderId.HasValue)
            {
                var orderIdRange = GetOrderIdRange(query.OrderId.Value);
                var min = orderIdRange[0];
                if (orderIdRange.Length == 2)
                {
                    var max = orderIdRange[1];
                    db.Where(item => item.OrderId >= min && item.OrderId <= max);
                }
                else
                    db.Where(item => item.OrderId == min);
            }

            if (query.EndDate.HasValue)
            {
                var enddate = query.EndDate.Value.Date.AddDays(1);
                db.Where(item => item.ApplyDate < enddate);
            }

            if (query.ConfirmStatus.HasValue)
                db.Where(item => item.ManagerConfirmStatus == query.ConfirmStatus.Value);

            if (query.ShopId.HasValue)
                db.Where(item => query.ShopId == item.ShopId);

            if (query.UserId.HasValue)
                db.Where(item => item.UserId == query.UserId);

            if (!string.IsNullOrWhiteSpace(query.ProductName))
                db.Where<OrderItemInfo>(item => item.ProductName.Contains(query.ProductName));

            if (!string.IsNullOrWhiteSpace(query.ShopName))
                db.Where(item => item.ShopName.Contains(query.ShopName));

            if (!string.IsNullOrWhiteSpace(query.UserName))
                db.Where(item => item.Applicant.Contains(query.UserName));

            //多订单结果集查询
            if (query.MoreOrderId != null && query.MoreOrderId.Count > 0)
            {
                query.MoreOrderId = query.MoreOrderId.Distinct().ToList();
                db.Where(d => d.OrderId.ExIn(query.MoreOrderId));
            }
            if (query.ShowRefundType.HasValue)
            {
                switch (query.ShowRefundType)
                {
                    case 1:
                        db.Where(d => d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund);
                        break;
                    case 2:
                        db.Where(d => d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderItemRefund || d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund);
                        break;
                    case 3:
                        db.Where(d => d.RefundMode == OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund);
                        break;
                    case 4:
                        db.Where(d => d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderItemRefund);
                        break;
                }
            }
            if (query.RefundModes != null && query.RefundModes.Count > 0)
                db.Where(p => p.RefundMode.ExIn(query.RefundModes));

            if (query.AuditStatus.HasValue)
            {
                if (query.AuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                    db.Where(item => item.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit || item.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving);
                else
                    db.Where(item => item.SellerAuditStatus == query.AuditStatus);
            }

            //商家审核状态
            if (query.AuditStatusList != null && query.AuditStatusList.Count > 0)
                db.Where(p => p.SellerAuditStatus.ExIn(query.AuditStatusList));

            if (query.ShopBranchId.HasValue)
            {
                if (query.ShopBranchId.Value > 0)
                {
                    var sbId = query.ShopBranchId.Value;
                    db.Where<OrderInfo>(p => p.ShopBranchId.ExIfNull(0) == sbId);
                }
                else
                {
                    db.Where<OrderInfo>(p => p.ShopBranchId.ExIfNull(0) == 0);
                }
            }
            if (query.IsOngoing)
            {
                db.Where<OrderRefundInfo>(d => d.ManagerConfirmStatus != OrderRefundInfo.OrderRefundConfirmStatus.Confirmed && d.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.UnAudit);
            }
            if (query.IsFilterVirtual.HasValue && query.IsFilterVirtual.Value)
            {
                db.Where<OrderInfo>(p => p.OrderType != OrderInfo.OrderTypes.Virtual);
            }
            return db;
        }

        /// <summary>
        /// 获取退款列表(忽略分页)
        /// </summary>
        /// <param name="refundQuery"></param>
        /// <returns></returns>
        public List<OrderRefundInfo> GetAllOrderRefunds(RefundQuery refundQuery)
        {
            var refunds = WhereBuilder(refundQuery);

            return refunds.OrderByDescending(o => o.Id).ToList();
        }

        private long[] GetOrderIdRange(long orderId)
        {
            var temp = 16;
            var length = orderId.ToString().Length;
            if (length < temp)
            {
                var len = temp - length;
                orderId = orderId * (long)Math.Pow(10, len);
                var max = orderId + long.Parse(string.Join("", new int[len].Select(p => 9)));
                return new[] { orderId, max };
            }
            else if (length == temp)
                return new[] { orderId };
            return null;
        }

        #region 退款方式方法体
        private object lockobj = new object();
        /// <summary>
        /// 生成一个新的退款批次号
        /// </summary>
        /// <returns></returns>
        private string GetNewRefundBatchNo()
        {
            string result = "";
            lock (lockobj)
            {
                int rand;
                char code;
                result = string.Empty;
                Random random = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                for (int i = 0; i < 5; i++)
                {
                    rand = random.Next();
                    code = (char)('0' + (char)(rand % 10));
                    result += code.ToString();
                }
                result = DateTime.Now.ToString("yyyyMMddfff") + result;
            }
            return result;
        }

        /// <summary>
        /// 对PaymentId进行加密（因为PaymentId中包含小数点"."，因此进行编码替换）
        /// </summary>
        string EncodePaymentId(string paymentId)
        {
            return paymentId.Replace(".", "-");
        }

        /// <summary>
        /// 原路退回
        /// </summary>
        /// <param name="refund"></param>
        /// <returns>异步请求的地址，如果同步请返回空</returns>
        private string RefundBackOutGroup(OrderRefundInfo refund, string notifyurl, out bool isRefundIntegral)
        {
            isRefundIntegral = false;
            string result = "";
            decimal refundfee = refund.Amount;
            var order = DbFactory.Default.Get<OrderInfo>(p => p.Id == refund.OrderId).FirstOrDefault();
            var orderitem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();
            decimal ratediscount = 0;
            if (refundfee > 0 && orderitem.EnabledRefundAmount.HasValue && orderitem.EnabledRefundAmount.Value > 0)
            {
                if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund && order.OrderType != OrderInfo.OrderTypes.Virtual)
                {
                    ratediscount = order.IntegralDiscount;
                }
                else
                {
                    ratediscount = orderitem.EnabledRefundIntegral.Value / (orderitem.EnabledRefundAmount.Value / refundfee);//根据退款金额计算积分抵扣的对应比率；
                }
            }
            //if (refund.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund)//整单退款不需要先减掉积分比例金额
            //{
            //    refundfee = refundfee - ratediscount;//得到积分抵扣对比率后的实际退款金额
            //}
            if (ratediscount > 0)//积分退回金额大于0则先退积分
            {
                #region 积分抵扣补回
                decimal refundinfee = ratediscount;
                //虚拟订单退积分不能直接用积分优惠金额，要实时计算
                if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund && order.OrderType != OrderInfo.OrderTypes.Virtual)
                {
                    refundinfee = order.IntegralDiscount;
                }
                Log.Error($"退积分1:{refund.Id}");
                isRefundIntegral = true;
                ReturnIntegral(refund.Id, refund.UserId, refundinfee);
                #endregion
            }
            decimal payRefundIntegralAmount = 0;
            if (refund.RefundPayStatus != OrderRefundInfo.OrderRefundPayStatus.PaySuccess)
            {
                var Order = DbFactory.Default.Get<OrderInfo>(p => p.Id == refund.OrderId).FirstOrDefault();
                if (Order == null)
                    throw new HimallException("退款单对应订单异常！");
                var OrderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();
                if (OrderItem == null)
                    throw new HimallException("退款单对应订单项异常！");

                var payWay = Order.PaymentTypeGateway;

                var paymentPlugins = Core.PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(item => item.PluginInfo.PluginId == payWay);
                if (paymentPlugins.Count() > 0)
                {
                    var OrderPayInfo = DbFactory.Default.Get<OrderPayInfo>().Where(e => e.PayState && e.OrderId == refund.OrderId).FirstOrDefault();
                    if (OrderPayInfo != null)
                    {
                        //总微信支付金额
                        var orderIds = DbFactory.Default.Get<OrderPayInfo>().Where(item => item.PayId == OrderPayInfo.PayId && item.PayState == true).Select(e => e.OrderId).ToList<long>();
                        var amount = DbFactory.Default.Get<OrderInfo>().Where(o => o.Id.ExIn(orderIds)).ToList().Sum(e => e.OrderTotalAmount);
                        amount = amount - DbFactory.Default.Get<OrderInfo>().Where(o => o.Id.ExIn(orderIds)).ToList().Sum(e => e.CapitalAmount);
                        string paytradeno = Order.GatewayOrderId;

                        if (string.IsNullOrEmpty(paytradeno))
                        {
                            throw new HimallException("未找到支付流水号！");
                        }
                        notifyurl = string.Format(notifyurl, EncodePaymentId(payWay));
                        ///订单实收金额大于等于退款金额
                        //本订单微信支付总金额

                        decimal vxPayAmount = 0;
                        //扣分抵扣金额>余额支付金额，这时需要从在线支付的金额中减少退款金额必免多退,否则积分抵扣的金额都从余额支付中减掉
                        if (ratediscount > order.CapitalAmount)
                        {
                            payRefundIntegralAmount = ratediscount - order.CapitalAmount;
                        }
                        if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                        {
                            vxPayAmount = order.TotalAmount - Order.RefundTotalAmount - Order.CapitalAmount;
                        }
                        else
                        {
                            vxPayAmount = Order.TotalAmount - Order.RefundTotalAmount - Order.CapitalAmount - payRefundIntegralAmount;
                        }
                        if (vxPayAmount < 0) vxPayAmount = 0;

                        if (vxPayAmount < refundfee)
                        {
                            refundfee = vxPayAmount;
                        }

                        if (refundfee > 0)
                        {
                            string refund_batch_no = GetNewRefundBatchNo();
                            //退款流水号处理
                            if (!refund.RefundPostTime.HasValue)
                            {
                                refund.RefundPostTime = DateTime.Now.AddDays(-2);
                            }
                            //支付宝一天内可共用同一个流水号
                            if (refund.RefundPostTime.Value.Date == DateTime.Now.Date && !string.IsNullOrWhiteSpace(refund.RefundBatchNo))
                            {
                                refund_batch_no = refund.RefundBatchNo;
                            }
                            else
                            {
                                refund.RefundBatchNo = refund_batch_no;
                            }
                            refund.RefundPostTime = DateTime.Now;


                            PaymentPara para = new PaymentPara()
                            {
                                out_refund_no = refund_batch_no,
                                out_trade_no = OrderPayInfo.PayId.ToString(),
                                pay_trade_no = paytradeno,
                                refund_fee = refundfee,
                                total_fee = amount,
                                notify_url = notifyurl
                            };

                            var refundResult = paymentPlugins.FirstOrDefault().Biz.ProcessRefundFee(para);
                            if (refundResult.RefundResult == RefundState.Success)
                            {
                                if (refundResult.RefundMode == RefundRunMode.Sync)
                                {
                                    refund.RefundPayStatus = OrderRefundInfo.OrderRefundPayStatus.PaySuccess;
                                }
                                if (refundResult.RefundMode == RefundRunMode.Async)
                                {
                                    result = refundResult.ResponseContentWhenFinished;
                                    refund.RefundBatchNo = refundResult.RefundNo;
                                    refund.RefundPayStatus = OrderRefundInfo.OrderRefundPayStatus.Payed;
                                }

                                DbFactory.Default.Update(refund);
                            }
                            else
                            {
                                throw new HimallException("退款插件工作未完成！");
                            }
                        }
                        else
                        {
                            refund.RefundPayStatus = OrderRefundInfo.OrderRefundPayStatus.PaySuccess;
                        }
                        //组合支付如果预存款支付金额大于0先退预存款
                        if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                        {
                            RefundBackCapitalGroup(refund, order.CapitalAmount, Order, OrderItem);
                        }
                        else
                        {
                            //总申请金额-原路返回退款金额-积分退回金额=预付应退金额
                            var reamount = refund.Amount - refundfee - ratediscount;
                            RefundBackCapitalGroup(refund, reamount, Order, OrderItem);
                        }
                    }
                    else
                    {
                        throw new HimallException("退款时，未找到原支付订单信息！");
                    }
                }
                else
                {
                    throw new HimallException("退款时，未找到支付方式！");
                }
            }
            return result;
        }


        /// <summary>
        /// 组合支付退预存款 部分金额退到预存款
        /// </summary>
        /// <param name="refund"></param>
        private void RefundBackCapitalGroup(OrderRefundInfo refund, decimal reamount, OrderInfo order, OrderItemInfo orderItem)
        {

            if (refund.RefundPayStatus == OrderRefundInfo.OrderRefundPayStatus.PaySuccess)
            {
                decimal refundfee = reamount;

                if (order.CapitalAmount > 0)
                {
                    if (refundfee > order.CapitalAmount)
                    {
                        refundfee = order.CapitalAmount;
                    }

                }
                else
                {
                    refundfee = 0;
                }

                if (refundfee > 0)
                {
                    CapitalDetailModel capita = new CapitalDetailModel
                    {
                        UserId = refund.UserId,
                        Amount = refundfee,
                        SourceType = CapitalDetailInfo.CapitalDetailType.Refund,
                        SourceData = refund.OrderId.ToString(),
                        CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    new MemberCapitalService().AddCapital(capita, false);
                }

            }

        }

        private void ReturnIntegral(long refundId, long userId, decimal integralFee)
        {

            if (integralFee <= 0) return;
            var integralService = ServiceProvider.Instance<MemberIntegralService>.Create;
            var integralExchange = integralService.GetIntegralChangeRule();
            if (integralExchange != null && integralExchange.IntegralPerMoney > 0)
            {
                //只处理有兑换规则的积分处理
                int IntegralPerMoney = integralExchange.IntegralPerMoney;
                int BackIntegral = (int)Math.Floor(integralFee * IntegralPerMoney);
                var member = DbFactory.Default.Get<MemberInfo>().Where(p => p.Id == userId).FirstOrDefault();
                var _curuintg = integralService.GetMemberIntegral(member.Id);
                if (BackIntegral > 0)
                {
                    //补充订单退款的积分
                    MemberIntegralRecordInfo info = new MemberIntegralRecordInfo();
                    info.UserName = member.UserName;
                    info.MemberId = member.Id;
                    info.RecordDate = DateTime.Now;
                    info.TypeId = MemberIntegralInfo.IntegralType.Others;
                    info.ReMark = "售后编号【" + refundId + "】退款时退还抵扣积分" + BackIntegral.ToString();
                    var memberIntegral = new MemberIntegralConversionFactoryService().Create(MemberIntegralInfo.IntegralType.Others, BackIntegral);
                    integralService.AddMemberIntegral(info, memberIntegral);
                }
            }
        }

        /// <summary>
        /// 退到预付款
        /// </summary>
        /// <param name="refund"></param>
        private void RefundBackCapital(OrderRefundInfo refund)
        {

            if (refund.RefundPayStatus != OrderRefundInfo.OrderRefundPayStatus.PaySuccess)
            {
                decimal refundfee = refund.Amount;
                var order = DbFactory.Default.Get<OrderInfo>(p => p.Id == refund.OrderId).FirstOrDefault();
                var orderitem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();

                if (order.CapitalAmount > 0)
                {

                    if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                    {
                        refundfee = refundfee - order.IntegralDiscount;
                    }
                    else
                    {
                        if (orderitem.EnabledRefundIntegral < 0)
                            throw new HimallException("退款时，积分可退金额异常！");
                        if (orderitem.EnabledRefundAmount < 0)
                            throw new HimallException("退款时，总可退金额异常！");

                        decimal ratediscount = 0;
                        if (refundfee > 0 && orderitem.EnabledRefundAmount.Value > 0)
                        {
                            ratediscount = orderitem.EnabledRefundIntegral.Value / (orderitem.EnabledRefundAmount.Value / refundfee);//根据退款金额计算积分抵扣的对应比率；
                        }
                        refundfee = refundfee - ratediscount;//得到积分抵扣对比率后的实际退款金额

                        if (refundfee > order.CapitalAmount + order.IntegralDiscount - order.RefundTotalAmount)
                        {
                            refundfee = order.CapitalAmount + order.IntegralDiscount - order.RefundTotalAmount;
                        }
                    }
                }
                else
                {
                    refundfee = 0;
                }

                if (refundfee > 0)
                {
                    CapitalDetailModel capita = new CapitalDetailModel
                    {
                        UserId = refund.UserId,
                        Amount = refundfee,
                        SourceType = CapitalDetailInfo.CapitalDetailType.Refund,
                        SourceData = refund.OrderId.ToString(),
                        CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    new MemberCapitalService().AddCapital(capita, false);
                }
                refund.RefundPayStatus = OrderRefundInfo.OrderRefundPayStatus.PaySuccess;
            }
        }
        /// <summary>
        /// 线下退款
        /// </summary>
        /// <param name="refund"></param>
        private bool RefundBackOffLine(OrderRefundInfo refund)
        {
            MemberCapitalService capitalServicer = Himall.ServiceProvider.Instance<MemberCapitalService>.Create;

            if (refund.RefundPayStatus != OrderRefundInfo.OrderRefundPayStatus.PaySuccess)
            {
                decimal refundfee = refund.Amount;

                var order = DbFactory.Default.Get<OrderInfo>(p => p.Id == refund.OrderId).FirstOrDefault();
                var orderitem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();

                if (refund.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund)
                {
                    if (orderitem.EnabledRefundIntegral > 0 && orderitem.EnabledRefundAmount > 0)
                    {
                        if (refundfee > (orderitem.EnabledRefundAmount - orderitem.EnabledRefundIntegral))
                        {
                            refundfee = orderitem.EnabledRefundAmount.Value - orderitem.EnabledRefundIntegral.Value;
                        }
                    }
                }
                else
                {
                    if (order.OrderType != OrderInfo.OrderTypes.Virtual)
                    {
                        refundfee = order.OrderTotalAmount;
                    }
                    else
                    {
                        var totalrefundmoney = orderitem.EnabledRefundAmount.Value - orderitem.EnabledRefundIntegral.Value - orderitem.RefundPrice;
                        if (totalrefundmoney < refund.Amount)
                            refundfee = totalrefundmoney;
                    }
                }
                refund.RefundPayStatus = OrderRefundInfo.OrderRefundPayStatus.PaySuccess;
                return (refundfee > 0);
            }

            return false;
        }
        #endregion

        #region 退款成功处理
        /// <summary>
        /// 退款成功后的处理
        /// </summary>
        /// <param name="refund"></param>
        private bool RefundSuccessed(OrderRefundInfo refund, string managerName, bool isRefundIntegral = false)
        {
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == refund.OrderId).FirstOrDefault();
            var orderItem = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.Id == refund.OrderItemId).FirstOrDefault();
            var member = DbFactory.Default.Get<MemberInfo>().Where(p => p.Id == refund.UserId).FirstOrDefault();
            if (refund.ManagerConfirmStatus != OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm)
                throw new HimallException("只有未确认状态的退款/退货才能进行确认操作！");
            var ret = DbFactory.Default.InTransaction(() =>
            {
                if (refund.RefundPayStatus == OrderRefundInfo.OrderRefundPayStatus.PaySuccess)
                {
                    if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                    {
                        var orditemlist = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == refund.OrderId).ToList();
                        foreach (var i in orditemlist)
                        {
                            if (order.OrderType == OrderInfo.OrderTypes.Virtual)//虚拟订单退款很特殊，是按核销码去退的
                            {
                                //一个虚拟订单可以分多次退
                                i.ReturnQuantity += refund.VerificationCodeIds.Split(',').Count();//退款核销码
                                var refundprice = refund.Amount;
                                var totalrefundmoney = i.EnabledRefundAmount.Value - i.RefundPrice;
                                if (totalrefundmoney < refund.Amount)
                                    refundprice = totalrefundmoney;
                                i.RefundPrice += refundprice;//退款金额
                            }
                            else
                            {
                                i.ReturnQuantity = i.Quantity;
                                if (i.EnabledRefundAmount == null)
                                    i.EnabledRefundAmount = 0;
                                i.RefundPrice = i.EnabledRefundAmount.Value;
                            }
                            if (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || order.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp)  //待发货退库存
                            {
                                ReturnStock(i, order, i.Quantity);
                            }
                            //虚拟订单退款，按核销码个数退
                            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                            {
                                ReturnStock(i, order, refund.VerificationCodeIds.Split(',').Count());
                            }

                            DbFactory.Default.Update(i);
                        }
                    }
                    else
                    {
                        orderItem.RefundPrice = refund.Amount;
                        orderItem.ReturnQuantity = refund.ShowReturnQuantity;
                        DbFactory.Default.Update(orderItem);
                    }

                    //实付(金额不含运费)
                    decimal orderRealPay = (order.OrderTotalAmount - order.Freight);
                    //可退(金额)
                    decimal orderCanRealRefund = orderRealPay;

                    decimal realRefundAmount = refund.Amount;
                    if (orderItem.EnabledRefundIntegral > 0 && orderItem.EnabledRefundAmount > 0)
                    {
                        if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                        {
                            var re = orderItem.EnabledRefundAmount.Value - orderItem.EnabledRefundIntegral.Value - orderItem.RefundPrice;
                            //修正负数，为负数，说明没有可退的实付金额（当只剩积分未退时，可能为负）
                            re = re > 0 ? re : 0.00M;
                            if (realRefundAmount > re)
                                realRefundAmount = re;
                        }
                        else
                        {
                            if (realRefundAmount > orderItem.EnabledRefundAmount - orderItem.EnabledRefundIntegral)
                            {
                                realRefundAmount = orderItem.EnabledRefundAmount.Value - orderItem.EnabledRefundIntegral.Value;
                            }
                        }
                    }

                    realRefundAmount = Math.Round(realRefundAmount, 2);
                    decimal refundActualPayAmount = 0;
                    if (realRefundAmount > 0)
                    {
                        //修改实收金额
                        order.ActualPayAmount -= realRefundAmount;
                        refundActualPayAmount = realRefundAmount;
                        if (order.OrderType != OrderInfo.OrderTypes.Virtual)//虚拟订单只会订单退款，这里不处理退款金额
                        {
                            order.RefundTotalAmount += refund.Amount;
                            if (order.RefundTotalAmount > orderRealPay)
                            {
                                order.RefundTotalAmount = orderRealPay;
                            }
                        }
                    }
                    //修正整笔退
                    if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                    {
                        orderRealPay = order.OrderTotalAmount;
                        orderCanRealRefund = order.OrderTotalAmount;
                        if (order.OrderType != OrderInfo.OrderTypes.Virtual)
                        {
                            realRefundAmount = order.OrderTotalAmount;
                            order.RefundTotalAmount = realRefundAmount;//普通订单整笔退，为订单实付总金额
                        }
                        else
                        {
                            order.RefundTotalAmount += realRefundAmount;//一个虚拟订单可以分多次退，故累计
                        }
                    }

                    DbFactory.Default.Update(order);

                    var integralExchange = ServiceProvider.Instance<MemberIntegralService>.Create.GetIntegralChangeRule();

                    #region 积分抵扣补回

                    decimal refundinfee = 0;
                    if (!isRefundIntegral && order.IntegralDiscount > 0)
                    {
                        //整单退返回所有积分抵扣金额
                        if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                        {
                            refundinfee = order.IntegralDiscount;
                        }
                        else
                        {
                            refundinfee = ClacIntegralRatio(refund, orderItem.EnabledRefundAmount.Value, orderItem.EnabledRefundIntegral.Value);
                        }
                        ReturnIntegral(refund.Id, refund.UserId, refundinfee);
                    }
                    #endregion

                    //数据持久
                    refund.ManagerConfirmStatus = OrderRefundInfo.OrderRefundConfirmStatus.Confirmed;
                    refund.ManagerConfirmDate = DateTime.Now;

                    //销量退还(店铺、商品)
                    if (order.PayDate.HasValue)
                    {
                        // 修改店铺访问量
                        UpdateShopVisti(refund, order.PayDate.Value);

                        // 修改商品销量
                        UpdateProductVisti(refund, order.PayDate.Value);

                        //会员服务
                        var memberService = ServiceProvider.Instance<MemberService>.Create;

                        memberService.UpdateNetAmount(refund.UserId, -refundActualPayAmount);//减少用户的净消费额

                        //下单量即消费次数，退款不做处理
                        //减少限时抢购销量
                        LimitTimeBuyService limitServicer = Himall.ServiceProvider.Instance<LimitTimeBuyService>.Create;
                        limitServicer.ReduceSaleCount(refund);
                    }
                    DbFactory.Default.Update(refund);

                    #region 处理分销分佣
                    if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                    {
                        Himall.ServiceProvider.Instance<DistributionService>.Create.TreatedOrderDistributionBrokerage(refund.OrderId, false);
                    }
                    else
                    {
                        Himall.ServiceProvider.Instance<DistributionService>.Create.TreatedOrderDistributionBrokerage(refund.OrderId, false, refund.OrderItemId);
                    }
                    #endregion

                    #region 全部退货后关闭订单
                    bool isCloseOrder = true;
                    var orderItems = DbFactory.Default.Get<OrderItemInfo>(p => p.OrderId == order.Id).ToList();
                    var refunds = DbFactory.Default.Get<OrderRefundInfo>(p => p.OrderId == order.Id).ToList();

                    if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                    {
                        //虚拟订单状态是随核销码变化而变化
                        ServiceProvider.Instance<OrderService>.Create.UpdateOrderVerificationCodeStatusByCodes(refund.VerificationCodeIds.Split(',').ToList(), order.Id, OrderInfo.VerificationCodeStatus.RefundComplete, null);
                    }
                    if (orderItems.Any(p => p.Quantity > p.ReturnQuantity)
        || refunds.Any(p => p.ManagerConfirmStatus != OrderRefundInfo.OrderRefundConfirmStatus.Confirmed))
                        isCloseOrder = false;
                    if (order.OrderType != OrderInfo.OrderTypes.Virtual && isCloseOrder)
                    {
                        managerName = "已退货/退款，订单自动关闭";
                        order.CloseReason = managerName;
                        order.OrderStatus = OrderInfo.OrderOperateStatus.Close;
                        DbFactory.Default.Update(order);
                        //发生退款时重新计算待付结算订单
                        RefundSettlement(refund.OrderId, refund.Id, refund.VerificationCodeIds, true);
                    }
                    else
                    {
                        //发生退款时重新计算待付结算订单
                        RefundSettlement(refund.OrderId, refund.Id, refund.VerificationCodeIds, false);
                    }

                    RefundCoupons(order);
                    #endregion
                }
            });
            return ret;
        }

        /// <summary>
        /// 订单取消退回代金红包 TODO:ZYF
        /// </summary>
        private void ReturnShopBonus(long userId, long orderId, long couponId)
        {
            if (couponId <= 0)
            {
                return;
            }
            var shopbouns = DbFactory.Default.Get<ShopBonusInfo>().Where(c => c.Id == couponId).FirstOrDefault();
            if (shopbouns == null)
            {
                throw new HimallException("带金红包不存在");
            }
            var bounsRecord = DbFactory.Default.Get<ShopBonusReceiveInfo>().Where(r => r.UserId == userId && r.UsedOrderId.Equals(orderId)).FirstOrDefault();
            if (bounsRecord == null)
                throw new HimallException("用户领取的代金红包记录不存在");
            bounsRecord.UsedTime = null;
            bounsRecord.UsedOrderId = null;
            bounsRecord.State = ShopBonusReceiveInfo.ReceiveState.NotUse;
            DbFactory.Default.Update(bounsRecord);
        }

        /// <summary>
        ///根据退款金额计算出退款积分比率
        /// </summary>
        /// <param name="refundmode">退款实体</param>
        /// <param name="enablerefundmoney">允许退款金额</param>
        /// <returns></returns>
        private decimal ClacIntegralRatio(OrderRefundInfo refundmode, decimal enablerefundmoney, decimal enableintegraldiscount)
        {
            decimal actrefundmoney = refundmode.Amount;//获取实际退款金额
            var integraratio = enableintegraldiscount / (enablerefundmoney / actrefundmoney);
            return integraratio;
        }
        /// <summary>
        /// 验证某张平台券是否可退
        /// </summary>
        /// <param name="couponId"></param>
        /// <returns></returns>
        private bool VailidReturnPlateCoupon(long couponId, long userId, DateTime dt)
        {

            return DbFactory.Default.Get<OrderInfo>().Where(o => o.PlatCouponId == couponId && o.PlatDiscountAmount > 0 && o.OrderStatus != OrderInfo.OrderOperateStatus.Close && o.UserId == userId && o.OrderDate.Equals(dt)).Exist();
        }

        /// <summary>
        /// 退款时退还商家优惠券和平台优惠券
        /// </summary>
        /// <param name="orderId"></param>
        private void RefundCoupons(OrderInfo order)
        {
            //关闭订单退还商家券
            if (order.OrderStatus == OrderInfo.OrderOperateStatus.Close && order.CouponId > 0)
            {

                if (order.CouponType.HasValue)
                {
                    if (order.CouponType.Value == Entities.CouponType.Coupon)
                        ServiceProvider.Instance<CouponService>.Create.ReturnCoupon(order);// 退回优惠券
                    else
                        ReturnShopBonus(order.UserId, order.Id, order.CouponId);//退回带金红包
                }
            }

            //退还平台券
            if (order.OrderStatus == OrderInfo.OrderOperateStatus.Close && order.PlatCouponId > 0)
            {
                var otherorders = DbFactory.Default.Get<OrderInfo>().Where(o => o.MainOrderId == order.MainOrderId && o.OrderStatus != OrderInfo.OrderOperateStatus.Close).FirstOrDefault();//不存在售后的订单
                if (otherorders == null && !VailidReturnPlateCoupon(order.PlatCouponId, order.UserId, order.OrderDate))
                { //进行平台券的退还
                    var couponrecord = DbFactory.Default.Get<CouponRecordInfo>().Where(c => c.CouponId == order.PlatCouponId && c.UserId == order.UserId && c.OrderId == order.Id.ToString() && c.CounponStatus == CouponRecordInfo.CounponStatuses.Used).FirstOrDefault();
                    if (couponrecord != null)
                    {
                        couponrecord.CounponStatus = CouponRecordInfo.CounponStatuses.Unuse;
                        couponrecord.OrderId = "";
                        couponrecord.UsedTime = null;
                        DbFactory.Default.Update(couponrecord);
                    }
                }
            }
        }

        private void SaveSuccessMessage(OrderRefundInfo refund, string managerName, long orderId)
        {
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();


            //日志记录            
            if (string.IsNullOrEmpty(managerName))
                managerName = "系统";
            string strOperateContent = "确认退款/退货";
            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                strOperateContent = "虚拟商品自动确认退款/退货";

            OrderOperationLogInfo orderOperationLog = new OrderOperationLogInfo();
            orderOperationLog.Operator = managerName;
            orderOperationLog.OrderId = refund.OrderId;
            orderOperationLog.OperateDate = DateTime.Now;
            orderOperationLog.OperateContent = strOperateContent;

            DbFactory.Default.Add(orderOperationLog);

            //消息通知
            var orderMessage = new MessageOrderInfo();
            orderMessage.OrderId = order.Id.ToString();
            orderMessage.ShopId = order.ShopId;
            orderMessage.ShopName = order.ShopName;
            orderMessage.RefundMoney = refund.Amount;
            orderMessage.UserName = order.UserName;
            orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
            orderMessage.TotalMoney = order.OrderTotalAmount;
            orderMessage.ProductName = orderItem.ProductName;
            orderMessage.RefundTime = refund.ApplyDate;
            if (order.Platform == PlatformType.WeiXinSmallProg)
            {
                orderMessage.MsgOrderType = MessageOrderType.Applet;
            }
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnOrderRefund(order.UserId, orderMessage, refund.Id, refund.RefundPayType.ToDescription()));

            //发布退款成功消息
            //MessageQueue.PublishTopic(CommonConst.MESSAGEQUEUE_REFUNDSUCCESSED, refund.Id);
            try
            {
                if (OnRefundSuccessed != null)
                    OnRefundSuccessed(refund.Id);
            }
            catch
            {
                //Log.Error("OnRefundSuccessed=" + e.Message);
            }
            //退款日志
            AddRefundLog(refund.Id, refund.ApplyNumber, OrderRefundStep.Confirmed, refund.RefundStatus, managerName, refund.ManagerRemark);
        }


        /// <summary>
        /// 检查是否可以退款
        /// </summary>
        /// <param name="refundId"></param>
        /// <returns></returns>
        public bool HasMoneyToRefund(long refundId)
        {
            var model = DbFactory.Default.Get<OrderRefundInfo>().Where(a => a.Id == refundId).FirstOrDefault();
            var shopAccount = DbFactory.Default.Get<ShopAccountInfo>().Where(a => a.ShopId == model.ShopId).FirstOrDefault();
            var IsSettlement = DbFactory.Default.Get<AccountDetailInfo>().Where(a => a.OrderId == model.OrderId).FirstOrDefault();
            var result = true;
            if (IsSettlement != null && model.Amount > shopAccount.Balance)
            {
                return false;
            }
            return result;
        }

        /// <summary>
        /// 重新计算待结算
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="refundId"></param>
        /// <param name="verificationCodeIds"></param>
        /// <param name="isClose"></param>
        private void RefundSettlement(long orderId, long refundId, string verificationCodeIds, bool isClose)
        {
            //获取该订单详情
            var orderInfo = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId).FirstOrDefault();
            if (orderInfo == null)
            {
                return; //如果没有订单
            }
            //获取该订单下所有的退款
            var list = GetOrderRefundList(orderId);

            //根据订单号获取待结算的订单
            var model = DbFactory.Default.Get<PendingSettlementOrderInfo>().Where(a => a.OrderId == orderId).FirstOrDefault();
            decimal platCommissionReturn = 0;
            decimal distributorCommissionReturn = 0;
            decimal refundAmountTotal = 0; //总退款金额
            decimal discountAmountReturn = 0;//平台优惠券退款抵扣
            var refund = list.FirstOrDefault(a => a.Id == refundId);//单个项目退款
            if (!string.IsNullOrWhiteSpace(verificationCodeIds))
            {
                //有核销码
                refund = list.FirstOrDefault(e => e.VerificationCodeIds == verificationCodeIds);
            }

            var AccountNo = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + refund.Id;

            var orderItemInfos = DbFactory.Default.Get<OrderItemInfo>().Where(a => a.OrderId == orderId).ToList();

            var coupon = DbFactory.Default.Get<CouponInfo>().Where(c => c.Id == orderInfo.PlatCouponId).FirstOrDefault();

            if (model != null) //如果没结算，更新待结算订单
            {
                foreach (var m in list)
                {
                    platCommissionReturn += m.ReturnPlatCommission;
                    refundAmountTotal += m.Amount;
                }

                if (isClose)
                {
                    //退款、退货关闭订单时，更新完成时间
                    model.OrderFinshTime = DateTime.Now;
                }

                #region 平台优惠券计算
                if (coupon != null && orderInfo.PlatDiscountAmount > 0)
                {
                    var info = orderItemInfos.FirstOrDefault(o => o.Id == refund.OrderItemId);
                    if (info != null)
                    {
                        if (orderInfo.OrderType == OrderInfo.OrderTypes.Virtual)
                        {
                            discountAmountReturn = Math.Round(info.PlatCouponDiscount / info.Quantity * refund.ReturnQuantity, 2);
                        }
                        else
                        {
                            if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                            {
                                discountAmountReturn = orderInfo.PlatDiscountAmount;
                            }
                            else if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund)
                            {
                                //根据退货数量计算优惠券占比
                                discountAmountReturn = Math.Round(info.PlatCouponDiscount / info.Quantity * refund.ReturnQuantity, 2);
                            }
                            else if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderItemRefund)
                            {
                                //按照退款金额比例计算优惠券占比
                                discountAmountReturn = Math.Round((refund.Amount / info.EnabledRefundAmount.Value) * info.PlatCouponDiscount, 2);
                            }
                        }
                    }
                    else
                    {
                        discountAmountReturn = orderInfo.PlatDiscountAmount;
                    }
                }

                model.DiscountAmountReturn = model.DiscountAmountReturn + discountAmountReturn;
                if (model.DiscountAmount < model.DiscountAmountReturn)
                {
                    model.DiscountAmountReturn = model.DiscountAmount;
                }
                #endregion

                model.PlatCommissionReturn = platCommissionReturn;
                model.RefundAmount = refundAmountTotal;
                model.RefundDate = DateTime.Now;
                //平台佣金=平台佣金-退的平台佣金
                if (refund.ReturnPlatCommission > model.PlatCommission)//防止溢出，多次退款四舍五入的影响
                {
                    refund.ReturnPlatCommission = model.PlatCommission;
                    model.PlatCommission = 0;
                }
                else
                    model.PlatCommission -= refund.ReturnPlatCommission;


                if (orderInfo.RefundTotalAmount == orderInfo.TotalAmount && orderInfo.TotalAmount > 0)
                {
                    var returnAmount = list.Where(t => t.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed).Sum(t => t.Amount) + refund.Amount;//总共退款金额
                    var canReAmount = orderInfo.TotalAmount + orderInfo.IntegralDiscount;//是否金额和积分抵扣金额它是可以抵扣的；
                    if (returnAmount == canReAmount)
                    {
                        model.PlatCommission = 0;//说明可退金额已全退了，防止溢出，多次退款四舍五入的影响,这里设置为0
                    }
                }
                //最新的分销佣金（此部分的处理逻辑在此之前的分销那里已经处理）
                var distributorCommission_new = ServiceProvider.Instance<DistributionService>.Create.GetDistributionBrokerageAmount(orderId);

                //本次退还分销佣金=退之前分销佣金-最新的分销佣金
                distributorCommissionReturn = model.DistributorCommission - distributorCommission_new;
                model.DistributorCommissionReturn += distributorCommissionReturn;
                model.DistributorCommission = distributorCommission_new;

                //结算金额-本次退的金额+本次返回的平台佣金和分销佣金-本次平台优惠券退款抵扣
                model.SettlementAmount = model.SettlementAmount - refund.Amount + refund.ReturnPlatCommission + distributorCommissionReturn - discountAmountReturn;
                //归0处理
                model.SettlementAmount = model.SettlementAmount < 0 ? 0.00M : model.SettlementAmount;
                DbFactory.Default.Update(model);
            }
        }


        /// <summary>
        /// 修改店铺访问量
        /// </summary>
        /// <param name="refund"></param>
        /// <param name="payDate"></param>
        void UpdateShopVisti(OrderRefundInfo refund, DateTime payDate)
        {
            //退款不影响金额、数量
            //ShopVistiInfo shopVisti = Context.ShopVistiInfo.FindBy(
            //    item => item.ShopId == refund.ShopId && item.Date == payDate.Date).FirstOrDefault();
            //if (shopVisti != null)
            //{
            //    if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
            //    {
            //        //整笔退
            //        var orditemlist = Context.OrderItemInfo.Where(d => d.OrderId == refund.OrderId).ToList();
            //        foreach (var item in orditemlist)
            //        {
            //            shopVisti.SaleCounts -= item.Quantity;
            //        }
            //    }
            //    else
            //    {
            //        if (refund.IsReturn)
            //            shopVisti.SaleCounts -= refund.OrderItemInfo.ReturnQuantity;
            //    }

            //    shopVisti.SaleAmounts = shopVisti.SaleAmounts - refund.Amount;
            //    Context.SaveChanges();
            //}

        }
        /// <summary>
        /// 修改商品访问量
        /// </summary>
        /// <param name="refund"></param>
        /// <param name="payDate"></param>
        void UpdateProductVisti(OrderRefundInfo refund, DateTime payDate)
        {
            var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();
            var orderInfo = DbFactory.Default.Get<OrderInfo>(p => p.Id == refund.OrderId).FirstOrDefault();

            var product = new ProductInfo();
            var productVisti = new ProductVistiInfo();
            var _FightGroupService = ServiceProvider.Instance<FightGroupService>.Create;
            var fgord = _FightGroupService.GetFightGroupOrderStatusByOrderId(refund.OrderId);
            DbFactory.Default.InTransaction(() =>
            {
                //处理成交量
                if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                {
                    //整笔退
                    var orditemlist = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == refund.OrderId).ToList();
                    foreach (var item in orditemlist)
                    {
                        product = DbFactory.Default.Get<ProductInfo>().Where(d => d.Id == item.ProductId).FirstOrDefault();
                        if (product != null)
                        {
                            if (orderInfo != null)
                            {
                                if (orderInfo.OrderType != OrderInfo.OrderTypes.Virtual)
                                {
                                    product.SaleCounts -= item.Quantity;
                                }
                                else
                                {
                                    product.SaleCounts -= refund.ReturnQuantity;
                                }

                                var searchProduct = DbFactory.Default.Get<SearchProductInfo>().Where(r => r.ProductId == item.ProductId).FirstOrDefault();
                                if (searchProduct != null)
                                {
                                    if (orderInfo.OrderType != OrderInfo.OrderTypes.Virtual)
                                    {
                                        searchProduct.SaleCount -= (int)item.Quantity;
                                    }
                                    else
                                    {
                                        searchProduct.SaleCount -= (int)refund.ReturnQuantity;
                                    }
                                }
                                if (searchProduct.SaleCount < 0)
                                {
                                    searchProduct.SaleCount = 0;
                                }
                                if (product.SaleCounts < 0)
                                {
                                    product.SaleCounts = 0;
                                }
                                DbFactory.Default.Update(product);
                                DbFactory.Default.Update(searchProduct);
                            }
                        }


                        //退拼团库存
                        if (fgord != null)
                        {
                            _FightGroupService.UpdateActiveStock(fgord.ActiveId, item.SkuId, item.Quantity);
                        }
                        //productVisti = Context.ProductVistiInfo.FindBy(
                        //d => d.ProductId == item.ProductId && d.Date == payDate.Date).FirstOrDefault();

                        //if (null != productVisti)
                        //{
                        //    productVisti.SaleCounts -= orderItem.Quantity;
                        //    productVisti.SaleAmounts -= refund.Amount;
                        //}
                    }
                }
                else if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund)
                {
                    if (refund.IsReturn)
                    {
                        //判断是否有会员发货，没有会员发货视为弃货，不扣销量,不退库存
                        if (!string.IsNullOrEmpty(refund.ExpressCompanyName) && !string.IsNullOrEmpty(refund.ShipOrderNumber))
                        {
                            var productid = orderItem.ProductId;
                            product = DbFactory.Default.Get<ProductInfo>().Where(d => d.Id == productid).FirstOrDefault();
                            var returnQuantity = refund.ReturnQuantity;
                            if (product != null)
                            {
                                //product.SaleCounts -= refund.OrderItemInfo.ReturnQuantity;//这个时候事务还没提交，订单项表里的ReturnQuantity未更新，还是0
                                product.SaleCounts -= returnQuantity;
                                var searchProduct = DbFactory.Default.Get<SearchProductInfo>().Where(r => r.ProductId == product.Id).FirstOrDefault();
                                if (searchProduct != null)
                                    //searchProduct.SaleCount -= (int)refund.OrderItemInfo.ReturnQuantity;
                                    searchProduct.SaleCount -= (int)returnQuantity;
                                if (searchProduct.SaleCount < 0)
                                {
                                    searchProduct.SaleCount = 0;
                                }
                                if (product.SaleCounts < 0)
                                {
                                    product.SaleCounts = 0;
                                }
                                DbFactory.Default.Update(product);
                                DbFactory.Default.Update(searchProduct);
                            }

                            //productVisti = Context.ProductVistiInfo.FindBy(
                            //    item => item.ProductId == orderItem.ProductId && item.Date == payDate.Date).FirstOrDefault();

                            //if (null != productVisti)
                            //{
                            //    productVisti.SaleCounts -= orderItem.Quantity;
                            //    productVisti.SaleAmounts -= refund.Amount;
                            //}

                            //退拼团库存
                            if (fgord != null)
                            {
                                _FightGroupService.UpdateActiveStock(fgord.ActiveId, orderItem.SkuId, returnQuantity);
                            }
                        }
                    }
                }
            });
        }

        #endregion
        /// <summary>
        /// 退款处理
        /// </summary>
        /// <param name="refundId"></param>
        /// <param name="managerRemark"></param>
        /// <param name="managerName"></param>
        /// <param name="notifyurl"></param>
        /// <param name="isLine">true指定线下退款，false原来指定原路返回或余额退货线下退</param>
        /// <returns></returns>
        public string ConfirmRefund(long refundId, string managerRemark, string managerName, string notifyurl, bool isLine = false)
        {
            string result = "";
            //退款信息与状态
            var refund = DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.Id == refundId).FirstOrDefault();
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == refund.OrderId).FirstOrDefault();
            var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();
            var member = DbFactory.Default.Get<MemberInfo>().Where(p => p.Id == refund.UserId).FirstOrDefault();
            if (refund.ManagerConfirmStatus != OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm)
                throw new HimallException("只有未确认状态的退款/退货才能进行确认操作！");

            bool isRefundIntegral = false;
            var ret = DbFactory.Default.InTransaction(() =>
            {
                if (isLine)
                {
                    result = "";
                    RefundBackOffLine(refund);//指定线下退
                    managerRemark = managerRemark + "(原" + refund.RefundPayType.ToDescription() + "转线下退款)";
                    refund.RefundPayType = OrderRefundInfo.OrderRefundPayType.OffLine;
                }
                else
                {
                    switch (refund.RefundPayType)
                    {
                        case OrderRefundInfo.OrderRefundPayType.BackOut:
                            if (!string.IsNullOrEmpty(order.GatewayOrderId))
                            {
                                result = RefundBackOutGroup(refund, notifyurl, out isRefundIntegral);
                            }
                            else
                            {
                                RefundBackCapital(refund);
                            }
                            break;
                        case OrderRefundInfo.OrderRefundPayType.BackCapital:
                            result = "";
                            RefundBackCapital(refund);
                            break;
                        case OrderRefundInfo.OrderRefundPayType.OffLine:
                            result = "";
                            RefundBackOffLine(refund);
                            break;
                    }
                }
                refund.ManagerRemark = managerRemark;
                DbFactory.Default.Update(refund);

                if (refund.RefundPayStatus == OrderRefundInfo.OrderRefundPayStatus.PaySuccess)
                {
                    RefundSuccessed(refund, managerName, isRefundIntegral);
                }

            }, failedAction: (ex) =>
            {
                throw ex;
            });
            if (ret)
            {
                SaveSuccessMessage(refund, managerName, order.Id);
            }
            #region 退款失败发送消息
            if (!string.IsNullOrWhiteSpace(result))
            {
                if (order != null)
                {
                    //消息通知
                    var orderMessage = new MessageOrderInfo();
                    orderMessage.UserName = order.UserName;
                    orderMessage.OrderId = order.Id.ToString();
                    orderMessage.ShopId = order.ShopId;
                    orderMessage.ShopName = order.ShopName;
                    orderMessage.RefundMoney = refund.Amount;
                    orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                    orderMessage.TotalMoney = order.OrderTotalAmount;
                    orderMessage.ProductName = orderItem.ProductName;
                    orderMessage.RefundAuditTime = DateTime.Now;
                    orderMessage.Remark = string.IsNullOrEmpty(refund.SellerRemark) ? "退款失败" : refund.SellerRemark;
                    if (order.Platform == PlatformType.WeiXinSmallProg)
                    {
                        orderMessage.MsgOrderType = MessageOrderType.Applet;
                    }
                    Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnOrderRefundFail(order.UserId, orderMessage, refund.RefundMode.GetHashCode(), refund.Id));
                }
            }
            #endregion
            return result;
        }
        /// <summary>
        /// 异步通知确认退款
        /// </summary>
        /// <param name="batchno"></param>
        public void NotifyRefund(string batchNo)
        {
            if (string.IsNullOrWhiteSpace(batchNo))
            {
                throw new HimallException("错误的批次号");
            }

            OrderRefundInfo refund = DbFactory.Default.Get<OrderRefundInfo>().Where(d => d.RefundBatchNo == batchNo).FirstOrDefault();
            if (refund != null)
            {
                refund.RefundPayStatus = OrderRefundInfo.OrderRefundPayStatus.PaySuccess;
                var ret = RefundSuccessed(refund, "系统异步退款");
                if (ret)
                {
                    SaveSuccessMessage(refund, "系统异步退款", refund.OrderId);
                }
            }
        }
        /// <summary>
        /// 商家审核
        /// </summary>
        /// <param name="id"></param>
        /// <param name="auditStatus"></param>
        /// <param name="sellerRemark"></param>
        /// <param name="sellerName"></param>
        public void SellerDealRefund(long id, OrderRefundInfo.OrderRefundAuditStatus auditStatus, string sellerRemark, string sellerName)
        {
            OrderRefundInfo refund = DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.Id == id).FirstOrDefault();
            if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.UnAudit)
            {
                if (refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.WaitAudit
                    && refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery   //自动任务
                    && refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving  //商家收到的货有问题
                    )
                    throw new HimallException("只有待审核状态的退款/退货才能进行处理，自动任务时需要状态为待买家寄货");
            }
            else
            {
                if (refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                    throw new HimallException("只有待审核状态的退款/退货才能进行处理");
            }
            if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
            {
                //订单退款无需发货
                if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery)
                {
                    //直接转换为商家审核通过
                    auditStatus = OrderRefundInfo.OrderRefundAuditStatus.Audited;
                    // ServiceProvider.Instance<OrderService>.Create.AgreeToRefundBySeller(refund.OrderId);        //关闭订单
                }
            }
            else
            {
                if (refund.IsReturn == false)
                {
                    if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery)
                    {
                        //直接转换为商家审核通过
                        auditStatus = OrderRefundInfo.OrderRefundAuditStatus.Audited;
                    }
                }
            }


            if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery && !refund.IsReturn)
                refund.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.Audited;
            else
                refund.SellerAuditStatus = auditStatus;

            refund.SellerAuditDate = DateTime.Now;
            refund.SellerRemark = sellerRemark;
            if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.Audited)
                refund.ManagerConfirmDate = DateTime.Now;

            DbFactory.Default.Update(refund);

            OrderOperationLogInfo orderOperationLog = new OrderOperationLogInfo();
            orderOperationLog.Operator = sellerName;
            orderOperationLog.OrderId = refund.OrderId;
            orderOperationLog.OperateDate = DateTime.Now;
            orderOperationLog.OperateContent = "商家处理退款退货申请";

            DbFactory.Default.Add(orderOperationLog);

            var stepMap = new Dictionary<OrderRefundInfo.OrderRefundAuditStatus, OrderRefundStep>();
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.Audited, OrderRefundStep.UnConfirm);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.UnAudit, OrderRefundStep.UnAudit);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.WaitAudit, OrderRefundStep.WaitAudit);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery, OrderRefundStep.WaitDelivery);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving, OrderRefundStep.WaitReceiving);

            //退款日志
            AddRefundLog(refund.Id, refund.ApplyNumber, stepMap[auditStatus], refund.RefundStatus, sellerName, refund.SellerRemark);

            #region 发送售后发货消息
            if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery)
            {
                if (refund != null)
                {
                    var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == refund.OrderId).FirstOrDefault();
                    var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();
                    if (order != null)
                    {
                        //消息通知
                        var orderMessage = new MessageOrderInfo();
                        orderMessage.UserName = order.UserName;
                        orderMessage.OrderId = order.Id.ToString();
                        orderMessage.ShopId = order.ShopId;
                        orderMessage.ShopName = order.ShopName;
                        orderMessage.RefundMoney = refund.Amount;
                        orderMessage.RefundQuantity = refund.ReturnQuantity;
                        orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                        orderMessage.TotalMoney = order.OrderTotalAmount;
                        orderMessage.ProductName = orderItem.ProductName;
                        orderMessage.RefundAuditTime = DateTime.Now;
                        orderMessage.Remark = string.IsNullOrWhiteSpace(sellerRemark) ? "请及时登录系统确认寄货并填写快递信息" : sellerRemark;
                        if (order.Platform == PlatformType.WeiXinSmallProg)
                        {
                            orderMessage.MsgOrderType = MessageOrderType.Applet;
                        }
                        Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnRefundDeliver(order.UserId, orderMessage, refund.Id));
                    }
                }
            }
            #endregion
            #region 拒绝退款后发送消息
            if (auditStatus == OrderRefundInfo.OrderRefundAuditStatus.UnAudit)
            {
                if (refund != null)
                {
                    Core.Log.Info("[模板消息]ConfirmRefund----");
                    var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == refund.OrderId).FirstOrDefault();
                    var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == refund.OrderItemId).FirstOrDefault();
                    if (order != null)
                    {
                        //消息通知
                        var orderMessage = new MessageOrderInfo();
                        orderMessage.UserName = order.UserName;
                        orderMessage.OrderId = order.Id.ToString();
                        orderMessage.ShopId = order.ShopId;
                        orderMessage.ShopName = order.ShopName;
                        orderMessage.RefundMoney = refund.Amount;
                        orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                        orderMessage.TotalMoney = order.OrderTotalAmount;
                        orderMessage.ProductName = orderItem.ProductName;
                        orderMessage.RefundAuditTime = DateTime.Now;
                        orderMessage.Remark = string.IsNullOrWhiteSpace(refund.SellerRemark) ? "商家拒绝退款" : refund.SellerRemark;
                        if (order.Platform == PlatformType.WeiXinSmallProg)
                        {
                            orderMessage.MsgOrderType = MessageOrderType.Applet;
                        }
                        Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnOrderRefundFail(order.UserId, orderMessage, refund.RefundMode.GetHashCode(), refund.Id));
                    }
                }
            }
            #endregion
        }
        /// <summary>
        /// 商家确认到货
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sellerName"></param>
        public void SellerConfirmRefundGood(long id, string sellerName, string remark = "")
        {
            OrderRefundInfo refund = DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.Id == id).FirstOrDefault();
            if (refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving)
                throw new HimallException("只有待收货状态的退货才能进行确认收货操作");
            refund.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.Audited;
            refund.SellerConfirmArrivalDate = DateTime.Now;
            refund.ManagerConfirmDate = DateTime.Now;
            if (!string.IsNullOrEmpty(remark))
                refund.ManagerRemark = remark;
            DbFactory.Default.Update(refund);

            OrderOperationLogInfo orderOperationLog = new OrderOperationLogInfo();
            orderOperationLog.Operator = sellerName;
            orderOperationLog.OrderId = refund.OrderId;
            orderOperationLog.OperateDate = DateTime.Now;
            orderOperationLog.OperateContent = "商家确认收到退货";
            DbFactory.Default.Add(orderOperationLog);

            if (refund.OrderItemId > 0 && refund.ReturnQuantity > 0)
            {
                var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == refund.OrderId).FirstOrDefault();
                var orderItem = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.Id == refund.OrderItemId).FirstOrDefault();
                if (order != null && orderItem != null)
                {
                    ReturnStock(orderItem, order, refund.ReturnQuantity);
                    // 限购还原活动库存
                    if (order.OrderType == OrderInfo.OrderTypes.LimitBuy)
                    {
                        var flashSaleDetailInfo = DbFactory.Default.Get<FlashSaleDetailInfo>().Where(a => a.SkuId == orderItem.SkuId && a.FlashSaleId == orderItem.FlashSaleId).FirstOrDefault();
                        if (flashSaleDetailInfo != null)
                        {
                            flashSaleDetailInfo.TotalCount += (int)refund.ReturnQuantity;
                            DbFactory.Default.Update(flashSaleDetailInfo);
                        }
                    }
                }
            }

            //退款日志
            AddRefundLog(refund.Id, refund.ApplyNumber, OrderRefundStep.UnConfirm, refund.RefundStatus, sellerName, refund.ManagerRemark);
        }
        /// <summary>
        /// 用户发货
        /// </summary>
        /// <param name="id"></param>
        /// <param name="sellerName"></param>
        /// <param name="expressCompanyName"></param>
        /// <param name="shipOrderNumber"></param>
        public void UserConfirmRefundGood(long id, string sellerName, string expressCompanyName, string shipOrderNumber)
        {
            OrderRefundInfo refund = DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.Id == id).FirstOrDefault();
            if (refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery)
                throw new HimallException("只有待等待发货状态的能进行发货操作");
            refund.ShipOrderNumber = shipOrderNumber;
            refund.ExpressCompanyName = expressCompanyName;
            refund.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving;
            refund.BuyerDeliverDate = DateTime.Now;
            DbFactory.Default.Update(refund);

            OrderOperationLogInfo orderOperationLog = new OrderOperationLogInfo();
            orderOperationLog.Operator = sellerName;
            orderOperationLog.OrderId = refund.OrderId;
            orderOperationLog.OperateDate = DateTime.Now;
            orderOperationLog.OperateContent = "买家确认发回商品";
            DbFactory.Default.Add(orderOperationLog);
            //退款日志
            AddRefundLog(refund.Id, refund.ApplyNumber, OrderRefundStep.WaitReceiving, refund.RefundStatus, sellerName, refund.ExpressCompanyName + "：" + refund.ShipOrderNumber);
            //发送售后消息
            var order = ServiceProvider.Instance<OrderService>.Create.GetOrder(refund.OrderId, refund.UserId);
            SendRefundAppMessage(refund, order);


        }

        public List<OrderRefundInfo> GetAllOrderRefunds()
        {
            return DbFactory.Default.Get<OrderRefundInfo>().ToList();
        }


        /// <summary>
        /// 根据订单ID获取退款成功的列表
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        public List<OrderRefundInfo> GetOrderRefundList(long orderId)
        {
            var list = DbFactory.Default.Get<OrderRefundInfo>().Where(a => a.OrderId == orderId && a.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed).OrderByDescending(a => a.Id).ToList();
            return list;
        }
        /// <summary>
        /// 申请退款
        /// </summary>
        /// <param name="info"></param>
        public void AddOrderRefund(OrderRefundInfo info)
        {
            var ordser = ServiceProvider.Instance<OrderService>.Create;
            var _FightGroupService = ServiceProvider.Instance<FightGroupService>.Create;
            var order = ordser.GetOrder(info.OrderId, info.UserId);
            if (order == null)
                throw new Himall.Core.HimallException("该订单已删除或不属于该用户");

            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
            {
                var orderVerificationCodes = Himall.ServiceProvider.Instance<OrderService>.Create.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { order.Id });
                long num = orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification).Count();
                if (num == 0)
                {
                    throw new Himall.Core.HimallException("该商品没有可退的核销码");
                }

                var orderitem = Himall.ServiceProvider.Instance<OrderService>.Create.GetOrderItem(info.OrderItemId);
                if (orderitem == null)
                    throw new Himall.Core.HimallException("该订单条目已删除或不属于该用户");

                if (info.Amount > (orderitem.EnabledRefundAmount - orderitem.RefundPrice))
                    throw new Himall.Core.HimallException("退款金额不能超过订单的可退金额");

                if (info.ReturnQuantity > (orderitem.Quantity - orderitem.ReturnQuantity))
                    throw new Himall.Core.HimallException("退货数量不可以超出可退数量");

                //虚拟订单退款不需要平台和商家操作，直接自动审核并退款；如果退款出现异常，可在平台后台手动操作退款
                info.ShopId = order.ShopId;
                info.ShopName = order.ShopName;
                info.RefundMode = OrderRefundInfo.OrderRefundMode.OrderRefund;
                info.IsReturn = false;
                info.SellerAuditDate = DateTime.Now;
                info.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.Audited;
                info.ManagerConfirmDate = DateTime.Now;
                info.ManagerConfirmStatus = OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm;
                info.OrderItemId = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == info.OrderId).FirstOrDefault().Id;
                info.ApplyNumber = 1;

                #region 处理佣金
                SetCommission(info, order.OrderType);
                #endregion

                DbFactory.Default.Add(info);
                ordser.UpdateOrderVerificationCodeStatusByCodes(info.VerificationCodeIds.Split(',').ToList(), order.Id, OrderInfo.VerificationCodeStatus.Refund, null);

                #region 处理消息和日志
                SendMessage(info, order);
                #endregion

            }
            else
            {
                if ((int)order.OrderStatus < 2)
                    throw new Himall.Core.HimallException("错误的售后申请,订单状态有误");
                info.ShopId = order.ShopId;
                info.ShopName = order.ShopName;

                if (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || order.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                {
                    info.RefundMode = OrderRefundInfo.OrderRefundMode.OrderRefund;
                    info.ReturnQuantity = 0;
                }
                //售后时间限制
                if (ordser.IsRefundTimeOut(info.OrderId))
                {
                    throw new Himall.Core.HimallException("订单已超过售后期");
                }
                if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
                {
                    var fgord = _FightGroupService.GetFightGroupOrderStatusByOrderId(order.Id);
                    if (!fgord.CanRefund)
                    {
                        throw new Himall.Core.HimallException("拼团订单处于不可售后状态");
                    }
                }
                if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.Finish)
                {
                    throw new Himall.Core.HimallException("货到付款订单未完成前不可售后");
                }
                var orderitem = Himall.ServiceProvider.Instance<OrderService>.Create.GetOrderItem(info.OrderItemId);
                if (orderitem == null && info.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund)
                    throw new Himall.Core.HimallException("该订单条目已删除或不属于该用户");
                if (info.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                {
                    if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                        throw new Himall.Core.HimallException("错误的订单退款申请,订单状态有误");
                    info.IsReturn = false;
                    info.ReturnQuantity = 0;
                    if (info.Amount > order.OrderEnabledRefundAmount)
                        throw new Himall.Core.HimallException("退款金额不能超过订单的实际支付金额");
                }
                else
                {
                    if (info.Amount > (orderitem.EnabledRefundAmount - orderitem.RefundPrice))
                        throw new Himall.Core.HimallException("退款金额不能超过订单的可退金额");
                    if (info.ReturnQuantity > (orderitem.Quantity - orderitem.ReturnQuantity))
                        throw new Himall.Core.HimallException("退货数量不可以超出可退数量");
                }
                if (info.ReturnQuantity < 0)
                    throw new Himall.Core.HimallException("错误的退货数量");
                bool isOrderRefund = false;    //是否整笔订单退款
                if (info.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
                {
                    isOrderRefund = true;
                }

                var isCanApply = CanApplyRefund(info.OrderId, info.OrderItemId, isOrderRefund);

                if (!isCanApply)
                    throw new Himall.Core.HimallException("您已申请过售后，不可重复申请");
                if (!isOrderRefund)
                {
                    if (info.ReturnQuantity > 0)
                    {
                        info.RefundMode = OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund;
                    }
                    else
                    {
                        info.RefundMode = OrderRefundInfo.OrderRefundMode.OrderItemRefund;
                    }
                }
                info.SellerAuditDate = DateTime.Now;
                info.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.WaitAudit;
                info.ManagerConfirmDate = DateTime.Now;
                info.ManagerConfirmStatus = OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm;
                if (isOrderRefund == true)
                {
                    info.OrderItemId = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == info.OrderId).FirstOrDefault().Id;
                }

                var orditemlist = new List<OrderItemInfo>();
                //订单退款、退货都要计算退款佣金
                var model = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.Id == info.OrderItemId).FirstOrDefault();
                //计算佣金
                SetCommission(info, order.OrderType);

                if (!isOrderRefund)
                {
                    if (info.RefundMode == OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund)
                    {
                        if (info.ReturnQuantity <= 0 || info.ReturnQuantity > (model.Quantity - model.ReturnQuantity))
                            info.ReturnQuantity = model.Quantity - model.ReturnQuantity;
                    }
                    else
                        info.ReturnQuantity = 0;
                }
                else
                {
                    info.ReturnQuantity = 0;
                }

                info.ApplyNumber = 1;

                DbFactory.Default.Add(info);

                var user = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == info.UserId).FirstOrDefault();
                var reason = info.Reason;
                if (!string.IsNullOrEmpty(info.ReasonDetail))
                    reason += ":" + info.ReasonDetail;
                //退款日志
                AddRefundLog(info.Id, info.ApplyNumber, OrderRefundStep.WaitAudit, info.RefundStatus, user.UserName, reason);


                //新增小程序推送Form数据
                if (!string.IsNullOrEmpty(info.formId))
                {
                    WXAppletFormDataInfo wxInfo = new WXAppletFormDataInfo();
                    wxInfo.EventId = Convert.ToInt64(MessageTypeEnum.OrderRefundSuccess);
                    wxInfo.EventTime = DateTime.Now;
                    wxInfo.EventValue = info.OrderId.ToString();
                    wxInfo.ExpireTime = DateTime.Now.AddDays(7);
                    wxInfo.FormId = info.formId;
                    ServiceProvider.Instance<WXMsgTemplateService>.Create.AddWXAppletFromData(wxInfo);
                }

                //发送售后消息
                SendRefundAppMessage(info, order);

                var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == info.OrderItemId).FirstOrDefault();
                //新增微信短信邮件消息推送
                var orderMessage = new MessageOrderInfo();
                orderMessage.UserName = order.UserName;
                orderMessage.OrderId = order.Id.ToString();
                orderMessage.ShopId = order.ShopId;
                orderMessage.ShopName = order.ShopName;
                orderMessage.RefundMoney = info.Amount;
                orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                orderMessage.TotalMoney = order.OrderTotalAmount;
                orderMessage.ProductName = orderItem.ProductName;
                orderMessage.RefundAuditTime = DateTime.Now;
                if (order.Platform == PlatformType.WeiXinSmallProg)
                {
                    orderMessage.MsgOrderType = MessageOrderType.Applet;
                }
                Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnRefundApply(order.UserId, orderMessage, info.RefundMode.GetHashCode(), info.Id));

            }
        }
        /// <summary>
        /// 计算佣金
        /// </summary>
        /// <param name="info"></param>
        /// <param name="ordertype"></param>
        private static void SetCommission(OrderRefundInfo info, OrderInfo.OrderTypes ordertype)
        {
            var returnPlatCommission = 0.00M;
            var order = DbFactory.Default.Get<OrderInfo>().Where(o => o.Id == info.OrderId).FirstOrDefault();
            var isPlatConpon = false;//订单是否使用平台优惠券
            if (order != null)
            {
                var coupon = DbFactory.Default.Get<CouponInfo>().Where(c => c.Id == order.PlatCouponId).FirstOrDefault();
                if (coupon != null && coupon.ShopId == 0 && order.PlatDiscountAmount > 0)
                {
                    isPlatConpon = true;
                }
            }

            if (info.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
            {
                var itemlist = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == info.OrderId).ToList();
                foreach (var c in itemlist)
                {
                    decimal refundPrice = c.RealTotalPrice;
                    if (ordertype == OrderInfo.OrderTypes.Virtual)
                    {
                        refundPrice = info.Amount;
                    }
                    returnPlatCommission += calcReturnCommission((refundPrice + c.PlatCouponDiscount), c.CommisRate);
                }
            }
            else
            {
                var model = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.Id == info.OrderItemId).FirstOrDefault();
                decimal itemRealTotalMoney = model.RealTotalPrice;  //实付金额
                if ((model.Quantity - model.ReturnQuantity) < info.ReturnQuantity || (itemRealTotalMoney - model.RefundPrice) < info.Amount)
                {
                    throw new HimallException("退货和退款数量不能超过订单的实际数量和金额！");
                }
                if (model.CommisRate > 0 && info.Amount > 0)
                {
                    var platdiscount = (model.RealTotalPrice + model.PlatCouponDiscount) * model.CommisRate;//计算出平台佣金总额  117.6/(1176/588)
                    returnPlatCommission = Core.Helper.CommonHelper.SubDecimal(platdiscount / (model.RealTotalPrice / info.Amount), 2);//按退款金额比率计算出平台应该退还的佣金

                }
            }

            //if (ordertype != OrderInfo.OrderTypes.Virtual)
            //{
            //    //非虚拟 订单
            //    if (info.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
            //    {
            //        var itemlist = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == info.OrderId).ToList();
            //        foreach (var c in itemlist)
            //        {
            //            decimal refundPrice = c.RealTotalPrice;
            //            returnPlatCommission += calcReturnCommission(refundPrice, c.CommisRate);
            //        }
            //    }
            //    else
            //    {
            //        var model = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.Id == info.OrderItemId).FirstOrDefault();
            //        decimal itemRealTotalMoney = model.RealTotalPrice;  //实付金额
            //        if ((model.Quantity - model.ReturnQuantity) < info.ReturnQuantity || (itemRealTotalMoney - model.RefundPrice) < info.Amount)
            //        {
            //            throw new HimallException("退货和退款数量不能超过订单的实际数量和金额！");
            //        }
            //        if (model.CommisRate > 0)
            //        {
            //            var platdiscount = (model.RealTotalPrice + model.PlatCouponDiscount) * model.CommisRate;//计算出平台佣金总额
            //            returnPlatCommission = Core.Helper.CommonHelper.SubDecimal(platdiscount / (model.RealTotalPrice / info.Amount), 2);//按退款金额比率计算出平台应该退还的佣金

            //        }
            //    }
            //}
            //else
            //{
            //    //虚拟订单退还佣金计算
            //    var model = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.Id == info.OrderItemId).FirstOrDefault();
            //    returnPlatCommission = calcReturnCommission(info.Amount, model.CommisRate);
            //    if (isPlatConpon)
            //    {
            //        decimal refundAmount = Math.Round((model.RealTotalPrice) * model.CommisRate / model.Quantity * info.ReturnQuantity, 2, MidpointRounding.AwayFromZero);
            //        returnPlatCommission = refundAmount;
            //    }
            //    else
            //    {
            //        decimal refundAmount = Math.Round((model.RealTotalPrice) * model.CommisRate / model.Quantity * info.ReturnQuantity, 2, MidpointRounding.AwayFromZero);
            //        returnPlatCommission = refundAmount;
            //    }
            //}
            // 加上对退款表的维护
            info.ReturnPlatCommission = returnPlatCommission;
        }

        private void SendMessage(OrderRefundInfo info, OrderInfo order)
        {
            var user = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == info.UserId).FirstOrDefault();
            var reason = info.Reason;
            if (!string.IsNullOrEmpty(info.ReasonDetail))
                reason += ":" + info.ReasonDetail;
            //退款日志
            AddRefundLog(info.Id, info.ApplyNumber, OrderRefundStep.WaitAudit, info.RefundStatus, user.UserName, reason);


            //新增小程序推送Form数据
            if (!string.IsNullOrEmpty(info.formId))
            {
                WXAppletFormDataInfo wxInfo = new WXAppletFormDataInfo();
                wxInfo.EventId = Convert.ToInt64(MessageTypeEnum.OrderRefundSuccess);
                wxInfo.EventTime = DateTime.Now;
                wxInfo.EventValue = info.OrderId.ToString();
                wxInfo.ExpireTime = DateTime.Now.AddDays(7);
                wxInfo.FormId = info.formId;
                ServiceProvider.Instance<WXMsgTemplateService>.Create.AddWXAppletFromData(wxInfo);
            }

            if (order.OrderType != OrderInfo.OrderTypes.Virtual)
            {
                //发送售后消息
                SendRefundAppMessage(info, order);
            }

            var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.Id == info.OrderItemId).FirstOrDefault();
            //新增微信短信邮件消息推送
            var orderMessage = new MessageOrderInfo();
            orderMessage.UserName = order.UserName;
            orderMessage.OrderId = order.Id.ToString();
            orderMessage.ShopId = order.ShopId;
            orderMessage.ShopName = order.ShopName;
            orderMessage.RefundMoney = info.Amount;
            orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
            orderMessage.TotalMoney = order.OrderTotalAmount;
            orderMessage.ProductName = orderItem.ProductName;
            orderMessage.RefundAuditTime = DateTime.Now;
            if (order.Platform == PlatformType.WeiXinSmallProg)
            {
                orderMessage.MsgOrderType = MessageOrderType.Applet;
            }
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnRefundApply(order.UserId, orderMessage, info.RefundMode.GetHashCode(), info.Id));
        }
        /// <summary>
        /// 计算退款佣金
        /// </summary>
        /// <param name="refundAmount"></param>
        /// <param name="commRate"></param>
        /// <returns></returns>
        static decimal calcReturnCommission(decimal refundAmount, decimal commRate)
        {
            return Core.Helper.CommonHelper.SubDecimal(refundAmount * commRate, 2);
        }
        /// <summary>
        /// 激活售后
        /// </summary>
        /// <param name="info"></param>
        public void ActiveRefund(OrderRefundInfo info)
        {
            var order = ServiceProvider.Instance<OrderService>.Create.GetOrder(info.OrderId, info.UserId);
            var refund = DbFactory.Default.Get<OrderRefundInfo>().Where(d => d.Id == info.Id).FirstOrDefault();
            if (refund == null)
            {
                throw new HimallException("错误的售后记录");
            }
            if (refund.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit && refund.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm)
            {
                throw new HimallException("您已提交过申请，请不要频繁操作");
            }
            if (refund.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.UnAudit)
            {
                throw new HimallException("售后记录状态有误，不可激活");
            }

            //info数据值转换给refund
            refund.Applicant = info.Applicant;
            refund.ContactPerson = info.ContactPerson;
            refund.ContactCellPhone = info.ContactCellPhone;
            refund.RefundAccount = info.RefundAccount;
            refund.ApplyDate = info.ApplyDate;
            refund.Amount = info.Amount;
            refund.Reason = info.Reason;
            refund.SellerAuditStatus = info.SellerAuditStatus;
            refund.SellerAuditDate = info.SellerAuditDate;
            //refund.SellerRemark = info.SellerRemark;
            refund.ManagerConfirmStatus = info.ManagerConfirmStatus;
            refund.ManagerConfirmDate = info.ManagerConfirmDate;
            //refund.ManagerRemark = info.ManagerRemark;
            refund.IsReturn = info.IsReturn;
            refund.ExpressCompanyName = info.ExpressCompanyName;
            refund.ShipOrderNumber = info.ShipOrderNumber;
            refund.Payee = info.Payee;
            refund.PayeeAccount = info.PayeeAccount;
            refund.RefundPayStatus = info.RefundPayStatus;
            refund.RefundPayType = info.RefundPayType;
            refund.BuyerDeliverDate = info.BuyerDeliverDate;
            refund.SellerConfirmArrivalDate = info.SellerConfirmArrivalDate;
            refund.RefundBatchNo = info.RefundBatchNo;
            refund.RefundPostTime = info.RefundPostTime;
            refund.ReturnQuantity = info.ReturnQuantity;
            if (!string.IsNullOrEmpty(info.ReasonDetail))
                refund.ReasonDetail = info.ReasonDetail;
            refund.CertPic1 = info.CertPic1;
            refund.CertPic2 = info.CertPic2;
            refund.CertPic3 = info.CertPic3;
            if (refund.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund)
            {
                refund.RefundMode = info.RefundMode;
            }

            bool isOrderRefund = false;
            if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund)
            {
                isOrderRefund = true;
            }

            if (!isOrderRefund)
            {
                if (refund.ReturnQuantity > 0)
                {
                    refund.RefundMode = OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund;
                }
                else
                {
                    refund.RefundMode = OrderRefundInfo.OrderRefundMode.OrderItemRefund;
                }
            }
            refund.SellerAuditDate = DateTime.Now;
            refund.SellerAuditStatus = OrderRefundInfo.OrderRefundAuditStatus.WaitAudit;
            refund.ManagerConfirmDate = DateTime.Now;
            refund.ManagerConfirmStatus = OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm;
            if (isOrderRefund == true)
            {
                refund.OrderItemId = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId == refund.OrderId).FirstOrDefault().Id;
            }


            List<OrderItemInfo> orditemlist = new List<OrderItemInfo>();
            if (!isOrderRefund)
            {
                var model = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.Id == refund.OrderItemId).FirstOrDefault();
                decimal itemRealTotalMoney = model.RealTotalPrice;   //实付金额
                if ((model.Quantity - model.ReturnQuantity) < refund.ReturnQuantity || (itemRealTotalMoney - model.RefundPrice) < refund.Amount)
                {
                    throw new HimallException("退货和退款数量不能超过订单的实际数量和金额！");
                }

                if (model.CommisRate > 0)
                {
                    //计算退还佣金
                    decimal unitPrice = Math.Round((itemRealTotalMoney / model.Quantity), 2);
                    int rnum = 0;
                    if (unitPrice > 0)
                        rnum = (int)Math.Ceiling(refund.Amount / unitPrice);
                    decimal refundPrice = (unitPrice * rnum);
                    if (refundPrice > itemRealTotalMoney)
                    {
                        refundPrice = itemRealTotalMoney;
                    }
                    var returnPlatCommission = calcReturnCommission(refundPrice, model.CommisRate);
                    // 加上对退款表的维护
                    refund.ReturnPlatCommission = returnPlatCommission;
                }

                if (refund.RefundMode == OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund)
                {
                    if (refund.ReturnQuantity <= 0 || refund.ReturnQuantity > (model.Quantity - model.ReturnQuantity))
                        refund.ReturnQuantity = model.Quantity - model.ReturnQuantity;
                }
                else
                    refund.ReturnQuantity = 0;
            }
            else
            {
                refund.ReturnQuantity = 0;
            }

            if (refund.ApplyNumber == null)
            {
                refund.ApplyNumber = 1;
            }
            refund.ApplyNumber += 1;

            DbFactory.Default.Update(refund);

            var user = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == refund.UserId).FirstOrDefault();

            var reason = info.Reason;
            if (!string.IsNullOrEmpty(info.ReasonDetail))
                reason += ":" + info.ReasonDetail;
            //退款日志
            AddRefundLog(refund.Id, refund.ApplyNumber, OrderRefundStep.WaitAudit, refund.RefundStatus, user.UserName, reason);


            //新增小程序推送Form数据
            if (!string.IsNullOrEmpty(info.formId))
            {
                WXAppletFormDataInfo wxInfo = new WXAppletFormDataInfo();
                wxInfo.EventId = Convert.ToInt64(MessageTypeEnum.OrderRefundSuccess);
                wxInfo.EventTime = DateTime.Now;
                wxInfo.EventValue = info.OrderId.ToString();
                wxInfo.ExpireTime = DateTime.Now.AddDays(7);
                wxInfo.FormId = info.formId;
                ServiceProvider.Instance<WXMsgTemplateService>.Create.AddWXAppletFromData(wxInfo);
            }

            //发送售后消息
            SendRefundAppMessage(info, order);
        }



        /// <summary>
        /// 通过订单编号获取整笔退款
        /// </summary>
        /// <param name="id">订单编号</param>
        /// <returns></returns>
        public OrderRefundInfo GetOrderRefundByOrderId(long id)
        {
            return DbFactory.Default.Get<OrderRefundInfo>().Where(a => a.OrderId == id && a.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund).FirstOrDefault();
        }


        public List<OrderRefundInfo> GetOrderRefundsByOrder(IEnumerable<long> orders)
        {
            return DbFactory.Default.Get<OrderRefundInfo>(p => p.OrderId.ExIn(orders)).ToList();
        }
        public List<OrderRefundInfo> GetOrderRefundsByOrder(long order)
        {
            return DbFactory.Default.Get<OrderRefundInfo>(p => p.OrderId == order).ToList();
        }

        /// <summary>
        /// 获取待平台确认退款的售后
        /// </summary>
        /// <param name="orders"></param>
        /// <returns></returns>
        public List<OrderRefundInfo> GetOrderRefundsByOrderPlatformConfirm(IEnumerable<long> orderIds)
        {
            return DbFactory.Default.Get<OrderRefundInfo>(p => p.OrderId.ExIn(orderIds) && p.SellerAuditStatus == OrderRefundAuditStatus.Audited && p.ManagerConfirmStatus == OrderRefundConfirmStatus.UnConfirm).ToList();
        }

        public OrderRefundInfo GetOrderRefund(long id, long? userId = null, long? shopId = null)
        {
            var model = DbFactory.Default.Get<OrderRefundInfo>().Where(a => a.Id == id).FirstOrDefault();

            if (model == null || userId.HasValue && userId.Value != model.UserId || shopId.HasValue && shopId.Value != model.ShopId)
                return null;
            return model;
        }

        public OrderRefundInfo GetOrderRefundById(long id)
        {
            return DbFactory.Default.Get<OrderRefundInfo>().Where(a => a.OrderId == id).FirstOrDefault();
        }
        /// <summary>
        /// 是否可以申请退款
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="orderItemId"></param>
        /// <param name="isAllOrderRefund">是否为整笔退 null 所有 true 整笔退 false 货品售后</param>
        /// <returns></returns>
        public bool CanApplyRefund(long orderId, long orderItemId, bool? isAllOrderRefund = null)
        {
            bool result = false;
            var sql = DbFactory.Default.Get<OrderRefundInfo>().Where(d => d.OrderId == orderId && d.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.UnAudit);
            if (DbFactory.Default.Get<OrderInfo>().Where(d => d.Id == orderId
            && d.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery && d.OrderStatus != OrderInfo.OrderOperateStatus.Finish).Exist())
            {
                //货到付款订单在未完成订单前不可售后
                return false;
            }
            if (isAllOrderRefund == true)
            {
                sql.Where(d => d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund);
            }
            else
            {
                sql.Where(d => d.OrderItemId == orderItemId);
                if (isAllOrderRefund == false)
                {
                    sql.Where(d => d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                }
            }
            result = (sql.Count() < 1);
            return result;
        }

        /// <summary>
        /// 添加或修改售后原因
        /// </summary>
        /// <param name="id"></param>
        /// <param name="reason"></param>
        public void UpdateAndAddRefundReason(string reason, long id)
        {
            if (DbFactory.Default.Get<RefundReasonInfo>().Where(d => d.Id != id && d.AfterSalesText == reason).Exist())
            {
                throw new HimallException("售后原因重复");
            }
            RefundReasonInfo data = DbFactory.Default.Get<RefundReasonInfo>().Where(d => d.Id == id).FirstOrDefault();
            if (id == 0)
            {
                data = new RefundReasonInfo();
            }
            if (data == null)
            {
                throw new HimallException("售后原因为空");
            }
            data.AfterSalesText = reason;
            data.Sequence = 100;   //默认排序100

            DbFactory.Default.Save(data);
        }
        /// <summary>
        /// 获取售后原因列表
        /// </summary>
        /// <returns></returns>
        public List<RefundReasonInfo> GetRefundReasons()
        {
            return DbFactory.Default.Get<RefundReasonInfo>().ToList();
        }
        /// <summary>
        /// 删除售后原因
        /// </summary>
        /// <param name="id"></param>
        public void DeleteRefundReason(long id)
        {
            //var data = Context.RefundReasonInfo.FirstOrDefault(d => d.Id == id);
            //if (data != null)
            //{
            //    Context.RefundReasonInfo.Remove(data);
            //    Context.SaveChanges();
            //}
            DbFactory.Default.Del<RefundReasonInfo>().Where(n => n.Id == id).Succeed();
        }
        /// <summary>
        /// 获取售后日志
        /// </summary>
        /// <param name="refundId"></param>
        /// <returns></returns>
        public List<OrderRefundLogInfo> GetRefundLogs(long refundId, int currentApplyNumber = 0, bool haveCurrentApplyNumber = true)
        {
            var sql = DbFactory.Default.Get<OrderRefundLogInfo>().Where(d => d.RefundId == refundId);
            if (currentApplyNumber > 0)
            {
                int getappnum = currentApplyNumber - 1;
                if (haveCurrentApplyNumber)
                {
                    getappnum++;
                }

                sql.Where(d => d.ApplyNumber <= getappnum);
            }
            sql.OrderByDescending(d => d.OperateDate).OrderByDescending(d => d.Id);
            var list = sql.ToList();

            #region 填充Step和Remark
            //step和remark是后来添加的，为了适应老数据，所以需要根据OperateContent填充Step和Remark
            var stepMap = new Dictionary<string, OrderRefundStep>();
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.Audited.ToDescription(), OrderRefundStep.UnConfirm);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.UnAudit.ToDescription(), OrderRefundStep.UnAudit);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.WaitAudit.ToDescription(), OrderRefundStep.WaitAudit);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery.ToDescription(), OrderRefundStep.WaitDelivery);
            stepMap.Add(OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving.ToDescription(), OrderRefundStep.WaitReceiving);
            stepMap.Add(OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm.ToDescription(), OrderRefundStep.UnConfirm);
            stepMap.Add(OrderRefundInfo.OrderRefundConfirmStatus.Confirmed.ToDescription(), OrderRefundStep.Confirmed);

            foreach (var item in list)
            {
                if (item.Step > 0)
                    continue;

                var match = System.Text.RegularExpressions.Regex.Match(item.OperateContent, "【(.+)】(.+)$");
                if (match.Success)
                {
                    var refundState = match.Groups[1].Value;
                    if (stepMap.ContainsKey(refundState))
                        item.Step = stepMap[refundState];
                    item.Remark = match.Groups[2].Value;
                }
            }
            #endregion

            return list;
        }
        /// <summary>
        /// 写入售后日志
        /// <para>写入日志的内容为：[状态]日志说明</para>
        /// </summary>
        /// <param name="RefundId"></param>
        /// <param name="LogContent"></param>
        public void AddRefundLog(long refundId, int? applyNumber, OrderRefundStep step, string refundState, string userName, string remark)
        {
            applyNumber = applyNumber.HasValue ? applyNumber.Value : 1;
            var data = new OrderRefundLogInfo();
            data.RefundId = refundId;
            data.ApplyNumber = applyNumber ?? 0;
            data.Operator = userName;
            data.OperateDate = DateTime.Now;
            data.OperateContent = "【" + refundState + "】" + remark;
            data.Remark = remark;
            data.Step = step;
            DbFactory.Default.Add(data);
        }
        /// <summary>
        /// 自动审核退款(job)
        /// </summary>
        //public void AutoAuditRefund()
        //{
        //    var sitesetser = ServiceProvider.Instance<ISiteSettingService>.Create;
        //    // var siteSetting = sitesetser.GetSiteSettings();
        //    var siteSetting = sitesetser.GetSiteSettingsByObjectCache();
        //    if (siteSetting.AS_ShopConfirmTimeout > 0)
        //    {
        //        DateTime stime = DateTime.Now.AddDays(-siteSetting.AS_ShopConfirmTimeout);
        //        var rflist = DbFactory.Default.Get<OrderRefundInfo>().Where(d => d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit && d.ApplyDate < stime).Select(d => d.Id).ToList<long>();
        //        if (rflist.Count > 0)
        //        {
        //            Himall.Core.Log.Debug("RefundJob : AutoAuditRefund Number=" + rflist.Count);
        //        }
        //        foreach (var item in rflist)
        //        {
        //            try
        //            {
        //                SellerDealRefund(item, OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery, "卖家超时未处理，系统自动同意售后", "系统Job");
        //            }
        //            catch (Exception ex)
        //            {
        //                Log.Debug("RefundJob : AutoAuditRefund [有错误]编号：" + item.ToString(), ex);
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// 自动关闭过期未寄货退款(job)
        /// </summary>
        //public void AutoCloseByDeliveryExpired()
        //{
        //    var sitesetser = ServiceProvider.Instance<ISiteSettingService>.Create;
        //    //  var siteSetting = sitesetser.GetSiteSettings();
        //    //windows服务调用此处不报错
        //    var siteSetting = sitesetser.GetSiteSettingsByObjectCache();
        //    if (siteSetting.AS_SendGoodsCloseTimeout > 0)
        //    {
        //        DateTime stime = DateTime.Now.AddDays(-siteSetting.AS_SendGoodsCloseTimeout);
        //        var rflist = DbFactory.Default.Get<OrderRefundInfo>().Where(d => d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery && d.SellerAuditDate < stime).Select(d => d.Id).ToList<long>();
        //        if (rflist.Count > 0)
        //        {
        //            Himall.Core.Log.Debug("RefundJob : AutoCloseByDeliveryExpired Number=" + rflist.Count);
        //        }
        //        foreach (var item in rflist)
        //        {
        //            try
        //            {
        //                SellerDealRefund(item, OrderRefundInfo.OrderRefundAuditStatus.UnAudit, "买家超时未寄货，系统自动拒绝售后", "系统Job");
        //            }
        //            catch (Exception ex)
        //            {
        //                Log.Debug("RefundJob : AutoCloseByDeliveryExpired [有错误]编号：" + item.ToString(), ex);
        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// 自动商家确认到货(job)
        /// </summary>
        //public void AutoShopConfirmArrival()
        //{
        //    var sitesetser = ServiceProvider.Instance<ISiteSettingService>.Create;
        //    //  var siteSetting = sitesetser.GetSiteSettings();
        //    //windows服务得用此缓存
        //    var siteSetting = sitesetser.GetSiteSettingsByObjectCache();
        //    if (siteSetting.AS_ShopNoReceivingTimeout > 0)
        //    {
        //        DateTime stime = DateTime.Now.AddDays(-siteSetting.AS_ShopNoReceivingTimeout);
        //        var rflist = DbFactory.Default.Get<OrderRefundInfo>().Where(d => d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving && d.BuyerDeliverDate < stime).Select(d => d.Id).ToList<long>();
        //        if (rflist.Count > 0)
        //        {
        //            Himall.Core.Log.Debug("RefundJob : AutoShopConfirmArrival Number=" + rflist.Count);
        //        }
        //        foreach (var item in rflist)
        //        {
        //            try
        //            {
        //                SellerConfirmRefundGood(item, "系统Job");
        //            }
        //            catch (Exception ex)
        //            {
        //                Log.Debug("RefundJob : AutoShopConfirmArrival [有错误]编号：" + item.ToString(), ex);
        //            }
        //        }
        //    }
        //}
        #endregion

        /// <summary>
        /// 售后发送app消息
        /// </summary>
        /// <param name="orderInfo"></param>
        public void SendRefundAppMessage(OrderRefundInfo refundInfo, OrderInfo orderInfo)
        {
            AppMessageService _iAppMessageService = Himall.ServiceProvider.Instance<AppMessageService>.Create;
            var app = new AppMessageInfo()
            {
                IsRead = false,
                sendtime = DateTime.Now,
                SourceId = refundInfo.Id,
                TypeId = (int)AppMessagesType.AfterSale,
                OrderPayDate = Core.Helper.TypeHelper.ObjectToDateTime(orderInfo.PayDate),
                ShopId = 0,
                ShopBranchId = 0
            };
            if (refundInfo.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
            {
                app.Content = string.Format("{0} 等待您审核", orderInfo.Id);
                app.Title = "您有新的售后申请";
            }
            else if (refundInfo.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving)
            {
                app.Content = string.Format("{0} 等待您收货", orderInfo.Id);
                app.Title = "您有买家寄回的商品";
            }
            if (orderInfo.ShopBranchId > 0)
            {
                app.ShopBranchId = orderInfo.ShopBranchId;
            }
            else
            {
                app.ShopId = refundInfo.ShopId;
            }
            if (!string.IsNullOrEmpty(app.Title))
                _iAppMessageService.AddAppMessages(app);
        }

        /// <summary>
        /// 确认收货后，处理库存
        /// </summary>
        /// <param name="order"></param>
        private void ReturnStock(OrderItemInfo orderItem, OrderInfo order, long returnQuantity)
        {
            SKUInfo sku = DbFactory.Default.Get<SKUInfo>().Where(p => p.Id == orderItem.SkuId).FirstOrDefault();
            if (sku != null)
            {
                if (order.ShopBranchId > 0)
                {
                    var sbSku = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(p => p.SkuId == sku.Id && p.ShopBranchId == order.ShopBranchId).FirstOrDefault();
                    if (sbSku != null)
                    {
                        sbSku.Stock += (int)returnQuantity;
                        DbFactory.Default.Update(sbSku);

                        // 限购还原活动库存
                        if (order.OrderType == OrderInfo.OrderTypes.LimitBuy)
                        {
                            var flashSaleDetailInfo = DbFactory.Default.Get<FlashSaleDetailInfo>().Where(a => a.SkuId == orderItem.SkuId && a.FlashSaleId == orderItem.FlashSaleId).FirstOrDefault();
                            if (flashSaleDetailInfo != null)
                            {
                                flashSaleDetailInfo.TotalCount += (int)returnQuantity;
                                DbFactory.Default.Update(flashSaleDetailInfo);
                            }
                        }

                        //还原商品销量
                        //var productid = orderItem.ProductId;
                        //var product = DbFactory.Default.Get<ProductInfo>().Where(d => d.Id == productid).FirstOrDefault();
                        //if (product != null)
                        //{
                        //    product.SaleCounts -= returnQuantity;
                        //    var searchProduct = DbFactory.Default.Get<SearchProductInfo>().Where(r => r.ProductId == product.Id).FirstOrDefault();
                        //    if (searchProduct != null)
                        //        searchProduct.SaleCount -= (int)returnQuantity;
                        //    if (searchProduct.SaleCount < 0)
                        //    {
                        //        searchProduct.SaleCount = 0;
                        //    }
                        //    if (product.SaleCounts < 0)
                        //    {
                        //        product.SaleCounts = 0;
                        //    }
                        //    DbFactory.Default.Update(product);
                        //    DbFactory.Default.Update(searchProduct);
                        //}
                    }
                }
                else
                {
                    sku.Stock += returnQuantity;
                    DbFactory.Default.Update(sku);
                }

            }

        }
    }
}
