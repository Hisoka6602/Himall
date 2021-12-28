﻿using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Http;

namespace Himall.API
{
    public class ShopBranchHomeController : BaseShopBranchLoginedApiController
    {
        public object GetShopBranchHome()
        {
            try
            {
                CheckUserLogin();

                var now = DateTime.Now;

                var orderQuery = new OrderCountStatisticsQuery()
                {
                    ShopBranchId = CurrentShopBranch.Id,
                    Fields = new List<OrderCountStatisticsFields> { OrderCountStatisticsFields.ActualPayAmount }
                };
                //三月内
                orderQuery.OrderDateBegin = new DateTime(now.Year, now.Month, 1).AddMonths(-2);
                var threeMonthAmount = StatisticApplication.GetOrderCount(orderQuery).TotalActualPayAmount;
                //本周
                orderQuery.OrderDateBegin = now.Date.AddDays(-(int)now.DayOfWeek);
                var weekAmount = StatisticApplication.GetOrderCount(orderQuery).TotalActualPayAmount;
                //今天
                orderQuery.OrderDateBegin = now.Date;
                var todayAmount = StatisticApplication.GetOrderCount(orderQuery).TotalActualPayAmount;


                //待自提订单数
                orderQuery = new OrderCountStatisticsQuery()
                {
                    ShopBranchId = CurrentShopBranch.Id,
                    OrderOperateStatus = Entities.OrderInfo.OrderOperateStatus.WaitSelfPickUp,
                    Fields = new List<OrderCountStatisticsFields> { OrderCountStatisticsFields.OrderCount }
                };
                var pickUpOrderCount = StatisticApplication.GetOrderCount(orderQuery).OrderCount;

                //近三天发布商品数
                var productCount = ProductManagerApplication.GetProductCount(new ProductQuery
                {
                    ShopBranchId = CurrentShopBranch.Id,
                    AuditStatus = new[] { Entities.ProductInfo.ProductAuditStatus.Audited },
                    StartDate = now.Date.AddDays(-2)
                });


                var vshop = ServiceProvider.Instance<VShopService>.Create.GetVShopByShopId(CurrentShopBranch.ShopId);
                var logo = "/Images/branchapp.jpg";
                if (vshop != null && vshop.State == Entities.VShopInfo.VshopStates.Normal && !string.IsNullOrEmpty(vshop.WXLogo))
                {
                    logo = vshop.WXLogo;
                }
                var shopBranch = ShopBranchApplication.GetShopBranchById(CurrentShopBranch.Id);
                var isShelvesProduct = false;
                if (shopBranch != null && shopBranch.Status == ShopBranchStatus.Normal)
                    isShelvesProduct = shopBranch.IsShelvesProduct;
                return new
                {
                    success = true,
                    data = new
                    {
                        shopName = CurrentShopBranch.ShopBranchName,
                        todayAmount = todayAmount,
                        weekAmount = weekAmount,
                        threeMonthAmounht = threeMonthAmount,
                        createProductCount = productCount,
                        pickUpOrderCount = pickUpOrderCount,
                        logo = logo,
                        shopBranchId = CurrentShopBranch.Id,
                        IsShelvesProduct = isShelvesProduct
                    }
                };
            }
            catch (Exception ex)
            {

                Log.Error(ex.ToString());
                return new { success = false, data = new { } };
            }
        }
        [Obsolete("ShopHomeController/GetShopHome重复")]
        public object GetShopHome()
        {
            CheckUserLogin();

            var now = DateTime.Now;

            var orderQuery = new OrderCountStatisticsQuery()
            {
                ShopId = CurrentUser.ShopId,
                Fields = new List<OrderCountStatisticsFields> {
                    OrderCountStatisticsFields.ActualPayAmount
                }
            };
            //三月内
            orderQuery.OrderDateBegin = new DateTime(now.Year, now.Month, 1).AddMonths(-2);
            var threeMonthAmount = StatisticApplication.GetOrderCount(orderQuery).TotalActualPayAmount;
            //本周
            orderQuery.OrderDateBegin = now.Date.AddDays(-(int)now.DayOfWeek);
            var weekAmount = StatisticApplication.GetOrderCount(orderQuery).TotalActualPayAmount;
            //今天
            orderQuery.OrderDateBegin = now.Date;
            var todayAmount = StatisticApplication.GetOrderCount(orderQuery).TotalActualPayAmount;

            //近三天发布商品数
            var productCount = ProductManagerApplication.GetProductCount(new ProductQuery
            {
                ShopId = CurrentUser.ShopId,
                AuditStatus = new[] { Entities.ProductInfo.ProductAuditStatus.Audited },
                StartDate = now.Date.AddDays(-2)
            });

            //待审核退货/退款
            var refundCount = RefundApplication.GetOrderRefundsCount(new RefundQuery()
            {
                ShopId = CurrentUser.ShopId,
                AuditStatus = Entities.OrderRefundInfo.OrderRefundAuditStatus.WaitAudit,
            });

            return new
            {
                success = true,
                data = new
                {
                    shopName = CurrentShopBranch.ShopBranchName,
                    todayAmount = todayAmount,
                    weekAmount = weekAmount,
                    threeMonthAmounht = threeMonthAmount,
                    createProductCount = productCount,
                    refundCount = refundCount
                }
            };
        }

        public object GetShopBranchInfo()
        {
            CheckUserLogin();

            var shopBranch = ShopBranchApplication.GetShopBranchs(new List<long> { CurrentShopBranch.Id });
            return new { data = shopBranch, success = true };
        }

        public object PostShopBranchInfo(DTO.ShopBranch model)
        {
            CheckUserLogin();

            ShopBranchApplication.UpdateShopBranch(model);
            return new { success = true, msg = "更新成功！" };
        }

        public object GetUpdateApp(string appVersion, int type)
        {
            var siteSetting = SiteSettingApplication.SiteSettings;

            if (string.IsNullOrWhiteSpace(appVersion) || (3 < type && type < 2))
            {
                return ErrorResult("版本号不能为空或者平台类型错误", 10006);
            }
            Version ver = null;
            try
            {
                ver = new Version(appVersion);
            }
            catch (Exception)
            {
                return ErrorResult("错误的版本号", 10005);
            }
            if (string.IsNullOrWhiteSpace(siteSetting.ShopAppVersion))
            {
                siteSetting.ShopAppVersion = "0.0.0";
            }
            var downLoadUrl = "";
            Version v1 = new Version(siteSetting.ShopAppVersion), v2 = new Version(appVersion);
            if (v1 > v2)
            {
                if (type == (int)PlatformType.IOS)
                {
                    if (string.IsNullOrWhiteSpace(siteSetting.ShopIOSDownLoad))
                    {
                        return ErrorResult("站点未设置IOS下载地址", 10004);
                    }
                    downLoadUrl = siteSetting.ShopIOSDownLoad;
                }
                if (type == (int)PlatformType.Android)
                {
                    if (string.IsNullOrWhiteSpace(siteSetting.ShopAndriodDownLoad))
                    {
                        return ErrorResult("站点未设置Andriod下载地址", 10003);
                    }
                    string str = siteSetting.ShopAndriodDownLoad.Substring(siteSetting.ShopAndriodDownLoad.LastIndexOf("/"), siteSetting.ShopAndriodDownLoad.Length - siteSetting.ShopAndriodDownLoad.LastIndexOf("/"));
                    var curProjRootPath = System.Web.Hosting.HostingEnvironment.MapPath("~/app") + str;
                    if (!File.Exists(curProjRootPath))
                    {
                        return ErrorResult("站点未上传app安装包", 10002);
                    }
                    downLoadUrl = siteSetting.ShopAndriodDownLoad;
                }
            }
            else
            {
                return ErrorResult("当前为最新版本", 10001);
            }
            return new { success = true, code = 10000, DownLoadUrl = downLoadUrl, Description = siteSetting.ShopAppUpdateDescription };
        }
        /// <summary>
        /// 获取未读消息数
        /// </summary>
        /// <returns></returns>
        public object GetNoReadMessageCount()
        {
            CheckUserLogin();
            long sbid = CurrentUser.ShopBranchId;
            int count = AppMessageApplication.GetBranchNoReadMessageCount(sbid);
            return new { success = true, count = count };
        }
        /// <summary>
        /// 设置商家管理
        /// </summary>
        /// <param name="enable"></param>
        /// <returns></returns>
        [HttpGet]
        public object SetShopManager(bool enable)
        {
            if (enable) ShopBranchApplication.EnableManager(CurrentShopBranch.Id);
            else ShopBranchApplication.DisableManger(CurrentShopBranch.Id);
            return new { success = true };
        }
        /// <summary>
        /// 获取商家管理设置
        /// </summary>
        /// <returns></returns>
        public object GetShopManager()
        {
            var branch = ShopBranchApplication.GetShopBranchById(CurrentShopBranch.Id);
            return new { success = true, enable = branch.EnableSellerManager };
        }
    }
}
