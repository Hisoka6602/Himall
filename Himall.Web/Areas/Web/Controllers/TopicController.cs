using Himall.Application;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using Himall.Web.Models;
using System.Linq;
using System.Web.Mvc;

namespace Himall.Web.Areas.Web.Controllers
{
    public class TopicController : BaseController
    {
        private TopicService _iTopicService;
        public TopicController(TopicService TopicService)
        {
            _iTopicService = TopicService;
        }
        // GET: Web/Topic
        public ActionResult Detail(long id)
        {
            var topic = TopicApplication.GetTopic(id);
            if (topic == null)
            {
                //404页面
            }
            var model = new TopicViewModel();
            model.Topic = topic;
            model.Modules = TopicApplication.GetModules(id);
            model.ModuleProducts = TopicApplication.GetModuleProducts(model.Modules.Select(p => p.Id));
            model.Products = ProductManagerApplication.GetProducts(model.ModuleProducts.Select(p => p.ProductId));
            ViewBag.Keyword = SiteSettings.Keyword;
            return View(model);
        }

        public ActionResult List()
        {
            TopicQuery topicQuery = new TopicQuery()
            {
                IsRecommend = true,
                PlatformType = Core.PlatformType.PC,
                PageNo = 1,
                PageSize = 5
            };

            var pagemodel = _iTopicService.GetTopics(topicQuery);
            ViewBag.TopicInfo = pagemodel.Models.ToList();
            ViewBag.Keyword = SiteSettings.Keyword;
            return View();
        }
        [HttpPost]
        public JsonResult List(int page,int pageSize)
        {
            TopicQuery topicQuery = new TopicQuery()
            {
                IsRecommend = true,
                PlatformType = Core.PlatformType.PC,
                PageNo = page,
                PageSize = 5
            };

            var pagemodel = _iTopicService.GetTopics(topicQuery);
            var model = pagemodel.Models.ToList().Select(item => new {id=item.Id, name = item.Name, topimage = item.TopImage });
            return Json(new { success = true, data = model });
        }
    }
}