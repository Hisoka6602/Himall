using Himall.Application;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    public class LiveTasks : ITasks
    {
        [Task("直播状态同步", "0 * * * * ?")]
        public static void SyncLive() 
        {
            LiveApplication.SyncLiveData();
        }
    }
}
