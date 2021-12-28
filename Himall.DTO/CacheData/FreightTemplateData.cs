using Himall.CommonModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class FreightTemplateData
    {
        public long Id { get; set; }
        public bool IsFree { get; set; }

        /// <summary>
        /// 计算单位
        /// </summary>
        public ValuationMethodType ValuationMethod { get; set; }
        /// <summary>
        /// 计算规则
        /// </summary>
        public List<FreightTemplateRuleData> Rules { get; set; }
        /// <summary>
        /// 计算规则区域关系
        /// </summary>
        public Dictionary<int, long> RulesMap { get; set; }
        /// <summary>
        /// 免邮规则
        /// </summary>
        public List<FreightTemplateFreeRuleData> FreeRules { get; set; }
        /// <summary>
        /// 免邮区域关系
        /// </summary>
        public Dictionary<long, long> FreeRulesMap { get; set; }

    }
    /// <summary>
    /// 计算规则
    /// </summary>
    public class FreightTemplateRuleData
    {
        public long Id { get; set; }
        /// <summary>
        /// 首重
        /// </summary>
        public int FirstUnit { get; set; }
        /// <summary>
        /// 首重金额
        /// </summary>
        public decimal FirstUnitMonry { get; set; }
        /// <summary>
        /// 追加单位
        /// </summary>
        public int AccumulationUnit { get; set; }
        /// <summary>
        /// 追加金额
        /// </summary>
        public decimal AccumulationUnitMoney { get; set; }
        /// <summary>
        /// 是否默认
        /// </summary>
        public bool IsDefault { get; set; }
    }

    /// <summary>
    /// 免邮规则
    /// </summary>
    public class FreightTemplateFreeRuleData 
    {
        public long Serial { get; set; }
        public FreightTempateFreeType FreeType { get; set; }
        public int Piece { get; set; }
        public decimal Amount { get; set; }
    }

    
}
