using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Himall.Application
{
    public class CommentApplication:BaseApplicaion<CommentService>
    {
        /// <summary>
        /// 获取用户订单中商品的评价列表
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<ProductEvaluation> GetProductEvaluationByOrderId(long orderId, long userId)
        {
            return Service.GetProductEvaluationByOrderId(orderId, userId);
        }

        public static List<ProductEvaluation> GetProductEvaluationByOrderIdNew(long orderId, long userId)
        {
            return Service.GetProductEvaluationByOrderIdNew(orderId, userId);
        }
        /// <summary>
        /// 根据评论ID取评论
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Entities.ProductCommentInfo GetComment(long id)
        {
            return Service.GetComment(id);
        }
        public static int GetCommentCountByProduct(long product)
        {
            //TODO:FG 此方法调用需要优化
            var query = new CommentQuery
            {
                ProductID = product,
                IsHidden = false,
            };
            return Service.GetCommentCount(query);
        }

        public static List<ProductCommentInfo> GetCommentsByProduct(long product, long shopbranchId = 0)
        {
            return Service.GetCommentByProduct(product, shopbranchId).Where(p => !p.IsHidden).ToList();
        }

        /// <summary>
        /// 评论统计
        /// </summary>
        /// <param name="productId">商品id</param>
        /// <param name="shopbranchId">门店id</param>
        /// <returns></returns>
        public static CommentSummaryData GetSummary(long productId, long shopbranchId = 0)
        {
            return Service.GetCommentSummary(productId, shopbranchId);
        }

        public static List<ProductCommentInfo> GetCommentsByProduct(IEnumerable<long> products)
        {
            return Service.GetCommentByProduct(products.ToList()).Where(p => !p.IsHidden).ToList();
        }


        public static List<ProductCommentInfo> GetCommentss(IEnumerable<long> ids)
        {
            return Service.GetCommentsByIds(ids).ToList();
        }
        public static void Add(ProductComment comment)
        {
            var info = comment.Map<ProductCommentInfo>();
            Service.AddComment(info);
            comment.Id = info.Id;
        }
        public static void AddComment(ProductCommentInfo model)
        {
            Service.AddComment(model);
        }

        public static void Add(IEnumerable<ProductComment> comments)
        {
            var list = comments.ToList().Map<List<ProductCommentInfo>>();
            Service.AddComment(list);
        }

        /// <summary>
        /// 追加评论
        /// </summary>
        public static void Append(List<AppendCommentModel> models)
        {
            Service.AppendComment(models);
        }

        /// <summary>
        /// 获取商品评价列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<ProductComment> GetProductComments(ProductCommentQuery query)
        {
            var datalist = Service.GetProductComments(query);
            foreach(var item in datalist.Models)
            {
                item.ProductCommentImageInfo = GetProductCommentImagesByCommentIds(new List<long> { item.Id });
            }
            return new QueryPageModel<ProductComment>
            {
                Total = datalist.Total,
                Models = AutoMapper.Mapper.Map<List<ProductCommentInfo>, List<ProductComment>>(datalist.Models)
            };
        }

        public static QueryPageModel<ProductCommentInfo> GetComments(ProductCommentQuery query)
        {
            return Service.GetProductComments(query);
        }

        /// <summary>
        /// 商品评论整理返回
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<ProductCommentModelDTO> GetCommentList(ProductCommentQuery query)
        {
            var result = Service.GetProductComments(query);

            var comments = result.Models;
            var orderitems = OrderApplication.GetOrderItems(comments.Select(p => (long)p.SubOrderId));
            var orders = OrderApplication.GetOrders(orderitems.Select(p => p.OrderId));
            var commentImages = CommentApplication.GetProductCommentImagesByCommentIds(comments.Select(p => p.Id));
            var members = MemberApplication.GetMembers(comments.Select(p => p.UserId).ToList());

            var data = comments.Select(item =>
            {
                var orderitem = orderitems.FirstOrDefault(p => p.Id == item.SubOrderId);
                var order = orders.FirstOrDefault(p => p.Id == orderitem.OrderId);
                var cimages = commentImages.Where(a => a.CommentId == item.Id);
                var typeInfo = GetService<TypeService>().GetTypeByProductId(item.ProductId);
                var meminfo = members.FirstOrDefault(p => p.Id == item.UserId);

                string colorAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.ColorAlias)) ? SpecificationType.Color.ToDescription() : typeInfo.ColorAlias;
                string sizeAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.SizeAlias)) ? SpecificationType.Size.ToDescription() : typeInfo.SizeAlias;
                string versionAlias = (typeInfo == null || string.IsNullOrEmpty(typeInfo.VersionAlias)) ? SpecificationType.Version.ToDescription() : typeInfo.VersionAlias;
                var productInfo = ProductManagerApplication.GetProduct(item.ProductId);
                if (productInfo != null)
                {
                    colorAlias = !string.IsNullOrWhiteSpace(productInfo.ColorAlias) ? productInfo.ColorAlias : colorAlias;
                    sizeAlias = !string.IsNullOrWhiteSpace(productInfo.SizeAlias) ? productInfo.SizeAlias : sizeAlias;
                    versionAlias = !string.IsNullOrWhiteSpace(productInfo.VersionAlias) ? productInfo.VersionAlias : versionAlias;
                }

                string strShowName = item.UserName;
                if (meminfo != null)
                    strShowName = !string.IsNullOrEmpty(meminfo.Nick) ? meminfo.Nick : meminfo.UserName;

                return new ProductCommentModelDTO
                {
                    Picture = (meminfo != null && !string.IsNullOrEmpty(meminfo.Photo)) ? HimallIO.GetRomoteImagePath(meminfo.Photo) : string.Empty,
                    UserName = GetNamestrAsterisk(strShowName),
                    ReviewContent = item.ReviewContent,
                    AppendContent = item.AppendContent,
                    AppendDate = item.AppendDate.HasValue ? item.AppendDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    ReplyAppendContent = item.ReplyAppendContent,
                    ReplyAppendDate = item.ReplyAppendDate.HasValue ? item.ReplyAppendDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                    FinishDate = order != null && order.FinishDate.HasValue ? order.FinishDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                    Images = cimages == null ? null : cimages.Where(a => a.CommentType == 0).ToList(),
                    AppendImages = cimages == null ? null : cimages.Where(a => a.CommentType == 1).ToList(),
                    ReviewDate = item.ReviewDate.ToString("yyyy-MM-dd HH:mm:ss"),
                    ReplyContent = item.ReplyContent,
                    ReplyDate = item.ReplyDate.HasValue ? item.ReplyDate.Value.ToString("yyyy-MM-dd HH:mm:ss") : " ",
                    ReviewMark = item.ReviewMark,
                    BuyDate = order == null ? order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss") : string.Empty,
                    Color = orderitem?.Color ?? string.Empty,
                    Version = orderitem?.Version ?? string.Empty,
                    Size = orderitem?.Size ?? string.Empty,
                    ColorAlias = colorAlias,
                    SizeAlias = sizeAlias,
                    VersionAlias = versionAlias
                };
            }).ToList();

            QueryPageModel<ProductCommentModelDTO> querypage = new QueryPageModel<ProductCommentModelDTO>();
            querypage.Total = result.Total;
            querypage.Models = data;

            return querypage;
        }

        public static bool IsZHCN(string data)
        {
            Regex RegChinese = new Regex(@"^[\u4e00-\u9fa5]+$", RegexOptions.IgnoreCase);
            return RegChinese.IsMatch(data);
        }

        /// <summary>
        /// 名称星号处理，比如abcd，改为“ab*d”
        /// </summary>
        /// <param name="strShowName"></param>
        /// <returns></returns>
        private static string GetNamestrAsterisk(string strShowName)
        {
            string rsult = strShowName+"***";
            int len = strShowName.Length;
            if (len > 2) {
                rsult=strShowName.Substring(0,2) + "****";
            }
            return rsult;
        }

        public static List<ProductCommentImageInfo> GetProductCommentImagesByCommentIds(IEnumerable<long> commentIds)
        {
            return Service.GetProductCommentImagesByCommentIds(commentIds);
        }

        public static int GetCommentCount(ProductCommentQuery query) {
            return Service.GetCommentCount(query);
        }
        /// <summary>
        /// 获取商品评价数量聚合
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static ProductCommentCountAggregateModel GetProductCommentStatistic(long? productId = null, long? shopId = null, long? shopBranchId = null,bool iso2o = false)
        {
            return Service.GetProductCommentStatistic(productId, shopId, shopBranchId, iso2o: iso2o);
        }
        /// <summary>
        /// 获取商品好评评价数量
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static List<long> GetProductCommentHightStatisticList(List<long> productIds, long shopId, long? shopBranchId = null, bool iso2o = false)
        {
            return Service.GetProductCommentHightStatisticList(productIds, shopId, shopBranchId, iso2o: iso2o);
        }
        /// <summary>
        /// 获取商品平均评分
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public static decimal GetProductAverageMark(long product)
        {
            return Service.GetProductAverageMark(product);
        }

        public static int GetProductCommentCount(long product) {
            return Service.GetProductCommentCount(product);
        }
        /// <summary>
        /// 获取商品评价好评数
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="shopId"></param>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public static int GetProductHighCommentCount(long? productId = null, long? shopId = null, long? shopBranchId = null)
        {
            return Service.GetProductHighCommentCount(productId, shopId, shopBranchId);
        }
        /// <summary>
        /// 订单列表项判断有没有追加评论
        /// </summary>
        /// <param name="subOrderId"></param>
        /// <returns></returns>
        public static bool HasAppendComment(long subOrderId)
        {
            return Service.HasAppendComment(subOrderId);
        }

        public static List<OrderCommentInfo> GetOrderCommentByOrder(IEnumerable<long> orders) {
           return  GetService<CommentService>().GetOrderCommentsByOrder(orders);
        }

        /// <summary>
        /// 是否显示绑定手机号页面
        /// </summary>
        /// <returns></returns>
        public static bool IsShowBindCellPhone(long userId)
        {
            var setting = SiteSettingApplication.SiteSettings;
            var IsBind = false;
            if (setting.IsConBindCellPhone)
            {
                var member = MemberApplication.GetMembers(userId);
                IsBind = member != null ? (string.IsNullOrEmpty(member.CellPhone) ? true : false) : false;
            }

            return IsBind;
        }
    }
}
