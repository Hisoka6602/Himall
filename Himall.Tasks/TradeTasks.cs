using Himall.Application;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    public class TradeTasks:ITasks
    {
        [Task("统计交易数据", "0 0 1 * * ?")]
        public static void StatisticOrder()
        {
            var yesterday = DateTime.Now.Date.AddDays(-1);
            StatisticApplication.SettlementOrder(yesterday);
        }

        [Task("更新会员活跃时间", "0 0 1 * * ?")]
        public static void StatisticMemeberActivity() {
            MemberApplication.StatisticMemeberActivity();
        }

        [Task("更新会员分组", "0 0 1 * * ?")]
        public static void StatisticMemeberGroup() {
            MemberApplication.StatisticMemeberGroup();
        }

        [Task("初始化门店统计数据", "0 0 0 * * ?")]
        public static void InitShopVisit() {
            StatisticApplication.InitShopVisit();
        }

        [Task("自动清理手机IP数据", "0 0 1 * * ?")]
        public static void PhoneIpCode()
        {
            PhoneIPCodeApplication.Clear();
        }
    }
}
