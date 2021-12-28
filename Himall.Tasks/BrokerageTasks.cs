using Himall.Application;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    public class BrokerageTasks: ITasks
    {
        [Task("分销佣金结算", "0 0 * * * ?")]
        public static void Settlement()
        {
            DistributionApplication.Settlement();
        }
    }
}
