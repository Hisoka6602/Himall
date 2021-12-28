using Himall.Application;
using Himall.Core;
using Himall.Core.Plugins.Payment;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.CommonModel;
using Himall.Entities;
using Himall.Web.Framework;
using Senparc.Weixin.MP.CommonAPIs;
using System;
using System.Linq;
using System.Web.Http.Results;
using Himall.Core.Plugins.Message;
using System.Collections.Generic;
using Himall.DTO.Live;

namespace Himall.SmallProgAPI
{
    public class LiveController : BaseApiController
    {
        /// <summary>
        /// 直播首页列表
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>

        public Result<dynamic> GetLiveHomeList(int pageNo, int pageSize,long shopId=0)
        {
            var query = new LiveQuery();
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            query.StatusList.Add(LiveRoomStatus.Pause.GetHashCode());
            query.StatusList.Add(LiveRoomStatus.End.GetHashCode());
            query.StatusList.Add(LiveRoomStatus.Living.GetHashCode());
            query.StatusList.Add(LiveRoomStatus.NotStart.GetHashCode());
            query.StatusList.Add(LiveRoomStatus.Pause.GetHashCode());
            if (shopId > 0) {
                query.ShopId = shopId;
            }

            var data = LiveApplication.GetLiveList(query);
            for (var i = 0; i < data.Models.Count; i++)
            {
                var item = data.Models[i];
                var productQuery = new LiveQuery();
                productQuery.RoomId = item.RoomId;
                productQuery.PageNo = 1;
                productQuery.PageSize = 1;
                var productData = LiveApplication.GetLiveProducts(productQuery);
                item.ProductList = productData.Models;
                item.ProductCount = 1;
            }
            return SuccessResult<dynamic>(data.Models);
        }

        /// <summary>
        /// 获取第一个正在直播的场次
        /// </summary>
        /// <returns></returns>
        public Result<dynamic> GetFirstLivingScene()
        {
            var data = LiveApplication.GetFirstLivingRoom();
            if (data == null) return null;
            var productQuery = new LiveQuery();
            productQuery.RoomId = data.RoomId;
            productQuery.PageNo = 1;
            productQuery.PageSize = 3;
            var productData = LiveApplication.GetLiveProducts(productQuery);
            data.ProductList = productData.Models;
            data.ProductCount = productData.Total;
            return SuccessResult<dynamic>(data);
        }

        /// <summary>
        /// 获取场次详情
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        public Result<dynamic> GetLiveDetail(long roomId)
        {
            var model = LiveApplication.GetLiveDetail(roomId);
            return SuccessResult<dynamic>(model);
        }

        /// <summary>
        /// 获取场次商品
        /// </summary>
        /// <param name="roomId"></param>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        public Result<dynamic> GetLiveProducts(long roomId, int pageNo, int pageSize)
        {
            LiveQuery query = new LiveQuery();
            query.RoomId = roomId;
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            var data = LiveApplication.GetLiveProducts(query);
            return SuccessResult<dynamic>(data.Models);
        }

        #region  更新直播间
        public Result<dynamic> GetHomeLiveData(string ids)
        {
            List<long> roomIds = ValidRoomIds(ids);
            List<LiveProductInfo> liveprodcut = new List<LiveProductInfo>();
            liveprodcut = LiveApplication.GetLiveProducts(roomIds);//获取指定直播间的商品
            var liverooms = LiveApplication.GetLiveRoomByIds(roomIds);//获取指定直播间
           
            return SuccessResult<dynamic>(liverooms.Select(item => {
                var prolist = liveprodcut.Where(p => p.RoomId == item.RoomId).ToList();
                return new
                {
                    item.Id,
                    item.RoomId,
                    item.Name,
                    item.CoverImg,
                    item.ShareImg,
                    item.AnchorName,
                    item.AnchorImg,
                    item.HasReplay,
                    ProductCount = prolist.Count(),
                    ProductList = prolist,
                    item.Status,
                    item.StatusDesc,
                    item.StartTimeStr,
                    item.EndTimeStr,
                    item.StartTimeDesc,
                    item.Sequence
                };
            }));
        }

        public List<long> ValidRoomIds(string ids) {
            if (string.IsNullOrEmpty(ids))
            {
                throw new HimallException("请传入要查询的直播房号");
            }
            string[] roomIds = ids.Split(',');
            bool tag = true;
            List<long> roomIdlist = new List<long>();
            foreach (var id in roomIds)
            {
                if (!long.TryParse(id, out long r))
                {
                    tag = false;
                    break;
                }
                roomIdlist.Add(r);
            }
            if (!tag)
            {
                throw new HimallException("请传入正确的直播号");
            }
            return roomIdlist;
        }
        #endregion

    }
}
