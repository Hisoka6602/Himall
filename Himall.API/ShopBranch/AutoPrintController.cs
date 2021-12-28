﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Hubs;
using Microsoft.AspNet.SignalR;
using Himall.API.Hubs;
using Himall.API.Base;
using Himall.Application;
using System.Threading;
using Himall.DTO;
using Himall.API.Model;
using Himall.Core.Helper;

namespace Himall.API
{
    public class AutoPrintController : ApiControllerWithHub<PrintHub>
    {
        private string _userKey = string.Empty;
        private readonly TimeSpan BroadcastInterval = TimeSpan.FromSeconds(300);
        public void DoWorker(object state)
        {
            Core.Log.Info("DoWorker方法进入");
            var currentInfo = PrintHub.ConnectedUsers.FirstOrDefault(p => p.UserKey == _userKey);
            if (currentInfo == null)//如果已失去客户端连接，则结束
                return;

            int printCount = currentInfo.PrintCount;
            bool isAutoPrint = currentInfo.IsAutoPrint;
            if (isAutoPrint)
            {
                var orderIds = OrderApplication.GetOrderIdsByLatestTime(5, CurrentShopBranch.Id,0);//读取最近时间段内满足打印条件都的订单ID
                Core.Log.Info("取到订单了吗" + orderIds.Count);
                if (orderIds != null && orderIds.Count > 0)
                {
                    orderIds.ForEach(p =>
                    {
                        Core.Log.Info("开始调用客户端了吗,打印订单:" + p);
                        Clients.Client(currentInfo.ConnectionId).autoPrint(new PrintOrder()
                        {
                            OrderId = p,
                            PrintCount = printCount
                        });//无法检测是否打印成功
                    });
                }
            }
            bool isVoice = true;
            if (isVoice)
            {
                var orderIds = OrderApplication.GetOrderIdsByLatestTimeYuyin(5, CurrentShopBranch.Id, 0);//读取最近时间段内支付订单
                Core.Log.Info("取到订单了吗111-" + orderIds.Count);
                if (orderIds != null && orderIds.Count > 0)
                {
                    orderIds.ForEach(p =>
                    {
                        Core.Log.Info("开始调用客户端了吗,播报订单:" + p);
                        Clients.Client(currentInfo.ConnectionId).autoVoice(new PrintOrder()
                        {
                            OrderId = p,
                        });//无法检测是否打印成功
                    });
                }
            }


        }

        ///退出门店APP后，必须解除连接，停止检测
        public void GetCloseAutoPrint(string userKey)
        {
            Core.Log.Info(string.Format("退出登录,userKey:{0}", userKey));
            var currentInfo = PrintHub.ConnectedUsers.FirstOrDefault(p => p.UserKey == userKey);
            if (currentInfo != null)
            {
                if (currentInfo.BroadcastLoop != null)
                {
                    currentInfo.BroadcastLoop.Change(Timeout.Infinite, Timeout.Infinite);
                    currentInfo.BroadcastLoop.Dispose();
                }
                PrintHub.ConnectedUsers.Remove(currentInfo);
            }
        }

        //门店APP登录后调用该方法，启动打印广播。每次刷新都会调用该方法，只要不退出
        public void GetPrintBroadcastShape(string userKey)
        {
            Core.Log.Info("GetPrintBroadcastShape方法的进入");
            if (CurrentShopBranch != null && IsBranchManager)//必须是门店管理员才能启动自动打印服务
            {
                _userKey = userKey;
                Core.Log.Info("GetPrintBroadcastShape" + userKey);
                var currentInfo = PrintHub.ConnectedUsers.FirstOrDefault(p => p.UserKey == userKey);
                if (currentInfo != null)
                {
                    Core.Log.Info("74行代码进入");
                    var shopBranchInfo = ShopBranchApplication.GetShopBranchById(CurrentShopBranch.Id);
                    if (shopBranchInfo != null)
                    {
                        currentInfo.IsAutoPrint = shopBranchInfo.IsAutoPrint;
                        currentInfo.PrintCount = shopBranchInfo.PrintCount;
                        Core.Log.Info("打印状态" + currentInfo.IsAutoPrint);
                    }
                    currentInfo.BroadcastLoop = new Timer(
                            DoWorker,
                            null, BroadcastInterval, BroadcastInterval);

                }
                //StartAutoPrint();//登录后调用
            }
            // return Json(new { Msg = "success" });
        }

        public object PostUpdteAutoPrint(ShopBranch info)
        {
            var shopBranchInfo = ShopBranchApplication.GetShopBranchById(CurrentShopBranch.Id);
            if (shopBranchInfo != null)
            {
                shopBranchInfo.IsAutoPrint = info.IsAutoPrint;
                ShopBranchApplication.UpdateShopBranch(shopBranchInfo);
                string userkey = WebHelper.GetFormString("userkey");
                var currentInfo = PrintHub.ConnectedUsers.FirstOrDefault(p => p.UserKey == userkey);
                if (currentInfo != null)
                {
                    currentInfo.IsAutoPrint = info.IsAutoPrint;
                }
            }
            return new { success = true };

        }

        public object PostUpdtePrintCount(ShopBranch info)
        {
            var shopBranchInfo = ShopBranchApplication.GetShopBranchById(CurrentShopBranch.Id);
            if (shopBranchInfo != null)
            {
                shopBranchInfo.PrintCount = info.PrintCount;
                ShopBranchApplication.UpdateShopBranch(shopBranchInfo);
                string userkey = WebHelper.GetFormString("userkey");
                var currentInfo = PrintHub.ConnectedUsers.FirstOrDefault(p => p.UserKey == userkey);
                if (currentInfo != null)
                {
                    currentInfo.PrintCount = info.PrintCount;
                }
            }
            return new { success = true };

        }
        public object GetShopBranchPrintInfo()
        {
            var shopBranchInfo = ShopBranchApplication.GetShopBranchById(CurrentShopBranch.Id);
            if (shopBranchInfo != null)
            {
                return new
                {
                    PrintCount = shopBranchInfo.PrintCount,
                    IsAutoPrint = shopBranchInfo.IsAutoPrint
                };
            }
            return null;
        }

        /// <summary>
        /// 获取前5分钟可以语音播报的订单数
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public object GetVoiceOrder()
        {
            CheckUserLogin();
            var orderIds = OrderApplication.GetOrderIdsByLatestTimeYuyin(5, CurrentShopBranch.Id, 0);//读取最近时间段内支付订单
            Core.Log.Info("语音取到订单了吗?" + orderIds.Count);
            if (orderIds != null && orderIds.Count > 0)
            {
                return new
                {
                    success = true,
                    count = orderIds.Count
                };
            }
            return new
            {
                success = false,
                count = 0
            };
        }
    }
}
