using Himall.API.Model;
using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.CommonModel;
using Himall.CommonModel.Enum;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Entities;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using System.Drawing;
using System.Web;
using System.Text.RegularExpressions;

namespace Himall.API
{
    public class MemberOrderController : BaseApiController
    {
        public object GetOrders(int? orderStatus, int pageNo, int pageSize = 8)
        {
            CheckUserLogin();

            var orderStatistic = StatisticApplication.GetMemberOrderStatistic(CurrentUser.Id);

            var orderService = ServiceProvider.Instance<OrderService>.Create;
            if (orderStatus.HasValue && orderStatus == 0)
                orderStatus = null;

            var queryModel = new OrderQuery()
            {
                Status = (OrderInfo.OrderOperateStatus?)orderStatus,
                UserId = CurrentUser.Id,
                PageSize = pageSize,
                PageNo = pageNo,
                IsFront = true
            };
            if (queryModel.Status.HasValue && queryModel.Status.Value == OrderInfo.OrderOperateStatus.WaitReceiving)
            {
                if (queryModel.MoreStatus == null)
                {
                    queryModel.MoreStatus = new List<OrderInfo.OrderOperateStatus>() { };
                }
                queryModel.MoreStatus.Add(OrderInfo.OrderOperateStatus.WaitSelfPickUp);
            }
            if (orderStatus.GetValueOrDefault() == (int)OrderInfo.OrderOperateStatus.Finish)
                queryModel.Commented = false;//只查询未评价的订单
            QueryPageModel<OrderInfo> orders = orderService.GetOrders<OrderInfo>(queryModel);
            var productService = ServiceProvider.Instance<ProductService>.Create;
            var vshopService = ServiceProvider.Instance<VShopService>.Create;
            var orderRefundService = ServiceProvider.Instance<RefundService>.Create;
            var orderItems = OrderApplication.GetOrderItemsByOrderId(orders.Models.Select(p => p.Id));
            var orderRefunds = OrderApplication.GetOrderRefunds(orderItems.Select(p => p.Id));
            //查询结果的门店ID
            var branchIds = orders.Models.Where(e => e.ShopBranchId > 0).Select(p => p.ShopBranchId).ToList();
            //根据门店ID获取门店信息
            var shopBranchs = ShopBranchApplication.GetStores(branchIds);

            //TODO:FG 关联数据查询提取至循环外层
            var result = orders.Models.Select(order =>
            {
                if (order.OrderStatus >= OrderInfo.OrderOperateStatus.WaitDelivery)
                {
                    orderService.CalculateOrderItemRefund(order.Id);
                }
                var vshop = vshopService.GetVShopByShopId(order.ShopId);
                var _ordrefobj = orderRefundService.GetOrderRefundByOrderId(order.Id) ?? new OrderRefundInfo { Id = 0 };
                if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                {
                    _ordrefobj = new OrderRefundInfo { Id = 0 };
                }
                int? ordrefstate = (_ordrefobj == null ? null : (int?)_ordrefobj.SellerAuditStatus);
                ordrefstate = (ordrefstate > 4 ? (int?)_ordrefobj.ManagerConfirmStatus : ordrefstate);
                var branchObj = shopBranchs.FirstOrDefault(e => e.Id == order.ShopBranchId);
                string branchName = branchObj == null ? string.Empty : branchObj.ShopBranchName;
                //参照PC端会员中心的状态描述信息
                string statusText = order.OrderStatus.ToDescription();
                if (order.OrderStatus == OrderInfo.OrderOperateStatus.WaitDelivery || order.OrderStatus == OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                {
                    if (ordrefstate.HasValue && ordrefstate != 0 && ordrefstate != 4)
                    {
                        statusText = "退款中";
                    }
                }
                var hasAppendComment = ServiceProvider.Instance<CommentService>.Create.HasAppendComment(orderItems.FirstOrDefault(d=>d.OrderId==order.Id).Id);
                return new
                {
                    id = order.Id,
                    status = statusText,
                    orderStatus = order.OrderStatus,
                    orderType = order.OrderType,
                    orderTypeName = order.OrderType.ToDescription(),
                    shopname = order.ShopName,
                    vshopId = vshop == null ? 0 : vshop.Id,
                    orderTotalAmount = order.OrderTotalAmount.ToString("F2"),
                    productCount = OrderApplication.GetOrderTotalProductCount(order.Id),
                    commentCount = OrderApplication.GetOrderComment(order.Id).Count,
                    pickupCode = order.PickupCode,
                    ShopBranchId = order.ShopBranchId,
                    ShopBranchName = branchName,
                    EnabledRefundAmount = order.OrderEnabledRefundAmount,
                    itemInfo = orderItems.Where(oi => oi.OrderId == order.Id).Select(a =>
                    {
                        var prodata = productService.GetProduct(a.ProductId);
                        TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetType(prodata.TypeId);
                        string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                        string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                        string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                        if (prodata != null)
                        {
                            colorAlias = !string.IsNullOrWhiteSpace(prodata.ColorAlias) ? prodata.ColorAlias : colorAlias;
                            sizeAlias = !string.IsNullOrWhiteSpace(prodata.SizeAlias) ? prodata.SizeAlias : sizeAlias;
                            versionAlias = !string.IsNullOrWhiteSpace(prodata.VersionAlias) ? prodata.VersionAlias : versionAlias;
                        }
                        var itemrefund = orderRefunds.Where(or => or.OrderItemId == a.Id).FirstOrDefault(d => d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                        int? itemrefstate = (itemrefund == null ? null : (int?)itemrefund.SellerAuditStatus);
                        itemrefstate = (itemrefstate > 4 ? (int?)itemrefund.ManagerConfirmStatus : itemrefstate);
                        string picimg = HimallIO.GetRomoteProductSizeImage(a.ThumbnailsUrl, 1, (int)ImageSize.Size_350);
                        if (a.ThumbnailsUrl.Contains("skus"))
                        {
                            picimg = HimallIO.GetRomoteImagePath(a.ThumbnailsUrl);
                        }
                        return new
                        {
                            productId = a.ProductId,
                            productName = a.ProductName,
                            image = picimg,
                            count = a.Quantity,
                            price = a.SalePrice,
                            Unit = prodata == null ? "" : prodata.MeasureUnit,
                            color = a.Color,
                            size = a.Size,
                            version = a.Version,
                            ColorAlias = colorAlias,
                            SizeAlias = sizeAlias,
                            VersionAlias = versionAlias,
                            RefundStats = itemrefstate,
                            OrderRefundId = (itemrefund == null ? 0 : itemrefund.Id),
                            EnabledRefundAmount = a.EnabledRefundAmount
                        };
                    }),
                    RefundStats = ordrefstate,
                    OrderRefundId = _ordrefobj.Id,
                    HasExpressStatus = !string.IsNullOrWhiteSpace(order.ShipOrderNumber),
                    HasAppendComment = hasAppendComment,
                    //Invoice = order.InvoiceType.ToDescription(),
                    //InvoiceValue = (int)order.InvoiceType,
                    //InvoiceContext = order.InvoiceContext,
                    //InvoiceTitle = order.InvoiceTitle,
                    PaymentType = order.PaymentType.ToDescription(),
                    PaymentTypeValue = (int)order.PaymentType,
                    CanRefund = OrderApplication.CanRefund(order, itemId: 0),
                    IsVirtual = order.OrderType == OrderInfo.OrderTypes.Virtual ? 1 : 0,
                    IsPay = order.PayDate.HasValue ? 1 : 0
                };
            });
            return new
            {
                success = true,
                AllOrderCounts = orderStatistic.OrderCount,
                WaitingForComments = orderStatistic.WaitingForComments,
                WaitingForRecieve = orderStatistic.WaitingForRecieve + orderStatistic.WaitingForSelfPickUp + OrderApplication.GetWaitConsumptionOrderNumByUserId(CurrentUser.Id),
                WaitingForPay = orderStatistic.WaitingForPay,
                Orders = result
            };
        }

        public object GetElectronicCredentials(long orderId)
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

            return new
            {
                success = true,
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
            };
        }

        private string GetQRCode(string verificationCode)
        {
            string qrCodeImagePath = string.Empty;
            Image qrcode = Core.Helper.QRCodeHelper.Create(verificationCode);
            string fileName = DateTime.Now.ToString("yyMMddHHmmssffffff") + ".jpg";
            qrCodeImagePath = CurrentUrlHelper.CurrentUrl() + "/temp/" + fileName;
            qrcode.Save(HttpContext.Current.Server.MapPath("~/temp/") + fileName);
            return qrCodeImagePath;
        }

        public object GetOrderDetail(long id)
        {
            CheckUserLogin();
            OrderInfo order = ServiceProvider.Instance<OrderService>.Create.GetOrder(id, CurrentUser.Id);

            var orderService = ServiceProvider.Instance<OrderService>.Create;
            var bonusService = ServiceProvider.Instance<ShopBonusService>.Create;
            var orderRefundService = ServiceProvider.Instance<RefundService>.Create;
            var bonusmodel = bonusService.GetGrantByUserOrder(id, CurrentUser.Id);
            bool hasBonus = bonusmodel != null ? true : false;
            string shareHref = "";
            string shareTitle = "";
            string shareDetail = "";
            string shareImg = "";
            if (hasBonus)
            {
                shareHref = CurrentUrlHelper.CurrentUrlNoPort() + "/m-weixin/shopbonus/index/" + bonusService.GetGrantIdByOrderId(id);
                var bonus = ShopBonusApplication.GetBonus(bonusmodel.ShopBonusId);
                shareTitle = bonus.ShareTitle;
                shareDetail = bonus.ShareDetail;
                shareImg = HimallIO.GetRomoteImagePath(bonus.ShareImg);
            }
            var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(order.ShopId);

            var customerServices = CustomerServiceApplication.GetMobileCustomerServiceAndMQ(order.ShopId,true,CurrentUser,null,PlatformType.Android);

            var shop = ShopApplication.GetShop(order.ShopId);
            var orderItems = OrderApplication.GetOrderItemsByOrderId(order.Id);
            var products = ProductManagerApplication.GetProducts(orderItems.Select(p => p.ProductId));
            var refunds = OrderApplication.GetOrderRefundsByOrder(order.Id);            //获取订单商品项数据
            var orderDetail = new
            {
                ShopId = shop.Id,
                ShopName = shop.ShopName,
                OrderItems = orderItems.Select(item =>
                {
                    var product = products.FirstOrDefault(p => p.Id == item.ProductId);
                    var typeInfo = TypeApplication.GetType(product.TypeId);

                    string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    if (product != null)
                    {
                        colorAlias = !string.IsNullOrWhiteSpace(product.ColorAlias) ? product.ColorAlias : colorAlias;
                        sizeAlias = !string.IsNullOrWhiteSpace(product.SizeAlias) ? product.SizeAlias : sizeAlias;
                        versionAlias = !string.IsNullOrWhiteSpace(product.VersionAlias) ? product.VersionAlias : versionAlias;
                    }
                    var itemrefund = refunds.FirstOrDefault(d => d.OrderItemId == item.Id && d.RefundMode != OrderRefundInfo.OrderRefundMode.OrderRefund);
                    int? itemrefstate = (itemrefund == null ? 0 : (int?)itemrefund.SellerAuditStatus);
                    itemrefstate = (itemrefstate > 4 ? (int?)itemrefund.ManagerConfirmStatus : itemrefstate);
                    string itemStatusText = "";
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
                    var IsCanRefund = OrderApplication.CanRefund(order, itemrefstate, itemId: item.Id);
                    return new
                    {
                        ItemId = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Count = item.Quantity,
                        Price = item.SalePrice,
                        ProductImage=item.ThumbnailsUrl.Contains("skus") ? HimallIO.GetRomoteImagePath(item.ThumbnailsUrl) : Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_100),
                        color = item.Color,
                        size = item.Size,
                        version = item.Version,
                        IsCanRefund = IsCanRefund,
                        ColorAlias = colorAlias,
                        SizeAlias = sizeAlias,
                        VersionAlias = versionAlias,
                        EnabledRefundAmount = item.EnabledRefundAmount,
                        OrderRefundId = (itemrefund == null ? 0 : itemrefund.Id),
                        RefundStats = itemrefstate
                    };
                })
            };
            //取拼团订单状态
            var fightGroupOrderInfo = ServiceProvider.Instance<FightGroupService>.Create.GetFightGroupOrderStatusByOrderId(order.Id);
            var _ordrefobj = orderRefundService.GetOrderRefundByOrderId(order.Id) ?? new OrderRefundInfo { Id = 0 };
            if (order.OrderStatus != OrderInfo.OrderOperateStatus.WaitDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.WaitSelfPickUp)
            {
                _ordrefobj = new OrderRefundInfo { Id = 0 };
            }
            int? ordrefstate = (_ordrefobj == null ? null : (int?)_ordrefobj.SellerAuditStatus);
            ordrefstate = (ordrefstate > 4 ? (int?)_ordrefobj.ManagerConfirmStatus : ordrefstate);

            var hasAppendComment = ServiceProvider.Instance<CommentService>.Create.HasAppendComment(orderItems.FirstOrDefault().Id);
            var orderModel = new
            {
                Id = order.Id,
                OrderType = order.OrderType,
                OrderTypeName = order.OrderType.ToDescription(),
                Status = order.OrderStatus.ToDescription(),
                JoinStatus = fightGroupOrderInfo == null ? -2 : fightGroupOrderInfo.JoinStatus,
                ShipTo = order.ShipTo,
                Phone = order.CellPhone,
                Address = order.RegionFullName + " " + order.Address,
                HasExpressStatus = !string.IsNullOrWhiteSpace(order.ShipOrderNumber),
                ExpressCompanyName = order.ExpressCompanyName,
                Freight = order.Freight,
                Tax = order.Tax,
                IntegralDiscount = order.IntegralDiscount,
                RealTotalAmount = order.OrderTotalAmount,
                CapitalAmount = order.CapitalAmount,
                RefundTotalAmount = order.RefundTotalAmount,
                ProductTotalAmount = order.ProductTotalAmount,
                OrderPayAmount = order.OrderPayAmount,//订单需要第三方支付的金额
                PaymentTypeName = PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName,
                PaymentTypeDesc = order.PaymentTypeDesc,
                OrderDate = order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                ShopName = order.ShopName,
                VShopId = vshop == null ? 0 : vshop.Id,
                commentCount = OrderApplication.GetOrderCommentCount(order.Id),
                ShopId = order.ShopId,
                orderStatus = (int)order.OrderStatus,
                //Invoice = order.InvoiceType.ToDescription(),
                //InvoiceValue = (int)order.InvoiceType,
                //InvoiceContext = order.InvoiceContext,
                //InvoiceTitle = order.InvoiceTitle,
                //InvoiceCode = order.InvoiceCode,
                PaymentType = order.PaymentType.ToDescription(),
                PaymentTypeValue = (int)order.PaymentType,
                FullDiscount = order.FullDiscount,
                DiscountAmount = order.DiscountAmount,
                PlatDiscountAmount = order.PlatDiscountAmount,
                OrderRemarks = string.IsNullOrEmpty(order.OrderRemarks) ? "" : order.OrderRemarks,
                HasBonus = hasBonus,
                ShareHref = shareHref,
                ShareTitle = shareTitle,
                ShareDetail = shareDetail,
                ShareImg = shareImg,
                IsCanRefund = !(orderDetail.OrderItems.Any(e => e.IsCanRefund == true)) && OrderApplication.CanRefund(order, ordrefstate, null),
                RefundStats = ordrefstate,
                OrderRefundId = _ordrefobj.Id > 0 ? _ordrefobj.Id : 0,
                EnabledRefundAmount = order.OrderEnabledRefundAmount,
                HasAppendComment = hasAppendComment,
                SelfTake = order.DeliveryType == Himall.CommonModel.DeliveryType.SelfTake ? 1 : 0,
                OrderInvoice = OrderApplication.GetOrderInvoiceInfo(order.Id)
            };
            #region 门店配送信息
            Himall.DTO.ShopBranch storeInfo = null;
            if (order.ShopBranchId > 0)
            {
                storeInfo = ShopBranchApplication.GetShopBranchById(order.ShopBranchId);
            }
            #endregion
            #region 虚拟订单信息
            VirtualProductInfo virtualProductInfo = null;
            int validityType = 0; string startDate = string.Empty, endDate = string.Empty;
            List<dynamic> orderVerificationCodes = null;
            List<dynamic> virtualOrderItemInfos = null;
            bool isCanRefundVirtual = false;
            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
            {
                var orderItemInfo = orderItems.FirstOrDefault();
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
                if (order.ShopBranchId > 0 && storeInfo != null)
                {
                    shipperAddress = RegionApplication.GetFullName(storeInfo.AddressId) + " " + storeInfo.AddressDetail;
                    shipperTelPhone = storeInfo.ContactPhone;
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
            return new
            {
                success = true,
                Order = orderModel,
                OrderItem = orderDetail.OrderItems,
                StoreInfo = storeInfo,
                CustomerServices = customerServices,
                ValidityType = validityType,
                StartDate = startDate,
                EndDate = endDate,
                OrderVerificationCodes = orderVerificationCodes,
                VirtualOrderItemInfos = virtualOrderItemInfos,
                IsCanRefundVirtual = isCanRefundVirtual,
                ShipperAddress = shipperAddress,
                ShipperTelPhone = shipperTelPhone
            };
        }

        public object GetExpressInfo(long orderId)
        {
            CheckUserLogin();
            OrderInfo order = ServiceProvider.Instance<OrderService>.Create.GetOrder(orderId, CurrentUser.Id);
            if (order.DeliveryType == DeliveryType.CityExpress)
            {
                decimal StoreLat = 0, Storelng = 0;
                if (order == null)
                {
                    throw new HimallException("错误的订单编号");
                }
                if (order.ShopBranchId > 0)
                {
                    var sbdata = ShopBranchApplication.GetShopBranchById(order.ShopBranchId);
                    if (sbdata != null)
                    {
                        StoreLat = sbdata.Latitude;
                        Storelng = sbdata.Longitude;
                    }
                }
                else
                {
                    var shopshiper = ShopShippersApplication.GetDefaultSendGoodsShipper(order.ShopId);
                    if (shopshiper != null && shopshiper.Latitude.HasValue && shopshiper.Longitude.HasValue)
                    {
                        StoreLat = shopshiper.Latitude.Value;
                        Storelng = shopshiper.Longitude.Value;
                    }
                }
                return new
                {
                    success = true,
                    ExpressNum = order.ShipOrderNumber,
                    ExpressCompanyName = order.ExpressCompanyName,
                    deliveryType = DeliveryType.CityExpress.GetHashCode(),
                    userLat = order.ReceiveLatitude,
                    userLng = order.ReceiveLongitude,
                    storeLat = StoreLat,
                    Storelng = Storelng,
                };
            }
            else
            {
                var expressData = ServiceProvider.Instance<ExpressService>.Create.GetExpressData(order.ExpressCompanyName, order.ShipOrderNumber, order.Id.ToString(), order.CellPhone);

                if (expressData.Success)
                    expressData.ExpressDataItems = expressData.ExpressDataItems.OrderByDescending(item => item.Time);//按时间逆序排列
                var json = new
                {
                    success = expressData.Success,
                    msg = expressData.Message,
                    data = expressData.ExpressDataItems.Select(item => new
                    {
                        time = item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                        content = item.Content
                    })
                };
                return new
                {
                    success = true,
                    ExpressNum = order.ShipOrderNumber,
                    ExpressCompanyName = order.ExpressCompanyName,
                    Comment = json
                };
            }
        }

        //确认收货
        public object PostConfirmOrder(MemberOrderConfirmOrderModel value)
        {
            CheckUserLogin();
            long orderId = value.orderId;
            ServiceProvider.Instance<OrderService>.Create.MembeConfirmOrder(orderId, CurrentUser.UserName);

            var data = ServiceProvider.Instance<OrderService>.Create.GetOrder(orderId);
            if (data.PaymentType == OrderInfo.PaymentTypes.CashOnDelivery)
            {//货到付款的订单，在会员确认收货时
                MemberApplication.UpdateNetAmount(data.UserId, data.OrderTotalAmount);
                MemberApplication.IncreaseMemberOrderNumber(data.UserId);
            }
            //确认收货写入结算表(修改LH的方法)
            // ServiceProvider.Instance<OrderService>.Create.WritePendingSettlnment(data);
            return SuccessResult();
        }

        //取消订单
        public object PostCloseOrder(MemberOrderCloseOrderModel value)
        {
            CheckUserLogin();
            long orderId = value.orderId;
            var order = ServiceProvider.Instance<OrderService>.Create.GetOrder(orderId, CurrentUser.Id);
            if (order != null)
            {
                //拼团处理
                if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
                {
                    throw new HimallApiException("拼团订单，会员不能取消！");
                }
                ServiceProvider.Instance<OrderService>.Create.MemberCloseOrder(orderId, CurrentUser.UserName);
            }
            else
            {
                throw new HimallApiException("取消失败，该订单已删除或者不属于当前用户！");
            }
            return SuccessResult();
        }
        /// <summary>
        /// 订单提货码
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public object GetPickupGoods(long id)
        {
            CheckUserLogin();
            var orderInfo = OrderApplication.GetOrder(id);
            if (orderInfo == null)
                return ErrorResult("订单不存在！");
            if (orderInfo.UserId != CurrentUser.Id)
                return ErrorResult("只能查看自己的提货码！");
            var productService = ServiceProvider.Instance<ProductService>.Create;
            AutoMapper.Mapper.CreateMap<Order, Himall.DTO.OrderListModel>();
            AutoMapper.Mapper.CreateMap<DTO.OrderItem, OrderItemListModel>();
            var orderModel = AutoMapper.Mapper.Map<Order, Himall.DTO.OrderListModel>(orderInfo);
            var orderItems = OrderApplication.GetOrderItemsByOrderId(orderInfo.Id);
            var newOrderItems = new List<DTO.OrderItem>();
            foreach (var item in orderItems)
            {
                item.ThumbnailsUrl = Core.HimallIO.GetRomoteProductSizeImage(productService.GetProduct(item.ProductId).RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_50);
                // item.ThumbnailsUrl = Himall.Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_50);
                newOrderItems.Add(item);
            }
            // orderModel.OrderItemList = AutoMapper.Mapper.Map<List<DTO.OrderItem>, List<OrderItemListModel>>(orderItems);
            orderModel.OrderItemList = AutoMapper.Mapper.Map<List<DTO.OrderItem>, List<OrderItemListModel>>(newOrderItems);
            if (orderInfo.ShopBranchId> 0)
            {//补充数据
                var branch = ShopBranchApplication.GetShopBranchById(orderInfo.ShopBranchId);
                orderModel.ShopBranchName = branch.ShopBranchName;
                orderModel.ShopBranchAddress = branch.AddressFullName;
                orderModel.ShopBranchContactPhone = branch.ContactPhone;
            }

            return new { success = true, OrderModel = orderModel };
        }
        //public object PostPayOrder([FromBody]dynamic value)
        //{
        //    string id = value.id;
        //    id = DecodePaymentId(id);
        //    string errorMsg = string.Empty;

        //    try
        //    {
        //        var payment = Core.PluginsManagement.GetPlugin<IPaymentPlugin>(id);
        //        var payInfo = payment.Biz.ProcessReturn(HttpContext.Request);
        //        if (payInfo != null)
        //        {
        //            var payTime = payInfo.TradeTime;

        //            var orderid = payInfo.OrderIds.FirstOrDefault();
        //            var orderIds = ObjectContainer.Current.Resolve<OrderService>().GetOrderPay(orderid).Select(item => item.OrderId).ToList();

        //            ViewBag.OrderIds = string.Join(",", orderIds);
        //            ObjectContainer.Current.Resolve<OrderService>().PaySucceed(orderIds, id, payInfo.TradeTime.Value, payInfo.TradNo, payId: orderid);

        //            string payStateKey = CacheKeyCollection.PaymentState(string.Join(",", orderIds));//获取支付状态缓存键
        //            Cache.Insert(payStateKey, true, 15);//标记为已支付
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        errorMsg = ex.Message;
        //        Core.Log.Error("移动端同步返回出错，支持方式：" + id, ex);
        //    }
        //    ServiceProvider.Instance<OrderService>.Create.PaySucceed(orderIds, id, payInfo.TradeTime.Value, payInfo.TradNo, payId: orderid);
        //}
        private string DecodePaymentId(string paymentId)
        {
            return paymentId.Replace("-", ".");
        }

        /// <summary>
        /// 订单分享红包
        /// </summary>
        /// <param name="orderIds">订单号集合</param>
        /// <returns></returns>
        public object GetOrderBonus(string orderIds)
        {
            CheckUserLogin();
            List<BonuModel> bonus = new List<BonuModel>();
            var shopService = ServiceProvider.Instance<ShopService>.Create;
            var orderService = ServiceProvider.Instance<OrderService>.Create;
            var bonusService = ServiceProvider.Instance<ShopBonusService>.Create;
            string orderids = orderIds;
            string[] orderArray = orderids.Split(',');
            foreach (string item in orderArray)
            {
                long orderid = 0;
                if (long.TryParse(item, out orderid))
                {
                    var BonusInfo = bonusService.GetGrantByUserOrder(orderid, CurrentUser.Id);
                    if (BonusInfo != null)
                    {
                        BonuModel bonuObject = new BonuModel();
                        var info = ShopBonusApplication.GetBonus(BonusInfo.ShopBonusId);
                        bonuObject.ShareHref = CurrentUrlHelper.CurrentUrlNoPort() + "/m-weixin/shopbonus/index/" + BonusInfo.Id;
                        bonuObject.ShareCount = info.Count;
                        bonuObject.ShareDetail = info.ShareDetail;
                        bonuObject.ShareTitle = info.ShareTitle;
                        bonuObject.ShopName = shopService.GetShop(info.ShopId).ShopName;
                        bonus.Add(bonuObject);
                    }
                }
            }

            return new { success = true, List = bonus };
        }
        /// <summary>
        /// 获取订单状态
        /// <para>供支付时使用</para>
        /// </summary>
        /// <param name="orderIds"></param>
        /// <returns></returns>
        public object GetOrerStatus(string orderIds)
        {
            CheckUserLogin();
            var orderService = ServiceProvider.Instance<OrderService>.Create;
            var fgService = ServiceProvider.Instance<FightGroupService>.Create;
            List<long> ordids = orderIds.Split(',').Where(d => !string.IsNullOrWhiteSpace(d)).Select(t => long.Parse(t)).ToList();
            IEnumerable<OrderInfo> orders = orderService.GetOrders(ordids).ToList();
            var data = orders.Select(d =>
            {
                long activeId = 0, groupId = 0;
                bool isgroupsuccess = false;
                if (d.OrderType == OrderInfo.OrderTypes.FightGroup)
                {
                    var fg = fgService.GetFightGroupOrderStatusByOrderId(d.Id);
                    if (fg != null)
                    {
                        activeId = fg.ActiveId;
                        groupId = fg.GroupId;
                        isgroupsuccess = fg.GroupStatus == FightGroupBuildStatus.Success;
                    }
                }
                return new MemberOrderGetStatusModel
                {
                    orderId = d.Id,
                    status = d.OrderStatus.GetHashCode(),
                    activeId = activeId,
                    groupId = groupId,
                    groupsuccess = isgroupsuccess

                };
            }).ToList();
            return new { success = true, list = data };
        }

        private List<string> ReplaceImage(string content, ProductInfo.VirtualProductItemType type)
        {
            if (type != ProductInfo.VirtualProductItemType.Picture)
                return new List<string>() { content };

            List<string> list = content.Split(',').ToList();
            return list.Select(a => a = CurrentUrlHelper.CurrentUrl() + a).ToList();
        }
    }
}