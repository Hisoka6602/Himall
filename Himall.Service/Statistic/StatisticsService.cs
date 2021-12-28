using Himall.CommonModel;
using Himall.DTO;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Himall.Core;
using System.Text;

namespace Himall.Service
{
    public class StatisticsService : ServiceBase
    {
        public QueryPageModel<ProductStatisticModel> GetProductVisits(ProductStatisticQuery query)
        {
            string sql = "SELECT V.ProductId,P.ProductName,SUM(V.SaleAmounts)as SaleAmounts,SUM(V.SaleCounts)as SaleCounts,SUM(V.PayUserCounts)as PayUserCounts  ";
            string countSql = " select count(1) from ( SELECT count(1)  FROM himall_productvisti V ";
            countSql += " left join himall_product P on P.Id=V.ProductId   ";
            sql += " ,SUM(V.VisitUserCounts)as VisitUserCounts ";
            sql += " ,SUM(V.VistiCounts)as VistiCounts ";
            sql += " ,(SUM(V.PayUserCounts)/SUM(V.VisitUserCounts))as Conversion ";
            sql += " ,(case  when SUM(V.VisitUserCounts)=0 and SUM(V.PayUserCounts)>0  then 100 else SUM(V.PayUserCounts)/SUM(V.VisitUserCounts) end )as _Conversion ";
            sql += " FROM himall_productvisti V left join himall_product P on P.Id=V.ProductId  ";
            sql += string.Format(" where V.Date>='{0}' and V.Date<'{1}' ", query.StartDate.ToString("yyyy-MM-dd HH:mm:ss"), query.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
            countSql += string.Format(" where V.Date>='{0}' and V.Date<'{1}' ", query.StartDate.ToString("yyyy-MM-dd HH:mm:ss"), query.EndDate.ToString("yyyy-MM-dd HH:mm:ss"));
            if (query.ShopId.HasValue)
            {
                sql += string.Format(" and V.ShopId={0} ", query.ShopId.Value);
                countSql += string.Format(" and V.ShopId={0} ", query.ShopId.Value);
            }
            sql += " group by V.ProductId ";
            countSql += " group by V.ProductId )T";

            switch (query.Sort.ToLower())
            {
                case "visticounts":
                    if (query.IsAsc)
                        sql += " order by VistiCounts";
                    else
                        sql += " order by VistiCounts desc ";
                    break;
                case "visitusercounts":
                    if (query.IsAsc)
                        sql += " order by VisitUserCounts";
                    else
                        sql += " order by VisitUserCounts desc ";
                    break;
                case "payusercounts":
                    if (query.IsAsc)
                        sql += " order by PayUserCounts";
                    else
                        sql += " order by PayUserCounts desc ";
                    break;
                case "singlepercentconversion":
                    if (query.IsAsc)
                        sql += " order by _Conversion";
                    else
                        sql += " order by _Conversion desc ";
                    break;
                case "salecounts":
                    if (query.IsAsc)
                        sql += " order by SaleCounts";
                    else
                        sql += " order by SaleCounts desc ";
                    break;
                case "saleamounts":
                    if (query.IsAsc)
                        sql += " order by SaleAmounts";
                    else
                        sql += " order by SaleAmounts desc ";
                    break;
                default:
                    sql += " order by ProductId desc ";
                    break;
            }
            sql += GetSearchPage(query);
            Core.Log.Info(sql);
            Core.Log.Info(countSql);
            var model = DbFactory.Default.Query<ProductStatisticModel>(sql).ToList();
            return new QueryPageModel<ProductStatisticModel>
            {
                Models = model,
                Total = DbFactory.Default.ExecuteScalar<int>(countSql)
            };
        }

        public void InitShopVisit()
        {
            StringBuilder strSql = new StringBuilder("insert into Himall_ShopVisti(ShopId,ShopBranchId,Date,VistiCounts,OrderUserCount,OrderCount,OrderProductCount,OrderAmount,OrderPayUserCount,OrderPayCount,SaleCounts,SaleAmounts,OrderRefundCount,OrderRefundAmount,UnitPrice,JointRate,StatisticFlag) ");
            strSql.AppendFormat("select Id,0,date(NOW()),0,0,0,0,0,0,0,0,0,0,0,0,0,0 from Himall_Shop where id not in(select ShopId from Himall_ShopVisti  where Date>='{0}' and Date<='{1}' group by ShopId)", DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("yyyy-MM-dd 23:59:59"));
            DbFactory.Default.Execute(strSql.ToString());
        }

        public void SettlementPayProduct(DateTime startTime, DateTime endTime)
        {
            string sql = "select o.Id as OrderId,o.OrderDate,o.ShopId,o.UserId,o.TotalAmount,i.ProductId,i.ProductName,i.RealTotalPrice,i.Quantity as OrderProductQuantity " +
                " from himall_order as o left join Himall_OrderItem as i on o.Id = i.OrderId where o.PayDate >= '{0}' and o.PayDate<'{1}'  AND i.Quantity>0";
            sql = string.Format(sql, startTime.ToString("yyyy-MM-dd HH:mm:ss"), endTime.ToString("yyyy-MM-dd HH:mm:ss"));
            var payOrders = DbFactory.Default.Query<dynamic>(sql);
            var payOrderGroups = payOrders.GroupBy(e => e.ProductId);
            var payOrderList = payOrderGroups.ToList();
            var pids = payOrderList.Select(e => (long)e.Key).Distinct().ToList();
            List<ProductVistiInfo> oldProductVisits = new List<ProductVistiInfo>();
            if (pids.Count() > 0)
                oldProductVisits = DbFactory.Default.Get<ProductVistiInfo>().Where(e => e.Date == startTime && e.ProductId.ExIn(pids)).ToList();

            foreach (var g in payOrderGroups)
            {
                var orders = g.ToList();
                ProductVistiInfo productVisit = oldProductVisits.FirstOrDefault(e => e.ProductId == g.Key);
                bool isAdd = false;
                if (productVisit == null)
                {
                    productVisit = new ProductVistiInfo();
                    isAdd = true;
                    //销售量、销售金额在订单逻辑里有实时处理，如果没有记录则进行统计
                    productVisit.SaleCounts = orders.Sum(e => (long)e.OrderProductQuantity);
                    productVisit.SaleAmounts = orders.Sum(e => (decimal)e.TotalAmount);
                }
                productVisit.Date = startTime;
                productVisit.ProductId = g.Key;
                productVisit.PayUserCounts = orders.Select(e => e.UserId).Distinct().Count();
                productVisit.StatisticFlag = true;
                var item = orders.FirstOrDefault();
                if (item != null)
                {
                    productVisit.ShopId = item.ShopId;
                }

                if (isAdd)
                    DbFactory.Default.Add(productVisit);
                else
                    DbFactory.Default.Update(productVisit);

            }
            //将没有付款记录的统计信息，统一修改为已统计
            var noPayOrdersStatistic = DbFactory.Default.Get<ProductVistiInfo>().Where(t => t.StatisticFlag == false && t.Date >= startTime && t.Date <= endTime).ToList();
            foreach (var v in noPayOrdersStatistic)
                DbFactory.Default.Set<ProductVistiInfo>().Set(p => p.StatisticFlag, true).Where(p => p.Id == v.Id).Succeed();
        }

        private string GetSearchPage(ProductStatisticQuery query)
        {
            return string.Format(" LIMIT {0},{1} ", (query.PageNo - 1) * query.PageSize, query.PageSize);
        }

        public List<ProductCategoryStatistic> GetProductCategoryStatistic(long shop, DateTime begin, DateTime end)
        {
            var enddate = DateTime.Parse(end.ToShortTimeString()).AddHours(23).AddMinutes(59).AddSeconds(59);
            var db = DbFactory.Default.Get<ProductVistiInfo>()
                    .LeftJoin<ProductInfo>((v, p) => v.ProductId == p.Id)
                    .Where(p => p.Date >= begin && p.Date < enddate)
                    .GroupBy<ProductInfo>(p => p.CategoryId)
                    .Select<ProductInfo>(p => new { CategoryId = p.CategoryId })
                    .Select(p => new
                    {
                        SaleAmounts = p.SaleAmounts.ExSum(),
                        SaleCounts = p.SaleCounts.ExSum()
                    });
            if (shop > 0)
                db.Where(p => p.ShopId == shop);
            return db.ToList<ProductCategoryStatistic>();
        }
        public List<TradeStatisticModel> GetShopVisits(long shop, long? shopbranchId, DateTime begin, DateTime end)
        {
            var db = DbFactory.Default.Get<ShopVistiInfo>().Where(e => e.Date >= begin.Date && e.Date < end.Date);
            if (shop > 0)
                db = db.Where(p => p.ShopId == shop);
            if (shopbranchId.HasValue)
                db = db.Where(p => p.ShopBranchId == shopbranchId);
            return db.ToList().Select(item => new TradeStatisticModel
            {
                Date = item.Date,
                VisitCounts = item.VistiCounts,
                OrderAmount = item.OrderAmount,
                OrderCount = item.OrderCount,
                OrderPayCount = item.OrderPayCount,
                OrderPayUserCount = item.OrderPayUserCount,
                OrderProductCount = item.OrderProductCount,
                OrderUserCount = item.OrderUserCount,
                SaleAmounts = item.SaleAmounts,
                SaleCounts = item.SaleCounts,
                StatisticFlag = item.StatisticFlag,
                OrderRefundCount =(int) item.OrderRefundCount,
                OrderRefundProductCount = item.OrderRefundProductCount,
                OrderRefundAmount = item.OrderRefundAmount
            }).ToList();
        }
        public List<TradeStatisticModel> GetShopVisitsByRealTime(long shop, long? shopbranchId, DateTime begin, DateTime end)
        {
            if (begin > end)
            {
                throw new HimallException("时间段异常：开始时间大于结束时间！");
            }
            var models = new List<TradeStatisticModel>();
            var model = new TradeStatisticModel() {
                Date = begin
            };
            
            //取浏览人数
            var db = DbFactory.Default.Get<ShopVistiInfo>().Where(e => e.Date >= begin && e.Date < end);
            if (shop > 0)
                db = db.Where(p => p.ShopId == shop);
            if (shopbranchId.HasValue)
                db = db.Where(p => p.ShopBranchId == shopbranchId);
            model.VisitCounts = db.Sum<long>(d => d.VistiCounts);//浏览人数

            #region 下单量统计
            var orders = DbFactory.Default.Get<OrderInfo>().Where(e => e.OrderDate >= begin && e.OrderDate < end);
            if (shop > 0)
                orders = orders.Where(o => o.ShopId == shop);
            if (shopbranchId.HasValue)
                orders = orders.Where(o => o.ShopBranchId == shopbranchId);
            var orderList = orders.ToList();
            model.OrderCount = orderList.Count();//订单数
            model.OrderUserCount = orderList.Select(e => e.UserId).Distinct().Count();//下单人数
            model.OrderAmount = orderList.Sum(e => e.TotalAmount);//下单金额
            var orderids = orderList.Select(p => p.Id).ToList<long>();
            long orderProductQuantity = DbFactory.Default.Get<OrderItemInfo>()
                .Where(p => p.OrderId.ExIn(orderids))
                .Sum<long>(p => p.Quantity);
            model.OrderProductCount = orderProductQuantity;//下单件数
            #endregion

            #region 付款量统计
            var payOrders = DbFactory.Default.Get<OrderInfo>().Where(e => e.PayDate >= begin && e.PayDate < end);
            if (shop > 0)
                payOrders = payOrders.Where(o => o.ShopId == shop);
            if (shopbranchId.HasValue)
                payOrders = payOrders.Where(o => o.ShopBranchId == shopbranchId);
            var payOrderList = payOrders.ToList();
            model.OrderPayUserCount = payOrderList.Select(e => e.UserId).Distinct().Count();//付款人数
            model.OrderPayCount = payOrderList.Count();//付款订单数
            model.SaleAmounts = payOrderList.Sum(e => e.TotalAmount);//付款金额
            orderids = payOrderList.Select(p => p.Id).ToList<long>();
            orderProductQuantity = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderids)).Sum<long>(p => p.Quantity);
            model.SaleCounts = orderProductQuantity;//付款下单件数
            #endregion

            #region 退款量统计
            var refunds = DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed);
            if (shop > 0)
                refunds = refunds.Where(r => r.ShopId == shop);
            if (shopbranchId.HasValue)
            {
                var ids = DbFactory.Default.Get<OrderInfo>().Where(o => o.ShopBranchId == shopbranchId.Value).Select(i => i.Id).ToList<long>();
                refunds = refunds.Where(r => r.OrderId.ExIn(ids));
            }
            refunds = refunds.Where(p => p.ManagerConfirmDate >= begin && p.ManagerConfirmDate <= end);

            var refundList = refunds.ToList();
            model.OrderRefundProductCount = refundList.Sum(p => (long)p.ReturnQuantity);//退款件数
            model.OrderRefundAmount = refundList.Sum(p => (decimal)p.Amount);//退款金额
                        
            var _refundOrderCount = refundList.Select(p => p.OrderId).Distinct().Count();
            model.OrderRefundCount = _refundOrderCount;//退款订单数
            #endregion

            models.Add(model);
            return models;
        }

        public List<TradeStatisticModel> GetPlatVisits(DateTime begin, DateTime end)
        {
            return DbFactory.Default.Get<PlatVisitInfo>().Where(e => e.Date >= begin && e.Date < end).ToList<TradeStatisticModel>();
        }
        /// <summary>
        /// 获取商品销量排行数据
        /// </summary>
        /// <param name="shop"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="rankSize"></param>
        /// <returns>[ProductName:SaleCount]</returns>
        public List<ChartDataItem<long, int>> GetProductChartBySaleCount(long shop, DateTime begin, DateTime end, int rankSize)
        {
            var enddate = DateTime.Parse(end.ToShortTimeString()).AddHours(23).AddMinutes(59).AddSeconds(59);
            var data = DbFactory.Default
                .Get<ProductVistiInfo>()
                .LeftJoin<ProductInfo>((pv, p) => pv.ProductId == p.Id)
                .Where(pv => pv.Date >= begin && pv.Date < enddate)
                .Where<ProductInfo>(p => p.ShopId == shop && p.IsDeleted == false)
                .Select(pv => new { ItemKey = pv.ProductId, ItemValue = pv.SaleCounts.ExSum() })
                .Select<ProductInfo>(p => new { Expand = p.ProductName })
                .GroupBy(n => n.ProductId)
                .OrderByDescending(n => "ItemValue")
                .Take(rankSize);
            if (shop > 0)
                data.Where(p => p.ShopId == shop);
            return data.ToList<ChartDataItem<long, int>>();
        }
        /// <summary>
        /// 获取商品销售额排行数据
        /// </summary>
        /// <param name="shop">为0 不筛选</param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="rankSize"></param>
        /// <returns></returns>
        public List<ChartDataItem<long, decimal>> GetProductChartBySaleAmount(long shop, DateTime begin, DateTime end, int rankSize)
        {
            var enddate = DateTime.Parse(end.ToShortTimeString()).AddHours(23).AddMinutes(59).AddSeconds(59);
            var data = DbFactory.Default
                 .Get<ProductVistiInfo>()
                 .LeftJoin<ProductInfo>((pv, p) => pv.ProductId == p.Id)
                 .Where(pv => pv.Date >= begin && pv.Date < enddate)
                 .Where<ProductInfo>(p => p.IsDeleted == false)
                 .Select(pv => new { ItemKey = pv.ProductId, ItemValue = pv.SaleAmounts.ExSum() })
                 .Select<ProductInfo>(p => new { Expand = p.ProductName })
                 .GroupBy(n => n.ProductId)
                 .OrderByDescending(n => "ItemValue");
            if (shop > 0)
                data.Where(p => p.ShopId == shop);
            return data.ToList<ChartDataItem<long, decimal>>();
        }
        /// <summary>
        /// 获取商品访问量排行数据
        /// </summary>
        /// <returns></returns>
        public List<ChartDataItem<long, long>> GetProductChartByVisti(long shop, DateTime begin, DateTime end, int rankSize)
        {
            var enddate = DateTime.Parse(end.ToShortTimeString()).AddHours(23).AddMinutes(59).AddSeconds(59);
            var data = DbFactory.Default
              .Get<ProductVistiInfo>()
              .LeftJoin<ProductInfo>((pv, p) => pv.ProductId == p.Id)
              .Where(pv => pv.Date >= begin && pv.Date < enddate)
              .Where<ProductInfo>(p => p.IsDeleted == false)
              .Select(pv => new { ItemKey = pv.ProductId, ItemValue = pv.VistiCounts.ExSum() })
              .Select<ProductInfo>(p => new { Expand = p.ProductName })
              .GroupBy(n => n.ProductId)
              .OrderByDescending(n => "ItemValue");
            if (shop > 0)
                data.Where(p => p.ShopId == shop);
            return data.ToList<ChartDataItem<long, long>>();
        }
        /// <summary>
        /// 获取店铺销量数据
        /// </summary>
        /// <param name="shop"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<DateTime, int>> GetShopBySaleCount(long shop, DateTime begin, DateTime end)
        {
            return DbFactory.Default
                .Get<ShopVistiInfo>()
                .Where(p => p.ShopId == shop && p.Date >= begin && p.Date < end)
                .Select(m => new { ItemKey = m.Date, ItemValue = m.SaleCounts.ExSum() })
                .GroupBy(m => new { m.Date })
                .ToList<ChartDataItem<DateTime, int>>();
        }
        /// <summary>
        /// 获取店铺流量数据
        /// </summary>
        /// <param name="shop"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<DateTime, int>> GetShopFlowChart(long shop, DateTime begin, DateTime end)
        {
            return DbFactory.Default.Get<ShopVistiInfo>()
                .Where(p => p.ShopId == shop && p.Date >= begin && p.Date < end)
                .Select(m => new { ItemKey = m.Date, ItemValue = m.VistiCounts.ExSum() })
                .GroupBy(m => m.Date)
                .ToList<ChartDataItem<DateTime, int>>();
        }
        /// <summary>
        /// 获取门店转化率(SaleCounts/VistiCounts)
        /// </summary>
        /// <param name="shop"></param>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<DateTime, decimal>> GetConversionRate(long shop, DateTime begin, DateTime end)
        {
            return DbFactory.Default.Get<ShopVistiInfo>()
                .Where(m => m.ShopId == shop && m.Date >= begin && m.Date < end)
             .Select(m => new { m.Date, m.VistiCounts, m.SaleCounts, })
             .GroupBy(m => new { m.Date.Date })
             .ToList<dynamic>()
             .Select(item => new ChartDataItem<DateTime, decimal>
             {
                 ItemKey = (DateTime)item.Date,
                 ItemValue = item.VistiCounts > 0 ? ((decimal)item.SaleCounts) / item.VistiCounts : 0
             }).ToList();
        }
        /// <summary>
        /// 获取新增店铺数据
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<DateTime, int>> GetNewShopChart(DateTime begin, DateTime end)
        {
            var data = DbFactory.Default.Get<ShopInfo>().Where(m => m.CreateDate >= begin && m.CreateDate < end && m.Stage == ShopInfo.ShopStage.Finish)
               .Select(m => new { ItemKey = m.CreateDate.Date, ItemValue = m.ExCount(false) })
               .GroupBy(m => new { m.CreateDate.Date })
               .ToList<ChartDataItem<DateTime, int>>();
            return data;
        }
        /// <summary>
        /// 获取新增会员数据
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<DateTime, int>> GetMemberChart(DateTime begin, DateTime end)
        {
            var data = DbFactory.Default.Get<MemberInfo>().Where(m => m.CreateDate >= begin && m.CreateDate < end)
            .Select(m => new { ItemKey = m.CreateDate.Date, ItemValue = m.ExCount(false) })
            .GroupBy(m => new { m.CreateDate.Date })
            .ToList<ChartDataItem<DateTime, int>>();
            return data;
        }
        /// <summary>
        /// 获取店铺订单量排行数据
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="rankSize"></param>
        /// <returns>[ShopId,OrderCount,ShopName]</returns>
        public List<ChartDataItem<long, int>> GetShopRankingByOrderCount(DateTime begin, DateTime end, int rankSize)
        {
            return DbFactory.Default.Get<OrderInfo>()
              .LeftJoin<ShopInfo>((oi, si) => oi.ShopId == si.Id)
              .Where(o => o.OrderDate >= begin && o.OrderDate < end
                && o.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay
                && o.OrderStatus != OrderInfo.OrderOperateStatus.Close)
              .GroupBy(n => n.ShopId)
              .Select(o => new { ItemKey = o.ShopId, ItemValue = o.ExCount(false) })
              .Select<ShopInfo>(s => new { Expand = s.ShopName })
              .OrderByDescending(o => "ItemValue")
              .ToList<ChartDataItem<long, int>>();
        }
        /// <summary>
        /// 获取店铺销售额排行数据
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <param name="rankSize"></param>
        /// <returns>[ShopId,SaleAmount,ShopName]</returns>
        public List<ChartDataItem<long, decimal>> GetShopRankingBySaleAmount(DateTime begin, DateTime end, int rankSize)
        {
            return DbFactory.Default
              .Get<ShopVistiInfo>()
              .LeftJoin<ShopInfo>((v, s) => v.ShopId == s.Id)
              .Where(p => p.Date >= begin && p.Date < end)
              .GroupBy(s => s.ShopId)
              .Select(p => new { ItemKey = p.ShopId, ItemValue = p.SaleAmounts.ExSum() })
              .Select<ShopInfo>(s => new { Expand = s.ShopName })
              .OrderByDescending(n => "ItemValue")
              .Take(rankSize)
              .ToList<ChartDataItem<long, decimal>>();
        }
        /// <summary>
        /// 获取区域订单量
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<long, int>> GetAreaMapByOrderCount(DateTime begin, DateTime end)
        {
            //TODO:FG [BUG]自提订单需设置TopRegionId字段
            var exclude = new[] { OrderInfo.OrderOperateStatus.Close, OrderInfo.OrderOperateStatus.WaitPay };
            return DbFactory.Default.Get<OrderInfo>()
                .Where(p => p.OrderDate >= begin && p.OrderDate < end && p.OrderStatus.ExNotIn(exclude) && p.TopRegionId > 0)
                .GroupBy(p => p.TopRegionId)
                .Select(p => new { ItemKey = p.TopRegionId, ItemValue = p.ExCount(false) })
                .ToList<ChartDataItem<long, int>>();
        }
        /// <summary>
        /// 获取区域订单金额
        /// </summary>
        /// <param name="begin"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public List<ChartDataItem<long, decimal>> GetAreaMapByOrderAmount(DateTime begin, DateTime end)
        {
            //TODO:FG [BUG]自提订单需设置TopRegionId字段
            var exclude = new[] { OrderInfo.OrderOperateStatus.Close, OrderInfo.OrderOperateStatus.WaitPay };
            return DbFactory.Default.Get<OrderInfo>()
                .Where(p => p.OrderDate >= begin && p.OrderDate < end && p.OrderStatus.ExNotIn(exclude) && p.TopRegionId > 0)
                .GroupBy(p => p.TopRegionId)
                .Select(p => new { ItemKey = p.TopRegionId, ItemValue = (p.ProductTotalAmount + p.Freight + p.Tax).ExSum() })
                .ToList<ChartDataItem<long, decimal>>();
        }
        public void AddPlatVisitUser(DateTime date, long number)
        {
            //var platVisit = Context.PlatVisitsInfo.FirstOrDefault(e => e.Date == dt);
            var platVisit = DbFactory.Default.Get<PlatVisitInfo>().Where(e => e.Date == date).FirstOrDefault();
            if (platVisit == null)
            {
                platVisit = new PlatVisitInfo();
                platVisit.Date = date;
                platVisit.VisitCounts = 1;
                //Context.PlatVisitsInfo.Add(platVisit);
                DbFactory.Default.Add(platVisit);
            }
            else
            {
                platVisit.VisitCounts += number;
                DbFactory.Default.Update(platVisit);
            }
            //Context.SaveChanges();
        }
        public void AddShopVisitUser(DateTime date, long shop, long number)
        {
            date = date.Date;
            var success = DbFactory.Default.Set<ShopVistiInfo>()
                .Where(p => p.Date == date && p.ShopId == shop)
                .Set(s => s.VistiCounts, v => v.VistiCounts + number)
                .Succeed();
            if (!success)
            {
                DbFactory.Default.Add(new ShopVistiInfo
                {
                    Date = date,
                    VistiCounts = number,
                    ShopId = shop,
                    ShopBranchId = 0
                });
            }
        }

        public void AddShopBranchVisitUser(DateTime date, long shopId, long shopBranchId, long num)
        {
            date = date.Date;
            var success = DbFactory.Default.Set<ShopVistiInfo>()
                .Where(p => p.Date == date && p.ShopId == shopId && p.ShopBranchId == shopBranchId)
                .Set(s => s.VistiCounts, v => v.VistiCounts + num)
                .Succeed();
            if (!success)
            {
                DbFactory.Default.Add(new ShopVistiInfo
                {
                    Date = date,
                    VistiCounts = num,
                    ShopId = shopId,
                    ShopBranchId = shopBranchId
                });
            }
        }

        public void AddProductVisitUser(DateTime date, long product, long shop, long num)
        {
            date = date.Date;
            var success = DbFactory.Default.Set<ProductVistiInfo>()
               .Where(p => p.Date == date && p.ProductId == product && p.ShopId == shop)
               .Set(p => p.VisitUserCounts, n => n.VisitUserCounts + num)
               .Succeed();
            if (!success)
            {
                DbFactory.Default.Add(new ProductVistiInfo
                {
                    Date = date,
                    ShopId = shop,
                    ProductId = product,
                    VisitUserCounts = num
                });
            }
        }
        public void AddProductVisit(DateTime date, long product, long shop, long num)
        {
            date = date.Date;
            var success = DbFactory.Default.Set<ProductVistiInfo>()
                .Where(p => p.Date == date && p.ProductId == product && p.ShopId == shop)
                .Set(p => p.VistiCounts, n => n.VistiCounts + num)
                .Succeed();
            if (!success)
            {
                DbFactory.Default.Add(new ProductVistiInfo
                {
                    Date = date,
                    ShopId = shop,
                    ProductId = product,
                    VistiCounts = num
                });
            }
        }
        public void SettlementOrder(DateTime startDate, DateTime endDate)
        {
            //时间段内所有订单（下单数据统计）
            var orders = DbFactory.Default.Get<OrderInfo>().Where(e => e.OrderDate >= startDate && e.OrderDate < endDate).ToList();

            #region 商家统计   

            //商家订单分组         
            var orderShopGroups = orders.Where(o => o.ShopBranchId <= 0).GroupBy(e => e.ShopId);
            StatisticOrderCount(orderShopGroups, startDate, false);
            //门店订单分组
            var orderShopbranchGroups = orders.Where(o => o.ShopBranchId > 0).GroupBy(e => e.ShopBranchId);
            StatisticOrderCount(orderShopbranchGroups, startDate, true);


            //时间段内已支付订单(付款数据统计)
            //var payOrders = entity.OrderInfo.Where(e => e.PayDate.HasValue && e.PayDate.Value >= statisticStartDate && e.PayDate.Value < statisticEndDate).ToList();
            var payOrders = DbFactory.Default.Get<OrderInfo>().Where(e => e.PayDate >= startDate && e.PayDate < endDate).ToList();
            //商家付款订单分组
            var payOrderShopGroups = payOrders.Where(o => o.ShopBranchId <= 0).GroupBy(e => e.ShopId);
            StatisticOrderPayCount(payOrderShopGroups, startDate, endDate, false);
            //门店付款订单分组
            var payOrderShopBranchGroups = payOrders.Where(o => o.ShopBranchId > 0).GroupBy(e => e.ShopBranchId);
            StatisticOrderPayCount(payOrderShopBranchGroups, startDate, endDate, true);

            #endregion            

            #region 平台统计
            //PlatVisitsInfo platVisit = entity.PlatVisitsInfo.FirstOrDefault(e => e.Date == statisticStartDate);
            bool platIsAdd = false;
            PlatVisitInfo platVisit = DbFactory.Default.Get<PlatVisitInfo>().Where(e => e.Date == startDate).FirstOrDefault();
            if (platVisit == null)
            {
                platVisit = new PlatVisitInfo();
                //添加
                //entity.PlatVisitsInfo.Add(platVisit);
                platIsAdd = true;
            }
            platVisit.Date = startDate;
            platVisit.OrderCount = orders.Count();
            platVisit.OrderAmount = orders.Sum(e => e.TotalAmount);
            platVisit.OrderUserCount = orders.Select(e => e.UserId).Distinct().Count();

            var orderids1 = orders.Select(p => p.Id).ToList();
            long orderProductCount = 0;
            if (orderids1.Count() > 0)
                orderProductCount = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderids1)).Sum<long>(p => p.Quantity);
            platVisit.OrderProductCount = orderProductCount;
            //已支付订单
            platVisit.OrderPayCount = payOrders.Count();
            platVisit.OrderPayUserCount = payOrders.Select(e => e.UserId).Distinct().Count();
            platVisit.SaleAmounts = payOrders.Sum(e => e.TotalAmount);
            var orderids2 = payOrders.Select(p => p.Id).ToList();
            long saleCounts = 0;
            if (orderids2.Count() > 0)
                saleCounts = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderids2)).Sum<long>(p => p.Quantity);
            platVisit.SaleCounts = saleCounts;
            platVisit.StatisticFlag = true;
            if (platIsAdd)
                DbFactory.Default.Add(platVisit);
            else
                DbFactory.Default.Update(platVisit);
            #endregion
        }

        /// <summary>
        /// 下单量统计
        /// </summary>
        private void StatisticOrderCount(IEnumerable<IGrouping<long, OrderInfo>> orderGroups, DateTime statisticStartDate, bool isShopBranch)
        {
            List<ShopVistiInfo> shopVisitInfos = new List<ShopVistiInfo>();
            //已存在的店铺统计
            var shopids = orderGroups.Select(e => e.Key).Distinct().ToList();
            if (!isShopBranch)
            {
                if (orderGroups.Count() > 0)
                    shopVisitInfos = DbFactory.Default.Get<ShopVistiInfo>().Where(e => e.ShopId.ExIn(shopids) && e.Date == statisticStartDate).ToList();
            }
            else
            {
                if (orderGroups.Count() > 0)
                    shopVisitInfos = DbFactory.Default.Get<ShopVistiInfo>().Where(e => e.ShopBranchId.ExIn(shopids) && e.Date == statisticStartDate).ToList();
            }
            foreach (var g in orderGroups)
            {
                var item = g.ToList();
                var shopId = item.FirstOrDefault().ShopId;
                bool isAdd = false;
                ShopVistiInfo shopVisit = shopVisitInfos.FirstOrDefault(e => e.ShopId == g.Key && e.ShopBranchId <= 0);
                if (isShopBranch)
                    shopVisit = shopVisitInfos.FirstOrDefault(e => e.ShopBranchId == g.Key && e.ShopId == shopId);
                if (shopVisit == null)
                {
                    shopVisit = new ShopVistiInfo();
                    isAdd = true;
                }
                shopVisit.ShopBranchId = isShopBranch ? g.Key : 0;
                shopVisit.Date = statisticStartDate;
                shopVisit.ShopId = isShopBranch ? shopId : g.Key;
                shopVisit.OrderCount = item.Count();
                shopVisit.OrderAmount = item.Sum(e => e.TotalAmount);
                shopVisit.OrderUserCount = item.Select(e => e.UserId).Distinct().Count();
                var orderids = item.Select(p => p.Id).ToList();
                long orderProductQuantity = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderids)).Sum<long>(p => p.Quantity);
                shopVisit.OrderProductCount = orderProductQuantity;
                shopVisit.StatisticFlag = true;
                if (isAdd)
                    DbFactory.Default.Add(shopVisit);
                else
                    DbFactory.Default.Update(shopVisit);
            }

            //将没有订单记录的统计信息，统一修改为已统计
            var noOrdersStatistic = shopVisitInfos.Where(e => !shopids.Any(p => p == e.ShopId));
            foreach (var v in noOrdersStatistic)
            {
                DbFactory.Default.Set<ShopVistiInfo>().Set(p => p.StatisticFlag, true).Where(p => p.Id == v.Id).Succeed();
            }
        }

        /// <summary>
        /// 付款订单统计
        /// </summary>
        /// <param name="payOrderGroups"></param>
        /// <param name="statisticStartDate"></param>
        /// <param name="isShopBranch"></param>
        private void StatisticOrderPayCount(IEnumerable<IGrouping<long, OrderInfo>> payOrderGroups, DateTime statisticStartDate, DateTime statisticEndDate, bool isShopBranch)
        {
            List<ShopVistiInfo> shopVisitInfos = new List<ShopVistiInfo>();
            //已存在的店铺统计
            var shopids = payOrderGroups.Select(e => e.Key).Distinct().ToList();
            if (payOrderGroups.Count() > 0)
            {
                if (!isShopBranch)
                    shopVisitInfos = DbFactory.Default.Get<ShopVistiInfo>().Where(e => e.ShopId.ExIn(shopids) && e.Date == statisticStartDate).ToList();
                else
                    shopVisitInfos = DbFactory.Default.Get<ShopVistiInfo>().Where(e => e.ShopBranchId.ExIn(shopids) && e.Date == statisticStartDate).ToList();
            }
            foreach (var g in payOrderGroups)
            {
                var item = g.ToList();
                var shopId = item.FirstOrDefault().ShopId;
                var orderids = item.Select(p => p.Id).ToList();

                bool isAdd = false;
                ShopVistiInfo shopVisit = shopVisitInfos.FirstOrDefault(e => e.ShopId == g.Key && e.ShopBranchId <= 0);
                if (isShopBranch)
                    shopVisit = shopVisitInfos.FirstOrDefault(e => e.ShopBranchId == g.Key && e.ShopId == shopId);
                if (shopVisit == null)
                {
                    shopVisit = new ShopVistiInfo();
                    isAdd = true;
                }
                shopVisit.Date = statisticStartDate;
                shopVisit.ShopId = isShopBranch ? shopId : g.Key;
                shopVisit.ShopBranchId = isShopBranch ? g.Key : 0;
                shopVisit.OrderPayCount = item.Count();
                shopVisit.OrderPayUserCount = item.Select(e => e.UserId).Distinct().Count();
                shopVisit.SaleAmounts = item.Sum(e => e.TotalAmount);
                long orderProductQuantity = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId.ExIn(orderids)).Sum<long>(p => p.Quantity);
                shopVisit.SaleCounts = orderProductQuantity;

                //筛选时间内退款成功的订单
                var refunds = DbFactory.Default.Get<OrderRefundInfo>().Where(p => p.ManagerConfirmStatus == OrderRefundInfo.OrderRefundConfirmStatus.Confirmed);
                refunds = refunds.Where(p => p.ManagerConfirmDate >= statisticStartDate && p.ManagerConfirmDate <= statisticEndDate);
                if (isShopBranch)
                {
                    var ids = DbFactory.Default.Get<OrderInfo>().Where(o => o.ShopBranchId == g.Key).Select(i => i.Id).ToList<long>();
                    refunds = refunds.Where(r => r.OrderId.ExIn(ids));
                }
                else
                {
                    var ids = DbFactory.Default.Get<OrderInfo>().Where(o => o.ShopId == g.Key && o.ShopBranchId == 0).Select(i => i.Id).ToList<long>();
                    refunds = refunds.Where(r => r.OrderId.ExIn(ids));
                }

                var refundList = refunds.ToList();
                shopVisit.OrderRefundProductCount = refundList.Sum(p => (long)p.ReturnQuantity);//退款件数
                shopVisit.OrderRefundAmount = refundList.Sum(p => (decimal)p.Amount);//退款金额
                //退款订单数
                var _refundOrderCount = refundList.Select(p => p.OrderId).Distinct().Count();
                shopVisit.OrderRefundCount = _refundOrderCount;

                shopVisit.StatisticFlag = true;
                if (isAdd)
                    DbFactory.Default.Add(shopVisit);
                else
                    DbFactory.Default.Update(shopVisit);
            }
            //将没有订单记录的统计信息，统一修改为已统计
            var noPayOrdersStatistic = shopVisitInfos.Where(e => !shopids.Any(p => p == e.ShopId));
            foreach (var v in noPayOrdersStatistic)
            {
                DbFactory.Default.Set<ShopVistiInfo>().Set(p => p.StatisticFlag, true).Where(p => p.Id == v.Id).Succeed();
            }
        }
    }
}
