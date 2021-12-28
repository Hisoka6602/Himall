using Himall.Core;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using Himall.Web.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using Himall.Application;
using Himall.Entities;
using System;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class OrderController : BaseAdminController
    {
        private OrderService _OrderService;
        private ExpressService _ExpressService;
        private PaymentConfigService _iPaymentConfigService;
        private FightGroupService _FightGroupService;
        public OrderController(OrderService OrderService, ExpressService ExpressService, PaymentConfigService PaymentConfigService
            , FightGroupService FightGroupService)
        {
            _OrderService = OrderService;
            _ExpressService = ExpressService;
            _iPaymentConfigService = PaymentConfigService;
            _FightGroupService = FightGroupService;
        }

        public ActionResult Management(long? shopBranchId)
        {
            ViewBag.hasHistory = SiteSettingApplication.SiteSettings.IsOpenHistoryOrder;
            var model = PaymentApplication.GetPaymentTypeDesc();
            var shopbranchName = "";
            if (shopBranchId.HasValue)
            {
                var shopbranch = ShopBranchApplication.GetShopBranchById(shopBranchId.Value);
                if (shopbranch != null)
                    shopbranchName = shopbranch.ShopBranchName;
            }
            ViewBag.ShopBranchName = shopbranchName;
            return View(model);
        }


        public ActionResult Detail(long id)
        {
            var order = _OrderService.GetOrder(id);
            if (order == null)
            {
                throw new HimallException("错误的订单信息");
            }
            if (order.OrderType == Entities.OrderInfo.OrderTypes.FightGroup)
            {
                var fgord = _FightGroupService.GetFightGroupOrderStatusByOrderId(order.Id);
                order.FightGroupOrderJoinStatus = fgord.GetJoinStatus;
                order.FightGroupCanRefund = fgord.CanRefund;
            }
            var orderItems = _OrderService.GetOrderItemsByOrderId(order.Id);

            //处理平台佣金
            var orderRefunds = RefundApplication.GetOrderRefundList(id);
            foreach (var item in orderItems)
            {
                var refund = orderRefunds.Where(e => e.OrderItemId == item.Id).Sum(e => e.ReturnPlatCommission);
                item.PlatCommission = item.CommisRate * (item.RealTotalPrice + item.PlatCouponDiscount);
                if (refund > 0)
                {
                    item.PlatCommission = item.PlatCommission - refund;
                }

                item.PlatCommission = (item.PlatCommission < 0) ? 0 : Core.Helper.CommonHelper.SubDecimal(item.PlatCommission, 2);

            }
            ViewBag.OrderItems = orderItems;
            ViewBag.Logs = _OrderService.GetOrderLogs(order.Id);
            ViewBag.Coupon = 0;
            string shipperAddress = string.Empty, shipperTelPhone = string.Empty;
            #region 门店信息
            if (order.ShopBranchId > 0)
            {
                var shopBranchInfo = ShopBranchApplication.GetShopBranchById(order.ShopBranchId);
                if (shopBranchInfo != null)
                {
                    ViewBag.ShopBranchInfo = shopBranchInfo;
                    if (order.OrderStatus == Entities.OrderInfo.OrderOperateStatus.Finish) ViewBag.ShopBranchContactUser = shopBranchInfo.UserName;
                    if (order.OrderType == OrderInfo.OrderTypes.Virtual)
                    {
                        shipperAddress = RegionApplication.GetFullName(shopBranchInfo.AddressId) + " " + shopBranchInfo.AddressDetail;
                        shipperTelPhone = shopBranchInfo.ContactPhone;
                    }
                }
            }
            #endregion
            if (order.OrderType == OrderInfo.OrderTypes.Virtual)
            {
                ViewBag.VirtualOrderItemInfos = OrderApplication.GetVirtualOrderItemInfosByOrderId(order.Id);
                ViewBag.OrderVerificationCodeInfos = OrderApplication.GetOrderVerificationCodeInfosByOrderIds(new List<long>() { order.Id });
                if (order.ShopBranchId == 0)
                {
                    var verificationShipper = ShopShippersApplication.GetDefaultVerificationShipper(order.ShopId);
                    if (verificationShipper != null)
                    {
                        shipperAddress = RegionApplication.GetFullName(verificationShipper.RegionId) + " " + verificationShipper.Address;
                        shipperTelPhone = verificationShipper.TelPhone;
                    }
                }
            }
            ViewBag.ShipperAddress = shipperAddress;
            ViewBag.ShipperTelPhone = shipperTelPhone;
            //发票信息
            ViewBag.OrderInvoiceInfo = OrderApplication.GetOrderInvoiceInfo(order.Id);
            //统一显示支付方式名称
            order.PaymentTypeName = PaymentApplication.GetPaymentTypeDescById(order.PaymentTypeGateway) ?? order.PaymentTypeName;
            order.Block(p => p.CellPhone);//收货人手机
            return View(order);
        }


        [HttpPost]
        [UnAuthorize]
        public JsonResult List(OrderQuery query, int page, int rows)
        {
            query.PageNo = page;
            query.PageSize = rows;
            query.Operator = Operator.Admin;
            query.PaymentTypeGateways = PaymentApplication.GetPaymentIdByDesc(query.PaymentTypeGateway);
            var fullOrders = OrderApplication.GetFullOrders(query);
            var models = fullOrders.Models.ToList();

            var shops = ShopApplication.GetShops(fullOrders.Models.Select(p => p.ShopId).ToList());
            var shopBranchs = ShopBranchApplication.GetShopBranchs(models.Where(p => p.DeliveryType == CommonModel.DeliveryType.SelfTake && p.ShopBranchId != 0).Select(p => p.ShopBranchId).ToList());
            var users = MemberApplication.GetMembersByIds(models.Select(p => p.UserId).ToList());

            var orderModels = models.Select(item =>
            {
                var shop = shops != null ? shops.Where(sp => sp.Id == item.ShopId).First() : null;
                var memberinfo = users != null ? users.Where(t => t.Id == item.UserId).FirstOrDefault() : null;
                string nickname = item.UserName;
                if (memberinfo != null)
                    nickname = memberinfo != null && string.IsNullOrEmpty(memberinfo.Nick) ? memberinfo.UserName : memberinfo.Nick;

                return new OrderModel()
                {
                    OrderId = item.Id,
                    OrderStatus = item.OrderStatus.ToDescription(),
                    OrderState = (int)item.OrderStatus,
                    OrderDate = item.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    ShopId = item.ShopId,
                    ShopName = item.ShopBranchId > 0 ? item.ShopBranchName : item.ShopName,
                    ShopBranchName = item.DeliveryType == CommonModel.DeliveryType.SelfTake && item.ShopBranchId > 0 && shopBranchs.FirstOrDefault(sb => sb.Id == item.ShopBranchId) != null ? shopBranchs.FirstOrDefault(sb => sb.Id == item.ShopBranchId).ShopBranchName : "",
                    UserId = item.UserId,
                    UserName = nickname,
                    TotalPrice = item.OrderTotalAmount,
                    PaymentTypeName = item.PaymentTypeName,
                    PlatForm = (int)item.Platform,
                    IconSrc = GetIconSrc(item.Platform),
                    PlatformText = item.Platform.ToDescription(),
                    PaymentTypeGateway = item.PaymentTypeGateway,
                    PayDate = item.PayDate,
                    PaymentTypeStr = item.PaymentTypeDesc,
                    PaymentType = item.PaymentType,
                    OrderType = item.OrderType,
                    GatewayOrderId = item.GatewayOrderId,
                    Payee = shop != null ? shop.ContactsName : string.Empty,
                    CellPhone = string.IsNullOrEmpty(item.CellPhone) ? "" : item.CellPhone,
                    RegionFullName = item.RegionFullName,
                    Address = item.Address,
                    SellerRemark = item.SellerRemark,
                    UserRemark = item.UserRemark,
                    OrderItems = item.OrderItems,
                    SellerRemarkFlag = item.SellerRemarkFlag,
                    IsVirtual = item.OrderType == OrderInfo.OrderTypes.Virtual,
                    IsLive = item.IsLive
                };
            }).ToList();

            orderModels.ForEach(o => o.Block(p => p.CellPhone));//收货人手机
            DataGridModel<OrderModel> dataGrid = new DataGridModel<OrderModel>()
            {
                rows = orderModels,
                total = fullOrders.Total
            };
            return Json(dataGrid);
        }

        public ActionResult ExportToExcel(OrderQuery query)
        {
            query.PaymentTypeGateways = PaymentApplication.GetPaymentIdByDesc(query.PaymentTypeGateway);
            var orders = OrderApplication.GetAllFullOrders(query);
            orders.ForEach(o => o.Block(p => p.CellPhone));//收货人手机

            return ExcelView("ExportOrderinfo", "平台订单信息", orders);
        }


        /// <summary>
        /// 获取订单来源图标地址
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        string GetIconSrc(PlatformType platform)
        {
            if (platform == PlatformType.IOS || platform == PlatformType.Android)
                return "/images/app.png";
            return string.Format("/images/{0}.png", platform.ToString());
        }

        /// <summary>
        /// 付款
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="payRemark">收款备注</param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult ConfirmPay(long orderId, string payRemark)
        {
            Result result = new Result();
            OrderApplication.PlatformConfirmOrderPay(orderId, payRemark, CurrentManager.UserName);
            //PaymentHelper.IncreaseSaleCount(new List<long> { orderId });
            result.success = true;
            return Json(result);

        }

        /// <summary>
        /// 取消订单
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="payRemark">收款备注</param>
        /// <returns></returns>
        [HttpPost]
        [UnAuthorize]
        public JsonResult CloseOrder(long orderId)
        {
            Result result = new Result();
            var order = OrderApplication.GetOrder(orderId);
            bool needRunClose = true;
            //拼团处理
            if (order.OrderType == OrderInfo.OrderTypes.FightGroup)
            {
                var ford = _FightGroupService.GetOrder(orderId);
                if (ford != null)
                {
                    needRunClose = false;
                    _FightGroupService.OrderBuildFailed(ford, CurrentManager.UserName, "平台取消订单");
                }
            }
            if (needRunClose)
            {
                _OrderService.PlatformCloseOrder(orderId, CurrentManager.UserName);
            }
            result.success = true;

            return Json(result);

        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult GetExpressData(string expressCompanyName, string shipOrderNumber)
        {
            string content = "暂时没有此快递单号的信息";
            if (string.IsNullOrEmpty(expressCompanyName) || string.IsNullOrEmpty(shipOrderNumber))
                return Json(content);
            string kuaidi100Code = _ExpressService.GetExpress(expressCompanyName).Kuaidi100Code;
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(string.Format("https://www.kuaidi100.com/query?type={0}&postid={1}", kuaidi100Code, shipOrderNumber));
            request.Timeout = 8000;


            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            if (response.StatusCode == HttpStatusCode.OK)
            {
                Stream stream = response.GetResponseStream();
                System.IO.StreamReader streamReader = new StreamReader(stream, System.Text.Encoding.GetEncoding("UTF-8"));

                // 读取流字符串内容
                content = streamReader.ReadToEnd();
                content = content.Replace("&amp;", "");
                content = content.Replace("&nbsp;", "");
                content = content.Replace("&", "");
            }

            return Json(content);
        }

        public ActionResult InvoiceContext()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetInvoiceContexts(int page = 1, int rows = 20)
        {
            var model = _OrderService.GetInvoiceContexts(page, rows);
            return Json(new
            {
                rows = model.Models,
                total = model.Total
            });
        }

        [HttpPost]
        public ActionResult SaveInvoiceContext(string name, long id = -1)
        {
            Entities.InvoiceContextInfo info = new Entities.InvoiceContextInfo()
            {
                Id = id,
                Name = name
            };
            _OrderService.SaveInvoiceContext(info);
            return Json(true);
        }

        [HttpPost]
        public ActionResult DeleteInvoiceContexts(long id)
        {
            _OrderService.DeleteInvoiceContext(id);
            return Json(true);
        }

        public JsonResult GetShopAndShopBranch(string keyWords, sbyte type)
        {
            var result = OrderApplication.GetShopOrShopBranch(keyWords, type);
            var values = result.Select(item => new { type = item.Type, value = item.Name, id = item.SearchId });
            return Json(values);
        }

    }
}