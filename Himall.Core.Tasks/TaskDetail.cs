using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Core.Tasks
{
    public class TaskDetail
    {
        public string Name { get; set; }
        public string Group { get; set; }
        public bool StartNow { get; set; }
      
        public FastInvoke.FastInvokeHandler Handler { get; set; }
        public TaskInterval Interval { get; set; }

    }
}
