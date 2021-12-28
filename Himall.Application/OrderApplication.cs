using AutoMapper;
using Himall.CommonModel;
using Himall.CommonModel.Delegates;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.Core.Plugins.Payment;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Himall.Application
{
    public class OrderApplication : BaseApplicaion<OrderService>
    {
        #region 字段
        private static MemberIntegralService _iMemberIntegralService = ObjectContainer.Current.Resolve<MemberIntegralService>();
        private static ProductService _ProductService = ObjectContainer.Current.Resolve<ProductService>();
        private static LimitTimeBuyService _LimitTimeBuyService = ObjectContainer.Current.Resolve<LimitTimeBuyService>();

        /// <summary>
        /// 未支付关闭
        /// </summary>
        public static void AutoCloseOrder()
        {
            var unpaidTimeout = SiteSettingApplication.SiteSettings.UnpaidTimeout;
            var expireTime = DateTime.Now.AddHours(-unpaidTimeout);
            Service.CloseExpireTime(expireTime);
        }

        public static void Settlement()
        {
            Service.Settlement();
        }

        /// <summary>
        /// 结算积分
        /// </summary>
        public static void SettlementIntegral()
        {
            var rule = _iMemberIntegralService.GetIntegralChangeRule();
            var timeout = SiteSettingApplication.SiteSettings.SalesReturnTimeout;
            var expireTime = DateTime.Now.AddDays(-timeout);
            _iMemberIntegralService.SettlementOrder(expireTime, rule.MoneyPerIntegral);
        }
        /// <summary>
        /// 自动确认订单
        /// </summary>
        public static void AutoConfirmOrder()
        {
            Service.AutoConfirmOrder();
        }

        public static void AutoConfirmGiftOrder()
        {
            ServiceProvider.Instance<GiftsOrderService>.Create.AutoConfirmOrder();
        }
        public static void SyncOrder()
        {
            Service.SyncOrder();
        }

        /// <summary>
        /// 发送未支付通知
        /// </summary>
        public static void AutoSendSMS()
        {
            var unpaidTimeout = SiteSettingApplication.SiteSettings.UnpaidTimeout;
            var notifyTime = DateTime.Now.AddHours(-unpaidTimeout).AddMinutes(10);
            var orders = Service.GetWaitSendMsgOrder(notifyTime);
            var success = new List<long>();
            foreach (var order in orders)
            {
                try
                {
                    MessageApplication.SendMsgWaitPay(order);
                    success.Add(order.Id);
                }
                catch (Exception ex) { }
                {
                }
            }
            Service.SendComplete(success);
        }
        public static void AutoComment() =>
            Service.AutoComment();
        private static RefundService _RefundService = ObjectContainer.Current.Resolve<RefundService>();

        private static ShopBranchService _iShopBranchService = ObjectContainer.Current.Resolve<ShopBranchService>();
        #endregion

        #region 属性
        /// <summary>
        /// 订单支付成功事件
        /// </summary>
        public static event OrderPaySuccessed OnOrderPaySuccessed
        {
            add
            {
                Service.OnOrderPaySuccessed += value;
            }
            remove
            {
                Service.OnOrderPaySuccessed -= value;
            }
        }
        #endregion

        #region web公共方法

        /// <summary>
        /// 根据订单ID获取订单信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static Order GetOrder(long orderId)
        {
            return Service.GetOrder(orderId).Map<DTO.Order>();
        }

        /// <summary>
        /// 根据订单ID获取订单信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static OrderInfo GetOrderInfo(long orderId)
        {
            var order = Service.GetOrder(orderId);
            if (order != null)
            {
                //统一显示支付方式名称
                order.PaymentTypeName = PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName;
                return order;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 根据提货码取订单
        /// </summary>
        /// <param name="pickCode"></param>
        /// <param name="fullOrderItems">是否填充OrderItems属性</param>
        /// <returns></returns>
        public static Order GetOrderByPickCode(string pickCode)
        {
            var order = Service.GetOrderByPickCode(pickCode);
            if (order != null)
            {
                //统一显示支付方式名称
                order.PaymentTypeName = PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName;
                return order.Map<DTO.Order>();
            }
            return null;
        }

        /// <summary>
        /// 获取商品已购数(过滤拼团、限时购购买数)
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="productIds"></param>
        /// <returns></returns>
        public static Dictionary<long, long> GetProductBuyCount(long userId, IEnumerable<long> productIds)
        {
            var fightBuyCounts = FightGroupApplication.GetMarketSaleCountForProductIdAndUserId(productIds, userId);
            var buyCounts = Service.GetProductBuyCountNotLimitBuy(userId, productIds).ToDictionary(e => e.Key, e => e.Value - (fightBuyCounts.ContainsKey(e.Key) ? fightBuyCounts[e.Key] : 0));
            return buyCounts;
        }

        /// <summary>
        /// 查询订单
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<Order> GetOrders(OrderQuery query)
        {
            var data = Service.GetOrders(query);
            var models = data.Models.Map<List<DTO.Order>>();
            foreach (var item in data.Models)
            {
                if (item.OrderStatus >= OrderInfo.OrderOperateStatus.WaitDelivery)
                {
                    Service.CalculateOrderItemRefund(item.Id);
                }
            }
            return new QueryPageModel<Order>
            {
                Models = models,
                Total = data.Total
            };
        }

        /// <summary>
        /// 根据每个店铺的购物列表获取每个店铺的满额减优惠金额
        /// </summary>
        /// <param name="cartItems"></param>
        /// <returns></returns>
        private static decimal GetShopFullDiscount(List<CartItemModel> cartItems, bool isShopBranchOrder = false)
        {
            decimal shopFullDiscount = 0;
            List<CartItemModel> fulldiscountP = new List<CartItemModel>();
            foreach (var p in cartItems)
            {
                var canJoin = true;
                if (!isShopBranchOrder)
                {
                    //限时购不参与满额减（bug需求34735）
                    var ltmbuy = GetService<LimitTimeBuyService>().GetLimitTimeMarketItemByProductId(p.id);
                    if (ltmbuy != null)
                    {
                        canJoin = false;
                    }
                }
                if (canJoin)
                    fulldiscountP.Add(p);
            }
            if (fulldiscountP.Count() <= 0)
                return shopFullDiscount;
            fulldiscountP = fulldiscountP.OrderBy(d => d.skuId).ToList();

            var productIds = fulldiscountP.Select(a => a.id).Distinct();
            var shopId = fulldiscountP.FirstOrDefault().shopId;
            var actives = FullDiscountApplication.GetOngoingActiveByProductIds(productIds, shopId);

            foreach (var active in actives)
            {
                var pids = active.Products.Select(a => a.ProductId);
                List<CartItemModel> items = fulldiscountP;
                if (!active.IsAllProduct)
                {
                    items = items.Where(a => pids.Contains(a.id)).ToList();
                }
                var realTotal = items.Sum(a => a.price * a.count);  //满额减的总金额
                var rule = active.Rules.Where(a => a.Quota <= realTotal).OrderByDescending(a => a.Quota).FirstOrDefault();
                decimal fullDiscount = 0;
                if (rule != null)//找不到就是不满足金额
                {
                    fullDiscount = rule.Discount;
                    decimal itemFullDiscount = 0;
                    for (var i = 0; i < items.Count(); i++)
                    {
                        var item = items[i];
                        if (i < items.Count() - 1)
                        {
                            item.fullDiscount = Math.Round(fullDiscount * (item.price * item.count) / realTotal, 2);
                            itemFullDiscount += item.fullDiscount;
                        }
                        else
                        {
                            item.fullDiscount = fullDiscount - itemFullDiscount;
                        }
                    }
                    shopFullDiscount += fullDiscount; //店铺总优惠金额
                }
            }
            return shopFullDiscount;
        }

        public static QueryPageModel<FullOrder> GetShopBranchOrders(OrderQuery query)
        {
            var order = Service.GetOrders(query);
            var models = order.Models.ToList().Map<List<FullOrder>>();

            var branchIds = models.Where(e => e.ShopBranchId > 0).Select(e => e.ShopBranchId).Distinct().ToList();
            var branchModels = ShopBranchApplication.GetStores(branchIds);

            foreach (var item in models)
            {
                if (item.ShopBranchId > 0)
                {//补充门店名称
                    var branch = branchModels.FirstOrDefault(e => e.Id == item.ShopBranchId);
                    if (branch != null)
                    {
                        item.ShopBranchName = branch.ShopBranchName;
                        item.SellerRemark = branch.UserName;

                    }
                }
                else
                {
                    item.ShopBranchName = item.ShopName;
                }

            }

            return new QueryPageModel<FullOrder>()
            {
                Total = order.Total,
                Models = models
            };
        }
        /// <summary>
        /// 查询订单
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<FullOrder> GetFullOrders(OrderQuery query)
        {
            var data = Service.GetOrders(query);
            if (data.Models.Count <= 0)
                return new QueryPageModel<FullOrder>()
                {
                    Models = new List<FullOrder>(),
                    Total = data.Total
                };
            var models = data.Models.Map<List<DTO.FullOrder>>();
            var orderids = models.Select(p => p.Id).ToList();
            var orderItems = GetOrderItemsByOrderId(models.Select(p => p.Id));
            //补充商品单位
            var products = ProductManagerApplication.GetAllStatusProductByIds(orderItems.Select(e => e.ProductId).Distinct());
            //补充门店名称
            var branchIds = models.Where(e => e.ShopBranchId > 0).Select(e => e.ShopBranchId).Distinct().ToList();
            var branchModels = ShopBranchApplication.GetStores(branchIds);


            var refunds = _RefundService.GetOrderRefundsByOrder(orderids);


            foreach (var order in models)
            {
                order.Refunds = refunds.Where(p => p.OrderId == order.Id).ToList();
                order.OrderItems = orderItems.Where(p => p.OrderId == order.Id).ToList();
                order.OrderProductQuantity = order.OrderItems.Sum(a => a.Quantity);
                if (order.ShopBranchId > 0)
                {//补充门店名称
                    var branch = branchModels.FirstOrDefault(e => e.Id == order.ShopBranchId);
                    if (branch != null)
                    {
                        order.ShopBranchName = branch.ShopBranchName;
                    }
                }
                else
                {
                    order.ShopBranchName = order.ShopName;
                }
                //订单售后
                var ordref = refunds.FirstOrDefault(d => d.OrderId == order.Id && d.RefundMode == OrderRefundInfo.OrderRefundMode.OrderRefund);
                if (ordref != null && order.OrderStatus < OrderInfo.OrderOperateStatus.WaitReceiving)
                {
                    order.RefundStats = ordref.RefundStatusValue;
                    order.ShowRefundStats = ordref.RefundStatus;
                    if (order.ShopBranchId > 0)
                    {
                        order.ShowRefundStats = order.ShowRefundStats.Replace("商家", "门店");
                    }
                }
                foreach (var item in order.OrderItems)
                {
                    var p = products.FirstOrDefault(e => e.Id == item.ProductId);
                    if (p != null)
                    {
                        item.Unit = p.MeasureUnit;


                        var picimg = Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_100);
                        if (item.ThumbnailsUrl.Contains("skus"))
                        {
                            picimg = HimallIO.GetRomoteImagePath(item.ThumbnailsUrl);
                        }

                        item.ThumbnailsUrl = picimg;
                    }
                    Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetTypeByProductId(item.ProductId);
                    item.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    item.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    item.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    if (p != null)
                    {
                        item.ColorAlias = !string.IsNullOrWhiteSpace(p.ColorAlias) ? p.ColorAlias : item.ColorAlias;
                        item.SizeAlias = !string.IsNullOrWhiteSpace(p.SizeAlias) ? p.SizeAlias : item.SizeAlias;
                        item.VersionAlias = !string.IsNullOrWhiteSpace(p.VersionAlias) ? p.VersionAlias : item.VersionAlias;
                    }

                    //订单项售后
                    var orditemref = refunds.FirstOrDefault(d => d.OrderId == order.Id && d.OrderItemId == item.Id && d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                    if (orditemref != null)
                    {
                        item.Refund = orditemref;
                        item.RefundStats = orditemref.RefundStatusValue;
                        item.ShowRefundStats = orditemref.RefundStatus;
                        if (order.ShopBranchId > 0)
                        {
                            item.ShowRefundStats = item.ShowRefundStats.Replace("商家", "门店");
                        }
                    }
                }
            }
            return new QueryPageModel<FullOrder>
            {
                Models = models,
                Total = data.Total
            };
        }

        /// <summary>
        /// 获取订单列表(忽略分页)
        /// </summary>
        /// <param name="orderQuery"></param>
        /// <param name="fullOrderItems">是否填充OrderItems属性</param>
        /// <returns></returns>
        public static List<FullOrder> GetAllFullOrders(OrderQuery orderQuery)
        {
            var data = Service.GetAllOrders(orderQuery);
            var list = data.Select(item => new FullOrder
            {
                ActualPayAmount = item.ActualPayAmount,
                Address = item.Address,
                CapitalAmount = item.CapitalAmount,
                CellPhone = item.CellPhone,
                CloseReason = item.CloseReason,
                CommisTotalAmount = item.CommisTotalAmount,
                DadaStatus = item.DadaStatus,
                DeliveryType = item.DeliveryType,
                DiscountAmount = item.DiscountAmount,
                ExpressCompanyName = item.ExpressCompanyName,
                FightGroupCanRefund = item.FightGroupCanRefund,
                FightGroupOrderJoinStatus = item.FightGroupOrderJoinStatus,
                FinishDate = item.FinishDate,
                Freight = item.Freight,
                FullDiscount = item.FullDiscount,
                GatewayOrderId = item.GatewayOrderId,
                Id = item.Id,
                IntegralDiscount = item.IntegralDiscount,
                LastModifyTime = item.LastModifyTime,
                OrderAmount = item.OrderAmount,
                OrderDate = item.OrderDate,
                OrderEnabledRefundAmount = item.OrderEnabledRefundAmount,
                OrderRemarks = item.OrderRemarks,
                OrderStatus = item.OrderStatus,
                OrderTotalAmount = item.OrderTotalAmount,
                OrderType = item.OrderType,
                PayDate = item.PayDate,
                PaymentType = item.PaymentType,
                PaymentTypeGateway = item.PaymentTypeGateway,
                PaymentTypeName = item.PaymentTypeName,
                PayRemark = item.PayRemark,
                PickupCode = item.PickupCode,
                Platform = item.Platform,
                ProductTotal = item.ProductTotal,
                ProductTotalAmount = item.ProductTotalAmount,
                ReceiveLatitude = item.ReceiveLatitude,
                ReceiveLongitude = item.ReceiveLongitude,
                RefundCommisAmount = item.RefundCommisAmount,
                RefundTotalAmount = item.RefundTotalAmount,
                RegionId = item.RegionId,
                RegionFullName = item.RegionFullName,
                SellerAddress = item.SellerAddress,
                SellerPhone = item.SellerPhone,
                SellerRemark = item.SellerRemark,
                SellerRemarkFlag = item.SellerRemarkFlag,
                ShipOrderNumber = item.ShipOrderNumber,
                ShippingDate = item.ShippingDate,
                ShipTo = item.ShipTo,
                ShopBranchId = item.ShopBranchId,
                ShopId = item.ShopId,
                ShopName = item.ShopName,
                Tax = item.Tax,
                TopRegionId = item.TopRegionId,
                TotalAmount = item.TotalAmount,
                UserId = item.UserId,
                UserName = item.UserName,
                UserRemark = item.UserRemark,
                OrderItems = new List<OrderItem>(),
            }).ToList();

            IEnumerable<long> orderIds = list.Select(p => p.Id);//补充门店名称

            var branchIds = list.Where(e => e.ShopBranchId > 0).Select(e => e.ShopBranchId).Distinct().ToList();
            var branchModels = ShopBranchApplication.GetStores(branchIds);

            var orderItems = GetOrderItemsByOrderId(orderIds);
            var psoItems = Service.GetPendingSettlementOrdersByOrderId(orderIds);//待结算
            if (psoItems == null)
            {
                psoItems = new List<PendingSettlementOrderInfo>();
            }
            var finishPsoItems = Service.GetAccountDetailByOrderId(orderIds);//已结算订单
            if (finishPsoItems == null)
            {
                finishPsoItems = new List<AccountDetailInfo>();
            }

            var virItems = Service.GeVirtualOrderItemsByOrderId(orderIds);//虚拟订单
            if (virItems == null)
            {
                virItems = new List<VirtualOrderItemInfo>();
            }

            var invoiceItems = Service.GetOrderInvoicesByOrderId(orderIds);//发票订单
            if (invoiceItems == null)
            {
                invoiceItems = new List<OrderInvoiceInfo>();
            }

            List<Entities.ProductInfo> productList = new List<ProductInfo>();
            var nullsku = orderItems.Where(t => string.IsNullOrEmpty(t.SKU));
            if (nullsku != null)
            {
                productList = ProductManagerApplication.GetAllProductByIds(nullsku.Select(t => t.ProductId));
            }

            list.ForEach(order =>
            {
                order.OrderItems = orderItems.Where(p => p.OrderId == order.Id).ToList();//子订单
                foreach (var item in order.OrderItems)
                {
                    item.VirtualOrderItem = virItems.Where(p => p.OrderItemId == item.Id).ToList();//订单里虚拟商品信息

                    #region 只没有货号商品读取商品编号
                    if (string.IsNullOrEmpty(item.SKU))
                    {
                        var firstpro = productList.Where(t => t.Id == item.ProductId).FirstOrDefault();
                        if (firstpro != null)
                            item.ProductCode = firstpro.ProductCode;
                    }
                    #endregion
                }

                order.OrderProductQuantity = order.OrderItems.Sum(p => p.Quantity);
                order.OrderReturnQuantity = order.OrderItems.Sum(p => p.ReturnQuantity);
                order.OrderInvoice = invoiceItems.Where(p => p.OrderId == order.Id).FirstOrDefault();//发票订单

                #region 平台佣金和分销员佣金
                var noSettlement = psoItems.Where(p => p.OrderId == order.Id).FirstOrDefault();//待结算订单
                if (noSettlement == null)
                {
                    //已结算的订单它会删除待结算订单表记录，则待结算没数据是读取已结算订单数据
                    var yesSettlement = finishPsoItems.Where(p => p.OrderId == order.Id).FirstOrDefault();//已结算订单
                    if (yesSettlement != null)
                    {
                        order.PlatCommission = yesSettlement.CommissionAmount;//平台佣金
                        order.DistributorCommission = yesSettlement.BrokerageAmount;//分销员佣金
                    }
                }
                else
                {
                    order.PlatCommission = noSettlement.PlatCommission;//平台佣金
                    order.DistributorCommission = noSettlement.DistributorCommission;//分销员佣金
                }
                #endregion

                #region //补充门店名称
                if (order.ShopBranchId > 0)
                {
                    var branch = branchModels.FirstOrDefault(e => e.Id == order.ShopBranchId);
                    if (branch != null)
                    {
                        order.ShopBranchName = branch.ShopBranchName;
                    }
                }
                else
                {
                    order.ShopBranchName = order.ShopName;
                }
                #endregion
            });
            return list;
        }

        /// <summary>
        /// 分页查询平台会员购买记录
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<Order> GetUserBuyRecord(long userId, OrderQuery query)
        {
            query.UserId = userId;
            query.IsBuyRecord = true;
            var order = Service.GetOrders<OrderInfo>(query);
            var models = order.Models.ToList().Map<List<Order>>();

            return new QueryPageModel<Order>()
            {
                Total = order.Total,
                Models = models
            };
        }

        /// <summary>
        /// 根据订单id获取订单
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<Order> GetOrders(IEnumerable<long> ids)
        {
            var orderInfoList = Service.GetOrders(ids);
            return orderInfoList.Map<List<Order>>();
        }

        /// <summary>
        /// 判断用户是否有支付密码
        /// </summary>
        /// <param name="userid">用户标识</param>
        /// <returns>是否</returns>
        public static bool GetPayPwd(long userid)
        {
            string paypwd = MemberApplication.GetMember(userid).PayPwd;
            if (string.IsNullOrWhiteSpace(paypwd))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 根据用户ID获取用户收获地址列表
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <returns>收获地址列表</returns>
        public static List<ShipAddressInfo> GetUserShippingAddresses(long userid)
        {
            var addresses = ShippingAddressApplication.GetUserShippingAddressByUserId(userid).ToArray();
            List<ShipAddressInfo> result = new List<ShipAddressInfo>();
            foreach (var item in addresses)
            {
                ShipAddressInfo addr = new ShipAddressInfo();
                addr.id = item.Id;
                addr.fullRegionName = item.RegionFullName;
                addr.address = item.Address;
                addr.addressDetail = item.AddressDetail;
                addr.phone = item.Phone;
                addr.shipTo = item.ShipTo;
                addr.fullRegionIdPath = item.RegionIdPath;
                addr.regionId = item.RegionId;
                addr.latitude = item.Latitude;
                addr.longitude = item.Longitude;
                addr.NeedUpdate = item.NeedUpdate;
                result.Add(addr);
            }
            return result;
        }

        /// <summary>
        /// 确认订单(零元订单或积分支付订单)
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <param name="orderIds">订单ID集合</param>
        public static void ConfirmOrder(long userid, string orderIds)
        {
            var orderIdArr = orderIds.Split(',').Select(item => long.Parse(item));
            Service.ConfirmZeroOrder(orderIdArr, userid);
        }

        /// <summary>
        /// 保存发票抬头
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <param name="name">抬头名称</param>
        /// <returns>返回发票抬头ID</returns>
        public static long SaveInvoiceTitle(long userid, string name, string code, long id = 0)
        {
            InvoiceTitleInfo info = new InvoiceTitleInfo
            {
                Name = name,
                Code = code,
                UserId = userid,
                IsDefault = 0,
                InvoiceType = InvoiceType.OrdinaryInvoices
            };
            long result = -1;
            if (string.IsNullOrWhiteSpace(info.Name) || string.IsNullOrWhiteSpace(info.Code)) return result;
            if (id > 0)
            {
                info.Id = id;
                result = Service.EditInvoiceTitle(info);
            }
            else
            {
                result = Service.SaveInvoiceTitle(info);
            }
            return result;
        }

        /// <summary>
        /// 保存发票信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static void SaveInvoiceTitleNew(InvoiceTitleInfo info)
        {
            if (info.InvoiceType == InvoiceType.ElectronicInvoice)
            {
                if (string.IsNullOrEmpty(info.CellPhone))
                {
                    throw new HimallException("收票人手机号不能为空");
                }
                if (!Core.Helper.ValidateHelper.IsMobile(info.CellPhone))
                    throw new HimallException("收票人手机号格式不正确");

                if (string.IsNullOrEmpty(info.Email))
                    throw new HimallException("收票人邮箱不能为空");

                if (!Core.Helper.ValidateHelper.IsEmail(info.Email))
                    throw new HimallException("收票人邮箱格式不正确");
            }
            else if (info.InvoiceType == InvoiceType.VATInvoice)
            {
                if (string.IsNullOrEmpty(info.Name))
                {
                    throw new HimallException("单位名称不能为空");
                }
                if (string.IsNullOrEmpty(info.Code))
                {
                    throw new HimallException("纳税人识别号不能为空");
                }
                if (string.IsNullOrEmpty(info.RegisterAddress))
                {
                    throw new HimallException("注册地址不能为空");
                }
                if (string.IsNullOrEmpty(info.RegisterPhone))
                {
                    throw new HimallException("收票人手机号不能为空");
                }
                if (string.IsNullOrEmpty(info.CellPhone))
                {
                    throw new HimallException("注册电话不能为空");
                }
                if (string.IsNullOrEmpty(info.BankName))
                {
                    throw new HimallException("开户银行不能为空");
                }
                if (string.IsNullOrEmpty(info.BankNo))
                {
                    throw new HimallException("银行账户不能为空");
                }
                if (string.IsNullOrEmpty(info.RealName))
                {
                    throw new HimallException("收票人姓名不能为空");
                }
                if (string.IsNullOrEmpty(info.CellPhone))
                {
                    throw new HimallException("收票人手机号不能为空");
                }
                if (!Core.Helper.ValidateHelper.IsMobile(info.CellPhone))
                    throw new HimallException("收票人手机号格式不正确");
                if (info.RegionID <= 0)
                {
                    throw new HimallException("请选择收票人地区");
                }
                if (string.IsNullOrEmpty(info.Address))
                {
                    throw new HimallException("收票人详细地址不能为空");
                }
            }
            info.IsDefault = 1;
            Service.SaveInvoiceTitleNew(info);
        }

        /// <summary>
        /// 删除发票抬头
        /// </summary>
        /// <param name="id">发票抬头标识</param>
        public static void DeleteInvoiceTitle(long id, long userId = 0)
        {
            Service.DeleteInvoiceTitle(id, userId);
        }
      
        /// <summary>
        /// 获取运费
        /// </summary>
        /// <param name="addressId"></param>
        /// <param name="counts">门店，商品id和数量的集合</param>
        /// <returns></returns>
        public static Dictionary<long, decimal> CalcFreight(int addressId, Dictionary<long, Dictionary<long, string>> counts)
        {
            var result = new Dictionary<long, decimal>();

            foreach (var shopId in counts.Keys)
            {
                List<long> excludeIds = new List<long>();//排除掉包邮的商品

                var productInfos = ProductManagerApplication.GetProductsByIds(counts[shopId].Keys);//商家下所有的商品集合
                if (productInfos != null && productInfos.Count > 0)
                {
                    var templateIds = productInfos.Select(a => a.FreightTemplateId).ToList();
                    if (templateIds != null && templateIds.Count > 0)
                    {
                        templateIds.ForEach(tid =>
                        {
                            var ids = productInfos.Where(a => a.FreightTemplateId == tid).Select(b => b.Id).ToList();//属于当前模板的商品ID集合
                            bool isFree = false;
                            var freeRegions = ServiceProvider.Instance<FreightTemplateService>.Create.GetShippingFreeRegions(tid);
                            freeRegions.ForEach(c =>
                            {
                                c.RegionSubList = ServiceProvider.Instance<RegionService>.Create.GetSubsNew(c.RegionId, true).Select(a => a.Id).ToList();
                            });
                            var regions = freeRegions.Where(d => d.RegionSubList.Contains(addressId)).ToList();//根据模板设置的包邮地区过滤出当前配送地址所在地址
                            if (regions != null && regions.Count > 0)
                            {
                                var groupIds = regions.Select(e => e.GroupId).ToList();
                                var freeGroups = ServiceProvider.Instance<FreightTemplateService>.Create.GetShippingFreeGroupInfos(tid, groupIds);

                                //只要有一个符合包邮条件，则退出
                                long count = counts[shopId].Where(p => ids.Contains(p.Key)).Sum(a => int.Parse(a.Value.Split('$')[0]));
                                decimal amount = counts[shopId].Where(p => ids.Contains(p.Key)).Sum(a => decimal.Parse(a.Value.Split('$')[1]));
                                freeGroups.ForEach(f =>
                                {
                                    if (f.ConditionType == 1)//购买件数
                                    {
                                        if (count >= int.Parse(f.ConditionNumber))
                                        {
                                            isFree = true;
                                            return;
                                        }
                                    }
                                    else if (f.ConditionType == 2)//金额
                                    {
                                        if (amount >= decimal.Parse(f.ConditionNumber))
                                        {
                                            isFree = true;
                                            return;
                                        }
                                    }
                                    else if (f.ConditionType == 3)//件数+金额
                                    {
                                        var condition1 = int.Parse(f.ConditionNumber.Split('$')[0]);
                                        var condition2 = decimal.Parse(f.ConditionNumber.Split('$')[1]);
                                        if (count >= condition1 && amount >= condition2)
                                        {
                                            isFree = true;
                                            return;
                                        }
                                    }
                                });
                            }
                            if (isFree)
                            {
                                excludeIds.AddRange(ids);
                            }

                        });
                    }
                }
                //要排除掉指定地区包邮的商品ID
                IEnumerable<long> pIds = counts[shopId].Where(a => !excludeIds.Contains(a.Key)).Select(b => b.Key);
                IEnumerable<int> pCounts = counts[shopId].Where(a => !excludeIds.Contains(a.Key)).Select(b => int.Parse(b.Value.Split('$')[0]));
                decimal freight = 0;
                if (pIds != null && pIds.Count() > 0 && pCounts != null && pCounts.Count() > 0)
                {
                    freight = _ProductService.GetFreight(pIds, pCounts, addressId);
                }
                result.Add(shopId, freight);
            }

            return result;
        }

        /// <summary>
        /// 预付款支付
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <param name="orderIds">订单ID集合</param>
        /// <param name="pwd">密码</param>
        /// <param name="hostUrl">网站地址</param>
        /// <returns>支付是否成功</returns>
        public static bool PayByCapital(long userid, string orderIds, string pwd, string hostUrl)
        {
            if (string.IsNullOrWhiteSpace(orderIds))
            {
                throw new HimallException("错误的订单编号");
            }
            var success = MemberApplication.VerificationPayPwd(userid, pwd);
            if (!success)
            {
                throw new HimallException("支付密码不对");
            }
            IEnumerable<long> ids = orderIds.Split(',').Select(e => long.Parse(e));
            //获取待支付的所有订单
            var orders = Service.GetOrders(ids).Where(item => item.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay && item.UserId == userid).ToList();

            if (orders == null || orders.Count() == 0) //订单状态不正确
            {
                throw new HimallException("错误的订单编号");
            }
            /* 积分支付的订单金额，可能为0
            decimal total = orders.Sum(a => a.OrderTotalAmount);
            if (total == 0)
            {
                throw new HimallException("错误的订单总价");
            }*/

            foreach (var item in orders)
            {
                if (item.OrderType == OrderInfo.OrderTypes.FightGroup)
                {
                    if (!FightGroupApplication.OrderCanPay(item.Id))
                    {
                        throw new HimallException("有拼团订单为不可付款状态");
                    }
                }
            }

            #region 支付流水获取
            var orderPayModel = orders.Select(item => new OrderPayInfo
            {
                PayId = 0,
                OrderId = item.Id
            });
            //保存支付订单
            long payid = Service.SaveOrderPayInfo(orderPayModel, PlatformType.PC);
            #endregion

            WDTConfigModel wDTConfig = WDTOrderApplication.GetConfigModel();
            Service.PayCapital(ids, wDTConfig, payId: payid);

            //限时购
            IncreaseSaleCount(ids.ToList());
            //红包
            GenerateBonus(ids, hostUrl);
            return true;
        }
        public static bool PayByCapitalIsOk(long userid, string orderIds)
        {
            IEnumerable<long> ids = orderIds.Split(',').Select(e => long.Parse(e));
            return Service.PayByCapitalIsOk(userid, ids);
        }
        /// <summary>
        /// 获取支付页面数据
        /// </summary>
        /// <param name="userid">用户ID</param>
        /// <param name="orderIds">订单ID集合</param>
        /// <param name="webRoot">站点地址</param>
        /// <returns>数据</returns>
        public static PaymentViewModel GetPay(long userid, string orderIds, string webRoot)
        {
            PaymentViewModel result = new PaymentViewModel();
            result.IsSuccess = true;
            if (string.IsNullOrEmpty(orderIds))
            {
                result.IsSuccess = false;
                result.Msg = "订单号错误，不能进行支付。";
                return result;
            }
            var orderIdArr = orderIds.Split(',').Select(item => long.Parse(item));
            var orders = Service.GetOrders(orderIdArr).Where(p => p.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay && p.UserId == userid).ToList();
            if (orders.Count <= 0)//订单已经支付，则跳转至订单页面
            {

                var errorOrder = Service.GetOrders(orderIdArr).Where(p => p.OrderStatus == OrderInfo.OrderOperateStatus.Close && p.UserId == userid).Count();
                result.IsSuccess = false;
                if (errorOrder > 0)
                    result.Msg = "订单已关闭，不能进行支付。";
                else
                    result.Msg = "没有钱要付";
                return result;
            }
            else
            {

                //获取待支付的所有订单
                var orderser = Service;

                foreach (var item in orders)
                {
                    if (item.OrderType == OrderInfo.OrderTypes.FightGroup)
                    {
                        if (!FightGroupApplication.OrderCanPay(item.Id))
                        {
                            throw new HimallException("有拼团订单为不可付款状态");
                        }
                    }
                }

                #region 数据补偿
                //是否有已删商品
                bool isHaveNoSaleProOrd = false;   //是否有非销售中的商品
                List<OrderInfo> delOrders = new List<OrderInfo>();
                foreach (var order in orders)
                {
                    if (order.OrderStatus == OrderInfo.OrderOperateStatus.Close)
                    {
                        delOrders.Add(order);
                        isHaveNoSaleProOrd = true;
                    }
                }
                if (isHaveNoSaleProOrd)
                {
                    foreach (var _item in delOrders)
                    {
                        orders.Remove(_item);  //执行清理
                    }
                    throw new HimallException("有订单商品处于非销售状态，请手动处理。");
                }
                result.HaveNoSalePro = isHaveNoSaleProOrd;
                #endregion

                if (orders == null || orders.Count == 0) //订单状态不正确
                {
                    result.IsSuccess = false;
                    result.Msg = "系统错误，您可以到 “我的订单” 查看付款操作是否成功。";
                }

                result.Orders = orders;

                decimal total = orders.Sum(a => a.OrderTotalAmount - a.CapitalAmount);

                result.TotalAmount = total;

                //获取所有订单中的商品名称
                //var productInfos = GetProductNameDescriptionFromOrders(orders);

                //获取同步返回地址
                string returnUrl = webRoot + "/Pay/Return/{0}";

                //获取异步通知地址
                string payNotify = webRoot + "/Pay/Notify/{0}";

                var payments = Core.PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(item => item.Biz.SupportPlatforms.Contains(PlatformType.PC));

                const string RELATEIVE_PATH = "/Plugins/Payment/";

                var models = payments.Select(item =>
                {
                    string requestUrl = string.Empty;

                    return new PaymentModel()
                    {
                        Logo = RELATEIVE_PATH + item.PluginInfo.ClassFullName.Split(',')[1] + "/" + item.Biz.Logo,
                        RequestUrl = requestUrl,
                        UrlType = item.Biz.RequestUrlType,
                        Id = item.PluginInfo.PluginId
                    };
                });
                result.Models = models.OrderByDescending(d => d.Id);
                //models = models.Where( item => !string.IsNullOrEmpty( item.RequestUrl ) );//只选择正常加载的插件
                //TODO:【2015-08-31】支付页面增加预付款
                //var capital = MemberCapitalApplication.GetCapitalInfo(userid);
                //if (capital == null)
                //{
                //    result.Capital = 0;
                //}
                //else
                //{
                //    result.Capital = capital.Balance != null ? capital.Balance.Value : 0;
                //}
                return result;
            }
        }

        /// <summary>
        /// 获取支付相关信息
        /// </summary>
        /// <param name="userid">用户id</param>
        /// <param name="orderIds">订单id</param>
        /// <param name="webRoot">网站根目录</param>
        /// <returns>支付相信息</returns>
        public static ChargePayModel ChargePay(long userid, string orderIds, string webRoot)
        {

            ChargePayModel viewmodel = new ChargePayModel();
            var model = MemberCapitalApplication.GetChargeDetail(long.Parse(orderIds));
            if (model == null || model.MemId != userid || model.ChargeStatus == Himall.Entities.ChargeDetailInfo.ChargeDetailStatus.ChargeSuccess)//订单已经支付，则跳转至订单页面
            {
                Log.Error("调用ChargePay方法时未找到充值申请记录：" + orderIds);
                //return RedirectToAction("index", "userCenter", new { url = "/UserCapital", tar = "UserCapital" });
                return null;
            }
            else
            {

                //ViewBag.Orders = model;
                viewmodel.Orders = model;

                //获取同步返回地址
                string returnUrl = webRoot + "/Pay/CapitalChargeReturn/{0}";

                //获取异步通知地址
                string payNotify = webRoot + "/Pay/CapitalChargeNotify/{0}/";

                var payments = Core.PluginsManagement.GetPlugins<IPaymentPlugin>(true).Where(item => item.Biz.SupportPlatforms.Contains(PlatformType.PC));

                const string RELATEIVE_PATH = "/Plugins/Payment/";

                var models = payments.Select(item =>
                {
                    string requestUrl = string.Empty;
                    try
                    {
                        requestUrl = item.Biz.GetRequestUrl(string.Format(returnUrl, EncodePaymentId(item.PluginInfo.PluginId)), string.Format(payNotify, EncodePaymentId(item.PluginInfo.PluginId)), orderIds, model.ChargeAmount, "预存款充值");
                    }
                    catch (Exception ex)
                    {
                        Core.Log.Error("支付页面加载支付插件出错", ex);
                    }
                    return new PaymentModel()
                    {
                        Logo = RELATEIVE_PATH + item.PluginInfo.ClassFullName.Split(',')[1] + "/" + item.Biz.Logo,
                        RequestUrl = requestUrl,
                        UrlType = item.Biz.RequestUrlType,
                        Id = item.PluginInfo.PluginId
                    };
                });
                models = models.Where(item => !string.IsNullOrEmpty(item.RequestUrl));//只选择正常加载的插件
                viewmodel.OrderIds = orderIds;
                viewmodel.TotalAmount = model.ChargeAmount;
                viewmodel.Step = 1;
                viewmodel.UnpaidTimeout = SiteSettingApplication.SiteSettings.UnpaidTimeout;
                viewmodel.models = models.OrderByDescending(d => d.Id).ToList();
                //return View(viewmodel);
                return viewmodel;
            }
        }

        #endregion

        #region mobile公共方法
    
        /// <summary>
        /// 使用积分支付的订单取消
        /// </summary>
        /// <param name="orderIds">订单id</param>
        /// <param name="userid">用户id</param>
        public static void CancelOrder(string orderIds, long userid)
        {
            var orderIdArr = orderIds.Split(',').Select(item => long.Parse(item));
            Service.CancelOrders(orderIdArr, userid);

        }

        /// <summary>
        /// 是否全部抵扣
        /// </summary>
        /// <param name="integral">积分</param>
        /// <param name="total">总共需要积分</param>
        /// <param name="userid">用户标识</param>
        /// <returns>抵扣是否成功</returns>
        public static bool IsAllDeductible(int integral, decimal total, long userid)
        {
            if (integral == 0) //没使用积分时的0元订单
                return false;
            var result = Service.GetIntegralDiscountAmount(integral, userid);
            if (result < total)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 根据用户获收获地址列表
        /// </summary>
        /// <param name="userid">用户id</param>
        /// <returns>收获地址列表</returns>
        public static List<Entities.ShippingAddressInfo> GetUserAddresses(long userid, long shopBranchId = 0)
        {
            var addresss = ShippingAddressApplication.GetUserShippingAddressByUserId(userid).ToList();
            if (shopBranchId > 0)
            {
                var shopBranchInfo = _iShopBranchService.GetShopBranchData(shopBranchId);
                if (shopBranchInfo == null)
                    return addresss;
                foreach (var item in addresss)
                {
                    if (item.NeedUpdate) continue;
                    string form = string.Format("{0},{1}", item.Latitude, item.Longitude);//收货地址的经纬度
                    if (form.Length <= 1)
                        continue;//地址不含经纬度的不可配送
                    int Distances = RegionApplication.GetDistance(shopBranchInfo.Latitude, shopBranchInfo.Longitude, item.Latitude, item.Longitude);

                    if (Distances > shopBranchInfo.ServeRadius * 1000)
                        continue;//距离超过配送距离的不可配送,距离计算失败不可配送
                    item.CanDelive = true;
                }
            }
            return addresss;
        }

        /// <summary>
        /// 设置用户默认收货地址
        /// </summary>
        /// <param name="addrId">地址Id</param>
        /// <param name="userid">用户Id</param>
        public static void SetDefaultUserShippingAddress(long addrId, long userid)
        {
            ShippingAddressApplication.SetDefaultShippingAddress(addrId, userid);
        }

        /// <summary>
        /// 获取指定收获地址的信息
        /// </summary>
        /// <param name="addressId">收获地址Id</param>
        /// <returns>收获地址信息</returns>
        public static Entities.ShippingAddressInfo GetUserAddress(long addressId)
        {
            var ShipngInfo = new Entities.ShippingAddressInfo();
            if (addressId != 0)
            {
                ShipngInfo = ShippingAddressApplication.GetUserShippingAddress(addressId);
            }
            return ShipngInfo;
        }

        /// <summary>
        /// 删除指定的收获地址信息
        /// </summary>
        /// <param name="addressId">收获地址Id</param>
        public static void DeleteShippingAddress(long addressId, long userid)
        {
            ShippingAddressApplication.DeleteShippingAddress(addressId, userid);
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="orderId">订单Id</param>
        /// <param name="userid">用户Id</param>
        /// <param name="username">用户名</param>
        /// <returns>是否成功</returns>
        public static bool CloseOrder(long orderId, long userid, string username)
        {
            var order = Service.GetOrder(orderId, userid);
            if (order != null)
            {
                Service.MemberCloseOrder(orderId, username);
            }
            else
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 确认订单收货
        /// </summary>
        public static int ConfirmOrder(long orderId, long userId, string username)
        {
            var order = Service.GetOrder(orderId, userId);
            if (order.OrderStatus == OrderInfo.OrderOperateStatus.Finish)
            {
                return 1;
                //throw new HimallException("该订单已经确认过!");
            }
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitReceiving && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
            {
                return 2;
                //throw new HimallException("订单状态发生改变，请重新刷页面操作!");
            }
            Service.MembeConfirmOrder(orderId, username);
            if (order.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
            {//货到付款的订单，在会员确认收货时
                MemberApplication.UpdateNetAmount(order.UserId, order.OrderTotalAmount);
                MemberApplication.IncreaseMemberOrderNumber(order.UserId);
            }
            return 0;
        }
        /// <summary>
        /// 门店核销订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="managerName"></param>
        public static void ShopBranchConfirmOrder(long orderId, long shopBranchId, string managerName)
        {
            Service.ShopBranchConfirmOrder(orderId, shopBranchId, managerName);
        }
        /// <summary>
        /// 获取订单详细信息
        /// </summary>
        /// <param name="id">订单Id</param>
        /// <param name="userid">用户Id</param>
        /// <param name="type">平台类型</param>
        /// <param name="host">网站host地址</param>
        /// <returns>订单详细信息</returns>
        public static OrderDetailView Detail(long id, long userid, PlatformType type, string host)
        {
            OrderInfo order = Service.GetOrder(id, userid);
            var shopinfo = ShopApplication.GetShopInfo(order.ShopId);
            var vshop = VshopApplication.GetVShopByShopId(shopinfo.Id)?.Id ?? 0;
            bool IsRefundTimeOut = false;
            var _ordrefobj = RefundApplication.GetOrderRefundByOrderId(id) ?? new OrderRefundInfo { Id = 0 };
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
            {
                _ordrefobj = new OrderRefundInfo { Id = 0 };
            }
            int? ordrefstate = (_ordrefobj == null ? null : (int?)_ordrefobj.SellerAuditStatus);
            ordrefstate = (ordrefstate > 4 ? (int?)_ordrefobj.ManagerConfirmStatus : ordrefstate);
            var orderItems = Service.GetOrderItemsByOrderId(id);
            var refunds = RefundApplication.GetOrderRefundsByOrder(id);
            var products = _ProductService.GetProducts(orderItems.Select(p => p.ProductId).ToList());
            //获取订单商品项数据
            var orderDetail = new OrderDetail()
            {
                ShopName = shopinfo.ShopName,
                ShopId = order.ShopId,
                VShopId = vshop,
                RefundStats = ordrefstate,
                OrderRefundId = _ordrefobj.Id,
                OrderItems = orderItems.Select(item =>
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    var refund = refunds.FirstOrDefault(p => p.OrderItemId == item.Id && p.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                    int? refundState = (refund == null ? null : (int?)refund.SellerAuditStatus);
                    refundState = (refundState > 4 ? (int?)refund.ManagerConfirmStatus : refundState);
                    string picimg = product.GetImage(ImageSize.Size_100);
                    if (item.ThumbnailsUrl.Contains("skus"))
                    {
                        picimg = HimallIO.GetRomoteImagePath(item.ThumbnailsUrl);
                    }
                    return new OrderItem
                    {
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Count = item.Quantity,
                        Price = item.SalePrice,
                        ProductImage = picimg,
                        Id = item.Id,
                        Unit = product.MeasureUnit,
                        IsCanRefund = CanRefund(order, itemId: item.Id),
                        Color = item.Color,
                        Size = item.Size,
                        Version = item.Version,
                        RefundStats = refundState,
                        OrderRefundId = (refund == null ? 0 : refund.Id),
                        EnabledRefundAmount = item.EnabledRefundAmount
                    };
                })
            };
            OrderDetailView view = new OrderDetailView();
            IsRefundTimeOut = Service.IsRefundTimeOut(id);
            view.Detail = orderDetail;
            view.Bonus = null;
            if (type == Core.PlatformType.WeiXin)
            {
                var bonusmodel = ShopBonusApplication.GetGrantByUserOrder(id, userid);
                if (bonusmodel != null)
                {
                    view.Bonus = bonusmodel;
                    view.ShareHref = Core.Helper.WebHelper.GetScheme() + "://" + host + "/m-weixin/shopbonus/index/" + ShopBonusApplication.GetGrantIdByOrderId(id);
                }
            }
            view.Order = order;

            view.FightGroupCanRefund = true;
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)  //拼团状态补充
            {
                var fgord = FightGroupApplication.GetFightGroupOrderStatusByOrderId(order.Id);
                view.FightGroupJoinStatus = CommonModel.FightGroupOrderJoinStatus.JoinFailed;
                if (fgord != null)
                {
                    view.FightGroupJoinStatus = fgord.GetJoinStatus;
                    view.FightGroupCanRefund = fgord.CanRefund;
                }
            }

            view.IsRefundTimeOut = IsRefundTimeOut;
            return view;
        }

        /// <summary>
        /// 是否超过售后期
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static bool IsRefundTimeOut(DTO.Order order)
        {
            return Service.IsRefundTimeOut(order.Map<OrderInfo>());
        }

        /// <summary>
        /// 获取快递信息
        /// </summary>
        /// <param name="orderId">订单Id</param>
        /// <param name="userid">用户Id</param>
        /// <returns>快递信息 [0]:快递公司 [1]:单号</returns>
        public static string[] GetExpressInfo(long orderId)
        {
            OrderInfo order = Service.GetOrder(orderId);
            string[] result = new string[2];
            if (order != null)
            {
                result[0] = order.ExpressCompanyName;
                result[1] = order.ShipOrderNumber;
            }
            return result;
        }
        #endregion
       
        /// <summary>
        /// 根据SKUID获取SKU
        /// </summary>
        /// <param name="skuid"></param>
        /// <returns></returns>
        public static SKUInfo GetSkuByID(string skuid)
        {
            return Service.GetSkuByID(skuid);
        }

        #region mobile私有方法
       
        private static void FreeShipping(int cityId, IGrouping<long, CartItemModel> shopcartItem, out IEnumerable<long> pIds, out IEnumerable<int> pCounts)
        {
            List<long> excludeIds = new List<long>();//排除掉包邮的商品
            var templateIds = shopcartItem.Select(p => p.FreightTemplateId).Distinct().ToList();//当前商家下所有商品模板ID集合
            templateIds.ForEach(p =>
            {
                var ids = shopcartItem.Where(a => a.FreightTemplateId == p).Select(a => a.id).ToList();//属于当前模板的商品ID集合
                bool isFree = false;
                var freeRegions = ServiceProvider.Instance<FreightTemplateService>.Create.GetShippingFreeRegions(p);
                freeRegions.ForEach(c =>
                {
                    c.RegionSubList = ServiceProvider.Instance<RegionService>.Create.GetSubsNew(c.RegionId, true).Select(a => a.Id).ToList();
                });
                var regions = freeRegions.Where(d => d.RegionSubList.Contains(cityId)).ToList();//根据模板设置的包邮地区过滤出当前配送地址所在地址
                if (regions != null && regions.Count > 0)
                {
                    var groupIds = regions.Select(e => e.GroupId).ToList();
                    var freeGroups = ServiceProvider.Instance<FreightTemplateService>.Create.GetShippingFreeGroupInfos(p, groupIds);

                    //只要有一个符合包邮条件，则退出
                    long count = shopcartItem.Where(a => ids.Contains(a.id)).Sum(b => b.count);//总数量
                    decimal amount = shopcartItem.Where(a => ids.Contains(a.id)).Sum(b => b.price * b.count);//总金额
                    freeGroups.ForEach(f =>
                    {
                        if (f.ConditionType == 1)//购买件数
                        {
                            if (count >= int.Parse(f.ConditionNumber))
                            {
                                isFree = true;
                                return;
                            }
                        }
                        else if (f.ConditionType == 2)//金额
                        {
                            if (amount >= decimal.Parse(f.ConditionNumber))
                            {
                                isFree = true;
                                return;
                            }
                        }
                        else if (f.ConditionType == 3)//件数+金额
                        {
                            var condition1 = int.Parse(f.ConditionNumber.Split('$')[0]);
                            var condition2 = decimal.Parse(f.ConditionNumber.Split('$')[1]);
                            if (count >= condition1 && amount >= condition2)
                            {
                                isFree = true;
                                return;
                            }
                        }
                    });
                }
                if (isFree)
                {
                    excludeIds.AddRange(ids);
                }
            });
            pIds = shopcartItem.Where(a => !excludeIds.Contains(a.id)).Select(b => b.id);
            pCounts = shopcartItem.Where(a => !excludeIds.Contains(a.id)).Select(b => b.count);
        }
       
        /// <summary>
        /// 手动选择的优惠券
        /// </summary>
        /// <param name="baseCoupons"></param>
        /// <param name="shopNum"></param>
        /// <returns></returns>
        public static BaseCoupon GetSelectedCoupon(decimal totalPrice, IEnumerable<BaseAdditionalCoupon> baseCoupons, int shopNum = 0)
        {
            BaseCoupon c;
            if (baseCoupons != null)
            {
                var couponlist = baseCoupons.ToList();
                if (couponlist.Count > shopNum)
                {
                    var obj = couponlist[shopNum];
                    if (obj != null)//存在使用优惠券的情况
                    {
                        if (obj.Type == 0 && obj.Coupon != null)//优惠券
                        {
                            var uc = (obj.Coupon as Entities.CouponRecordInfo);
                            var info = CouponApplication.GetCouponInfo(uc.CouponId);
                            c = new BaseCoupon();
                            c.BaseEndTime = info.EndTime;
                            c.BaseId = uc.Id;
                            c.BaseName = info.CouponName;
                            c.BasePrice = info.Price;
                            c.BaseShopId = uc.ShopId;
                            c.BaseShopName = uc.ShopName;
                            c.BaseType = CommonModel.CouponType.Coupon;
                            c.OrderAmount = info.OrderAmount;
                            if (c.BasePrice >= totalPrice)
                                c.BasePrice = totalPrice;
                            return c;
                        }
                        else if (obj.Type == 1 && obj.Coupon != null)//代金红包
                        {
                            var sb = (obj.Coupon as ShopBonusReceiveInfo);

                            var service = GetService<ShopBonusService>();
                            var grant = service.GetGrant(sb.BonusGrantId);
                            var bonus = service.GetShopBonus(grant.ShopBonusId);
                            var shop = ShopApplication.GetShop(bonus.ShopId);

                            c = new BaseCoupon();
                            c.BaseEndTime = bonus.BonusDateEnd;
                            c.BaseId = sb.Id;
                            c.BaseName = bonus.Name;
                            c.BasePrice = sb.Price;
                            c.BaseShopId = shop.Id;
                            c.BaseShopName = shop.ShopName;
                            c.BaseType = CommonModel.CouponType.ShopBonus;
                            c.OrderAmount = bonus.UsrStatePrice;
                            //超过优惠券金额，使用优惠券最大金额
                            if (c.BasePrice >= totalPrice)
                                c.BasePrice = totalPrice;
                            return c;
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 在无法手动选择优惠券的场景下，自动选择合适的优惠券
        /// </summary>
        public static BaseCoupon GetDefaultCoupon(long shopid, long userid, decimal totalPrice, List<CartItemModel> cartItems = null, List<MobileShopCartItemModel> oneCouponlist = null, List<ShopCartItemModel> baselist = null)
        {
            var shopBonus = ShopBonusApplication.GetDetailToUse(shopid, userid, totalPrice);
            var userCouponsAll = CouponApplication.GetUserCoupon(shopid, userid, totalPrice);
            if (shopid > 0)
            {
                //userCouponsAll = userCouponsAll.Concat(CouponApplication.GetUserCoupon(0, userid, totalPrice)).ToList();
            }
            List<CouponRecordInfo> list = new List<CouponRecordInfo>();
            var coupons = CouponApplication.GetCouponInfo(userCouponsAll.Select(p => p.CouponId));
            var platCoupons = CouponApplication.GetPaltCouponList(shopid);
            foreach (var coupon in userCouponsAll)
            {
                var cou = coupons.FirstOrDefault(p => p.Id == coupon.CouponId);
                coupon.CouponInfo = cou;
                if (cou.UseArea == 1)
                {
                    decimal totalAmount = 0;
                    var canUse = false;
                    if (cou.ShopId > 0)
                    {
                        var pids = CouponApplication.GetCouponProductsByCouponId(coupon.CouponId).Select(p => p.ProductId).ToList();
                        foreach (var cartitem in cartItems)
                        {
                            if (pids.Contains(cartitem.id))
                            {
                                totalAmount += cartitem.count * cartitem.price - cartitem.fullDiscount;
                                canUse = true;
                            }
                        }
                    }
                    else
                    {
                        //平台券
                        if (platCoupons.Any(p => p.Id == cou.Id && p.OrderAmount <= totalPrice))
                        {
                            canUse = true;
                            totalAmount = cou.OrderAmount;
                        }
                    }

                    if (canUse && totalAmount >= cou.OrderAmount)
                    {
                        if (cou.Price > totalAmount)
                        {
                            cou.Price = totalAmount;
                        }
                        list.Add(coupon);
                    }
                }
                else
                {
                    list.Add(coupon);
                }
            }
            var userCoupons = list.OrderByDescending(p => p.CouponInfo.Price).ToList();
            BaseCoupon c;
            if (shopBonus.Count() > 0 && userCoupons.Count() > 0)
            {
                var sb = shopBonus.FirstOrDefault();      //商家红包
                //var uc = userCoupons.FirstOrDefault();  //优惠卷
                CouponRecordInfo uc = null;
                #region 同一张优惠券只能使用一次
                if (oneCouponlist != null)
                {
                    uc = userCoupons.FirstOrDefault(f => !oneCouponlist.Where(o => o.OneCoupons != null).Select(o => o.OneCoupons.BaseId).Contains(f.BaseId));
                }
                else if (baselist != null)
                {
                    uc = userCoupons.FirstOrDefault(f => !baselist.Where(o => o.OneCoupons != null).Select(o => o.OneCoupons.BaseId).Contains(f.BaseId));
                }
                if (uc == null)
                {
                    return null;
                }
                #endregion

                var info = CouponApplication.GetCouponInfo(uc.CouponId);
                if (sb.Price > info.Price)
                {
                    c = new BaseCoupon();
                    var service = GetService<ShopBonusService>();
                    var grant = service.GetGrant(sb.BonusGrantId);
                    var bonus = service.GetShopBonus(grant.ShopBonusId);
                    var shop = ShopApplication.GetShop(bonus.ShopId);

                    c.BaseEndTime = bonus.BonusDateEnd;
                    c.BaseId = sb.Id;
                    c.BaseName = bonus.Name;
                    c.BasePrice = sb.Price;
                    c.ShowPrice = sb.Price;
                    c.BaseShopId = shop.Id;
                    c.BaseShopName = shop.ShopName;
                    c.BaseType = CommonModel.CouponType.ShopBonus;
                    c.OrderAmount = bonus.UsrStatePrice;
                    //超过优惠券金额，使用优惠券最大金额
                    if (c.BasePrice >= totalPrice)
                        c.BasePrice = totalPrice;

                    return c;
                }
                else
                {
                    c = new BaseCoupon();

                    c.BaseEndTime = info.EndTime;
                    c.BaseId = uc.Id;
                    c.BaseName = info.CouponName;
                    c.BasePrice = info.Price;
                    c.ShowPrice = info.Price;
                    c.BaseShopId = info.ShopId;
                    c.BaseShopName = info.ShopName;
                    c.BaseType = CommonModel.CouponType.Coupon;
                    c.OrderAmount = info.OrderAmount;

                    var totalAmount = totalPrice;
                    if (info.UseArea == 1)
                    {
                        if (info.ShopId > 0)
                        {
                            var couponProducts = CouponApplication.GetCouponProductsByCouponId(uc.CouponId).Select(p => p.ProductId).ToList();
                            decimal coupontotal = 0;
                            foreach (var p in cartItems)
                            {
                                if (couponProducts.Contains(p.id))
                                    coupontotal += p.price * p.count - p.fullDiscount;
                            }
                            totalAmount = coupontotal;
                        }
                    }
                    if (c.BasePrice >= totalAmount)
                        c.BasePrice = totalAmount;
                    return c;
                }
            }
            else if (shopBonus.Count() <= 0 && userCoupons.Count() <= 0)
            {
                return null;
            }
            else if (shopBonus.Count() <= 0 && userCoupons.Count() > 0)
            {
                CouponRecordInfo coupon = null;
                #region 同一张优惠券只能使用一次
                if (oneCouponlist != null)
                {
                    coupon = userCoupons.FirstOrDefault(f => !oneCouponlist.Where(o => o.OneCoupons != null).Select(o => o.OneCoupons.BaseId).Contains(f.BaseId));
                }
                else if (baselist != null)
                {

                    coupon = userCoupons.FirstOrDefault(f => !baselist.Where(o => o.OneCoupons != null).Select(o => o.OneCoupons.BaseId).Contains(f.BaseId));
                }
                if (coupon == null)
                {
                    return null;
                }
                #endregion

                c = new BaseCoupon();
                var info = CouponApplication.GetCouponInfo(coupon.CouponId);
                c.BaseEndTime = info.EndTime;
                c.BaseId = coupon.Id;
                c.BaseName = info.CouponName;
                c.BasePrice = info.Price;
                c.ShowPrice = info.Price;
                c.BaseShopId = info.ShopId;
                c.BaseShopName = info.ShopName;
                c.BaseType = CommonModel.CouponType.Coupon;
                c.OrderAmount = info.OrderAmount;
                var totalAmount = totalPrice;
                if (info.UseArea == 1)
                {
                    if (info.ShopId > 0)
                    {
                        var couponProducts = CouponApplication.GetCouponProductsByCouponId(coupon.CouponId).Select(p => p.ProductId).ToList();
                        decimal coupontotal = 0;
                        foreach (var p in cartItems)
                        {
                            if (couponProducts.Contains(p.id))
                                coupontotal += p.price * p.count - p.fullDiscount;
                        }
                        totalAmount = coupontotal;
                    }
                }
                if (c.BasePrice >= totalAmount)
                    c.BasePrice = totalAmount;
                return c;
            }
            else if (shopBonus.Count() > 0 && userCoupons.Count() <= 0)
            {
                var coupon = shopBonus.FirstOrDefault();
                c = new BaseCoupon();


                var service = GetService<ShopBonusService>();
                var grant = service.GetGrant(coupon.BonusGrantId);
                var bonus = service.GetShopBonus(grant.ShopBonusId);
                var shop = ShopApplication.GetShop(bonus.ShopId);

                c.BaseEndTime = bonus.BonusDateEnd;
                c.BaseId = coupon.Id;
                c.BaseName = bonus.Name;
                c.BasePrice = coupon.Price;
                c.ShowPrice = coupon.Price;
                c.BaseShopId = shop.Id;
                c.BaseShopName = shop.ShopName;
                c.BaseType = CommonModel.CouponType.ShopBonus;
                c.OrderAmount = bonus.UsrStatePrice;
                if (c.BasePrice >= totalPrice)
                    c.BasePrice = totalPrice;
                return c;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获取销量
        /// </summary>
        /// <returns></returns>
        public static long GetSaleCount(DateTime? startDate = null, DateTime? endDate = null, long? shopBranchId = null, long? shopId = null, long? productId = null, bool hasReturnCount = true, bool hasWaitPay = false)
        {
            return Service.GetSaleCount(startDate, endDate, shopBranchId, shopId, productId, hasReturnCount, hasWaitPay);
        }

        #endregion

        #region 公共方法
        /// <summary>
        /// 获取订单统计
        /// </summary>
        /// <param name="shop">门店ID,为0表示不筛选</param>
        /// <param name="begin">开始日期</param>
        /// <param name="end">结束日期</param>
        /// <returns></returns>
        public static OrderDayStatistics GetDayStatistics(long shop, DateTime begin, DateTime end)
        {
            return Service.GetOrderDayStatistics(shop, begin, end);
        }

        /// <summary>
        /// 获取订单统计
        /// </summary>
        /// <param name="shop"></param>
        /// <returns></returns>
        public static OrderDayStatistics GetYesterDayStatistics(long shop)
        {
            var today = DateTime.Now.Date;
            var yesterday = today.AddDays(-1);
            var key = CacheKeyCollection.YesterDayStatistics(shop);
            var result = Cache.Get<OrderDayStatistics>(key);
            if (result == null)
            {
                result = GetDayStatistics(shop, yesterday, today);
                Cache.Insert(key, result);
            }
            return result;
        }


        /// <summary>
        /// 商家给订单备注
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="remark"></param>
        /// <param name="shopId">店铺ID</param>
        /// <param name="flag">紧急标识</param>
        public static void UpdateSellerRemark(long orderId, long shopId, string remark, int flag)
        {
            var order = Service.GetOrder(orderId);
            if (order == null)
                throw new MessageException(ExceptionMessages.NoFound, "订单");
            if (order.ShopId != shopId)
                throw new MessageException(ExceptionMessages.UnauthorizedOperation);
            Service.UpdateSellerRemark(orderId, remark, flag);
        }

        /// <summary>
        /// 根据订单项id获取订单项
        /// </summary>
        /// <param name="orderItemIds"></param>
        /// <returns></returns>
        public static List<OrderItem> GetOrderItems(IEnumerable<long> orderItemIds)
        {
            var list = Service.GetOrderItemsByOrderItemId(orderItemIds);
            return list.Map<List<OrderItem>>();
        }

        public static List<OrderItemInfo> GetOrderItems(long order)
        {
            return Service.GetOrderItemsByOrderId(order);
        }

        /// <summary>
        /// 根据订单id获取订单项
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static List<OrderItem> GetOrderItemsByOrderId(long orderId)
        {
            var list = Service.GetOrderItemsByOrderId(orderId);
            return list.Map<List<OrderItem>>();
        }


        /// <summary>
        /// 根据订单id获取订单项
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public static List<OrderItem> GetOrderItemsByOrderId(IEnumerable<long> orderIds)
        {
            var list = Service.GetOrderItemsByOrderId(orderIds);
            return list.Map<List<DTO.OrderItem>>();
        }

        /// <summary>
        /// 获取订单的评论数
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public static Dictionary<long, long> GetOrderCommentCount(IEnumerable<long> orderIds)
        {
            return Service.GetOrderCommentCount(orderIds);
        }

        public static long GetOrderCommentCount(long order)
        {
            var result = GetOrderCommentCount(new List<long> { order });
            return result.ContainsKey(order) ? result[order] : 0;
        }

        /// <summary>
        /// 根据订单项id获取售后记录
        /// </summary>
        /// <param name="orderItemIds"></param>
        /// <returns></returns>
        public static List<DTO.OrderRefund> GetOrderRefunds(IEnumerable<long> orderItemIds)
        {
            var result = Service.GetOrderRefunds(orderItemIds).Map<List<DTO.OrderRefund>>();
            return result;
        }
        public static List<OrderRefundInfo> GetOrderRefundsByOrder(long order)
        {
            return GetService<RefundService>().GetOrderRefundsByOrder(order);
        }
        /// <summary>
        /// 商家发货
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="companyName"></param>
        /// <param name="shipOrderNumber"></param>
        /// <param name="kuaidi100ReturnUrl"></param>
        public static void SellerSendGood(long orderId, string sellerName, string companyName, string shipOrderNumber, string kuaidi100ReturnUrl = "")
        {
            var order = Service.SellerSendGood(orderId, sellerName, companyName, shipOrderNumber);
            var siteSetting = SiteSettingApplication.SiteSettings;
            //var key = siteSetting.Kuaidi100Key;
            //if (!string.IsNullOrEmpty(key))
            //{
            //    Task.Factory.StartNew(() => ServiceProvider.Instance<ExpressService>.Create.SubscribeExpress100(order.ExpressCompanyName, order.ShipOrderNumber, key, order.RegionFullName, kuaidi100ReturnUrl));
            //}
            //if (siteSetting.KuaidiType != 0)
            //{
            //快递鸟物流轨迹，部分物流公司需要先订阅，目前通过调用获取接口实现
            Task.Factory.StartNew(() => ServiceProvider.Instance<ExpressService>.Create.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber, order.Id.ToString(), order.CellPhone));
            //}
            var orderitems = OrderApplication.GetOrderItems(order.Id);
            //发送通知消息
            var orderMessage = new MessageOrderInfo();
            orderMessage.OrderTime = order.OrderDate;
            orderMessage.OrderId = order.Id.ToString();
            orderMessage.ShopId = order.ShopId;
            orderMessage.UserName = order.UserName;
            orderMessage.ShopName = order.ShopName;
            orderMessage.SiteName = siteSetting.SiteName;
            orderMessage.TotalMoney = order.OrderTotalAmount;
            orderMessage.ShippingCompany = string.IsNullOrEmpty(companyName) ? "商家自有物流" : companyName;
            orderMessage.ShippingNumber = string.IsNullOrEmpty(shipOrderNumber) ? "无" : shipOrderNumber;
            orderMessage.ShipTo = (order.Platform == PlatformType.WeiXinSmallProg) ? ((DateTime)order.ShippingDate).ToString("yyyy-MM-dd HH:mm:ss") : (order.ShipTo + " " + order.RegionFullName + " " + order.Address);
            orderMessage.ProductName = orderitems.FirstOrDefault().ProductName;
            if (order.Platform == PlatformType.WeiXinSmallProg)
            {
                orderMessage.MsgOrderType = MessageOrderType.Applet;
            }
            Task.Factory.StartNew(() => SendGoodTask(orderMessage, order));
        }

        private static void SendGoodTask(MessageOrderInfo orderMessage, OrderInfo order)
        {
            ServiceProvider.Instance<MessageService>.Create.SendMessageOnOrderShipping(order.UserId, orderMessage);

        }

        /// <summary>
        /// 门店发货
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="companyName"></param>
        /// <param name="shipOrderNumber"></param>
        /// <param name="kuaidi100ReturnUrl"></param>
        public static void ShopSendGood(long orderId, int deliveryType, string shopkeeperName, string companyName, string shipOrderNumber, string kuaidi100ReturnUrl = "")
        {
            var order = Service.ShopSendGood(orderId, deliveryType, shopkeeperName, companyName, shipOrderNumber);
            if (deliveryType != 2 && deliveryType != DeliveryType.CityExpress.GetHashCode())
            {
                var siteSetting = SiteSettingApplication.SiteSettings;

                //var key = siteSetting.Kuaidi100Key;
                //if (!string.IsNullOrEmpty(key))
                //{
                //    Task.Factory.StartNew(() => ServiceProvider.Instance<ExpressService>.Create.SubscribeExpress100(order.ExpressCompanyName, order.ShipOrderNumber, key, order.RegionFullName, kuaidi100ReturnUrl));
                //}
                //if (siteSetting.KuaidiType != 0)
                //{
                //快递鸟物流轨迹，部分物流公司需要先订阅，目前通过调用获取接口实现
                Task.Factory.StartNew(() => ServiceProvider.Instance<ExpressService>.Create.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber, order.Id.ToString(), order.CellPhone));
                //}
            }
            //发送通知消息
            if (deliveryType != DeliveryType.CityExpress.GetHashCode())  //达达物流在回调中发送消息
            {
                SendMessageOnOrderShipping(orderId);
            }
        }
        public static void SendMessageOnOrderShipping(long orderId)
        {
            var order = Service.GetOrder(orderId);
#if DEBUG
            Log.Debug("[SGM]" + orderId + "_" + order.ExpressCompanyName + "_" + order.ShipOrderNumber);
#endif
            var orderitems = OrderApplication.GetOrderItems(order.Id);
            //发送通知消息
            var orderMessage = new MessageOrderInfo();
            orderMessage.OrderTime = order.OrderDate;
            orderMessage.OrderId = order.Id.ToString();
            orderMessage.ShopId = order.ShopId;
            orderMessage.UserName = order.UserName;
            orderMessage.ShopName = order.ShopName;
            orderMessage.SiteName = SiteSettingApplication.SiteSettings.SiteName;
            orderMessage.TotalMoney = order.OrderTotalAmount;
            orderMessage.ShippingCompany = string.IsNullOrEmpty(order.ExpressCompanyName) ? "商家自有物流" : order.ExpressCompanyName;
            orderMessage.ShippingNumber = string.IsNullOrEmpty(order.ShipOrderNumber) ? "无" : order.ShipOrderNumber;
            orderMessage.ShipTo = (order.Platform == PlatformType.WeiXinSmallProg) ? ((DateTime)order.ShippingDate).ToString("yyyy-MM-dd HH:mm:ss") : (order.ShipTo + " " + order.RegionFullName + " " + order.Address);
            orderMessage.ProductName = orderitems.FirstOrDefault().ProductName;
            if (order.Platform == PlatformType.WeiXinSmallProg)
            {
                orderMessage.MsgOrderType = MessageOrderType.Applet;
            }
            Task.Factory.StartNew(() => ServiceProvider.Instance<MessageService>.Create.SendMessageOnOrderShipping(order.UserId, orderMessage));
        }
        /// <summary>
        /// 判断订单是否正在申请售后
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static bool IsOrderAfterService(long orderId)
        {
            return Service.IsOrderAfterService(orderId);
        }

        /// <summary>
        /// 修改快递信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="companyName"></param>
        /// <param name="shipOrderNumber"></param>
        /// <returns></returns>
        public static void UpdateExpress(long orderId, string companyName, string shipOrderNumber, string kuaidi100ReturnUrl = "")
        {
            var order = Service.UpdateExpress(orderId, companyName, shipOrderNumber);

            var key = SiteSettingApplication.SiteSettings.Kuaidi100Key;
            if (!string.IsNullOrEmpty(key))
            {
                Task.Factory.StartNew(() => ServiceProvider.Instance<ExpressService>.Create.SubscribeExpress100(order.ExpressCompanyName, order.ShipOrderNumber, key, order.RegionFullName, kuaidi100ReturnUrl));
            }
        }
        /// <summary>
        /// 所有订单是否都支付
        /// </summary>
        /// <param name="orderids"></param>
        /// <returns></returns>
        public static bool AllOrderIsPaied(string orderids)
        {
            var orders = Service.GetOrders(orderids.Split(',').Select(t => long.Parse(t)));
            IEnumerable<OrderInfo> waitPayOrders = orders.Where(p => p.OrderStatus == OrderInfo.OrderOperateStatus.WaitPay);
            if (waitPayOrders.Count() > 0)
            {//有待付款的订单，则未支付完成
                return false;
            }
            return true;
        }
        #endregion

        #region web私有方法

        /// <summary>
        /// 对PaymentId进行加密（因为PaymentId中包含小数点"."，因此进行编码替换）
        /// </summary>
        static string EncodePaymentId(string paymentId)
        {
            return paymentId.Replace(".", "-");
        }
     

        /// <summary>
        /// 获取默认发票信息
        /// </summary>
        /// <param name="userid"></param>
        /// <param name="cellPhone"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public static InvoiceTitleInfo GetDefaultInvoiceInfo(long userid, ref string cellPhone, ref string email, ref string invoiceName, ref string invoiceCode)
        {
            //默认电子发票信息
            cellPhone = string.Empty;
            email = string.Empty;
            invoiceName = string.Empty;
            invoiceCode = string.Empty;
            var invoice = ShopApplication.GetInvoiceTitleInfo(userid, InvoiceType.OrdinaryInvoices);
            if (invoice != null)
            {
                invoiceName = invoice.Name;
                invoiceCode = invoice.Code;
            }

            var invoiceTitle = ShopApplication.GetInvoiceTitleInfo(userid, InvoiceType.ElectronicInvoice);
            if (invoiceTitle != null)
            {
                cellPhone = invoiceTitle.CellPhone;
                email = invoiceTitle.Email;
            }
            else
            {
                var bindPhone = MessageApplication.GetDestination(userid, "Himall.Plugin.Message.SMS", Entities.MemberContactInfo.UserTypes.General);
                if (!string.IsNullOrEmpty(bindPhone))
                    cellPhone = bindPhone;
                var bindEmail = MessageApplication.GetDestination(userid, "Himall.Plugin.Message.Email", Entities.MemberContactInfo.UserTypes.General);
                if (!string.IsNullOrEmpty(bindEmail))
                    email = bindEmail;
            }
            //默认增值税发票信息
            var vatInvoice = ShopApplication.GetInvoiceTitleInfo(userid, InvoiceType.VATInvoice);
            if (vatInvoice == null)
            {
                vatInvoice = new InvoiceTitleInfo();
                var defaultAddress = ShippingAddressApplication.GetDefaultUserShippingAddressByUserId(userid);
                if (defaultAddress != null)
                {
                    vatInvoice.RealName = defaultAddress.ShipTo;
                    vatInvoice.CellPhone = defaultAddress.Phone;
                    vatInvoice.RegionID = defaultAddress.RegionId;
                    vatInvoice.Address = defaultAddress.Address + " " + defaultAddress.AddressDetail;
                }

            }
            vatInvoice.RegionFullName = RegionApplication.GetFullName(vatInvoice.RegionID);
            return vatInvoice;
        }



        /// <summary>
        /// 获取购物车中的商品
        /// </summary>
        /// <returns></returns>
        public static Himall.Entities.ShoppingCartInfo GetCart(long memberId, string cartInfo)
        {
            Himall.Entities.ShoppingCartInfo shoppingCartInfo;
            if (memberId > 0)//已经登录，系统从服务器读取购物车信息，否则从Cookie获取购物车信息
                shoppingCartInfo = CartApplication.GetCart(memberId);
            else
            {
                shoppingCartInfo = new Himall.Entities.ShoppingCartInfo();

                if (!string.IsNullOrWhiteSpace(cartInfo))
                {
                    string[] cartItems = cartInfo.Split(',');
                    var cartInfoItems = new List<Himall.Entities.ShoppingCartItem>();
                    int i = 0;
                    foreach (string cartItem in cartItems)
                    {
                        var cartItemParts = cartItem.Split(':');
                        cartInfoItems.Add(new Himall.Entities.ShoppingCartItem() { ProductId = long.Parse(cartItemParts[0].Split('_')[0]), SkuId = cartItemParts[0], Quantity = int.Parse(cartItemParts[1]) });
                    }
                    shoppingCartInfo.Items = cartInfoItems;
                }
            }
            return shoppingCartInfo;
        }

        /// <summary>
        /// 支付完生成红包
        /// </summary>
        private static Dictionary<long, ShopBonusInfo> GenerateBonus(IEnumerable<long> orderIds, string urlHost)
        {
            Dictionary<long, ShopBonusInfo> bonusGrantIds = new Dictionary<long, ShopBonusInfo>();
            string url = Core.Helper.WebHelper.GetScheme() + "://" + urlHost + "/m-weixin/shopbonus/index/";
            var buyOrders = Service.GetOrders(orderIds);
            foreach (var o in buyOrders)
            {
                var shopBonus = ShopBonusApplication.GetByShopId(o.ShopId);
                if (shopBonus == null)
                {
                    continue;
                }
                if (shopBonus.GrantPrice <= o.TotalAmount)
                {
                    long grantid = ShopBonusApplication.GenerateBonusDetail(shopBonus, o.Id, url,o.TotalAmount);
                    bonusGrantIds.Add(grantid, shopBonus);
                }
            }
            return bonusGrantIds;
        }

        /// <summary>
        /// 更改限时购销售量
        /// </summary>
        private static void IncreaseSaleCount(List<long> orderid)
        {
            if (orderid.Count == 1)
            {
                _LimitTimeBuyService.IncreaseSaleCount(orderid);
            }
        }

        // 平台确认订单支付
        public static void PlatformConfirmOrderPay(long orderId, string payRemark, string managerName)
        {
            WDTConfigModel wDTConfigModel = WDTOrderApplication.GetConfigModel();
            Service.PlatformConfirmOrderPay(orderId, payRemark, managerName, wDTConfigModel);
        }

        /// <summary>
        /// 处理会员订单类别
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="userId"></param>
        public static void DealDithOrderCategoryByUserId(long orderId, long userId)
        {
            var orderItem = GetOrderItemsByOrderId(orderId);
            var productIds = orderItem.Select(p => p.ProductId);
            var product = ProductManagerApplication.GetProductsByIds(productIds);
            foreach (var item in product)
            {
                var categoryId = long.Parse(item.CategoryPath.Split('|')[0]);
                OrderAndSaleStatisticsApplication.SynchronizeMemberBuyCategory(categoryId, userId);
            }
        }
        /// <summary>
        /// 根据订单ID获取订单商品明细，包括商品店铺信息
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static List<OrderDetailView> GetOrderDetailViews(IEnumerable<long> ids)
        {
            var list = Service.GetOrders(ids).Map<List<FullOrder>>();
            List<OrderDetailView> orderDetails = new List<OrderDetailView>();
            var orderItems = GetOrderItemsByOrderId(list.Select(p => p.Id));//订单明细
            var shops = ShopApplication.GetShops(orderItems.Select(e => e.ShopId).ToList());//店铺信息
            var shopbranchs = ShopBranchApplication.GetStores(list.Where(d => d.ShopBranchId > 0).Select(d => d.ShopBranchId).ToList());
            var vShops = VshopApplication.GetVShopsByShopIds(orderItems.Select(e => e.ShopId));//微店信息
            foreach (var orderItem in orderItems)
            {
                //完善图片地址
                string s_pimg = orderItem.ThumbnailsUrl;
                orderItem.ThumbnailsUrl = s_pimg.Contains("skus") ? HimallIO.GetRomoteImagePath(s_pimg) : HimallIO.GetRomoteProductSizeImage(s_pimg, 1, (int)ImageSize.Size_500);
                orderItem.ProductImage = HimallIO.GetRomoteProductSizeImage(s_pimg, 1, (int)ImageSize.Size_100);
                orderItem.ShareImage = HimallIO.GetRomoteProductSizeImage(s_pimg, 1, (int)ImageSize.Size_50);
            }
            foreach (var item in list)
            {
                OrderDetailView detail = new OrderDetailView() { };
                var vshop = vShops.FirstOrDefault(e => e.ShopId == item.ShopId);
                long vshopId = 0;
                if (vshop != null)
                {
                    vshopId = vshop.Id;
                }
                detail.Detail = new OrderDetail
                {
                    ShopId = item.ShopId,
                    ShopName = shops.FirstOrDefault(e => e.Id == item.ShopId).ShopName,
                    VShopId = vshopId,
                    OrderItems = orderItems.Where(p => p.OrderId == item.Id).ToList()
                };
                if (item.ShopBranchId > 0)
                {
                    var sb = shopbranchs.FirstOrDefault(d => d.Id == item.ShopBranchId);
                    if (sb != null)
                    {
                        detail.Detail.ShopBranchName = sb.ShopBranchName;
                        detail.Detail.ShopBranchId = sb.Id;
                    }
                }
                detail.Order = item.Map<OrderInfo>();
                orderDetails.Add(detail);
            }

            return orderDetails;
        }
        #endregion
        #region 商家手动分配门店
        /// <summary>
        /// 分配门店时更新商家、门店库存
        /// </summary>
        /// <param name="skuIds"></param>
        /// <param name="quantity"></param>
        public static void AllotStoreUpdateStock(List<string> skuIds, List<int> counts, long shopBranchId)
        {
            Service.AllotStoreUpdateStock(skuIds, counts, shopBranchId);
        }
        /// <summary>
        /// 分配门店订单到新门店
        /// </summary>
        /// <param name="skuIds"></param>
        /// <param name="newShopBranchId"></param>
        /// <param name="oldShopBranchId"></param>
        public static void AllotStoreUpdateStockToNewShopBranch(List<string> skuIds, List<int> counts, long newShopBranchId, long oldShopBranchId)
        {
            Service.AllotStoreUpdateStockToNewShopBranch(skuIds, counts, newShopBranchId, oldShopBranchId);
        }
        /// <summary>
        /// 分配门店订单回到商家
        /// </summary>
        /// <param name="skuIds"></param>
        /// <param name="shopBranchId"></param>
        /// <param name="shopId"></param>
        public static void AllotStoreUpdateStockToShop(List<string> skuIds, List<int> counts, long shopBranchId)
        {
            Service.AllotStoreUpdateStockToShop(skuIds, counts, shopBranchId);
        }
        /// <summary>
        /// 更新订单所属门店
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="shopBranchId"></param>
        public static void UpdateOrderShopBranch(long orderId, long shopBranchId)
        {
            Service.UpdateOrderShopBranch(orderId, shopBranchId);
        }

        #endregion


        public static void CalculateOrderItemRefund(long orderId, bool isCompel = false)
        {
            Service.CalculateOrderItemRefund(orderId, isCompel);
        }
        public static OrderInfo GetOrder(long orderId, long userId)
        {
            return Service.GetOrder(orderId, userId);
        }
        public static bool IsRefundTimeOut(long orderId)
        {
            return Service.IsRefundTimeOut(orderId);
        }
        public static void MembeConfirmOrder(long orderId, string memberName)
        {
            Service.MembeConfirmOrder(orderId, memberName);
        }

        public static OrderItemInfo GetOrderItem(long orderItemId)
        {
            return Service.GetOrderItem(orderItemId);
        }
        /// <summary>
        /// 取最近time分钟内的满足打印的订单数据
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static List<long> GetOrderIdsByLatestTime(int time, long shopBranchId, long shopId)
        {
            return Service.GetOrderIdsByLatestTime(time, shopBranchId, shopId);
        }
       /// <summary>
       /// 获取最近语音播报的订单数据
       /// </summary>
       /// <param name="time"></param>
       /// <param name="shopBranchId"></param>
       /// <param name="shopId"></param>
       /// <returns></returns>
        public static List<long> GetOrderIdsByLatestTimeYuyin(int time, long shopBranchId, long shopId)
        {
            return Service.GetOrderIdsByLatestTimeYuyin(time, shopBranchId, shopId);
        }
        /// <summary>
        /// 是否可以售后
        /// </summary>
        /// <param name="data"></param>
        /// <param name="refundStatus">售后状态,null表示方法自查</param>
        /// <param name="itemId">订单项编号,null表示订单退款</param>
        /// <returns></returns>
        public static bool CanRefund(Order data, int? refundStatus = null, long? itemId = null)
        {
            bool result = false;
            if (itemId == null || itemId <= 0)
            {
                if (refundStatus == null)
                {
                    OrderRefundInfo _ordrefobj = _RefundService.GetOrderRefundByOrderId(data.Id);
                    if (data.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery && data.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                    {
                        _ordrefobj = null;
                    }
                    refundStatus = (_ordrefobj == null ? null : (int?)_ordrefobj.SellerAuditStatus);
                    refundStatus = (refundStatus > 4 ? (int?)_ordrefobj.ManagerConfirmStatus : refundStatus);
                }

                result = (data.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || data.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                    && !data.RefundStats.HasValue && data.PaymentType != Entities.OrderInfo.PaymentTypes.CashOnDelivery && data.PaymentType != Entities.OrderInfo.PaymentTypes.None
                    && (data.FightGroupCanRefund == null || data.FightGroupCanRefund == true);
                result = result && (refundStatus.GetValueOrDefault().Equals(0) || refundStatus.GetValueOrDefault().Equals(4));
                ;
            }
            else
            {
                if (data.OrderType == OrderInfo.OrderTypes.Virtual)
                {
                    var itemInfo = GetOrderItems(data.Id).FirstOrDefault();
                    if (itemInfo != null)
                    {
                        var virtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(itemInfo.ProductId);
                        if (virtualProductInfo != null)
                        {
                            //如果该商品支持退款，而订单状态为待消费，则可退款
                            if (virtualProductInfo.SupportRefundType == (sbyte)ProductInfo.SupportVirtualRefundType.SupportAnyTime)
                            {
                                if (data.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification)
                                {
                                    result = true;
                                }
                            }
                            else if (virtualProductInfo.SupportRefundType == (sbyte)ProductInfo.SupportVirtualRefundType.SupportValidity)
                            {
                                if (virtualProductInfo.EndDate.Value > DateTime.Now)
                                {
                                    if (data.OrderStatus == OrderInfo.OrderOperateStatus.WaitVerification)
                                    {
                                        result = true;
                                    }
                                }
                            }
                            else if (virtualProductInfo.SupportRefundType == (sbyte)ProductInfo.SupportVirtualRefundType.NonSupport)
                            {
                                result = false;
                            }
                            if (result)
                            {
                                var orderVerificationCodes = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { data.Id });
                                long num = orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification).Count();
                                if (num > 0)
                                {
                                    result = true;
                                }
                                else
                                {
                                    result = false;
                                }
                            }
                        }
                    }
                }
                else
                {
                    result = RefundApplication.CanApplyRefund(data.Id, itemId.Value, false);
                    result = result && !IsRefundTimeOut(data.Id);

                    if (data.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || data.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                    {
                        result = false;  //待收货 待自提只可以订单退款
                    }
                    if (data.PaymentType == Entities.OrderInfo.PaymentTypes.CashOnDelivery && data.OrderStatus != OrderInfo.OrderOperateStatus.Finish)
                    {
                        result = false;  //货到付款在订单未完成前不可以售后
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 是否可以售后
        /// </summary>
        /// <param name="data"></param>
        /// <param name="refundStatus">售后状态,null表示方法自查</param>
        /// <param name="itemId">订单项编号,null表示订单退款</param>
        /// <returns></returns>
        public static bool CanRefund(OrderInfo data, int? refundStatus = null, long? itemId = null)
        {
            var cdata = AutoMapper.Mapper.Map<DTO.Order>(data);
            return CanRefund(cdata, refundStatus, itemId);
        }

        public static List<OrderInfo> GetUserOrders(long user, int top)
        {
            return Service.GetTopOrders(top, user);
        }
        public static List<OrderComplaintInfo> GetOrderComplaintByOrders(List<long> orders)
        {
            return Service.GetOrderComplaintByOrders(orders);
        }
        public static int GetOrderTotalProductCount(long order)
        {
            return Service.GetOrderTotalProductCount(order);
        }
        public static List<OrderCommentInfo> GetOrderComment(long order)
        {
            return GetService<TradeCommentService>().GetOrderCommentsByOrder(order);
        }

        /// <summary>
        /// 虚拟订单用户信息项
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static List<VirtualOrderItemInfo> GetVirtualOrderItemInfosByOrderId(long orderId)
        {
            return Service.GetVirtualOrderItemInfosByOrderId(orderId);
        }

        /// <summary>
        /// 订单核销码
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static List<OrderVerificationCodeInfo> GetOrderVerificationCodeInfosByOrderIds(List<long> orderIds)
        {
            return Service.GetOrderVerificationCodeInfosByOrderIds(orderIds);
        }
        /// <summary>
        /// 根据核销码获取唯一条核销码信息
        /// </summary>
        /// <param name="verificationCode"></param>
        /// <returns></returns>
        public static OrderVerificationCodeInfo GetOrderVerificationCodeInfoByCode(string verificationCode)
        {
            return Service.GetOrderVerificationCodeInfoByCode(verificationCode);
        }
        public static List<OrderVerificationCodeInfo> GetOrderVerificationCodeInfoByCodes(List<string> verificationCodes)
        {
            return Service.GetOrderVerificationCodeInfoByCodes(verificationCodes);
        }
        /// <summary>
        /// 更新订单核销码状态
        /// </summary>
        /// <param name="verficationCodes"></param>
        /// <returns></returns>
        public static bool UpdateOrderVerificationCodeStatusByCodes(List<string> verficationCodes, long orderId, OrderInfo.VerificationCodeStatus status, DateTime? verificationTime, string verificationUser = "")
        {
            return Service.UpdateOrderVerificationCodeStatusByCodes(verficationCodes, orderId, status, verificationTime, verificationUser);
        }
        /// <summary>
        /// 新增核销记录
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public static bool AddVerificationRecord(VerificationRecordInfo info)
        {
            return Service.AddVerificationRecord(info);
        }
        public static VerificationRecordInfo GetVerificationRecordInfoById(long id)
        {
            return Service.GetVerificationRecordInfoById(id);
        }

        public static int GetWaitConsumptionOrderNumByUserId(long userId = 0, long shopId = 0, long shopBranchId = 0)
        {
            return Service.GetWaitConsumptionOrderNumByUserId(userId, shopId, shopBranchId);
        }
        public static QueryPageModel<VerificationRecordModel> GetVerificationRecords(VerificationRecordQuery query)
        {
            var data = Service.GetVerificationRecords(query);
            if (data.Models.Count <= 0)
                return new QueryPageModel<VerificationRecordModel>()
                {
                    Models = new List<VerificationRecordModel>(),
                    Total = data.Total
                };

            var models = data.Models.Map<List<DTO.VerificationRecordModel>>();
            var orderItems = OrderApplication.GetOrderItemsByOrderId(models.Select(a => a.OrderId));
            var products = ProductManagerApplication.GetAllProductByIds(orderItems.Select(a => a.ProductId));
            var shopBranchs = ShopBranchApplication.GetStores(models.Select(a => a.ShopBranchId).ToList());
            var shops = ShopApplication.GetShops(models.Select(a => a.ShopId).ToList());
            foreach (var a in models)
            {
                var shopBranchInfo = shopBranchs.FirstOrDefault(p => p.Id == a.ShopBranchId);
                if (shopBranchInfo != null)
                {
                    a.Name = shopBranchInfo.ShopBranchName;
                }
                if (string.IsNullOrWhiteSpace(a.Name) && a.ShopBranchId == 0)
                {
                    var shop = shops.FirstOrDefault(p => p.Id == a.ShopId);
                    if (shop != null)
                        a.Name = shop.ShopName;
                }
                if (string.IsNullOrWhiteSpace(a.Name) && a.ShopBranchId == 0)
                {
                    var shop = shops.FirstOrDefault(p => p.Id == a.ShopId);
                    if (shop != null)
                        a.Name = shop.ShopName;
                }

                a.VerificationTimeText = a.VerificationTime.ToString("yyyy-MM-dd HH:mm:ss");

                a.PayDateText = a.PayDate.HasValue ? a.PayDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";


                a.PayDateText = a.PayDate.HasValue ? a.PayDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";


                a.VerificationCodeIds = Regex.Replace(a.VerificationCodeIds, @"(\d{4})", "$1 ").Replace(",", " ");



                var orderItemInfo = orderItems.FirstOrDefault(p => p.OrderId == a.OrderId);
                if (orderItemInfo != null)
                {
                    var productInfo = products.FirstOrDefault(p => p.Id == orderItemInfo.ProductId);
                    if (productInfo != null)
                    {
                        a.ProductName = productInfo.ProductName;
                        a.ImagePath = Core.HimallIO.GetRomoteProductSizeImage(productInfo.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_350);
                        a.Specifications = string.Format("{0}{1}{2}", orderItemInfo.Color, orderItemInfo.Size, orderItemInfo.Version);
                        a.Quantity = a.VerificationCodeIds.Trim(',').Split(',').Count();
                        a.Price = orderItemInfo.SalePrice;
                        a.Time = a.VerificationTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                }
            }


            foreach (var a in models)
            {
                var shopBranchInfo = shopBranchs.FirstOrDefault(p => p.Id == a.ShopBranchId);
                if (shopBranchInfo != null)
                {
                    a.Name = shopBranchInfo.ShopBranchName;
                }
                if (string.IsNullOrWhiteSpace(a.Name) && a.ShopBranchId == 0)
                {
                    var shop = shops.FirstOrDefault(p => p.Id == a.ShopId);
                    if (shop != null)
                        a.Name = shop.ShopName;
                }

            }

            return new QueryPageModel<VerificationRecordModel>
            {
                Models = models,
                Total = data.Total
            };
        }

        public static QueryPageModel<OrderVerificationCodeModel> GetOrderVerificationCodeInfos(VerificationRecordQuery query)
        {
            var data = Service.GetOrderVerificationCodeInfos(query);
            if (data.Models.Count <= 0)
                return new QueryPageModel<OrderVerificationCodeModel>()
                {
                    Models = new List<OrderVerificationCodeModel>(),
                    Total = data.Total
                };

            var models = data.Models.Map<List<DTO.OrderVerificationCodeModel>>();
            var shopBranchs = ShopBranchApplication.GetStores(models.Select(a => a.ShopBranchId).ToList());
            var shops = ShopApplication.GetShops(models.Select(a => a.ShopId).ToList());
            foreach (var a in models)
            {
                var shopBranchInfo = shopBranchs.FirstOrDefault(p => p.Id == a.ShopBranchId);
                if (shopBranchInfo != null)
                {
                    a.Name = shopBranchInfo.ShopBranchName;
                }
                if (string.IsNullOrWhiteSpace(a.Name) && a.ShopBranchId == 0)
                {
                    var shop = shops.FirstOrDefault(p => p.Id == a.ShopId);
                    if (shop != null)
                        a.Name = shop.ShopName;
                }
                a.VerificationTimeText = a.VerificationTime.HasValue ? a.VerificationTime.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                a.StatusText = a.Status.ToDescription();
                a.PayDateText = a.PayDate.HasValue ? a.PayDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "";
                if (a.Status == OrderInfo.VerificationCodeStatus.WaitVerification || a.Status == OrderInfo.VerificationCodeStatus.Refund)
                {
                    a.VerificationCode = Regex.Replace(a.VerificationCode, "(\\d{4})\\d{4}(\\d{4})", "$1 **** $2");
                }
                else
                {
                    a.VerificationCode = Regex.Replace(a.VerificationCode, @"(\d{4})", "$1 ");
                }
            }
            return new QueryPageModel<OrderVerificationCodeModel>
            {
                Models = models,
                Total = data.Total
            };
        }

        public static List<SearchShopAndShopbranchModel> GetShopOrShopBranch(string keyword, sbyte? type = null)
        {
            return Service.GetShopOrShopBranch(keyword, type);
        }

        /// <summary>
        /// 获取订单发票实体
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public static OrderInvoiceInfo GetOrderInvoiceInfo(long orderId)
        {
            return Service.GetOrderInvoiceInfo(orderId);
        }

        public static void UpdateVirtualProductStatus()
        {
            Service.UpdateVirtualProductStatus();
        }

        public static void UpdateOrderVerificationCodeStatus()
        {
            Service.UpdateOrderVerificationCodeStatus();
        }

        //发票抬头
        public static List<InvoiceTitleInfo> GetInvoiceTitles(long userid, InvoiceType? type)
        {
            if (type.HasValue)
            {
                return Service.GetInvoiceTitles(userid, type.Value);
            }
            else
            {
                return Service.GetInvoiceTitles(userid);
            }
        }
        /// <summary>
        /// 获取订单支付信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<OrderPayInfo> GetOrderPay(long id)
        {
            return Service.GetOrderPay(id);
        }
        /// <summary>
        /// 支付
        /// </summary>
        /// <param name="orderIds"></param>
        /// <param name="paymentId"></param>
        /// <param name="payTime"></param>
        /// <param name="payNo"></param>
        /// <param name="payId"></param>
        /// <param name="paymentType"></param>
        /// <param name="payRemark"></param>
        public static void PaySucceed(IEnumerable<long> orderIds, string paymentId, DateTime payTime, string payNo = null
           , long payId = 0, OrderInfo.PaymentTypes paymentType = OrderInfo.PaymentTypes.Online
           , string payRemark = "")
        {
            WDTConfigModel wDTConfigModel = WDTOrderApplication.GetConfigModel();
            Service.PaySucceed(orderIds, paymentId, payTime, wDTConfigModel, payNo, payId, paymentType, payRemark);
        }
    }
}
