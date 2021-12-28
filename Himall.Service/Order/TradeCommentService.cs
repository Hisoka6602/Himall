using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Service
{
    public class TradeCommentService : ServiceBase
    {
        public QueryPageModel<OrderCommentInfo> GetOrderComments(OrderCommentQuery query)
        {
            var orderComments = DbFactory.Default.Get<OrderCommentInfo>();

            #region 条件组合
            if (query.OrderId.HasValue)
            {
                orderComments.Where(item => query.OrderId == item.OrderId);
            }
            if (query.StartDate.HasValue)
            {
                orderComments.Where(item => item.CommentDate >= query.StartDate.Value);
            }
            if (query.EndDate.HasValue)
            {

                var end = query.EndDate.Value.Date.AddDays(1);
                orderComments.Where(item => item.CommentDate < end);
            }
            if (query.ShopId.HasValue)
            {
                orderComments.Where(item => query.ShopId == item.ShopId);
            }
            if (query.UserId.HasValue)
            {
                orderComments.Where(item => query.UserId == item.UserId);
            }
            if (!string.IsNullOrWhiteSpace(query.ShopName))
            {
                orderComments.Where(item => item.ShopName.Contains(query.ShopName));
            }
            if (!string.IsNullOrWhiteSpace(query.UserName))
            {
                orderComments.Where(item => item.UserName.Contains(query.UserName));
            }
            #endregion

            var rst = orderComments.OrderByDescending(o => o.Id).ToPagedList(query.PageNo, query.PageSize);

            QueryPageModel<OrderCommentInfo> pageModel = new QueryPageModel<OrderCommentInfo>() { Models = rst, Total = rst.TotalRecordCount };
            return pageModel;
        }

        public void DeleteOrderComment(long Id)
        {
            OrderCommentInfo ociobj = DbFactory.Default.Get<OrderCommentInfo>().Where(p => p.Id == Id).FirstOrDefault();
            if (ociobj != null)
            {
                //删除相关信息
                List<long> orditemid = DbFactory.Default
                    .Get<OrderItemInfo>()
                    .Where(d => d.OrderId == ociobj.OrderId)
                    .Select(d => d.Id)
                    .ToList<long>();
                DbFactory.Default.InTransaction(() =>
                {
                    DbFactory.Default.Del<ProductCommentInfo>(d => d.SubOrderId.ExIn(orditemid));
                    //删除订单评价
                    DbFactory.Default.Del(ociobj);
                });
            }
        }

        public void AddOrderComment(OrderCommentInfo info, int productNum)
        {
            var order = DbFactory.Default.Get<OrderInfo>().Where(a => a.Id == info.OrderId && a.UserId == info.UserId).FirstOrDefault();
            if (order == null)
            {
                throw new HimallException("该订单不存在，或者不属于该用户！");
            }
            var orderComment = DbFactory.Default.Get<OrderCommentInfo>().Where(a => a.OrderId == info.OrderId && a.UserId == info.UserId);
            if (orderComment.Count() > 0)
                throw new HimallException("您已经评论过该订单！");
            info.ShopId = order.ShopId;
            info.ShopName = order.ShopName;
            info.UserName = order.UserName;
            info.CommentDate = DateTime.Now;
            info.OrderId = order.Id;
            DbFactory.Default.Add(info);

            Himall.Entities.MemberIntegralRecordInfo record = new Himall.Entities.MemberIntegralRecordInfo();
            record.UserName = info.UserName;
            record.ReMark = "订单号:" + info.OrderId;
            record.MemberId = info.UserId;
            record.RecordDate = DateTime.Now;
            record.TypeId = Himall.Entities.MemberIntegralInfo.IntegralType.Comment;
            Himall.Entities.MemberIntegralRecordActionInfo action = new Himall.Entities.MemberIntegralRecordActionInfo();
            action.VirtualItemTypeId = Himall.Entities.MemberIntegralInfo.VirtualItemType.Comment;
            action.VirtualItemId = info.OrderId;
            record.MemberIntegralRecordActionInfo.Add(action);
            var memberIntegral = ServiceProvider.Instance<MemberIntegralConversionFactoryService>.Create.Create(Himall.Entities.MemberIntegralInfo.IntegralType.Comment);
            if (memberIntegral != null)
            {
                record.Integral = productNum * memberIntegral.ConversionIntegral();
            }
            ServiceProvider.Instance<MemberIntegralService>.Create.AddMemberIntegral(record, null);
        }

        public OrderCommentInfo GetOrderCommentInfo(long orderId, long userId)
        {
            return DbFactory.Default.Get<OrderCommentInfo>().Where(a => a.UserId == userId && a.OrderId == orderId).FirstOrDefault();
        }
        public List<OrderCommentInfo> GetOrderCommentsByOrder(long orderId)
        {
            return DbFactory.Default.Get<OrderCommentInfo>().Where(a => a.OrderId == orderId).ToList();
        }

        public List<OrderCommentsModel> GetCommentSummary()
        {
            var result = DbFactory.Default.Get<OrderCommentInfo>()
                      .GroupBy(p => p.ShopId)
                      .Select(p => new
                      {
                          ShopId = p.ShopId,
                          //AvgDeliveryMark = p.DeliveryMark.ExAvg(),
                          AvgPackMark = p.PackMark.ExAvg(),
                          AvgServiceMark = p.ServiceMark.ExAvg(),
                      }).ToList<OrderCommentsModel>();
            if (result.Count == 0) 
                return result;
            //重新计算物流评分，排除了该项为0的数据。因为虚拟订单物流评分是0，但不能参与统计，故重新计算一次排除掉为0的物流评分
            var delivery = DbFactory.Default.Get<OrderCommentInfo>().Where(a => a.DeliveryMark > 0)
                    .GroupBy(p => p.ShopId)
                    .Select(p => new { p.ShopId, AvgDeliveryMark = p.DeliveryMark.ExAvg() })
                    .ToList<OrderCommentsModel>();

            foreach (var item in result)
            {
                var update = delivery.FirstOrDefault(a => a.ShopId == item.ShopId);
                if (update != null)
                {
                    item.AvgDeliveryMark = update.AvgDeliveryMark;
                }
            }
            return result;
        }

        public void Save(List<StatisticOrderCommentInfo> summary)
        {
            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in summary)
                {
                    var exists = DbFactory.Default.Get<StatisticOrderCommentInfo>().Where(c => c.ShopId == item.ShopId && c.CommentKey == item.CommentKey).FirstOrDefault();
                    if (exists == null)
                    {
                        var shop = DbFactory.Default.Get<ShopInfo>().Where(c => c.Id == item.ShopId).FirstOrDefault();
                        if (shop != null)
                        {
                            DbFactory.Default.Add(item);
                        }
                    }
                    else
                    {
                        DbFactory.Default.Set<StatisticOrderCommentInfo>().Set(p => p.CommentValue, item.CommentValue).Where(p => p.Id == exists.Id).Succeed();
                    }
                }
            });
            
        }
    }
}
