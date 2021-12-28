using AutoMapper;
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
    /// <summary>
    /// 拼团逻辑
    /// </summary>
    public class RechargePresentRuleApplication
    {

        private static RechargePresentRuleService _iRechargePresentRuleService = ObjectContainer.Current.Resolve<RechargePresentRuleService>();

        /// <summary>
        /// 新增修改充值赠送规则
        /// </summary>
        /// <param name="data"></param>
        /// <param name="rules"></param>
        /// <param name="products"></param>
        public static void SetRules(IEnumerable<RechargePresentRule> rules)
        {
            var data = Mapper.Map<IEnumerable<RechargePresentRule>, List<RechargePresentRuleInfo>>(rules);
            _iRechargePresentRuleService.SetRules(data);
        }
        /// <summary>
        /// 获取充值赠送规则
        /// </summary>
        /// <returns></returns>
        public static List<RechargePresentRule> GetRules()
        {
            var data = _iRechargePresentRuleService.GetRules();
            var result = Mapper.Map<List<RechargePresentRuleInfo>, List<RechargePresentRule>>(data);
            return result;
        }
    }
}
