using Himall.Application;
using Himall.Service;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class ProductCommentController : BaseController
    {
       private CommentService _CommentService;
       private ProductService _ProductService;
        public ProductCommentController(CommentService CommentService, ProductService ProductService)
       {
           _CommentService = CommentService;
           _ProductService = ProductService;

        }

        // GET: Web/ProductComment
        public ActionResult Index(long id)
        {
            var productMark = CommentApplication.GetProductAverageMark(id);
            ViewBag.CommentCount = CommentApplication.GetCommentCountByProduct(id);
            ViewBag.productMark = productMark;
            var productinfo = _ProductService.GetProduct(id);
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(productinfo);
        }



    }
}