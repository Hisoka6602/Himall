using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using Himall.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Himall.Entities;
using System.Text.RegularExpressions;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class OrderRefundController : BaseAdminController
    {
        private RefundService _RefundService;
        public OrderRefundController(RefundService RefundService)
        {
            _RefundService = RefundService;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="showtype">0 所有 1 订单退款 2 仅退款(包含订单退款) 3 退货 4 仅退款</param>
        /// <returns></returns>
        public ActionResult Management(int showtype = 0)
        {
            ViewBag.ShowType = showtype;
            return View();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <param name="orderId"></param>
        /// <param name="auditStatus"></param>
        /// <param name="shopName"></param>
        /// <param name="ProductName"></param>
        /// <param name="userName"></param>
        /// <param name="page"></param>
        /// <param name="rows"></param>
        /// <param name="showtype">0 所有 1 订单退款 2 仅退款(包含订单退款) 3 退货 4 仅退款</param>
        /// <returns></returns>
        [ValidateInput(false)]
        [HttpPost]
        [UnAuthorize]
        public JsonResult List(DateTime? startDate, DateTime? endDate, long? orderId, int? auditStatus, string shopName, string ProductName, string userName, int page, int rows, int showtype = 0)
        {
            var queryModel = new RefundQuery()
            {
                StartDate = startDate,
                EndDate = endDate,
                OrderId = orderId,
                ProductName = ProductName,
                AuditStatus = (Entities.OrderRefundInfo.OrderRefundAuditStatus?)auditStatus,
                ShopName = shopName,
                UserName = userName,
                PageSize = rows,
                PageNo = page,
                ShowRefundType = showtype
            };

            if (auditStatus.HasValue && auditStatus.Value == (int)OrderRefundInfo.OrderRefundAuditStatus.Audited)
                queryModel.ConfirmStatus = Entities.OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm;

            var refunds = _RefundService.GetOrderRefunds(queryModel);
            var orders = OrderApplication.GetOrders(refunds.Models.Select(p => p.OrderId));
            var orderitems = OrderApplication.GetOrderItems(refunds.Models.Select(p => p.OrderItemId));
            var refundModels = refunds.Models.Select(item =>
            {
                var order = orders.FirstOrDefault(p => p.Id == item.OrderId);
                var orderitem = orderitems.FirstOrDefault(p => p.Id == item.OrderItemId);
                string spec = ((string.IsNullOrWhiteSpace(orderitem.Color) ? "" : orderitem.Color + "，")
                                + (string.IsNullOrWhiteSpace(orderitem.Size) ? "" : orderitem.Size + "，")
                                + (string.IsNullOrWhiteSpace(orderitem.Version) ? "" : orderitem.Version + "，")).TrimEnd('，');
                if (!string.IsNullOrWhiteSpace(spec))
                {
                    spec = "  【" + spec + " 】";
                }
                string showAuditStatus = "";
                //  showAuditStatus = item.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.Audited ? item.ManagerConfirmStatus.ToDescription() : item.SellerAuditStatus.ToDescription();

                showAuditStatus = ((item.SellerAuditStatus == Entities.OrderRefundInfo.OrderRefundAuditStatus.Audited)
                                    ? item.ManagerConfirmStatus.ToDescription()
                                    : (order.DeliveryType == CommonModel.DeliveryType.SelfTake ? ((CommonModel.Enum.OrderRefundShopAuditStatus)item.SellerAuditStatus).ToDescription() : item.SellerAuditStatus.ToDescription()));
                if (item.SellerAuditStatus == Entities.OrderRefundInfo.OrderRefundAuditStatus.Audited
                    && item.ManagerConfirmStatus == Entities.OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm
                    && item.RefundPayStatus == Entities.OrderRefundInfo.OrderRefundPayStatus.Payed)
                {
                    showAuditStatus = "退款中";
                }
                var verificationCodeIds = new List<string>();
                if (!string.IsNullOrWhiteSpace(item.VerificationCodeIds))
                {
                    verificationCodeIds = item.VerificationCodeIds.Split(',').ToList();
                    verificationCodeIds = verificationCodeIds.Select(a => a = Regex.Replace(a, @"(\d{4})", "$1 ")).ToList();
                }
                return new OrderRefundModel()
                {
                    RefundId = item.Id,
                    OrderId = item.OrderId,
                    AuditStatus = showAuditStatus,
                    ProductId = orderitem.ProductId,
                    ThumbnailsUrl=orderitem.ThumbnailsUrl.Contains("skus")? HimallIO.GetImagePath(orderitem.ThumbnailsUrl) : Core.HimallIO.GetProductSizeImage(orderitem.ThumbnailsUrl, 1, (int)ImageSize.Size_100),//截取图片所在目录，从而获取图片
                    ConfirmStatus = item.ManagerConfirmStatus.ToDescription(),
                    ApplyDate = item.ApplyDate.ToShortDateString(),
                    ShopId = item.ShopId,
                    ShopName = item.ShopName.Replace("'", "‘").Replace("\"", "”"),
                    UserId = item.UserId,
                    UserName = item.Applicant,
                    Amount = item.Amount.ToString("F2"),
                    SalePrice = item.EnabledRefundAmount.ToString("F2"),
                    ReturnQuantity = item.ShowReturnQuantity == 0 ? orderitem.ReturnQuantity : item.ShowReturnQuantity,
                    ProductName = orderitem.ProductName + spec,
                    Reason = string.IsNullOrEmpty(item.Reason) ? string.Empty : HTMLEncode(item.Reason.Replace("'", "‘").Replace("\"", "”")),
                    ReasonDetail = string.IsNullOrEmpty(item.ReasonDetail) ? string.Empty : item.ReasonDetail.Replace("'", "‘").Replace("\"", "”"),
                    RefundAccount = string.IsNullOrEmpty(item.RefundAccount) ? string.Empty : HTMLEncode(item.RefundAccount.Replace("'", "‘").Replace("\"", "”")),
                    ContactPerson = string.IsNullOrEmpty(item.ContactPerson) ? string.Empty : HTMLEncode(item.ContactPerson.Replace("'", "‘").Replace("\"", "”")),
                    ContactCellPhone = HTMLEncode(item.ContactCellPhone),
                    PayeeAccount = string.IsNullOrEmpty(item.PayeeAccount) ? string.Empty : HTMLEncode(item.PayeeAccount.Replace("'", "‘").Replace("\"", "”")),
                    Payee = string.IsNullOrEmpty(item.Payee) ? string.Empty : HTMLEncode(item.Payee),
                    RefundMode = (int)item.RefundMode,
                    SellerRemark = string.IsNullOrEmpty(item.SellerRemark) ? string.Empty : HTMLEncode(item.SellerRemark.Replace("'", "‘").Replace("\"", "”")),
                    ManagerRemark = string.IsNullOrEmpty(item.ManagerRemark) ? string.Empty : HTMLEncode(item.ManagerRemark.Replace("'", "‘").Replace("\"", "”")),
                    ManagerConfirmDate =item.ManagerConfirmDate.ToShortDateString(),
                    RefundStatus = ((item.SellerAuditStatus == Entities.OrderRefundInfo.OrderRefundAuditStatus.Audited)
                                    ? item.ManagerConfirmStatus.ToDescription()
                                    : ((order.DeliveryType == CommonModel.DeliveryType.SelfTake ||  order.ShopBranchId > 0) ? ((CommonModel.Enum.OrderRefundShopAuditStatus)item.SellerAuditStatus).ToDescription() : item.SellerAuditStatus.ToDescription())),
                    RefundPayType = item.RefundPayType.ToDescription(),
                    RefundPayStatus = (int)item.RefundPayStatus,
                    ApplyNumber = item.ApplyNumber,
                    CertPic1 = Core.HimallIO.GetImagePath(item.CertPic1),
                    CertPic2 = Core.HimallIO.GetImagePath(item.CertPic2),
                    CertPic3 = Core.HimallIO.GetImagePath(item.CertPic3),
                    IsVirtual = item.IsVirtual,
                    VerificationCodeIds = string.Join(",", verificationCodeIds),
                    RefundBatchNo = item.RefundBatchNo ?? ""
                };
            }).ToList();
            refundModels.ForEach(o => o.Block(p => p.ContactCellPhone));//收货人手机

            DataGridModel<OrderRefundModel> dataGrid = new DataGridModel<OrderRefundModel>() { rows = refundModels, total = refunds.Total };
            return Json(dataGrid);
        }

        private double GetNextSecond(OrderRefundInfo data)
        {
            double result = -999;
            var siteSetting = SiteSettingApplication.SiteSettings;
            if (data != null)
            {
                if (data.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitAudit ||
                    data.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery ||
                    data.SellerAuditStatus == OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving
                    )
                {
                    int num = 0;
                    DateTime _time = DateTime.Now;
                    switch (data.SellerAuditStatus)
                    {
                        case OrderRefundInfo.OrderRefundAuditStatus.WaitAudit:
                            _time = data.ApplyDate;
                            num = siteSetting.AS_ShopConfirmTimeout;
                            break;
                        case OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery:
                            _time = data.SellerAuditDate;
                            num = siteSetting.AS_SendGoodsCloseTimeout;
                            break;
                        case OrderRefundInfo.OrderRefundAuditStatus.WaitReceiving:
                            _time = data.BuyerDeliverDate.GetValueOrDefault();
                            num = siteSetting.AS_ShopNoReceivingTimeout;
                            break;
                    }
                    TimeSpan ts = (DateTime.Now - _time);
                    if (num > 0)
                    {
                        result = num * 24 * 60 * 60;
                        result = result - ts.TotalSeconds;
                        if (result < 0)
                        {
                            result = -1;
                        }
                    }
                }
            }
            return result;
        }

        [HttpPost]
        public JsonResult ConfirmRefund(long refundId, string managerRemark)
        {
            Result result = new Result();
            string notifyurl = "";

            string webRoot = CurrentUrlHelper.CurrentUrlNoPort();
            //获取异步通知地址
            notifyurl = webRoot + "/Pay/RefundNotify/{0}";

            string refundurl = _RefundService.ConfirmRefund(refundId, managerRemark, CurrentManager.UserName, notifyurl);

            result.success = true;
            if (!string.IsNullOrWhiteSpace(refundurl))
            {
                result.msg = refundurl;
                result.status = 2;   //表示需要继续异步请求
            }

            return Json(result);
        }

        public JsonResult BatchConfirmRefund(string ids, string managerRemark)
        {
            Result result = new Result();
            string notifyurl = "";

            string webRoot = CurrentUrlHelper.CurrentUrlNoPort();
            //获取异步通知地址
            notifyurl = webRoot + "/Pay/RefundNotify/{0}";
            if (string.IsNullOrWhiteSpace(ids))
            {
                return ErrorResult("审核的ID,不能为空");
            }
            var idArray = ids.Split(',').Select(e =>
            {
                long id = 0;
                long.TryParse(e, out id);
                return id;
            }).Where(e => e > 0);

            foreach (long refundId in idArray)
            {
                _RefundService.ConfirmRefund(refundId, managerRemark, CurrentManager.UserName, notifyurl);
            }
            result.success = true;
            return Json(result);
        }
        [HttpPost]
        public JsonResult CheckRefund(long refundId)
        {
            Result result = new Result();
            var model = _RefundService.HasMoneyToRefund(refundId);
            result.success = model;
            return Json(result);
        }

        [HttpPost]
        public JsonResult ConfirmRefundOffLine(long refundId, string managerRemark)
        {
            Result result = new Result();
            string notifyurl = "";

            string refundurl = _RefundService.ConfirmRefund(refundId, managerRemark, CurrentManager.UserName, notifyurl, true);

            result.success = true;
            if (!string.IsNullOrWhiteSpace(refundurl))
            {
                result.msg = refundurl;
                result.status = 2;   //表示需要继续异步请求
            }

            return Json(result);
        }


        public static string HTMLEncode(string txt)
        {
            if (string.IsNullOrEmpty(txt))
                return string.Empty;
            string Ntxt = txt;

            Ntxt = Ntxt.Replace(" ", "&nbsp;");

            Ntxt = Ntxt.Replace("<", "&lt;");

            Ntxt = Ntxt.Replace(">", "&gt;");

            Ntxt = Ntxt.Replace("\"", "&quot;");

            Ntxt = Ntxt.Replace("'", "&#39;");

            //Ntxt = Ntxt.Replace("\n", "<br>");

            return Ntxt;

        }

        /// <summary>
        /// 导出退款记录
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public ActionResult ExportToExcel(RefundQuery query)
        {
            if (query.AuditStatus.HasValue && query.AuditStatus.Value == OrderRefundInfo.OrderRefundAuditStatus.Audited)
                query.ConfirmStatus = Entities.OrderRefundInfo.OrderRefundConfirmStatus.UnConfirm;

            var orderResults = RefundApplication.GetAllFullOrderReFunds(query);

            string strTitle = "退款记录";
            #region 导出时标题名称
            if (query.ShowRefundType.HasValue)
            {
                switch (query.ShowRefundType)
                {
                    case 1:
                        strTitle = "订单退款";
                        break;
                    case 2:
                        strTitle = "退款记录";
                        break;
                    case 3:
                        strTitle = "退货记录";
                        break;
                    case 4:
                        strTitle = "货品退款";
                        break;
                }
            }
            #endregion

            return ExcelView("ExportOrderRefundinfo", "平台" + strTitle, orderResults);
        }
    }
}