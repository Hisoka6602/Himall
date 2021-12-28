using Himall.API.Model;
using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.CommonModel;
using Himall.CommonModel.Enum;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Entities;
using Himall.Web.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Mvc;

namespace Himall.API
{
    /// <summary>
    /// 门店订单类
    /// </summary>
    public class ShopBranchOrderController : BaseShopBranchLoginedApiController
    {
        /// <summary>
        /// 根据提货码取订单
        /// </summary>
        /// <param name="pickcode"></param>
        /// <returns></returns>
        public object GetShopBranchOrder(string pickcode)
        {
            CheckUserLogin();

            var codeInfo = OrderApplication.GetOrderVerificationCodeInfoByCode(pickcode);
            if (codeInfo != null)
            {
                var order = OrderApplication.GetOrderInfo(codeInfo.OrderId);
                if (order == null)
                    return new { success = false, msg = "该核销码无效" };

                if (order.OrderType != OrderInfo.OrderTypes.Virtual)
                    return new { success = false, msg = "核销订单无效" };

                if (order.ShopBranchId != CurrentShopBranch.Id)
                    return new { success = false, msg = "非本店核销码，请买家核对信息" };

                if (codeInfo.Status == OrderInfo.VerificationCodeStatus.AlreadyVerification)
                    return new { success = false, msg = string.Format("该核销码于{0}已核销", codeInfo.VerificationTime.Value.ToString("yyyy-MM-dd HH:mm")) };

                if (codeInfo.Status == OrderInfo.VerificationCodeStatus.Expired)
                    return new { success = false, msg = "此核销码已过期，无法核销" };

                if (codeInfo.Status == OrderInfo.VerificationCodeStatus.Refund)
                    return new { success = false, msg = "此核销码正处于退款中，无法核销" };

                if (codeInfo.Status == OrderInfo.VerificationCodeStatus.RefundComplete)
                    return new { success = false, msg = "此核销码已经退款成功，无法核销" };

                var orderItem = Application.OrderApplication.GetOrderItemsByOrderId(order.Id);
                var orderItemInfo = orderItem.FirstOrDefault();
                var virtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(orderItemInfo.ProductId);
                if (virtualProductInfo != null && virtualProductInfo.ValidityType && DateTime.Now < virtualProductInfo.StartDate.Value)
                {
                    return new { success = false, msg = "该核销码暂时不能核销，请留意生效时间！" };
                }
                if (orderItemInfo.EffectiveDate.HasValue)
                {
                    if (DateTime.Now < orderItemInfo.EffectiveDate.Value)
                        return new { success = false, msg = "该核销码暂时不能核销，请留意生效时间！" };
                }

                foreach (var item in orderItem)
                {
                    var picimg = Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_100);
                    if (item.ThumbnailsUrl.Contains("skus"))
                    {
                        picimg = HimallIO.GetRomoteImagePath(item.ThumbnailsUrl);
                    }

                    item.ThumbnailsUrl = picimg;
                    Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetTypeByProductId(item.ProductId);
                    item.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    item.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    item.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    var productInfo = ProductManagerApplication.GetProduct(item.ProductId);
                    if (productInfo != null)
                    {
                        item.ColorAlias = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : item.ColorAlias;
                        item.SizeAlias = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : item.SizeAlias;
                        item.VersionAlias = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : item.VersionAlias;
                    }
                }

                var verifications = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { order.Id }).Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification);
                var codes = verifications.ToList();//待消费的核销码
                if (codes != null)
                {
                    codes.ForEach(a =>
                    {
                        a.SourceCode = a.VerificationCode;
                        a.VerificationCode = System.Text.RegularExpressions.Regex.Replace(a.VerificationCode, "(\\d{4})\\d{4}(\\d{4})", "$1****$2");
                    });
                }
                var virtualOrderItemInfos = OrderApplication.GetVirtualOrderItemInfosByOrderId(order.Id).Select(p =>
                {
                    return new
                    {
                        VirtualProductItemType = p.VirtualProductItemType,
                        VirtualProductItemName = p.VirtualProductItemName,
                        Content = ReplaceImage(p.Content, p.VirtualProductItemType)
                    };
                });
                order.OrderStatusText = order.OrderStatus.ToDescription();
                order.PaymentTypeName= PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName;//统一显示支付方式名称
                return new { success = true, order = order, orderItem = orderItem, virtualProductInfo = virtualProductInfo, virtualOrderItemInfos = virtualOrderItemInfos, verificationCodes = codes };
            }
            else
            {
                var order = Application.OrderApplication.GetOrderByPickCode(pickcode);
                if (order == null)
                    return new { success = false, msg = "该核销码无效" };
                if (order.ShopBranchId != CurrentShopBranch.Id)
                    return new { success = false, msg = "非本门店核销码，请买家核对提货信息" };
                if (order.OrderStatus == Entities.OrderInfo.OrderOperateStatus.Finish && order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                    return new { success = false, msg = "该核销码于" + order.FinishDate.ToString() + "已核销" };
                var orderRefundInfo = RefundApplication.GetOrderRefundByOrderId(order.Id);
                if (orderRefundInfo != null && orderRefundInfo.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed)
                {
                    return new { success = false, msg = "该订单已退款，不能再核销" };
                }
                var orderItem = Application.OrderApplication.GetOrderItemsByOrderId(order.Id);
                foreach (var item in orderItem)
                {
                    string pic = Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_100);
                    if (item.ThumbnailsUrl.Contains("skus"))
                    {
                        pic = HimallIO.GetRomoteImagePath(item.ThumbnailsUrl);

                    }
                    item.ThumbnailsUrl = pic;
                    Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetTypeByProductId(item.ProductId);
                    item.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    item.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    item.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    var productInfo = ProductManagerApplication.GetProduct(item.ProductId);
                    if (productInfo != null)
                    {
                        item.ColorAlias = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : item.ColorAlias;
                        item.SizeAlias = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : item.SizeAlias;
                        item.VersionAlias = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : item.VersionAlias;
                    }
                }
                //退款状态
                var refundobjs = OrderApplication.GetOrderRefunds(orderItem.Select(e => e.Id));
                //小于4表示商家未确认；与平台未审核，都算退款、退货中
                var refundProcessing = refundobjs.Where(e => (int)e.SellerAuditStatus > 4 && ((int)e.SellerAuditStatus < 4 || e.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm));
                if (refundProcessing.Count() > 0)
                    order.RefundStats = 1;

                order.OrderStatusText = order.OrderStatus.ToDescription();
                return new { success = true, order = order, orderItem = orderItem };
            }
        }
        /// <summary>
        /// 门店核销订单
        /// </summary>
        /// <param name="pickcode"></param>
        /// <returns></returns>
        public object GetShopBranchOrderConfirm(string pickcode)
        {
            CheckUserLogin();

            if (string.IsNullOrWhiteSpace(pickcode))
                return new { success = false, msg = "该核销码无效" };

            var codes = pickcode.Split(',').ToList();
            var list = OrderApplication.GetOrderVerificationCodeInfoByCodes(codes);
            if (list != null && list.Count > 0)
            {
                var orderCount = list.Select(a => a.OrderId).Distinct().Count();
                if (orderCount > 1)
                {
                    return new { success = false, msg = "非同一个订单的核销码无法一起核销" };
                }

                var order = OrderApplication.GetOrderInfo(list[0].OrderId);
                if (order == null)
                    return new { success = false, msg = "该核销码无效" };

                if (order.OrderType != OrderInfo.OrderTypes.Virtual)
                    return new { success = false, msg = "核销订单无效" };

                if (order.ShopBranchId != CurrentShopBranch.Id)
                    return new { success = false, msg = "非本店核销码，请买家核对信息" };


                var waitVerificationNum = list.Where(a => a.Status == OrderInfo.VerificationCodeStatus.WaitVerification).Count();
                if (waitVerificationNum != list.Count)
                {
                    return new { success = false, msg = "包含非待消费的核销码" };
                }

                var orderItem = Application.OrderApplication.GetOrderItemsByOrderId(order.Id).FirstOrDefault();
                if (orderItem != null && orderItem.EffectiveDate.HasValue)
                {
                    if (DateTime.Now < orderItem.EffectiveDate.Value)
                        return new { success = false, msg = "该核销码暂时不能核销，请留意生效时间！" };
                }
                var verificationTime = DateTime.Now;
                OrderApplication.UpdateOrderVerificationCodeStatusByCodes(list.Select(a => a.VerificationCode).ToList(), order.Id, OrderInfo.VerificationCodeStatus.AlreadyVerification, DateTime.Now, this.CurrentShopBranch.UserName);
                OrderApplication.AddVerificationRecord(new VerificationRecordInfo()
                {
                    OrderId = order.Id,
                    VerificationCodeIds = "," + string.Join(",", list.Select(a => a.VerificationCode)) + ",",
                    VerificationTime = verificationTime,
                    VerificationUser = this.CurrentShopBranch.UserName
                });
                return new { success = true, msg = "已核销", verificationCodes = string.Join(",", list.Select(a => a.VerificationCode)), verificationTime = verificationTime.ToString("yyyy-MM-dd HH:mm:ss"), verificationUser = this.CurrentShopBranch.UserName };
            }
            else
            {
                var order = Application.OrderApplication.GetOrderByPickCode(pickcode);
                if (order == null)
                    return new { success = false, msg = "该提货码无效" };
                if (order.ShopBranchId != CurrentShopBranch.Id)
                    return new { success = false, msg = "非本门店提货码，请买家核对提货信息" };
                if (order.OrderStatus == Entities.OrderInfo.OrderOperateStatus.Finish && order.DeliveryType == CommonModel.DeliveryType.SelfTake)
                    return new { success = false, msg = "该提货码于" + order.FinishDate.ToString() + "已核销" };
                if (order.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp)
                    return new { success = false, msg = "只有待自提的订单才能进行核销" };

                Application.OrderApplication.ShopBranchConfirmOrder(order.Id, CurrentShopBranch.Id, this.CurrentUser.UserName);
                return new { success = true, msg = "已核销" };
            }
        }

        /// <summary>
        /// 搜索门店订单
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<DTO.FullOrder> PostSearchShopBranchOrder(OrderQuery query)
        {
            if (query.PageNo < 1)
                query.PageNo = 1;
            if (query.PageSize < 1)
                query.PageSize = 10;

            CheckUserLogin();

            query.ShopBranchId = CurrentShopBranch.Id;

            var status = new[] {
              Entities.  OrderInfo.OrderOperateStatus.WaitPay,
              Entities.   OrderInfo.OrderOperateStatus.WaitDelivery,
             Entities.     OrderInfo.OrderOperateStatus.WaitReceiving,
            Entities.    OrderInfo.OrderOperateStatus.WaitSelfPickUp,
            Entities.    OrderInfo.OrderOperateStatus.Finish,
             Entities.   OrderInfo.OrderOperateStatus.Close,
             Entities.OrderInfo.OrderOperateStatus.WaitVerification
            };
            if (query.IsSelfTake.HasValue && query.IsSelfTake.Value == 1)
            {
                query.Status = OrderInfo.OrderOperateStatus.Finish;
            }
            else
            {
                if (query.Status == null || !status.Contains(query.Status.Value))//门店只能查询这几种状态的订单
                    query.Status = Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp;
            }

            var data = OrderApplication.GetFullOrders(query);
            data.Models.ForEach(p => p.Block(i => i.CellPhone));//手机号门店

            return data.Models;
        }

        public object PostSearchVerificationRecord(VerificationRecordQuery query)
        {
            if (query.PageNo < 1)
                query.PageNo = 1;
            if (query.PageSize < 1)
                query.PageSize = 10;

            CheckUserLogin();

            query.ShopBranchId = CurrentShopBranch.Id;

            var data = OrderApplication.GetVerificationRecords(query);
            return data.Models.Select(p =>
            {
                return new
                {
                    Quantity = p.Quantity,
                    ImagePath = p.ImagePath,
                    Specifications = p.Specifications,
                    ProductName = p.ProductName,
                    Price = p.Price,
                    OrderId = p.OrderId,
                    VerificationUser = p.VerificationUser,
                    VerificationTime = p.VerificationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    Name = this.CurrentShopBranch.ShopBranchName,
                    Id = p.Id
                };

            });
        }

        public object GetVerificationRecordDetail(long id)
        {
            CheckUserLogin();

            var recordInfo = OrderApplication.GetVerificationRecordInfoById(id);
            if (recordInfo == null)
                return new { success = false, msg = "错误的ID" };

            var order = OrderApplication.GetOrderInfo(recordInfo.OrderId);
            if (order == null)
                return new { success = false, msg = "该核销码无效" };

            if (order.ShopBranchId != this.CurrentShopBranch.Id)
                return new { success = false, msg = "非本店核销记录" };

            var orderItem = Application.OrderApplication.GetOrderItemsByOrderId(order.Id);
            foreach (var item in orderItem)
            {
                item.ThumbnailsUrl = Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_100);
                Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetTypeByProductId(item.ProductId);
                item.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                item.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                item.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                var productInfo = ProductManagerApplication.GetProduct(item.ProductId);
                if (productInfo != null)
                {
                    item.ColorAlias = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : item.ColorAlias;
                    item.SizeAlias = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : item.SizeAlias;
                    item.VersionAlias = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : item.VersionAlias;
                }
            }
            int validityType = 0; string startDate = string.Empty, endDate = string.Empty;
            var virtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(orderItem.FirstOrDefault().ProductId);
            if (virtualProductInfo != null)
            {
                validityType = virtualProductInfo.ValidityType ? 1 : 0;
                if (validityType == 1)
                {
                    startDate = virtualProductInfo.StartDate.Value.ToString("yyyy-MM-dd");
                    endDate = virtualProductInfo.EndDate.Value.ToString("yyyy-MM-dd");
                }
            }

            var verificationCode = recordInfo.VerificationCodeIds.Trim(',').Split(',').ToList();
            verificationCode = verificationCode.Select(a => a = Regex.Replace(a, @"(\d{4})", "$1 ")).ToList();
            var virtualOrderItemInfos = OrderApplication.GetVirtualOrderItemInfosByOrderId(order.Id).Select(p =>
            {
                return new
                {
                    VirtualProductItemType = p.VirtualProductItemType,
                    VirtualProductItemName = p.VirtualProductItemName,
                    Content = ReplaceImage(p.Content, p.VirtualProductItemType)
                };
            });
            order.OrderStatusText = order.OrderStatus.ToDescription();
            return new { success = true, order = order, orderItem = orderItem, virtualOrderItemInfos = virtualOrderItemInfos, verificationCodes = verificationCode, validityType = validityType, startDate = startDate, endDate = endDate, verificationTime = recordInfo.VerificationTime.ToString("yyyy-MM-dd HH:mm:ss"), verificationUser = recordInfo.VerificationUser };
        }

        public object GetShopBranchOrderCount()
        {
            CheckUserLogin();
            var statistic = StatisticApplication.GetBranchOrderStatistic(CurrentUser.ShopBranchId);
            return new
            {
                success = true,
                waitPayCount = statistic.WaitingForPay,
                waitReceive = statistic.WaitingForRecieve,
                waitDelivery = statistic.WaitingForDelivery,
                waitSelfPickUp = statistic.WaitingForSelfPickUp,
                waitConsumption = OrderApplication.GetWaitConsumptionOrderNumByUserId(shopBranchId: CurrentUser.ShopBranchId)
            };
        }

        /// <summary>
        /// 获取订单信息
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public object GetOrderInfo(long orderId)
        {
            AutoMapper.Mapper.CreateMap<DTO.Order, ShopBranchOrderGetOrderModel>();
            var data = Application.OrderApplication.GetOrder(orderId);
            ShopBranchOrderGetOrderModel result = AutoMapper.Mapper.Map<DTO.Order, ShopBranchOrderGetOrderModel>(data);
            result.CanDaDaExpress = CityExpressConfigApplication.GetStatus(data.ShopId);
            result.Block(p => p.CellPhone);//收货人手机
            return result;
        }
        /// <summary>
        /// 获取所有快递公司名称
        /// </summary>
        /// <returns></returns>
        public string GetAllExpress()
        {
            var listData = ExpressApplication.GetAllExpress().Select(i => i.Name);
            return String.Join(",", listData);
        }
        /// <summary>
        /// 确认自提
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        [HttpPost]
        public object PostConfirmGoods(OrderDeliveryModel model)
        {
            CheckUserLogin();
            OrderApplication.ShopBranchConfirmOrder(model.orderId, CurrentShopBranch.Id, CurrentShopBranch.UserName);
            return new { success = true, msg = "" };
        }
        /// <summary>
        /// 门店发货
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public object PostShopSendGood(OrderDeliveryModel model)
        {
            CheckUserLogin();
            string shopkeeperName = "";
            long shopid = CurrentShopBranch.ShopId;
            long sbid = CurrentUser.ShopBranchId;
            shopkeeperName = CurrentShopBranch.UserName;
            var returnurl = String.Format("{0}/Common/ExpressData/SaveExpressData", CurrentUrlHelper.CurrentUrlNoPort());
            if (model.deliveryType == DeliveryType.CityExpress.GetHashCode())  //同城物流
            {
                var dadaconfig = CityExpressConfigApplication.GetDaDaCityExpressConfig(shopid);
                if (!dadaconfig.IsEnable)
                {
                    throw new HimallApiException("未开启同城合作物流");
                }
                var order = OrderApplication.GetOrder(model.orderId);
                if (order == null || order.ShopId != shopid || !(order.ShopBranchId > 0) || order.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitDelivery)
                {
                    throw new HimallApiException("错误的订单编号");
                }
                //Log.Error("达达发货ShopBranchOrder-开始01：orderId:" + model.orderId);

                var sbdata = ShopBranchApplication.GetShopBranchById(sbid);

                string json = ExpressDaDaHelper.addAfterQuery(shopid, sbdata.DaDaShopId, model.shipOrderNumber);
                var resultObj = JsonConvert.DeserializeObject(json) as JObject;
                string status = resultObj["status"].ToString();

                //Log.Error("达达发货ShopBranchOrder-开始02：status:" + status);
                if (status != "success")
                {
                    //订单码过期，重发单
                    json = SendDaDaExpress(model.orderId, shopid, sbid, false);
                    var rObj2 = JsonConvert.DeserializeObject(json) as JObject;
                    string status2 = rObj2["status"].ToString();
                    if (status2 != "success")
                    {
                        string msg = rObj2["msg"].ToString();
                        return ErrorResult(msg);
                    }
                }
            }
            OrderApplication.ShopSendGood(model.orderId, model.deliveryType, shopkeeperName, model.companyName, model.shipOrderNumber, returnurl);
            return SuccessResult("发货成功");
        }

        /// <summary>
        /// 订单是否正在申请售后
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public object GetIsOrderAfterService(long orderId)
        {
            bool isAfterService = Application.OrderApplication.IsOrderAfterService(orderId);
            if (isAfterService)
            {
                return new { success = true, isAfterService = true };
            }
            else
            {
                return new { success = true, isAfterService = false };
            }
        }

        /// <summary>
        /// 查看订单物流
        /// </summary>
        /// <param name="orderId"></param>
        /// <returns></returns>
        public object GetLogisticsData(string expressCompanyName, string shipOrderNumber, long orderid)
        {
            var order = OrderApplication.GetOrder(orderid);
            if (order != null && order.DeliveryType == DeliveryType.CityExpress)
            {
                decimal StoreLat = 0, Storelng = 0;
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
                    deliveryType = DeliveryType.CityExpress.GetHashCode(),
                    userLat = order.ReceiveLatitude,
                    userLng = order.ReceiveLongitude,
                    storeLat = StoreLat,
                    Storelng = Storelng,
                    shipOrderNumber = order.ShipOrderNumber,
                };
            }
            else
            {
                var expressData = ExpressApplication.GetExpressData(expressCompanyName, shipOrderNumber, orderid.ToString(), order.CellPhone);
                if (expressData != null)
                {
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
                    return json;
                }
                else
                {
                    var json = new
                    {
                        success = false,
                        msg = "无物流信息"
                    };
                    return json;
                }

            }
        }

        public object GetOrderDetail(long id)
        {
            CheckUserLogin();
            long shopid = CurrentShopBranch.ShopId;
            long sbid = CurrentUser.ShopBranchId;

            var ordser = ServiceProvider.Instance<OrderService>.Create;

            Entities.OrderInfo order = ordser.GetOrder(id);
            if (order == null || order.ShopBranchId != sbid)
            {
                throw new HimallApiException("错误的订单编号");
            }
            var bonusService = ServiceProvider.Instance<ShopBonusService>.Create;
            var orderRefundService = ServiceProvider.Instance<RefundService>.Create;
            var shopService = ServiceProvider.Instance<ShopService>.Create;
            var productService = ServiceProvider.Instance<ProductService>.Create;
            var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(order.ShopId);
            var orderitems = OrderApplication.GetOrderItems(order.Id);
            bool isCanApply = false;
            //获取订单商品项数据
            var orderDetail = new
            {
                ShopName = shopService.GetShop(order.ShopId).ShopName,
                ShopId = order.ShopId,
                OrderItems = orderitems.Select(item =>
                {
                    var productinfo = productService.GetProduct(item.ProductId);
                    if (order.OrderStatus == Entities.OrderInfo.OrderOperateStatus.WaitDelivery)
                    {
                        isCanApply = orderRefundService.CanApplyRefund(id, item.Id);
                    }
                    else
                    {
                        isCanApply = orderRefundService.CanApplyRefund(id, item.Id, false);
                    }

                    Entities.TypeInfo typeInfo = ServiceProvider.Instance<TypeService>.Create.GetType(productinfo.TypeId);
                    string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                    if (productinfo != null)
                    {
                        colorAlias = !string.IsNullOrWhiteSpace(productinfo.ColorAlias) ? productinfo.ColorAlias : colorAlias;
                        sizeAlias = !string.IsNullOrWhiteSpace(productinfo.SizeAlias) ? productinfo.SizeAlias : sizeAlias;
                        versionAlias = !string.IsNullOrWhiteSpace(productinfo.VersionAlias) ? productinfo.VersionAlias : versionAlias;
                    }
                    
                    return new
                    {
                        ItemId = item.Id,
                        ProductId = item.ProductId,
                        ProductName = item.ProductName,
                        Count = item.Quantity,
                        Price = item.SalePrice,
                        //ProductImage = "http://" + Url.Request.RequestUri.Host + productService.GetProduct(item.ProductId).GetImage(ProductInfo.ImageSize.Size_100),

                        ProductImage =item.ThumbnailsUrl.Contains("skus")? HimallIO.GetRomoteImagePath(item.ThumbnailsUrl): Core.HimallIO.GetRomoteProductSizeImage(item.ThumbnailsUrl, 1, (int)Himall.CommonModel.ImageSize.Size_100),
                        color = item.Color,
                        size = item.Size,
                        version = item.Version,
                        IsCanRefund = isCanApply,
                        ColorAlias = colorAlias,
                        SizeAlias = sizeAlias,
                        VersionAlias = versionAlias
                    };
                })
            };

            VirtualProductInfo virtualProductInfo = null;
            List<dynamic> codes = null;
            List<dynamic> virtualItems = null;
            int validityType = 0; string startDate = string.Empty, endDate = string.Empty;
            if (order.OrderType == OrderInfo.OrderTypes.Virtual && orderDetail.OrderItems != null)
            {
                virtualProductInfo = ProductManagerApplication.GetVirtualProductInfoByProductId(orderDetail.OrderItems.FirstOrDefault().ProductId);
                if (virtualProductInfo != null)
                {
                    validityType = virtualProductInfo.ValidityType ? 1 : 0;
                    if (validityType == 1)
                    {
                        startDate = virtualProductInfo.StartDate.Value.ToString("yyyy-MM-dd");
                        endDate = virtualProductInfo.EndDate.Value.ToString("yyyy-MM-dd");
                    }
                }
                var verificationCodes = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { order.Id });
                if (verificationCodes != null)
                {
                    verificationCodes.ForEach(a =>
                    {
                        if (a.Status == OrderInfo.VerificationCodeStatus.WaitVerification || a.Status == OrderInfo.VerificationCodeStatus.Refund)
                        {
                            a.VerificationCode = System.Text.RegularExpressions.Regex.Replace(a.VerificationCode, "(\\d{4})\\d{4}(\\d{4})", "$1****$2");
                        }
                        a.VerificationCode = System.Text.RegularExpressions.Regex.Replace(a.VerificationCode, @"(\d{4})", "$1 ");
                    });
                }
                codes = verificationCodes.Select(p =>
                {
                    return new
                    {
                        VerificationCode = p.VerificationCode,
                        Status = p.Status,
                        StatusText = p.Status.ToDescription()
                    };
                }).ToList<dynamic>();

                var virtualOrderItems = OrderApplication.GetVirtualOrderItemInfosByOrderId(order.Id);
                virtualItems = virtualOrderItems.Select(p =>
                {
                    return new
                    {
                        VirtualProductItemName = p.VirtualProductItemName,
                        VirtualProductItemType = p.VirtualProductItemType,
                        Content = ReplaceImage(p.Content, p.VirtualProductItemType)
                    };
                }).ToList<dynamic>();
            }
            var orderModel = new
            {
                Id = order.Id,
                OrderType = order.OrderType,
                OrderTypeName = order.OrderType.ToDescription(),
                Status = order.OrderStatus.ToDescription(),
                ShipTo = order.ShipTo,
                Phone = order.CellPhone,
                Address = order.RegionFullName + " " + order.Address,
                HasExpressStatus = !string.IsNullOrWhiteSpace(order.ShipOrderNumber),
                ExpressCompanyName = order.ExpressCompanyName,
                Freight = order.Freight,
                Tax = order.Tax,
                IntegralDiscount = order.IntegralDiscount,
                RealTotalAmount = order.OrderTotalAmount - order.RefundTotalAmount,
                CapitalAmount = order.CapitalAmount,
                RefundTotalAmount = order.RefundTotalAmount,
                ProductTotalAmount = order.ProductTotalAmount,
                OrderDate = order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                FinishDate=order.FinishDate.HasValue? order.FinishDate.Value.ToString("yyyy-MM-dd HH:mm:ss"):null,
                ShopName = order.ShopName,
                ShopBranchName = CurrentShopBranch.ShopBranchName,
                VShopId = vshop == null ? 0 : vshop.Id,
                commentCount = OrderApplication.GetOrderCommentCount(order.Id),
                ShopId = order.ShopId,
                orderStatus = (int)order.OrderStatus,
                //Invoice = order.InvoiceType.ToDescription(),
                //InvoiceValue = (int)order.InvoiceType,
                //InvoiceContext = order.InvoiceContext,
                //InvoiceTitle = order.InvoiceTitle,
                PaymentType = order.PaymentType.ToDescription(),
                PaymentTypeValue = (int)order.PaymentType,
                PaymentTypeDesc = order.PaymentTypeDesc,
                OrderPayAmount = order.OrderPayAmount,
                PaymentTypeName = PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName,
                FullDiscount = order.FullDiscount,
                DiscountAmount = order.DiscountAmount,
                PlatDiscountAmount = order.PlatDiscountAmount,
                OrderRemarks = order.OrderRemarks,
                DeliveryType = order.DeliveryType,
                //InvoiceCode = order.InvoiceCode,
                OrderInvoice = OrderApplication.GetOrderInvoiceInfo(order.Id)
            };
            return new { success = true, Order = orderModel, OrderItem = orderDetail.OrderItems, VerificationCodes = codes, VirtualOrderItems = virtualItems, StartDate = startDate, EndDate = endDate, ValidityType = validityType };
        }

        public object GetCancelDadaExpress(long orderId, int reasonId, string cancelReason)
        {
            CheckUserLogin();
            long shopid = CurrentShopBranch.ShopId;
            long sbid = CurrentUser.ShopBranchId;
            var order = OrderApplication.GetOrder(orderId);
            if (order == null || order.ShopBranchId != sbid || order.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitReceiving || order.DeliveryType != DeliveryType.CityExpress)
            {
                throw new HimallApiException("错误的订单编号");
            }
            if (order.DadaStatus > DadaStatus.WaitTake.GetHashCode())
            {
                throw new HimallApiException("订单配送不可取消！");
            }
            var sbdata = ShopBranchApplication.GetShopBranchById(sbid);
            string json = ExpressDaDaHelper.orderFormalCancel(shopid, orderId.ToString(), reasonId, cancelReason);
            var resultObj = JsonConvert.DeserializeObject(json) as JObject;
            string status = resultObj["status"].ToString();
            if (status != "success")
            {
                throw new HimallApiException(resultObj["msg"].ToString());
            }
            ExpressDaDaApplication.SetOrderCancel(orderId, "商家主动取消");
            var result = JsonConvert.DeserializeObject(resultObj["result"].ToString()) as JObject;
            return new
            {
                success = true,
                deduct_fee = result["deduct_fee"].ToString()
            };
        }

        public object GetCityExpressDaDa(long orderId)
        {
            CheckUserLogin();
            long shopid = CurrentShopBranch.ShopId;
            long sbid = CurrentUser.ShopBranchId;

            string json = SendDaDaExpress(orderId, shopid, sbid, true);
            var resultObj = JsonConvert.DeserializeObject(json) as JObject;
            string status = resultObj["status"].ToString();
            if (status != "success")
            {
                throw new HimallApiException(resultObj["msg"].ToString());
            }
            var result = JsonConvert.DeserializeObject(resultObj["result"].ToString()) as JObject;

            return new
            {
                success = true,
                distance = result["distance"].ToString(),
                fee = result["fee"].ToString(),
                deliveryNo = result["deliveryNo"].ToString(),
            };
        }
        public object GetDadaCancelReasons()
        {
            CheckUserLogin();
            long shopid = CurrentShopBranch.ShopId;
            long sbid = CurrentUser.ShopBranchId;
            string json = ExpressDaDaHelper.orderCancelReasons(shopid);
            var resultObj = JsonConvert.DeserializeObject(json) as JObject;
            return resultObj;
        }

        private string SendDaDaExpress(long orderId, long shopid, long sbid, bool isQueryOrder)
        {
            //Log.Error("达达发货ShopBranchOrder-01：orderId:" + orderId);
            var order = OrderApplication.GetOrder(orderId);
            var orderitem = OrderApplication.GetOrderItemsByOrderId(orderId);
            if (order == null || order.ShopBranchId != sbid || order.OrderStatus != Entities.OrderInfo.OrderOperateStatus.WaitDelivery)
            {
                throw new HimallApiException("错误的订单编号");
            }
            var dadaconfig = CityExpressConfigApplication.GetDaDaCityExpressConfig(shopid);
            if (!dadaconfig.IsEnable)
            {
                throw new HimallApiException("未开启同城合作物流");
            }
            //Log.Error("达达发货ShopBranchOrder-02：OrderStatus:" + order.OrderStatus + "--RegionId:" + order.RegionId);
            if (order.ReceiveLatitude <= 0 || order.ReceiveLongitude <= 0)
            {
                throw new HimallApiException("未获取到客户收货地址坐标信息，无法使用该配送方式");
            }
            var sbdata = ShopBranchApplication.GetShopBranchById(sbid);
            if (sbdata == null || string.IsNullOrWhiteSpace(sbdata.DaDaShopId))
            {
                throw new HimallApiException("门店未在达达注册，或所在城市达达不支持配送，无法发单，请商家在后台进行设置");
            }
            //Log.Error("达达发货ShopBranchOrder-03：Latitude:" + sbdata.DaDaShopId);
            string cityCode = "";
            var _adregion = RegionApplication.GetRegion(order.RegionId);
            var _city = GetCity(_adregion);
            try
            {
                string cityJson = ExpressDaDaHelper.cityCodeList(shopid);
                var cityObj = JsonConvert.DeserializeObject(cityJson) as JObject;
                JArray citylist = (JArray)cityObj["result"];
                foreach (JToken item in citylist)
                {
                    if (_city.ShortName == item["cityName"].ToString())
                    {
                        cityCode = item["cityCode"].ToString();
                        break;
                    }
                }

            }
            catch
            {
            }
            //达达不支持的城市
            if (cityCode == "")
            {
                throw new HimallApiException("配送范围超区，无法配送");
            }
            string callback = CurrentUrlHelper.CurrentUrl() + "/pay/dadaOrderNotify/";
            bool isreaddorder = (order.DadaStatus == DadaStatus.Cancel.GetHashCode());
            if (isQueryOrder)
            {
                isreaddorder = false;
            }
            decimal weight = 0;
            orderitem.ForEach(item => {
                weight += item.Freight * item.Quantity;
            });
            //Log.Error(DateTime.Now + "ShopBranchOrder-0:callback:" + callback + "|shopid:" + shopid + "|DaDaShopId:" + sbdata.DaDaShopId + "|orderId:" + order.Id + "|cityCode:" + cityCode + "|isreaddorder:" + isreaddorder);
            string json = ExpressDaDaHelper.addOrder(shopid, sbdata.DaDaShopId, order.Id.ToString()
                , cityCode, (double)order.TotalAmount, 0, ExpressDaDaHelper.DateTimeToUnixTimestamp(DateTime.Now.AddMinutes(15))
                , order.ShipTo, order.Address, order.ReceiveLatitude, order.ReceiveLongitude
                , callback, order.CellPhone, order.CellPhone, (double)weight, isQueryDeliverFee: isQueryOrder
                , isReAddOrder: isreaddorder);

            //Log.Error(DateTime.Now + "达达发货ShopBranchOrder-1：json:" + json);
            return json;
        }

        /// <summary>
        /// 获取市级地区
        /// </summary>
        /// <param name="region"></param>
        /// <returns></returns>
        private CommonModel.Region GetCity(CommonModel.Region region)
        {
            CommonModel.Region _city = region;
            if (_city.Level == CommonModel.Region.RegionLevel.City || _city.Level == CommonModel.Region.RegionLevel.Province || _city.Parent == null)
            {
                return _city;
            }
            _city = _city.Parent;
            return GetCity(_city);
        }

        private List<string> ReplaceImage(string content, ProductInfo.VirtualProductItemType type)
        {
            if (type != ProductInfo.VirtualProductItemType.Picture)
                return new List<string>() { content };

            List<string> list = content.Split(',').ToList();
            return list.Select(a => a = CurrentUrlHelper.CurrentUrl() + a).ToList();
        }

        /// <summary>
        /// 获取前5分钟可以语音播报的订单数
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public object GetVoiceOrder()
        {
            CheckUserLogin();
            var orderIds = OrderApplication.GetOrderIdsByLatestTimeYuyin(5, CurrentShopBranch.Id, 0);//读取最近时间段内支付订单
            Core.Log.Info("语音取到订单了吗?" + orderIds.Count);
            if (orderIds != null && orderIds.Count > 0)
            {
                return new
                {
                    success = true,
                    count = orderIds.Count
                };
            }
            return new
            {
                success = false,
                count = 0
            };
        }
        public object GetLongConnectionVoiceOrder()
        {
            CheckUserLogin();
            int status = 0;
            int c = 0;
            int ordercount = 0;
            while (status != 1)
            {
                c++;
                
                var orderIds = OrderApplication.GetOrderIdsByLatestTimeYuyin(5,CurrentShopBranch.Id, 0);//读取最近时间段内支付订单
                Core.Log.Info("语音取到订单了吗?" + orderIds.Count);
                if (orderIds != null && orderIds.Count > 0)
                {
                    status = 1;
                    ordercount = orderIds.Count;
                }
                else
                {
                    status = -1;
                }

                if (c == 10)
                {
                    break;
                }
                Thread.Sleep(2000);
            }
            return new
            {
                success = ordercount > 0,
                count = ordercount
            };

        }
    }
}
