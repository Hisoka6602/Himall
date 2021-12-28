using Himall.Core;
using Himall.Service;
using Himall.DTO.QueryModel;
using Himall.Web.Framework;
using System.Linq;
using System.Web.Mvc;
using Himall.Application;
using Himall.DTO;
using Himall.CommonModel;

namespace Himall.Web.Areas.Admin.Controllers
{
    [MarketingAuthorization]
    public class MobileTopicController : BaseAdminController
    {
        private TopicService _iTopicService;

        public MobileTopicController(TopicService TopicService)
        {
            _iTopicService = TopicService;
        }
        // GET: Admin/MobileTopic
        public ActionResult Management()
        {
            return View();
        }

        [HttpPost]
        [UnAuthorize]
        public JsonResult List(int page, int rows, string titleKeyword, string tagsKeyword, PlatformType? platformType)
        {
            TopicQuery query = new TopicQuery()
            {
                IsAsc = false,
                PageSize = rows,
                PageNo = page,
                Name = titleKeyword,
                Tags = tagsKeyword,
                PlatformType = platformType,
                MorePlatForm = new System.Collections.Generic.List<PlatformType> { PlatformType.Mobile, PlatformType.WeiXinSmallProg, PlatformType.IOS },
                ShopId = CurrentManager.ShopId,
                Sort = "id"
            };

            var topics = _iTopicService.GetTopics(query);
            var curUrl = CurrentUrlHelper.CurrentUrlNoPort();
            var list = new
            {
                rows = topics.Models.ToArray().Select(item => new
                {
                    id = item.Id,
                    name = item.Name,
                    imgUrl = item.FrontCoverImage,
                    url = curUrl + "/m-wap/topic/detail/" + item.Id,
                    tags = string.IsNullOrWhiteSpace(item.Tags) ? "" : item.Tags.Replace(",", " "),
                    platform = item.PlatForm == PlatformType.Mobile ? "微信端" : (item.PlatForm == PlatformType.WeiXinSmallProg ? "小程序" : "APP端"),
                    client=item.PlatForm== PlatformType.Mobile? (int)VTemplateClientTypes.WapSpecial: (item.PlatForm == PlatformType.WeiXinSmallProg ? (int)VTemplateClientTypes.WXSmallProgramSpecial : (int)VTemplateClientTypes.AppSpecial)
                }),
                total = topics.Total
            };
            return Json(list);
        }


        public ActionResult Save(long id = 0)
        {
            var topic = new Entities.TopicInfo();
            if (id > 0) topic = TopicApplication.GetTopic(id);
           

            var modules = TopicApplication.GetModules(id);
            var products = TopicApplication.GetModuleProducts(modules.Select(p => p.Id));
            var topicModel = new Models.TopicModel()
            {
                Id = topic.Id,
                Name = topic.Name,
                TopImage = topic.TopImage,
                TopicModuleInfo = modules,
                ModuleProducts = products,
                Tags = topic.Tags,
            };

            return View(topicModel);
        }

        [HttpPost]
        public JsonResult Add(string topicJson)
        {
            var s = new Newtonsoft.Json.JsonSerializerSettings();
            s.MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore;
            s.NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore;
            var topic = Newtonsoft.Json.JsonConvert.DeserializeObject<Topic>(topicJson, s);
            var oriTopic = TopicApplication.GetTopic(topic.Id);
            topic.Platform = PlatformType.Mobile;
            topic.BackgroundImage = oriTopic?.BackgroundImage ?? string.Empty;
            topic.FrontCoverImage = oriTopic?.FrontCoverImage ?? string.Empty;

            if (topic.Id == 0)
                _iTopicService.AddTopic(topic);
            else
                _iTopicService.UpdateTopic(topic);

            return Json(new { success = true });
        }


        [UnAuthorize]
        [HttpPost]
        public JsonResult Delete(long id)
        {
            Result result = new Result();
            _iTopicService.DeleteTopic(id);
            result.success = true;
            return Json(result);
        }


    }
}