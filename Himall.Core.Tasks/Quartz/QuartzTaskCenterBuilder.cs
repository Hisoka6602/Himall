using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Core.Tasks.Quartz
{
    public static class QuartzTaskCenterBuilder
    {
        public static TaskCenterBuilder UseQuartz(this TaskCenterBuilder center)
        {
            center.SetCenter(new QuartzTaskCenter());
            return center;
        }
    }
}
