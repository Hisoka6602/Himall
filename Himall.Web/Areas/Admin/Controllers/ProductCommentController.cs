using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Web.Mvc;
using Himall.Web.Models;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using Himall.Core;
using Himall.CommonModel;
using Himall.Application;

namespace Himall.Web.Areas.Admin.Controllers
{
    public class ProductCommentController : BaseAdminController
    {
        private CommentService _CommentService;
        private OrderService _OrderService;
        private TypeService _iTypeService;
        public ProductCommentController(OrderService OrderService, CommentService CommentService, TypeService TypeService)
        {
            _OrderService = OrderService;
            _CommentService = CommentService;
            _iTypeService = TypeService;
        }
        public ActionResult Management()
        {
            return View();
        }

        [UnAuthorize]
        public JsonResult List(int page, int rows, string productName, int shopid = 0, bool? isReply = null, int Rank = -1, bool hasAppend = false)
        {
            if (!string.IsNullOrEmpty(productName))
            {
                productName = productName.Trim();
            }
            var orderItemService = _OrderService;
            var TypeService = _iTypeService;
            var query = new CommentQuery() { PageNo = page, PageSize = rows, HasAppend = hasAppend, ProductName = productName, Rank = Rank, ShopID = shopid, IsReply = isReply };
            var result = _CommentService.GetComments(query);
            var orderItems = OrderApplication.GetOrderItems(result.Models.Select(a => a.SubOrderId).ToList()).ToDictionary(item=> item.Id,item => item);
            var comments = result.Models.Select(item => {
                var product = ProductManagerApplication.GetProduct(item.ProductId);
                
                return new ProductCommentModel()
                {
                    CommentContent = item.ReviewContent,
                    CommentDate = item.ReviewDate,
                    ReplyContent = item.ReplyContent,
                    CommentMark = item.ReviewMark,
                    ReplyDate = item.ReplyDate,
                    AppendContent = item.AppendContent,
                    AppendDate = item.AppendDate,
                    ReplyAppendDate = item.ReplyAppendDate,
                    Id = item.Id,
                    ProductName = (product == null) ? "" : product.ProductName,
                    ProductId = item.ProductId,
                    ImagePath = orderItems[item.SubOrderId].ThumbnailsUrl,
                    UserName = item.UserName,
                    OderItemId = item.SubOrderId,
                    Color = "",
                    Version = "",
                    Size = "",
                    IsHidden = item.IsHidden
                };
            }).ToList();
            //TODO LRL 2015/08/06 从评价信息添加商品的规格信息
            foreach (var item in comments)
            {
                string pic = Core.HimallIO.GetProductSizeImage(item.ImagePath, 1, 100);
                if (pic.Contains("skus")) {
                    pic = HimallIO.GetImagePath(item.ImagePath);
                }
                item.ImagePath = pic;
                if (item.OderItemId.HasValue)
                {
                    var obj = orderItemService.GetOrderItem(item.OderItemId.Value);
                    if (obj != null)
                    {
                        item.Color = obj.Color;
                        item.Size = obj.Size;
                        item.Version = obj.Version;
                    }
                }
                Entities.TypeInfo typeInfo = TypeService.GetTypeByProductId(item.ProductId);
                var productInfo = Himall.Application.ProductManagerApplication.GetProduct(item.ProductId);
                item.ColorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                item.SizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                item.VersionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                if (productInfo != null)
                {
                    item.ColorAlias = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : item.ColorAlias;
                    item.SizeAlias = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : item.SizeAlias;
                    item.VersionAlias = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : item.VersionAlias;
                }
            }
            DataGridModel<ProductCommentModel> model = new DataGridModel<ProductCommentModel>() { rows = comments, total = result.Total };
            return Json(model);
        }
        [UnAuthorize]
        [HttpPost]
        public JsonResult Delete(long id)
        {
            _CommentService.HiddenComment(id);
            return Json(new Result() { success = true, msg = "清除成功！" });
        }
        [UnAuthorize]
        [HttpPost]
        public JsonResult Detail(long id)
        {
            var model = _CommentService.GetComment(id);
            return Json(new { ConsulationContent = model.ReviewContent, ReplyContent = model.ReplyContent });
        }

        public ActionResult GetComment(long Id)
        {
            var model = _CommentService.GetComment(Id);
            var commentImages = _CommentService.GetProductCommentImagesByCommentIds(new List<long> { Id });
            foreach (var item in commentImages)
                item.CommentImage = Himall.Core.HimallIO.GetImagePath(item.CommentImage);
            ViewBag.Images = commentImages;
            return View(model);
        }
    }
}
