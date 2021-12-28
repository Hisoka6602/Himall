using Himall.Application;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    /// <summary>
    /// 虚拟订单
    /// </summary>
    public class VirtualOrderTasks : ITasks
    {
        /// <summary>
        /// 每日凌晨执行一次
        /// </summary>
        [Task("虚拟商品管理", "0 0 0 * * ?")]
        public static void Order()
        {
            OrderApplication.UpdateOrderVerificationCodeStatus();
            OrderApplication.UpdateVirtualProductStatus();
        }

        /// <summary>
        /// 每日凌晨10分执行一次（与上面方法推迟5分钟是特意让上面方法先执行值修改了，里面方法能调用到最新数据）
        /// Task里面星号参数规则：*    *     *     *    *     *   *     
        ///               格式： [秒] [分] [小时] [日] [月] [周] [年] 
        /// </summary>
        [Task("虚拟订单自动退货", "0 5 0 * * ?")]
        public static void Refund() {
            RefundApplication.AutoVirtualRefund();
        }
    }
}
