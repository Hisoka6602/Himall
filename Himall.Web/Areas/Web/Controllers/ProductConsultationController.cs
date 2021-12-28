using Himall.Application;
using Himall.DTO;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class ProductConsultationController : BaseWebController
    {
        private CommentService _CommentService;
        private ConsultationService _iConsultationService;
        private ProductService _ProductService;
        private LimitTimeBuyService _LimitTimeBuyService;
        public ProductConsultationController(
            CommentService CommentService ,
            ConsultationService ConsultationService ,
            ProductService ProductService ,
            LimitTimeBuyService LimitTimeBuyService
            )
        {
            _CommentService = CommentService;
            _iConsultationService = ConsultationService;
            _ProductService = ProductService;
            _LimitTimeBuyService = LimitTimeBuyService;
        }
        // GET: Web/ProductConsultation
        public ActionResult Index( long id = 0 )
        {
            var productMark = CommentApplication.GetProductAverageMark(id);
            ViewBag.CommentCount = CommentApplication.GetCommentCountByProduct(id);
            ViewBag.productMark = productMark;
            var productinfo = _ProductService.GetProduct(id);
            List<FlashSalePrice> falseSalePrice = _LimitTimeBuyService.GetPriceByProducrIds( new List<long> { id } );
            if( falseSalePrice != null && falseSalePrice.Count == 1 )
            {
                productinfo.MinSalePrice = falseSalePrice[ 0 ].MinPrice;
            }
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(productinfo);
        }

        [HttpPost]
        public JsonResult AddConsultation( string Content , long productId = 0 )
        {
            if( productId == 0 )
            {
                return Json( new Result() { success = false , msg = "咨询失败，该商品不存在或已经删除！" } );
            }
            if( CurrentUser == null )
            {
                return Json( new Result() { success = false , msg = "登录超时，请重新登录！" } );
            }
            Himall.Entities.ProductConsultationInfo model = new Entities.ProductConsultationInfo();
            model.ConsultationContent = Content;
            model.ConsultationDate = DateTime.Now;
            model.ProductId = productId;
            model.UserId = CurrentUser.Id;
            model.UserName = CurrentUser.UserName;
            model.Email = CurrentUser.Email;
            _iConsultationService.AddConsultation( model );
            return Json( new Result() { success = true , msg = "咨询成功" } );
        }
    }
}