using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using Himall.Web.Framework;
using System.Collections.Generic;
using System.Linq;

namespace Himall.API
{
    public class TopicController : BaseApiController
    {
        public object GetTopicList(int pageNo, int pageSize)
        {
            TopicQuery topicQuery = new TopicQuery();
            topicQuery.ShopId = 0;
            topicQuery.PlatformType = PlatformType.IOS;//IOS表示APP端
            topicQuery.PageNo = pageNo;
            topicQuery.PageSize = pageSize;
            var pagemodel = ServiceProvider.Instance<TopicService>.Create.GetTopics(topicQuery);
            var topics = pagemodel.Models.ToList();
            var model = topics.Select(item => new
            {
                Id = item.Id,
                //TopImage = "http://" + Url.Request.RequestUri.Host + item.TopImage,
                TopImage = Core.HimallIO.GetRomoteImagePath(item.TopImage),
                Name = item.Name
            }
                );
            dynamic result = SuccessResult();
            result.Topic = model;
            result.total = pagemodel.Total;
            return result;
        }
        public object GetTopicDetail(long id)
        {
            var topic = TopicApplication.GetTopic(id);
            var modules = TopicApplication.GetModules(id);
            var allModuleProducts = TopicApplication.GetModuleProducts(modules.Select(p => p.Id));
            var allOnSaleProducts = ProductManagerApplication.GetOnSaleProducts(allModuleProducts.Select(p => p.ProductId).ToList());

            TopicModel model = new TopicModel();
            model.Id = topic.Id;
            model.Name = topic.Name;
            model.TopImage = HimallIO.GetRomoteImagePath(topic.TopImage);
            model.TopicModule = new List<TopicModuleModel>();
            foreach (var module in modules)
            {
                var topicModuleModel = new TopicModuleModel();
                var moduleProducts = allModuleProducts.Where(p => p.ModuleId == module.Id);
                topicModuleModel.Id = module.Id;
                topicModuleModel.Name = module.Name;
                topicModuleModel.TopicModelProduct = allOnSaleProducts
                    .Where(p => moduleProducts.Select(x => x.ProductId).Contains(p.Id))
                    .Select(item => new TopicModuleProductModel
                    {
                        Name = item.ProductName,
                        Id = item.Id,
                        Image = Core.HimallIO.GetRomoteProductSizeImage(item.RelativePath, 1, (int)Himall.CommonModel.ImageSize.Size_350),
                        Price = item.MinSalePrice,
                        MarketPrice = item.MarketPrice
                    }).ToList();

                model.TopicModule.Add(topicModuleModel);
            }
            dynamic result = SuccessResult();
            result.Topic = model;
            return result;
        }

        public object GetLoadProducts(long topicId, long moduleId)
        {
            var topic = TopicApplication.GetTopic(topicId);
            var module = TopicApplication.GetModules(moduleId);
            var products = TopicApplication.GetModuleProducts(moduleId);
            var onSales = ProductManagerApplication.GetOnSaleProducts(products.Select(p => p.ProductId).ToList());
            var model = onSales.Select(item => new TopicModuleProductModel
            {
                Name = item.ProductName,
                Id = item.Id,
                Image = item.GetImage(ImageSize.Size_350),
                Price = item.MinSalePrice,
                MarketPrice = item.MarketPrice
            });
            dynamic result = SuccessResult();
            result.data = model.ToList();
            return result;
        }
    }

}
