using Himall.Application;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    public class ShopTasks :ITasks
    {
        [Task("门店过期", "0 0 0 * * ?")]
        public static void ShopExpire()
        {
            ShopApplication.AutoExpire();
        }
    }
}
