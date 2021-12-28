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
using System.Drawing;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    public class UserOrderController : BaseApiController
    {
        /// <summary>
        /// 获取订单详情
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetOrderDetail(long orderId)
        {
            CheckUserLogin();
            var orderService = ServiceProvider.Instance<OrderService>.Create;
            var order = orderService.GetOrder(orderId, CurrentUser.Id);
            var orderitems = orderService.GetOrderItemsByOrderId(order.Id);
            var orderRefundService = ServiceProvider.Instance<RefundService>.Create;
            var productService = ServiceProvider.Instance<ProductService>.Create;
            var coupon = ServiceProvider.Instance<CouponService>.Create.GetCouponRecordInfo(order.UserId, order.Id);

            string couponName = "";
            decimal couponAmout = 0;
            if (coupon != null)
            {
                var c = CouponApplication.GetCouponInfo(coupon.CouponId);
                couponName = c.CouponName;
                couponAmout = c.Price;
            }

            //订单信息是否正常
            if (order == null)
            {
                throw new HimallException("订单号不存在！");
            }
            dynamic expressTrace = new ExpandoObject();

            //取订单物流信息
            if (!string.IsNullOrWhiteSpace(order.ShipOrderNumber))
            {
                var expressData = ServiceProvider.Instance<ExpressService>.Create.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber,order.Id.ToString(), order.CellPhone);
                if (expressData.Success)
                {
                    expressData.ExpressDataItems = expressData.ExpressDataItems.OrderByDescending(item => item.Time);//按时间逆序排列
                    expressTrace.traces = expressData.ExpressDataItems.Select(item => new
                    {
                        acceptTime = item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                        acceptStation = item.Content
                    });

                }
            }
            var orderRefunds = OrderApplication.GetOrderRefunds(orderitems.Select(p => p.Id));
            var isCanOrderReturn = OrderApplication.CanRefund(order);
            //获取订单商品项数据
            var orderDetail = new
            {
                ShopId = order.ShopId,
                EnabledRefundAmount = order.OrderEnabledRefundAmount,
                OrderItems = orderitems.Select(item =>
                {
                    var productinfo = productService.GetProduct(item.ProductId);
                    Entities.TypeInfo typeInfo = productinfo == null ? null : ServiceProvider.Instance<TypeService>.Create.GetType(productinfo.TypeId);
                    string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    var itemStatusText = "";
                    var itemrefund = orderRefunds.Where(or => or.OrderItemId == item.Id).FirstOrDefault(d => d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                    int? itemrefstate = (itemrefund == null ? 0 : (int?)itemrefund.SellerAuditStatus);
                    itemrefstate = (itemrefstate > 4 ? (int?)itemrefund.ManagerConfirmStatus : itemrefstate);
                    if (itemrefund != null)
                    {//默认为商家处理进度
                        if (itemrefstate == 4)
                        {//商家拒绝,可以再发起申请
                            itemStatusText = "";
                        }
                        else
                        {
                            itemStatusText = "售后处理中";
                        }
                    }
                    if (itemrefstate > 4)
                    {//如果商家已经处理完，则显示平台处理进度
                        if (itemrefstate == 7)
                        {
                            itemStatusText = "退款成功";
                        }
                    }
                    if (productinfo != null)
                    {
                        colorAlias = (!string.IsNullOrWhiteSpace(productinfo.ColorAlias)) ? productinfo.ColorAlias : colorAlias;//如果商品有自定义规格名称，则用
                        sizeAlias = (!string.IsNullOrWhiteSpace(productinfo.SizeAlias)) ? productinfo.SizeAlias : sizeAlias;
                        versionAlias = (!string.IsNullOrWhiteSpace(productinfo.VersionAlias)) ? productinfo.VersionAlias : versionAlias;
                    }

                    long activeId = 0;
                    int activetype = 0;
                    var limitbuyser = ServiceProvider.Instance<LimitTimeBuyService>.Create;
                    var limitBuy = limitbuyser.GetLimitTimeMarketItemByProductId(item.ProductId);
                    if (limitBuy != null)
                    {
                        //salePrice = limitBuy.MinPrice;
                        activeId = limitBuy.Id;
                        activetype = 1;
                    }
                    else
                    {
                        #region 限时购预热
                        var FlashSale = limitbuyser.IsFlashSaleDoesNotStarted(item.ProductId);
                        var FlashSaleConfig = limitbuyser.GetConfig();

                        if (FlashSale != null)
                        {
                            TimeSpan flashSaleTime = DateTime.Parse(FlashSale.BeginDate) - DateTime.Now;  //开始时间还剩多久
                            TimeSpan preheatTime = new TimeSpan(FlashSaleConfig.Preheat, 0, 0);  //预热时间是多久
                            if (preheatTime >= flashSaleTime)  //预热大于开始
                            {
                                if (!FlashSaleConfig.IsNormalPurchase)
                                {
                                    activeId = FlashSale.Id;
                                    activetype = 1;
                                }
                            }
                        }
                        #endregion
                    }
                    string picimg = HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)ImageSize.Size_100);
                    if (!string.IsNullOrEmpty(item.ThumbnailsUrl) && item.ThumbnailsUrl.Contains("skus"))
                        picimg = HimallIO.GetRomoteImagePath(item.ThumbnailsUrl);

                    return new
                    {
                        Status = itemrefstate,
                        StatusText = itemStatusText,
                        Id = item.Id,
                        SkuId = item.SkuId,
                        ProductId = item.ProductId,
                        Name = item.ProductName,
                        Amount = item.Quantity,
                        Price = item.SalePrice,
                        //ProductImage = "http://" + Url.Request.RequestUri.Host + productService.GetProduct(item.ProductId).GetImage(ProductInfo.ImageSize.Size_100),
                        Image = picimg,
                        color = item.Color,
                        size = item.Size,
                        version = item.Version,
                        IsCanRefund = OrderApplication.CanRefund(order, itemrefstate, itemId: item.Id),
                        ColorAlias = colorAlias,
                        SizeAlias = sizeAlias,
                        VersionAlias = versionAlias,
                        SkuText = colorAlias + ":" + item.Color + ";" + sizeAlias + ":" + item.Size + ";" + versionAlias + ":" + item.Version,
                        EnabledRefundAmount = item.EnabledRefundAmount,
                        ActiveId = activeId,//活动Id
                        ActiveType = activetype//活动类型（1代表限购，2代表团购，3代表商品预售，4代表限购预售，5代表团购预售）
                    };
                })
            };

            //取拼团订单状态
            var fightGroupOrderInfo = ServiceProvider.Instance<FightGroupService>.Create.GetFightGroupOrderStatusByOrderId(order.Id);
            #region 门店信息
            var branchInfo = new ShopBranch();
            if (order.ShopBranchId > 0)
            {
                branchInfo = ShopBranchApplication.GetShopBranchById(order.ShopBranchId);
            }
            else
                branchInfo = null;
            #endregion

            #region 虚拟订单信息
            VirtualProductInfo virtualProductInfo = null;
            int validityType = 0; string startDate = string.Empty, endDate = string.Empty;
            List<dynamic> orderVerificationCodes = null;
            List<dynamic> virtualOrderItemInfos = null;
            bool isCanRefundVirtual = false;
            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
            {
                var orderItemInfo = orderitems.FirstOrDefault();
                if (orderItemInfo != null)
                {
                    virtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(orderItemInfo.ProductId);
                    if (virtualProductInfo != null)
                    {
                        validityType = virtualProductInfo.ValidityType ? 1 : 0;
                        if (validityType == 1)
                        {
                            startDate = virtualProductInfo.StartDate.Value.ToString("yyyy-MM-dd");
                            endDate = virtualProductInfo.EndDate.Value.ToString("yyyy-MM-dd");
                        }
                    }
                    var codes = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { order.Id });
                    orderVerificationCodes = codes.Select(p =>
                    {
                        return new
                        {
                            VerificationCode = Regex.Replace(p.VerificationCode, @"(\d{4})", "$1 "),
                            Status = p.Status,
                            StatusText = p.Status.ToDescription(),
                            QRCode = GetQRCode(p.VerificationCode)
                        };
                    }).ToList<dynamic>();

                    var virtualItems = OrderApplication.GetVirtualOrderItemInfosByOrderId(order.Id);
                    virtualOrderItemInfos = virtualItems.Select(p =>
                    {
                        return new
                        {
                            VirtualProductItemName = p.VirtualProductItemName,
                            Content = ReplaceImage(p.Content, p.VirtualProductItemType),
                            VirtualProductItemType = p.VirtualProductItemType
                        };
                    }).ToList<dynamic>();
                }
            }
            if (order.OrderStatus == Himall.Entities.OrderInfo.OrderOperateStatus.WaitVerification)
            {
                if (virtualProductInfo != null)
                {
                    if (virtualProductInfo.SupportRefundType == 2)
                    {
                        isCanRefundVirtual = true;
                    }
                    else if (virtualProductInfo.SupportRefundType == 1)
                    {
                        if (virtualProductInfo.EndDate.Value > DateTime.Now)
                        {
                            isCanRefundVirtual = true;
                        }
                    }
                    else if (virtualProductInfo.SupportRefundType == 3)
                    {
                        isCanRefundVirtual = false;
                    }

                    if (isCanRefundVirtual)
                    {
                        long num = orderVerificationCodes.Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification).Count();
                        if (num > 0)
                        {
                            isCanRefundVirtual = true;
                        }
                        else
                        {
                            isCanRefundVirtual = false;
                        }
                    }
                }
            }
            #endregion
            #region 虚拟订单核销地址信息
            string shipperAddress = string.Empty, shipperTelPhone = string.Empty;
            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
            {
                if (order.ShopBranchId > 0 && branchInfo != null)
                {
                    shipperAddress = RegionApplication.GetFullName(branchInfo.AddressId) + " " + branchInfo.AddressDetail;
                    shipperTelPhone = branchInfo.ContactPhone;
                }
                else
                {
                    var verificationShipper = ShopShippersApplication.GetDefaultVerificationShipper(order.ShopId);
                    if (verificationShipper != null)
                    {
                        shipperAddress = RegionApplication.GetFullName(verificationShipper.RegionId) + " " + verificationShipper.Address;
                        shipperTelPhone = verificationShipper.TelPhone;
                    }
                }
            }
            #endregion
            var bonusmodel = ServiceProvider.Instance<ShopBonusService>.Create.GetGrantByUserOrder(orderId, CurrentUser.Id);
            bool hasBonus = bonusmodel != null? true : false;
            string shareHref = "";
            string shareTitle = "";
            string shareDetail = "";
            string shareImg = "";
            long bonusmodeId = 0;
            if (hasBonus)
            {
                bonusmodeId = bonusmodel.Id;
                shareHref = "/m-weixin/ShopBonus/Index/" + bonusmodeId;
                var bonus = ShopBonusApplication.GetBonus(bonusmodel.ShopBonusId);
                shareTitle = bonus.ShareTitle;
                shareDetail = bonus.ShareDetail;
                shareImg = HimallIO.GetRomoteImagePath(bonus.ShareImg);
            }
            var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(order.ShopId);
            var orderModel = new
            {
                OrderId = order.Id,
                Status = (int)order.OrderStatus,
                StatusText = order.OrderStatus.ToDescription(),
                EnabledRefundAmount = order.OrderEnabledRefundAmount,
                OrderTotal = order.OrderTotalAmount,
                CapitalAmount = order.CapitalAmount,
                OrderAmount = order.ProductTotalAmount,
                DeductionPoints = 0,
                DeductionMoney = order.IntegralDiscount,
                PlatCouponAmount = order.PlatDiscountAmount,//平台优惠劵金额
                CouponAmount = order.DiscountAmount,//优惠劵金额
                CouponName = couponName,//优惠劵名称
                RefundAmount = order.RefundTotalAmount,
                Tax = order.Tax,
                AdjustedFreight = order.Freight,
                OrderDate = order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ItemStatus = 0,
                ItemStatusText = "",
                ShipTo = order.ShipTo,
                ShipToDate = order.ShippingDate.HasValue ? order.ShippingDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                Cellphone = order.CellPhone,
                Address = order.DeliveryType == CommonModel.DeliveryType.SelfTake && branchInfo != null ? branchInfo.AddressFullName : (order.RegionFullName + " " + order.Address),
                FreightFreePromotionName = string.Empty,
                ReducedPromotionName = string.Empty,
                ReducedPromotionAmount = order.FullDiscount,
                SentTimesPointPromotionName = string.Empty,
                CanBackReturn = !string.IsNullOrWhiteSpace(order.PaymentTypeGateway),
                CanCashierReturn = false,
                PaymentType = order.PaymentType.ToDescription(),
                OrderPayAmount = order.OrderPayAmount,//订单需要第三方支付的金额
                PaymentTypeName = PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName,
                PaymentTypeDesc = order.PaymentTypeDesc,
                Remark = string.IsNullOrEmpty(order.OrderRemarks) ? "" : order.OrderRemarks,
                ModeName = order.DeliveryType.ToDescription(),
                LogisticsData = expressTrace,
                TakeCode = order.PickupCode,
                LineItems = orderDetail.OrderItems,
                IsCanRefund = isCanOrderReturn,
                //IsCanRefund = !(orderDetail.OrderItems.Any(e => e.IsCanRefund == true)) && OrderApplication.CanRefund(order, null, null),
                IsSelfTake = order.DeliveryType ==DeliveryType.SelfTake ? 1 : 0,
                BranchInfo = branchInfo,
                DeliveryType = (int)order.DeliveryType,
                OrderInvoice = OrderApplication.GetOrderInvoiceInfo(order.Id),
                ValidityType = validityType,
                StartDate = startDate,
                EndDate = endDate,
                OrderVerificationCodes = orderVerificationCodes,
                VirtualOrderItemInfos = virtualOrderItemInfos,
                IsCanRefundVirtual = isCanRefundVirtual,
                ShipperAddress = shipperAddress,
                ShipperTelPhone = shipperTelPhone,
                OrderType = order.OrderType,
                JoinStatus = fightGroupOrderInfo == null ? -2 : fightGroupOrderInfo.JoinStatus,
                BonusId= bonusmodeId,
                HasBonus = hasBonus,
                ShareHref = shareHref,
                ShareTitle = shareTitle,
                ShareDetail = shareDetail,
                ShareImg = shareImg,
                order.ShopName,
                order.ShopId,
                VshopId = vshop != null ? vshop.Id : 0
            };

            return JsonResult<dynamic>(orderModel);
        }

        /// <summary>
        /// 获取订单列表
        /// </summary>
        /// <param name="status"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetOrders(int? status,string keywords, int pageIndex, int pageSize = 8)
        {
            CheckUserLogin();
            var orderService = ServiceProvider.Instance<OrderService>.Create;
            if (status.HasValue && status == 0)
            {
                status = null;
            }
            var queryModel = new OrderQuery()
            {
                Status = (OrderInfo.OrderOperateStatus?)status,
                UserId = CurrentUser.Id,
                PageSize = pageSize,
                PageNo = pageIndex,
                SearchKeyWords=keywords,
                IsFront = true
            };
            if (queryModel.Status.HasValue && queryModel.Status.Value == Entities.OrderInfo.OrderOperateStatus.WaitReceiving)
            {
                if (queryModel.MoreStatus == null)
                {
                    queryModel.MoreStatus = new List<Entities.OrderInfo.OrderOperateStatus>() { };
                }
                queryModel.MoreStatus.Add(Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp);
            }
            if (status.GetValueOrDefault() == (int)OrderInfo.OrderOperateStatus.Finish)
                queryModel.Commented = false;//只查询未评价的订单

            var orders = orderService.GetOrders<OrderInfo>(queryModel);
            var productService = ServiceProvider.Instance<ProductService>.Create;
            var vshopService = ServiceProvider.Instance<VShopService>.Create;
            var orderRefundService = ServiceProvider.Instance<RefundService>.Create;
            var orderItems = OrderApplication.GetOrderItemsByOrderId(orders.Models.Select(p => p.Id));
            var orderRefunds = OrderApplication.GetOrderRefunds(orderItems.Select(p => p.Id));
            var shopBranchs = ShopBranchApplication.GetStores(orders.Models.Where(a => a.ShopBranchId > 0).Select(p => p.ShopBranchId).ToList());

            var proids = orderItems.Select(p => p.ProductId).Distinct();
            var orderitemproducts = productService.GetAllProductByIds(proids);

            var result = orders.Models.Select(item =>
            {
                var orderitems = orderItems.Where(p => p.OrderId == item.Id);
                var shopBranchInfo = shopBranchs.FirstOrDefault(a => a.Id == item.ShopBranchId);//当前订单所属门店信息
                if (item.OrderStatus >= Entities.OrderInfo.OrderOperateStatus.WaitDelivery)
                {
                    orderService.CalculateOrderItemRefund(item.Id);
                }
                var vshop = vshopService.GetVShopByShopId(item.ShopId);
                var _ordrefobj = orderRefundService.GetOrderRefundByOrderId(item.Id) ?? new Entities.OrderRefundInfo { Id = 0 };
                if (item.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitDelivery && item.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                {
                    _ordrefobj = new Entities.OrderRefundInfo { Id = 0 };
                }
                int? ordrefstate = (_ordrefobj == null ? null : (int?)_ordrefobj.SellerAuditStatus);
                ordrefstate = (ordrefstate > 4 ? (int?)_ordrefobj.ManagerConfirmStatus : ordrefstate);
                //参照PC端会员中心的状态描述信息
                string statusText = item.OrderStatus.ToDescription();
                if (item.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitDelivery || item.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                {
                    if (ordrefstate.HasValue && ordrefstate != 0 && ordrefstate != 4)
                    {
                        statusText = "退款中";
                    }
                }
                //是否可售后
                bool IsShowReturn = OrderApplication.CanRefund(item, ordrefstate, null);
                var hasAppendComment = ServiceProvider.Instance<CommentService>.Create.HasAppendComment(orderitems.FirstOrDefault().Id);
                return new
                {
                    OrderId = item.Id,
                    StatusText = statusText,
                    Status = item.OrderStatus,
                    orderType = item.OrderType,
                    orderTypeName = item.OrderType.ToDescription(),
                    shopname = item.ShopName,
                    vshopId = vshop == null ? 0 : vshop.Id,
                    Amount = item.OrderTotalAmount.ToString("F2"),
                    Quantity = OrderApplication.GetOrderTotalProductCount(item.Id),
                    commentCount = OrderApplication.GetOrderCommentCount(item.Id),
                    pickupCode = item.PickupCode,
                    EnabledRefundAmount = item.OrderEnabledRefundAmount,
                    LineItems = orderitems.Select(a =>
                    {
                        var prodata = orderitemproducts == null ? null : orderitemproducts.Where(p => p.Id == a.ProductId).FirstOrDefault();
                        Entities.TypeInfo typeInfo = prodata == null ? null : ServiceProvider.Instance<TypeService>.Create.GetType(prodata.TypeId);
                        string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                        string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                        string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                        var itemStatusText = "";
                        var itemrefund = orderRefunds.Where(or => or.OrderItemId == a.Id).FirstOrDefault(d => d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                        int? itemrefstate = (itemrefund == null ? 0 : (int?)itemrefund.SellerAuditStatus);
                        itemrefstate = (itemrefstate > 4 ? (int?)itemrefund.ManagerConfirmStatus : itemrefstate);
                        if (itemrefund != null)
                        {//默认为商家处理进度
                            if (itemrefstate == 4)
                            {//商家拒绝
                                itemStatusText = "";
                            }
                            else
                            {
                                itemStatusText = "售后处理中";
                            }
                        }
                        if (itemrefstate > 4)
                        {//如果商家已经处理完，则显示平台处理进度
                            if (itemrefstate == 7)
                            {
                                itemStatusText = "退款成功";
                            }
                        }
                        var picimg = Core.HimallIO.GetRomoteProductSizeImage(a.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_350);
                        if (!string.IsNullOrEmpty(a.ThumbnailsUrl) && a.ThumbnailsUrl.Contains("skus")) 
                            picimg = HimallIO.GetRomoteImagePath(a.ThumbnailsUrl);
                        
                        return new
                        {
                            Status = itemrefstate,
                            StatusText = itemStatusText,
                            Id = a.SkuId,
                            productId = a.ProductId,
                            Name = a.ProductName,
                            Image = picimg,
                            Amount = a.Quantity,
                            Price = a.SalePrice,
                            Unit = prodata == null ? "" : prodata.MeasureUnit,
                            SkuText = colorAlias + ":" + a.Color + " " + sizeAlias + ":" + a.Size + " " + versionAlias + ":" + a.Version,
                            color = a.Color,
                            size = a.Size,
                            version = a.Version,
                            ColorAlias = (prodata != null && !string.IsNullOrWhiteSpace(prodata.ColorAlias)) ? prodata.ColorAlias : colorAlias,//如果商品有自定义规格名称则用
                            SizeAlias = (prodata != null && !string.IsNullOrWhiteSpace(prodata.SizeAlias)) ? prodata.SizeAlias : sizeAlias,
                            VersionAlias = (prodata != null && !string.IsNullOrWhiteSpace(prodata.VersionAlias)) ? prodata.VersionAlias : versionAlias,
                            RefundStats = itemrefstate,
                            OrderRefundId = (itemrefund == null ? 0 : itemrefund.Id),
                            EnabledRefundAmount = a.EnabledRefundAmount,
                            IsShowRefund = IsShowReturn,
                            IsShowAfterSale = IsShowReturn
                        };
                    }),
                    RefundStats = ordrefstate,
                    OrderRefundId = _ordrefobj.Id,
                    IsShowLogistics = !string.IsNullOrWhiteSpace(item.ShipOrderNumber) || item.DeliveryType == DeliveryType.ShopStore,
                    ShipOrderNumber = item.ShipOrderNumber,
                    IsShowCreview = (item.OrderStatus == Entities.OrderInfo.OrderOperateStatus.Finish && !hasAppendComment),
                    IsShowPreview = false,
                    //Invoice = item.InvoiceType.ToDescription(),
                    //InvoiceValue = (int)item.InvoiceType,
                    //InvoiceContext = item.InvoiceContext,
                    //InvoiceTitle = item.InvoiceTitle,
                    PaymentType = item.PaymentType.ToDescription(),
                    PaymentTypeValue = (int)item.PaymentType,
                    IsShowClose = (item.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitPay),
                    IsShowFinishOrder = (item.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitReceiving),
                    IsShowRefund = IsShowReturn,
                    IsShowReturn = IsShowReturn,
                    IsShowTakeCodeQRCode = (!string.IsNullOrWhiteSpace(item.PickupCode) && item.OrderStatus != Entities.OrderInfo.OrderOperateStatus.Finish && item.OrderStatus != Entities.OrderInfo.OrderOperateStatus.Close),
                    OrderDate = item.OrderDate,
                    SupplierId = 0,
                    ShipperName = string.Empty,
                    StoreName = shopBranchInfo != null ? shopBranchInfo.ShopBranchName : string.Empty,
                    IsShowCertification = false,
                    HasAppendComment = hasAppendComment,
                    CreviewText = OrderApplication.GetOrderCommentCount(item.Id) > 0 ? "追加评论" : "评价订单",
                    ProductCommentPoint = 0,
                    DeliveryType = (int)item.DeliveryType
                };
            });
            var statistic = StatisticApplication.GetMemberOrderStatistic(CurrentUser.Id);
            return JsonResult<dynamic>(
                new
                {
                    AllOrderCounts = statistic.OrderCount,
                    WaitingForComments = statistic.WaitingForComments,
                    WaitingForRecieve = statistic.WaitingForRecieve + statistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(CurrentUser.Id),
                    WaitingForPay = statistic.WaitingForPay,
                    Data = result
                });
        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="openId">openId</param>
        /// <param name="orderId">订单编号</param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetCloseOrder(string openId, string orderId)
        {
            CheckUserLogin();
            long order_Id = long.Parse(orderId);
            var order = ServiceProvider.Instance<OrderService>.Create.GetOrder(order_Id, CurrentUser.Id);
            if (order != null)
            {
                //拼团处理
                if (order.OrderType == Entities.OrderInfo.OrderTypes.FightGroup)
                {
                    return Json(ErrorResult<int>("拼团订单，会员不能取消！"));
                }
                ServiceProvider.Instance<OrderService>.Create.MemberCloseOrder(order_Id, CurrentUser.UserName);
            }
            else
            {
                return Json(ErrorResult<int>("取消失败，该订单已删除或者不属于当前用户！"));
            }
            return JsonResult<int>(msg: "操作成功！");
        }

        /// <summary>
        /// 确认收货
        /// </summary>
        /// <param name="openId">openId</param>
        /// <param name="orderId">订单编号</param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetConfirmOrder(string openId, string orderId)
        {
            CheckUserLogin();
            long order_Id = long.Parse(orderId);
            ServiceProvider.Instance<OrderService>.Create.MembeConfirmOrder(order_Id, CurrentUser.UserName);
            // var data = ServiceProvider.Instance<OrderService>.Create.GetOrder(orderId);
            //确认收货写入结算表(修改LH的方法)
            // ServiceProvider.Instance<OrderService>.Create.WritePendingSettlnment(data);
            return JsonResult<int>(msg: "操作成功！");
        }

        /// 订单提货码【门店】
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        //public JsonResult<Result<OrderListModel>> GetPickupGoods(long id)
        //{
        //    CheckUserLogin();
        //    var orderInfo = OrderApplication.GetOrder(id);
        //    if (orderInfo == null)
        //        return Json(ErrorResult<OrderListModel>("订单不存在！"));
        //    if (orderInfo.UserId != CurrentUser.Id)
        //        return Json(ErrorResult<OrderListModel>("只能查看自己的提货码！"));
        //    //var productService = ServiceProvider.Instance<ProductService>.Create;
        //    AutoMapper.Mapper.CreateMap<Order, Himall.DTO.OrderListModel>();
        //    AutoMapper.Mapper.CreateMap<DTO.OrderItem, OrderItemListModel>();
        //    var orderModel = AutoMapper.Mapper.Map<Order, Himall.DTO.OrderListModel>(orderInfo);
        //    var orderItems = OrderApplication.GetOrderItemsByOrderId(orderInfo.Id);
        //    var newOrderItems = new List<DTO.OrderItem>();
        //    foreach (var item in orderItems)
        //    {
        //        item.ThumbnailsUrl = Core.HimallIO.GetRomoteProductSizeImage(ProductManagerApplication.GetProduct(item.ProductId).RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_50);
        //        newOrderItems.Add(item);
        //    }
        //    orderModel.OrderItemList = AutoMapper.Mapper.Map<List<DTO.OrderItem>, List<OrderItemListModel>>(newOrderItems);
        //    if (orderInfo.ShopBranchId > 0)
        //    {//补充数据
        //        var branch = ShopBranchApplication.GetShopBranchById(orderInfo.ShopBranchId);
        //        orderModel.ShopBranchName = branch.ShopBranchName;
        //        orderModel.ShopBranchAddress = branch.AddressFullName;
        //        orderModel.ShopBranchContactPhone = branch.ContactPhone;
        //    }

        //    return JsonResult(orderModel);
        //}

        /// <summary>
        /// 获取订单红包【门店】
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public JsonResult<Result<List<BonuModel>>> GetOrderBonus(string orderIds)
        {
            CheckUserLogin();
            List<BonuModel> bonus = new List<BonuModel>();
            string orderids = orderIds;
            string[] orderArray = orderids.Split(',');
            foreach (string item in orderArray)
            {
                long orderid = 0;
                if (long.TryParse(item, out orderid))
                {
                    var BonusInfo = ShopBonusApplication.GetGrantByUserOrder(orderid, CurrentUser.Id);
                    if (BonusInfo != null)
                    {
                        BonuModel bonuObject = new BonuModel();
                        var info = ShopBonusApplication.GetBonus(BonusInfo.ShopBonusId);
                        bonuObject.ShareHref = CurrentUrlHelper.CurrentUrlNoPort() + "/m-weixin/shopbonus/index/" + BonusInfo.Id;
                        bonuObject.ShareCount = info.Count;
                        bonuObject.ShareDetail = info.ShareDetail;
                        bonuObject.ShareTitle = info.ShareTitle;
                        bonuObject.ShopName = ShopApplication.GetShop(info.ShopId).ShopName;
                        bonus.Add(bonuObject);
                    }
                }
            }
            return JsonResult(bonus);
        }

        /// <summary>
        /// 获取订单状态【门店】
        /// <para>供支付时使用</para>
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        //public JsonResult<Result<IEnumerable<MemberOrderGetStatusModel>>> GetOrerStatus(string orderIds)
        //{
        //    CheckUserLogin();
        //    List<long> ordids = orderIds.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).Select(t => long.Parse(t)).ToList();
        //    IEnumerable<Order> orders = OrderApplication.GetOrders(ordids).ToList();
        //    var data = orders.Select(d =>
        //    {
        //        long activeId = 0, groupId = 0;
        //        if (d.OrderType == Entities.OrderInfo.OrderTypes.FightGroup)
        //        {
        //            var fg = FightGroupApplication.GetFightGroupOrderStatusByOrderId(d.Id);
        //            if (fg != null)
        //            {
        //                activeId = fg.ActiveId;
        //                groupId = fg.GroupId;
        //            }
        //        }
        //        return new MemberOrderGetStatusModel
        //        {
        //            orderId = d.Id,
        //            status = d.OrderStatus.GetHashCode(),
        //            activeId = activeId,
        //            groupId = groupId

        //        };
        //    });
        //    return JsonResult(data);
        //}

        /// <summary>
        /// 获取订单物流信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetExpressInfo(long orderId)
        {
            CheckUserLogin();
            var orderService = ServiceProvider.Instance<OrderService>.Create;
            Entities.OrderInfo order = orderService.GetOrder(orderId, CurrentUser.Id);
            //订单信息是否正常
            if (order == null)
            {
                return Json(ErrorResult<dynamic>("订单号不存在"));
            }
            List<object> TracesList = new List<object>();
            //取订单物流信息
            if (!string.IsNullOrWhiteSpace(order.ShipOrderNumber))
            {
                var expressData = ServiceProvider.Instance<ExpressService>.Create.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber,order.Id.ToString(), order.CellPhone);
                if (expressData.Success)
                {
                    expressData.ExpressDataItems = expressData.ExpressDataItems.OrderByDescending(item => item.Time);//按时间逆序排列
                    foreach (var item in expressData.ExpressDataItems)
                    {
                        var traces = new
                        {
                            acceptStation = item.Content,
                            acceptTime = item.Time.ToString("yyyy-MM-dd HH:mm:ss")
                        };
                        TracesList.Add(traces);
                    }
                }
            }

            var data = new
            {
                LogisticsData = new
                {
                    success = TracesList.Count > 0,
                    traces = TracesList
                },
                ExpressCompanyName = order.DeliveryType == DeliveryType.ShopStore ? "门店店员配送" : order.ExpressCompanyName,
                ShipOrderNumber = order.DeliveryType == DeliveryType.ShopStore ? "" : order.ShipOrderNumber,
                ShipTo = order.ShipTo,
                CellPhone = order.CellPhone,
                Address = order.RegionFullName + order.Address
            };
            return JsonResult<dynamic>(data: data);
        }

        /// <summary>
        /// 获取提货码二维码
        /// </summary>
        /// <param name="pickupCode"></param>
        /// <returns></returns>
        public JsonResult<Result<string>> GetPickupCodeQRCode(string pickupCode)
        {
            string result = "";
            if (!string.IsNullOrWhiteSpace(pickupCode))
            {
                var qrcode = QRCodeHelper.Create(pickupCode);

                Bitmap bmp = new Bitmap(qrcode);
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();
                qrcode.Dispose();
                result = Convert.ToBase64String(arr);
                result = "data:image/png;base64," + result;
            }
            return JsonResult(result);
        }

        /// <summary>
        /// 获取订单评价商品信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        //public JsonResult<Result<dynamic>> GetOrderCommentProduct(long orderId)
        //{
        //    CheckUserLogin();
        //    var order = ServiceProvider.Instance<OrderService>.Create.GetOrder(orderId);
        //    var comment = OrderApplication.GetOrderCommentCount(order.Id);
        //    if (order != null && comment == 0)
        //    {
        //        var model = ServiceProvider.Instance<CommentService>.Create.GetProductEvaluationByOrderId(orderId, CurrentUser.Id).Select(item => new
        //        {
        //            ProductId = item.ProductId,
        //            ProductName = item.ProductName,
        //            SkuContent = item.ColorAlias + ":" + item.Color + ";" + item.SizeAlias + ":" + item.Size + ";" + item.VersionAlias + ":" + item.Version,
        //            Price = item.Price,
        //            SkuId = item.SkuId,
        //            Image = Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_220) //商城App评论时获取商品图片
        //        });

        //        var orderEvaluation = ServiceProvider.Instance<TradeCommentService>.Create.GetOrderCommentInfo(orderId, CurrentUser.Id);
        //        return JsonResult<dynamic>(model);
        //    }
        //    else
        //        return Json(ErrorResult<dynamic>("该订单不存在或者已评论过"));
        //}

        /// <summary>
        /// 获取提货码二维码
        /// </summary>
        /// <param name="pickupCode"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetElectronicCredentials(long orderId)
        {
            bool validityType = false;
            string validityDate = string.Empty, validityDateStart = string.Empty;
            int total = 0;
            List<OrderVerificationCodeInfo> orderVerificationCodes = null;
            var orderInfo = OrderApplication.GetOrder(orderId);
            if (orderInfo != null && orderInfo.OrderType == OrderInfo.OrderTypes.Virtual)
            {
                var orderItemInfo = OrderApplication.GetOrderItemsByOrderId(orderId).FirstOrDefault();
                if (orderItemInfo != null)
                {
                    var virtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(orderItemInfo.ProductId);
                    if (virtualProductInfo != null)
                    {
                        validityType = virtualProductInfo.ValidityType;
                        if (validityType)
                        {
                            validityDate = virtualProductInfo.EndDate.Value.ToString("yyyy-MM-dd");
                            validityDateStart = virtualProductInfo.StartDate.Value.ToString("yyyy-MM-dd");
                        }
                    }
                    orderVerificationCodes = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { orderId });
                    total = orderVerificationCodes.Count;
                    orderVerificationCodes.ForEach(a =>
                    {
                        a.QRCode = GetQRCode(a.VerificationCode);
                        a.VerificationCode = System.Text.RegularExpressions.Regex.Replace(a.VerificationCode, @"(\d{4})", "$1 ");
                    });
                }
            }

            return JsonResult<dynamic>(new
            {
                validityType = validityType,
                validityDate = validityDate,
                validityDateStart = validityDateStart,
                total = total,
                orderVerificationCodes = orderVerificationCodes.Select(a =>
                {
                    return new
                    {
                        QRCode = a.QRCode,
                        VerificationCode = a.VerificationCode,
                        Status = a.Status,
                        StatusText = a.Status.ToDescription()
                    };
                })
            });
        }

        private string GetQRCode(string verificationCode)
        {
            //string qrCodeImagePath = string.Empty;
            //Image qrcode = Core.Helper.QRCodeHelper.Create(verificationCode);
            //string fileName = DateTime.Now.ToString("yyMMddHHmmssffffff") + ".jpg";
            //qrCodeImagePath = CurrentUrlHelper.CurrentUrl() + "/temp/" + fileName;
            //qrcode.Save(Current.Server.MapPath("~/temp/") + fileName);
            //return qrCodeImagePath;
            
            string result = "";
            if (!string.IsNullOrWhiteSpace(verificationCode))
            {
                var qrcode = QRCodeHelper.Create(verificationCode);

                Bitmap bmp = new Bitmap(qrcode);
                MemoryStream ms = new MemoryStream();
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int)ms.Length);
                ms.Close();
                qrcode.Dispose();
                result = Convert.ToBase64String(arr);
                result = "data:image/png;base64," + result;
            }
            return result;
        }

        private List<string> ReplaceImage(string content, ProductInfo.VirtualProductItemType type)
        {
            if (type != ProductInfo.VirtualProductItemType.Picture)
                return new List<string>() { content };

            List<string> list = content.Split(',').ToList();
            return list.Select(a => a = CurrentUrlHelper.CurrentUrl() + a).ToList();
        }


        /// <summary>
        /// 根据订单号获取我要晒单数据
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetShareOrderDetail(long orderId)
        {
            CheckUserLogin();
            var orderService = ServiceProvider.Instance<OrderService>.Create;
            var order = orderService.GetOrder(orderId, CurrentUser.Id);
            var orderitems = orderService.GetOrderItemsByOrderId(order.Id);
            var productService = ServiceProvider.Instance<ProductService>.Create;
            var memberIntegralService = ServiceProvider.Instance<MemberIntegralService>.Create;
          
            //订单信息是否正常
            if (order == null)
            {
                throw new HimallException("订单号不存在！");
            }
            var IntegralRule = memberIntegralService.GetIntegralRule();
            var share = IntegralRule.FirstOrDefault(a => a.TypeId == (int)MemberIntegralInfo.IntegralType.Share);
            bool hasShareIntegral = share == null ? false : share.Integral > 0;//晒单是否得积分
            
            var user = CurrentUser;
            //是否分销员
            var IsDistributorStr = string.Empty;
            if (user != null && user.Id > 0 && SiteSettingApplication.SiteSettings.DistributionIsEnable)
            {
                var prom = DistributionApplication.GetDistributor(user.Id);
                if (prom != null && prom.DistributionStatus == (int)DistributorStatus.Audited)
                {
                    IsDistributorStr = string.Format("&distributorId={0}", user.Id);
                }
            }
            var shopBranchIdStr = string.Empty;
            string shopname = "";
            #region 门店信息
            var branchInfo = new ShopBranch();
            if (order.ShopBranchId > 0)
            {
                branchInfo = ShopBranchApplication.GetShopBranchById(order.ShopBranchId);
                shopname = branchInfo.ShopBranchName;
                shopBranchIdStr = string.Format("&shopBranchId={0}", order.ShopBranchId);
            }
            else
            {
                shopname = order.ShopName;
                branchInfo = null;
            }
            #endregion
            //获取订单商品项数据
            var orderDetail = new
            {
                OrderItems = orderitems.Select(item =>
                {
                    var skulist = productService.GetSKUs(item.ProductId);
                    var relativePath = productService.GetProduct(item.ProductId).RelativePath;
                    var shareHref = string.Format("/page/productdetail/productdetail?id={0}{1}{2}", item.ProductId, IsDistributorStr, shopBranchIdStr);
                    return new
                    {
                        Id = item.ProductId,
                        Name = item.ProductName,
                        Price = skulist.Count > 0 ? skulist.Min(s => s.SalePrice) : item.SalePrice,//所有SKU中最低价
                        Image = Core.HimallIO.GetRomoteProductSizeImage(relativePath, 1, (int)Himall.CommonModel.ImageSize.Size_350),
                        ShareHref = shareHref,
                        ShareTitle = @"我买了一些好东西，分享给你看看",
                        ShareImg = Core.HimallIO.GetRomoteProductSizeImage(relativePath, 1, (int)Himall.CommonModel.ImageSize.Size_150),
                    };
                })
            };
            
            var orderModel = new
            {
                OrderId = order.Id,
                LineItems = orderDetail.OrderItems,
                //BranchInfo = branchInfo,
                hasShareIntegral = hasShareIntegral,
                ShopName = shopname,
                ShopId = order.ShopId,
            };

            return JsonResult<dynamic>(orderModel);
        }
    }
}
