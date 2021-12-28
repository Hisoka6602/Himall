﻿using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.DTO.QueryModel;
using System;
using System.Linq;
using System.Web.Http;

namespace Himall.API
{
    public class ShopBranchMessageController : BaseShopBranchLoginedApiController
    {
        /// <summary>
        /// 获取未读消息数
        /// </summary>
        /// <returns></returns>
        public object GetMessages(
            int pageNo = 1, /*页码*/
            int pageSize = 10/*每页显示数据量*/)
        {
            CheckUserLogin();
            long sbid = CurrentUser.ShopBranchId;
            AppMessageQuery query = new AppMessageQuery();
            query.ShopBranchId = sbid;
            query.PageNo = pageNo;
            query.PageSize = pageSize;
            query.StartTime = DateTime.Now.AddDays(-30).Date;
            var data = AppMessageApplication.GetMessages(query);
            var list = data.Models.ToList().Select(item => new
            {
                Id = item.Id,
                Content = item.Content,
                IsRead = item.IsRead,
                OrderPayDate = item.OrderPayDate,
                sendtime = item.sendtime,
                ShopId = item.ShopId,
                SourceId = item.SourceId,
                Title = item.Title,
                TypeId = item.TypeId,
                ShopBranchId = item.ShopBranchId,
                HasShopbranch = item.TypeId == 1 ? GetShopbranch(item.SourceId) : 0
            });
            return new { success = true, rows = list, total = data.Total };
        }
        private int GetShopbranch(long id)
        {
            var info = OrderApplication.GetOrder(id);
            if (info != null &&info.ShopBranchId > 0) return 1;
            return 0;
        }
        /// <summary>
        /// 消息状态改已读
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public object PostReadMessage(ShopAppReadMessageModel model)
        {
            CheckUserLogin();
            AppMessageApplication.ReadMessage(model.id);
            return new { success = true };
        }
    }
}
