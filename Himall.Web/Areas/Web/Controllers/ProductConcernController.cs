using Himall.Application;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class ProductConcernController:BaseMemberController
    {
        private ProductService _ProductService;
        public ProductConcernController(ProductService ProductService)
        {
            _ProductService = ProductService;
        }

        public ActionResult Index(int pageSize=10,int pageNo=1)
        {
            var model = _ProductService.GetUserConcernProducts(CurrentUser.Id,pageNo,pageSize);
            PagingInfo info = new PagingInfo
            {
                CurrentPage = pageNo,
                ItemsPerPage = pageSize,
                TotalItems = model.Total
            };
            ViewBag.pageInfo = info;
            ViewBag.Keyword = string.IsNullOrWhiteSpace(SiteSettings.SearchKeyword) ? SiteSettings.Keyword : SiteSettings.SearchKeyword;
            ViewBag.Keywords = SiteSettings.HotKeyWords;
            if (model.Models.Count == 0 && pageNo > 1)
            {//如果当前页没有数据，跳转到前一页
               return Redirect("/ProductConcern/Index?pageNo=" + (pageNo - 1));
            }
            ViewBag.Products = ProductManagerApplication.GetProducts(model.Models.Select(p => p.ProductId));
            ViewBag.SKUs = ProductManagerApplication.GetSKUsByProduct(model.Models.Select(p => p.ProductId));
            //TODO:FG 此实现待优化
            ViewBag.Comments = CommentApplication.GetCommentsByProduct(model.Models.Select(p => p.ProductId));
            return View(model.Models);
        }
        public JsonResult CancelConcernProducts(string ids)
        {
            var strArr = ids.Split(',');
            List<long> listid = new List<long>();
            foreach (var arr in strArr)
            {
                listid.Add(Convert.ToInt64(arr));
            }
            _ProductService.CancelConcernProducts(listid, CurrentUser.Id);
            return Json(new Result() { success = true, msg = "取消成功！" });
        }
    }
}


