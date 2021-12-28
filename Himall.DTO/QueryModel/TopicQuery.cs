
using Himall.Core;
using System.Collections.Generic;

namespace Himall.DTO.QueryModel
{
    public partial class TopicQuery : QueryBase
    {

        public string Name { get; set; }

        public string Tags { get; set; }

        public long ShopId { get; set; }
        public PlatformType? PlatformType { get; set; }
        /// <summary>
        /// 多个平台(比PlatformType参数的优先级高)
        /// </summary>
        public List<PlatformType> MorePlatForm { get; set; }
        //是否推荐
        public  bool? IsRecommend { get; set; }
        public TopicQuery()
        {
            PlatformType = Himall.Core.PlatformType.PC;
        }


    }
}
