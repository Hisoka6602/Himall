using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.Market
{
    /// <summary>
    /// 通用优惠券(门店券,平台券,代金红包)记录
    /// </summary>
    public class GeneralRecord
    {
        /// <summary>
        /// 记录ID
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 活动ID
        /// </summary>
        public long MarketId { get; set; }
        /// <summary>
        /// 所属商家
        /// </summary>
        public long ShopId { get; set; }
        /// <summary>
        /// 记录类型
        /// </summary>
        public GeneralRecordType RecordType { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// 面值
        /// </summary>
        public decimal Amount { get; set; }
        /// <summary>
        /// 可抵扣金额
        /// </summary>
        public decimal ActualAmount { get; set; }

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool Selected { get; set; }

        /// <summary>
        /// 开始文本
        /// </summary>
        public string StartText => StartTime.ToString("yyyy.MM.dd");

        /// <summary>
        /// 结束文本
        /// </summary>
        public string EndText => EndTime.ToString("yyyy.MM.dd");

        [JsonIgnore]
        public DateTime StartTime { get; set; }
        [JsonIgnore]
        public DateTime EndTime { get; set; }
        /// <summary>
        /// 使用门槛
        /// </summary>
        public decimal OrderAmount { get; set; }
        /// <summary>
        /// 是否含有使用限制
        /// </summary>
        public bool IsLimit { get; set; }
        /// <summary>
        /// 关联项目
        /// </summary>
        [JsonIgnore]
        public List<MarketingOrderItem> Items { get; set; }
    }

    public class GeneralRecordChoice
    {
        public long ShopId { get; set; }
        public long RecordId { get; set; }
        public GeneralRecordType RecordType { get; set; }
    }

    public enum GeneralRecordType
    {
        /// <summary>
        /// 优惠券
        /// </summary>
        Coupon = 1,
        /// <summary>
        /// 平台券
        /// </summary>
        Platform = 2,
        /// <summary>
        /// 代金红包
        /// </summary>
        Bouns = 3,
    }
}
