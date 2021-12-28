using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class OrderComplaintController : BaseMemberController
    {
        private OrderService _OrderService;
        private ShopService _ShopService;
        private ComplaintService _iComplaintService;
        private TypeService _iTypeService;

        public OrderComplaintController(OrderService OrderService, ShopService ShopService, ComplaintService ComplaintService, TypeService TypeService)
        {
            _OrderService = OrderService;
            _ShopService = ShopService;
            _iComplaintService = ComplaintService;
            _iTypeService = TypeService;
        }

        public ActionResult Index(int pageSize = 10, int pageNo = 1)
        {
            OrderQuery query = new OrderQuery();
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            query.UserId = CurrentUser.Id;
            query.Status = OrderInfo.OrderOperateStatus.Finish;
            var orders = _OrderService.GetOrders(query);
            var complaints = OrderApplication.GetOrderComplaintByOrders(orders.Models.Select(p => p.Id).ToList());
            var model = orders.Models.Where(o => !complaints.Any(p => p.OrderId == o.Id));
            ViewBag.Complaints = complaints;
            var orderItems = _OrderService.GetOrderItemsByOrderId(orders.Models.Select(p => p.Id));
            if (orderItems != null)
            {
                foreach (var item in orderItems)
                {
                    Entities.TypeInfo typeInfo = _iTypeService.GetTypeByProductId(item.ProductId);
                    var productInfo = Himall.Application.ProductManagerApplication.GetProduct(item.ProductId);
                    item.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                    item.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                    item.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                }
            }
            ViewBag.OrderItems = orderItems;
            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = orders.Total
            };
            ViewBag.pageInfo = info;
            ViewBag.UserPhone = CurrentUser.CellPhone;
            ViewBag.UserId = CurrentUser.Id;
            #endregion
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            return View(model);
        }

        [HttpPost]
        public JsonResult AddOrderComplaint(Himall.Entities.OrderComplaintInfo model)
        {
            model.UserId = CurrentUser.Id;
            model.UserName = CurrentUser.UserName;
            model.ComplaintDate = DateTime.Now;
            model.Status = Himall.Entities.OrderComplaintInfo.ComplaintStatus.WaitDeal;
            var shop = _ShopService.GetShop(model.ShopId);
            var order = _OrderService.GetOrder(model.OrderId, CurrentUser.Id);
            if (model.ComplaintReason.Length < 5)
            {
                throw new HimallException("投诉内容不能小于5个字符！");
            }
            if (model.ComplaintReason.Length > 500)
            {
                throw new HimallException("字数过长，限制500个字！");
            }
            if (string.IsNullOrWhiteSpace(model.UserPhone))
            {
                throw new HimallException("投诉电话不能为空！");
            }
            if (order == null || order.ShopId != model.ShopId)
            {
                throw new HimallException("该订单不属于当前用户！");
            }
            model.ShopName = shop == null ? "" : shop.ShopName;
            model.ShopPhone = shop == null ? "" : shop.CompanyPhone;
            if (model.ShopPhone == null)
            {
                //管理员信息
                long uid = Himall.Application.ShopApplication.GetShopManagers(shop.Id);
                Himall.DTO.MemberAccountSafety mMemberAccountSafety = Himall.Application.MemberApplication.GetMemberAccountSafety(uid);
                model.ShopPhone = mMemberAccountSafety.Phone;
            }
            model.ShopName = model.ShopName == null ? "" : model.ShopName;

            _iComplaintService.AddComplaint(model);
            return Json(new { success = true, msg = "提交成功" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Record(int pageSize = 10, int pageNo = 1)
        {
            ComplaintQuery query = new ComplaintQuery();
            query.UserId = CurrentUser.Id;
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            var model = _iComplaintService.GetOrderComplaints(query);
            #region 分页控制
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = model.Total
            };
            ViewBag.pageInfo = info;
            #endregion
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            if (model.Models != null)
            {
                foreach (var item in model.Models)
                {
                    item.ComplaintReason = ReplaceHtmlTag(Server.HtmlDecode(item.ComplaintReason));
                    item.SellerReply = ReplaceHtmlTag(Server.HtmlDecode(item.SellerReply));
                }
            }
            return View(model.Models.ToList());
        }
        [HttpPost]
        public JsonResult ApplyArbitration(long id)
        {
            _iComplaintService.UserApplyArbitration(id, CurrentUser.Id);
            return Json(new { success = true, msg = "处理成功" });
        }
        [HttpPost]
        public JsonResult DealComplaint(long id)
        {
            _iComplaintService.UserDealComplaint(id, CurrentUser.Id);
            return Json(new { success = true, msg = "处理成功" });
        }

        public static string ReplaceHtmlTag(string html, int length = 0)
        {
            string strText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            strText = System.Text.RegularExpressions.Regex.Replace(strText, "&[^;]+;", "");

            if (length > 0 && strText.Length > length)
                return strText.Substring(0, length);

            return strText;
        }

    }
}