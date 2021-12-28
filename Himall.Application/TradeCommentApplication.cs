using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Application
{
    public class TradeCommentApplication
    {
        private static TradeCommentService _tradeCommentService = ObjectContainer.Current.Resolve<TradeCommentService>();
        private static ShopService _shopService = ObjectContainer.Current.Resolve<ShopService>();

        /// <summary>
        /// 根据用户ID和订单ID获取单个订单评价信息
        /// </summary>
        /// <param name="?"></param>
        /// <returns></returns>
        public static DTO.OrderComment GetOrderComment(long orderId, long userId)
        {
            return _tradeCommentService.GetOrderCommentInfo(orderId, userId).Map<DTO.OrderComment>();
        }

        /// <summary>
        /// 查询订单评价
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static QueryPageModel<Himall.Entities.OrderCommentInfo> GetOrderComments(long shopId)
        {
            return _tradeCommentService.GetOrderComments(new OrderCommentQuery
            {
                ShopId = shopId,
                PageNo = 1,
                PageSize = 100000
            });
        }

        public static void Add(DTO.OrderComment model,int productNum)
        {
            var info = model.Map<Himall.Entities.OrderCommentInfo>();
            _tradeCommentService.AddOrderComment(info, productNum);
            model.Id = info.Id;
        }
        public static Himall.Entities.OrderCommentInfo GetOrderCommentInfo(long orderId, long userId)
        {
            return _tradeCommentService.GetOrderCommentInfo(orderId, userId);
        }

        /// <summary>
        /// 订单评价汇总
        /// </summary>
        public static void CommentStatistics() {
            var summarys = _tradeCommentService.GetCommentSummary();
            foreach (var item in summarys)
                item.CategoryIds = _shopService.GetCategories(item.ShopId);

            var result = new List<StatisticOrderCommentInfo>();
            foreach (var item in summarys)
            {
                //获取同行业的店铺
                List<OrderCommentsModel> peerShops = new List<OrderCommentsModel>();
                foreach (var cId in item.CategoryIds)
                {
                    var shops = summarys.Where(c => c.CategoryIds.Contains(cId)).Select(c => c);
                    if (shops != null && shops.Count() > 0)
                    {
                        peerShops.AddRange(shops);
                    }
                }
                var avgPackMarkPeerShops = peerShops.Count != 0 ? peerShops.Average(c => c.AvgPackMark) : 0d;
                var avgDeliveryMarkPeerShops = peerShops.Count != 0 ? peerShops.Average(c => c.AvgDeliveryMark) : 0d;
                var avgServiceMarkPeerShops = peerShops.Count != 0 ? peerShops.Average(c => c.AvgServiceMark) : 0d;

                var productAndDescriptionMax = peerShops.Count != 0 ? summarys.Where(c => peerShops.Where(o => o.ShopId == c.ShopId).Count() > 0).Max(c => c.AvgPackMark) : 0;
                var productAndDescriptionMin = peerShops.Count != 0 ? summarys.Where(c => peerShops.Where(o => o.ShopId == c.ShopId).Count() > 0).Min(c => c.AvgPackMark) : 0;
                var sellerServiceAttitudeMax = peerShops.Count != 0 ? summarys.Where(c => peerShops.Where(o => o.ShopId == c.ShopId).Count() > 0).Max(c => c.AvgServiceMark) : 0;
                var sellerServiceAttitudeMin = peerShops.Count != 0 ? summarys.Where(c => peerShops.Where(o => o.ShopId == c.ShopId).Count() > 0).Min(c => c.AvgServiceMark) : 0;
                var sellerDeliverySpeedMax = peerShops.Count != 0 ? summarys.Where(c => peerShops.Where(o => o.ShopId == c.ShopId).Count() > 0).Max(c => c.AvgDeliveryMark) : 0;
                var sellerDeliverySpeedMin = peerShops.Count != 0 ? summarys.Where(c => peerShops.Where(o => o.ShopId == c.ShopId).Count() > 0).Min(c => c.AvgDeliveryMark) : 0;

                

                //宝贝与描述相符
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescription,
                    CommentValue = (decimal)item.AvgPackMark
                });

                //宝贝与描述相符 同行比较
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescriptionPeer,
                    CommentValue = (decimal)avgPackMarkPeerShops
                });

                //宝贝与描述相符 同行业商家最高得分
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescriptionMax,
                    CommentValue = (decimal)productAndDescriptionMax
                });

                //宝贝与描述相符 同行业商家最低得分
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.ProductAndDescriptionMin,
                    CommentValue = (decimal)productAndDescriptionMin
                });

                //卖家的服务态度 
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitude,
                    CommentValue = (decimal)item.AvgServiceMark
                });

                //卖家的服务态度  同行业比对
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitudePeer,
                    CommentValue = (decimal)avgServiceMarkPeerShops
                });

                //卖家服务态度 同行业商家最高得分
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitudeMax,
                    CommentValue = (decimal)sellerServiceAttitudeMax
                });
                //卖家服务态度 同行业商家最低得分
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerServiceAttitudeMin,
                    CommentValue = (decimal)sellerServiceAttitudeMin
                });

                //卖家的发货速度 
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeed,
                    CommentValue = (decimal)item.AvgDeliveryMark
                });
                //卖家的发货速度  同行业比对
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeedPeer,
                    CommentValue = (decimal)avgDeliveryMarkPeerShops
                });
                //卖家发货速度 同行业商家最高得分
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeedMax,
                    CommentValue = (decimal)sellerDeliverySpeedMax
                });
                //卖家发货速度 同行业商家最低得分
                result.Add(new StatisticOrderCommentInfo
                {
                    ShopId = item.ShopId,
                    CommentKey = StatisticOrderCommentInfo.EnumCommentKey.SellerDeliverySpeedMin,
                    CommentValue = (decimal)sellerDeliverySpeedMin
                });
            }
            _tradeCommentService.Save(result);
        }
    }
}
