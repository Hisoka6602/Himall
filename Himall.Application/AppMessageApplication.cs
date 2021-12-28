using Himall.Service;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.DTO.QueryModel;
using Himall.CommonModel;
using System.Collections.Generic;
namespace Himall.Application
{
    public class AppMessageApplication
    {
        private static AppMessageService _appMessageService = ObjectContainer.Current.Resolve<AppMessageService>();

        /// <summary>
        /// 未读消息数（30天内）
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static int GetShopNoReadMessageCount(long shopId)
        {
            return _appMessageService.GetShopNoReadMessageCount(shopId);
        }
        /// <summary>
        /// 未读消息数（30天内）
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public static int GetBranchNoReadMessageCount(long shopBranchId)
        {
            return _appMessageService.GetBranchNoReadMessageCount(shopBranchId);
        }

        public static Dictionary<long, int> GetBranchNoReadMessageCount(List<long> branchs)
        {
            return _appMessageService.GetBranchNoReadMessageCount(branchs);
        }

        /// <summary>
        /// 获取消息列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<AppMessageInfo> GetMessages(AppMessageQuery query)
        {
            return _appMessageService.GetMessages(query);
        }
        /// <summary>
        /// 消息状态改已读
        /// </summary>
        /// <param name="id"></param>
        public static void ReadMessage(long id)
        {
            _appMessageService.ReadMessage(id);
        }
        /// <summary>
        /// 新增消息
        /// </summary>
        /// <param name="appMessagesInfo"></param>
        public static void AddAppMessages(AppMessages appMessages)
        {
            AutoMapper.Mapper.CreateMap<AppMessages, AppMessageInfo>();
            var appMessagesInfo = AutoMapper.Mapper.Map<AppMessages, AppMessageInfo>(appMessages);
            _appMessageService.AddAppMessages(appMessagesInfo);
        }
    }
}
