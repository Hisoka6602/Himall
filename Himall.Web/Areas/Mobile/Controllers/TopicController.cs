using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Mobile.Controllers
{
    public class TopicController : BaseMobileTemplatesController
    {
        private ProductService _ProductService;
        private TopicService _iTopicService;
        private LimitTimeBuyService _LimitTimeBuyService;
        public TopicController(TopicService TopicService, ProductService ProductService, LimitTimeBuyService LimitTimeBuyService)
        {
            _ProductService = ProductService;
            _iTopicService = TopicService;
            _LimitTimeBuyService = LimitTimeBuyService;
        }
        // GET: Mobile/Topic

        public ActionResult List(int pageNo = 1, int pageSize = 10)
        {
            TopicQuery topicQuery = new TopicQuery();
            topicQuery.ShopId = 0;
            topicQuery.PlatformType = PlatformType.Mobile;
            topicQuery.PageNo = pageNo;
            topicQuery.PageSize = pageSize;
            var topics = _iTopicService.GetTopics(topicQuery).Models;
            return View(topics);
        }

        [HttpPost]
        public JsonResult TopicList(int pageNo = 1, int pageSize = 10)
        {
            TopicQuery topicQuery = new TopicQuery();
            topicQuery.ShopId = 0;
            topicQuery.PlatformType = PlatformType.Mobile;
            topicQuery.PageNo = pageNo;
            topicQuery.PageSize = pageSize;
            var topics = _iTopicService.GetTopics(topicQuery).Models.ToList();
            var model = topics.Select(item => new
            {
                Id = item.Id,
                TopImage = item.TopImage,
                Name = item.Name
            }
                );
            return SuccessResult<dynamic>(data: model);
        }
        public ActionResult Detail(long id)
        {
            var topic = TopicApplication.GetTopic(id);
            string tmppath = VTemplateHelper.GetTemplatePath("", VTemplateClientTypes.WapSpecial);
            tmppath = "~" + tmppath;
            string viewpath = tmppath + "Skin-HomePage.cshtml";
            if (topic != null)
            {//判空处理
                //ViewBag.Title = "专题-" + topic.Name;
                ViewBag.Title = topic.Name;
            }
            else
            {
                throw new Himall404();
            }
            VTemplateHelper.DownloadTemplate(id.ToString(), VTemplateClientTypes.WapSpecial);
            ViewBag.ShopId = topic.ShopId;
            var vshop = VshopApplication.GetVShopByShopId(topic.ShopId);
            ViewBag.VshopId = vshop == null ? 0 : vshop.Id;
            ViewBag.TopId = id;
            return View(viewpath, topic);
        }

        [HttpPost]
        public JsonResult GetUserShippingAddressesList(long topicId, long moduleId, int page, int pageSize)
        {
            var topic = TopicApplication.GetTopic(topicId);
            var module = TopicApplication.GetModules(moduleId);
            var products = TopicApplication.GetModuleProducts(moduleId);
            var onSales = ProductManagerApplication.GetOnSaleProducts(products.Select(p => p.ProductId).ToList())
                            .Skip(pageSize * (page - 1))
                            .Take(pageSize);//TODO:FG 数据分页 业务需要降层。

            var model = onSales.Select(item =>
                {
                    var flashSaleModel = _LimitTimeBuyService.GetFlaseSaleByProductId(item.Id);
                    return new
                    {
                        name = item.ProductName,
                        id = item.Id,
                        image = item.GetImage(ImageSize.Size_350),
                        price = flashSaleModel != null ? flashSaleModel.MinPrice : item.MinSalePrice,
                        marketPrice = item.MarketPrice
                    };
                });
            return SuccessResult<dynamic>(data: model);
        }

    }
}