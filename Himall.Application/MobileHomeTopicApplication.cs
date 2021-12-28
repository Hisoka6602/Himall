using Himall.Core;
using Himall.Service;
using Himall.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Application
{
    public class MobileHomeTopicApplication
    {
        private static MobileHomeTopicService _iMobileHomeTopicService = ObjectContainer.Current.Resolve<MobileHomeTopicService>();
        /// <summary>
        /// 获取移动端首页专题设置
        /// </summary>
        /// <param name="platformType">平台类型</param>
        /// <param name="shopId">店铺Id</param>
        /// <returns></returns>
        public static List<MobileHomeTopicInfo> GetMobileHomeTopicInfos(PlatformType platformType, long shopId = 0)
        {
            return _iMobileHomeTopicService.GetMobileHomeTopicInfos(platformType, shopId).ToList();
        }


    }
}
