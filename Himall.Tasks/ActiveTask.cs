using Himall.Application;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    public class ActiveTask : ITasks
    {
        [Task("拼团相关", "0 0/5 * * * ? *")]
        public static void FightGroup()
        {
            FightGroupApplication.UpdateProductMinPrice();
        }

        [Task("限时购相关", "0 0/5 * * * ? *")]
        public static void FlashSale() {

            LimitTimeApplication.UpdateProductMinPrice();
        }

    }
}
