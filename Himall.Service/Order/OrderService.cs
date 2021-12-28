using com.google.zxing.qrcode.decoder;
using Himall.CommonModel;
using Himall.CommonModel.Delegates;
using Himall.Core;
using Himall.Core.Extends;
using Himall.Core.Plugins.Message;
using Himall.Core.Plugins.Payment;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.ServiceProvider;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using NetRube;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using static Himall.Entities.OrderInfo;

namespace Himall.Service
{
    public class OrderService : ServiceBase
    {
        #region 静态字段
        private static readonly System.Security.Cryptography.RandomNumberGenerator _randomPickupCode = System.Security.Cryptography.RandomNumberGenerator.Create();
        private const string PAY_BY_CAPITAL_PAYMENT_ID = "预存款支付";
        private const string PAY_BY_OFFLINE_PAYMENT_ID = "线下收款";
        private const string PAY_BY_INTEGRAL_PAYMENT_ID = "积分支付";
        #endregion

        #region 字段
        private FightGroupService _FightGroupService;
        private AppMessageService _iAppMessageService;
        private WDTOrderService _WDTOrderService;
        public void Settlement()
        {
            try
            {
                var flag = DbFactory.Default
                    .InTransaction(() =>
                    {
                        var checkDate = DateTime.MinValue;
                        Log.Debug("AccountJob : start");
                        var weekSettlement = 0;//结算周期
                        var SalesReturnTimeout = 0;//售后维权期
                        var integralrule = GetIntegralChangeRule();
                        var sitesetting = DbFactory.Default.Query<SiteSettingInfo>("Select `Key`,`Value` from Himall_SiteSetting where `Key`='WeekSettlement' or `Key`='SalesReturnTimeout'").ToList();
                        var weekSetting = sitesetting.FirstOrDefault(e => e.Key == "WeekSettlement");
                        var timeoutSetting = sitesetting.FirstOrDefault(e => e.Key == "SalesReturnTimeout");
                        var expriedtemp = weekSetting != null ? weekSetting.Value : "0";
                        var timeoutSettingStr = timeoutSetting != null ? timeoutSetting.Value : "0";
                        int.TryParse(expriedtemp, out weekSettlement);
                        int.TryParse(timeoutSettingStr, out SalesReturnTimeout);//售后维权期
                        if (weekSettlement < 1)
                        {
                            Log.Error("结算周期设置不正确！ ");
                            return false;
                        }
                        //处理开始时间
                        var account = DbFactory.Default.Get<AccountInfo>().OrderByDescending(a => a.Id).FirstOrDefault();
                        if (account == null)
                            checkDate = GetDate(checkDate).Date;  //第一笔结算数据
                        else
                            checkDate = account.EndDate.Date;//上一次结束日期（2015-11-23 00:00:00），作为开始时间

                        if (checkDate.Equals(DateTime.MinValue))
                            return false;
                        //开始结算
                        DateTime startDate = checkDate.Date;
                        DateTime endDate = startDate.AddDays(weekSettlement);
                        Log.Debug("AccountJob:endDate" + endDate + "DateTime:" + DateTime.Now.Date + "result:" + (endDate < DateTime.Now.Date));
                        while (endDate < DateTime.Now)
                        {
                            //结算日期内的待结算订单 不计算开始时间，防止漏单，结算过后，会删除待结算订单
                            //完成【时间】< 结算周期结束【日期】（包括未过售后期的）；
                            var queryEndDate = endDate.AddDays(-SalesReturnTimeout);
                            var prePendingSetllementData = DbFactory.Default.Get<PendingSettlementOrderInfo>().Where(c =>
                            (c.OrderFinshTime.HasValue && c.OrderFinshTime.Value < endDate))
                            .OrderByDescending(c => c.OrderFinshTime).ToList();

                            //已关闭的订单ID
                            var preOrderIds = prePendingSetllementData.Select(e => e.OrderId);
                            var closeIds = DbFactory.Default.Get<OrderInfo>().Where(e => e.Id.ExIn(preOrderIds) && e.OrderStatus == OrderInfo.OrderOperateStatus.Close).ToList().Select(e => e.Id);
                            //已过售后期、已关闭的订单,已完成的虚拟订单，都可以结算
                            var pendingSetllementData = prePendingSetllementData.Where(e => e.OrderFinshTime < queryEndDate || closeIds.Contains(e.OrderId) || e.OrderType == OrderInfo.OrderTypes.Virtual).ToList();

                            //可结算的订单，是否有未完成的售后
                            var orderIds = pendingSetllementData.Select(e => e.OrderId);
                            var refundOrderIds = DbFactory.Default.Get<OrderRefundInfo>().Where(e => e.OrderId.ExIn(orderIds)
                            && (e.SellerAuditStatus != OrderRefundInfo.OrderRefundAuditStatus.UnAudit) && e.ManagerConfirmStatus != OrderRefundInfo.OrderRefundConfirmStatus.Confirmed).ToList().Select(e => e.OrderId);

                            //过滤有未完成的售后订单
                            pendingSetllementData = pendingSetllementData.Where(e => !refundOrderIds.Contains(e.OrderId)).ToList();

                            if (pendingSetllementData.Count == 0)
                            {
                                //无可结算订单，进入下一周期
                                startDate = endDate.Date;
                                endDate = startDate.AddDays(weekSettlement);
                                continue;
                            }
                            Log.Debug("Count:" + pendingSetllementData.Count());
                            var accountInfo = new AccountInfo();
                            accountInfo.ShopId = 0;
                            accountInfo.ShopName = "系统定时任务结算";
                            accountInfo.AccountDate = DateTime.Now;
                            accountInfo.StartDate = startDate;
                            accountInfo.EndDate = endDate;
                            accountInfo.Status = AccountInfo.AccountStatus.Accounted;
                            accountInfo.ProductActualPaidAmount = pendingSetllementData.Sum(a => a.ProductsAmount);
                            accountInfo.FreightAmount = pendingSetllementData.Sum(b => b.FreightAmount);
                            accountInfo.CommissionAmount = pendingSetllementData.Sum(c => c.PlatCommission);
                            accountInfo.RefundCommissionAmount = pendingSetllementData.Sum(d => d.PlatCommissionReturn);
                            accountInfo.RefundAmount = pendingSetllementData.Sum(e => e.RefundAmount);
                            accountInfo.AdvancePaymentAmount = 0;
                            accountInfo.Brokerage = pendingSetllementData.Sum(f => f.DistributorCommission);
                            accountInfo.ReturnBrokerage = pendingSetllementData.Sum(g => g.DistributorCommissionReturn);
                            accountInfo.PeriodSettlement = pendingSetllementData.Sum(h => h.SettlementAmount);
                            DbFactory.Default.Add(accountInfo);
                            //结算主表汇总数据
                            var details = pendingSetllementData.Select(item => new AccountDetailInfo
                            {
                                AccountId = accountInfo.Id,
                                ShopId = item.ShopId,
                                ShopName = item.ShopName,
                                OrderType = AccountDetailInfo.EnumOrderType.FinishedOrder,
                                Date = DateTime.Now,
                                OrderFinshDate = item.OrderFinshTime.Value,
                                OrderId = item.OrderId,
                                ProductActualPaidAmount = item.ProductsAmount,
                                FreightAmount = item.FreightAmount,
                                CommissionAmount = item.PlatCommission,
                                RefundCommisAmount = item.PlatCommissionReturn,
                                OrderRefundsDates = item.RefundDate.HasValue ? item.RefundDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                                RefundTotalAmount = item.RefundAmount,
                                OrderAmount = item.OrderAmount,
                                IntegralDiscount = item.IntegralDiscount,
                                TaxAmount = item.TaxAmount,
                                BrokerageAmount = item.DistributorCommission,
                                ReturnBrokerageAmount = item.DistributorCommissionReturn,
                                SettlementAmount = item.SettlementAmount,
                                PaymentTypeName = item.PaymentTypeName,
                                DiscountAmount = item.DiscountAmount,
                                DiscountAmountReturn = item.DiscountAmountReturn
                            });
                            DbFactory.Default.Add(details);

                            Random r = new Random();
                            var plat = DbFactory.Default.Get<PlatAccountInfo>().FirstOrDefault();//平台账户
                            var platAccountItem = new PlatAccountItemInfo();
                            platAccountItem.AccoutID = plat.Id;
                            platAccountItem.CreateTime = DateTime.Now;
                            platAccountItem.AccountNo = string.Format("{0:yyyyMMddHHmmssfff}{1}", DateTime.Now, r.Next(1000, 9999));
                            platAccountItem.Amount = accountInfo.CommissionAmount;//平台佣金
                            platAccountItem.Balance = plat.Balance + platAccountItem.Amount;//账户余额+平台佣金
                            platAccountItem.TradeType = PlatAccountType.SettlementIncome;
                            platAccountItem.IsIncome = true;
                            platAccountItem.ReMark = DateTime.Now + "平台结算" + accountInfo.Id;
                            platAccountItem.DetailId = accountInfo.Id.ToString();
                            DbFactory.Default.Add(platAccountItem);

                            if (plat != null)
                            {
                                //平台账户总金额(加这次平台的佣金)
                                plat.Balance += platAccountItem.Amount;//平台佣金
                                                                       //平台待结算金额
                                plat.PendingSettlement -= accountInfo.PeriodSettlement;//本次结算的总金额。//platAccountItem.Amount;//平台佣金-平台佣金退还
                                                                                       //平台已结算金额
                                plat.Settled += accountInfo.PeriodSettlement;//本次结算的总金额。//platAccountItem.Amount;//平台佣金-平台佣金退还
                                DbFactory.Default.Update(plat);
                            }

                            var shoppendingSetllement = pendingSetllementData.GroupBy(a => a.ShopId).ToList();
                            foreach (var item in shoppendingSetllement)
                            {
                                //商户资金明细表
                                var shopAccount = DbFactory.Default.Get<ShopAccountInfo>().Where(a => a.ShopId == item.Key).FirstOrDefault();
                                if (shopAccount == null)
                                {
                                    shopAccount = new ShopAccountInfo();
                                    shopAccount.ShopId = item.Key;
                                    var orderpen = item.FirstOrDefault();
                                    shopAccount.ShopName = orderpen != null ? orderpen.ShopName : "";
                                    shopAccount.Balance = shopAccount.PendingSettlement = shopAccount.Settled = 0;
                                    shopAccount.ReMark = "";
                                    DbFactory.Default.Add(shopAccount);
                                }

                                var shopAccountItemInfo = new ShopAccountItemInfo();
                                shopAccountItemInfo.AccoutID = shopAccount.Id;
                                shopAccountItemInfo.AccountNo = string.Format("{0:yyyyMMddHHmmssfff}{1}", DateTime.Now, r.Next(1000, 9999));
                                shopAccountItemInfo.ShopId = shopAccount.ShopId;
                                shopAccountItemInfo.ShopName = shopAccount.ShopName;
                                shopAccountItemInfo.CreateTime = DateTime.Now;
                                shopAccountItemInfo.Amount = item.Sum(a => a.SettlementAmount);//结算金额
                                shopAccountItemInfo.Balance = shopAccount.Balance + shopAccountItemInfo.Amount; ;//账户余额+结算金额
                                shopAccountItemInfo.TradeType = ShopAccountType.SettlementIncome;
                                shopAccountItemInfo.IsIncome = true;
                                shopAccountItemInfo.ReMark = "店铺结算明细" + accountInfo.Id; ;
                                shopAccountItemInfo.DetailId = accountInfo.Id.ToString();
                                shopAccountItemInfo.SettlementCycle = weekSettlement;
                                DbFactory.Default.Add(shopAccountItemInfo);

                                if (shopAccount != null)
                                {
                                    shopAccount.Balance += shopAccountItemInfo.Amount;//结算金额
                                    shopAccount.PendingSettlement -= shopAccountItemInfo.Amount;
                                    if (shopAccount.PendingSettlement <= 0)
                                        shopAccount.PendingSettlement = 0;//如上面刚添加如为负，默认0；
                                    shopAccount.Settled += shopAccountItemInfo.Amount;//平台佣金
                                    DbFactory.Default.Update(shopAccount);
                                }
                            }
                            PendingSetteMemberInterg(pendingSetllementData, integralrule);//结算积分

                            DbFactory.Default.Del<PendingSettlementOrderInfo>(pendingSetllementData);
                            startDate = endDate.Date;
                            endDate = startDate.AddDays(weekSettlement);
                        }
                        return true;
                    });

            }
            catch (Exception ex)
            {
                Log.Error("AccountJob : " + ex.Message);
            }
        }

        public void SyncOrder()
        {
            var movedCount = 0;
            var pageSize = 1000;
            var SyncMaxOrderId = GetSyncMaxOrderId();
            var totalRecord = GetOrderRecordCount(SyncMaxOrderId);
            var totalCount = 0;
            while (movedCount < totalRecord)
            {
                var orders = GetOldOrders(SyncMaxOrderId, 1, pageSize);
                if (orders.Count == 0)
                    break;

                var newOrders = new List<OrderInfo>();
                var newOrderItems = new List<OrderItemInfo>();
                foreach (var order in orders)
                {
                    if (!IsOldOrder(order.Id))
                    {
                        newOrders.Add(order);
                        if (null == order.OrderItemInfo || order.OrderItemInfo.Count <= 0) break;
                        foreach (var orderitem in order.OrderItemInfo)
                        {
                            newOrderItems.Add(orderitem);
                        }
                    }
                }
                if (newOrders.Count > 0)
                {
                    var coll = DbFactory.MongoDB.Get<OrderInfo>();
                    var collItem = DbFactory.MongoDB.Get<OrderItemInfo>();
                    coll.InsertMany(newOrders);
                    collItem.InsertMany(newOrderItems);
                    totalCount += newOrders.Count;
                }
                foreach (var order in orders)
                {
                    DeleteOrder(order.Id);
                }
                movedCount += orders.Count;
            }
            AddSyncRecord(SyncMaxOrderId);
        }
        /// <summary>
        /// 添加同步记录
        /// </summary>
        /// <param name="lastOrderId"></param>
        internal void AddSyncRecord(long lastOrderId)
        {
            var coll = DbFactory.MongoDB.Get<SyncOrderRecord>();
            SyncOrderRecord record = new SyncOrderRecord();
            record.SyncTime = DateTime.Now;
            record.LastOrderId = lastOrderId;
            record.TableName = "OrderInfo" + " " + "OrderItemInfo";
            coll.InsertOne(record);
        }
        internal long GetSyncMaxOrderId()
        {
            var date = DateTime.Now.Date;
            var strSYNC_MONTH = ConfigurationManager.AppSettings["SYNCMONTH"];
            var iSYNC_MONTH = 3;
            int.TryParse(strSYNC_MONTH, out iSYNC_MONTH);
            var threeMonths = date.AddMonths(-iSYNC_MONTH);
            var id = long.Parse(threeMonths.ToString("yyyyMMddfff00000"));
            return id;
        }
        /// <summary>
        /// 判断订单是否已经同步
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        private bool IsOldOrder(long orderId)
        {
            var coll = DbFactory.MongoDB.Get<OrderInfo>();
            return coll.Find(a => a.Id == orderId).Any();
        }

        private long GetOrderRecordCount(long id)
        {
            //无参数查询，返回列表，带参数查询和之前的参数赋值法相同。
            var num = DbFactory.Default
                .Get<OrderInfo>()
                .Where(n => n.Id < id && (n.OrderStatus == OrderInfo.OrderOperateStatus.Finish || n.OrderStatus == OrderInfo.OrderOperateStatus.Close))
                .Count<long>();
            return num;
        }
        /// <summary>
        ///分页获取三个月之前的订单基础信息
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        private List<OrderInfo> GetOldOrders(long id, int pageNo, int pageSize)
        {
            //无参数查询，返回列表，带参数查询和之前的参数赋值法相同。
            //OrderInfo current = null;
            var orders = DbFactory.Default
                .Get<OrderInfo>()
                .Where(n => n.Id < id && (n.OrderStatus == OrderInfo.OrderOperateStatus.Finish || n.OrderStatus == OrderInfo.OrderOperateStatus.Close))
                .OrderBy(n => n.Id)
                .ToPagedList(pageNo, pageSize);
            return orders;
        }

        /// <summary>
        /// 删除订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        private bool DeleteOrder(long orderId)
        {
            //无参数查询，返回列表，带参数查询和之前的参数赋值法相同。
            var flag = DbFactory.Default.
                InTransaction(() =>
                {
                    DbFactory.Default
                        .Del<OrderItemInfo>()
                        .FOREIGNKEYCHECKS(true)
                        .Where(n => n.OrderId == orderId)
                        .Succeed();
                    return DbFactory.Default
                        .Del<OrderInfo>()
                        .FOREIGNKEYCHECKS(true)
                        .Where(n => n.Id == orderId)
                        .Succeed();
                });
            return flag;
        }
        private DateTime GetDate(DateTime checkDate)
        {
            var firstFinishedOrder = DbFactory.Default.Get<PendingSettlementOrderInfo>().Where(e => e.OrderFinshTime.HasValue).OrderBy(c => c.OrderFinshTime).FirstOrDefault();
            if (firstFinishedOrder != null)
            {
                checkDate = firstFinishedOrder.OrderFinshTime.Value;
            }
            else
            {
                checkDate = DateTime.MinValue;
            }
            return checkDate;
        }
        public void PendingSetteMemberInterg(List<PendingSettlementOrderInfo> pendingSetllementData, MemberIntegralExchangeRuleInfo inetgralruleInfo)
        {
            var pendorderIds = pendingSetllementData.Select(p => p.OrderId);//待结算的订单号
            var pendmembers = DbFactory.Default.Get<OrderInfo>().Where(o => o.Id.ExIn(pendorderIds)).ToList();//取待结算的会员编号
            var members = DbFactory.Default.Get<MemberInfo>().Where(m => m.Id.ExIn(pendmembers.Select(p => p.UserId)));//获取要增加积分的会员信息
            foreach (var item in pendmembers)
            {
                var currentmember = members.Where(m => m.Id == item.UserId).FirstOrDefault();
                if (currentmember != null)
                {
                    AddIntegral(currentmember, item.Id, item.ActualPayAmount);
                }
            }
        }
        public MemberIntegralExchangeRuleInfo GetIntegralChangeRule()
        {
            var model = DbFactory.Default.Get<MemberIntegralExchangeRuleInfo>().FirstOrDefault();
            #region 当新的项目没数据时初始数据
            if (model == null)
            {
                model = new MemberIntegralExchangeRuleInfo();
                model.IntegralPerMoney = 0;
                model.MoneyPerIntegral = 0;
                DbFactory.Default.Add(model);
            }
            #endregion
            return model;
        }

        public void AddIntegral(MemberInfo member, long orderId, decimal orderTotal)
        {
            if (member == null)
                return;
            if (orderTotal <= 0)
            {
                return;
            }
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
            MemberIntegralRecordActionInfo action = new MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = MemberIntegralInfo.VirtualItemType.Consumption;
            action.VirtualItemId = orderId;
            record.MemberIntegralRecordActionInfo.Add(action);

            AddMemberIntegral(record, integral);
        }

        public void AddMemberIntegral(MemberIntegralRecordInfo model, int integral)
        {
            model.Integral = integral;
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
                userIntegral.HistoryIntegrals += model.Integral;
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Add(userIntegral);
            }
            else
            {
                userIntegral.HistoryIntegrals += model.Integral;
                userIntegral.AvailableIntegrals += model.Integral;
                DbFactory.Default.Update(userIntegral);
            }

            DbFactory.Default.Add(model);

            if (model.MemberIntegralRecordActionInfo != null)
            {
                model.MemberIntegralRecordActionInfo.ForEach(p => p.IntegralRecordId = model.Id);
                DbFactory.Default.AddRange(model.MemberIntegralRecordActionInfo);
            }
        }

        #endregion

        #region 构造函数
        public OrderService()
        {
            _FightGroupService = Instance<FightGroupService>.Create;
            _iAppMessageService = Instance<AppMessageService>.Create;
            _WDTOrderService = Instance<WDTOrderService>.Create;
        }


        /// <summary>
        /// 获取待通知订单
        /// </summary>
        public List<OrderInfo> GetWaitSendMsgOrder(DateTime notifyTime)
        {
            return DbFactory.Default.Get<OrderInfo>().Where(a => a.OrderDate < notifyTime && a.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay && a.IsSend != true).ToList();
        }
        public void SendComplete(List<long> orders)
        {
            DbFactory.Default.Set<OrderInfo>()
                .Where(p => p.Id.ExIn(orders))
                .Set(p => p.IsSend, true)
                .Execute();
        }
        #endregion

        #region 属性
        public event OrderPaySuccessed OnOrderPaySuccessed;
        #endregion

        public QueryPageModel<OrderInfo> GetOrders<Tout>(OrderQuery query, Expression<Func<OrderInfo, Tout>> sort = null)
        {
            var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
            GetBuilder<OrderInfo> orders = null;
            var history = DbFactory.MongoDB.AsQueryable<OrderInfo>();
            if (query.Status != OrderInfo.OrderOperateStatus.History)
            {
                orders = DbFactory.Default.Get<OrderInfo>();
            }
            history = ToWhere(orders, history, query);

            var temp = 0;
            var rets = new List<OrderInfo>();
            if (sort == null)
            {
                if (null != orders)
                    orders.OrderByDescending(item => item.OrderDate);
                history = history.OrderByDescending(item => item.OrderDate);
            }
            else
            {
                if (null != orders)
                    orders.OrderBy((Expression)sort);
                history = history.OrderBy(sort);
            }
            if (null == orders)
            {
                if (flag)//就算orders查不到数据，但如果没开启历史订单，应该也不能执行
                {
                    temp = history.Count();
                    rets = history.Skip((query.PageNo - 1) * query.PageSize).Take(query.PageSize).ToList();
                }
            }
            else
            {
                var result = orders.ToPagedList(query.PageNo, query.PageSize);
                rets = result;
                temp = result.TotalRecordCount;
            }

            var total = temp + (!flag || query.Status == OrderInfo.OrderOperateStatus.History || query.Operator != Operator.None ? 0 : history.Count(item => item.Id > 0));
            if (flag && rets.Count < query.PageSize && query.Status != OrderInfo.OrderOperateStatus.History && query.Operator == Operator.None)//开启历史记录才从mongodb获取
            {
                query.PageNo -= (int)Math.Ceiling((float)(temp / query.PageSize));
                temp = history.Count();
                var historyorders = history.OrderByDescending(item => item.OrderDate).Skip((query.PageNo - 1) * query.PageSize).Take(query.PageSize).ToList();

                if (rets.Count > 0)
                {
                    rets.AddRange(historyorders.Take(query.PageSize - rets.Count));
                }
                else
                {
                    rets.AddRange(historyorders);
                }
            }
            var orderIds = rets.Select(p => p.Id).ToList();
            var allOrderItems = DbFactory.Default.Get<OrderItemInfo>(p => p.OrderId.ExIn(orderIds)).ToList();
            List<OrderInvoiceInfo> invoiceInfos = GetOrderInvoicesByOrderId(orderIds);
            foreach (var orderInfo in rets)
            {
                if (invoiceInfos != null)
                {
                    orderInfo.OrderInvoice = invoiceInfos.FirstOrDefault(i => i.OrderId == orderInfo.Id);
                }
                var orderitems = allOrderItems.Where(p => p.OrderId == orderInfo.Id).ToList();
                if (orderitems.Count <= 0)
                {
                    orderitems = GetOrderItemsByOrderId(orderInfo.Id);
                }
                foreach (var itemInfo in orderitems)
                {
                    var typeInfo = DbFactory.Default
                        .Get<TypeInfo>()
                        .InnerJoin<ProductInfo>((ti, pi) => ti.Id == pi.TypeId && pi.Id == itemInfo.ProductId)
                        .FirstOrDefault();
                    var prodata = DbFactory.Default
                        .Get<ProductInfo>().Where(pi => pi.Id == itemInfo.ProductId).FirstOrDefault();
                    itemInfo.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    itemInfo.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    itemInfo.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    if (prodata != null)
                    {
                        itemInfo.ColorAlias = !string.IsNullOrWhiteSpace(prodata.ColorAlias) ? prodata.ColorAlias : itemInfo.ColorAlias;
                        itemInfo.SizeAlias = !string.IsNullOrWhiteSpace(prodata.SizeAlias) ? prodata.SizeAlias : itemInfo.SizeAlias;
                        itemInfo.VersionAlias = !string.IsNullOrWhiteSpace(prodata.VersionAlias) ? prodata.VersionAlias : itemInfo.VersionAlias;
                    }
                }
            }
            var pageModel = new QueryPageModel<OrderInfo>()
            {
                Models = rets,
                Total = total
            };

            return pageModel;
        }

        /// <summary>
        /// 分页获取订单
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<OrderInfo> GetOrders(OrderQuery query)
        {
            var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
            GetBuilder<OrderInfo> orders = null;
            var history = DbFactory.MongoDB.AsQueryable<OrderInfo>();
            if (query.Status != OrderInfo.OrderOperateStatus.History)
            {
                orders = DbFactory.Default.Get<OrderInfo>();
            }
            history = ToWhere(orders, history, query);
            var temp = 0;
            var data = new List<OrderInfo>();
            if (orders == null)
            {
                if (flag)
                {
                    temp = history.Count();
                    data = history.OrderByDescending(n => n.OrderDate).Skip((query.PageNo - 1) * query.PageSize).Take(query.PageSize).ToList();
                }
            }
            else
            {
                var result = orders.OrderByDescending(n => n.OrderDate).ToPagedList(query.PageNo, query.PageSize);
                data = result;
                temp = result.TotalRecordCount;
            }

            var total = temp + (!flag || query.Status == OrderInfo.OrderOperateStatus.History || query.Operator != Operator.None ? 0 : history.Count(item => item.Id > 0));
            if (flag && data.Count < query.PageSize && query.Status != OrderInfo.OrderOperateStatus.History && query.Operator == Operator.None)//开启历史记录才从mongodb获取
            {
                query.PageNo -= (int)Math.Ceiling((float)(temp / query.PageSize));
                temp = history.Count();
                var historyorders = history.OrderByDescending(item => item.OrderDate).Skip((query.PageNo - 1) * query.PageSize).Take(query.PageSize).ToList();

                if (data.Count > 0)
                {
                    data.AddRange(historyorders.Take(query.PageSize - data.Count));
                }
                else
                {
                    data.AddRange(historyorders);
                }
            }

            return new QueryPageModel<OrderInfo>()
            {
                Models = data,
                Total = total
            };

        }

        public QueryPageModel<VerificationRecordInfo> GetVerificationRecords(VerificationRecordQuery query)
        {
            var db = WhereBuilder(query);
            db = db.OrderByDescending(p => p.VerificationTime);

            var data = db.ToPagedList(query.PageNo, query.PageSize);

            return new QueryPageModel<VerificationRecordInfo>()
            {
                Models = data,
                Total = data.TotalRecordCount
            };
        }

        private GetBuilder<VerificationRecordInfo> WhereBuilder(VerificationRecordQuery query)
        {

            var db = DbFactory.Default.Get<VerificationRecordInfo>()
               .LeftJoin<OrderInfo>((fi, pi) => fi.OrderId == pi.Id)
               .Select()
               .Select<OrderInfo>(p => new { p.PayDate, p.ShopBranchId, p.ShopId });

            if (query.ShopBranchId.HasValue)
            {
                var ordersql = DbFactory.Default
                    .Get<OrderInfo>()
                    .Where<OrderInfo, VerificationRecordInfo>((si, pi) => si.Id == pi.OrderId && si.ShopBranchId == query.ShopBranchId.Value);
                db.Where(p => p.ExExists(ordersql));
            }
            if (query.ShopId.HasValue)
            {
                var ordersql = DbFactory.Default
                    .Get<OrderInfo>()
                    .Where<OrderInfo, VerificationRecordInfo>((si, pi) => si.Id == pi.OrderId && si.ShopId == query.ShopId.Value);
                db.Where(p => p.ExExists(ordersql));
            }

            if (!string.IsNullOrWhiteSpace(query.OrderId))
            {
                var _where = PredicateExtensions.False<VerificationRecordInfo>();
                _where = _where.Or(p => p.VerificationCodeIds.Contains(string.Format(",{0},", query.OrderId)));

                var orderIdRange = GetOrderIdRange(query.OrderId);
                if (orderIdRange != null)
                {
                    var min = orderIdRange[0];
                    if (orderIdRange.Length == 2)
                    {
                        var max = orderIdRange[1];
                        _where = _where.Or(item => item.OrderId >= min && item.OrderId <= max);
                    }
                    else
                        _where = _where.Or(item => item.OrderId == min);
                }
                db.Where(_where);
            }
            return db;
        }

        /// <summary>
        /// 获取订单列表(忽略分页)
        /// </summary>
        /// <param name="orderQuery"></param>
        /// <returns></returns>
        public List<OrderInfo> GetAllOrders(OrderQuery orderQuery)
        {
            GetBuilder<OrderInfo> orders = null;
            var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
            var history = DbFactory.MongoDB.AsQueryable<OrderInfo>();
            if (orderQuery.Status != OrderInfo.OrderOperateStatus.History)
            {
                orders = DbFactory.Default.Get<OrderInfo>().OrderByDescending(p => p.OrderDate);
            }
            history = ToWhere(orders, history, orderQuery).OrderByDescending(p => p.OrderDate);
            if (null == orders && flag) return history.ToList();
            return orders.ToList();
        }

        /// <summary>
        /// 获取增量订单
        /// </summary>
        /// <param name="orderQuery"></param>
        /// <returns></returns>
        public QueryPageModel<OrderInfo> GetOrdersByLastModifyTime(OrderQuery orderQuery)
        {
            var orders = DbFactory.Default.Get<OrderInfo>();
            if (orderQuery.ShopId.HasValue)
                orders.Where(n => n.ShopId == orderQuery.ShopId.Value);
            if (!string.IsNullOrEmpty(orderQuery.ShopName))
                orders.Where(n => n.ShopName.Contains(orderQuery.ShopName));
            if (!string.IsNullOrEmpty(orderQuery.UserName))
                orders.Where(n => n.UserName.Contains(orderQuery.UserName));
            if (orderQuery.UserId.HasValue)
                orders.Where(n => n.UserId == orderQuery.UserId.Value);
            if (!string.IsNullOrEmpty(orderQuery.PaymentTypeName))
                orders.Where(n => n.PaymentTypeName.Contains(orderQuery.PaymentTypeName));
            if (!string.IsNullOrEmpty(orderQuery.PaymentTypeGateway))
                orders.Where(n => n.PaymentTypeGateway.Contains(orderQuery.PaymentTypeGateway));
            if (orderQuery.IgnoreSelfPickUp.HasValue)
            {
                if (orderQuery.IgnoreSelfPickUp.Value)
                {
                    orders.Where(p => p.DeliveryType != DeliveryType.SelfTake);
                }
                else
                {
                    orders.Where(p => p.DeliveryType == DeliveryType.SelfTake);
                }
            }
            if (!string.IsNullOrEmpty(orderQuery.SearchKeyWords))
            {
                long result;
                bool IsNumber = long.TryParse(orderQuery.SearchKeyWords, out result);
                var productname = DbFactory.Default
                    .Get<OrderItemInfo>()
                    .Where<OrderInfo>((oii, oi) => oii.OrderId == oi.Id && oii.ProductName.Contains(orderQuery.SearchKeyWords));
                if (IsNumber)
                {
                    var productid = DbFactory.Default
                        .Get<OrderItemInfo>()
                        .Where<OrderInfo>((oii, oi) => oii.OrderId == oi.Id && oii.ProductId == result);
                    orders.Where(n => n.Id == result || n.ExExists(productid) || n.ExExists(productname));
                }
                else
                {
                    orders.Where(n => n.ExExists(productname));
                }
            }
            if (orderQuery.Commented.HasValue)
            {
                var commented = orderQuery.Commented.Value;
                if (commented)
                {

                    var sub = DbFactory.Default
                        .Get<OrderCommentInfo>()
                        .Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id);
                    orders.Where(p => p.ExExists(sub));
                }
                else
                {
                    var sub = DbFactory.Default
                        .Get<OrderCommentInfo>()
                        .Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id);

                    orders.Where(p => p.ExNotExists(sub));
                }
            }
            var orderIdRange = GetOrderIdRange(orderQuery.OrderId);
            if (orderIdRange != null)
            {
                var min = orderIdRange[0];
                if (orderIdRange.Length == 2)
                {
                    var max = orderIdRange[1];
                    orders.Where(item => item.Id >= min && item.Id <= max);
                }
                else
                    orders.Where(item => item.Id == min);
            }

            if (orderQuery.Commented.HasValue && !orderQuery.Commented.Value)
            {
                var pc = DbFactory.Default
                    .Get<ProductCommentInfo>()
                    .LeftJoin<OrderItemInfo>((pci, oii) => pci.SubOrderId == oii.Id)
                    .Select<OrderItemInfo>(n => n.OrderId)
                    .Distinct()
                    .ToList<long>();
                orders.Where(item => item.Id.ExNotIn(pc));
            }
            //订单类型
            if (orderQuery.OrderType.HasValue)
            {
                var platform = orderQuery.OrderType.Value.ToEnum(PlatformType.PC);
                orders.Where(item => item.Platform == platform);
            }
            if (orderQuery.Status.HasValue)
            {
                switch (orderQuery.Status.Value)
                {
                    case OrderInfo.OrderOperateStatus.UnComment:
                        //TODO:FG 查询待优化
                        var comments = DbFactory.Default.Get<OrderCommentInfo>().Select(p => p.OrderId);
                        orders.Where(d => d.Id.ExNotIn(comments) && d.OrderStatus == OrderInfo.OrderOperateStatus.Finish);
                        break;
                    case OrderInfo.OrderOperateStatus.WaitDelivery:
                        var fgordids = DbFactory.Default
                               .Get<FightGroupOrderInfo>()
                               .Where(d => d.JoinStatus != 4)
                               .Select(d => d.OrderId.ExIfNull(0))
                               .ToList<long>();

                        //处理拼团的情况
                        orders.Where(d => d.OrderStatus == orderQuery.Status && d.Id.ExNotIn(fgordids));
                        break;
                    case OrderInfo.OrderOperateStatus.History:
                        break;
                    default:
                        orders.Where(d => d.OrderStatus == orderQuery.Status);
                        break;
                }


                if (orderQuery.MoreStatus != null)
                {
                    foreach (var stitem in orderQuery.MoreStatus)
                    {
                        orders.Where(d => d.OrderStatus == stitem);
                    }
                }
            }

            if (orderQuery.PaymentType != OrderInfo.PaymentTypes.None)
            {
                orders.Where(item => item.PaymentType == orderQuery.PaymentType);
            }

            //开始结束时间
            if (orderQuery.StartDate.HasValue)
            {
                DateTime sdt = orderQuery.StartDate.Value;
                orders.Where(d => d.LastModifyTime >= sdt);
            }
            if (orderQuery.EndDate.HasValue)
            {
                DateTime edt = orderQuery.EndDate.Value.AddDays(1).AddSeconds(-1);
                orders.Where(d => d.LastModifyTime <= edt);
            }

            switch (orderQuery.Sort.ToLower())
            {
                case "commentcount":

                    break;
            }
            var rets = orders.OrderByDescending(item => item.OrderDate).ToPagedList(orderQuery.PageNo, orderQuery.PageSize);

            var pageModel = new QueryPageModel<OrderInfo>()
            {
                Models = rets,
                Total = rets.TotalRecordCount
            };
            return pageModel;
        }

        public Dictionary<long, long> GetBuyCount(long memberId, List<long> product)
        {
            return DbFactory.Default.Get<OrderItemInfo>()
                .LeftJoin<OrderInfo>((i, o) => i.OrderId == o.Id)
                .Where(p => p.ProductId.ExIn(product))
                .Where<OrderInfo>(p => p.UserId == memberId && p.OrderStatus != OrderInfo.OrderOperateStatus.Close)
                .GroupBy(p => p.ProductId)
                .Select(p => new
                {
                    Item1 = p.ProductId,
                    ITem2 = p.Quantity.ExSum()
                }).ToList<SimpItem<long, long>>()
                .ToDictionary(k => k.Item1, v => v.Item2);
        }

        public List<OrderInfo> GetOrders(IEnumerable<long> ids)
        {
            return DbFactory.Default.Get<OrderInfo>().Where(item => item.Id.ExIn(ids)).ToList();
        }

        public OrderInfo GetOrder(long orderId, long userId, bool loadInvoiceInfo = false)
        {
            var result = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId && a.UserId == userId).FirstOrDefault();
            if (null == result)
            {
                var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
                if (flag)
                {
                    result = DbFactory.MongoDB.AsQueryable<OrderInfo>().Where(a => a.Id == orderId && a.UserId == userId).FirstOrDefault();
                }
            }
            if (result != null && result.OrderStatus >= OrderInfo.OrderOperateStatus.WaitDelivery)
            {
                CalculateOrderItemRefund(orderId);
            }
            if (loadInvoiceInfo)
            {
                result.OrderInvoice = GetOrderInvoiceInfo(orderId);
            }
            return result;
        }

        public OrderInfo GetOrder(long orderId)
        {
            var result = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (null == result)
            {
                var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
                if (flag)
                {
                    result = DbFactory.MongoDB.AsQueryable<OrderInfo>().FirstOrDefault(p => p.Id == orderId);
                }
            }
            return result;
        }

        public List<OrderPayInfo> GetOrderPay(long id)
        {
            return DbFactory.Default.Get<OrderPayInfo>().Where(item => item.PayId == id).ToList();
        }

        public List<OrderInfo> GetTopOrders(int top, long userId)
        {
            return DbFactory.Default.Get<OrderInfo>().Where(a => a.UserId == userId)
                .OrderByDescending(a => a.OrderDate).Take(top).ToList();
        }

        public List<OrderComplaintInfo> GetOrderComplaintByOrders(List<long> orders)
        {
            return DbFactory.Default.Get<OrderComplaintInfo>().Where(p => p.OrderId.ExIn(orders)).ToList();
        }

        public int GetOrderTotalProductCount(long order)
        {
            return DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == order).Sum<int>(p => p.Quantity);
        }

        /// <summary>
        /// 根据订单项id获取订单项
        /// </summary>
        /// <param name="orderItemIds"></param>
        /// <returns></returns>
        public List<OrderItemInfo> GetOrderItemsByOrderItemId(IEnumerable<long> orderItemIds)
        {
            var itemlist = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.Id.ExIn(orderItemIds)).ToList();
            foreach (var orderitem in itemlist)
            {
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
            return itemlist;
        }

        /// <summary>
        /// 根据订单id获取订单项
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public List<OrderItemInfo> GetOrderItemsByOrderId(long orderId)
        {
            var orderitems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == orderId).ToList();
            var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
            if (flag)
            {
                orderitems.AddRange(DbFactory.MongoDB.AsQueryable<OrderItemInfo>().Where(p => p.OrderId == orderId));
            }
            return orderitems;
        }


        public List<OrderOperationLogInfo> GetOrderLogs(long order)
        {
            return DbFactory.Default.Get<OrderOperationLogInfo>(p => p.OrderId == order).ToList();
        }
        /// <summary>
        /// 根据订单id获取订单项
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public List<OrderItemInfo> GetOrderItemsByOrderId(IEnumerable<long> orderIds)
        {
            var rets = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
            var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
            if (flag)
            {
                rets.AddRange(DbFactory.MongoDB.AsQueryable<OrderItemInfo>().Where(p => orderIds.Contains(p.OrderId)));
            }
            return rets;
        }

        /// <summary>
        /// 获取订单的评论数
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public Dictionary<long, long> GetOrderCommentCount(IEnumerable<long> orderIds)
        {
            return DbFactory.Default
                .Get<OrderCommentInfo>()
                .Where(p => p.OrderId.ExIn(orderIds))
                .GroupBy(p => p.OrderId)
                .Select(p => new { p.OrderId, total = p.ExCount(false) })
                .ToList<dynamic>()
                .ToDictionary<dynamic, long, long>(p => p.OrderId, p => p.total);
        }

        public SKUInfo GetSkuByID(string skuid)
        {
            return DbFactory.Default.Get<SKUInfo>().Where(p => p.Id == skuid).FirstOrDefault();
        }

        /// <summary>
        /// 根据订单项id获取售后记录
        /// </summary>
        /// <param name="orderItemIds"></param>
        /// <returns></returns>
        public List<OrderRefundInfo> GetOrderRefunds(IEnumerable<long> orderItemIds)
        {
            return DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.OrderItemId.ExIn(orderItemIds)).ToList();
        }

        /// <summary>
        /// 获取非限时购商品已购数
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="productIds"></param>
        /// <returns></returns>
        public Dictionary<long, long> GetProductBuyCountNotLimitBuy(long userId, IEnumerable<long> productIds)
        {
            return DbFactory.Default.Get<OrderItemInfo>()
                .Where(p => p.ProductId.ExIn(productIds) && p.IsLimitBuy == false)
                .LeftJoin<OrderInfo>((oii, oi) => oii.OrderId == oi.Id)
                .Where<OrderInfo>(p => p.UserId == userId && p.OrderStatus != OrderInfo.OrderOperateStatus.Close)
                .GroupBy(p => p.ProductId)
                .Select(p => new { p.ProductId, total = p.Quantity.ExSum() })
                .ToList<dynamic>()
                .ToDictionary<dynamic, long, long>(p => p.ProductId, p => (long)p.total);
        }

        public void CreateOrder(OrderCreating orderCreating)
        {
            var orders = new List<OrderInfo>();
            var orderItems = new List<OrderItemInfo>();
            var orderInvoices = new List<OrderInvoiceInfo>();
            var virtualItems = new List<VirtualOrderItemInfo>();
            foreach (var subOrder in orderCreating.SubOrders)
            {
                //生成订单号
                subOrder.OrderId = GenerateOrderNumber();
                var order = new OrderInfo
                {
                    Id = subOrder.OrderId,
                    MainOrderId = orders.FirstOrDefault()?.Id ?? subOrder.OrderId,
                    Platform = orderCreating.Platform,
                    OrderStatus = OrderInfo.OrderOperateStatus.WaitPay,
                    OrderDate = DateTime.Now,
                    ShopId = subOrder.ShopId,
                    ShopName = subOrder.ShopName,
                    ShopBranchId = subOrder.ShopBranchId,
                    UserId = orderCreating.Member.Id,
                    UserName = orderCreating.Member.UserName,
                    UserRemark = subOrder.Remark,
                    OrderRemarks = subOrder.Remark,
                    Freight = subOrder.Freight,
                    Tax = subOrder.Tax,
                    IntegralDiscount = subOrder.IntegralDiscount,
                    ProductTotalAmount = subOrder.Items.Sum(p => p.SalePrice * p.Quantity),
                    LastModifyTime = DateTime.Now,
                    DeliveryType = subOrder.DeliveryType,
                    CapitalAmount = subOrder.Capital,
                    PlatCouponId = subOrder.PlatformCouponId,
                    IsLive = subOrder.Items.Exists(i => i.RoomId > 0),
                    OrderType = subOrder.OrderType,
                    ActiveType = 0,
                    TotalAmount = subOrder.OrderAmount,
                };
                if (subOrder.BonusId > 0)
                {
                    order.CouponType = Entities.CouponType.ShopBonus;
                    order.CouponId = subOrder.BonusId;
                }

                if (subOrder.CouponId > 0)
                {
                    order.CouponType = Entities.CouponType.Coupon;
                    order.CouponId = subOrder.CouponId;
                }


                //货到付款
                if (subOrder.IsCashOnDelivery)
                {
                    order.PaymentType = OrderInfo.PaymentTypes.CashOnDelivery;
                    order.OrderStatus = OrderInfo.OrderOperateStatus.WaitDelivery;
                }

                if (orderCreating.Address != null)
                {
                    var address = orderCreating.Address;
                    order.ShipTo = address.ShipTo;
                    order.CellPhone = address.Phone;
                    order.RegionId = address.RegionId;
                    var service = Instance<RegionService>.Create;
                    var province = service.GetRegion(address.RegionId, Region.RegionLevel.Province);
                    order.TopRegionId = province.Id;
                    order.RegionFullName = address.RegionFullName;
                    order.Address = address.Address + ' ' + address.AddressDetail;
                    order.ReceiveLongitude = address.Longitude;
                    order.ReceiveLatitude = address.Latitude;
                }
                else
                {
                    order.ShipTo = orderCreating.Member.UserName;
                    order.CellPhone = orderCreating.Member.CellPhone;
                    order.RegionFullName = string.Empty;
                    order.Address = string.Empty;
                }
                var items = AutoMapper.Mapper.Map<List<OrderItemInfo>>(subOrder.Items);
                items.ForEach(item =>
                {
                    item.OrderId = subOrder.OrderId;
                    item.ShopId = subOrder.ShopId;
                    item.IsLimitBuy = item.FlashSaleId > 0;
                });
                order.CommisTotalAmount = items.Sum(p => FormatMonty((p.RealTotalPrice + p.PlatCouponDiscount) * p.CommisRate));
                if (subOrder.Invoice != null && subOrder.Invoice.InvoiceType != InvoiceType.None)
                {//发票相关
                    var invoice = AutoMapper.Mapper.Map<OrderInvoiceInfo>(subOrder.Invoice);
                    invoice.OrderId = subOrder.OrderId;
                    orderInvoices.Add(invoice);
                }
                if (subOrder.IsVirtual)
                {//虚拟订单
                    foreach (var item in subOrder.Items.SelectMany(p => p.VirtualItems))
                    {
                        virtualItems.Add(new VirtualOrderItemInfo
                        {
                            OrderId = subOrder.OrderId,
                            VirtualProductItemName = item.Name,
                            VirtualProductItemType = item.Type,
                            Content = item.Content,
                        });
                    }
                }

                order.DiscountAmount = items.Sum(p => p.CouponDiscount);
                order.FullDiscount = items.Sum(p => p.FullDiscount);
                order.PlatDiscountAmount = items.Sum(p => p.PlatCouponDiscount);
                orders.Add(order);
                orderItems.AddRange(items);
            }
            //插入订单
            DbFactory.Default.InTransaction(() =>
            {
                //订单
                DbFactory.Default.AddRange(orders);
                //订单项
                DbFactory.Default.AddRange(orderItems);
                if (virtualItems.Count > 0)
                {
                    foreach (var item in virtualItems)
                    {
                        var orderItem = orderItems.FirstOrDefault(p => p.OrderId == item.OrderId);
                        item.OrderItemId = orderItem.Id;
                    }
                    DbFactory.Default.Add(virtualItems);
                }
                //订单发票信息
                if (orderInvoices.Count > 0)
                    DbFactory.Default.Add(orderInvoices);
            });


        }
        private static decimal FormatMonty(decimal money) =>
          Math.Floor(money * 100) / 100M;

        /// <summary>
        /// 设置订单的商家优惠券金额分摊到每个子订单
        /// </summary>
        /// <param name="infos"></param>
        /// <param name="Coupon"></param>
        /// <param name="oneOrderCouponDiscount">当前订单所占平台券金额</param>
        /// <returns></returns>
        private void SetActualItemPrice(OrderInfo info)
        {
            var t = info.OrderItemInfo;
            if (t == null || t.Count < 1)
            {
                t = DbFactory.Default.Get<OrderItemInfo>().Where(a => a.OrderId == info.Id).ToList();
            }
            decimal couponDiscount = 0;
            decimal platCouponDiscount = 0;
            var num = t.Count();
            List<long> couponProducts = new List<long>();
            decimal coupontotal = 0;
            bool singleCal = false;
            if (info.CouponId > 0)
            {
                var coupon = DbFactory.Default.Get<CouponInfo>().Where(p => p.Id == info.CouponId).FirstOrDefault();
                if (coupon != null && coupon.UseArea == 1 && coupon.ShopId > 0)
                {
                    singleCal = true;
                    couponProducts = DbFactory.Default
                        .Get<CouponProductInfo>()
                        .Where(p => p.CouponId == coupon.Id)
                        .Select(p => p.ProductId)
                        .ToList<long>();
                    foreach (var p in t)
                    {
                        if (couponProducts.Contains(p.ProductId))
                        {
                            coupontotal += p.RealTotalPrice - p.FullDiscount;
                        }
                    }
                }
            }
            for (var i = 0; i < t.Count(); i++)
            {
                var _item = t[i];
                if (i < num - 1)
                {
                    if (singleCal)
                    {
                        if (couponProducts.Contains(_item.ProductId))
                            _item.CouponDiscount = Math.Round((_item.RealTotalPrice - _item.FullDiscount) / coupontotal * info.DiscountAmount, 2);
                    }
                    else
                    {
                        _item.CouponDiscount = GetItemCouponDisCount(_item.RealTotalPrice - _item.FullDiscount, info.ProductTotalAmount - info.FullDiscount, info.DiscountAmount);
                        // //平台券抵扣订单项金额
                        //_item.PlatCouponDiscount = GetItemCouponDisCount(_item.RealTotalPrice - _item.FullDiscount, info.ProductTotalAmount - info.FullDiscount, oneOrderCouponDiscount);
                    }
                    couponDiscount += _item.CouponDiscount;
                    platCouponDiscount += _item.PlatCouponDiscount;
                }
                else
                {
                    if ((singleCal && couponProducts.Contains(_item.ProductId)) || !singleCal)
                    {
                        _item.CouponDiscount = info.DiscountAmount - couponDiscount;
                        //订单项最后一项的平台券抵扣金额
                        //_item.PlatCouponDiscount = oneOrderCouponDiscount - platCouponDiscount;
                    }
                }
            }
        }

        public long SaveOrderPayInfo(IEnumerable<OrderPayInfo> model, Core.PlatformType platform)
        {
            //只有一个订单就取第一个订单号，否则生成一个支付订单号
            //var orderid = long.Parse(model.FirstOrDefault().OrderId.ToString() + ((int)platform).ToString());
            var payid = GetOrderPayId();
            DbFactory.Default
                .InTransaction(() =>
                {
                    foreach (var pay in model)
                    {
                        var orderPayInfo = DbFactory.Default.Get<OrderPayInfo>().Where(item => item.PayId == payid && item.OrderId == pay.OrderId).FirstOrDefault();
                        if (orderPayInfo == null)
                        {
                            orderPayInfo = new OrderPayInfo
                            {
                                OrderId = pay.OrderId,
                                PayId = payid
                            };
                            DbFactory.Default.Add(orderPayInfo);
                        }
                    }
                });
            return payid;
        }


        #region 订单操作 done
        public void CloseExpireTime(DateTime expireTime)
        {
            var orders = DbFactory.Default.Get<OrderInfo>().Where(a => a.OrderDate < expireTime && a.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay).ToList();
            foreach (var o in orders)
            {
                try
                {
                    bool needClose = true;
                    if (o.OrderType == OrderInfo.OrderTypes.FightGroup)
                    {
                        try
                        {
                            var ford = _FightGroupService.GetOrder(o.Id);
                            if (ford != null)
                            {
                                needClose = false;
                                _FightGroupService.OrderBuildFailed(ford);
                            }
                        }
                        catch (Exception ex)
                        {
                            //忽略错误
                            Log.Error($"订单关闭退出拼团异常:[{o.Id}]", ex);
                        }
                    }
                    if (needClose)
                    {
                        CloseOrder(o, "系统", "过期没付款，自动关闭");
                    }
                }
                catch (Exception ex)
                {
                    Log.Error($"订单自动关闭异常:{o.Id}", ex);
                }
            }
        }

        public void AutoConfirmOrder()
        {
            var noReceivingTimeout = 0;
            var expriedtemp = DbFactory.Default.Query<string>("Select `Value` from Himall_SiteSetting where `Key`='NoReceivingTimeout'").FirstOrDefault();
            int.TryParse(expriedtemp, out noReceivingTimeout);
            //退换货间隔天数
            int intIntervalDay = noReceivingTimeout == 0 ? 7 : noReceivingTimeout;
            DateTime waitReceivingDate = DateTime.Now.AddDays(-intIntervalDay);
            var orders = DbFactory.Default.Get<OrderInfo>().Where(a => a.ShippingDate < waitReceivingDate && a.OrderStatus == OrderInfo.OrderOperateStatus.WaitReceiving).ToList();
            var allItmes = DbFactory.Default.Get<OrderItemInfo>().Where(i => i.OrderId.ExIn(orders.Select(o => o.Id))).ToList();
            foreach (var o in orders)
            {
                try
                {
                    var set = DbFactory.Default.Set<OrderInfo>();

                    set.Set(p => p.OrderStatus, OrderInfo.OrderOperateStatus.Finish)
                        .Set(p => p.CloseReason, "完成过期未确认收货的订单")
                        .Set(p => p.FinishDate, DateTime.Now);
                    if (o.OrderType != OrderInfo.OrderTypes.Virtual)
                    {
                        var member = DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == o.UserId).FirstOrDefault();
                        AddIntegral(member, o.Id, o.TotalAmount - o.RefundTotalAmount);//增加积分
                    }
                    if (o.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
                    {
                        set.Set(p => p.PayDate, DateTime.Now);
                        UpdateProductVisti(allItmes.Where(i => i.OrderId == o.Id).ToList());
                    }
                    set.Where(p => p.Id == o.Id).Succeed();
                    #region 更新待结算订单完成时间
                    UpdatePendingSettlnmentFinishDate(o.Id, DateTime.Now);
                    #endregion

                }
                catch (Exception ex)
                {
                    Log.Error($"自动确认订单异常:{o.Id}", ex);
                }
            }



        }

        // 商家发货
        public OrderInfo SellerSendGood(long orderId, string sellerName, string companyName, string shipOrderNumber)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery)
            {
                throw new HimallException("只有待发货状态的订单才能发货");
            }
            if (!CanSendGood(orderId))
            {
                throw new HimallException("拼团完成后订单才可以发货");
            }
            order.OrderStatus = OrderInfo.OrderOperateStatus.WaitReceiving;
            order.ExpressCompanyName = companyName;
            order.ShipOrderNumber = shipOrderNumber;
            order.ShippingDate = DateTime.Now;
            order.LastModifyTime = DateTime.Now;

            //处理订单退款
            var refund = DbFactory.Default
                .Get<OrderRefundInfo>()
                .Where(d => d.OrderId == orderId && d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund
                    && d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                .FirstOrDefault();
            if (refund != null)
            {
                //自动拒绝退款申请
                ServiceProvider.Instance<RefundService>.Create.SellerDealRefund(refund.Id, OrderRefundInfo.OrderRefundAuditStatus.UnAudit, "商家已发货", sellerName);
            }
            DbFactory.Default.Update(order);

            AddOrderOperationLog(orderId, sellerName, "商家发货");

            return order;
        }

        /// <summary>
        /// 门店发货
        /// </summary>
        /// <param name="orderId">订单号</param>
        /// <param name="deliveryType">配送方式（2店员配送或1快递配送）</param>
        /// <param name="shopkeeperName">发货人（门店管理员账号名称）</param>
        /// <param name="companyName">快递公司</param>
        /// <param name="shipOrderNumber">快递单号</param>
        /// <returns></returns>
        public OrderInfo ShopSendGood(long orderId, int deliveryType, string shopkeeperName, string companyName, string shipOrderNumber)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery)
            {
                throw new HimallException("只有待发货状态的订单才能发货");
            }
            if (!CanSendGood(orderId))
            {
                throw new HimallException("拼团完成后订单才可以发货");
            }
            order.OrderStatus = OrderInfo.OrderOperateStatus.WaitReceiving;
            if (deliveryType == 2)
            {
                order.DeliveryType = CommonModel.DeliveryType.ShopStore;
            }
            else if (deliveryType == CommonModel.DeliveryType.CityExpress.GetHashCode())
            {
                order.DeliveryType = CommonModel.DeliveryType.CityExpress;
                order.DadaStatus = DadaStatus.WaitOrder.GetHashCode();
            }
            else
            {
                order.DeliveryType = CommonModel.DeliveryType.Express;
            }
            order.ExpressCompanyName = companyName;
            order.ShipOrderNumber = shipOrderNumber;
            order.ShippingDate = DateTime.Now;
            order.LastModifyTime = DateTime.Now;

            //处理订单退款
            var refund = DbFactory.Default
                .Get<OrderRefundInfo>()
                .Where(d => d.OrderId == orderId && d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund &&
                    d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                .FirstOrDefault();

            if (refund != null)
            {
                //自动拒绝退款申请
                ServiceProvider.Instance<RefundService>.Create.SellerDealRefund(refund.Id, OrderRefundInfo.OrderRefundAuditStatus.UnAudit, "门店已发货", shopkeeperName);
            }

            DbFactory.Default.Update(order);

            AddOrderOperationLog(orderId, shopkeeperName, "门店发货");

            return order;
        }

        /// <summary>
        /// 判断订单是否在申请售后
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public bool IsOrderAfterService(long orderId)
        {
            var refund = DbFactory.Default
                .Get<OrderRefundInfo>()
                .Where(d => d.OrderId == orderId && d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund &&
                    d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                .Exist();
            return refund;
        }

        /// <summary>
        /// 修改快递信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="companyName"></param>
        /// <param name="shipOrderNumber"></param>
        /// <returns></returns>
        public OrderInfo UpdateExpress(long orderId, string companyName, string shipOrderNumber)
        {
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();

            order.ExpressCompanyName = companyName;
            order.ShipOrderNumber = shipOrderNumber;
            order.ShippingDate = DateTime.Now;
            order.LastModifyTime = DateTime.Now;

            DbFactory.Default.Update(order);

            return order;
        }

        // 商家更新收货地址
        public void SellerUpdateAddress(long orderId, string sellerName, string shipTo, string cellPhone, int topRegionId, int regionId, string regionFullName, string address)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery)
            {
                throw new HimallException("只有待付款或待发货状态的订单才能修改收货地址");
            }

            order.ShipTo = shipTo;
            order.CellPhone = cellPhone;
            order.TopRegionId = topRegionId;
            order.RegionId = regionId;
            order.RegionFullName = regionFullName;
            order.Address = address;
            DbFactory.Default.Update(order);
            AddOrderOperationLog(orderId, sellerName, "商家修改订单的收货地址");
        }

        // 会员确认订单
        public void MembeConfirmOrder(long orderId, string memberName)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId && a.UserName == memberName).FirstOrDefault();

            if (order.OrderStatus == OrderInfo.OrderOperateStatus.Finish)
            {
                throw new HimallException("该订单已经确认过!");
            }
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitReceiving && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
            {
                throw new HimallException("订单状态发生改变，请重新刷页面操作!");
            }
            DbFactory.Default
                .InTransaction(() =>
                {
                    var orderItems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == order.Id).ToList();

                    this.SetStateToConfirm(order);
                    order.LastModifyTime = DateTime.Now;
                    order.DadaStatus = DadaStatus.Finished.GetHashCode();
                    if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
                    {//货到付款的订单，在会员确认收货时，计算实付金额
                        order.ActualPayAmount = order.OrderTotalAmount;
                    }

                    DbFactory.Default.Update(order);

                    //会员确认收货后，不会马上给积分，得需要过了售后维权期才给积分,（虚拟商品除外）
                    if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                    {
                        var member = DbFactory.Default.Get<MemberInfo>().Where(a => a.UserName == memberName).FirstOrDefault();
                    }
                    AddOrderOperationLog(orderId, memberName, "会员确认收货");
                    UpdateProductVistiOrderCount(orderId);

                    if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
                    {
                        UpdateProductVisti(orderItems);
                        ServiceProvider.Instance<DistributionService>.Create.TreatedOrderPaidDistribution(order.Id);
                        //货到付款订单，在确认时，写入待结算
                        WritePendingSettlnment(order);
                    }
                    else
                    {
                        //更新待结算订单完成时间
                        UpdatePendingSettlnmentFinishDate(orderId, DateTime.Now);
                    }
                });
        }
        /// <summary>
        /// 门店核销订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="managerName"></param>
        public void ShopBranchConfirmOrder(long orderId, long shopBranchId, string managerName)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId && a.ShopBranchId == shopBranchId).FirstOrDefault();
            if (order == null)
            {
                throw new HimallException("处理订单错误，请确认参数正确");
            }
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
            {
                throw new HimallException("只有待自提状态的订单才能进行核销操作");
            }
            var result = DbFactory.Default
                .InTransaction(() =>
                {
                    if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
                    {
                        order.PayDate = DateTime.Now;
                    }
                    var currDate = DateTime.Now;
                    order.OrderStatus = OrderInfo.OrderOperateStatus.Finish;
                    order.FinishDate = currDate;
                    order.LastModifyTime = currDate;
                    DbFactory.Default.Update(order);

                    //会员确认收货后，不会马上给积分，得需要过了售后维权期才给积分(虚拟商品除外)
                    if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                    {
                        var member = DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == order.UserId).FirstOrDefault();
                    }
                    AddOrderOperationLog(orderId, managerName, "门店核销订单");
                    UpdateProductVistiOrderCount(orderId);

                    if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
                    {
                        var orderItems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == order.Id).ToList();
                        UpdateProductVisti(orderItems);
                        Instance<DistributionService>.Create.TreatedOrderPaidDistribution(order.Id);
                        //货到付款的订单，在确认时，写入待结算
                        WritePendingSettlnment(order);
                    }
                    else
                    {
                        //更新待结算订单完成时间
                        UpdatePendingSettlnmentFinishDate(orderId, currDate);
                    }
                    //处理订单退款
                    var refund = DbFactory.Default
                        .Get<OrderRefundInfo>()
                        .Where(d => d.OrderId == orderId && d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund &&
                            d.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit)
                        .ToList();
                    if (refund != null && refund.Count > 0)
                    {
                        foreach (var item in refund)
                        {
                            //自动拒绝退款申请
                            ServiceProvider.Instance<RefundService>.Create.SellerDealRefund(item.Id, OrderRefundInfo.OrderRefundAuditStatus.UnAudit, "门店已发货", managerName);
                        }
                    }

                });

            //发送消息
            if (result)
            {
                var orderItem = DbFactory.Default.Get<OrderItemInfo>(p => p.OrderId == order.Id).FirstOrDefault();
                //新增微信短信邮件消息推送
                var orderMessage = new MessageOrderInfo();
                orderMessage.UserName = order.UserName;
                orderMessage.OrderId = order.Id.ToString();
                orderMessage.ShopId = order.ShopId;
                orderMessage.ShopName = order.ShopName;
                if (order.ShopBranchId > 0)
                {
                    var shopbranch = DbFactory.Default.Get<ShopBranchInfo>(s => s.Id == order.ShopBranchId).FirstOrDefault();
                    if (shopbranch != null)
                        orderMessage.ShopName = shopbranch.ShopBranchName;
                }
                orderMessage.SiteName = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                orderMessage.TotalMoney = order.OrderTotalAmount;
                orderMessage.ProductName = orderItem.ProductName;
                orderMessage.RefundAuditTime = DateTime.Now;
                orderMessage.PickupCode = order.PickupCode;
                orderMessage.FinishDate = order.FinishDate.Value;
                if (order.Platform == PlatformType.WeiXinSmallProg)
                {
                    orderMessage.MsgOrderType = MessageOrderType.Applet;
                }
                Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnAlreadyVerification(order.UserId, orderMessage));

            }
        }
        // 平台关闭订单
        public void PlatformCloseOrder(long orderId, string managerName, string closeReason = "")
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(closeReason))
            {
                closeReason = "平台取消订单";
            }
            CloseOrder(order, managerName, closeReason);
        }

        // 商家关闭订单
        public void SellerCloseOrder(long orderId, string sellerName)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (order == null)
            {
                throw new HimallException("错误的订单编号！");
            }
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                throw new HimallException("拼团订单，不可以手动取消！");
            }
            CloseOrder(order, sellerName, "商家取消订单");
        }

        /// <summary>
        /// 会员关闭订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="memberName"></param>
        public void MemberCloseOrder(long orderId, string memberName)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId && a.UserName == memberName).FirstOrDefault();

            if (order == null)
            {
                throw new HimallException("该订单不属于该用户！");
            }

            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                throw new HimallException("拼团订单，不可以手动取消！");
            }
            CloseOrder(order, memberName, "会员取消订单");
        }
        /// <summary>
        /// 关闭订单操作
        /// </summary>
        /// <param name="order"></param>
        /// <param name="managerName"></param>
        /// <param name="closeReason"></param>
        private void CloseOrder(OrderInfo order, string managerName, string closeReason = "")
        {
            CheckCloseOrder(order);
            DbFactory.Default
                .InTransaction(() =>
                {
                    ReturnStock(order);
                    DbFactory.Default.Set<OrderInfo>()
                    .Where(d => d.Id == order.Id)
                    .Set(d => d.CloseReason, closeReason)
                    .Set(d => d.OrderStatus, OrderInfo.OrderOperateStatus.Close)
                    .Set(d => d.LastModifyTime, DateTime.Now)
                    .Succeed();
                    order.OrderStatus = OrderInfo.OrderOperateStatus.Close;
                    order.LastModifyTime = DateTime.Now;
                    order.CloseReason = closeReason;
                    //分销处理
                    ServiceProvider.Instance<DistributionService>.Create.RemoveBrokerageByOrder(order.Id);
                    CancelIntegral(order.UserId, order.UserName, order.Id, order.IntegralDiscount);  //取消订单增加积分
                    if (order.CapitalAmount > 0)
                    {
                        CancelCapital(order.UserId, order.Id, order.CapitalAmount);//退回预存款
                    }

                    if (order.CouponId > 0)
                    {
                        if (order.CouponType.HasValue)
                        {
                            if (order.CouponType.Value == Entities.CouponType.Coupon)
                                ReturnCoupon(order.UserId, order.Id, order.CouponId);//退回优惠券
                            else
                                ReturnShopBonus(order.UserId, order.Id, order.CouponId);//退回带金红包
                        }

                    }
                    if (order.PlatCouponId > 0 && order.PlatDiscountAmount > 0)
                    {
                        ReturnPlatCoupon(order.UserId, order);
                    }
                    AddOrderOperationLog(order.Id, managerName, closeReason);
                });
        }
        /// <summary>
        /// 是否超过售后期
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns>true 不可售后 false 可以售后</returns>
        public bool IsRefundTimeOut(long orderId)
        {
            var order = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId).FirstOrDefault();
            return IsRefundTimeOut(order);
        }

        public bool IsRefundTimeOut(OrderInfo order)
        {
            var result = true;
            if (order != null)
            {
                result = false;   //默认可以售后
                switch (order.OrderStatus)
                {
                    case OrderInfo.OrderOperateStatus.Close:
                        result = true;
                        break;

                        //case OrderInfo.OrderOperateStatus.CloseByUser:
                        //    result = true;
                        //    break;
                }
                if (order.OrderStatus == OrderInfo.OrderOperateStatus.Finish)
                {
                    result = false;
                    if (order.FinishDate.HasValue)
                    {
                        DateTime EndSalesReturn = order.FinishDate.Value.AddDays(ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SalesReturnTimeout);
                        if (EndSalesReturn <= DateTime.Now)
                        {
                            result = true;
                        }
                    }
                }
            }
            return result;
        }


        // 设置订单快递信息
        public void SetOrderExpressInfo(long shopId, string expressName, string startCode, IEnumerable<long> orderIds)
        {
            var express = DbFactory.Default.Get<ExpressInfoInfo>().Where(e => e.Name.Contains(expressName)).FirstOrDefault();
            if (express == null)
            {
                throw new HimallException("快递公司不存在");
            }
            if (!express.CheckExpressCodeIsValid(startCode))
            {
                throw new HimallException("起始快递单号格式不正确");
            }

            var orders = DbFactory.Default.Get<OrderInfo>().Where(item => item.ShopId == shopId && item.Id.ExIn(orderIds)).ToList();
            var orderedOrders = orderIds.Select(item => orders.FirstOrDefault(t => item == t.Id)).Where(item => item != null);

            int i = 0;
            string code = string.Empty;
            var shopShipper = DbFactory.Default.Get<ShopShipperInfo>().Where(e => e.ShopId == shopId && e.IsDefaultSendGoods == true).FirstOrDefault();
            if (shopShipper == null)
            {
                throw new HimallException("未设置默认发货地址");
            }
            string sendFullAddress = ServiceProvider.Instance<RegionService>.Create.GetFullName(shopShipper.RegionId) + " " + shopShipper.Address;

            DbFactory.Default
                .InTransaction(() =>
                {
                    foreach (var order in orderedOrders)
                    {
                        if (i++ == 0)
                        {
                            code = startCode;
                        }
                        else
                        {
                            code = express.NextExpressCode(expressName, code);
                        }
                        order.ShipOrderNumber = code;
                        order.ExpressCompanyName = express.Name;
                        order.SellerPhone = shopShipper.TelPhone;
                        order.SellerAddress = sendFullAddress;
                        DbFactory.Default.Update(order);
                    }
                });
        }
        /// <summary>
        /// 设置订单商家备注
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="mark"></param>
        public void SetOrderSellerRemark(long orderId, string mark)
        {
            var orderdata = DbFactory.Default.Get<OrderInfo>().Where(d => d.Id == orderId).FirstOrDefault();
            if (orderdata == null)
            {
                throw new HimallException("错误的订单编号");
            }
            orderdata.SellerRemark = mark;
            DbFactory.Default.Update(orderdata);
        }

        //商家更新金额
        public void SellerUpdateItemDiscountAmount(long orderItemId, decimal discountAmount, string sellerName)
        {
            OrderItemInfo item = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.Id == orderItemId).FirstOrDefault();
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == item.OrderId).FirstOrDefault();
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                throw new HimallException("拼团订单不可以改价");
            }

            item.DiscountAmount += discountAmount;
            item.RealTotalPrice = this.GetRealTotalPrice(order, item, discountAmount);
            if ((order.OrderTotalAmount - order.CapitalAmount - order.Freight - discountAmount) <= 0)
            {
                throw new HimallException("优惠金额异常，改价不可以使订单为零元或负数订单！");
            }
            DbFactory.Default
                .InTransaction(() =>
                {
                    DbFactory.Default.Update(item);
                    item = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.Id == orderItemId).FirstOrDefault();
                    order.ProductTotalAmount = order.ProductTotalAmount - discountAmount;
                    order.TotalAmount = order.OrderTotalAmount;
                    order.CommisTotalAmount = DbFactory.Default.Get<OrderItemInfo>().Where(i => i.OrderId == item.OrderId).Sum(i => (i.RealTotalPrice + i.PlatCouponDiscount) * i.CommisRate);

                    SetActualItemPrice(order);         //平摊订单的优惠券金额
                    order.LastModifyTime = DateTime.Now;
                    DbFactory.Default.Update(order);

                    AddOrderOperationLog(item.OrderId, sellerName, "商家修改订单商品的优惠金额");
                });
        }

        public void SellerUpdateOrderFreight(long orderId, decimal freight, string sellerName)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                throw new HimallException("拼团订单不可以改价");
            }
            this.SetFreight(order, freight);
            order.LastModifyTime = DateTime.Now;
            DbFactory.Default.Update(order);
            AddOrderOperationLog(order.Id, sellerName, "商家修改订单运费金额：" + freight + "元");
        }

        // 平台确认订单支付
        public void PlatformConfirmOrderPay(long orderId, string payRemark, string managerName, WDTConfigModel configModel)
        {
            OrderInfo order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay)
            {
                throw new HimallException("只有待付款状态的订单才能进行收款操作");
            }
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                if (!_FightGroupService.OrderCanPay(orderId))
                {
                    throw new HimallException("拼团订单的状态为不可收款状态");
                }
            }
            PaySucceed(new List<long> { orderId }, PAY_BY_OFFLINE_PAYMENT_ID, DateTime.Now, configModel, paymentType: OrderInfo.PaymentTypes.Offline, payRemark: payRemark);

            AddOrderOperationLog(orderId, managerName, "平台确认收到订单货款");
        }

        // 订单支付成功
        public void PaySucceed(IEnumerable<long> orderIds, string paymentId, DateTime payTime, WDTConfigModel wDTConfigModel
            , string payNo = null
            , long payId = 0, OrderInfo.PaymentTypes paymentType = OrderInfo.PaymentTypes.Online, string payRemark = "")
        {

            var orders = DbFactory.Default.Get<OrderInfo>().Where(item => item.Id.ExIn(orderIds)).ToList();

            string PaymentTypeName = paymentId;
            bool isOnlinePay = paymentType == OrderInfo.PaymentTypes.Online;
            bool isPlugPay = isOnlinePay
                && paymentId != PAY_BY_CAPITAL_PAYMENT_ID
                && paymentId != PAY_BY_INTEGRAL_PAYMENT_ID
                && paymentId != PAY_BY_OFFLINE_PAYMENT_ID;
            if (isPlugPay)
            {
                var payment = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(paymentId);
                PaymentTypeName = payment.PluginInfo.DisplayName;
            }
            var orderItems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
            var invoiceItems = DbFactory.Default.Get<OrderInvoiceInfo>().Where(i => i.OrderId.ExIn(orderIds)).ToList();
            if (invoiceItems == null) { invoiceItems = new List<OrderInvoiceInfo>(); }
            var isSendMsg = false;
            Dictionary<long, decimal> shopTotalMoney = new Dictionary<long, decimal>();//每个订单对应商家总额
            foreach (var order in orders)
            {
                order.OrderInvoice = invoiceItems.Where(i => i.OrderId == order.Id).FirstOrDefault();
                //不是预存款支付，并且订单是使用预存款支付的则不进行操作
                if (paymentId != "预存款支付" && (order.OrderAmount - order.IntegralDiscount - order.CapitalAmount) <= 0)
                {
                    continue;
                }
                if (!shopTotalMoney.Keys.Contains(order.ShopId))
                    shopTotalMoney.Add(order.ShopId, order.TotalAmount);
                //判断货到付款订单是否预存款全额抵扣
                var isCash = order.CapitalAmount >= order.TotalAmount && order.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery;
                if (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay || isCash)
                {
                    var orderPayInfo = DbFactory.Default.Get<OrderPayInfo>().Where(item => item.OrderId == order.Id && item.PayId == payId).FirstOrDefault();
                    DbFactory.Default
                         .InTransaction(() =>
                         {
                             order.PayDate = payTime;
                             if (order.OrderTotalAmount == 0 && order.CapitalAmount == 0)
                             {
                                 order.PaymentTypeName = PAY_BY_INTEGRAL_PAYMENT_ID;
                             }
                             else
                             {
                                 order.PaymentTypeName = PaymentTypeName;
                             }
                             order.PaymentType = paymentType;
                             if (isPlugPay)
                             {
                                 order.PaymentTypeGateway = paymentId;
                             }

                             if (order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                             {
                                 OperaOrderPickupCode(order);
                             }
                             else
                             {
                                 order.OrderStatus = OrderInfo.OrderOperateStatus.WaitDelivery;
                             }

                             if (orderPayInfo != null)
                             {
                                 orderPayInfo.PayState = true;
                                 orderPayInfo.PayTime = payTime;
                             }

                             //设置实收金额=实付金额
                             order.ActualPayAmount = order.TotalAmount;
                             order.GatewayOrderId = payNo;
                             order.PayRemark = payRemark;
                             order.LastModifyTime = DateTime.Now;

                             //  SetActualItemPrice(order);         //平摊订单的优惠券金额
                             UpdateShopVisti(order);               // 修改店铺销量
                             UpdateProductVisti(orderItems.Where(p => p.OrderId == order.Id));           // 修改商品销量
                             UpdateLimitTimeBuyLog(orderItems.Where(p => p.OrderId == order.Id));   // 修改限时购销售数量
                             if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                             {
                                 order.OrderStatus = OrderInfo.OrderOperateStatus.WaitVerification;//虚拟订单付款后，则为待消费
                             }
                             DbFactory.Default.Update(order);
                             if (orderPayInfo != null)
                             {
                                 DbFactory.Default.Update(orderPayInfo);
                             }
                             if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                             {
                                 var orderItemInfo = orderItems.Where(p => p.OrderId == order.Id).FirstOrDefault();//虚拟订单项只有一个
                                 UpdateOrderItemEffectiveDateByIds(orderItems.Where(p => p.OrderId == order.Id).Select(a => a.Id).ToList(), order.PayDate.Value);
                                 if (orderItemInfo != null)
                                 {
                                     AddOrderVerificationCodeInfo(orderItemInfo.Quantity, orderItemInfo.OrderId, orderItemInfo.Id);
                                     SendMessageOnVirtualOrderPay(order, orderItemInfo.ProductId);
                                 }
                             }
                         });
                    var firstOrderItem = orderItems.First(p => p.OrderId == order.Id);
                    order.OrderItemInfo = orderItems;
                    PaySuccessed_SingleOrderOp(order, firstOrderItem.ProductName, PaymentTypeName, "已付款", payTime, wDTConfigModel);
                    isSendMsg = true;
                }
            }

            if (isSendMsg)
            {
                List<long> shopIds = orders.Select(o => o.ShopId).ToList();
                List<long> sendShopIds = new List<long>();
                foreach (long shopId in shopIds)
                {
                    if (sendShopIds.Contains(shopId))
                    {
                        continue;
                    }
                    List<long> shopOrderIds = orders.Where(o => o.ShopId == shopId).Select(o => o.Id).ToList();

                    ServiceProvider.Instance<ShopService>.Create.OrderPaySendMsgToShop(shopId, string.Format("您的店铺有订单已支付，订单号：{0}，请及时发货!", string.Join(",", shopOrderIds)));
                    sendShopIds.Add(shopId);
                }
                var firstOrder = orders.FirstOrDefault();
                if (firstOrder != null)
                {
                    var userId = firstOrder.UserId;
                    var orderItem = orderItems.FirstOrDefault(e => e.OrderId == firstOrder.Id); ;

                    //发送通知消息
                    var orderMessage = new MessageOrderInfo();
                    orderMessage.OrderId = string.Join(",", orderIds);
                    orderMessage.ShopId = 0;
                    orderMessage.ShopIds = orders.Select(o => o.ShopId).ToList<long>();
                    orderMessage.SiteName = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                    orderMessage.TotalMoney = orders.Sum(a => a.OrderTotalAmount);
                    orderMessage.PaymentType = PaymentTypeName;

                    orderMessage.OrderTime = firstOrder.OrderDate;
                    orderMessage.PayTime = payTime;
                    orderMessage.PaymentType = "已付款";
                    orderMessage.ProductName = orderItem != null ? orderItem.ProductName : "";
                    orderMessage.UserName = firstOrder.UserName;
                    orderMessage.ShopTotalMoney = shopTotalMoney;//每个订单对应商家总额


                    if (firstOrder != null && firstOrder.Platform == PlatformType.WeiXinSmallProg)
                    {
                        orderMessage.MsgOrderType = MessageOrderType.Applet;
                    }
                    if (firstOrder.DeliveryType == DeliveryType.SelfTake && firstOrder.ShopBranchId > 0)
                    {
                        orderMessage.PickupCode = firstOrder.PickupCode;
                        var shopbranch = DbFactory.Default.Get<ShopBranchInfo>().Where(s => s.Id == firstOrder.ShopBranchId).FirstOrDefault();
                        var address = firstOrder.Address;
                        if (shopbranch != null)
                            address = ServiceProvider.Instance<RegionService>.Create.GetFullName(shopbranch.AddressId) + " " + shopbranch.AddressDetail;
                        orderMessage.ShopBranchAddress = address;
                        if (firstOrder.OrderType != OrderInfo.OrderTypes.FightGroup)
                            Task.Factory.StartNew(() => Instance<MessageService>.Create.SendMessageOnSelfTakeOrderPay(userId, orderMessage));
                    }
                    else
                    {
                        Task.Factory.StartNew(() => Instance<MessageService>.Create.SendMessageOnOrderPay(userId, orderMessage));
                        if (firstOrder.OrderType != OrderInfo.OrderTypes.Virtual)
                        {
                            //发送给商家
                            Task.Factory.StartNew(() => Instance<MessageService>.Create.SendMessageOnShopOrderShipping(orderMessage));
                        }
                    }
                }
            }
        }

        public void PayCapital(IEnumerable<long> orderIds, WDTConfigModel setting, string payNo = null, long payId = 0)
        {
            Log.Info("PayCapital597进入");
            var orders = DbFactory.Default.Get<OrderInfo>().Where(item => item.Id.ExIn(orderIds)).ToList();
            var totalAmount = orders.Sum(e => e.OrderTotalAmount - e.CapitalAmount);
            var userid = orders.FirstOrDefault().UserId;
            var capital = DbFactory.Default.Get<CapitalInfo>().Where(e => e.MemId == userid).FirstOrDefault();
            if (capital == null)
            {
                throw new HimallException("预存款金额少于订单金额");
            }
            if (capital.Balance < totalAmount)
            {
                throw new HimallException("预存款金额少于订单金额");
            }
            var orderItems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
            Dictionary<long, decimal> shopTotalMoney = new Dictionary<long, decimal>();//每个订单对应商家总额
            var invoiceItems = DbFactory.Default.Get<OrderInvoiceInfo>().Where(item => item.OrderId.ExIn(orderIds)).ToList();
            if (invoiceItems == null) { invoiceItems = new List<OrderInvoiceInfo>(); }
            foreach (var order in orders)
            {
                order.OrderInvoice = invoiceItems.Where(i => i.OrderId == order.Id).FirstOrDefault();
                if (!shopTotalMoney.Keys.Contains(order.ShopId))
                    shopTotalMoney.Add(order.ShopId, order.TotalAmount);
                var needPay = order.TotalAmount - order.CapitalAmount;
                if (order != null && (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay))
                {
                    var orderPayInfo = DbFactory.Default.Get<OrderPayInfo>().Where(item => item.OrderId == order.Id && item.PayId == payId).FirstOrDefault();
                    if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
                    {
                        if (!_FightGroupService.OrderCanPay(order.Id))
                        {
                            throw new HimallException("拼团订单的状态为不可付款状态");
                        }
                    }
                    CapitalDetailInfo detail = new CapitalDetailInfo()
                    {
                        Amount = -needPay,
                        CapitalID = capital.Id,
                        CreateTime = DateTime.Now,
                        SourceType = CapitalDetailInfo.CapitalDetailType.Consume,
                        SourceData = order.Id.ToString(),
                        Id = this.GenerateOrderNumber()
                    };
                    DbFactory.Default
                        .InTransaction(() =>
                        {
                            order.PayDate = DateTime.Now;
                            order.PaymentTypeGateway = string.Empty;
                            if (order.OrderTotalAmount == 0 && order.CapitalAmount == 0)
                            {
                                order.PaymentTypeName = PAY_BY_INTEGRAL_PAYMENT_ID;
                            }
                            else
                            {
                                order.PaymentTypeName = PAY_BY_CAPITAL_PAYMENT_ID;
                            }
                            order.PaymentType = OrderInfo.PaymentTypes.Online;
                            if (order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                            {
                                OperaOrderPickupCode(order);
                            }
                            else
                                order.OrderStatus = OrderInfo.OrderOperateStatus.WaitDelivery;
                            //设置实收金额=实付金额
                            order.ActualPayAmount += needPay;
                            order.LastModifyTime = DateTime.Now;
                            if (orderPayInfo != null)
                            {
                                orderPayInfo.PayState = true;
                                orderPayInfo.PayTime = DateTime.Now;
                            }
                            capital.Balance -= needPay;
                            DbFactory.Default.Add(detail);
                            //    SetActualItemPrice(order);         //平摊订单的优惠券金额
                            UpdateShopVisti(order);               // 修改店铺销量
                            UpdateProductVisti(orderItems.Where(p => p.OrderId == order.Id));           // 修改商品销量
                            UpdateLimitTimeBuyLog(orderItems.Where(p => p.OrderId == order.Id));   // 修改限时购销售数量
                            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                            {
                                order.OrderStatus = OrderInfo.OrderOperateStatus.WaitVerification;//虚拟订单付款后，则为待消费
                            }
                            DbFactory.Default.Update(order);
                            DbFactory.Default.Update(orderPayInfo);
                            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                            {
                                var orderItemInfo = orderItems.Where(p => p.OrderId == order.Id).FirstOrDefault();//虚拟订单项只有一个
                                UpdateOrderItemEffectiveDateByIds(orderItems.Where(p => p.OrderId == order.Id).Select(a => a.Id).ToList(), order.PayDate.Value);
                                if (orderItemInfo != null)
                                {
                                    AddOrderVerificationCodeInfo(orderItemInfo.Quantity, orderItemInfo.OrderId, orderItemInfo.Id);
                                    SendMessageOnVirtualOrderPay(order, orderItemInfo.ProductId);
                                }
                            }

                            var firstOrderItem = orderItems.First(p => p.OrderId == order.Id);
                            order.OrderItemInfo = orderItems;
                            PaySuccessed_SingleOrderOp(order, firstOrderItem.ProductName, PAY_BY_CAPITAL_PAYMENT_ID, "已付款", DateTime.Now, setting);
                        });
                }
            }
            var orderFirst = orders.FirstOrDefault();
            if (orderFirst != null)
            {
                //发送通知消息
                var orderMessage = new MessageOrderInfo();
                orderMessage.OrderId = string.Join(",", orderIds);
                orderMessage.OrderTime = orders.FirstOrDefault().OrderDate;
                orderMessage.ShopId = 0;
                orderMessage.ShopIds = orders.Select(o => o.ShopId).ToList<long>();
                orderMessage.SiteName = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
                orderMessage.TotalMoney = orders.Sum(a => a.OrderTotalAmount);
                orderMessage.UserName = orders.FirstOrDefault().UserName;
                orderMessage.PaymentType = PAY_BY_CAPITAL_PAYMENT_ID;
                orderMessage.PayTime = DateTime.Now;
                orderMessage.PaymentType = "已付款";
                orderMessage.ProductName = orderItems.First().ProductName;
                orderMessage.ShopTotalMoney = shopTotalMoney;//每个订单对应商家总额
                var userId = orders.FirstOrDefault().UserId;
                if (orders.FirstOrDefault().Platform == PlatformType.WeiXinSmallProg)
                {
                    orderMessage.MsgOrderType = MessageOrderType.Applet;
                }
                var firstOrder = orders.FirstOrDefault();
                if (firstOrder.DeliveryType == DeliveryType.SelfTake && firstOrder.ShopBranchId > 0)
                {
                    orderMessage.PickupCode = firstOrder.PickupCode;
                    var shopbranch = DbFactory.Default.Get<ShopBranchInfo>().Where(s => s.Id == firstOrder.ShopBranchId).FirstOrDefault();
                    var address = firstOrder.Address;
                    if (shopbranch != null)
                        address = Himall.ServiceProvider.Instance<RegionService>.Create.GetFullName(shopbranch.AddressId) + " " + shopbranch.AddressDetail;
                    orderMessage.ShopBranchAddress = address;
                    if (firstOrder.OrderType != OrderInfo.OrderTypes.FightGroup)
                        Task.Factory.StartNew(() => Instance<MessageService>.Create.SendMessageOnSelfTakeOrderPay(userId, orderMessage));
                }
                else
                {
                    Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnOrderPay(userId, orderMessage));
                    if (orderFirst.OrderType != OrderInfo.OrderTypes.Virtual)
                    {
                        //发送给商家
                        Task.Factory.StartNew(() => Instance<MessageService>.Create.SendMessageOnShopOrderShipping(orderMessage));
                    }
                }
            }
        }

        private void PaySuccessed_SingleOrderOp(OrderInfo order, string productName, string paymentType, string paymentStatus, DateTime payTime, WDTConfigModel model)
        {
            //拼团成功
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                _FightGroupService.ExecPaid(order.Id);
            }
            //处理分销分佣
            Instance<DistributionService>.Create.TreatedOrderPaidDistribution(order.Id);

            if (order.PaymentType != OrderInfo.PaymentTypes.CashOnDelivery)
            {//写入待结算
                WritePendingSettlnment(order);
            }
            if (order.IsLive)
            {
                // 处理直播统计
                HandleLiveStatiscts(order);
            }


            //发布付款成功消息
            if (OnOrderPaySuccessed != null)
            {
                OnOrderPaySuccessed(order.Id);
            }
            else
            {
                //没有事件，则直接执行
                try
                {
                    var ser_member = ServiceProvider.Instance<MemberService>.Create;
                    ser_member.UpdateNetAmount(order.UserId, order.TotalAmount);
                    ser_member.IncreaseMemberOrderNumber(order.UserId);
                    ser_member.UpdateLastConsumptionTime(order.UserId, DateTime.Now);

                    var orderItem = GetOrderItemsByOrderId(order.Id);
                    var productIds = orderItem.Select(p => p.ProductId).ToList();
                    var products = ServiceProvider.Instance<ProductService>.Create.GetProducts(productIds);
                    foreach (var item in products)
                    {
                        var categoryId = long.Parse(item.CategoryPath.Split('|')[0]);
                        ServiceProvider.Instance<OrderAndSaleStatisticsService>.Create.SynchronizeMemberBuyCategory(categoryId, order.UserId);
                    }
                }
                catch (Exception e)
                {
                    Log.Error("处理付款成功消息出错", e);
                }
            }
            //发送店铺通知消息
            var sordmsg = new MessageOrderInfo();
            sordmsg.OrderId = order.Id.ToString();
            sordmsg.ShopId = order.ShopId;
            sordmsg.ShopName = order.ShopName;
            sordmsg.SiteName = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
            sordmsg.TotalMoney = order.OrderTotalAmount;
            sordmsg.PaymentType = paymentType;
            sordmsg.PayTime = payTime;
            sordmsg.OrderTime = order.OrderDate;
            sordmsg.PaymentStatus = paymentStatus;
            sordmsg.ProductName = productName;
            sordmsg.UserName = order.UserName;
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnShopHasNewOrder(order.ShopId, sordmsg));

            SendAppMessage(order);//支付成功后推送APP消息
                                  //SendPushWdtOrder(order);//订单推送
            MemberInfo member = new MemberService().GetMember(order.UserId);
            if (model.OpenErp)
            {
                string message = "";
                long selfShopId = ServiceProvider.Instance<ShopService>.Create.GetSelfShop().Id;
                if (order.ShopId == selfShopId)
                {
                    _WDTOrderService.PushWangDianTongOrder(model, order.ShopId, order);
                }
            }

        }

        private void SendPushWdtOrder(OrderInfo order)
        {
            if (order.OrderType != OrderInfo.OrderTypes.Virtual && order.DeliveryType != DeliveryType.SelfTake)
            {
                MemberInfo member = new MemberService().GetMember(order.UserId);
                _WDTOrderService.PushWdtOrders(order, member);
                //TODO:[210218] 在service里不可以调用Application
                //WDTOrderApplication.PushWdtOrders(order);
            }
        }

        private void HandleLiveStatiscts(OrderInfo order)
        {
            OrderService orderService = ObjectContainer.Current.Resolve<OrderService>();
            LiveService liveService = ObjectContainer.Current.Resolve<LiveService>();
            var roomGroups = orderService.GetOrderItemsByOrderId(order.Id)
                    .Where(p => p.RoomId > 0).GroupBy(p => p.RoomId).ToList();

            foreach (var roomItems in roomGroups)
            {
                var roomId = roomItems.Key;
                var amount = 0M;
                foreach (var item in roomItems)
                {
                    amount += item.RealTotalPrice;
                    liveService.IncreasecProduct(roomId, item.ProductId, item.Quantity, item.RealTotalPrice);
                }
                var newMemberPayment = CheckLivePaymentMember(roomId, order.UserId);
                liveService.IncreasecPayment(roomId, amount, newMemberPayment);
            }
        }

        private bool CheckLivePaymentMember(long roomId, long memberId)
        {
            var key = $"live:payment:{roomId}:{memberId}";
            if (Cache.Exists(key))
                return false;//已存在
            Cache.Insert(key, 1, DateTime.Now.AddDays(1));
            return true;
        }

        public bool PayByCapitalIsOk(long userid, IEnumerable<long> orderIds)
        {
            var orders = DbFactory.Default.Get<OrderInfo>().Where(item => item.Id.ExIn(orderIds)).ToList();
            var totalAmount = orders.Sum(e => e.OrderTotalAmount);
            var capital = DbFactory.Default.Get<CapitalInfo>().Where(e => e.MemId == userid).FirstOrDefault();
            if (capital != null && capital.Balance >= totalAmount)
            {
                return true;
            }
            return false;
        }
        // 计算订单条目可退款金额
        // 看不懂具体逻辑，暂时不改
        public void CalculateOrderItemRefund(long orderId, bool isCompel = false)
        {
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == orderId).FirstOrDefault();
            var orderitems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == orderId).ToList();
            if (order != null)
            {
                if (!isCompel)
                {
                    var ord1stitem = orderitems.FirstOrDefault();
                    if (ord1stitem == null || ord1stitem.EnabledRefundAmount == null || ord1stitem.EnabledRefundAmount <= 0
                        || ord1stitem.EnabledRefundIntegral == null)
                    {
                        isCompel = true;
                    }
                }
            }
            if (isCompel)
            {
                Log.Info("进入计算订单条目可退款金额：CalculateOrderItemRefund");
                int orditemcnt = orderitems.Count();
                int curnum = 0;
                decimal ordprosumnum = order.ProductTotalAmount - order.DiscountAmount - order.FullDiscount - order.PlatDiscountAmount;
                decimal ordrefnum = order.ProductTotal;
                decimal ordindisnum = order.IntegralDiscount;
                decimal refcount = 0;
                decimal refincount = 0;   //只做整数处理
                long firstid = 0;
                var couponUseArea = 0;

                if (order.CouponId > 0)
                {
                    var selcoupon = DbFactory.Default.Get<CouponInfo>().Where(p => p.Id == order.CouponId).FirstOrDefault();
                    if (selcoupon != null && selcoupon.UseArea == 1)
                    {
                        couponUseArea = 1;
                    }
                }
                DbFactory.Default
                    .InTransaction(() =>
                    {
                        foreach (var item in orderitems)
                        {
                            decimal itemprosumnum = item.RealTotalPrice;
                            decimal curref = itemprosumnum;
                            decimal curinref = 0;
                            if (curnum == 0)
                            {
                                curref = 0;    //首件退款为结果计算
                                firstid = item.Id;
                            }
                            else
                            {
                                //计算积分
                                if (ordprosumnum > 0)
                                {
                                    curinref = (decimal)Math.Round(((ordindisnum / ordprosumnum) * curref), 2);
                                    if (curinref < 0)
                                        curinref = 0;
                                }
                            }
                            item.EnabledRefundAmount = curref;
                            item.EnabledRefundIntegral = curinref;
                            refcount += curref;
                            refincount += curinref;
                            curnum++;
                            DbFactory.Default.Update(item);
                        }
                        //处理首件
                        var firstitem = orderitems.FirstOrDefault(d => d.Id == firstid);
                        if (firstitem != null)
                        {
                            firstitem.EnabledRefundAmount = ordrefnum - refcount;
                            firstitem.EnabledRefundIntegral = ordindisnum - refincount;
                        }
                        DbFactory.Default.Update(firstitem);
                    });
            }
        }

        /// <summary>
        /// 获取销量
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="shopId"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        public long GetSaleCount(DateTime? startDate = null, DateTime? endDate = null, long? shopBranchId = null, long? shopId = null, long? productId = null, bool hasReturnCount = false, bool hasWaitPay = false)
        {
            long result = 0;
            var ordersql = DbFactory.Default.Get<OrderInfo>().Where(d => d.OrderStatus != OrderInfo.OrderOperateStatus.Close);
            var ordersqlAb = DbFactory.Default.Get<OrderInfo>();
            if (!hasWaitPay)
            {
                ordersql.Where(d => d.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay);
                ordersqlAb.Where(d => d.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay);
            }
            if (startDate.HasValue)
            {
                ordersql.Where(d => d.OrderDate >= startDate);
                ordersqlAb.Where(d => d.OrderDate >= startDate);
            }
            if (endDate.HasValue)
            {
                ordersql.Where(d => d.OrderDate <= endDate);
                ordersqlAb.Where(d => d.OrderDate <= endDate);
            }
            if (shopId.HasValue && shopId > 0)
            {
                ordersql.Where(d => d.ShopId == shopId.Value);
                ordersqlAb.Where(d => d.ShopId == shopId.Value);
            }
            if (shopBranchId.HasValue)
            {
                if (shopBranchId == 0)
                {  //查询总店
                    ordersql.Where(e => e.ShopBranchId == shopBranchId.Value || e.ShopBranchId.ExIsNull());
                    ordersqlAb.Where(e => e.ShopBranchId == shopBranchId.Value || e.ShopBranchId.ExIsNull());
                }
                else
                {
                    ordersql.Where(e => e.ShopBranchId == shopBranchId.Value);
                    ordersqlAb.Where(e => e.ShopBranchId == shopBranchId.Value);
                }
            }
            var orderids = ordersql.Select(d => d.Id).ToList<long>();
            var orderitemsql = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId.ExIn(orderids));

            var orderabIds = ordersqlAb.Where(o => o.DeliveryType != DeliveryType.SelfTake).Select(d => d.Id).ToList<long>();
            var orderitemsqlAb = DbFactory.Default.Get<OrderItemInfo>().Where(d => d.OrderId.ExIn(orderabIds));
            if (productId.HasValue && productId > 0)
            {
                orderitemsql.Where(d => d.ProductId == productId.Value);
                orderitemsqlAb.Where(d => d.ProductId == productId.Value);
            }
            try
            {
                if (hasReturnCount)
                {
                    result = orderitemsql.Sum<long>(d => d.Quantity - d.ReturnQuantity);

                }
                else
                {
                    result = orderitemsql.Sum<long>(d => d.Quantity);
                }
                var branchId = shopBranchId.HasValue ? shopBranchId.Value : 0;
                //统计商家同意弃货的数量
                var refundsql = DbFactory.Default.Get<OrderRefundInfo>()
                    .Where(r => r.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed)
                    .Where(r => r.RefundMode == OrderRefundInfo.OrderRefundMode.ReturnGoodsRefund)
                    .Where(r => r.IsReturn && r.ExpressCompanyName.ExIsNull() && r.ShipOrderNumber.ExIsNull());
                var orderitemids = orderitemsqlAb.Select(o => o.Id).ToList<long>();
                var refundCount = refundsql.Where(r => r.OrderItemId.ExIn(orderitemids)).Sum<long>(r => r.ReturnQuantity);
                result = result + refundCount;
            }
            catch
            {
                result = 0;
            }
            return result;
        }
        #endregion 订单操作 done

        #region 发票相关 done

        //发票内容
        public QueryPageModel<InvoiceContextInfo> GetInvoiceContexts(int PageNo, int PageSize = 20)
        {
            var data = DbFactory.Default.Get<InvoiceContextInfo>().OrderByDescending(o => o.Id).ToPagedList(PageNo, PageSize);
            QueryPageModel<InvoiceContextInfo> result = new QueryPageModel<InvoiceContextInfo>();
            result.Models = data;
            result.Total = data.TotalRecordCount;
            CacheManager.Clear("invoice:context");
            return result;
        }
        //发票内容
        public List<InvoiceContextInfo> GetInvoiceContexts() =>
            CacheManager.GetOrCreate("invoice:context", () => DbFactory.Default.Get<InvoiceContextInfo>().ToList());

        public void SaveInvoiceContext(InvoiceContextInfo info)
        {
            if (info.Id >= 0)  //update
            {
                var model = DbFactory.Default.Get<InvoiceContextInfo>().Where(p => p.Id == info.Id).FirstOrDefault();
                model.Name = info.Name;
                DbFactory.Default.Update(model);
            }
            else //create
            {
                DbFactory.Default.Add(info);
            }
            CacheManager.Clear("invoice:context");
        }

        public void DeleteInvoiceContext(long id)
        {
            DbFactory.Default.Del<InvoiceContextInfo>().Where(n => n.Id == id).Succeed();
            CacheManager.Clear("invoice:context");
        }


        public List<InvoiceTitleInfo> GetInvoiceTitles(long memberId) =>
            CacheManager.GetOrCreate($"invoice:title:{memberId}", () =>
                DbFactory.Default.Get<InvoiceTitleInfo>().Where(p => p.UserId == memberId).ToList());
        //发票抬头
        public List<InvoiceTitleInfo> GetInvoiceTitles(long userid, InvoiceType type) =>
            GetInvoiceTitles(userid).Where(p => p.InvoiceType == type).ToList();

        public long SaveInvoiceTitle(InvoiceTitleInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Name))
            {
                return -1;
            }
            var models = DbFactory.Default.Get<InvoiceTitleInfo>()
                .Where(i => i.InvoiceType == InvoiceType.OrdinaryInvoices).ToList();
            var flag = true;
            if (models.Count > 0)
            {
                flag = DbFactory.Default.Set<InvoiceTitleInfo>()
                    .Where(i => i.InvoiceType == InvoiceType.OrdinaryInvoices && i.UserId == info.UserId)
                    .Set(p => p.IsDefault, 0).Succeed();
            }

            //已存在则不添加
            var model = DbFactory.Default.Get<InvoiceTitleInfo>().Where(p => p.UserId == info.UserId && p.InvoiceType == InvoiceType.OrdinaryInvoices && (p.Name == info.Name || p.Code.ExIsNull())).FirstOrDefault();
            if (model != null)
            {
                model.Name = info.Name;

                model.Code = info.Code;
                model.InvoiceContext = info.InvoiceContext;
                model.IsDefault = 1;
                DbFactory.Default.Update<InvoiceTitleInfo>(model);
                return 0;
            }
            var result = DbFactory.Default.Add(info);

            CacheManager.Clear($"invoice:title:{info.UserId}");
            return info.Id;
        }

        /// <summary>
        /// 保存发票信息
        /// </summary>
        /// <param name="info"></param>
        public void SaveInvoiceTitleNew(InvoiceTitleInfo info)
        {
            if (info.InvoiceType == InvoiceType.OrdinaryInvoices)
            {
                SaveInvoiceTitle(info);
            }
            else
            {
                var model = DbFactory.Default.Get<InvoiceTitleInfo>().Where(p => p.UserId == info.UserId && p.InvoiceType == info.InvoiceType && p.IsDefault == 1).FirstOrDefault();
                if (model == null)
                    DbFactory.Default.Add<InvoiceTitleInfo>(info);
                else
                {
                    model.Name = info.Name;
                    model.Code = info.Code;
                    model.RegisterAddress = info.RegisterAddress;
                    model.RegisterPhone = info.RegisterPhone;
                    model.BankName = info.BankName;
                    model.BankNo = info.BankNo;
                    model.RealName = info.RealName;
                    model.CellPhone = info.CellPhone;
                    model.Email = info.Email;
                    model.RegionID = info.RegionID;
                    model.Address = info.Address;
                    DbFactory.Default.Update<InvoiceTitleInfo>(model);
                }
                if (info.InvoiceType == InvoiceType.ElectronicInvoice)
                {
                    info.InvoiceType = InvoiceType.OrdinaryInvoices;
                    SaveInvoiceTitle(info);
                }
            }
            CacheManager.Clear($"invoice:title:{info.UserId}");
        }

        public long EditInvoiceTitle(InvoiceTitleInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.Name) || string.IsNullOrWhiteSpace(info.Code))
            {
                return -1;
            }
            if (string.IsNullOrEmpty(info.Code))
            {
                return 0;
            }
            //已存在则不添加
            var entity = DbFactory.Default.Get<InvoiceTitleInfo>().Where(p => p.UserId == info.UserId && p.Id == info.Id).FirstOrDefault();
            if (null != entity)
            {
                var result = DbFactory.Default.Set<InvoiceTitleInfo>()
                .Where(i => i.InvoiceType == InvoiceType.OrdinaryInvoices)
                .Set(p => p.IsDefault, 0).Succeed();
                if (result)
                {
                    entity.Name = info.Name;
                    entity.Code = info.Code;
                    entity.IsDefault = 1;
                    DbFactory.Default.Update(entity);
                    return entity.Id;
                }
            }
            CacheManager.Clear($"invoice:title:{info.UserId}");
            return 0;
        }
        public void DeleteInvoiceTitle(long id, long userId = 0)
        {
            var sql = DbFactory.Default.Get<InvoiceTitleInfo>().Where(d => d.Id == id);
            if (userId > 0)
            {
                sql.Where(d => d.UserId == userId);
            }
            var obj = sql.FirstOrDefault();
            if (obj != null)
            {
                DbFactory.Default.Del(obj);
            }
            CacheManager.Clear($"invoice:title:{userId}");
        }

        #endregion 发票相关 done

        #region 私有函数

        /// <summary>
        /// 更改库存
        /// </summary>
        private void ReturnStock(OrderInfo order)
        {
            DbFactory.Default
                .InTransaction(() =>
                {
                    var orderItems = ObjectContainer.Current.Resolve<OrderService>().GetOrderItemsByOrderId(order.Id);

                    foreach (var orderItem in orderItems)
                    {
                        SKUInfo sku = DbFactory.Default.Get<SKUInfo>().Where(p => p.Id == orderItem.SkuId).FirstOrDefault();
                        if (sku != null)
                        {
                            //if (order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                            if (order.DeliveryType == DeliveryType.SelfTake || order.ShopBranchId > 0)//此处如果是系统自动将订单匹配到门店或者由商家手动分配订单到门店，其配送方式仍为快递。所以改为也能根据门店ID去判断
                            {
                                var sbSku = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(p => p.SkuId == sku.Id && p.ShopBranchId == order.ShopBranchId).FirstOrDefault();
                                if (sbSku != null)
                                {
                                    sbSku.Stock += (int)orderItem.Quantity;
                                    DbFactory.Default.Update(sbSku);
                                }
                            }
                            else
                            {
                                sku.Stock += orderItem.Quantity;
                                DbFactory.Default.Update(sku);
                            }

                            // 限购还原活动库存
                            if (order.OrderType == OrderInfo.OrderTypes.LimitBuy)
                            {
                                var flashSaleDetailInfo = DbFactory.Default.Get<FlashSaleDetailInfo>().Where(a => a.SkuId == orderItem.SkuId && a.FlashSaleId == orderItem.FlashSaleId).FirstOrDefault();
                                if (flashSaleDetailInfo != null)
                                {
                                    flashSaleDetailInfo.TotalCount += (int)orderItem.Quantity;
                                    DbFactory.Default.Update(flashSaleDetailInfo);
                                }
                            }
                        }
                    }
                });
        }

        private void CancelIntegral(long userId, string userName, long orderId, decimal integralDiscount)
        {
            if (integralDiscount == 0)
                return; //没使用积分直接返回
            var IntegralExchange = Instance<MemberIntegralService>.Create.GetIntegralChangeRule();
            if (IntegralExchange == null)
                return; //没设置兑换规则直接返回

            var IntegralPerMoney = IntegralExchange.IntegralPerMoney;
            var integral = Convert.ToInt32(Math.Floor(integralDiscount * IntegralPerMoney));
            var record = new MemberIntegralRecordInfo();
            record.UserName = userName;
            record.MemberId = userId;
            record.RecordDate = DateTime.Now;
            record.TypeId = MemberIntegralInfo.IntegralType.Cancel;
            record.ReMark = "订单被取消，返还积分，订单号:" + orderId.ToString();
            var action = new MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = MemberIntegralInfo.VirtualItemType.Cancel;
            action.VirtualItemId = orderId;
            record.MemberIntegralRecordActionInfo.Add(action);
            var memberIntegral = Instance<MemberIntegralConversionFactoryService>.Create.Create(MemberIntegralInfo.IntegralType.Cancel, integral);
            Instance<MemberIntegralService>.Create.AddMemberIntegralNotAddHistoryIntegrals(record, memberIntegral);
        }

        /// <summary>
        /// 退回预存款
        /// </summary>
        /// <param name="member"></param>
        /// <param name="order"></param>
        /// <param name="capitalAmount"></param>
        private void CancelCapital(long userId, long orderId, decimal capitalAmount)
        {
            DbFactory.Default
                .InTransaction(() =>
                {
                    if (capitalAmount <= 0) return;
                    var entity = DbFactory.Default.Get<CapitalInfo>().Where(e => e.MemId == userId).FirstOrDefault();
                    if (entity == null)
                    {
                        throw new HimallException("未存在预存款记录");
                    }

                    CapitalDetailInfo detail = new CapitalDetailInfo()
                    {
                        Amount = capitalAmount,
                        CapitalID = entity.Id,
                        CreateTime = DateTime.Now,
                        SourceType = CapitalDetailInfo.CapitalDetailType.Refund,
                        SourceData = orderId.ToString(),
                        Id = this.GenerateOrderNumber()
                    };
                    entity.Balance += capitalAmount;
                    DbFactory.Default.Add(detail);
                    DbFactory.Default.Update(entity);
                    //会员净消费处理
                    DbFactory.Default.Set<OrderInfo>()
                    .Where(d => d.Id == orderId)
                    .Set(d => d.ActualPayAmount, d => d.ActualPayAmount - capitalAmount)
                    .Succeed();
                });
        }

        /// <summary>
        /// 订单取消返回优惠券
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="orderId"></param>
        /// <param name="couponId"></param>
        private void ReturnCoupon(long userId, long orderId, long couponId)
        {
            if (couponId <= 0) return;
            var coupon = DbFactory.Default.Get<CouponInfo>().Where(c => c.Id == couponId).FirstOrDefault();
            if (coupon == null)
                throw new HimallException("优惠卷不存在");
            var couponRecord = DbFactory.Default.Get<CouponRecordInfo>().Where(r => r.CouponId == couponId && r.UserId == userId && r.OrderId.Equals(orderId)).FirstOrDefault();
            if (couponRecord == null)
                throw new HimallException("用户领取优惠卷记录不存在");

            couponRecord.UsedTime = null;
            couponRecord.OrderId = null;
            couponRecord.CounponStatus = CouponRecordInfo.CounponStatuses.Unuse;
            DbFactory.Default.Update(couponRecord);
        }

        /// <summary>
        /// 订单取消返回平台优惠卷 TODO:ZYF
        /// </summary>
        /// <param name="member"></param>
        /// <param name="order"></param>
        private void ReturnPlatCoupon(long memberId, OrderInfo order)
        {
            var couponId = order.PlatCouponId;
            if (couponId <= 0) return;
            var coupon = DbFactory.Default.Get<CouponInfo>().Where(c => c.Id == couponId).FirstOrDefault();
            if (coupon == null)
                throw new HimallException("优惠券不存在");
            var couponRecord = DbFactory.Default.Get<CouponRecordInfo>().Where(r => r.CouponId == couponId && r.UserId == memberId && r.OrderId.Contains(order.Id.ToString())).FirstOrDefault();
            if (couponRecord == null)
                throw new HimallException("用户领取优惠券记录不存在");
            if (!VailidReturnPlateCoupon(couponId, order.UserId, order.OrderDate))
            {
                couponRecord.UsedTime = null;
                couponRecord.OrderId = null;
                couponRecord.CounponStatus = CouponRecordInfo.CounponStatuses.Unuse;
                DbFactory.Default.Update(couponRecord);
            }
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

        // 添加订单操作日志
        private void AddOrderOperationLog(long orderId, string userName, string operateContent)
        {
            OrderOperationLogInfo orderOperationLog = new OrderOperationLogInfo();
            orderOperationLog.Operator = userName;
            orderOperationLog.OrderId = orderId;
            orderOperationLog.OperateDate = DateTime.Now;
            orderOperationLog.OperateContent = operateContent;

            DbFactory.Default.Add(orderOperationLog);
        }

        // 获取积分所能兑换的总金额
        public decimal GetIntegralDiscountAmount(int integral, long userId)
        {
            if (integral == 0)
            {
                return 0;
            }
            var integralService = Instance<MemberIntegralService>.Create;
            var userIntegral = ServiceProvider.Instance<MemberIntegralService>.Create.GetMemberIntegral(userId)?.AvailableIntegrals ?? 0;
            if (userIntegral < integral)
                throw new HimallException("用户积分不足不能抵扣订单");

            var exchangeModel = integralService.GetIntegralChangeRule();
            var integralPerMoney = exchangeModel == null ? 0 : exchangeModel.IntegralPerMoney;
            decimal money = 0;
            if (integralPerMoney > 0)
            {
                money = integral / (decimal)integralPerMoney;
                money = Math.Floor(money * (decimal)Math.Pow(10, 2)) / (decimal)Math.Pow(10, 2);
            }
            //return integralPerMoney == 0 ? 0 : Math.Round(integral / (decimal)integralPerMoney, 2, MidpointRounding.AwayFromZero);
            return money;
        }

        /// <summary>
        /// 获取单个订单项所使用的优惠券的金额大小
        /// </summary>
        /// <param name="realTotalPrice">當前商品的減價後的價格</param>
        /// <param name="ProductTotal">訂單所有商品的總價格</param>
        /// <param name="couponDisCount">總的優惠券金額</param>
        /// <returns></returns>
        private decimal GetItemCouponDisCount(decimal realTotalPrice, decimal ProductTotal, decimal couponDiscount)
        {
            var ItemCouponDiscount = Math.Round(couponDiscount * realTotalPrice / ProductTotal, 2);
            return ItemCouponDiscount;
        }

        // 更新限时购活动购买记录
        private void UpdateLimitTimeBuyLog(IEnumerable<OrderItemInfo> orderItems)
        {
            ServiceProvider.Instance<LimitTimeBuyService>.Create.IncreaseSaleCount(orderItems.Select(a => a.OrderId).ToList());
        }

        private void UpdateProductVisti(IEnumerable<OrderItemInfo> orderItems)
        {
            var date1 = DateTime.Now.Date;
            var date2 = DateTime.Now.Date.AddDays(1);
            DbFactory.Default
                .InTransaction(() =>
                {
                    foreach (OrderItemInfo orderItem in orderItems)
                    {
                        var productVisti = DbFactory.Default
                            .Get<ProductVistiInfo>()
                            .Where(item => item.ProductId == orderItem.ProductId && item.Date >= date1 && item.Date <= date2)
                            .FirstOrDefault();
                        if (productVisti == null)
                        {
                            productVisti = new ProductVistiInfo();
                            productVisti.ProductId = orderItem.ProductId;
                            productVisti.Date = DateTime.Now.Date;
                            productVisti.OrderCounts = 0;
                            DbFactory.Default.Add(productVisti);
                        }

                        var productInfo = DbFactory.Default.Get<ProductInfo>().Where(n => n.Id == orderItem.ProductId).FirstOrDefault();
                        var searchProduct = DbFactory.Default.Get<SearchProductInfo>().Where(r => r.ProductId == orderItem.ProductId).FirstOrDefault();
                        if (productInfo != null)
                        {
                            productInfo.SaleCounts += orderItem.Quantity;
                            DbFactory.Default.Update(productInfo);
                            if (searchProduct != null)
                            {
                                searchProduct.SaleCount += (int)orderItem.Quantity;
                                DbFactory.Default.Update(searchProduct);
                            }
                        }
                        productVisti.SaleCounts += orderItem.Quantity;
                        productVisti.SaleAmounts += orderItem.RealTotalPrice;
                        DbFactory.Default.Update(productVisti);
                    }
                });
        }

        // 更新商品购买的订单总数
        public void UpdateProductVistiOrderCount(long orderId)
        {
            DbFactory.Default
                .InTransaction(() =>
                {
                    //获取订单明细
                    var items = DbFactory.Default.Get<OrderItemInfo>().Where(o => o.OrderId == orderId).ToList();
                    //更新商品购买的订单总数
                    foreach (OrderItemInfo model in items)
                    {
                        ProductVistiInfo productVisti = DbFactory.Default.Get<ProductVistiInfo>().Where(p => p.ProductId == model.ProductId).FirstOrDefault();
                        if (productVisti != null)
                        {
                            productVisti.OrderCounts = (productVisti.OrderCounts == null ? 0 : productVisti.OrderCounts) + 1;
                            DbFactory.Default.Update(productVisti);
                        }
                    }
                });
        }

        // 更新店铺访问量
        private void UpdateShopVisti(OrderInfo order)
        {//TODO:店铺访问量统计，暂时取消实时统计
            /* 
            var date = DateTime.Now.Date;
            ShopVistiInfo shopVisti = Context.ShopVistiInfo.FindBy(item =>
                item.ShopId == order.ShopId && item.Date.Year == date.Year && item.Date.Month == date.Month && item.Date.Day == date.Day).FirstOrDefault();
            if (shopVisti == null)
            {
                shopVisti = new ShopVistiInfo();
                shopVisti.ShopId = order.ShopId;
                shopVisti.Date = DateTime.Now.Date;
                Context.ShopVistiInfo.Add(shopVisti);
            }
            shopVisti.SaleCounts += order.OrderProductQuantity;
            shopVisti.SaleAmounts += order.ProductTotalAmount;
            Context.SaveChanges();
             */
        }

        /// <summary>
        /// 是否可以发货
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        private bool CanSendGood(long orderId)
        {
            bool result = false;
            var ordobj = DbFactory.Default.Get<OrderInfo>().Where(d => d.Id == orderId).FirstOrDefault();
            if (ordobj == null)
            {
                throw new HimallException("错误的订单编号");
            }
            if (ordobj.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                var fgord = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderId == orderId).FirstOrDefault();
                if (fgord.CanSendGood)
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }
            return result;
        }

        #endregion 私有函数

        /// <summary>
        /// 写入待结算
        /// </summary>
        /// <param name="o"></param>
        public void WritePendingSettlnment(OrderInfo o)
        {
            try
            {
                DbFactory.Default
                    .InTransaction(() =>
                    {
                        var orderDetail = DbFactory.Default.Get<OrderItemInfo>().Where(a => a.OrderId == o.Id).ToList();

                        /*
                         订单金额=商品总价 - 满额优惠 - 优惠券 + 运费 + 税费
                         */
                        var item = new PendingSettlementOrderInfo();
                        item.ShopId = o.ShopId;
                        item.ShopName = o.ShopName;
                        item.OrderId = o.Id;
                        item.FreightAmount = o.Freight;
                        item.TaxAmount = o.Tax;
                        item.IntegralDiscount = o.IntegralDiscount;
                        item.OrderType = o.OrderType;

                        decimal refundAmount = 0M;
                        item.RefundAmount = refundAmount;
                        //平台佣金退还
                        item.PlatCommissionReturn = 0M;

                        //统计订单分销佣金
                        item.DistributorCommission = Instance<DistributionService>.Create.GetDistributionBrokerageAmount(o.Id);

                        //结算金额=商品总价 - 满额优惠 - 店铺优惠券 + 运费 + 税费 - 平台佣金 - 分销佣金 - 退款金额
                        if (o.PlatCouponId > 0 && o.PlatDiscountAmount > 0)
                        {
                            //平台优惠券抵扣金额需要支付给店铺
                            item.DiscountAmount = o.PlatDiscountAmount;
                        }
                        else
                        {
                            item.DiscountAmount = 0;
                        }

                        item.ProductsAmount = o.ProductTotalAmount - o.DiscountAmount - o.FullDiscount;
                        //平台佣金 = 商品金额 * 对应分类设置的平台抽佣比例（多种商品分开计算）
                        //单项四舍五入后，再累加
                        foreach (var c in orderDetail)
                        {
                            item.PlatCommission += FormatMonty((c.RealTotalPrice + c.PlatCouponDiscount) * c.CommisRate);
                        }

                        item.SettlementAmount = item.ProductsAmount + item.FreightAmount + item.TaxAmount - item.PlatCommission - item.DistributorCommission - refundAmount + item.PlatCommissionReturn + item.DistributorCommissionReturn;
                        item.CreateDate = DateTime.Now;
                        if (o.FinishDate.HasValue)
                        {
                            item.OrderFinshTime = (DateTime)o.FinishDate;
                        }

                        item.PaymentTypeName = o.PaymentTypeDesc;
                        item.OrderAmount = o.OrderTotalAmount;
                        DbFactory.Default.Add(item);
                        //更新店铺资金账户
                        var m = DbFactory.Default.Get<ShopAccountInfo>().Where(a => a.ShopId == o.ShopId).FirstOrDefault();
                        if (m != null)
                        {
                            m.PendingSettlement += item.SettlementAmount;
                            DbFactory.Default.Update(m);
                        }
                        //更新平台资金账户
                        var plat = DbFactory.Default.Get<PlatAccountInfo>().FirstOrDefault();
                        if (plat != null)
                        {
                            //  var mid = item.PlatCommission - item.PlatCommissionReturn;
                            plat.PendingSettlement += item.SettlementAmount;
                            DbFactory.Default.Update(plat);
                        }
                    });
            }
            catch (Exception ex)
            {
                Log.Error("WritePendingSettlnment:" + ex.Message + "/r/n" + ex.StackTrace);
            }
        }
        /// <summary>
        /// 更新待结算订单完成时间
        /// </summary>
        /// <param name="order"></param>
        private void UpdatePendingSettlnmentFinishDate(long orderid, DateTime dt)
        {
            DbFactory.Default.Set<PendingSettlementOrderInfo>().Set(e => e.OrderFinshTime, dt).Where(e => e.OrderId == orderid).Succeed();
        }

        public void ConfirmZeroOrder(IEnumerable<long> Ids, long userId)
        {
            var orders = DbFactory.Default
                .Get<OrderInfo>()
                .Where(item => item.Id.ExIn(Ids) && item.UserId == userId && item.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay
                    || item.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery && item.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery &&
                    item.Id.ExIn(Ids) && item.UserId == userId)
                .ToList();
            DbFactory.Default
                .InTransaction(() =>
                {
                    foreach (var order in orders)
                    {
                        if (order.OrderWaitPayAmountIsZero)
                        {
                            if (order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                            {
                                OperaOrderPickupCode(order);
                            }
                            else
                                order.OrderStatus = OrderInfo.OrderOperateStatus.WaitDelivery;

                            order.PaymentType = OrderInfo.PaymentTypes.Online;
                            order.PaymentTypeName = PAY_BY_INTEGRAL_PAYMENT_ID;
                            order.PayDate = DateTime.Now;
                            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                            {
                                order.OrderStatus = OrderInfo.OrderOperateStatus.WaitVerification;//虚拟订单付款后，则为待消费
                            }
                            DbFactory.Default.Update(order);

                            //发布付款成功消息
                            //MessageQueue.PublishTopic(CommonConst.MESSAGEQUEUE_PAYSUCCESSED, order.Id);
                            if (OnOrderPaySuccessed != null)
                                OnOrderPaySuccessed(order.Id);

                            SendAppMessage(order);//支付成功后推送APP消息
                        }
                    }

                    var orderItems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(Ids)).ToList();
                    foreach (var order in orders)
                    {
                        UpdateProductVisti(orderItems.Where(p => p.OrderId == order.Id));
                        if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                        {
                            var orderItemInfo = orderItems.Where(p => p.OrderId == order.Id).FirstOrDefault();//虚拟订单项只有一个
                            UpdateOrderItemEffectiveDateByIds(orderItems.Where(p => p.OrderId == order.Id).Select(a => a.Id).ToList(), order.PayDate.Value);
                            if (orderItemInfo != null)
                            {
                                AddOrderVerificationCodeInfo(orderItemInfo.Quantity, orderItemInfo.OrderId, orderItemInfo.Id);
                                SendMessageOnVirtualOrderPay(order, orderItemInfo.ProductId);
                            }
                        }
                    }
                    if (orders != null && orders.Count > 0)
                        ServiceProvider.Instance<LimitTimeBuyService>.Create.IncreaseSaleCount(orders.Select(a => a.Id).ToList());//这里应该传入重新过滤后的订单
                });
        }

        public void CancelOrders(IEnumerable<long> Ids, long userId)
        {
            if (Ids.Count() > 0)
            {
                DbFactory.Default.Del<OrderItemInfo>().Where(p => p.OrderId.ExIn(Ids)).Succeed();
                DbFactory.Default.Del<OrderInfo>().Where(p => p.Id.ExIn(Ids)).Succeed();
            }
        }

        //TODO LRL 2015/08/06 获取子订单对象
        public OrderItemInfo GetOrderItem(long orderItemId)
        {
            var orderitem = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.Id == orderItemId).FirstOrDefault();
            if (null == orderitem)
            {
                var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
                if (flag)
                {
                    orderitem = DbFactory.MongoDB.AsQueryable<OrderItemInfo>().FirstOrDefault(p => p.Id == orderItemId);
                }
            }
            return orderitem;
        }


        public OrderDayStatistics GetOrderDayStatistics(long shop, DateTime begin, DateTime end)
        {
            var result = new OrderDayStatistics();
            var payOrders = DbFactory.Default.Get<OrderInfo>().Where(a => a.PayDate >= begin && a.PayDate < end);
            var orders = DbFactory.Default.Get<OrderInfo>().Where(a => a.OrderDate >= begin && a.OrderDate < end);
            if (shop > 0)
            {
                payOrders.Where(p => p.ShopId == shop);
                orders.Where(p => p.ShopId == shop);
            }
            result.OrdersNum = orders.Count();
            result.PayOrdersNum = payOrders.Count();
            result.SaleAmount = payOrders.Sum<decimal>(p => p.ProductTotalAmount + p.Freight + p.Tax - p.DiscountAmount);
            return result;
        }

        /// <summary>
        /// 商家给订单备注
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="remark"></param>
        /// <param name="shopId">店铺ID</param>
        public void UpdateSellerRemark(long orderId, string remark, int flag)
        {
            DbFactory.Default.Set<OrderInfo>()
                .Where(p => p.Id == orderId)
               .Set(p => p.SellerRemark, remark)
               .Set(p => p.SellerRemarkFlag, flag)
               .Succeed();
        }

        /// <summary>
        /// 根据提货码取订单
        /// </summary>
        /// <param name="pickCode"></param>
        /// <returns></returns>
        public OrderInfo GetOrderByPickCode(string pickCode)
        {
            return DbFactory.Default.Get<OrderInfo>().Where(e => e.PickupCode == pickCode).FirstOrDefault();
        }



        #region OrderBO 的方法

        /// <summary>
        /// 设置运费
        /// </summary>
        /// <param name="order">订单</param>
        /// <param name="freight">运费</param>
        public void SetFreight(OrderInfo order, decimal freight)
        {
            if (freight < 0)
            {
                throw new HimallException("运费不能为负值！");
            }
            order.Freight = freight;
            order.TotalAmount = order.OrderTotalAmount;//订单实付金额重新计算
        }

        /// <summary>
        /// 设置订单状态为完成
        /// </summary>
        public void SetStateToConfirm(OrderInfo order)
        {
            if (order == null)
            {
                throw new HimallException("处理订单错误，请确认该订单状态正确");
            }
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitReceiving)
            {
                throw new HimallException("只有等待收货状态的订单才能进行确认操作");
            }
            if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
            {
                order.PayDate = DateTime.Now;
            }
            order.OrderStatus = OrderInfo.OrderOperateStatus.Finish;
            order.FinishDate = DateTime.Now;
        }
        /// <summary>
        /// 检测订单是否可以被关闭
        /// </summary>
        /// <param name="order"></param>
        public void CheckCloseOrder(OrderInfo order)
        {
            if (order == null) { throw new HimallException("错误的订单信息！"); }
            if (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay || (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery && order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery))
            {
                if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
                {
                    var fgser = ServiceProvider.Instance<FightGroupService>.Create;
                    var fgord = fgser.GetFightGroupOrderStatusByOrderId(order.Id);
                    if (
                        fgord.JoinStatus == FightGroupOrderJoinStatus.Ongoing.GetHashCode() ||
                        fgord.JoinStatus == FightGroupOrderJoinStatus.JoinSuccess.GetHashCode() ||
                        fgord.JoinStatus == FightGroupOrderJoinStatus.BuildSuccess.GetHashCode()
                        )
                    {
                        throw new HimallException("拼团订单不可关闭");
                    }
                }
            }
            else
            {
                throw new HimallException("只有待付款状态或货到付款待发货状态的订单才能进行取消操作");
            }
        }


        /// <summary>
        /// 获取真实费用
        /// </summary>
        public decimal GetRealTotalPrice(OrderInfo order, OrderItemInfo item, decimal discountAmount)
        {
            if (item.RealTotalPrice - discountAmount < 0)
            {
                throw new HimallException("优惠金额不能大于商品总金额！");
            }
            if (order.OrderTotalAmount - discountAmount < 0)
            {
                throw new HimallException("减价不能导致订单总金额为负值！");
            }

            return item.RealTotalPrice - discountAmount;
        }


        private static object obj = new object();
        /// <summary>
        ///  生成订单号
        /// </summary>
        public long GenerateOrderNumber()
        {
            lock (obj)
            {
                int rand;
                char code;
                string orderId = string.Empty;
                Random random = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                for (int i = 0; i < 5; i++)
                {
                    rand = random.Next();
                    code = (char)('0' + (char)(rand % 10));
                    orderId += code.ToString();
                }
                return long.Parse(DateTime.Now.ToString("yyyyMMddfff") + orderId);
            }
        }

        private static object objpay = new object();
        /// <summary>
        /// 生成支付订单号
        /// </summary>
        /// <returns></returns>
        public long GetOrderPayId()
        {
            lock (objpay)
            {
                int rand;
                char code;
                string orderId = string.Empty;
                Random random = new Random(BitConverter.ToInt32(Guid.NewGuid().ToByteArray(), 0));
                for (int i = 0; i < 6; i++)
                {
                    rand = random.Next();
                    code = (char)('0' + (char)(rand % 10));
                    orderId += code.ToString();
                }
                return long.Parse(DateTime.Now.ToString("yyMMddmmHHss") + orderId);
            }
        }

        /// <summary>
        /// 获取所有订单使用的优惠券列表
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="couponIdsStr"></param>
        /// <returns></returns>
        public IEnumerable<BaseAdditionalCoupon> GetOrdersCoupons(long userId, IEnumerable<string[]> couponIdsStr)
        {
            var couponService = ServiceProvider.Instance<CouponService>.Create;
            var shopBonusService = ServiceProvider.Instance<ShopBonusService>.Create;
            if (couponIdsStr == null || couponIdsStr.Count() <= 0)
            {
                return null;
            }
            List<BaseAdditionalCoupon> list = new List<BaseAdditionalCoupon>();
            foreach (string[] str in couponIdsStr)
            {
                BaseAdditionalCoupon item;
                if (int.Parse(str[1]) == 0)
                {
                    var obj = couponService.GetOrderCoupons(userId, new long[] { long.Parse(str[0]) }).FirstOrDefault();
                    if (obj == null)
                        throw new HimallException("优惠券不存在或优惠券已使用!");
                    item = new BaseAdditionalCoupon();
                    item.Type = 0;
                    item.Coupon = obj;
                    if (obj.ShopId > 0)
                    {
                        item.ShopId = obj.ShopId;
                    }
                    else
                    {
                        if (str.Length > 2)
                            item.ShopId = long.Parse(str[2]);
                    }
                }
                else if (int.Parse(str[1]) == 1)
                {
                    var obj = shopBonusService.GetDetailById(userId, long.Parse(str[0]));
                    var grant = shopBonusService.GetGrant(obj.BonusGrantId);
                    var bonus = shopBonusService.GetShopBonus(grant.ShopBonusId);
                    item = new BaseAdditionalCoupon();
                    item.Type = 1;
                    item.Coupon = obj;
                    item.ShopId = bonus.ShopId;
                }
                else
                {
                    item = new BaseAdditionalCoupon();
                    item.Type = 99;
                }

                list.Add(item);
            }

            return list;
        }

        public void UpdateVirtualProductStatus()
        {
            try
            {
                var codes = DbFactory.Default.Get<OrderVerificationCodeInfo>()
                    .LeftJoin<OrderItemInfo>((ov, oii) => ov.OrderItemId == oii.Id)
                    .LeftJoin<VirtualProductInfo, OrderItemInfo>((vp, oii) => vp.ProductId == oii.ProductId)
                    .Where(ov => ov.Status == OrderInfo.VerificationCodeStatus.WaitVerification)
                    .Where<VirtualProductInfo>(vp => vp.ValidityType && vp.EndDate.ExIsNull() == false && DateTime.Now > vp.EndDate.Value).ToList();
                var orderids = codes.Select(p => p.OrderId).Distinct();
                if (orderids.Count() > 0)
                    DbFactory.Default.Set<OrderVerificationCodeInfo>().Set(p => p.Status, OrderInfo.VerificationCodeStatus.Expired).Where(a => a.OrderId.ExIn(orderids) && a.Status == OrderInfo.VerificationCodeStatus.WaitVerification).Succeed();
                foreach (var orderId in orderids)
                {
                    UpdateOrderVerificationCodeStatusByCodes(orderId);
                }
            }
            catch (Exception ex)
            {
                Log.Error("更新过期核销码出现异常：" + ex.ToString());
            }
        }

        public void UpdateOrderVerificationCodeStatus()
        {
            var virtualProducts = DbFactory.Default.Get<VirtualProductInfo>().LeftJoin<ProductInfo>((pi, pri) => pi.ProductId == pri.Id).Where(a => a.ValidityType && a.EndDate.ExIsNull() == false && DateTime.Now > a.EndDate.Value).Where<ProductInfo>(a => a.ProductType == 1 && a.IsDeleted == false && a.SaleStatus == ProductInfo.ProductSaleStatus.OnSale && a.AuditStatus == ProductInfo.ProductAuditStatus.Audited).ToList();
            var pids = virtualProducts.Select(p => p.ProductId).ToList();
            DbFactory.Default
                    .Set<ProductInfo>().Set(p => p.SaleStatus, ProductInfo.ProductSaleStatus.InStock)
                    .Set(n => n.AuditStatus, ProductInfo.ProductAuditStatus.WaitForAuditing)
                    .Where(a => a.Id.ExIn(pids)).Succeed();

            DbFactory.Default.Set<ShopBranchSkuInfo>()
                .Where(p => p.ProductId.ExIn(pids))
                .Set(p => p.Status, ShopBranchSkuStatus.InStock).Succeed();

            DbFactory.Default.Set<SearchProductInfo>()
            .Set(p => p.CanSearch, false)
            .Where(p => p.CanSearch == true && p.ProductId.ExIn(pids))
            .Succeed();
        }
        public void UpdateOrderVerificationCodeStatusByCodes(long orderId)
        {
            OrderInfo.OrderOperateStatus orderStatus = 0;
            var orderVerificationCodes = DbFactory.Default.Get<OrderVerificationCodeInfo>().Where(a => a.OrderId == orderId).ToList();
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
                    orderStatus = OrderInfo.OrderOperateStatus.Finish;
                }

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
        }

        #endregion

        #region 私有方法
        /// <summary>
        /// 处理自提订单提货码
        /// <para>拼团订单的提货码需要成团成功后生成</para>
        /// </summary>
        /// <param name="order"></param>
        /// <param name="isMust">必须生成</param>
        private void OperaOrderPickupCode(OrderInfo order)
        {
            if (order.DeliveryType == CommonModel.DeliveryType.SelfTake)
            {
                order.OrderStatus = OrderInfo.OrderOperateStatus.WaitSelfPickUp;
                if (order.OrderType != OrderInfo.OrderTypes.FightGroup)
                {
                    order.PickupCode = GeneratePickupCode(order.Id);
                }
            }
        }
        public static string GeneratePickupCode(long orderId)
        {
            var digits = "0123456789";
            var random = new byte[3];
            _randomPickupCode.GetBytes(random);

            string newOrderId = orderId.ToString().Substring(2);
            var pickupCode = string.Format("{0}{1}", newOrderId, string.Join("", random.Select(p => digits[p % digits.Length])));
            return pickupCode;
        }

        private long[] GetOrderIdRange(string orderIdStr)
        {
            long orderId;
            if (!string.IsNullOrEmpty(orderIdStr) && long.TryParse(orderIdStr, out orderId))
            {
                var temp = this.GenerateOrderNumber().ToString();
                if (orderIdStr.Length < temp.Length)
                {
                    var len = temp.Length - orderIdStr.Length;
                    orderId = orderId * (long)Math.Pow(10, len);
                    var max = 8 + long.Parse(string.Join("", new int[len].Select(p => 9)));
                    return new[] { orderId, max };
                }
                else if (orderIdStr.Length == temp.Length)
                    return new[] { orderId };
            }

            return null;
        }
        private IMongoQueryable<OrderInfo> ToPaymentWhere(GetBuilder<OrderInfo> orders, IMongoQueryable<OrderInfo> history, OrderQuery query)
        {
            if (!string.IsNullOrWhiteSpace(query.PaymentTypeName))
            {
                if (null != orders)
                    orders.Where(p => p.PaymentTypeName.Contains(query.PaymentTypeName));

                history = history.Where(p => p.PaymentTypeName.Contains(query.PaymentTypeName));
            }
            if (query.PaymentTypeGateways != null && query.PaymentTypeGateways.Count > 0)
            {
                if (null != orders)
                    orders.Where(p => p.PaymentTypeGateway.ExIn(query.PaymentTypeGateways) && p.PaymentTypeGateway != "");

                history = history.Where(p => p.PaymentTypeGateway.ExIn(query.PaymentTypeGateways) && p.PaymentTypeGateway != "");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(query.PaymentTypeGateway))
                {
                    switch (query.PaymentTypeGateway)
                    {
                        case "组合支付":
                            orders.Where(p => p.CapitalAmount > 0 && p.PaymentTypeGateway != "");
                            break;
                        case "预存款支付":
                            orders.Where(p => p.CapitalAmount > 0);
                            history = history.Where(p => p.CapitalAmount > 0);
                            break;
                        case "货到付款":
                            orders.Where(p => p.PaymentTypeName == query.PaymentTypeGateway);
                            history = history.Where(p => p.PaymentTypeName == query.PaymentTypeGateway);
                            break;
                        case "线下收款":
                            orders.Where(p => p.PaymentType == OrderInfo.PaymentTypes.Offline);
                            history = history.Where(p => p.PaymentType == OrderInfo.PaymentTypes.Offline);
                            break;
                        case "其他":
                            orders.Where(p => p.TotalAmount == 0);
                            history = history.Where(p => p.TotalAmount == 0);
                            break;
                        default:
                            //此处会造成 查询微信支付 Himall.Plugin.Payment.WeiXinPay 时会把微信APP支付Himall.Plugin.Payment.WeiXinPay_App 和微信扫码支付 Himall.Plugin.Payment.WeiXinPay_Native查询出来
                            //  orders.Where(p => p.PaymentTypeGateway.Contains(query.PaymentTypeGateway)); 
                            if (null != orders)
                                orders.Where(p => p.PaymentTypeGateway == query.PaymentTypeGateway);

                            history = history.Where(p => p.PaymentTypeGateway == query.PaymentTypeGateway);
                            break;

                    }
                }
            }
            return history;
        }
        private IMongoQueryable<OrderInfo> ToWhere(GetBuilder<OrderInfo> orders, IMongoQueryable<OrderInfo> history, OrderQuery query)
        {
            var orderIdRange = GetOrderIdRange(query.OrderId);

            if (orderIdRange == null)
            {
                if (!string.IsNullOrWhiteSpace(query.OrderId))//如果是按订单ID查询，但又无法正确识别,那么直接按ID精准查询则无法查到数据
                {
                    long tempOrderId = 0;
                    long.TryParse(query.OrderId, out tempOrderId);
                    if (null != orders)
                        orders.Where(item => item.Id == tempOrderId);

                    history = history.Where(item => item.Id == tempOrderId);
                }
                if (!string.IsNullOrEmpty(query.SearchKeyWords))
                {
                    query.SearchKeyWords = query.SearchKeyWords.Trim();

                    if (query.SearchKeyWords.Length == 16)
                    {
                        orderIdRange = GetOrderIdRange(query.SearchKeyWords);
                    }
                    if (orderIdRange == null && !string.IsNullOrWhiteSpace(query.SearchKeyWords))
                    {
                        if (null != orders)
                        {
                            var sub = DbFactory.Default
                                .Get<OrderItemInfo>()
                                .Where<OrderInfo>((oii, oi) => oii.OrderId == oi.Id && oii.ProductName.Contains(query.SearchKeyWords));

                            orders.Where(p => p.ExExists(sub));
                        }
                        var flag = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenHistoryOrder;
                        if (flag)
                        {
                            var ids = DbFactory.MongoDB
                                .AsQueryable<OrderItemInfo>()
                                .Where(n => n.ProductName.Contains(query.SearchKeyWords))
                                .Select(n => n.OrderId)
                                .ToList();
                            history = history.Where(p => ids.Contains(p.Id));
                        }
                    }

                }
            }

            if (orderIdRange != null)
            {
                var min = orderIdRange[0];
                if (orderIdRange.Length == 2)
                {
                    var max = orderIdRange[1];
                    if (null != orders)
                        orders.Where(item => item.Id >= min && item.Id <= max);

                    history = history.Where(item => item.Id >= min && item.Id <= max);
                }
                else
                {
                    if (null != orders)
                        orders.Where(item => item.Id == min);

                    history = history.Where(item => item.Id == min);
                }
            }
            if (query.InvoiceType.HasValue)
            {
                var orderids = DbFactory.Default.Get<OrderInvoiceInfo>().Where(item => item.InvoiceType == (InvoiceType)query.InvoiceType).Select(item => item.OrderId).ToList<long>();
                if (orders != null)
                    orders.Where(o => o.Id.ExIn(orderids));
            }

            if (query.IsVirtual.HasValue)
            {
                if (query.IsVirtual.Value)
                {
                    if (null != orders)
                        orders.Where(p => p.OrderType == OrderInfo.OrderTypes.Virtual);

                    history = history.Where(p => p.OrderType == OrderInfo.OrderTypes.Virtual);
                }
                else
                {
                    if (null != orders)
                        orders.Where(p => p.OrderType != OrderInfo.OrderTypes.Virtual);

                    history = history.Where(p => p.OrderType != OrderInfo.OrderTypes.Virtual);
                }
            }


            if (query.IsLive.HasValue)
            {
                if (null != orders)
                {
                    orders.Where(item => item.IsLive == query.IsLive.Value);
                }

                history = history.Where(p => p.IsLive == query.IsLive.Value);
            }

            if (query.IsSelfTake.HasValue && query.IsSelfTake.Value == 1)
            {
                if (null != orders)
                    orders.Where(p => p.DeliveryType == Himall.CommonModel.DeliveryType.SelfTake);

                history = history.Where(p => p.DeliveryType == Himall.CommonModel.DeliveryType.SelfTake);
            }
            if (query.ShopId.HasValue)
            {
                var shopId = query.ShopId.Value;
                if (null != orders)
                    orders.Where(p => p.ShopId == shopId);

                history = history.Where(p => p.ShopId == shopId);
            }
            if (query.ShopBranchId.HasValue && query.ShopBranchId.Value != -1)
            {
                if (query.ShopBranchId.Value == 0)
                { //查询总店
                    if (null != orders)
                        orders.Where(e => e.ShopBranchId == query.ShopBranchId.Value || e.ShopBranchId.ExIsNull());

                    history = history.Where(e => e.ShopBranchId == query.ShopBranchId.Value);
                }
                else
                {
                    if (null != orders)
                        orders.Where(e => e.ShopBranchId == query.ShopBranchId.Value);

                    history = history.Where(e => e.ShopBranchId == query.ShopBranchId.Value);
                }
            }
            if (query.AllotStore.HasValue && query.AllotStore.Value != 0)
            {
                if (query.AllotStore.Value == 1)
                {
                    if (null != orders)
                        orders.Where(e => e.ShopBranchId > 0);

                    history = history.Where(e => e.ShopBranchId > 0);
                }
                else
                {
                    if (null != orders)
                        orders.Where(e => e.ShopBranchId <= 0);

                    history = history.Where(e => e.ShopBranchId <= 0);
                }
            }
            if (!string.IsNullOrWhiteSpace(query.ShopName))
            {
                if (null != orders)
                    orders.Where(p => p.ShopName.Contains(query.ShopName));

                history = history.Where(p => p.ShopName.Contains(query.ShopName));
            }
            if (!string.IsNullOrWhiteSpace(query.UserName))
            {
                var useridlist = DbFactory.Default.Get<MemberInfo>().Where(m => m.Nick.Contains(query.UserName)).Select(p => p.Id).ToList<long>();
                if (null != orders)
                {
                    orders.Where(p => p.UserName.Contains(query.UserName) || p.UserId.ExIn(useridlist));
                }
                history = history.Where(p => p.UserName.Contains(query.UserName) || p.UserId.ExIn(useridlist));
            }
            if (query.UserId.HasValue)
            {
                var userId = query.UserId.Value;
                if (null != orders)
                    orders.Where(p => p.UserId == userId);

                history = history.Where(p => p.UserId == userId);
            }

            history = ToPaymentWhere(orders, history, query);

            if (query.IgnoreSelfPickUp.HasValue)
            {
                if (query.IgnoreSelfPickUp.Value)
                {
                    orders.Where(p => p.DeliveryType != DeliveryType.SelfTake);
                }
                else
                {
                    orders.Where(p => p.DeliveryType == DeliveryType.SelfTake);
                }
            }
            if (query.Commented.HasValue)
            {
                var commented = query.Commented.Value;
                if (commented)
                {
                    if (null != orders)
                    {
                        var sub = DbFactory.Default
                            .Get<OrderCommentInfo>()
                            .Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id);
                        orders.Where(p => p.ExExists(sub));
                    }
                    var commentids = DbFactory.Default
                            .Get<OrderCommentInfo>()
                            .LeftJoin<OrderInfo>((oci, oi) => oci.OrderId == oi.Id)
                            .Where<OrderInfo>(n => n.Id.ExIsNull())
                            .Select(n => n.OrderId)
                            .Distinct()
                            .ToList<long>();
                    history = history.Where(p => p.Id.ExIn(commentids));
                }
                else
                {
                    if (null != orders)
                    {
                        var sub = DbFactory.Default
                            .Get<OrderCommentInfo>()
                            .Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id);
                        orders.Where(p => p.ExNotExists(sub));
                    }
                    var commentids = DbFactory.Default
                            .Get<OrderCommentInfo>()
                            .Select(n => n.OrderId)
                            .Distinct()
                            .ToList<long>();
                    history = history.Where(p => !commentids.Contains(p.Id));
                }
            }

            if (query.OrderType.HasValue)
            {
                var orderType = (PlatformType)query.OrderType.Value;
                if (null != orders)
                    orders.Where(item => item.Platform == orderType);

                history = history.Where(item => item.Platform == orderType);
            }

            if (query.Status.HasValue)
            {
                var _where = PredicateExtensions.False<OrderInfo>();
                var _wherehistory = PredicateExtensions.False<OrderInfo>();
                switch (query.Status)
                {
                    case OrderInfo.OrderOperateStatus.UnComment:
                        if (null != orders)
                        {
                            var comments = DbFactory.Default.Get<OrderCommentInfo>().Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id).Select(n => n.Id);
                            _where = _where.Or(d => d.ExNotExists(comments) && d.OrderStatus == OrderInfo.OrderOperateStatus.Finish);
                        }
                        var commentids = DbFactory.Default.Get<OrderCommentInfo>().Select(n => n.OrderId).Distinct();
                        if (query.UserId.HasValue && query.UserId.Value > 0) commentids.Where(n => n.UserId == query.UserId.Value);
                        var cids = commentids.ToList<long>();
                        _wherehistory = _wherehistory.Or(d => !cids.Contains(d.Id) && d.OrderStatus == OrderInfo.OrderOperateStatus.Finish);
                        break;
                    case OrderInfo.OrderOperateStatus.WaitDelivery:
                        var fgordids = DbFactory.Default
                               .Get<FightGroupOrderInfo>()
                               .Where(d => d.JoinStatus != (int)FightGroupOrderJoinStatus.BuildSuccess)
                               .Select(d => d.OrderId.ExIfNull(0))
                               .ToList<long>();
                        if (null != orders)
                        {
                            //处理拼团的情况
                            _where = _where.Or(d => d.OrderStatus == query.Status && d.Id.ExNotIn(fgordids));
                        }
                        _wherehistory = _wherehistory.Or(d => d.OrderStatus == query.Status && !fgordids.Contains(d.Id));
                        break;
                    case OrderInfo.OrderOperateStatus.History:
                        break;
                    default:
                        if (null != orders)
                            _where = _where.Or(d => d.OrderStatus == query.Status);
                        _wherehistory = _wherehistory.Or(d => d.OrderStatus == query.Status);

                        //如果是前端待收货状态时也查询出待消费的订单
                        if (query.IsFront && query.Status == OrderInfo.OrderOperateStatus.WaitReceiving)
                        {
                            if (null != orders)
                            {
                                _where = _where.Or(d => d.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification);
                            }
                            _wherehistory = _wherehistory.Or(d => d.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification);
                        }

                        break;
                }


                if (query.MoreStatus != null)
                {
                    foreach (var stitem in query.MoreStatus)
                    {
                        if (null != orders)
                            _where = _where.Or(d => d.OrderStatus == stitem);
                        _wherehistory = _wherehistory.Or(d => d.OrderStatus == stitem);
                    }
                }

                if (null != orders) orders.Where(_where);
                history = history.Where(_wherehistory);
            }

            if (query.PaymentType != OrderInfo.PaymentTypes.None)
            {
                if (null != orders)
                    orders.Where(item => item.PaymentType == query.PaymentType);

                history = history.Where(item => item.PaymentType == query.PaymentType);
            }

            if (query.IsBuyRecord)//购买记录只查询付了款的
            {
                if (null != orders)
                    orders.Where(a => a.PayDate.HasValue);

                history = history.Where(a => a.PayDate.HasValue);
            }

            if (!string.IsNullOrEmpty(query.VerificationCode))
            {
                if (query.IsVirtual.HasValue && query.IsVirtual.Value)
                {
                    var orderIds = DbFactory.Default
                       .Get<OrderVerificationCodeInfo>()
                       .Where(p => p.VerificationCode == query.VerificationCode)
                       .Select(p => p.OrderId)
                       .ToList<long>();
                    if (null != orders)
                        orders.Where(a => a.Id.ExIn(orderIds));

                    history = history.Where(a => a.Id.ExIn(orderIds));
                }
                else
                {
                    if (null != orders)
                        orders.Where(a => a.PickupCode.Contains(query.VerificationCode));

                    history = history.Where(a => a.PickupCode.Contains(query.VerificationCode));
                }
            }

            if (query.PayDateStart.HasValue)
            {
                if (null != orders)
                    orders.Where(a => a.PayDate >= query.PayDateStart.Value);

                history = history.Where(a => a.PayDate >= query.PayDateStart.Value);
            }
            if (query.PayDateEnd.HasValue)
            {
                if (null != orders)
                    orders.Where(a => a.PayDate.Value.Date <= query.PayDateEnd.Value.Date);

                history = history.Where(a => a.PayDate.Value.Date <= query.PayDateEnd.Value.Date);
            }
            if (query.VerificationTimeStart.HasValue)
            {
                if (null != orders)
                    orders.Where(a => a.FinishDate >= query.VerificationTimeStart.Value);

                history = history.Where(a => a.FinishDate >= query.VerificationTimeStart.Value);
            }
            if (query.VerificationTimeEnd.HasValue)
            {
                if (null != orders)
                    orders.Where(a => a.FinishDate.Value.Date <= query.VerificationTimeEnd.Value.Date);

                history = history.Where(a => a.FinishDate.Value.Date <= query.VerificationTimeEnd.Value.Date);
            }


            //开始结束时间
            if (query.StartDate.HasValue)
            {
                DateTime sdt = query.StartDate.Value;
                if (null != orders)
                    orders.Where(d => d.OrderDate >= sdt);

                history = history.Where(d => d.OrderDate >= sdt);
            }
            if (query.EndDate.HasValue)
            {
                DateTime edt = query.EndDate.Value.AddDays(1);
                if (null != orders)
                    orders.Where(d => d.OrderDate < edt);

                history = history.Where(d => d.OrderDate < edt);
            }

            if ((query.ShopBranchId.HasValue && query.ShopBranchId.Value > 0) || !string.IsNullOrWhiteSpace(query.ShopBranchName))
            {
                if (null != orders)
                    // orders.Where(p => p.DeliveryType == CommonModel.DeliveryType.SelfTake);
                    orders.Where(p => p.DeliveryType == DeliveryType.SelfTake || p.ShopBranchId.ExIfNull(0) > 0);//3.0版本新增订单自动分配到门店，其配送方式不是到店自提但仍属于门店订单

                history = history.Where(p => p.DeliveryType == DeliveryType.SelfTake || p.ShopBranchId > 0);//3.0版本新增订单自动分配到门店，其配送方式不是到店自提但仍属于门店订单

                if (query.ShopBranchId.HasValue)
                {
                    var shopBranchId = query.ShopBranchId.Value;
                    if (null != orders)
                        orders.Where(p => p.ShopBranchId == shopBranchId);

                    history = history.Where(p => p.ShopBranchId == shopBranchId);
                }
                else
                {
                    var sbIds = DbFactory.Default
                        .Get<ShopBranchInfo>()
                        .Where(p => p.ShopBranchName.Contains(query.ShopBranchName))
                        .Select(p => p.Id)
                        .ToList<long>();
                    if (null != orders)
                        orders.Where(p => p.ShopBranchId.ExIn(sbIds));

                    history = history.Where(p => sbIds.Contains(p.ShopBranchId));
                }
            }

            if (query.IsBranchShop)
            {//只查询门店的订单 
                if (null != orders)
                    orders.Where(p => p.ShopBranchId > 0);

                history = history.Where(p => p.ShopBranchId > 0);
            }

            if (!string.IsNullOrEmpty(query.UserContact))
            {
                if (null != orders)
                    orders.Where(p => p.CellPhone.StartsWith(query.UserContact));

                history = history.Where(p => p.CellPhone.StartsWith(query.UserContact));
            }
            return history;
        }
        #endregion

        #region 分配门店
        /// <summary>
        /// 商家订单分配门店时更新商家、门店库存(单个订单)
        /// </summary>
        /// <param name="skuIds"></param>
        /// <param name="quantity"></param>
        public void AllotStoreUpdateStock(List<string> skuIds, List<int> counts, long shopBranchId)
        {
            if (skuIds.Count > 0)
            {
                DbFactory.Default
                     .InTransaction(() =>
                     {
                         int quantity = 0; string skuId = string.Empty;
                         for (int i = 0; i < skuIds.Count(); i++)
                         {
                             skuId = skuIds[i];
                             quantity = counts.ElementAt(i);
                             SKUInfo sku = DbFactory.Default.Get<SKUInfo>().Where(p => p.Id == skuId).FirstOrDefault();
                             if (sku == null)
                             {
                                 throw new HimallException("门店商品库存不足");
                             }

                             sku.Stock += quantity;
                             DbFactory.Default.Update(sku);

                             ShopBranchSkuInfo sbSku = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == shopBranchId && e.SkuId == sku.Id).FirstOrDefault();
                             if (sbSku != null)
                             {
                                 sbSku.Stock -= quantity;
                                 DbFactory.Default.Update(sbSku);
                             }
                             if (sbSku.Stock < 0)
                                 throw new HimallException("门店商品库存不足");
                         }
                     });
            }
        }

        /// <summary>
        /// 更改旧门店订单到新门店(单个订单)
        /// </summary>
        /// <param name="stuIds"></param>
        /// <param name="newShopBranchId"></param>
        /// <param name="oldShopBranchId"></param>
        public void AllotStoreUpdateStockToNewShopBranch(List<string> skuIds, List<int> counts, long newShopBranchId, long oldShopBranchId)
        {
            DbFactory.Default
                 .InTransaction(() =>
                 {
                     int quantity = 0; string skuId = string.Empty;
                     for (int i = 0; i < skuIds.Count(); i++)
                     {
                         skuId = skuIds[i];
                         quantity = counts.ElementAt(i);
                         ShopBranchSkuInfo sbSkuNew = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == newShopBranchId && e.SkuId == skuId).FirstOrDefault();
                         if (sbSkuNew == null)
                         {
                             throw new HimallException("门店商品库存不足");
                         }

                         sbSkuNew.Stock -= quantity;
                         DbFactory.Default.Update(sbSkuNew);

                         ShopBranchSkuInfo sbSkuOld = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == oldShopBranchId && e.SkuId == skuId).FirstOrDefault();
                         if (sbSkuOld != null)
                         {
                             sbSkuOld.Stock += quantity;
                             DbFactory.Default.Update(sbSkuOld);
                         }
                         if (sbSkuNew.Stock < 0)
                             throw new HimallException("门店商品库存不足");
                     }
                 });
        }

        /// <summary>
        /// 更改门店订单回到商家(单个订单)
        /// </summary>
        public void AllotStoreUpdateStockToShop(List<string> skuIds, List<int> counts, long shopBranchId)
        {
            DbFactory.Default
                .InTransaction(() =>
                {
                    int quantity = 0; string skuId = string.Empty;
                    for (int i = 0; i < skuIds.Count(); i++)
                    {
                        skuId = skuIds[i];
                        quantity = counts.ElementAt(i);
                        SKUInfo sku = DbFactory.Default.Get<SKUInfo>().Where(p => p.Id == skuId).FirstOrDefault();
                        if (sku == null)
                        {
                            throw new HimallException("商品库存不足");
                        }
                        sku.Stock -= quantity;
                        if (sku.Stock < 0)
                            throw new HimallException("商品库存不足");
                        DbFactory.Default.Update(sku);

                        ShopBranchSkuInfo sbSku = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == shopBranchId && e.SkuId == skuId).FirstOrDefault();
                        if (sbSku != null)
                        {
                            sbSku.Stock += quantity;
                            DbFactory.Default.Update(sbSku);
                        }
                    }
                });
        }
        /// <summary>
        /// 更新订单所属门店
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shopBranchId"></param>
        public void UpdateOrderShopBranch(long orderId, long shopBranchId)
        {
            var orderdata = DbFactory.Default.Get<OrderInfo>().Where(d => d.Id == orderId).FirstOrDefault();
            if (orderdata == null)
            {
                throw new HimallException("错误的订单编号");
            }
            orderdata.ShopBranchId = shopBranchId;
            DbFactory.Default.Update(orderdata);
        }
        #endregion
        #region 门店/商家APP发货消息推送
        public void SendAppMessage(OrderInfo orderInfo)
        {
            var app = new AppMessageInfo()
            {
                Content = string.Format("{0} 等待您发货", orderInfo.Id),
                IsRead = false,
                sendtime = DateTime.Now,
                SourceId = orderInfo.Id,
                Title = "您有新的订单",
                TypeId = (int)AppMessagesType.Order,
                OrderPayDate = Core.Helper.TypeHelper.ObjectToDateTime(orderInfo.PayDate),
                ShopId = 0,
                ShopBranchId = 0
            };
            if (orderInfo.ShopBranchId > 0)
            {
                app.ShopBranchId = orderInfo.ShopBranchId;
            }
            else app.ShopId = orderInfo.ShopId;

            if (orderInfo.DeliveryType == DeliveryType.SelfTake)
            {
                app.Title = "您有新自提订单";
                app.TypeId = (int)AppMessagesType.Order;
                app.Content = string.Format("{0} 等待您备货", orderInfo.Id);
            }
            _iAppMessageService.AddAppMessages(app);
        }
        #endregion
        public List<long> GetOrderIdsByLatestTime(int time, long shopBranchId, long shopId)
        {
            var timeformat = string.Format("DATE_SUB(NOW(),INTERVAL {0} MINUTE)", time);
            var orders = DbFactory.Default
                .Get<OrderInfo>()
                .Select(n => n.Id)
                //.Where(n => n.ShopBranchId.ExIfNull(0) == shopBranchId && n.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay &&
                //    n.OrderStatus != OrderInfo.OrderOperateStatus.Close && n.OrderStatus != OrderInfo.OrderOperateStatus.Finish &&
                //    n.OrderStatus != OrderInfo.OrderOperateStatus.UnComment && n.OrderType!= OrderInfo.OrderTypes.Virtual && n.OrderDate >= timeformat.ExFormat<DateTime>());
                .Where(n => n.PayDate >= timeformat.ExFormat<DateTime>() && n.ShopBranchId.ExIfNull(0) == shopBranchId && n.OrderType != OrderInfo.OrderTypes.Virtual
                && (n.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || n.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp));
            if (shopId > 0)
            {
                orders.Where(n => n.ShopId == shopId);
            }
            return orders.ToList<long>();
        }
        /// <summary>
        /// 获取最近语音播报的订单数据
        /// </summary>
        /// <param name="time"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public List<long> GetOrderIdsByLatestTimeYuyin(int time, long shopBranchId, long shopId)
        {
            var timeformat = string.Format("DATE_SUB(NOW(),INTERVAL {0} MINUTE)", time);
            var orders = DbFactory.Default
                .Get<OrderInfo>()
                .Select(n => n.Id)
                .Where(n => n.PayDate >= timeformat.ExFormat<DateTime>() && n.ShopBranchId.ExIfNull(0) == shopBranchId
                && (n.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || n.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification || n.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp));
            if (shopId > 0)
            {
                orders.Where(n => n.ShopId == shopId);
            }
            return orders.ToList<long>();
        }
        /// <summary>
        /// 获取订单统计数据
        /// </summary>
        public OrderStatisticsItem GetOrderCountStatistics(OrderCountStatisticsQuery query)
        {
            var db = DbFactory.Default.Get<OrderInfo>();

            #region Where
            if (query.UserId > 0)
                db.Where(p => p.UserId == query.UserId);
            if (query.ShopId > 0)
                db.Where(p => p.ShopId == query.ShopId);
            if (query.ShopBranchId > 0)
                db.Where(p => p.ShopBranchId == query.ShopBranchId);

            if (query.OrderDateBegin.HasValue)
                db.Where(p => p.OrderDate > query.OrderDateBegin.Value);
            if (query.OrderDateEnd.HasValue)
                db.Where(p => p.OrderDate < query.OrderDateEnd.Value);

            //已支付订单
            if (query.IsPayed.HasValue && query.IsPayed.Value)
                db.Where(p => p.PayDate.ExIsNotNull());
            if (query.OrderOperateStatusList != null)
            {
                if (query.OrderOperateStatus.HasValue)
                    query.OrderOperateStatusList.Add(query.OrderOperateStatus.Value);
                db.Where(p => p.OrderStatus.ExIn(query.OrderOperateStatusList));
            }
            else if (query.OrderOperateStatus.HasValue)
            {
                //db.Where(p => p.OrderStatus == query.OrderOperateStatus.Value);
                switch (query.OrderOperateStatus)
                {
                    case OrderInfo.OrderOperateStatus.WaitDelivery:
                        var fgordids = DbFactory.Default
                               .Get<FightGroupOrderInfo>()
                               .Where(d => d.JoinStatus != 4)
                               .Select(d => d.OrderId.ExIfNull(0))
                               .ToList<long>();
                        //处理拼团的情况
                        db.Where(d => d.OrderStatus == query.OrderOperateStatus && d.Id.ExNotIn(fgordids));
                        break;
                    default:
                        db.Where(p => p.OrderStatus == query.OrderOperateStatus.Value);
                        break;
                }
            }

            if (query.IsCommented.HasValue)
            {
                if (query.IsCommented.Value)
                {
                    var sub = DbFactory.Default
                        .Get<OrderCommentInfo>()
                        .Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id);
                    db.Where(p => p.ExExists(sub));
                }
                else
                {
                    var sub = DbFactory.Default
                        .Get<OrderCommentInfo>()
                        .Where<OrderInfo>((oci, oi) => oci.OrderId == oi.Id);
                    db.Where(p => p.ExNotExists(sub));
                }
            }

            if (query.IsVirtual.HasValue)
            {
                if (query.IsVirtual.Value)
                    db.Where(p => p.OrderType == OrderInfo.OrderTypes.Virtual);//是虚拟订单
                else
                    db.Where(p => p.OrderType != OrderInfo.OrderTypes.Virtual);//非虚拟订单
            }
            #endregion

            var result = new OrderStatisticsItem();

            #region Fields
            if (query.Fields.Contains(OrderCountStatisticsFields.ActualPayAmount))
                result.TotalActualPayAmount = db.Sum(p => p.ActualPayAmount);
            if (query.Fields.Contains(OrderCountStatisticsFields.OrderCount))
                result.OrderCount = db.Count();
            #endregion
            return result;
        }
        public void AutoComment()
        {
            //windows服务调用此方法不报错
            var orderCommentTimeout = 0;
            var expriedtemp = DbFactory.Default.Query<string>("Select `Value` from Himall_SiteSetting where `Key`='OrderCommentTimeout'").FirstOrDefault();
            int.TryParse(expriedtemp, out orderCommentTimeout);
            //自动订单评价天数
            int intIntervalDay = orderCommentTimeout == 0 ? 7 : orderCommentTimeout;
            DateTime waitCommentDate = DateTime.Now.AddDays(-intIntervalDay);

            var comments = DbFactory.Default.Get<OrderCommentInfo>().Select(p => p.OrderId);
            var query = DbFactory.Default.Get<OrderInfo>().Where(a => a.FinishDate < waitCommentDate && a.OrderStatus == OrderInfo.OrderOperateStatus.Finish && a.Id.ExNotIn(comments));

            var info = query.ToList();
            try
            {
                AutoOrderComment(info);
                AutoProductComment(info);
            }
            catch (Exception ex)
            {
                Log.Error("AutoCommnetOrder:" + ex.Message + "/r/n" + ex.StackTrace);
            }
        }

        private void AutoOrderComment(List<OrderInfo> orders)
        {
            foreach (var order in orders)
            {
                OrderCommentInfo info = new OrderCommentInfo();
                info.UserId = order.UserId;
                info.PackMark = 5;
                info.DeliveryMark = 5;
                info.ServiceMark = 5;
                info.OrderId = order.Id;
                info.ShopId = order.ShopId;
                info.ShopName = order.ShopName;
                info.UserName = order.UserName;
                info.CommentDate = DateTime.Now;
                DbFactory.Default.Add(info);
            }
        }
        private void AutoProductComment(List<OrderInfo> orders)
        {
            foreach (var order in orders)
            {
                var orderItems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == order.Id).ToList();
                foreach (var item in orderItems)
                {
                    ProductCommentInfo model = new ProductCommentInfo();
                    model.ReviewDate = DateTime.Now;
                    model.ReviewContent = "好评!";
                    model.UserId = order.UserId;
                    model.UserName = order.UserName;
                    model.Email = "";
                    model.SubOrderId = item.Id;
                    model.ReviewMark = 5;
                    model.ShopId = order.ShopId;
                    model.ProductId = item.ProductId;
                    model.ShopName = order.ShopName;
                    model.IsHidden = false;
                    DbFactory.Default.Add(model);

                    //更新商品评论数
                    var searchProduct = DbFactory.Default.Get<SearchProductInfo>().Where(r => r.ProductId == item.ProductId).FirstOrDefault();
                    if (searchProduct != null)
                    {
                        DbFactory.Default.Set<SearchProductInfo>().Set(p => p.Comments, searchProduct.Comments + 1).Where(p => p.Id == searchProduct.Id).Succeed();
                    }
                }
            }
        }

        public List<VirtualOrderItemInfo> GetVirtualOrderItemInfosByOrderId(long orderId)
        {
            return DbFactory.Default.Get<VirtualOrderItemInfo>().Where(a => a.OrderId == orderId).ToList();
        }
        public List<OrderVerificationCodeInfo> GetOrderVerificationCodeInfosByOrderIds(List<long> orderIds)
        {
            return DbFactory.Default.Get<OrderVerificationCodeInfo>().Where(a => a.OrderId.ExIn(orderIds)).ToList();
        }
        public OrderVerificationCodeInfo GetOrderVerificationCodeInfoByCode(string verificationCode)
        {
            return DbFactory.Default.Get<OrderVerificationCodeInfo>().Where(a => a.VerificationCode == verificationCode).FirstOrDefault();
        }
        public List<OrderVerificationCodeInfo> GetOrderVerificationCodeInfoByCodes(List<string> verificationCodes)
        {
            return DbFactory.Default.Get<OrderVerificationCodeInfo>().Where(a => a.VerificationCode.ExIn(verificationCodes)).ToList();
        }
        public bool UpdateOrderVerificationCodeStatusByCodes(List<string> verficationCodes, long orderId, OrderInfo.VerificationCodeStatus status, DateTime? verificationTime, string verificationUser = "")
        {
            bool result = false;
            if (verificationTime.HasValue)
            {
                result = DbFactory.Default.Set<OrderVerificationCodeInfo>().Set(p => p.Status, status).Set(a => a.VerificationTime, verificationTime.Value).Set(a => a.VerificationUser, verificationUser).Where(p => p.VerificationCode.ExIn(verficationCodes)).Succeed();
            }
            else
            {
                result = DbFactory.Default.Set<OrderVerificationCodeInfo>().Set(p => p.Status, status).Where(p => p.VerificationCode.ExIn(verficationCodes)).Succeed();
            }
            OrderInfo.OrderOperateStatus orderStatus = 0;
            var orderVerificationCodes = GetOrderVerificationCodeInfosByOrderIds(new List<long>() { orderId });
            int count1 = orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification || a.Status == OrderInfo.VerificationCodeStatus.Refund).Count();
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

                    var order = GetOrder(orderId);
                    //会员确认收货后，不会马上给积分，得需要过了售后维权期才给积分,（虚拟商品除外）
                    var member = DbFactory.Default.Get<MemberInfo>().Where(a => a.Id == order.UserId).FirstOrDefault();
                    // AddIntegral(member, order.Id, order.TotalAmount - order.RefundTotalAmount);//增加积分
                    //更新待结算订单完成时间
                    UpdatePendingSettlnmentFinishDate(orderId, DateTime.Now);
                }
                else
                {
                    DbFactory.Default.Set<OrderInfo>().Set(p => p.OrderStatus, orderStatus).Where(a => a.Id == orderId).Succeed();
                }
                //订单付款时，就已经写入待结算
                //虚拟订单，订单状态为已完成或已关闭时，就给商家结算
                //if (orderStatus == OrderInfo.OrderOperateStatus.Finish || orderStatus == OrderInfo.OrderOperateStatus.Close)
                //{
                //    Log.Info(string.Format("订单状态：{0},写入待结算", orderStatus.ToDescription()));
                //    var order = DbFactory.Default.Get<OrderInfo>(a => a.Id == orderId).FirstOrDefault();
                //    var model = DbFactory.Default.Get<PendingSettlementOrderInfo>().Where(a => a.OrderId == orderId).FirstOrDefault();
                //    if (model != null)
                //    {
                //        DbFactory.Default.Delete<PendingSettlementOrderInfo>(model);
                //    }
                //    //每次状态变化时重新写入结算
                //    WritePendingSettlnment(order);
                //}
            }
            if (status == OrderInfo.VerificationCodeStatus.AlreadyVerification && verificationTime.HasValue)
            {
                SendMessageOnVirtualOrderVerificationSuccess(orderId, verficationCodes, verificationTime.Value);
            }
            return result;
        }
        public VerificationRecordInfo GetVerificationRecordInfoById(long id)
        {
            return DbFactory.Default.Get<VerificationRecordInfo>().Where(p => p.Id == id).FirstOrDefault();
        }
        public bool AddVerificationRecord(VerificationRecordInfo info)
        {
            return DbFactory.Default.Add(info);
        }
        public bool AddVirtualOrderItemInfo(List<VirtualOrderItemInfo> infos)
        {
            return DbFactory.Default.Add<VirtualOrderItemInfo>(infos);
        }
        public bool AddOrderVerificationCodeInfo(List<OrderVerificationCodeInfo> infos)
        {
            return DbFactory.Default.Add<OrderVerificationCodeInfo>(infos);
        }
        public int GetWaitConsumptionOrderNumByUserId(long userId = 0, long shopId = 0, long shopBranchId = 0)
        {
            if (shopId > 0)
                return DbFactory.Default.Get<OrderInfo>().Where(a => a.ShopId == shopId && a.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification).Count();
            else if (shopBranchId > 0)
                return DbFactory.Default.Get<OrderInfo>().Where(a => a.ShopBranchId == shopBranchId && a.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification).Count();

            return DbFactory.Default.Get<OrderInfo>().Where(a => a.UserId == userId && a.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification).Count();
        }

        /// <summary>
        /// 虚拟订单信息项实体
        /// </summary>
        /// <param name="orderIds">订单号集合</param>
        /// <returns></returns>
        public List<VirtualOrderItemInfo> GeVirtualOrderItemsByOrderId(IEnumerable<long> orderIds)
        {
            return DbFactory.Default.Get<VirtualOrderItemInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
        }

        public QueryPageModel<OrderVerificationCodeInfo> GetOrderVerificationCodeInfos(VerificationRecordQuery query)
        {
            var db = WhereVerificationCodeBuilder(query);
            db = db.OrderByDescending(p => "PayDate");

            var data = db.ToPagedList(query.PageNo, query.PageSize);

            return new QueryPageModel<OrderVerificationCodeInfo>()
            {
                Models = data,
                Total = data.TotalRecordCount
            };
        }

        private GetBuilder<OrderVerificationCodeInfo> WhereVerificationCodeBuilder(VerificationRecordQuery query)
        {
            var db = DbFactory.Default.Get<OrderVerificationCodeInfo>()
                .LeftJoin<OrderInfo>((fi, pi) => fi.OrderId == pi.Id)
                .Select()
                .Select<OrderInfo>(p => new { p.PayDate, p.ShopBranchId, p.ShopId });
            if (query.ShopBranchId.HasValue && query.ShopBranchId.Value > 0)
            {
                var ordersql = DbFactory.Default
                    .Get<OrderInfo>()
                    .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopBranchId == query.ShopBranchId.Value);
                db.Where(p => p.ExExists(ordersql));
            }
            else if (query.IsAll)
            {
                var ordersql = DbFactory.Default
                    .Get<OrderInfo>()
                    .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopId == query.ShopId.Value);
                db.Where(p => p.ExExists(ordersql));
            }
            else if (query.IsShop)
            {
                var ordersql = DbFactory.Default
                   .Get<OrderInfo>()
                   .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopId == query.ShopId.Value && si.ShopBranchId == 0);
                db.Where(p => p.ExExists(ordersql));
            }

            //该字段有值肯定是选了商家或门店后传过来的
            if (query.Type.HasValue)
            {
                if (query.Type.Value == 1)
                {
                    var ordersql = DbFactory.Default.Get<OrderInfo>()
                                    .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopId == query.SearchId);
                    db.Where(p => p.ExExists(ordersql));
                }
                else if (query.Type.Value == 2)
                {
                    var ordersql = DbFactory.Default.Get<OrderInfo>()
                                    .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopBranchId == query.SearchId);
                    db.Where(p => p.ExExists(ordersql));
                }
            }
            else if (!string.IsNullOrWhiteSpace(query.ShopBranchName))
            {
                var _where = PredicateExtensions.False<OrderVerificationCodeInfo>();
                var shops = DbFactory.Default.Get<ShopInfo>().Where(a => a.ShopName.Contains(query.ShopBranchName)).ToList();
                var shopIds = shops.Select(a => a.Id);
                var count = shops.Count;
                var shopName = "";
                if (count == 1)
                    shopName = shops.FirstOrDefault().ShopName;

                if (count == 1 && shopName == query.ShopBranchName)//如果模糊查询只有一个，而且搜索词与商家名称相同，则为检索商家全称，则会把此商家及门店的所有订单都检索出来
                {
                    var _ordersql = DbFactory.Default.Get<OrderInfo>()
                                             .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopId.ExIn(shopIds));
                    _where = _where.Or(p => p.ExExists(_ordersql));
                }
                else
                {
                    var _ordersql = DbFactory.Default.Get<OrderInfo>()
                                                .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopId.ExIn(shopIds) && si.ShopBranchId == 0);
                    _where = _where.Or(p => p.ExExists(_ordersql));

                    var shopBranchIds = DbFactory.Default.Get<ShopBranchInfo>().Where(a => a.ShopBranchName.Contains(query.ShopBranchName)).Select(a => a.Id);
                    var ordersql = DbFactory.Default.Get<OrderInfo>()
                                                 .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.ShopBranchId.ExIn(shopBranchIds));
                    _where = _where.Or(p => p.ExExists(ordersql));
                }

                db.Where(_where);
            }

            var orderIdRange = GetOrderIdRange(query.OrderId);
            if (orderIdRange != null)
            {
                var min = orderIdRange[0];
                if (orderIdRange.Length == 2)
                {
                    var max = orderIdRange[1];
                    db.Where(item => item.OrderId >= min && item.OrderId <= max);
                }
                else
                    db.Where(item => item.OrderId == min);
            }
            if (query.Status.HasValue)
            {
                db.Where(item => item.Status == query.Status.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.VerificationCode))
            {
                db.Where(item => item.VerificationCode == query.VerificationCode && item.Status != OrderInfo.VerificationCodeStatus.WaitVerification && item.Status != OrderInfo.VerificationCodeStatus.Refund);
            }

            if (query.PayDateStart.HasValue)
            {
                DateTime sdt = query.PayDateStart.Value;
                var ordersql = DbFactory.Default
                    .Get<OrderInfo>()
                    .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.PayDate >= sdt);
                db.Where(p => p.ExExists(ordersql));
            }
            if (query.PayDateEnd.HasValue)
            {
                DateTime sdt = query.PayDateEnd.Value.AddDays(1).AddSeconds(-1);
                var ordersql = DbFactory.Default
                    .Get<OrderInfo>()
                    .Where<OrderInfo, OrderVerificationCodeInfo>((si, pi) => si.Id == pi.OrderId && si.PayDate <= sdt);
                db.Where(p => p.ExExists(ordersql));
            }

            if (query.VerificationTimeStart.HasValue)
            {
                db.Where(item => item.VerificationTime >= query.VerificationTimeStart.Value);
            }

            if (query.VerificationTimeEnd.HasValue)
            {
                DateTime sdt = query.VerificationTimeEnd.Value.AddDays(1).AddSeconds(-1);
                db.Where(item => item.VerificationTime <= sdt);
            }

            return db;
        }

        public List<SearchShopAndShopbranchModel> GetShopOrShopBranch(string keyword, sbyte? type)
        {
            List<SearchShopAndShopbranchModel> list = new List<SearchShopAndShopbranchModel>();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var shops = DbFactory.Default.Get<ShopInfo>().Where(a => a.ShopName.Contains(keyword)).ToList();
                shops.ForEach(a =>
                {
                    list.Add(new SearchShopAndShopbranchModel()
                    {
                        Name = a.ShopName,
                        Type = 1,
                        SearchId = a.Id
                    });
                });
                var shopbranchs = DbFactory.Default.Get<ShopBranchInfo>().Where(a => a.ShopBranchName.Contains(keyword)).ToList();
                shopbranchs.ForEach(a =>
                {
                    list.Add(new SearchShopAndShopbranchModel()
                    {
                        Name = a.ShopBranchName,
                        Type = 2,
                        SearchId = a.Id
                    });
                });
                if (type.HasValue)
                {
                    if (type.Value == 1)
                    {
                        list.RemoveAll(a => a.Type == 2);
                    }
                    else if (type.Value == 2)
                    {
                        list.RemoveAll(a => a.Type == 1);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// 待结算实体
        /// </summary>
        /// <param name="orderIds">订单号集合</param>
        /// <returns></returns>
        public List<PendingSettlementOrderInfo> GetPendingSettlementOrdersByOrderId(IEnumerable<long> orderIds)
        {
            return DbFactory.Default.Get<PendingSettlementOrderInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
        }

        /// <summary>
        /// 已结算订单集合
        /// </summary>
        /// <param name="orderIds">订单号集合</param>
        /// <returns></returns>
        public List<AccountDetailInfo> GetAccountDetailByOrderId(IEnumerable<long> orderIds)
        {
            return DbFactory.Default.Get<AccountDetailInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
        }

        /// <summary>
        /// 订单发票实体集合
        /// </summary>
        /// <param name="orderIds">订单号集合</param>
        /// <returns></returns>
        public List<OrderInvoiceInfo> GetOrderInvoicesByOrderId(IEnumerable<long> orderIds)
        {
            return DbFactory.Default.Get<OrderInvoiceInfo>().Where(p => p.OrderId.ExIn(orderIds)).ToList();
        }

        /// <summary>
        /// 获取订单发票实体
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public OrderInvoiceInfo GetOrderInvoiceInfo(long orderId)
        {
            var model = DbFactory.Default.Get<OrderInvoiceInfo>().Where(o => o.OrderId == orderId).FirstOrDefault();
            if (model != null)
                model.RegionFullName = ServiceProvider.Instance<RegionService>.Create.GetFullName(model.RegionID);
            return model;
        }


        #region  旺店通订单推送
        /// <summary>
        /// 获取未推送的待发货订单
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<OrderInfo> GetPushOrders(int count,long shopId)
        {
            var model = DbFactory.Default.Get<OrderInfo>().Where(o => o.ShopId == shopId && o.IsPushWangDian == false && o.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay && o.OrderStatus != OrderInfo.OrderOperateStatus.Close && o.OrderStatus != OrderInfo.OrderOperateStatus.History && o.OrderType != OrderInfo.OrderTypes.Virtual).Take(count).ToList();
            var modelitem = DbFactory.Default.Get<OrderItemInfo>().Where(item => item.OrderId.ExIn(model.Select(o => o.Id))).ToList();
            var invoiceItems = DbFactory.Default.Get<OrderInvoiceInfo>().Where(item => item.OrderId.ExIn(model.Select(o => o.Id))).ToList();
            if (invoiceItems == null) { invoiceItems = new List<OrderInvoiceInfo>(); }
            model.ForEach(order =>
            {
                order.OrderItemInfo = modelitem.Where(item => item.OrderId == order.Id).ToList();
                order.OrderInvoice = invoiceItems.Where(i => i.OrderId == order.Id).FirstOrDefault();
            });
            return model;
        }
        public void UpdateOrderPushState(IEnumerable<long> orderIds)
        {
            DbFactory.Default.Set<OrderInfo>().Set(p => p.IsPushWangDian, true).Set(p => p.PushWangDianResult, 0).Where(m => m.Id.ExIn(orderIds)).Succeed();
        }
        #endregion

        #region 私有方法
        private void UpdateOrderItemEffectiveDateByIds(List<long> orderItemIds, DateTime? payDate)
        {
            if (payDate != null)
            {
                var orderItems = DbFactory.Default.Get<OrderItemInfo>(p => p.Id.ExIn(orderItemIds)).ToList();
                var virtualProducts = ServiceProvider.Instance<ProductService>.Create.GetVirtualProductInfoByProductIds(orderItems.Select(a => a.ProductId).ToList());
                foreach (var item in orderItems)
                {
                    var virtualPInfo = virtualProducts.FirstOrDefault(a => a.ProductId == item.ProductId);
                    if (virtualPInfo != null)
                    {
                        if (virtualPInfo.EffectiveType == 1)
                        {
                            item.EffectiveDate = DateTime.Now;
                        }
                        else if (virtualPInfo.EffectiveType == 2)
                        {
                            item.EffectiveDate = payDate.Value.AddHours(virtualPInfo.Hour);
                        }
                        else if (virtualPInfo.EffectiveType == 3)
                        {
                            item.EffectiveDate = DateTime.Parse(payDate.Value.AddDays(1).ToString("yyyy-MM-dd"));
                        }
                        DbFactory.Default.Update(item);
                    }
                }
            }
        }

        private void AddOrderVerificationCodeInfo(long quantitry, long orderId, long orderItemId)
        {
            var hasCode = DbFactory.Default.Get<OrderVerificationCodeInfo>().Where(p => p.OrderId == orderId).Count();
            if (hasCode >= quantitry) return;
            var codes = new List<OrderVerificationCodeInfo>();
            for (int i = 0; i < quantitry; i++)
            {
                codes.Add(new OrderVerificationCodeInfo()
                {
                    OrderId = orderId,
                    OrderItemId = orderItemId,
                    Status = OrderInfo.VerificationCodeStatus.WaitVerification,
                    VerificationCode = GenerateRandomCode(12),
                    VerificationUser = ""
                });
            }
            AddOrderVerificationCodeInfo(codes);
        }
        private void SendMessageOnVirtualOrderPay(OrderInfo orderInfo, long productId)
        {
            if (orderInfo == null)
            {
                return;
            }
            var virtualOrderMessage = new MessageVirtualOrderInfo();
            virtualOrderMessage.OrderId = orderInfo.Id.ToString();
            virtualOrderMessage.ShopId = orderInfo.ShopId;
            virtualOrderMessage.SiteName = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;

            if (orderInfo.ShopBranchId > 0)
            {
                var shopBranchInfo = DbFactory.Default.Get<ShopBranchInfo>().Where(a => a.Id == orderInfo.ShopBranchId).FirstOrDefault();
                if (shopBranchInfo != null)
                {
                    virtualOrderMessage.ShopName = shopBranchInfo.ShopBranchName;//门店名称
                    virtualOrderMessage.Phone = shopBranchInfo.ContactPhone;//门店电话
                    virtualOrderMessage.Address = ServiceProvider.Instance<RegionService>.Create.GetFullName(shopBranchInfo.AddressId, CommonConst.ADDRESS_PATH_SPLIT) + CommonConst.ADDRESS_PATH_SPLIT + shopBranchInfo.AddressDetail;//门店地址
                }
            }
            else
            {
                virtualOrderMessage.ShopName = orderInfo.ShopName;//商家名称
                var shopInfo = DbFactory.Default.Get<ShopShipperInfo>().Where(a => a.ShopId == orderInfo.ShopId && a.IsDefaultSendGoods).FirstOrDefault();
                if (shopInfo != null)
                {
                    virtualOrderMessage.Phone = shopInfo.TelPhone;//商家电话
                    virtualOrderMessage.Address = ServiceProvider.Instance<RegionService>.Create.GetFullName(shopInfo.RegionId, CommonConst.ADDRESS_PATH_SPLIT) + CommonConst.ADDRESS_PATH_SPLIT + shopInfo.Address;//门店地址
                }
            }
            var verificationCodes = DbFactory.Default.Get<OrderVerificationCodeInfo>(a => a.OrderId == orderInfo.Id).ToList().Select(a => a.VerificationCode).ToList();
            if (verificationCodes != null && verificationCodes.Count > 0)
            {
                var codeStr = "";
                int i = 1;
                foreach (var code in verificationCodes)
                {
                    if (i >= 10)
                        codeStr += "...";
                    else
                        codeStr += code + ",";
                }
                virtualOrderMessage.VerificationCodes = codeStr;//到店的核销码
            }
            var virtualProduct = DbFactory.Default.Get<VirtualProductInfo>().Where(a => a.ProductId == productId).FirstOrDefault();
            if (virtualProduct != null)
            {
                if (virtualProduct.ValidityType && virtualProduct.EndDate.HasValue)
                {
                    virtualOrderMessage.DueTime = virtualProduct.EndDate.Value.ToString("yyyy年MM月dd日");//到期时间
                }
                else if (!virtualProduct.ValidityType)
                {
                    virtualOrderMessage.DueTime = "长期有效";
                }
                virtualOrderMessage.EffectiveType = virtualProduct.EffectiveType;
                virtualOrderMessage.Hour = virtualProduct.Hour;
            }
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnVirtualOrderPay(orderInfo.UserId, virtualOrderMessage));
        }
        private void SendMessageOnVirtualOrderVerificationSuccess(long orderId, List<string> verificationCodes, DateTime verificationTime)
        {
            var orderInfo = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == orderId).FirstOrDefault();
            if (orderInfo == null)
            {
                return;
            }
            var virtualOrderMessage = new MessageVirtualOrderVerificationInfo();
            virtualOrderMessage.OrderId = orderInfo.Id.ToString();
            virtualOrderMessage.ShopId = orderInfo.ShopId;
            virtualOrderMessage.SiteName = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;

            var orderItemInfo = DbFactory.Default.Get<OrderItemInfo>().Where(a => a.OrderId == orderId).FirstOrDefault();
            if (orderItemInfo != null)
            {
                var productInfo = DbFactory.Default.Get<ProductInfo>().Where(a => a.Id == orderItemInfo.ProductId).FirstOrDefault();
                if (productInfo != null)
                {
                    virtualOrderMessage.ProductName = productInfo.ProductName;
                }
            }
            virtualOrderMessage.VerificationTime = verificationTime.ToString("yyyy年MM月dd日");

            if (orderInfo.ShopBranchId > 0)
            {
                var shopBranchInfo = DbFactory.Default.Get<ShopBranchInfo>().Where(a => a.Id == orderInfo.ShopBranchId).FirstOrDefault();
                if (shopBranchInfo != null)
                {
                    virtualOrderMessage.ShopBranchName = shopBranchInfo.ShopBranchName;//核销门店名称
                }
            }
            else
            {
                virtualOrderMessage.ShopBranchName = orderInfo.ShopName;//核销商家名称
            }

            if (verificationCodes != null && verificationCodes.Count > 0)
            {
                virtualOrderMessage.VerificationCodes = string.Join(",", verificationCodes);//本次核销的核销码
            }
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnVirtualOrderVerificationSuccess(orderInfo.UserId, virtualOrderMessage));
        }
        private string GenerateRandomCode(int length)
        {
            var result = new StringBuilder();
            for (var i = 0; i < length; i++)
            {
                var r = new Random(Guid.NewGuid().GetHashCode());
                result.Append(r.Next(0, 10));
            }
            return result.ToString();
        }
        #endregion

    }
}