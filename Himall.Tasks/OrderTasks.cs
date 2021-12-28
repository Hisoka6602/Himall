using Himall.Application;
using Himall.Core;
using Himall.Core.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Tasks
{
    public class OrderTasks : ITasks
    {
        /// <summary>
        /// 自动订单评价(每小时整点) (评论统计服务:WinOrderCommentsService)
        /// </summary>
        [Task("订单评价", "0 0 * * * ?")]
        public static void AutoOrderComment()
        {
            TradeCommentApplication.CommentStatistics();
        }
        /// <summary>
        /// 订单商品统计(每日凌晨1点) (订单商品统计服务:WinOrderProductStatisticsService)
        /// </summary>
        [Task("订单商品统计", "0 0 1 * * ?")]
        public static void OrderProductStatistic()
        {
            var yesterday = DateTime.Now.Date.AddDays(-1);
            StatisticApplication.SettlementPayProduct(yesterday);
        }

        /// <summary>
        /// 订单定时任务(每小时 15,45分执行) (订单处理服务:WinOrderService:OrderJob)
        /// </summary>
        [Task("订单定时任务", "0 15,45 * * * ?")]
        public static void OrderJob()
        {
            Log.Info("订单自动化任务");
            try
            {
                OrderApplication.AutoCloseOrder();
            }
            catch (Exception ex)
            {
                Log.Error("自动关闭订单", ex);
            }
            try
            {
                OrderApplication.AutoConfirmOrder();
            }
            catch (Exception ex)
            {
                Log.Error("自动确认订单异常", ex);
            }

            try
            {
                OrderApplication.AutoConfirmGiftOrder();
            }
            catch (Exception ex)
            {
                Log.Error("自动确认礼品订单异常", ex);
            }

            try
            {
                OrderApplication.SettlementIntegral();
            }
            catch (Exception ex)
            {
                Log.Error("自动结算积分异常", ex);
            }
        }

        [Task("发未支付通知", "0 0/6 * * * ?")]
        public static void AutoSendSMS()
        {
            try
            {
                OrderApplication.AutoSendSMS();
            }
            catch (Exception ex)
            {
                Log.Error("发未支付通知", ex);
            }
        }

        [Task("订单结算", "0 0 1 * * ?")]
        public static void OrderSettlement()
        {
            OrderApplication.Settlement();
        }

      
        [Task("旺店通商品推送", 5)]
        public static void WDTProductPush()
        {
            new WDTProductApplication();
        }
        [Task("售后自动处理", "0 15,45 * * * ?")]
        public static void OrderRefund()
        {
            RefundApplication.AutoAudit();
            RefundApplication.AutoCloseByDeliveryExpired();
            RefundApplication.AutoShopConfirmArrival();
        }

        [Task("拼团失败", "0 * * * * ?")]
        public static void FreightGroup()
        {
            FightGroupApplication.GroupFail();
        }

        [Task("自动评论", "0 15,45 * * * ?")]
        public static void OrderComment()
        {
            OrderApplication.AutoComment();
        }


        [Task("历史订单沉降", "0 0 4 * * ?")]
        public static void SyncOrder()
        {
            OrderApplication.SyncOrder();
        }
        [Task("旺店通订单推送", 5)]
        public static void WDTOrderPush()
        {
            new WDTOrderApplication();
        }
        [Task("旺店通订单发送状态同步", 5)]
        public static void WDTOrderSendGoodsStatusSync()
        {
            WDTOrderApplication.SyncOrderSendGoodsStatus();
        }

        [Task("旺店商品库存同步", 5)]
        public static void WDTProductStockSync()
        {
            WDTProductApplication.SyncStockFromWdt();
        }
    }
}
