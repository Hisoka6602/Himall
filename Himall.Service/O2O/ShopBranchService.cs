using Himall.CommonModel;
using Himall.Core;
using Himall.Core.Helper;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Himall.Service
{
    public class ShopBranchService : ServiceBase
    {
        /// <summary>
        /// 添加分店
        /// </summary>
        /// <param name="branch"></param>
        public void AddShopBranch(ShopBranchInfo branch)
        {
            DbFactory.Default.Add(branch);
            CacheManager.Clear($"shopBranch:{branch.Id}");
        }

        /// <summary>
        /// 添加分店管理员
        /// </summary>
        /// <param name="manager"></param>
        public void AddShopBranchManagers(ShopBranchManagerInfo manager)
        {
            DbFactory.Default.Add(manager);
        }

        /// <summary>
        /// 判断门店名称是否重复
        /// </summary>
        /// <param name="shopId">商家店铺ID</param>
        /// <param name="shopBranchName">门店名字</param>
        /// <returns></returns>
        public bool Exists(long shopId, long shopBranchId, string shopBranchName)
        {
            return DbFactory.Default.Get<ShopBranchInfo>()
                .Where(e => e.ShopBranchName == shopBranchName && e.ShopId == shopId && e.Id != shopBranchId)
                .Exist();
        }

        /// <summary>
        /// 根据查询条件判断是否有门店
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool Exists(ShopBranchQuery query)
        {
            var shopBranchs = ToWhere(query);
            return shopBranchs.Exist();
        }

        public ShopBranchData GetShopBranchData(long id) =>
            CacheManager.GetOrCreate($"shopBranch:{id}", () =>
            {
                return DbFactory.Default.Get<ShopBranchInfo>()
                .LeftJoin<ShopBranchManagerInfo>((s, m) => s.Id == m.ShopBranchId)
                .Select()
                .Select<ShopBranchManagerInfo>(p => new
                {
                    p.UserName
                })
                .Where(e => e.Id == id).FirstOrDefault<ShopBranchData>();
            });
        public List<ShopBranchData> GetShopBranchData(List<long> list)
        {
            var result = new List<ShopBranchData>();
            foreach (var item in list)
            {
                var store = GetShopBranchData(item);
                if (store != null)
                    result.Add(store);
            }
            return result;
        }

        public ShopBranchInfo GetShopBranchById(long id)
        {
            return DbFactory.Default.Get<ShopBranchInfo>().Where(e => e.Id == id).FirstOrDefault();
        }

        /// <summary>
        /// 根据门店联系方式获取门店信息
        /// </summary>
        /// <param name="contact"></param>
        /// <returns></returns>
        public ShopBranchInfo GetShopBranchByContact(string contact)
        {
            return DbFactory.Default.Get<ShopBranchInfo>().Where(p => p.ContactPhone == contact).FirstOrDefault();
        }

        public QueryPageModel<ShopBranchInfo> GetShopBranchs(ShopBranchQuery query)
        {
            var shopBranchs = ToWhere(query);
            var models = shopBranchs.OrderBy(e => e.Id).ToPagedList(query.PageNo, query.PageSize);
            return new QueryPageModel<ShopBranchInfo>
            {
                Models = models,
                Total = models.TotalRecordCount
            };
        }

        /// <summary>
        /// 获取周边门店-分页
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<ShopBranchInfo> GetNearShopBranchs(ShopBranchQuery query)
        {
            decimal latitude = 0, longitude = 0;
            if (query.FromLatLng.Split(',').Length != 2) return new QueryPageModel<ShopBranchInfo>();
            latitude = TypeHelper.ObjectToDecimal(query.FromLatLng.Split(',')[0]);
            longitude = TypeHelper.ObjectToDecimal(query.FromLatLng.Split(',')[1]);

            QueryPageModel<ShopBranchInfo> result = new QueryPageModel<ShopBranchInfo>();
            string countsql = "select count(1) from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId ";
            string sql = string.Format("select AddressDetail,AddressPath,ContactPhone,sb.Id,ShopBranchName,Status,Latitude,Longitude,ServeRadius,truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({0}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({1}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2) AS Distance,DeliveFee,DeliveTotalFee,IsStoreDelive,IsAboveSelf,ShopId,ShopImages,IsRecommend,FreeMailFee,IsFreeMail,DaDaShopId,sb.IsAutoPrint,sb.PrintCount from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId ", latitude, longitude);

            Sql where = new Sql();
            where.Append(" where ShopStatus = 7 AND Longitude>0 and Latitude>0 ");//周边门店只取定位了经纬度的数据
            if (query.ShopId > 0)//取商家下的门店数据
                where.Append(" and ShopId=@0 ", query.ShopId);
            if (query.CityId > 0)//同城门店
                where.Append(" and AddressPath like concat('%',@0,'%') ", CommonConst.ADDRESS_PATH_SPLIT + query.CityId + CommonConst.ADDRESS_PATH_SPLIT);
            if (query.Status.HasValue)
                where.Append(" and Status=@0 ", query.Status.Value);

            Sql order = new Sql();
            GetSearchOrder(query, order);
            if (query.OrderKey == 2)
            {
                order.Append(", id desc ");//如果存在相同距离，则按ID再次排序
            }
            //暂时不考虑索引
            string page = GetSearchPage(query);

            result.Models = DbFactory.Default.Query<ShopBranchInfo>(string.Concat(sql, where.SQL, order.SQL, page), where.Arguments).ToList();
            result.Total = DbFactory.Default.ExecuteScalar<int>(string.Concat(countsql, where.SQL), where.Arguments);

            return result;
        }
        /// <summary>
        /// 搜索周边门店-分页
        /// </summary>
        /// <param name="search"></param>
        /// <returns>关键字包括对门店名称和门店商品的过滤</returns>
        public QueryPageModel<ShopBranchInfo> SearchNearShopBranchs(ShopBranchQuery search)
        {
            decimal latitude = 0, longitude = 0;
            if (search.FromLatLng.Split(',').Length != 2) return new QueryPageModel<ShopBranchInfo>();
            latitude = TypeHelper.ObjectToDecimal(search.FromLatLng.Split(',')[0]);
            longitude = TypeHelper.ObjectToDecimal(search.FromLatLng.Split(',')[1]);

            QueryPageModel<ShopBranchInfo> result = new QueryPageModel<ShopBranchInfo>();

            string countsql = "SELECT COUNT(0) FROM (select 1 from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId left JOIN Himall_ShopBranchInTag tag ON sb.Id = tag.ShopBranchId LEFT JOIN Himall_ShopBranchSku sbs ON sb.Id = sbs.ShopBranchId LEFT JOIN Himall_Product p ON sbs.ProductId = p.Id {0}) a ";
            string sql = string.Format("select AddressDetail,AddressPath,ContactPhone,sb.Id,ShopBranchName,sb.Status,Latitude,Longitude,ServeRadius,truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({0}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({1}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2) AS Distance,DeliveFee,DeliveTotalFee,IsStoreDelive,IsAboveSelf,sb.ShopId,ShopImages,IsRecommend,RecommendSequence,FreeMailFee,IsFreeMail,DaDaShopId,sb.IsAutoPrint,sb.PrintCount from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId LEFT JOIN Himall_ShopBranchSku sbs ON sb.Id = sbs.ShopBranchId LEFT JOIN Himall_Product p ON sbs.ProductId = p.Id left JOIN Himall_ShopBranchInTag tag ON sb.Id = tag.ShopBranchId ", latitude, longitude);
            Sql where = new Sql();
            GetWhereForSearch(search, where, latitude, longitude);
            Sql order = new Sql();
            GetSearchOrder(search, order);
            if (search.OrderKey == 2)
            {
                order.Append(", sb.id desc ");//如果存在相同距离，则按ID再次排序
            }
            //暂时不考虑索引
            string page = GetSearchPage(search);

            result.Models = DbFactory.Default.Query<ShopBranchInfo>(string.Concat(sql, where, order, page), where.Arguments).ToList();
            result.Total = DbFactory.Default.ExecuteScalar<int>(string.Format(countsql, where), where.Arguments);

            return result;
        }
        /// <summary>
        /// 搜索周边门店-分页
        /// </summary>
        /// <param name="search"></param>
        /// <returns>标签的过滤</returns>
        public QueryPageModel<ShopBranchInfo> TagsSearchNearShopBranchs(ShopBranchQuery search)
        {
            decimal latitude = 0, longitude = 0;
            if (search.FromLatLng.Split(',').Length != 2) return new QueryPageModel<ShopBranchInfo>();
            latitude = TypeHelper.ObjectToDecimal(search.FromLatLng.Split(',')[0]);
            longitude = TypeHelper.ObjectToDecimal(search.FromLatLng.Split(',')[1]);

            QueryPageModel<ShopBranchInfo> result = new QueryPageModel<ShopBranchInfo>();
            string countsql = "SELECT COUNT(0) FROM (select 1 from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId INNER JOIN Himall_ShopBranchInTag tag ON sb.Id = tag.ShopBranchId LEFT JOIN Himall_ShopBranchSku sbs ON sb.Id = sbs.ShopBranchId {0}) a ";
            string sql = string.Format("select AddressDetail,AddressPath,ContactPhone,sb.Id,ShopBranchName,sb.Status,Latitude,Longitude,ServeRadius,truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({0}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({1}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2) AS Distance,DeliveFee,DeliveTotalFee,IsStoreDelive,IsAboveSelf,sb.ShopId,ShopImages,IsRecommend,RecommendSequence,FreeMailFee,DaDaShopId,sb.IsAutoPrint,sb.PrintCount from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId INNER JOIN Himall_ShopBranchInTag tag ON sb.Id = tag.ShopBranchId LEFT JOIN Himall_ShopBranchSku sbs ON sb.Id = sbs.ShopBranchId ", latitude, longitude);
            var where = new Sql();
            GetWhereForSearch(search, where);
            var order = new Sql();
            GetSearchOrder(search, order);
            if (search.OrderKey == 2)
            {
                order.Append(", sb.id desc ");//如果存在相同距离，则按ID再次排序
            }
            //暂时不考虑索引
            string page = GetSearchPage(search);

            result.Models = DbFactory.Default.Query<ShopBranchInfo>(string.Concat(sql, where, order, page), where.Arguments).ToList();
            result.Total = DbFactory.Default.ExecuteScalar<int>(string.Format(countsql, where), where.Arguments);

            return result;
        }
        /// <summary>
        /// 根据商品搜索周边门店-分页
        /// </summary>
        /// <param name="search"></param>
        /// <returns>标签的过滤</returns>
        public QueryPageModel<ShopBranchInfo> StoreByProductNearShopBranchs(ShopBranchQuery search)
        {
            decimal latitude = 0, longitude = 0;
            if (search.FromLatLng.Split(',').Length != 2) return new QueryPageModel<ShopBranchInfo>();
            latitude = TypeHelper.ObjectToDecimal(search.FromLatLng.Split(',')[0]);
            longitude = TypeHelper.ObjectToDecimal(search.FromLatLng.Split(',')[1]);

            QueryPageModel<ShopBranchInfo> result = new QueryPageModel<ShopBranchInfo>();
            string countsql = "SELECT COUNT(0) FROM (select 1 from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId left JOIN Himall_ShopBranchInTag tag ON sb.Id = tag.ShopBranchId LEFT JOIN Himall_ShopBranchSku sbs ON sb.Id = sbs.ShopBranchId LEFT JOIN Himall_Product p ON sbs.ProductId = p.Id {0}) a ";
            string sql = string.Format("select AddressDetail,AddressPath,ContactPhone,sb.Id,ShopBranchName,sb.Status,Latitude,Longitude,ServeRadius,truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({0}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({1}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2) AS Distance,DeliveFee,DeliveTotalFee,IsFreeMail,IsStoreDelive,IsAboveSelf,sb.ShopId,ShopImages,IsRecommend,RecommendSequence,FreeMailFee,DaDaShopId,sb.IsAutoPrint,sb.PrintCount from himall_shopbranch sb LEFT JOIN Himall_Shop s ON s.Id = sb.ShopId LEFT JOIN Himall_ShopBranchSku sbs ON sb.Id = sbs.ShopBranchId LEFT JOIN Himall_Product p ON sbs.ProductId = p.Id left JOIN Himall_ShopBranchInTag tag ON sb.Id = tag.ShopBranchId ", latitude, longitude);
            var where = new Sql();
            GetWhereForSearch(search, where);
            var order = new Sql();
            GetStoreByProductOrder(search, order);
            if (search.OrderKey == 2)
            {
                order.Append(", sb.id desc ");//如果存在相同距离，则按ID再次排序
            }
            //暂时不考虑索引
            string page = GetSearchPage(search);

            result.Models = DbFactory.Default.Query<ShopBranchInfo>(string.Concat(sql, where, order, page), where.Arguments).ToList();
            result.Total = DbFactory.Default.ExecuteScalar<int>(string.Format(countsql, where), where.Arguments);

            return result;
        }

        public QueryPageModel<ShopBranchInfo> GetShopBranchsAll(ShopBranchQuery query)
        {
            var shopBranchs = ToWhere(query).ToList();
            shopBranchs.ForEach(p =>
            {
                if (p.Latitude > 0 && p.Longitude > 0)
                    p.Distance = GetLatLngDistancesFromAPI(query.FromLatLng, string.Format("{0},{1}", p.Latitude, p.Longitude));
                else
                    p.Distance = 0;
            });
            return new QueryPageModel<ShopBranchInfo>() { Models = shopBranchs.OrderBy(p => p.Distance).ToList() };
        }
        /// <summary>
        /// 搜索门店-不分页
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public List<ShopBranchInfo> GetAllShopBranchs(ShopBranchQuery query)
        {
            var shopBranchs = ToWhere(query);
            return shopBranchs.ToList();
        }
        /// <summary>
        /// 推荐门店
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public bool RecommendShopBranch(long[] list)
        {
            DbFactory.Default.Set<ShopBranchInfo>()
                .Where(p => p.Id.ExIn(list))
                .Set(p => p.IsRecommend, true)
                .Execute();
            foreach (var item in list)
                CacheManager.Clear($"shopBranch:{item}");
            return true;
        }

        /// <summary>
        /// 推荐门店排序
        /// </summary>
        /// <param name="oriShopBranchId"></param>
        /// <param name="newShopBranchId"></param>
        /// <returns></returns>
        public bool RecommendChangeSequence(long oriShopBranchId, long newShopBranchId)
        {
            var oriShopBranch = DbFactory.Default.Get<ShopBranchInfo>().Where(n => n.Id == oriShopBranchId).FirstOrDefault();
            var newShopBranch = DbFactory.Default.Get<ShopBranchInfo>().Where(n => n.Id == newShopBranchId).FirstOrDefault();
            if (null == oriShopBranch || null == newShopBranch) return false;
            var oriSequence = oriShopBranch.RecommendSequence;
            var newSequence = newShopBranch.RecommendSequence;
            oriShopBranch.RecommendSequence = newSequence;
            newShopBranch.RecommendSequence = oriSequence;
            return DbFactory.Default.InTransaction(() =>
            {
                var flag = DbFactory.Default.Update(oriShopBranch) > 0;
                if (!flag) return false;
                flag = DbFactory.Default.Update(newShopBranch) > 0;
                return flag;
            });
        }
        /// <summary>
        /// 取消推荐门店
        /// </summary>
        /// <param name="shopBranchId">门店ID</param>
        /// <returns></returns>
        public bool ResetShopBranchRecommend(long shopBranchId)
        {
            var shopBranch = DbFactory.Default.Get<ShopBranchInfo>().Where(n => n.Id == shopBranchId).FirstOrDefault();
            if (null == shopBranch) return false;
            shopBranch.IsRecommend = false;
            shopBranch.RecommendSequence = 0;
            var count = DbFactory.Default.Update(shopBranch);
            return count > 0;
        }
        /// <summary>
        /// 获取门店配送范围在同一区域的门店
        /// </summary>
        /// <param name="areaId"></param>
        /// <returns></returns>
        public QueryPageModel<ShopBranchInfo> GetArealShopBranchsAll(int areaId, int shopId, float latitude, float longitude)
        {
            return new QueryPageModel<ShopBranchInfo>() { Models = ToArealShopWhere(areaId, shopId, latitude, longitude).ToList() };
        }
        /// <summary>
        /// 获取一个起点坐标到多个终点坐标之间的距离
        /// </summary>
        /// <param name="fromPoint"></param>
        /// <param name="toLatLngs"></param>
        /// <returns></returns>
        public double GetLatLngDistancesFromAPI(string fromPoint, string toPoint)
        {
            if (!string.IsNullOrWhiteSpace(fromPoint) && (!string.IsNullOrWhiteSpace(toPoint)))
            {
                try
                {
                    var aryLatlng = fromPoint.Split(',');
                    var fromlat = double.Parse(aryLatlng[0]);
                    var fromlng = double.Parse(aryLatlng[1]);
                    double EARTH_RADIUS = 6378.137;//地球半径

                    var aryToLatlng = toPoint.Split(',');
                    var tolat = double.Parse(aryToLatlng[0]);
                    var tolng = double.Parse(aryToLatlng[1]);
                    var fromRadLat = fromlat * Math.PI / 180.0;
                    var toRadLat = tolat * Math.PI / 180.0;
                    double a = fromRadLat - toRadLat;
                    double b = (fromlng * Math.PI / 180.0) - (tolng * Math.PI / 180.0);
                    double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
                     Math.Cos(fromRadLat) * Math.Cos(toRadLat) * Math.Pow(Math.Sin(b / 2), 2)));
                    s = s * EARTH_RADIUS;
                    return Math.Round((Math.Round(s * 10000) / 10000), 2);
                }
                catch (Exception ex)
                {
                    Core.Log.Error("计算经纬度距离异常", ex);
                    return 0;
                }
            }
            return 0;
        }

        /// <summary>
        /// 根据分店id获取分店信息
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public List<ShopBranchInfo> GetShopBranchs(List<long> ids)
        {
            return DbFactory.Default.Get<ShopBranchInfo>().Where(p => p.Id.ExIn(ids)).ToList();
        }

        public void FreezeShopBranch(long shopBranchId)
        {
            DbFactory.Default.Set<ShopBranchSkuInfo>()
                .Set(n => n.Status, ShopBranchSkuStatus.InStock)
                .Where(p => p.Status == ShopBranchSkuStatus.Normal && p.ShopBranchId == shopBranchId)
                .Succeed();
            DbFactory.Default.Set<ShopBranchInfo>()
                .Set(n => n.Status, ShopBranchStatus.Freeze)
                .Where(p => p.Id == shopBranchId)
                .Succeed();
            CacheManager.Clear($"shopBranch:{shopBranchId}");
        }

        public void UnFreezeShopBranch(long shopBranchId)
        {
            DbFactory.Default.Set<ShopBranchInfo>()
                .Set(n => n.Status, ShopBranchStatus.Normal)
                .Where(p => p.Id == shopBranchId)
                .Succeed();
            CacheManager.Clear($"shopBranch:{shopBranchId}");
        }

        public List<ShopBranchManagerInfo> GetShopBranchManagers(long branchId)
        {
            return DbFactory.Default.Get<ShopBranchManagerInfo>().Where(e => e.ShopBranchId == branchId).ToList();
        }

        public void UpdateShopBranch(ShopBranchInfo shopBranch)
        {
            var branchEntity = DbFactory.Default.Get<ShopBranchInfo>().Where(e => e.Id == shopBranch.Id).FirstOrDefault();
            if (branchEntity == null)
                throw new MessageException(ExceptionMessages.NoFound, "门店");
            branchEntity.ShopBranchName = shopBranch.ShopBranchName;
            branchEntity.AddressDetail = shopBranch.AddressDetail;
            branchEntity.AddressId = shopBranch.AddressId;
            branchEntity.ContactPhone = shopBranch.ContactPhone;
            branchEntity.ContactUser = shopBranch.ContactUser;
            branchEntity.AddressPath = shopBranch.AddressPath;
            branchEntity.ShopImages = shopBranch.ShopImages;
            branchEntity.Longitude = shopBranch.Longitude;
            branchEntity.Latitude = shopBranch.Latitude;
            branchEntity.ServeRadius = shopBranch.ServeRadius;
            branchEntity.IsAboveSelf = shopBranch.IsAboveSelf;
            branchEntity.IsStoreDelive = shopBranch.IsStoreDelive;
            branchEntity.DeliveFee = shopBranch.DeliveFee;
            branchEntity.DeliveTotalFee = shopBranch.DeliveTotalFee;
            branchEntity.StoreOpenStartTime = shopBranch.StoreOpenStartTime;
            branchEntity.StoreOpenEndTime = shopBranch.StoreOpenEndTime;
            branchEntity.FreeMailFee = shopBranch.FreeMailFee;
            branchEntity.DaDaShopId = shopBranch.DaDaShopId;
            branchEntity.IsAutoPrint = shopBranch.IsAutoPrint;
            branchEntity.PrintCount = shopBranch.PrintCount;
            branchEntity.IsFreeMail = shopBranch.IsFreeMail;
            branchEntity.IsShelvesProduct = shopBranch.IsShelvesProduct;
            DbFactory.Default.Update(branchEntity);
            CacheManager.Clear($"shopBranch:{shopBranch.Id}");
        }

        /// <summary>
        /// 修改门店管理密码
        /// </summary>
        /// <param name="managerId">管理员ID</param>
        /// <param name="password">密码</param>
        public void SetShopBranchManagerPassword(long managerId, string password, string passwordSalt)
        {
            var result = DbFactory.Default.Set<ShopBranchManagerInfo>()
              .Set(n => n.Password, password)
              .Set(n => n.PasswordSalt, passwordSalt)
              .Where(p => p.Id == managerId)
              .Succeed();
        }

        /// <summary>
        /// 删除门店
        /// </summary>
        /// <param name="id"></param>
        public void DeleteShopBranch(long id)
        {
            DbFactory.Default.Del<ShopBranchInfo>(p => p.Id == id);
            CacheManager.Clear($"shopBranch:{id}");
        }

        /// <summary>
        /// 获取分店经营的商品SKU
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="shopBranchIds"></param>
        /// <param name="status">null表示所有</param>
        /// <returns></returns>
        public List<ShopBranchSkuInfo> GetSkus(long shopId, List<long> shopBranchIds, ShopBranchSkuStatus? status = ShopBranchSkuStatus.Normal, List<string> skuids = null)
        {
            var db = DbFactory.Default.Get<ShopBranchSkuInfo>()
                .Where(p => p.ShopId == shopId && p.ShopBranchId.ExIn(shopBranchIds));
            if (status.HasValue)
                db.Where(p => p.Status == status.Value);
            if (skuids != null && skuids.Count() > 0)
                db.Where(p => p.SkuId.ExIn(skuids));

            return db.ToList();
        }

        /// <summary>
        /// 获取门店可用商品SKU
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="branchs"></param>
        /// <returns></returns>
        public List<ShopBranchSkuInfo> GetSkus(long shopId, List<long> branchs)
        {
            return GetSkus(shopId, branchs, ShopBranchSkuStatus.Normal, null);
        }

        public List<ShopBranchSkuInfo> GetSkusByBranchIds(List<long> shopBranchIds, List<string> skuids = null)
        {
            var sql = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(p => p.ShopBranchId.ExIn(shopBranchIds));
            if (skuids != null && skuids.Count() > 0)
            {
                sql = sql.Where(p => p.SkuId.ExIn(skuids));
            }
            return sql.ToList();
        }

        /// <summary>
        /// 根据SKUID取门店SKU
        /// </summary>
        /// <param name="skuIds"></param>
        /// <returns></returns>
        public List<ShopBranchSkuInfo> GetSkusByIds(long branch, List<string> skuIds)
        {
            return DbFactory.Default.Get<ShopBranchSkuInfo>().Where(p => p.SkuId.ExIn(skuIds) && p.ShopBranchId == branch).ToList();
        }

        /// <summary>
        /// 根据ID取门店管理员
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ShopBranchManagerInfo GetShopBranchManagersById(long id)
        {
            return DbFactory.Default.Get<ShopBranchManagerInfo>().Where(e => e.Id == id).FirstOrDefault();
        }


        public ShopBranchManagerInfo GetShopBranchManagersByName(string userName)
        {
            return DbFactory.Default.Get<ShopBranchManagerInfo>().Where(e => e.UserName == userName).FirstOrDefault();
        }

        /// <summary>
        /// 添加门店sku
        /// </summary>
        /// <param name="infos"></param>
        public void AddSkus(List<ShopBranchSkuInfo> infos)
        {
            DbFactory.Default.Add<ShopBranchSkuInfo>(infos);
        }

        #region 修改库存

        public void SetStock(long branch, string skuId, int stock, StockOptionType option)
        {
            var sku = DbFactory.Default.Set<ShopBranchSkuInfo>().Where(p => p.ShopBranchId == branch && p.SkuId == skuId);
            switch (option)
            {
                case StockOptionType.Add: sku.Set(p => p.Stock, n => n.Stock + stock); break;
                case StockOptionType.Reduce: sku.Set(p => p.Stock, n => n.Stock - stock); break;
                case StockOptionType.Normal: sku.Set(p => p.Stock, stock); break;
            }
            sku.Succeed();
        }
        public void SetStock(long branch, Dictionary<string, int> changes, StockOptionType option)
        {
            foreach (var item in changes)
                SetStock(branch, item.Key, item.Value, option);
        }
        public void SetProductStock(long branch, long productId, int stock, StockOptionType option)
        {
            var product = DbFactory.Default.Set<ShopBranchSkuInfo>().Where(p => p.ShopBranchId == branch && p.ProductId == productId);
            switch (option)
            {
                case StockOptionType.Add: product.Set(p => p.Stock, n => n.Stock + stock); break;
                case StockOptionType.Reduce: product.Set(p => p.Stock, n => n.Stock - stock); break;
                case StockOptionType.Normal: product.Set(p => p.Stock, stock); break;
            }
            product.Succeed();
        }
        #endregion 修改库存

        public void SetBranchProductStatus(long branchId, List<long> products, ShopBranchSkuStatus status)
        {
            DbFactory.Default.Set<ShopBranchSkuInfo>()
                .Where(p => p.ShopBranchId == branchId && p.ProductId.ExIn(products))
                .Set(p => p.Status, status).Succeed();
        }

        public void SetBranchProductStatus(long productId, ShopBranchSkuStatus status)
        {
            var flag = DbFactory.Default.Set<ShopBranchSkuInfo>().Set(n => n.Status, status).Where(p => p.ProductId == productId).Succeed();
        }

        public Dictionary<long, long> GetProductSaleCount(long? branch, List<long> products, DateTime? begin = null, DateTime? end = null)
        {
            var orderdb = DbFactory.Default.Get<OrderInfo>().Where(p => p.OrderStatus != OrderInfo.OrderOperateStatus.Close && p.ShopBranchId == branch);
            if (branch.HasValue)
                orderdb.Where(p => p.ShopBranchId >= branch.Value);
            if (begin.HasValue)
                orderdb.Where(p => p.OrderDate >= begin.Value);
            if (end.HasValue)
                orderdb.Where(p => p.OrderDate < end.Value);

            return DbFactory.Default.Get<OrderItemInfo>().Where(t => t.OrderId.ExIn(orderdb.Select(p => p.Id)))
                .GroupBy(t => t.ProductId)
                .Select(t => new { Item1 = t.ProductId, Item2 = (t.Quantity - t.ReturnQuantity).ExSum() })
                .ToList<SimpItem<long, long>>()
                .ToDictionary(t => t.Item1, v => v.Item2);
        }


        public QueryPageModel<ProductInfo> SearchProduct(ShopBranchProductQuery query)
        {
            var db = WhereBuilder(query);

            if (query.IsSelectShopBranchProductSaleCount)
            {
                #region 查询门店商品销量统计
                var cateIds = DbFactory.Default.Get<ProductShopCategoryInfo>().Where<ProductInfo>((oi, si) => si.Id == oi.ProductId)
                          .Select(p => p.ShopCategoryId.ExMax());

                var cates = DbFactory.Default.Get<ShopCategoryInfo>().Where(p => p.Id.ExIn(cateIds))
                    .Select<ShopCategoryInfo>(p => p.DisplaySequence);

                //从有效订单内统计该门店该时间段该商品的销量
                var dbor = DbFactory.Default.Get<OrderInfo>().Where<OrderInfo>(p => p.OrderStatus != OrderInfo.OrderOperateStatus.Close && p.ShopBranchId == query.ShopBranchId && p.OrderStatus != OrderInfo.OrderOperateStatus.WaitPay);
                if (query.SaleStartDate.HasValue)
                    dbor.Where(t => t.OrderDate >= query.SaleStartDate);
                if (query.SaleEndDate.HasValue)
                    dbor.Where(t => t.OrderDate < query.SaleEndDate);
                var orderIds = dbor.Select(a => a.Id);
                var saleCounts = DbFactory.Default.Get<OrderItemInfo>().Where<ProductInfo>((oi, si) => oi.OrderId.ExIn(orderIds) && oi.ProductId == si.Id)
                    .Select(p => ((p.Quantity - p.ReturnQuantity).ExSum()));

                db.Select().Select(p => new { ShopDisplaySequence = cates.ExResolve<long>(), ShopBranchSaleCounts = (saleCounts.ExResolve<long>().ExIfNull(0) + p.VirtualSaleCounts.ExIfNull(0)) }).OrderBy(p => "ShopDisplaySequence").OrderByDescending(p => "ShopBranchSaleCounts");


                var data = db.ToPagedList(query.PageNo, query.PageSize);
                return new QueryPageModel<ProductInfo>()
                {
                    Total = data.TotalRecordCount,
                    Models = data
                };
                #endregion
            }

            var data1 = db.ToPagedList(query.PageNo, query.PageSize);
            return new QueryPageModel<ProductInfo>()
            {
                Total = data1.TotalRecordCount,
                Models = data1
            };
        }



        private GetBuilder<ProductInfo> WhereBuilder(ShopBranchProductQuery query)
        {
            var db = DbFactory.Default.Get<ProductInfo>();
            //过滤已删除的商品
            db = db.Where(item => item.IsDeleted == false);

            if (query.FilterVirtualProduct.HasValue && query.FilterVirtualProduct.Value)
                db = db.Where(item => item.ProductType == 0);

            if (query.Ids != null && query.Ids.Count() > 0)//条件 编号
                db = db.Where(item => item.Id.ExIn(query.Ids));

            if (!string.IsNullOrWhiteSpace(query.ProductCode))
                db = db.Where(item => item.ProductCode == query.ProductCode);

            if (query.ProductId.HasValue && query.ProductId.Value > 0)//商品Id
                db = db.Where(item => item.Id == query.ProductId);

            if (query.RproductId.HasValue)//商品Id
                db = db.Where(item => item.Id != query.RproductId);

            if (query.ShopId.HasValue)//过滤店铺
            {
                db = db.Where(item => item.ShopId == query.ShopId);
                if (query.IsOverSafeStock.HasValue)
                {
                    List<long> pids = new List<long>();
                    if (query.IsOverSafeStock.Value)
                        pids = DbFactory.Default.Get<SKUInfo>().Where(e => e.SafeStock >= e.Stock && e.ProductId.ExIn(pids)).Select(e => e.ProductId).ToList<long>();
                    else
                        pids = DbFactory.Default.Get<SKUInfo>().Where(e => e.SafeStock < e.Stock && e.ProductId.ExIn(pids)).Select(e => e.ProductId).ToList<long>();
                    db = db.Where(e => e.Id.ExIn(pids));
                }
            }
            if (query.AuditStatus != null)//条件 审核状态
                db = db.Where(item => item.AuditStatus.ExIn(query.AuditStatus));

            if (query.SaleStatus.HasValue)
                db = db.Where(item => item.SaleStatus == query.SaleStatus);

            if (query.CategoryId.HasValue)//条件 分类编号
                db = db.Where(item => ("|" + item.CategoryPath + "|").Contains("|" + query.CategoryId.Value + "|"));

            if (query.NotIncludedInDraft)
                db = db.Where(item => item.SaleStatus != ProductInfo.ProductSaleStatus.InDraft);

            if (query.HasLadderProduct.HasValue && query.HasLadderProduct == false)
                db = db.Where(item => item.IsOpenLadder == false);

            if (query.StartDate.HasValue)//添加日期筛选
                db = db.Where(item => item.AddedDate >= query.StartDate);
            if (query.EndDate.HasValue)//添加日期筛选
            {
                var end = query.EndDate.Value.Date.AddDays(1);
                db = db.Where(item => item.AddedDate < end);
            }
            if (!string.IsNullOrWhiteSpace(query.KeyWords))// 条件 关键字
                db = db.Where(item => item.ProductName.Contains(query.KeyWords));

            if (!string.IsNullOrWhiteSpace(query.ShopName))//查询商家关键字
            {
                var shopIds = DbFactory.Default.Get<ShopInfo>().Where(item => item.ShopName.Contains(query.ShopName)).Select(item => item.Id).ToList<long>();
                db = db.Where(item => item.ShopId.ExIn(shopIds));
            }
            if (query.IsLimitTimeBuy)
            {
                var limits = DbFactory.Default.Get<FlashSaleInfo>().Where(l => l.Status == FlashSaleInfo.FlashSaleStatus.Ongoing).Select(l => l.ProductId).ToList<long>();
                db = db.Where(p => p.Id.ExNotIn(limits));
            }
            if (query.ShopBranchId.HasValue && query.ShopBranchId.Value != 0)
            {//过滤门店已选商品
                var pid = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == query.ShopBranchId.Value).Select(item => item.ProductId).Distinct().ToList<long>();
                db = db.Where(e => e.Id.ExIn(pid));
            }
            if (query.ShopBranchProductStatus.HasValue)
            {//门店商品状态
                var pid = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == query.ShopBranchId.Value && e.Status == query.ShopBranchProductStatus.Value).Select(item => item.ProductId).Distinct().ToList<long>();
                if (query.ShopBranchProductStatus.Value == ShopBranchSkuStatus.Normal)
                {
                    db = db.Where(e => e.Id.ExIn(pid) && e.IsOpenLadder == false);
                }
                else
                {
                    db = db.Where(e => e.Id.ExIn(pid) || e.IsOpenLadder == true);
                }
            }

            long shopCateogryId = query.ShopCategoryId.GetValueOrDefault();
            if (shopCateogryId > 0)
            {
                //店铺分类
                if (query.ShopCategoryId.HasValue)
                {
                    //门店分类下的子分类
                    var childcateIds = DbFactory.Default.Get<ShopCategoryInfo>().Where(e => e.ParentCategoryId == shopCateogryId && e.IsShow).Select(t => t.Id);

                    //门店分类下商品Id
                    var productIds = DbFactory.Default.Get<ProductShopCategoryInfo>().Where(e => e.ShopCategoryId == shopCateogryId || e.ShopCategoryId.ExIn(childcateIds))
                        .Select(t => t.ProductId).Distinct();

                    db.Where(item => item.Id.ExIn(productIds));
                }
                else
                    db = db.Where(item => item.Id == 0);
            }
            if (query.CounpId.HasValue && query.CounpId.Value > 0)
            {
                var productIds = DbFactory.Default.Get<CouponProductInfo>()
                    .Where(c => c.CouponId == query.CounpId.Value).Select(c => c.ProductId);
                db.Where(item => item.Id.ExIn(productIds));
            }

            return db;
        }

        public bool CheckProductIsExist(long shopBranchId, long productId)
        {
            return DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == shopBranchId && e.ProductId == productId).Exist();
        }

        /// <summary>
        /// 门店分类下是否存在正常商品
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <returns></returns>
        public bool CheckShopCategoryProductIsExist(long shopId, long shopBranchId, long shopCategoryId)
        {
            var db = DbFactory.Default.Get<ProductInfo>();
            db = db.Where(e => e.ShopId == shopId && e.IsDeleted == false && e.IsOpenLadder == false);//过滤已删除的商品和没开启阶梯价商品

            //门店分类下的子分类
            var childcateIds = DbFactory.Default.Get<ShopCategoryInfo>().Where(e => e.ParentCategoryId == shopCategoryId && e.IsShow).Select(t => t.Id);

            //门店分类下商品Id
            var cateproIds = DbFactory.Default.Get<ProductShopCategoryInfo>().Where(e => e.ShopCategoryId == shopCategoryId || e.ShopCategoryId.ExIn(childcateIds))
                .Select(t => t.ProductId).Distinct();

            //门店下正常商品
            var branchpro = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == shopBranchId && e.Status == ShopBranchSkuStatus.Normal)
                .Select(t => t.ProductId).Distinct();

            return db.Where(e => e.Id.ExIn(cateproIds) && e.Id.ExIn(branchpro)).Exist();
        }

        public long GetVirtualSaleCounts(long branchId)
        {
            var products = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(p => p.ShopBranchId == branchId && p.Status == ShopBranchSkuStatus.Normal).Select(p => p.ProductId);
            return DbFactory.Default.Get<ProductInfo>()
               .Where(p => p.IsDeleted == false && p.SaleStatus == ProductInfo.ProductSaleStatus.OnSale
               && p.AuditStatus == ProductInfo.ProductAuditStatus.Audited && p.Id.ExIn(products))
               .Sum<long>(p => p.VirtualSaleCounts.ExIfNull(0));
        }
        #region 私有方法
        private GetBuilder<ShopBranchInfo> ToWhere(ShopBranchQuery query)
        {
            var shopBranchs = DbFactory.Default.Get<ShopBranchInfo>();
            if (query.ShopBranchTagId.HasValue && query.ShopBranchTagId.Value > 0)
            {
                var sbids = DbFactory.Default.Get<ShopBranchInTagInfo>().Where(x => x.ShopBranchTagId == query.ShopBranchTagId.Value).Select(p => p.ShopBranchId).Distinct().ToList<long>();
                shopBranchs = shopBranchs.Where(p => p.Id.ExIn(sbids));  //查询单个门店的时候要用到
            }
            if (query.Id > 0)
            {
                shopBranchs = shopBranchs.Where(p => p.Id == query.Id);//查询单个门店的时候要用到
            }
            if (query.ShopId > 0)//取商家下的门店数据
            {
                shopBranchs = shopBranchs.Where(p => p.ShopId == query.ShopId);
            }
            if (query.CityId > 0)//同城市的门店 省,市,区,街
            {
                shopBranchs = shopBranchs.Where(p => p.AddressPath.Contains(CommonConst.ADDRESS_PATH_SPLIT + query.CityId + CommonConst.ADDRESS_PATH_SPLIT));
            }
            if (query.ProvinceId > 0)//同省的门店 省,市,区,街
            {
                shopBranchs = shopBranchs.Where(p => p.AddressPath.Contains(query.ProvinceId + CommonConst.ADDRESS_PATH_SPLIT));
            }
            if (!string.IsNullOrWhiteSpace(query.ShopBranchName))
            {
                shopBranchs = shopBranchs.Where(e => e.ShopBranchName.Contains(query.ShopBranchName));
            }
            if (!string.IsNullOrWhiteSpace(query.ContactUser))
            {
                shopBranchs = shopBranchs.Where(e => e.ContactUser.Contains(query.ContactUser));
            }
            if (!string.IsNullOrWhiteSpace(query.ContactPhone))
            {
                shopBranchs = shopBranchs.Where(e => e.ContactPhone.Contains(query.ContactPhone));
            }
            if (query.Status.HasValue)
            {
                var status = query.Status.Value;
                shopBranchs = shopBranchs.Where(e => e.Status == status);
            }
            if (!string.IsNullOrWhiteSpace(query.AddressPath))
            {
                var addressPath = query.AddressPath;
                if (!addressPath.EndsWith(CommonConst.ADDRESS_PATH_SPLIT))
                    addressPath += CommonConst.ADDRESS_PATH_SPLIT;
                shopBranchs = shopBranchs.Where(p => p.AddressPath.StartsWith(addressPath));
            }
            if (query.ProductIds != null && query.ProductIds.Length > 0)
            {
                var pids = query.ProductIds.Distinct().ToList();
                var length = pids.Count();
                var _sbsql = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(p => p.ShopId == query.ShopId && p.ProductId.ExIn(pids));
                if (query.ShopBranchProductStatus.HasValue)
                {
                    _sbsql = _sbsql.Where(p => p.Status == query.ShopBranchProductStatus.Value);
                }

                //新的框架里在上面GroupBy两次它havingd的count只等于1，则多条产品时取having不满足，则改为先groupby一次先放list里在groupby一次
                var branchlist = _sbsql.GroupBy(p => new { p.ShopBranchId, p.ProductId }).Select(p => new { p.ShopBranchId, p.ProductId }).ToList();
                var shopBranchIds = branchlist.GroupBy(p => p.ShopBranchId).Where(p => p.Count() == length).Select(p => p.Key);

                shopBranchs = shopBranchs.Where(p => p.Id.ExIn(shopBranchIds));
            }
            if (query.IsRecommend.HasValue)
            {
                shopBranchs = shopBranchs.Where(p => p.IsRecommend == query.IsRecommend.Value);
            }

            //是否门店配送
            if (query.IsStoreDelive.HasValue)
                shopBranchs = shopBranchs.Where(p => p.IsStoreDelive == query.IsStoreDelive.Value);

            //是否上门自提
            if (query.IsAboveSelf.HasValue)
                shopBranchs = shopBranchs.Where(p => p.IsAboveSelf == query.IsAboveSelf.Value);
            return shopBranchs;
        }
        #endregion


        public List<ShopBranchSkuInfo> SearchShopBranchSkus(long shopBranchId, DateTime? startDate, DateTime? endDate)
        {
            var branchSkus = DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == shopBranchId);
            if (startDate.HasValue)
            {
                var start = startDate.Value.Date;
                branchSkus = branchSkus.Where(e => e.CreateDate >= start);
            }
            if (endDate.HasValue)
            {
                var end = endDate.Value.Date.AddDays(1);
                branchSkus = branchSkus.Where(e => e.CreateDate < end);
            }
            return branchSkus.ToList();

        }

        /// <summary>
        /// 获取门店配送范围在同一区域的正常门店
        /// </summary>
        /// <param name="areaId"></param>
        /// <returns></returns>
        private List<ShopBranchInfo> ToArealShopWhere(int areaId, int shopId, float latitude, float longitude)
        {
            string strSql = @"SELECT Id,ShopId,ShopBranchName,AddressId,AddressPath,AddressDetail,ContactUser,ContactPhone,Status,CreateDate,ServeRadius,
                Longitude,Latitude,ShopImages,IsStoreDelive,IsAboveSelf,StoreOpenStartTime,StoreOpenEndTime,IsRecommend,RecommendSequence,DeliveFee,DeliveTotalFee,
                DaDaShopId,IsAutoPrint,PrintCount,
                FreeMailFee,truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({1}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({2}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2) AS Distance
                FROM Himall_ShopBranch WHERE Status = 0 AND Longitude> 0 AND Latitude > 0 ";
            StringBuilder where = new StringBuilder();
            if (shopId > 0)
            {
                where.Append(" AND ShopId = {3} ");
            }
            if (areaId > 0)
            {
                where.Append(" AND AddressPath LIKE '%,{4},%' ");
            }
            if (latitude > 0 && longitude > 0)
            {
                where.Append(" AND ServeRadius > truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({1}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({2}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2) ");
            }
            string order = " ORDER BY Distance ";
            //var shopBranchs = Context.Database.SqlQuery<ShopBranchInfo>(string.Format(strSql + where.ToString() + order, latitude, latitude, longitude, shopId, areaId));
            var shopBranchs = DbFactory.Default.Query<ShopBranchInfo>(string.Format(strSql + where.ToString() + order, latitude, latitude, longitude, shopId, areaId)).ToList();
            return shopBranchs;//.Where(n => n.ServeRadius > n.Distance);
        }
        public List<ShopBranchInfo> GetShopBranchByShopId(long shopId)
        {
            return DbFactory.Default.Get<ShopBranchInfo>().Where(e => e.ShopId == shopId).ToList();
        }

        private Dictionary<long, decimal> GetNearbyStores(long shopId, long city, decimal longitude, decimal latitude)
        {
            //查询同城就近门店
            var sql = $"select id as Item1,st_distance(point(longitude,latitude),point({longitude},{latitude}))*111195 as Item2 from himall_shopbranch where shopId={shopId} and addressPath like '%,{city},%' order by Item2";
            return DbFactory.Default.Query<SimpItem<long, decimal>>(sql).ToDictionary(k => k.Item1, v => v.Item2);
        }

        /// <summary>
        /// 分配门店最近可用门店
        /// </summary>
        public long AllotStore(long shopId, long city, decimal longitude, decimal latitude, Dictionary<string, int> items)
        {
            var stores = GetNearbyStores(shopId, city, longitude, latitude).Keys.ToList();
            //查询门店库存
            var branchStocks = DbFactory.Default.Get<ShopBranchSkuInfo>()
               .Where(p => p.ShopBranchId.ExIn(stores) && p.SkuId.ExIn(items.Keys) && p.Status == ShopBranchSkuStatus.Normal)
               .Select(p => new
               {
                   Item1 = p.ShopBranchId,
                   Item2 = p.SkuId,
                   Item3 = p.Stock
               }).ToList<SimpItem<long, string, int>>()
               .GroupBy(p => p.Item1)
               .ToDictionary(key => key.Key, value => value.ToDictionary(k => k.Item2, v => v.Item3));

            foreach (var branch in stores)
            {
                if (!branchStocks.TryGetValue(branch, out var stocks))
                    continue;//无库存信息
                if (stocks.Count < items.Count)
                    continue; //商品数不足

                if (items.All(item => item.Value <= stocks[item.Key]))
                    return branch;//所有商品满足库存
            }
            return 0;
        }

        public List<long> GetStores(long shopId, long city, decimal longitude, decimal latitude, Dictionary<string, int> items)
        {
            var stores = GetNearbyStores(shopId, city, longitude, latitude).Keys.ToList();
            //查询门店库存
            var branchStocks = DbFactory.Default.Get<ShopBranchSkuInfo>()
               .Where(p => p.ShopBranchId.ExIn(stores) && p.SkuId.ExIn(items.Keys) && p.Status == ShopBranchSkuStatus.Normal)
               .Select(p => new
               {
                   Item1 = p.ShopBranchId,
                   Item2 = p.SkuId,
                   Item3 = p.Stock
               }).ToList<SimpItem<long, string, int>>()
               .GroupBy(p => p.Item1)
               .ToDictionary(key => key.Key, value => value.ToDictionary(k => k.Item2, v => v.Item3));
            var result = new List<long>();
            foreach (var store in stores)
            {
                if (!branchStocks.TryGetValue(store, out var stocks))
                    continue;//无库存信息
                if (stocks.Count < items.Count)
                    continue; //商品数不足

                if (items.All(item => item.Value <= stocks[item.Key]))
                    result.Add(store);
            }
            return result;
        }


        #region 组合Sql

        private void GetWhereForSearch(ShopBranchQuery query, Sql where, decimal lat = 0, decimal lng = 0)
        {
            //StringBuilder where = new StringBuilder();
            where.Append(" where ShopStatus = 7 AND Longitude>0 and Latitude>0 ");//周边门店只取定位了经纬度的数据
            if (lat > 0 && lng > 0)
                where.Append(string.Format(" AND (IsAboveSelf=1 or(IsStoreDelive=1 AND truncate(6378.137*2*ASIN(SQRT(POW(SIN(({0}*PI()/180.0-Latitude*PI()/180.0)/2),2)+COS({0}*PI()/180.0)*COS(Latitude*PI()/180.0)*POW(SIN(({1}*PI()/180.0-Longitude*PI()/180.0)/2),2))),2)<=ServeRadius))", lat, lng));
            if (query.FilterVirtualProduct.HasValue && query.FilterVirtualProduct.Value)
            {
                where.Append(" and p.ProductType=0");
            }

            if (query.ShopId > 0)//取商家下的门店数据
            {
                where.Append(" and sb.ShopId=@0 ", query.ShopId);
                //parms.Add("@ShopId", query.ShopId);
            }
            if (query.CityId > 0)//同城门店
            {
                where.Append(" and AddressPath like concat('%',@0,'%') ", CommonConst.ADDRESS_PATH_SPLIT + query.CityId + CommonConst.ADDRESS_PATH_SPLIT);
                //parms.Add("@AddressPath", CommonConst.ADDRESS_PATH_SPLIT + query.CityId + CommonConst.ADDRESS_PATH_SPLIT);
            }
            if (query.Status.HasValue)
            {
                where.Append(" and sb.Status=@0 ", query.Status.Value);
                //parms.Add("@Status", query.Status.Value);
            }
            if (!string.IsNullOrEmpty(query.ShopBranchName))
            {
                where.Append(" and (ShopBranchName LIKE concat('%',@0,'%') OR (ProductName LIKE concat('%',@0,'%') AND sbs.`Status`= @1 )) ", query.ShopBranchName, ShopBranchSkuStatus.Normal.GetHashCode());
                //where.Append("  AND sbs.`Status`= " + ShopBranchSkuStatus.Normal.GetHashCode());
                //where.Append(" )) ");
                //parms.Add("@KeyWords", query.ShopBranchName);
            }
            if (query.ShopBranchTagId.HasValue && query.ShopBranchTagId.Value > 0)
            {
                where.Append(" and tag.ShopBranchTagId = @0 ", query.ShopBranchTagId.Value);
                //parms.Add("@ShopBranchTagId", query.ShopBranchTagId.Value);
            }
            if (null != query.ProductIds && query.ProductIds.Length > 0)
            {
                where.Append(" and ProductId in (@0) ", query.ProductIds);
                //parms.Add("@ProductIds", string.Join(",", query.ProductIds));
                if (query.ShopBranchProductStatus.HasValue)
                {
                    where.Append(" and sbs.Status = @0 ", query.ShopBranchProductStatus.Value.GetHashCode());
                    //parms.Add("@ShopBranchProductStatus", (int)query.ShopBranchProductStatus.Value);
                }
            }

            where.Append(" GROUP BY sb.Id ");

            //return where.ToString();
        }

        /// <summary>
        /// 获取搜索排序sql
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private void GetSearchOrder(ShopBranchQuery query, Sql order)
        {
            //在服务范围内的优先排在前面（上门自提默认在服务范围内）
            //(CASE WHEN ServeRadius>Distance OR IsAboveSelf=1 THEN 1 ELSE 0 END) DESC
            //排序优先顺序：服务范围内、推荐及推荐顺序、距离、门店ID
            order.Append(" ORDER BY (CASE WHEN ServeRadius>Distance OR IsAboveSelf=1 THEN 1 ELSE 0 END) DESC,IsRecommend DESC, RecommendSequence ASC ");
            switch (query.OrderKey)
            {
                case 2:
                    order.Append(", Distance ");
                    break;

                default:
                    order.Append(", sb.Id ");
                    break;
            }
            if (!query.OrderType)
                order.Append(" DESC ");
            else
                order.Append(" ASC ");

            //return order.ToString();
        }
        /// <summary>
        /// 获取搜索排序sql
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        private void GetStoreByProductOrder(ShopBranchQuery query, Sql order)
        {
            order.Append(" ORDER BY  ");
            switch (query.OrderKey)
            {
                case 2:
                    order.Append(" Distance ");
                    break;

                default:
                    order.Append(" sb.Id ");
                    break;
            }
            if (!query.OrderType)
                order.Append(" DESC ");
            else
                order.Append(" ASC ");

            //return order.ToString();
        }

        private string GetSearchPage(ShopBranchQuery query)
        {
            return string.Format(" LIMIT {0},{1} ", (query.PageNo - 1) * query.PageSize, query.PageSize);
        }
        #endregion

        /// <summary>
        /// 获取该商品所有门店规格
        /// </summary>
        /// <param name="productId">商品Id</param>
        /// <returns></returns>
        public List<ShopBranchSkuInfo> GetShopBranchSkusByProductId(long productId)
        {
            return DbFactory.Default.Get<ShopBranchSkuInfo>().Where(d => d.ProductId == productId).ToList();
        }

        /// <summary>
        /// 清除该商品在门店中不存在的规格(主表sku规格不存在，子表himall_ShopBranchSku同时清理)
        /// </summary>
        /// <param name="productId">商品Id</param>
        /// <returns></returns>
        public bool SetClearNotExistBranchSku(long productId)
        {
            var skuids = DbFactory.Default.Get<SKUInfo>().Where(d => d.ProductId == productId).Select(t => t.Id);
            return DbFactory.Default.Del<ShopBranchSkuInfo>().Where(d => d.ProductId == productId && d.SkuId.ExNotIn(skuids)).Succeed();
        }


        #region 门店标签
        public bool ExistTag(string title, long excludeId)
        {
            return DbFactory.Default.Get<ShopBranchTagInfo>()
                .Where(p => p.Title == title && p.Id != excludeId)
                .Exist();
        }
        /// <summary>
        /// 获取所有标签
        /// </summary>
        /// <returns></returns>
        public List<ShopBranchTagInfo> GetAllShopBranchTagInfo()
        {
            return DbFactory.Default.Get<ShopBranchTagInfo>().ToList();
        }

        public List<ShopBranchInTag> GetShopBranchInTagByBranchs(List<long> branchs)
        {
            return DbFactory.Default.Get<ShopBranchInTagInfo>()
                .LeftJoin<ShopBranchTagInfo>((bt, t) => bt.ShopBranchTagId == t.Id)
                .Where(p => p.ShopBranchId.ExIn(branchs))
                .Select(p => new { p.Id, p.ShopBranchId })
                .Select<ShopBranchTagInfo>(p => new { TagId = p.Id, p.Title })
                .ToList<ShopBranchInTag>();
        }
        public Dictionary<long, int> GetShopBranchInTagCount(List<long> ids)
        {
            return DbFactory.Default.Get<ShopBranchInTagInfo>()
                .Where(p => p.ShopBranchTagId.ExIn(ids))
                .GroupBy(p => p.ShopBranchTagId)
                .Select(p => new { Item1 = p.ShopBranchTagId, Item2 = p.ExCount(false) })
                .ToList<dynamic>()
                .ToDictionary(k => (long)k.Item1, v => (int)v.Item2);
        }
        public ShopBranchTagInfo GetShopBranchTagInfo(long id)
        {
            return DbFactory.Default.Get<ShopBranchTagInfo>().Where(p => p.Id == id).FirstOrDefault();
        }

        public int GetShopBranchInTagCount(long id)
        {
            return DbFactory.Default.Get<ShopBranchInTagInfo>().Where(p => p.ShopBranchTagId == id).Count();
        }

        /// <summary>
        /// 新增标签
        /// </summary>
        /// <param name="model"></param>
        public void AddShopBranchTagInfo(string title)
        {
            if (ExistTag(title, 0))
                throw new MessageException("标签名称不可重复");
            DbFactory.Default.Add(new ShopBranchTagInfo { Title = title });
        }
        /// <summary>
        /// 修改标签
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public void UpdateShopBranchTagInfo(long id, string title)
        {
            if (ExistTag(title, id))
                throw new MessageException("标签名称不可重复");

            DbFactory.Default.Set<ShopBranchTagInfo>()
                .Where(p => p.Id == id)
                .Set(p => p.Title, title)
                .Succeed();
        }
        /// <summary>
        /// 删除标签
        /// </summary>
        /// <param name="ShopBranchTagInfoId"></param>
        /// <returns></returns>
        public void DeleteShopBranchTagInfo(long id)
        {
            DbFactory.Default.InTransaction(() =>
            {
                //删除关联表
                DbFactory.Default.Del<ShopBranchInTagInfo>(p => p.ShopBranchTagId == id);
                //删除主表
                DbFactory.Default.Del<ShopBranchTagInfo>(p => p.Id == id);
            });
        }

        public void SetShopBranchTags(List<long> branchs, List<long> tags)
        {
            //删除原Tag
            DbFactory.Default.Del<ShopBranchInTagInfo>().Where(p => p.ShopBranchId.ExIn(branchs)).Succeed();
            var list = new List<ShopBranchInTagInfo>();
            foreach (long branch in branchs)
                foreach (long tag in tags)
                    list.Add(new ShopBranchInTagInfo
                    {
                        ShopBranchId = branch,
                        ShopBranchTagId = tag
                    });
            DbFactory.Default.Add<ShopBranchInTagInfo>(list);
        }

        /// <summary>
        /// 门店标签分页
        /// </summary>
        /// <param name="pageNo"></param>
        /// <param name="pageSize"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        public QueryPageModel<ShopBranchTagInfo> GetShopBranchTags(int pageNo = 1, int pageSize = 10, string title = "")
        {
            pageNo = pageNo > 0 ? pageNo : 1;
            pageSize = pageSize > 0 ? pageSize : 10;
            var shopBranchTags = DbFactory.Default.Get<ShopBranchTagInfo>();
            if (!string.IsNullOrWhiteSpace(title))
            {
                shopBranchTags = shopBranchTags.Where(e => e.Title.Contains(title));
            }

            var models = shopBranchTags.OrderBy(e => e.Id).ToPagedList(pageNo, pageSize);
            return new QueryPageModel<ShopBranchTagInfo>
            {
                Models = models,
                Total = models.TotalRecordCount
            };
        }

        #endregion

        /// <summary>
        /// 获取评分
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ShopStoreServiceMark GetServiceMark(long id)
        {
            var result = DbFactory.Default.Query<ShopStoreServiceMark>("select ifnull(avg(PackMark),5) PackMark,ifnull(avg(DeliveryMark),5) DeliveryMark,ifnull(AVG(ServiceMark),5) ServiceMark from himall_ordercomment oc where exists (select Id from himall_order o where ShopBranchId=" + id.ToString() + " and oc.OrderId=o.Id)").FirstOrDefault();
            if (result == null)
                result = new ShopStoreServiceMark
                {
                    PackMark = 5,
                    DeliveryMark = 5,
                    ServiceMark = 5
                };
            return result;
        }

        /// <summary>
        /// 获取允许管理的门店
        /// </summary>
        /// <param name="shop"></param>
        /// <returns></returns>
        public List<ShopBranchInfo> GetManagerShops(long shop)
        {
            return DbFactory.Default.Get<ShopBranchInfo>()
                .Where(p => p.ShopId == shop && p.Status == ShopBranchStatus.Normal && p.EnableSellerManager == true)
                .ToList();
        }
        /// <summary>
        /// 设置管理门店权限
        /// </summary>
        /// <param name="branch"></param>
        /// <param name="enable"></param>
        public void SetManagerShop(long branch, bool enable)
        {
            DbFactory.Default.Set<ShopBranchInfo>()
                .Where(p => p.Id == branch)
                .Set(p => p.EnableSellerManager, enable).Succeed();
        }

        /// <summary>
        /// 门店商品列表
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<ShopBranchProduct> GetShopBranchProductList(ShopBranchProductQuery query)
        {
            var db = DbFactory.Default.Get<ShopBranchSkuInfo>();

            #region WhereBuilder
            if (query.ShopId.HasValue)
                db.Where(t => t.ShopId == query.ShopId.Value);

            if (query.ProductId.HasValue)
                db.Where(t => t.ProductId == query.ProductId.Value);

            if (!string.IsNullOrEmpty(query.ShopName))
            {
                var shopids = DbFactory.Default.Get<ShopInfo>().Where(p => p.ShopName.Contains(query.ShopName)).Select(p => p.Id);
                db.Where(t => t.ShopId.ExIn(shopids));
            }

            if (query.ShopCategoryId.HasValue)
            {
                var productids = DbFactory.Default.Get<ProductShopCategoryInfo>().Where(p => p.ShopCategoryId == query.ShopCategoryId).Select(p => p.ProductId);
                db.Where(t => t.ProductId.ExIn(productids));
            }

            if (query.ShopBranchId.HasValue)
                db.Where(t => t.ShopBranchId == query.ShopBranchId.Value);

            if (query.ShopBranchProductStatus.HasValue)
                db.Where(t => t.Status == query.ShopBranchProductStatus.Value);

            if (query.CategoryId.HasValue || query.SaleStatus.HasValue
                || !string.IsNullOrEmpty(query.ProductCode) || !string.IsNullOrEmpty(query.KeyWords))
            {
                var prodb = DbFactory.Default.Get<ProductInfo>();
                if (query.CategoryId.HasValue)
                    prodb.Where(p => p.CategoryId == query.CategoryId.Value);

                if (query.SaleStatus.HasValue)
                    prodb.Where(p => p.SaleStatus == query.SaleStatus.Value);

                if (!string.IsNullOrEmpty(query.ProductCode))
                    prodb.Where(p => p.ProductCode == query.ProductCode);

                if (!string.IsNullOrEmpty(query.KeyWords))
                    prodb.Where(p => p.ProductName.Contains(query.KeyWords));

                db.Where(t => t.ProductId.ExIn(prodb.Select(p => p.Id)));
            }
            #endregion

            db.OrderByDescending(p => p.CreateDate).OrderByDescending(d => d.ProductId)
                .GroupBy(t => t.ProductId).Select(d => new
                {
                    Id = d.ProductId,
                    ProductId = d.ProductId,
                    ShopId = d.ShopId,
                    ShopBranchId = d.ShopBranchId,
                    Status = d.Status,
                    CreateDate = d.CreateDate,
                    Stock = d.Stock.ExSum()
                });

            var data1 = db.ToPagedList<ShopBranchProduct>(query.PageNo, query.PageSize);
            return new QueryPageModel<ShopBranchProduct>()
            {
                Total = data1.TotalRecordCount,
                Models = data1
            };
        }

        /// <summary>
        /// 根据商品ID获取列表
        /// </summary>
        /// <param name="shopBranchId"></param>
        /// <param name="products"></param>
        /// <returns></returns>
        public bool CheckOnSale(long shopBranchId, List<string> skus)
        {
            return skus.Count == DbFactory.Default.Get<ShopBranchSkuInfo>().Where(e => e.ShopBranchId == shopBranchId && e.SkuId.ExIn(skus) && e.Status == ShopBranchSkuStatus.Normal).Count();
        }
    }
}
