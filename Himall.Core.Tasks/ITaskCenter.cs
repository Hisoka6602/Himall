using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Core.Tasks
{
    public interface ITaskCenter
    {
        void Subscribe(TaskDetail detail);
        void Start();
        void Stop();
    }
}
