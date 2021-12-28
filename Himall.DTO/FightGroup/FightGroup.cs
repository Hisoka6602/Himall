using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.FightGroup
{
    public class FightGroup
    {
        /// <summary>
        /// 团长
        /// </summary>
        public long HeadUserId { get; set; }

        /// <summary>
        /// 成团人数
        /// </summary>
        public long LimitedNumber { get; set; }
        /// <summary>
        /// 时效
        /// </summary>
        public double LimitedHour { get; set; }
        /// <summary>
        /// 开团时间
        /// </summary>
        public DateTime AddGroupTime { get; set; }
        public DateTime TimeOut => AddGroupTime.AddHours(LimitedHour);
        /// <summary>
        /// 加入人数
        /// </summary>
        public long JoinedNumber { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public FightGroupBuildStatus GroupStatus { get; set; }
        /// <summary>
        /// 参团订单
        /// </summary>
        public List<FightGroupOrder> Items { get; set; }
    }
    public class FightGroupOrder
    {
        /// <summary>
        /// 参团人
        /// </summary>
        public long OrderUserId { get; set; }
    }
}
