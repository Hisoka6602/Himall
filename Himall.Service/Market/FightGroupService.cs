using Himall.CommonModel;
using Himall.CommonModel.WeiXin;
using Himall.Core;
using Himall.Core.Plugins.Message;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.FightGroup;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Himall.Service
{
    public class FightGroupService : ServiceBase
    {
        #region 拼团活动
        /// <summary>
        /// 新增拼团活动
        /// </summary>
        /// <param name="data"></param>
        public void AddActive(FightGroupActiveInfo data)
        {
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Add(data);

                //回填Id
                foreach (var item in data.ActiveItems)
                {
                    item.ActiveId = data.Id;
                    item.ProductId = data.ProductId;
                    DbFactory.Default.Add(item);
                }
            });
            CacheManager.ClearAvailableFightGroup(data.Id);
        }

        /// <summary>
        /// 商品是否可以参加拼团活动
        /// <para>其他活动限时请在bll层操作</para>
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public bool ProductCanJoinActive(long productId)
        {
            bool result = true;
            var edate = DateTime.Now;
            if (DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.ProductId == productId && d.EndTime > edate).Exist())
            {
                result = false;
            }
            return result;
        }
        /// <summary>
        /// 更新拼团活动
        /// </summary>
        /// <param name="data"></param>
        public void UpdateActive(FightGroupActiveInfo data)
        {
            if (data == null)
            {
                throw new HimallException("错误的拼团活动");
            }

            var model = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.Id == data.Id).FirstOrDefault();
            if (model == null)
            {
                throw new HimallException("错误的拼团活动");
            }

            DbFactory.Default.InTransaction(() =>
            {
                model.IconUrl = data.IconUrl;
                model.EndTime = data.EndTime;
                model.LimitedNumber = data.LimitedNumber;
                model.LimitedHour = data.LimitedHour;
                model.LimitQuantity = data.LimitQuantity;
                if (model.ActiveStatus == FightGroupActiveStatus.WillStart)
                {
                    model.MiniGroupPrice = data.MiniGroupPrice;
                }
                DbFactory.Default.Update(model);
                if (model.ActiveStatus == FightGroupActiveStatus.WillStart)
                {
                    foreach (var item in data.ActiveItems)
                    {
                        DbFactory.Default.Set<FightGroupActiveItemInfo>().Set(p => p.ActivePrice, item.ActivePrice).Set(p => p.ActiveStock, item.ActiveStock).Where(p => p.Id == item.Id).Succeed();
                    }
                }
                #region 更新活动项
                /*
                DbFactory.Default.Del<FightGroupActiveItemInfo>().Where(d => d.ActiveId == data.Id).Succeed();
                foreach (var item in data.ActiveItems)
                {
                    item.ActiveId = model.Id;
                    item.ProductId = model.ProductId;
                    DbFactory.Default.Add(item);
                }
                */
                #endregion
            });
            CacheManager.ClearAvailableFightGroup(data.Id);
        }


        /// <summary>
        /// 拼团活动实效
        /// </summary>
        /// <param name="activityId">活动Id</param>
        /// <returns></returns>
        public void DisableActivity(long activityId)
        {
            var endTime = DateTime.Now.AddMinutes(-1);
            DbFactory.Default.Set<FightGroupActiveInfo>().Set(t => t.EndTime, endTime)
                .Set(t => t.ActiveTimeStatus, -1)
                .Where(t => t.Id == activityId).Succeed();
            CacheManager.ClearAvailableFightGroup(activityId);
            Cache.Remove(CacheKeyCollection.CACHE_FIGHTGROUP); //活动清缓存
        }

        /// <summary>
        /// 修改活动库存
        /// </summary>
        /// <param name="actionId"></param>
        /// <param name="skuId"></param>
        /// <param name="stockChange">库存量 正数补充 负数消耗</param>
        public void UpdateActiveStock(long actionId, string skuId, long stockChange)
        {
            if (string.IsNullOrWhiteSpace(skuId))
            {
                throw new HimallException("错误的规格编号");
            }
            var actitemobj = DbFactory.Default.Get<FightGroupActiveItemInfo>().Where(d => d.ActiveId == actionId && d.SkuId == skuId).FirstOrDefault();
            if (actitemobj == null)
            {
                return;
                //throw new HimallException("错误的规格信息");
            }
            var skuinfo = DbFactory.Default.Get<SKUInfo>().Where(d => d.Id == skuId).Exist();
            if (!skuinfo)
            {
                throw new HimallException("错误的规格信息");
            }
            //actitemobj.ActiveStock = actitemobj.ActiveStock < skuinfo.Stock ? actitemobj.ActiveStock : skuinfo.Stock;  //库存无需修正
            if (actitemobj.ActiveStock + stockChange < 0)
            {
                throw new HimallException("库存不足");
            }
            actitemobj.ActiveStock += (int)stockChange;
            var buynum = stockChange;
            actitemobj.BuyCount -= (int)buynum;
            if (actitemobj.BuyCount < 0)
            {
                actitemobj.BuyCount = 0;   //零值修正
            }
            DbFactory.Default.Update(actitemobj);
        }

        /// <summary>
        /// 下架拼团活动
        /// </summary>
        /// <param name="id"></param>
        /// <param name="manageRemark">下架原因</param>
        /// <param name="manageId">管理员编号</param>
        public void CancelActive(long id, string manageRemark, long manageId)
        {
            var data = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.Id == id).FirstOrDefault();
            if (data == null)
            {
                throw new HimallException("错误的活动编号");
            }
            if (data.ActiveStatus == FightGroupActiveStatus.Ending)
            {
                throw new HimallException("活动已结束");
            }
            //直接改为过期
            data.EndTime = DateTime.Now.AddDays(-1);
            data.ManageAuditStatus = FightGroupManageAuditStatus.SoldOut.GetHashCode();
            data.ManageRemark = manageRemark;
            data.ManageDate = DateTime.Now;
            data.ManagerId = manageId;
            DbFactory.Default.Update(data);
            CacheManager.ClearAvailableFightGroup(id);
        }



        /// <summary>
        /// 根据商品ID取活动信息
        /// </summary>
        /// <param name="proId"></param>
        /// <returns></returns>
        public FightGroupActiveInfo GetActiveByProId(long proId)
        {
            var result = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.ProductId == proId && d.EndTime > DateTime.Now).FirstOrDefault();
            return result;
        }

        public FightGroupActiveInfo GetFightGroupActiveInfo(long id)
        {
            return DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.Id == id).FirstOrDefault();
        }
        public List<FightGroupInfo> GetGroupTimeOut() =>
            DbFactory.Default.Query<FightGroupInfo>($"select * from himall_FightGroup where TIMESTAMPDIFF(MINUTE,AddGroupTime,NOW())>(LimitedHour*60) and GroupStatus={(int)FightGroupBuildStatus.Ongoing}").ToList();

        /// <summary>
        /// 获取拼团活动
        /// </summary>
        /// <param name="id"></param>
        /// <param name="needGetProductCommentNumber">是否需要同步获取商品的评价数量,会自动加载产品信息</param>
        /// <param name="isLoadItems">是否加载节点信息</param>
        /// <param name="isLoadPorductInfo">是否加载产品信息</param>
        /// <returns></returns>
        public FightGroupActiveInfo GetActive(long id, bool needGetProductCommentNumber = false, bool isLoadItems = true, bool isLoadPorductInfo = true)
        {
            var result = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.Id == id).FirstOrDefault();
            if (result == null)
            {
                throw new HimallException("错误的活动编号");
            }
            result.HasStock = (DbFactory.Default.Get<FightGroupActiveItemInfo>().Where(d => d.ActiveId == id).Sum<long>(d => d.ActiveStock) > 0);
            if (needGetProductCommentNumber)
            {
                isLoadPorductInfo = true;
            }
            if (isLoadPorductInfo)
            {
                var pro = DbFactory.Default.Get<Entities.ProductInfo>().Where(d => d.Id == result.ProductId).FirstOrDefault();
                if (pro != null)
                {
                    result.VideoPath = pro.VideoPath;

                    result.ProductImgPath = pro.RelativePath;
                    result.FreightTemplateId = pro.FreightTemplateId;
                    result.ProductShortDescription = pro.ShortDescription;
                    result.ProductCode = pro.ProductCode;
                    result.MeasureUnit = pro.MeasureUnit;
                    if (pro.AuditStatus == Entities.ProductInfo.ProductAuditStatus.Audited
                        && pro.SaleStatus == Entities.ProductInfo.ProductSaleStatus.OnSale)
                    {
                        result.CanBuy = true;
                    }
                    else
                    {
                        result.CanBuy = false;
                    }
                    if (needGetProductCommentNumber)
                    {
                        result.ProductCommentNumber = DbFactory.Default.Get<ProductCommentInfo>().Where(p => p.ProductId == pro.Id && p.IsHidden == false).Count();
                    }
                }
            }
            if (isLoadItems)
            {
                result.ActiveItems = GetActiveItems(id);
            }
            return result;
        }
        public List<Entities.FightGroupActiveInfo> GetActive(long[] ids)
        {
            var result = DbFactory.Default.Get<Entities.FightGroupActiveInfo>().Where(d => d.Id.ExIn(ids)).ToList();
            var prolist = DbFactory.Default.Get<ProductInfo>().Where(p => p.Id.ExIn(result.Select(r => r.ProductId))).ToList();
            foreach (var item in result)
            {
                item.ActiveItems = GetActiveItems(item.Id);
                var currenpro = prolist.Where(p => p.Id == item.ProductId).FirstOrDefault();
                if (currenpro != null)
                {
                    item.ProductImgPath = currenpro.ImagePath;
                }
            }
            return result;
        }
        /// <summary>
        /// 使用商品编号获取正在进行的拼团活动编号
        /// <para>0表示无数据</para>
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        public long GetActiveIdByProductId(long productId)
        {
            long result = 0;
            DateTime curtime = DateTime.Now;
            var sql = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.ProductId == productId && d.StartTime <= curtime && d.EndTime >= curtime);
            var actobj = sql.FirstOrDefault();
            if (actobj != null)
            {
                result = actobj.Id;
            }
            return result;
        }
        public Dictionary<string, int> GetActiveStock(long id) =>
            DbFactory.Default.Get<FightGroupActiveItemInfo>(p => p.ActiveId == id).Select(p => new
            {
                Item1 = p.SkuId,
                Item2 = p.ActiveStock
            }).ToList<SimpItem<string, int>>()
            .ToDictionary(p => p.Item1, v => v.Item2);

        /// <summary>
        /// 未结束活动数据
        /// </summary>
        public IEnumerable<FightGroupData> GetNotEnd() =>
            CacheManager.GetAvailableFightGroup(() =>
            {
                var data = DbFactory.Default.Get<FightGroupActiveInfo>(p => p.EndTime > DateTime.Now).ToList<FightGroupData>();
                var list = data.Select(p => p.Id).ToList();
                var products = DbFactory.Default.Get<FightGroupActiveItemInfo>(p => p.ActiveId.ExIn(list)).ToList();
                foreach (var item in data)
                {
                    item.Items = products.Where(p => p.ActiveId == item.Id).Select(i => new FightGroupItemData
                    {
                        ProductId = i.ProductId,
                        SkuId = i.SkuId,
                        ActivePrice = i.ActivePrice,
                    }).ToList();
                }
                return data;
            }).Where(p => p.EndTime > DateTime.Now).ToList();

        /// <summary>
        /// 活动中
        /// </summary>
        public FightGroupData GetGoing(long id) =>
            GetNotEnd().FirstOrDefault(p => p.Id == id && p.StartTime < DateTime.Now);

        public IEnumerable<FightGroupData> GetGoingByProduct(List<long> products) =>
            GetNotEnd().Where(p => products.Contains(p.ProductId) && p.StartTime < DateTime.Now);


        /// <summary>
        /// 正在拼团的订单
        /// </summary>
        /// <param name="activeId"></param>
        /// <returns></returns>
        public List<FightGroupInfo> GetGroupOnGoings(long activeId)
        {
            return DbFactory.Default.Get<FightGroupInfo>(p => p.ActiveId == activeId && p.GroupStatus == (int)FightGroupBuildStatus.Ongoing).ToList();
        }

        public List<FightGroupsListModel> GetGroups(long activeId, int top)
        {
            return DbFactory.Default.Get<FightGroupInfo>(p => p.ActiveId == activeId && p.GroupStatus == (int)FightGroupBuildStatus.Ongoing)
                .LeftJoin<MemberInfo>((f, m) => f.HeadUserId == m.Id)
                .Select()
                .Select<MemberInfo>(p => new
                {
                    HeadUserName = p.Nick,
                    HeadUserIcon = p.Photo,
                })
                .Take(top)
                .ToList<FightGroupsListModel>();
        }


        public FightGroupData GetFightGroupData(long id) =>
            CacheManager.GetFightGroup(id, () =>
            {
                var data = DbFactory.Default.Get<FightGroupActiveInfo>(p => p.Id == id).FirstOrDefault<FightGroupData>();
                data.Items = DbFactory.Default.Get<FightGroupActiveItemInfo>(p => p.ActiveId == id).ToList<FightGroupItemData>();
                return data;
            });

        public FightGroupActiveInfo GetActiveIdByProductIdAndShopId(long productId, long shopId)
        {
            DateTime curtime = DateTime.Now;
            var info = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.ProductId == productId && d.ShopId == shopId && d.StartTime <= curtime && d.EndTime >= curtime).FirstOrDefault();
            if (info != null)
            {
                info.ActiveItems = DbFactory.Default.Get<Entities.FightGroupActiveItemInfo>().Where(d => d.ActiveId == info.Id).ToList();
            }
            return info;
        }

        public List<long> GetActiveIdByProductIds(long[] productIds)
        {
            var result = new List<long>();
            DateTime curtime = DateTime.Now;
            var sql = DbFactory.Default.Get<FightGroupActiveInfo>().Where(d => d.ProductId.ExIn(productIds) && d.StartTime <= curtime && d.EndTime >= curtime);
            var actobj = sql.ToList();
            if (actobj != null)
            {
                result = actobj.Select(p => p.Id).ToList();
            }
            return result;
        }
        /// <summary>
        /// 获取拼团活动项
        /// </summary>
        /// <param name="activeId"></param>
        /// <returns></returns>
        public List<Entities.FightGroupActiveItemInfo> GetActiveItems(long activeId)
        {
            var datalist = DbFactory.Default.Get<FightGroupActiveItemInfo>().Where(d => d.ActiveId == activeId).ToList();
            List<FightGroupActiveItemInfo> result = new List<FightGroupActiveItemInfo>();
            if (datalist.Count > 0)
            {
                //补充信息
                var skuids = datalist.Select(d => d.SkuId).ToList();
                var skulist = DbFactory.Default.Get<SKUInfo>().Where(d => d.Id.ExIn(skuids)).ToList();
                foreach (var item in datalist)
                {
                    var cursku = skulist.FirstOrDefault(d => d.Id == item.SkuId);
                    if (cursku != null)  //只使用有效的sku
                    {
                        item.Color = cursku.Color;
                        item.Size = cursku.Size;
                        item.Version = cursku.Version;
                        item.SkuName = cursku.Color + " " + cursku.Size + " " + cursku.Version;
                        item.ProductCostPrice = cursku.CostPrice;
                        item.ProductPrice = cursku.SalePrice;
                        item.ProductStock = cursku.Stock;
                        item.ShowPic = cursku.ShowPic;
                        if (item.ActiveStock > cursku.Stock)
                        {
                            item.ActiveStock = (int)cursku.Stock;
                        }
                        result.Add(item);
                    }
                }
            }
            return result;
        }

        public List<FightGroupActiveItemInfo> GetActiveItemsSimp(long activeId) =>
            DbFactory.Default.Get<FightGroupActiveItemInfo>(p => p.ActiveId == activeId)
                .LeftJoin<SKUInfo>((i, s) => i.SkuId == s.Id)
            .Select()
            .Select<SKUInfo>(p => new
            {
                p.Color,
                p.Size,
                p.Version
            })
            .ToList();

        public List<FightGroupActiveItemInfo> GetActiveItemsSimp(List<long> actives) =>
            DbFactory.Default.Get<FightGroupActiveItemInfo>(p => p.ActiveId.ExIn(actives)).ToList();

        /// <summary>
        /// 获取活动信息集
        /// </summary>
        /// <param name="Statuses"></param>
        /// <param name="StartTime"></param>
        /// <param name="EndTime"></param>
        /// <param name="ProductName">商品名</param>
        /// <param name="ShopName">店铺名</param>
        /// <param name="ShopId">店铺编号</param>
        /// <param name="PageNo"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>

        public QueryPageModel<FightGroupActiveInfo> GetActives(FightGroupActiveQuery query)
        {
            AutoUpdateActiveTimeStatus();

            var db = DbFactory.Default.Get<FightGroupActiveInfo>();
            if (query.StartTime.HasValue)
            {
                db.Where(d => d.StartTime >= query.StartTime.Value.Date);
            }
            if (query.EndTime.HasValue)
            {
                db.Where(d => d.EndTime <= query.EndTime.Value);
            }
            if (query.ShopId.HasValue && query.ShopId.Value > 0)
            {
                db.Where(d => d.ShopId == query.ShopId.Value);
            }
            if (!string.IsNullOrWhiteSpace(query.ProductName))
            {
                db.Where(d => d.ProductName.Contains(query.ProductName));
            }
            if (!string.IsNullOrWhiteSpace(query.ShopName))
            {
                var shops = DbFactory.Default.Get<ShopInfo>().Where(d => d.ShopName.Contains(query.ShopName)).Select(t => t.Id);
                db.Where(d => d.ShopId.ExIn(shops));
            }

            if (query.ActiveStatus.HasValue)
            {
                if (query.ActiveStatusList == null)
                {
                    query.ActiveStatusList = new List<FightGroupActiveStatus>();
                }
                query.ActiveStatusList.Add(query.ActiveStatus.Value);
            }

            if (query.ActiveStatusList != null)
            {
                if (query.ActiveStatusList.Count > 0)
                {
                    var _subwhere = Core.PredicateExtensions.False<FightGroupActiveInfo>();
                    var curtime = DateTime.Now;
                    foreach (var item in query.ActiveStatusList)
                    {
                        switch (item)
                        {
                            case FightGroupActiveStatus.Ending:
                                _subwhere = _subwhere.Or(d => d.EndTime < curtime);
                                break;
                            case FightGroupActiveStatus.Ongoing:
                                _subwhere = _subwhere.Or(d => d.StartTime <= curtime && d.EndTime >= curtime);
                                break;
                            case FightGroupActiveStatus.WillStart:
                                _subwhere = _subwhere.Or(d => d.StartTime > curtime && d.EndTime > d.StartTime);
                                break;
                        }
                    }
                    db.Where(_subwhere);
                }
            }

            if (query.SaleStatus.HasValue || query.Categories != null)
            {
                var pros = DbFactory.Default.Get<ProductInfo>();
                if (query.Categories != null)
                    pros.Where(item => item.CategoryId.ExIn(query.Categories));

                if (query.SaleStatus.HasValue)
                    pros.Where(d => d.SaleStatus == query.SaleStatus);
                var pIds = pros.Select(d => d.Id);
                db.Where(d => d.ProductId.ExIn(pIds));
            }

            QueryPageModel<FightGroupActiveInfo> result = new QueryPageModel<FightGroupActiveInfo>();

            switch (query.Sort.ToLower())
            {
                case "starttime":
                    if (query.IsAsc) db.OrderBy(p => p.StartTime);
                    else db.OrderByDescending(p => p.StartTime);
                    break;
                case "endtime":
                    if (query.IsAsc) db.OrderBy(p => p.EndTime);
                    else db.OrderByDescending(p => p.EndTime);
                    break;
                case "groupcount":
                    if (query.IsAsc) db.OrderBy(p => p.GroupCount);
                    else db.OrderByDescending(p => p.GroupCount);
                    break;
                case "okgroupcount":
                    if (query.IsAsc) db.OrderBy(p => p.OkGroupCount);
                    else db.OrderByDescending(p => p.OkGroupCount);
                    break;
                default:
                    db.OrderByDescending(p => p.Id);
                    break;
            }

            var datalist = db.ToPagedList(query.PageNo, query.PageSize);
            //外键值补充
            if (datalist.Count > 0)
            {
                //商家信息
                var shopids = datalist.Select(d => d.ShopId);
                var shopnames = DbFactory.Default.Get<ShopInfo>().Where(d => d.Id.ExIn(shopids)).Select(d => new { Id = d.Id, ShopName = d.ShopName }).ToList<dynamic>();

                //最低销售价补充
                var proids = datalist.Select(d => d.ProductId);
                var prominprices = DbFactory.Default.Get<SKUInfo>().Where(d => d.ProductId.ExIn(proids)).GroupBy(d => d.ProductId).Select(d => new { proid = d.ProductId, price = d.SalePrice.ExMin() }).ToList<dynamic>();

                //最低火拼价补充
                var actids = datalist.Select(d => d.Id);
                var actminprices = DbFactory.Default.Get<FightGroupActiveItemInfo>().Where(d => d.ActiveId.ExIn(actids)).GroupBy(d => d.ActiveId).Select(d => new { actid = d.ActiveId, price = d.ActivePrice.ExMin() }).ToList<dynamic>();

                var actgroupsumstock = DbFactory.Default.Get<FightGroupActiveItemInfo>().Where(d => d.ActiveId.ExIn(actids)).GroupBy(d => d.ActiveId).Select(d => new { ActiveId = d.ActiveId, SumStock = d.ActiveStock.ExSum() }).ToList<dynamic>();

                foreach (var item in datalist)
                {
                    var propriceobj = prominprices.FirstOrDefault(d => d.proid == item.ProductId);
                    if (propriceobj == null)
                        continue;//商品这条记录不存在(数据库里它手动删除或导入没这条产品记录)

                    var snameobj = shopnames.FirstOrDefault(d => d.Id == item.ShopId);
                    item.ShopName = (snameobj == null ? "" : snameobj.ShopName);
                    item.MiniSalePrice = propriceobj.price;

                    var actpriceobj = actminprices.FirstOrDefault(d => d.actid == item.Id);
                    if (actpriceobj != null)
                    {
                        item.MiniGroupPrice = actpriceobj.price;
                    }
                    var actsumstock = actgroupsumstock.FirstOrDefault(d => d.ActiveId == item.Id);
                    if (actsumstock != null)
                    {
                        item.HasStock = actsumstock.SumStock > 0;
                    }
                }
            }

            result.Models = datalist;
            result.Total = datalist.TotalRecordCount;
            return result;
        }

        public long GetBuyCount(long grouponId, long memberId)
        {
            var status = new[] { FightGroupOrderJoinStatus.JoinSuccess, FightGroupOrderJoinStatus.BuildSuccess };
            return DbFactory.Default.Get<FightGroupOrderInfo>()
                .Where(p => p.ActiveId == grouponId && p.OrderUserId == memberId && p.JoinStatus.ExIn(status))
                .Sum<long?>(p => p.Quantity) ?? 0;
        }



        #endregion


        #region 拼团详情
        /// <summary>
        /// 获取拼团组(只单纯获得拼团组)
        /// </summary>
        /// <param name="activeId">活动编号</param>
        /// <param name="groupId">团编号</param>
        /// <returns></returns>
        public FightGroupInfo GetGroupInfo(long groupId)
        {
            return DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == groupId).FirstOrDefault();
        }
        /// <summary>
        /// 获取拼团
        /// </summary>
        /// <param name="activeId">活动编号</param>
        /// <param name="groupId">团编号</param>
        /// <returns></returns>
        public FightGroupInfo GetGroup(long activeId, long groupId)
        {
            var result = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.ActiveId == activeId && d.Id == groupId).FirstOrDefault();
            if (result == null)
            {
                throw new HimallException("错误的拼团信息");
            }
            if (result.AddGroupTime.AddHours((double)result.LimitedHour) < DateTime.Now && result.GroupStatus == (int)FightGroupBuildStatus.Ongoing)
            {
                var _fv = (int)FightGroupBuildStatus.Failed;
                result.GroupStatus = _fv;
                //超时团先给状态，关团操作在另一线程
                Task.Factory.StartNew(() => GroupFailed(result.Id));
            }

            //补充订单与用户信息
            FightGroupOrderJoinStatus jstate = FightGroupOrderJoinStatus.JoinSuccess; //取参团成功、参团成功但拼团失败、拼团成功
            List<FightGroupInfo> fglist = new List<FightGroupInfo>();
            fglist.Add(result);
            Dictionary<long, string> fightskuid = new Dictionary<long, string>();

            var grouporder = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.ActiveId == activeId && d.GroupId == groupId).FirstOrDefault();
            fightskuid.Add(groupId, grouporder.SkuId);
            GroupsInfoFill(fglist, true, jstate, fightskuid);
            result = fglist.FirstOrDefault();

            return result;
        }

        /// <summary>
        /// 成团中的团
        /// </summary>
        public bool ExistGrouping(long grouponId, long memberId)
        {
            var status = new[] { FightGroupBuildStatus.Ongoing, FightGroupBuildStatus.Opening };
            return DbFactory.Default.Get<FightGroupInfo>()
                .Where(p => p.ActiveId == grouponId && p.HeadUserId == memberId && p.GroupStatus.ExIn(status))
                .Exist();
        }

        public FightGroup GetGroup(long groupId)
        {
            var model = DbFactory.Default.Get<FightGroupInfo>(p => p.Id == groupId).FirstOrDefault<FightGroup>();
            model.Items = DbFactory.Default.Get<FightGroupOrderInfo>(p => p.GroupId == groupId).ToList<FightGroupOrder>();
            return model;
        }

        /// <summary>
        /// 参团
        /// </summary>
        public void JoinGroup()
        {

        }
        /// <summary>
        /// 开团
        /// </summary>
        public void CreateGroup()
        {

        }

        /// <summary>
        /// 获取拼团详情列表
        /// </summary>
        /// <param name="activeId">活动编号</param>
        /// <param name="Statuses">状态集</param>
        /// <param name="StartTime">开始时间</param>
        /// <param name="EndTime">结束时间</param>
        /// <param name="PageNo"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        public QueryPageModel<FightGroupInfo> GetGroups(
             long activeId,
             List<FightGroupBuildStatus> Statuses = null,
             DateTime? StartTime = null,
             DateTime? EndTime = null,
             int PageNo = 1,
             int PageSize = 10
             )
        {
            QueryPageModel<FightGroupInfo> result = new QueryPageModel<FightGroupInfo>();
            var datalist = new List<FightGroupInfo>();
            int opings = FightGroupBuildStatus.Opening.GetHashCode();
            var sql = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.ActiveId == activeId && d.GroupStatus != opings);
            if (Statuses != null)
            {
                if (Statuses.Count > 0)
                {
                    var _sorwhere = PredicateExtensions.False<FightGroupInfo>();
                    foreach (var item in Statuses)
                    {
                        int _v = (int)item;
                        var _swhere = PredicateExtensions.True<FightGroupInfo>();
                        _swhere = _swhere.And(d => d.GroupStatus == _v);

                        _sorwhere = _sorwhere.Or(_swhere);
                    }
                    sql.Where(_sorwhere);
                }
            }
            if (StartTime.HasValue)
            {
                sql.Where(d => d.AddGroupTime >= StartTime.Value);
            }
            if (EndTime.HasValue)
            {
                EndTime = EndTime.Value.AddDays(1).Date;
                sql.Where(d => d.AddGroupTime < EndTime.Value);
            }
            var rets = sql.OrderByDescending(o => o.ActiveId).ToPagedList(PageNo, PageSize);
            result.Total = rets.TotalRecordCount;
            datalist = rets;

            GroupsInfoFill(datalist, true);

            result.Models = datalist;
            return result;
        }
        public List<FightGroupInfo> GetCanJoinGroupsFirst(List<FightGroupBuildStatus> Statuses, int PageNo = 1, int PageSize = 5)
        {
            AutoCloseGroup();
            var datalist = new List<FightGroupInfo>();
            int opings = FightGroupBuildStatus.Opening.GetHashCode();
            var sql = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.GroupStatus != opings);
            if (Statuses != null)
            {
                if (Statuses.Count > 0)
                {
                    var _vs = Statuses.Select(d => (int)d).ToList();
                    sql.Where(d => d.GroupStatus.ExIn(_vs));
                }
            }
            var pagesql = sql.OrderByDescending(o => o.ActiveId).ToPagedList(PageNo, PageSize);

            datalist = pagesql;
            GroupsInfoFill(datalist, true);

            return datalist;
        }
        public List<FightGroupInfo> GetCanJoinGroupsSecond(long[] unActiveId, List<FightGroupBuildStatus> Statuses)
        {
            AutoCloseGroup();
            int opings = FightGroupBuildStatus.Opening.GetHashCode();
            var sql = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.GroupStatus != opings && d.ActiveId.ExNotIn(unActiveId));
            if (Statuses != null)
            {
                if (Statuses.Count > 0)
                {
                    var _sorwhere = PredicateExtensions.False<FightGroupInfo>();
                    foreach (var item in Statuses)
                    {
                        int _v = (int)item;
                        var _swhere = PredicateExtensions.True<FightGroupInfo>();
                        _swhere = _swhere.And(d => d.GroupStatus == _v);

                        _sorwhere = _sorwhere.Or(_swhere);
                    }
                    sql.Where(_sorwhere);
                }
            }
            var datalist = sql.ToList();
            GroupsInfoFill(datalist, true);
            return datalist;
        }
        /// <summary>
        /// 补充拼团附属信息
        /// </summary>
        /// <param name="datalist"></param>
        /// <param name="isLoadOrderData">是否装载订单信息</param>
        /// <param name="joinStatus">最低参团状态 (默认：参团成功、参团成功但拼团失败、拼团成功)</param>
        private void GroupsInfoFill(List<FightGroupInfo> datalist, bool isLoadOrderData = false, FightGroupOrderJoinStatus joinStatus = FightGroupOrderJoinStatus.JoinSuccess, Dictionary<long, string> fightorder = null)
        {
            if (datalist == null)
            {
                throw new HimallException("错误的数据");
            }
            //商品信息补充
            var proids = datalist.Select(d => d.ProductId);
            var products = DbFactory.Default.Get<ProductInfo>().Where(d => d.Id.ExIn(proids)).ToList();
            var proskus = new List<SKUInfo>();
            if (fightorder != null)
                proskus = DbFactory.Default.Get<SKUInfo>().Where(s => s.Id.ExIn(fightorder.Values)).ToList();//获取所有的规格信息

            //团长信息补充
            var huserids = datalist.Select(d => d.HeadUserId);
            var husers = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id.ExIn(huserids)).ToList();

            //拼装数据
            foreach (var item in datalist)
            {
                //商品信息
                var _pro = products.FirstOrDefault(d => d.Id == item.ProductId);
                if (_pro != null)
                {
                    item.ProductName = _pro.ProductName;
                    item.ProductImgPath = _pro.RelativePath;
                    item.ProductDefaultImage = HimallIO.GetProductSizeImage(item.ProductImgPath, 1, (int)ImageSize.Size_350);
                    if (proskus.Count > 0 && fightorder.Keys.Contains(item.Id))
                    {
                        fightorder.TryGetValue(item.Id, out string skuId);
                        var showpic = proskus.Where(sk => sk.Id == skuId).Select(s => s.ShowPic).FirstOrDefault();
                        if (!string.IsNullOrEmpty(showpic))
                        {
                            item.ProductDefaultImage = showpic;
                        }
                    }
                }
                //团长信息
                var _user = husers.FirstOrDefault(d => d.Id == item.HeadUserId);
                if (_user != null)
                {
                    item.HeadUserName = _user.ShowNick;
                    item.HeadUserIcon = _user.Photo;
                }
            }

            #region 补充拼团订单信息
            if (isLoadOrderData)
            {
                var gpids = datalist.Select(d => d.Id);
                int jstate = joinStatus.GetHashCode();
                var gpordlist = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.GroupId.ExIn(gpids)).Where(d => d.JoinStatus >= jstate).ToList();
                var userids = gpordlist.Select(d => d.OrderUserId);
                var userinfos = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id.ExIn(userids)).ToList();
                foreach (var item in datalist)
                {
                    var curgpordlist = gpordlist.Where(d => d.GroupId == item.Id).ToList();
                    if (curgpordlist.Count > 0)
                    {
                        var _tmplist = new List<FightGroupOrderInfo>();
                        foreach (var subitem in curgpordlist)
                        {
                            var curuser = userinfos.FirstOrDefault(d => d.Id == subitem.OrderUserId);
                            if (curuser != null)
                            {
                                subitem.RealName = curuser.RealName;
                                subitem.UserName = curuser.ShowNick;
                                subitem.Photo = curuser.Photo;
                            }
                            if (subitem.IsFirstOrder == true)
                            {
                                _tmplist.Insert(0, subitem);
                            }
                            else
                            {
                                _tmplist.Add(subitem);

                            }
                        }
                        item.GroupOrders = _tmplist.OrderByDescending(d => d.IsFirstOrder).ThenByDescending(d => d.JoinTime).ToList();
                    }
                }
            }
            #endregion
        }


        public List<FightGroupActiveInfo> GetLastModify()
        {
            var curDate = DateTime.Now;
            var date = DateTime.Now.AddMinutes(-15);
            return DbFactory.Default.Get<FightGroupActiveInfo>().Where(p => (p.StartTime <= curDate && p.StartTime >= date) || (p.EndTime < curDate && p.EndTime >= date)).ToList();
        }

        /// <summary>
        /// 获取参与的拼团
        /// </summary>
        /// <param name="userId">用户编号</param>
        /// <param name="Statuses">参与状态</param>
        /// <param name="PageNo"></param>
        /// <param name="PageSize"></param>
        /// <returns></returns>
        public QueryPageModel<FightGroupInfo> GetJoinGroups(
            long userId
            , List<FightGroupOrderJoinStatus> Statuses = null
            , int PageNo = 1
            , int PageSize = 10
            )
        {
            QueryPageModel<FightGroupInfo> result = new QueryPageModel<FightGroupInfo>();
            var sql = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderUserId == userId);
            if (Statuses != null)
            {
                if (Statuses.Count > 0)
                {
                    var _vs = Statuses.Select(d => (int)d).ToList();
                    sql.Where(d => d.JoinStatus.ExIn(_vs));
                }
            }
            var pagesql = sql.OrderByDescending(o => o.JoinTime).ToPagedList(PageNo, PageSize);
            result.Total = pagesql.TotalRecordCount;
            var gpids = pagesql.Select(o => new { o.GroupId, o.SkuId }).ToDictionary(e => e.GroupId, v => v.SkuId);

            var gplist = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id.ExIn(gpids.Keys)).ToList();
            List<FightGroupInfo> datalist = new List<FightGroupInfo>();
            if (gplist != null)
            {
                if (gplist.Count > 0)
                {
                    foreach (var item in gpids.Keys)
                    {
                        datalist.Add(gplist.FirstOrDefault(d => d.Id == item));
                    }
                    if (datalist.Count > 0)
                    {
                        GroupsInfoFill(datalist, true, FightGroupOrderJoinStatus.JoinSuccess, gpids);
                    }
                }
            }
            result.Models = datalist;
            return result;
        }

        /// <summary>
        /// 获取用户参与的拼团数
        /// </summary>
        /// <param name="userId">用户编号</param>
        public int GetJoinGroupNumber(long userId)
        {
            //用户参与的团数量
            var seastatus = new List<FightGroupOrderJoinStatus>
            {
                FightGroupOrderJoinStatus.Ongoing,
                FightGroupOrderJoinStatus.JoinSuccess
            };


            return DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderUserId == userId && d.JoinStatus.ExIn(seastatus)).Count();
        }
        /// <summary>
        /// 拼团失败
        /// </summary>
        /// <param name="data"></param>
        public void GroupFailed(long groupId)
        {
            string _lock = groupId.ToString();
            lock (string.Intern(_lock))
            {
                var _fv = (int)FightGroupBuildStatus.Failed;
                var data = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == groupId && d.GroupStatus != _fv).FirstOrDefault();
                if (data != null)
                {
                    var gponstate = (int)FightGroupBuildStatus.Failed;
                    data.GroupStatus = gponstate;
                    data.OverTime = DateTime.Now;
                    DbFactory.Default.Update(data);
                    var gporders = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.GroupId == data.Id && d.ActiveId == data.ActiveId).ToList();
                    foreach (var item in gporders)
                    {
                        OrderBuildFailed(item);
                    }
                }
            }
        }

        /// <summary>
        /// 自动关闭过期拼团
        /// </summary>
        public void AutoCloseGroup()
        {
            var gponstate = (int)FightGroupBuildStatus.Ongoing;
            //活动到期对已开团的拼团无影响
            var edate = DateTime.Now.AddDays(1).Date;
            //处理超时的拼团
            var endgroups = DbFactory.Default.Get<FightGroupInfo>().Where(n => "TIMESTAMPDIFF(SECOND, AddGroupTime, NOW())".ExFormat<int>() > "(LimitedHour * 60 * 60)".ExFormat<int>() && n.GroupStatus == gponstate).ToList();
            if (endgroups.Count > 0)
                Log.Debug("[FG]" + DateTime.Now.ToString() + "AC_" + string.Join(",", endgroups.Select(d => d.Id).ToArray()));

            DbFactory.Default.InTransaction(() =>
            {
                foreach (var item in endgroups)
                {
                    GroupFailed(item.Id);
                }
            });
        }
        /// <summary>
        /// 参团
        /// </summary>
        /// <param name="activeId">活动编号</param>
        /// <param name="groupId">团组编号</param>
        /// <returns></returns>
        public void JoinGroup(FightGroupOrderInfo order)
        {
            string _lock = order.GroupId.ToString();
            lock (string.Intern(_lock))
            {
                var gpobj = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.ActiveId == order.ActiveId && d.Id == order.GroupId).FirstOrDefault();
                if (gpobj == null) { throw new HimallException("错误的拼团信息"); }
                if (gpobj.BuildStatus == FightGroupBuildStatus.Opening)
                {
                    //开团
                    gpobj.GroupStatus = FightGroupBuildStatus.Ongoing.GetHashCode();
                    gpobj.AddGroupTime = DateTime.Now;   //开团时间以付款时间为准

                    //维护开团数量
                    DbFactory.Default.Set<FightGroupActiveInfo>().Where(d => d.Id == gpobj.ActiveId).Set(d => d.GroupCount, d => d.GroupCount + 1).Succeed();
                }
                if (gpobj.AddGroupTime.AddHours((double)gpobj.LimitedHour) < DateTime.Now)
                {
                    Task.Factory.StartNew(() => GroupFailed(gpobj.Id));
                    throw new HimallException("拼团失败，成团超时");
                }
                if (gpobj.GroupStatus != FightGroupBuildStatus.Ongoing.GetHashCode() && gpobj.GroupStatus != FightGroupBuildStatus.Success.GetHashCode())
                {
                    Task.Factory.StartNew(() => OrderBuildFailed(order));
                    throw new HimallException("错误的拼团信息");
                }
                //参团成功
                order.JoinStatus = (int)FightGroupOrderJoinStatus.JoinSuccess;
                order.JoinTime = DateTime.Now;
                DbFactory.Default.Update(order);
                //发送提示消息
                Task.Factory.StartNew(() => ServiceProvider.Instance<FightGroupService>.Create.SendMessage(order.OrderId, FightGroupOrderJoinStatus.JoinSuccess));

                gpobj.JoinedNumber = gpobj.JoinedNumber + 1;
                order.OverTime = DateTime.Now;
                if (gpobj.JoinedNumber >= gpobj.LimitedNumber)
                {
                    if (gpobj.BuildStatus == FightGroupBuildStatus.Ongoing)
                    {
                        gpobj.GroupStatus = FightGroupBuildStatus.Success.GetHashCode();
                        gpobj.OverTime = DateTime.Now;
                        //维护成团数量
                        DbFactory.Default.Set<FightGroupActiveInfo>().Where(d => d.Id == gpobj.ActiveId).Set(d => d.OkGroupCount, d => d.OkGroupCount + 1).Succeed();
                    }
                    else if (gpobj.BuildStatus == FightGroupBuildStatus.Success)
                    {
                        gpobj.IsException = true;
                    }
                }
                DbFactory.Default.Update(gpobj);

                //成团成功
                if (gpobj.GroupStatus == FightGroupBuildStatus.Success.GetHashCode())
                {
                    if (gpobj.IsException)
                    {
                        BuildSuccess(gpobj.Id, gpobj.ActiveId, order);
                    }
                    else
                    {
                        BuildSuccess(gpobj.Id, gpobj.ActiveId, null);
                    }

                }
            }
        }

        /// <summary>
        /// 组团成功
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="order">异常团单订单提醒</param>
        public void BuildSuccess(long groupId, long activeId, FightGroupOrderInfo order)
        {
            List<FightGroupOrderInfo> orders = new List<FightGroupOrderInfo>();
            if (order == null)
            {
                var gporders = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.GroupId == groupId && d.ActiveId == activeId).ToList();
                orders.AddRange(gporders);
            }
            else
            {
                orders.Add(order);
            }
            if (orders != null && orders.Count > 0)
            {
                int jstate = FightGroupOrderJoinStatus.JoinSuccess.GetHashCode();  //参团成功的才可以拼团成功
                var _bsv = FightGroupOrderJoinStatus.BuildSuccess.GetHashCode();
                foreach (var item in orders)
                {
                    if (item.JoinStatus == jstate)
                    {
                        //处理订单
                        var orderobj = DbFactory.Default.Get<OrderInfo>().Where(d => d.Id == item.OrderId).FirstOrDefault();
                        if (orderobj != null)
                        {
                            orderobj.LastModifyTime = DateTime.Now;
                            if (orderobj.DeliveryType == CommonModel.DeliveryType.SelfTake)
                            {
                                orderobj.OrderStatus = OrderInfo.OrderOperateStatus.WaitSelfPickUp;
                                orderobj.PickupCode = OrderService.GeneratePickupCode(orderobj.Id);
                            }
                            DbFactory.Default.Update(orderobj);
                        }

                        //发送提示消息
                        Task.Factory.StartNew(() => ServiceProvider.Instance<FightGroupService>.Create.SendMessage(order.OrderId, FightGroupOrderJoinStatus.BuildSuccess));
                    }
                }
                DbFactory.Default.Set<FightGroupOrderInfo>()
                    .Where(d => d.GroupId == groupId && d.ActiveId == activeId && d.JoinStatus == jstate)
                    .Set(d => d.JoinStatus, _bsv)
                    .Set(d => d.OverTime, DateTime.Now)
                    .Succeed();
            }
        }

        /// <summary>
        /// 是否可以参团
        /// </summary>
        /// <param name="activeId"></param>
        /// <param name="groupId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool CanJoinGroup(long activeId, long groupId, long userId)
        {
            bool result = true;
            if (groupId > 0)
            {
                var gpobj = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.ActiveId == activeId && d.Id == groupId).FirstOrDefault();
                if (gpobj == null)
                {
                    result = false;
                    return result;
                }

                if (gpobj.GroupStatus != FightGroupBuildStatus.Ongoing.GetHashCode())
                {
                    if (gpobj.GroupStatus == FightGroupBuildStatus.Opening.GetHashCode())
                    {
                        if (gpobj.HeadUserId != userId)
                        {
                            result = false;
                            return result;
                        }
                    }
                    else
                    {
                        result = false;
                        return result;
                    }
                }
                if (result)
                {
                    int jstate = FightGroupOrderJoinStatus.JoinFailed.GetHashCode();  //参团失败可以再次参团
                    result = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.ActiveId == activeId && d.GroupId == groupId && d.OrderUserId == userId && d.JoinStatus > jstate).Exist();
                    result = !result;
                }
            }
            return result;
        }
        #endregion

        #region 拼团订单
        /// <summary>
        /// 根据拼团活动Id和团组Id获取用户
        /// </summary>
        /// <param name="activeId"></param>
        /// <param name="groupId"></param>
        /// <returns></returns>
        public List<FightGroupOrderInfo> GetActiveUsers(long activeId, long groupId)
        {
            //int jstate = (int)FightGroupOrderJoinStatus.JoinSuccess;
            var statuses = new List<FightGroupOrderJoinStatus>();
            statuses.Add(FightGroupOrderJoinStatus.JoinSuccess);
            statuses.Add(FightGroupOrderJoinStatus.BuildFailed);
            statuses.Add(FightGroupOrderJoinStatus.BuildSuccess);

            //var sql = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.ActiveId == activeId && d.GroupId == groupId);

            //var _sorwhere = PredicateExtensions.False<FightGroupOrderInfo>();
            //foreach (var item in statuses)
            //{
            //    int _v = (int)item;
            //    var _swhere = PredicateExtensions.True<FightGroupOrderInfo>();
            //    _swhere = _swhere.And(d => d.JoinStatus == _v);

            //    _sorwhere = _sorwhere.Or(_swhere);
            //}
            //sql.Where(_sorwhere);

            //var result = sql.ToList();
            //if (result.Count > 0)
            //{
            //    foreach (var item in result)
            //    {
            //        var pro = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == item.OrderUserId).FirstOrDefault();
            //        var groupData = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == item.GroupId).FirstOrDefault();
            //        if (pro != null)
            //        {
            //            item.RealName = pro.RealName;
            //            item.UserName = string.IsNullOrWhiteSpace(pro.Nick) ? pro.UserName : pro.Nick;
            //            item.Photo = pro.Photo;
            //            item.GroupStatus = (FightGroupBuildStatus)groupData.GroupStatus;
            //        }
            //    }
            //}
            //return result;
            return DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.ActiveId == activeId && d.GroupId == groupId && d.JoinStatus.ExIn(statuses)).ToList();
        }
        /// <summary>
        /// 根据用户id获取拼团订单
        /// </summary>
        /// <param name="userID">用户id</param>
        /// <returns></returns>
        public QueryPageModel<FightGroupOrderInfo> GetFightGroupOrderByUser(int PageNo, int PageSize, long userID, List<FightGroupOrderJoinStatus> status = null)
        {
            QueryPageModel<FightGroupOrderInfo> data = new QueryPageModel<FightGroupOrderInfo>();
            var sql = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderUserId == userID);
            if (status != null)
            {
                if (status.Count > 0)
                {
                    var _sorwhere = PredicateExtensions.False<FightGroupOrderInfo>();
                    foreach (var item in status)
                    {
                        int _v = (int)item;
                        var _swhere = PredicateExtensions.True<FightGroupOrderInfo>();
                        _swhere = _swhere.And(d => d.JoinStatus == _v);

                        _sorwhere = _sorwhere.Or(_swhere);
                    }
                    sql.Where(_sorwhere);
                }
            }

            var result = sql.OrderByDescending(o => o.JoinTime).ToPagedList(PageNo, PageSize);
            foreach (var item in result)
            {
                var group = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == item.GroupId).FirstOrDefault();
                item.GroupStatus = (FightGroupBuildStatus)group.GroupStatus;
                item.LimitedNumber = group.LimitedNumber;
                item.LimitedHour = group.LimitedHour;
                item.JoinedNumber = group.JoinedNumber;
                item.AddGroupTime = group.AddGroupTime;
            }
            data.Models = result;
            data.Total = result.TotalRecordCount;
            return data;
        }
        /// <summary>
        /// 用户在营销活动中已购买数量
        /// </summary>
        /// <param name="activeId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public long GetMarketSaleCountForUserId(long activeId, long userId)
        {
            int jfstate = FightGroupOrderJoinStatus.JoinFailed.GetHashCode();  //参团失败可以再次购买
            int bfstate = FightGroupOrderJoinStatus.BuildFailed.GetHashCode();  //拼团失败可以再次购买
            var actordsql = DbFactory.Default
                .Get<FightGroupOrderInfo>()
                .Where(d => d.ActiveId == activeId && d.OrderUserId == userId && (d.JoinStatus != jfstate && d.JoinStatus != bfstate))
                .Sum<long>(d => d.Quantity);
            return actordsql;
        }



        /// <summary>
        /// 判断用户当前营销活动详情页面是否显示去分享按钮
        /// </summary>
        /// <param name="activeId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public bool IsShareButtom(long activeId, long userId)
        {
            int jfstate = FightGroupOrderJoinStatus.JoinSuccess.GetHashCode();
            int bfstate = FightGroupOrderJoinStatus.Ongoing.GetHashCode();
            var actordsql = DbFactory.Default
                .Get<FightGroupOrderInfo>()
                .Where(d => d.ActiveId == activeId && d.OrderUserId == userId && (d.JoinStatus == jfstate || d.JoinStatus == bfstate))
                .Sum<long>(d => d.Quantity);
            return actordsql > 0;
        }

        /// <summary>
        /// 根据活动ID获取用户的活动订单
        /// </summary>
        /// <param name="activeId">活动Id</param>
        /// <returns></returns>
        public List<FightGroupOrderInfo> GetFightGroupOrderList(long activeId)
        {
            int jfstate = FightGroupOrderJoinStatus.JoinSuccess.GetHashCode();
            int bfstate = FightGroupOrderJoinStatus.Ongoing.GetHashCode();
            var actordsql = DbFactory.Default
                .Get<FightGroupOrderInfo>()
                .Where(d => d.ActiveId == activeId && (d.JoinStatus == jfstate || d.JoinStatus == bfstate)).ToList();
            return actordsql;
        }

        /// <summary>
        /// 根据活动ID和用户ID获取用户的活动订单
        /// </summary>
        /// <param name="activeId">活动Id</param>
        /// <param name="userId">用户Id</param>
        /// <returns></returns>
        public List<FightGroupOrderInfo> GetFightGroupOrderList(long activeId, long userId)
        {
            int jfstate = FightGroupOrderJoinStatus.JoinSuccess.GetHashCode();
            int bfstate = FightGroupOrderJoinStatus.Ongoing.GetHashCode();
            var actordsql = DbFactory.Default
                .Get<FightGroupOrderInfo>()
                .Where(d => d.ActiveId == activeId && d.OrderUserId == userId && (d.JoinStatus == jfstate || d.JoinStatus == bfstate)).ToList();
            return actordsql;
        }

        /// <summary>
        /// 根据拼团活动参团失败需自动退款拼团订单
        /// </summary>
        /// <param name="activeIds">活动Id集合</param>
        /// <param name="groupIds">活动组集合</param>
        /// <returns></returns>
        public List<FightGroupOrderInfo> GetFightGroupOrderListFail(List<long> activeIds, List<long> groupIds)
        {
            var joinStatusValue = FightGroupOrderJoinStatus.BuildFailed.GetHashCode(); //参团失败
            return DbFactory.Default.Get<FightGroupOrderInfo>()
                .Where(t => t.ActiveId.ExIn(activeIds) && t.GroupId.ExIn(groupIds) && t.JoinStatus == joinStatusValue).ToList();
        }

        /// <summary>
        /// 获取用户拼团商品的已购买数量（商品所有拼团活动）
        /// </summary>
        /// <param name="productId"></param>
        /// <param name="userId"></param>
        /// <returns></returns>
        public Dictionary<long, long> GetMarketSaleCountForProductIdAndUserId(IEnumerable<long> productIds, long userId)
        {
            int jfstate = FightGroupOrderJoinStatus.JoinFailed.GetHashCode();  //参团失败可以再次购买
            int bfstate = FightGroupOrderJoinStatus.BuildFailed.GetHashCode();  //拼团失败可以再次购买
            var countDict = DbFactory.Default
                .Get<FightGroupOrderInfo>()
                .Where(d => d.ProductId.ExIn(productIds) && d.OrderUserId == userId &&
                    (d.JoinStatus != jfstate && d.JoinStatus != bfstate))
                .GroupBy(e => e.ProductId)
                .Select(n => new { n.ProductId, Quantity = n.Quantity.ExSum() })
                .ToList<dynamic>()
                .ToDictionary<dynamic, long, object>(e => e.ProductId, e => e.Quantity);
            Dictionary<long, long> countDictt = new Dictionary<long, long>();
            foreach (var item in countDict)
            {
                countDictt.Add(item.Key, long.Parse(Convert.ToString(item.Value)));
            }
            return countDictt;
        }
        /// <summary>
        /// 订单拼团失败
        /// </summary>
        /// <param name="groupOrder"></param>
        /// <param name="group"></param>
        public void OrderBuildFailed(FightGroupOrderInfo groupOrder, string managerName = "系统Job", string closeReason = "拼团失败，系统自动处理")
        {
            long orderId = groupOrder.OrderId;
            string _lock = groupOrder.GroupId.ToString();
            bool sendmessage = false;
            lock (string.Intern(_lock))
            {
                if (groupOrder.JoinStatus == FightGroupOrderJoinStatus.JoinFailed.GetHashCode() || groupOrder.JoinStatus == FightGroupOrderJoinStatus.BuildFailed.GetHashCode())
                {
                    //已失败的订单不处理
                    return;
                }
                var _OrderService = ServiceProvider.Instance<OrderService>.Create;
                Entities.OrderInfo order = _OrderService.GetOrder(orderId);
                if (order?.OrderStatus >= Entities.OrderInfo.OrderOperateStatus.WaitDelivery)
                {
                    sendmessage = true;
                    groupOrder.JoinStatus = FightGroupOrderJoinStatus.BuildFailed.GetHashCode();
                }
                else
                {
                    groupOrder.JoinStatus = FightGroupOrderJoinStatus.JoinFailed.GetHashCode();
                }
                groupOrder.OverTime = DateTime.Now;
                DbFactory.Default.Update(groupOrder);
                //退库存,退款的时候会退还库存，所以此处不需要
                //UpdateActiveStock(groupOrder.ActiveId, groupOrder.SkuId, groupOrder.Quantity);

                #region 订单退款
                //订单有可能被删
                if (order != null)
                {
                    if (order.OrderStatus >= Entities.OrderInfo.OrderOperateStatus.WaitDelivery && order.OrderStatus != OrderInfo.OrderOperateStatus.Close)  //已付款订单
                    {
                        var userinfos = DbFactory.Default.Get<MemberInfo>().Where(d => d.Id == order.UserId).FirstOrDefault();
                        if (userinfos == null) { return; }
                        //处理退款
                        var _RefundService = ServiceProvider.Instance<RefundService>.Create;
                        //计算可退金额 预留
                        _OrderService.CalculateOrderItemRefund(orderId);
                        var orderitems = DbFactory.Default.Get<OrderItemInfo>().Where(p => p.OrderId == order.Id).ToList();
                        OrderRefundInfo refundinfo = new OrderRefundInfo();
                        refundinfo.OrderId = orderId;
                        refundinfo.UserId = groupOrder.OrderUserId;
                        refundinfo.RefundMode = OrderRefundInfo.OrderRefundMode.OrderRefund;
                        refundinfo.Applicant = userinfos.UserName;
                        refundinfo.ApplyDate = DateTime.Now;
                        refundinfo.Reason = "拼团失败，系统处理";
                        refundinfo.Amount = order.OrderEnabledRefundAmount;
                        refundinfo.ContactPerson = order.ShipTo;
                        refundinfo.ContactCellPhone = order.CellPhone;
                        refundinfo.RefundPayType = OrderRefundInfo.OrderRefundPayType.BackCapital;
                        refundinfo.OrderItemId = orderitems.FirstOrDefault().Id;
                        if (order.CanBackOut())
                        {
                            refundinfo.RefundPayType = OrderRefundInfo.OrderRefundPayType.BackOut;
                        }
                        try
                        {
                            _RefundService.AddOrderRefund(refundinfo);

                            try
                            {
                                //自动同意退款
                                _RefundService.SellerDealRefund(refundinfo.Id, Entities.OrderRefundInfo.OrderRefundAuditStatus.WaitDelivery, "拼团失败，系统自动处理", "系统Job");

                                //自动平台同意退款
                                string notifyurl = ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteUrl + "/Pay/RefundNotify/{0}";//获取异步通知地址
                                _RefundService.ConfirmRefund(refundinfo.Id, "拼团失败订单，自动确认退款", "自动同意售后", notifyurl);
                            }
                            catch (Exception ex)
                            {
                                string msg = string.Format("拼团失败订单，自动确认退款失败！售后Id：{0}，订单号：{1}，失败提示：{2}", refundinfo.Id, refundinfo.OrderId, ex.Message);
                                Log.Error(msg); //当比如有些原路退款 余额不足等退款失败,特意用try{}catch{}，让能执行下一个退款
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error("[FG]退款错误" + order.Id + "_" + ex.Message);
                        }
                    }
                    else
                    {
                        _OrderService.PlatformCloseOrder(orderId, managerName, closeReason);
                    }
                }
                #endregion

                //开团中的团关闭
                var _ov = FightGroupBuildStatus.Opening.GetHashCode();
                var _fv = FightGroupBuildStatus.Failed.GetHashCode();
                DbFactory.Default.Set<FightGroupInfo>()
                    .Where(d => d.ActiveId == groupOrder.ActiveId && d.Id == groupOrder.GroupId && d.GroupStatus == _ov)
                    .Set(d => d.GroupStatus, _fv)
                    .Succeed();
            }
            if (sendmessage)
            {
                //发送提示消息
                Task.Factory.StartNew(() => ServiceProvider.Instance<FightGroupService>.Create.SendMessage(groupOrder.OrderId, FightGroupOrderJoinStatus.BuildFailed));
            }
        }

        /// <summary>
        /// 发送提示消息
        /// </summary>
        /// <param name="orderId"></param>
        /// <param name="status"></param>
        public void SendMessage(long orderId, FightGroupOrderJoinStatus status)
        {
            var gpord = GetOrder(orderId);
            if (gpord == null)
            {
                return;
            }
            var order = DbFactory.Default.Get<OrderInfo>().Where(p => p.Id == gpord.OrderId).FirstOrDefault();
            //var actobj = GetActive(gpord.ActiveId.Value, false, false,false);

            //拼团信息
            var gpinfo = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == gpord.GroupId).FirstOrDefault();
            List<FightGroupInfo> fglist = new List<FightGroupInfo>();
            fglist.Add(gpinfo);
            GroupsInfoFill(fglist, false);
            gpinfo = fglist.FirstOrDefault();

            var wxmsgser = ServiceProvider.Instance<WXMsgTemplateService>.Create;
            var msgdata = new WX_MsgTemplateSendDataModel();
            string url = "";
            MessageOrderInfo orderMessage = new MessageOrderInfo();
            orderMessage.UserName = order.UserName;
            orderMessage.OrderId = order.Id.ToString();
            orderMessage.ShopId = order.ShopId;
            orderMessage.ShopName = order.ShopName;
            orderMessage.SiteName = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.SiteName;
            orderMessage.TotalMoney = order.OrderTotalAmount;
            orderMessage.RefundAuditTime = DateTime.Now;
            if (order.Platform == PlatformType.WeiXinSmallProg)
            {
                orderMessage.MsgOrderType = MessageOrderType.Applet;
            }
            switch (status)
            {
                case FightGroupOrderJoinStatus.BuildFailed:
                    SendSmsOrEmailMessage(gpord.OrderUserId, orderMessage, MessageTypeEnum.FightGroupFailed);
                    #region TDO:ZYF3.2 拼团失败
                    msgdata.first.value = "尊敬的会员，您的拼团参与人数不足，组团失败。";
                    msgdata.first.color = "#000000";
                    msgdata.keyword1.value = gpinfo.ProductName + "";
                    msgdata.keyword1.color = "#000000";
                    msgdata.keyword2.value = gpinfo.JoinedNumber + " 人";
                    msgdata.keyword2.color = "#000000";
                    msgdata.remark.value = "订单金额已自动申请退款，等待平台确认。";
                    msgdata.remark.color = "#000000";
                    url = wxmsgser.GetMessageTemplateShowUrl(MessageTypeEnum.FightGroupFailed);
                    url = url.Replace("{aid}", gpord.ActiveId.ToString());
                    wxmsgser.SendMessageByTemplate(MessageTypeEnum.FightGroupFailed, gpord.OrderUserId, msgdata, url);
                    #endregion
                    break;
                case FightGroupOrderJoinStatus.BuildSuccess:
                    SendSmsOrEmailMessage(gpord.OrderUserId, orderMessage, MessageTypeEnum.FightGroupSuccess);
                    #region TDO:ZYF3.2 拼团成功
                    msgdata.first.value = "尊敬的会员，您的拼团人数满了，组团成功。";
                    msgdata.first.color = "#000000";
                    msgdata.keyword1.value = gpinfo.ProductName + "";
                    msgdata.keyword1.color = "#000000";
                    msgdata.keyword2.value = gpord.JoinedNumber + " 人";
                    msgdata.keyword2.color = "#000000";
                    msgdata.remark.value = "商家已受理，准备发货。";
                    msgdata.remark.color = "#000000";
                    url = wxmsgser.GetMessageTemplateShowUrl(MessageTypeEnum.FightGroupSuccess);
                    url = url.Replace("{gid}", gpord.GroupId.ToString());
                    url = url.Replace("{aid}", gpord.ActiveId.ToString());
                    wxmsgser.SendMessageByTemplate(MessageTypeEnum.FightGroupSuccess, gpord.OrderUserId, msgdata, url);
                    #endregion
                    break;
                case FightGroupOrderJoinStatus.JoinSuccess:
                    if (gpord.IsFirstOrder == true)
                    {
                        SendSmsOrEmailMessage(gpord.OrderUserId, orderMessage, MessageTypeEnum.FightGroupOpenSuccess);
                        #region TDO:ZYF3.2 团长订单发送开团成功
                        msgdata.first.value = "尊敬的会员，您的拼团已开团成功";
                        msgdata.first.color = "#000000";
                        msgdata.keyword1.value = gpinfo.ProductName + "";
                        msgdata.keyword1.color = "#000000";
                        msgdata.keyword2.value = "￥" + gpord.SalePrice.ToString("F2");
                        msgdata.keyword2.color = "#000000";
                        msgdata.keyword3.value = gpinfo.AddGroupTime.AddHours((double)gpinfo.LimitedHour).ToString("yyyy-MM-dd HH:mm:ss");
                        msgdata.keyword3.color = "#FF0000";
                        msgdata.remark.value = "赶紧邀请小伙伴参团吧！";
                        msgdata.remark.color = "#000000";
                        url = wxmsgser.GetMessageTemplateShowUrl(MessageTypeEnum.FightGroupOpenSuccess);
                        url = url.Replace("{gid}", gpord.GroupId.ToString());
                        url = url.Replace("{aid}", gpord.ActiveId.ToString());
                        wxmsgser.SendMessageByTemplate(MessageTypeEnum.FightGroupOpenSuccess, gpord.OrderUserId, msgdata, url);
                        #endregion
                    }
                    else
                    {
                        SendSmsOrEmailMessage(gpord.OrderUserId, orderMessage, MessageTypeEnum.FightGroupJoinSuccess);
                        #region TDO:ZYF3.2 团员信息发送参团成功
                        msgdata.first.value = "尊敬的会员，您的拼团已参团成功";
                        msgdata.first.color = "#000000";
                        msgdata.keyword1.value = gpinfo.ProductName + "";
                        msgdata.keyword1.color = "#000000";
                        msgdata.keyword2.value = "￥" + gpord.SalePrice.ToString("F2");
                        msgdata.keyword2.color = "#000000";
                        msgdata.keyword3.value = gpinfo.AddGroupTime.AddHours((double)gpinfo.LimitedHour).ToString("yyyy-MM-dd HH:mm:ss");
                        msgdata.keyword3.color = "#FF0000";
                        msgdata.remark.value = "赶紧邀请小伙伴参团吧！";
                        msgdata.remark.color = "#000000";
                        url = wxmsgser.GetMessageTemplateShowUrl(MessageTypeEnum.FightGroupJoinSuccess);
                        url = url.Replace("{gid}", gpord.GroupId.ToString());
                        url = url.Replace("{aid}", gpord.ActiveId.ToString());
                        wxmsgser.SendMessageByTemplate(MessageTypeEnum.FightGroupJoinSuccess, gpord.OrderUserId, msgdata, url);
                        Task.Factory.StartNew(() => ServiceProvider.Instance<WXMsgTemplateService>.Create.SendMessageByTemplate(MessageTypeEnum.FightGroupJoinSuccess, gpord.OrderUserId, msgdata, url));
                        #endregion

                        #region TDO:ZYF3.2 有新团员提示消息取消
                        //long gpid = gpord.GroupId.Value;
                        //var firstorder = context.FightGroupOrderInfo.FirstOrDefault(d => d.IsFirstOrder == true && d.GroupId == gpid);
                        //if (gpinfo.LimitedNumber > gpinfo.JoinedNumber)
                        //{
                        //    #region  团员信息发送新成员参团信息给团长
                        //    msgdata = new WX_MsgTemplateSendDataModel();
                        //    msgdata.first.value = "您发起的拼团有新的订单";
                        //    msgdata.first.color = "#000000";
                        //    msgdata.keyword1.value = gpinfo.ProductName;
                        //    msgdata.keyword1.color = "#000000";
                        //    msgdata.keyword2.value = order == null ? "" : order.UserName;
                        //    msgdata.keyword2.color = "#000000";
                        //    msgdata.keyword3.value = "￥" + gpord.SalePrice.ToString("F2");
                        //    msgdata.keyword3.color = "#000000";
                        //    msgdata.keyword5.value =gpord.JoinTime.ToString("yyyy-MM-dd HH:mm:ss");
                        //    msgdata.keyword5.color = "#000000";
                        //    msgdata.remark.value = "距离成团还差" + (gpinfo.LimitedNumber - gpinfo.JoinedNumber).ToString() + "人，再接再厉哦，成团就在眼前！点击进入查看团详情进展情况，感谢你的使用。";
                        //    msgdata.remark.color = "#000000";
                        //    url = wxmsgser.GetMessageTemplateShowUrl(MessageTypeEnum.FightGroupJoinSuccess);
                        //    url = url.Replace("{gid}", gpord.GroupId.ToString());
                        //    url = url.Replace("{aid}", gpord.ActiveId.ToString());
                        //    wxmsgser.SendMessageByTemplate(MessageTypeEnum.FightGroupNewJoin,gpinfo.HeadUserId.Value, msgdata, url);
                        //    Task.Factory.StartNew(() => ServiceProvider.Instance<WXMsgTemplateService>.Create.SendMessageByTemplate(MessageTypeEnum.FightGroupNewJoin, gpinfo.HeadUserId, msgdata, url));
                        //    #endregion
                        //}
                        #endregion
                    }
                    break;
            }
#if DEBUG
            Log.Debug("[FG]SendMessage:" + orderId.ToString() + " _ " + status.ToDescription() + "_" + url);
#endif
        }

        /// <summary>
        /// 发送短信邮件信息
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="info"></param>
        private void SendSmsOrEmailMessage(long userId, MessageOrderInfo info, MessageTypeEnum type)
        {
            var message = PluginsManagement.GetPlugins<IMessagePlugin>().ToList();
            foreach (var msg in message)
            {
                if (msg.Biz.GetStatus(type) == StatusEnum.Open)
                {
                    string destination = ServiceProvider.Instance<MessageService>.Create.GetDestination(userId, msg.PluginInfo.PluginId, MemberContactInfo.UserTypes.General);
                    if (!msg.Biz.CheckDestination(destination))
                        throw new HimallException(msg.Biz.ShortName + "错误");
                    var content = string.Empty;
                    switch (type)
                    {
                        case MessageTypeEnum.FightGroupSuccess:
                            content = msg.Biz.SendMessageOnFightGroupSuccess(destination, info);
                            break;
                        case MessageTypeEnum.FightGroupFailed:
                            content = msg.Biz.SendMessageOnFightGroupFailed(destination, info);
                            break;
                        case MessageTypeEnum.FightGroupOpenSuccess:
                            content = msg.Biz.SendMessageOnFightGroupOpenSuccess(destination, info);
                            break;
                        case MessageTypeEnum.FightGroupJoinSuccess:
                            content = msg.Biz.SendMessageOnFightGroupJoinSuccess(destination, info);
                            break;
                    }
                    if (msg.Biz.EnableLog)
                    {
                        DbFactory.Default.Add(new MessageLogInfo() { SendTime = DateTime.Now, ShopId = info.ShopId, MessageContent = content, TypeId = "短信" });
                    }
                }
            }
        }

        /// <summary>
        /// 获取拼团订单详情
        /// </summary>
        /// <param name="Id">拼团订单流水Id</param>
        /// <returns></returns>
        public FightGroupOrderInfo GetFightGroupOrderById(long id)
        {
            var result = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.Id == id).FirstOrDefault();

            var group = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == result.GroupId).FirstOrDefault();
            if (group != null)
            {
                result.GroupStatus = (FightGroupBuildStatus)group.GroupStatus;
                result.LimitedNumber = group.LimitedNumber;
                result.LimitedHour = group.LimitedHour;
                result.JoinedNumber = group.JoinedNumber;
                result.AddGroupTime = group.AddGroupTime;
            }
            return result;
        }
        /// <summary>
        /// 订单是否可以支付
        /// <para>成团成功后，未完成支付的订单不可付款</para>
        /// <para>成团失败后，未完成支付的订单不可付款</para>
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <returns></returns>
        public bool OrderCanPay(long orderId)
        {
            bool result = true;
            var ordobj = GetOrder(orderId);
            if (ordobj != null)
            {
                switch (ordobj.GroupStatus)
                {
                    case FightGroupBuildStatus.Ongoing:
                        result = true;
                        break;
                    case FightGroupBuildStatus.Opening:
                        result = true;
                        break;
                }
            }
            return result;
        }
        /// <summary>
        /// 获取拼团订单详情
        /// </summary>
        /// <param name="orderId">订单编号</param>
        /// <returns></returns>
        public FightGroupOrderInfo GetOrder(long orderId)
        {
            var result = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderId == orderId).FirstOrDefault();
            if (result == null)
            {
                throw new HimallException("错误的拼团订单信息");
            }
            var group = DbFactory.Default.Get<FightGroupInfo>().Where(d => d.Id == result.GroupId).FirstOrDefault();
            if (group != null)
            {
                result.GroupStatus = (FightGroupBuildStatus)group.GroupStatus;
                result.LimitedNumber = group.LimitedNumber;
                result.LimitedHour = group.LimitedHour;
                result.JoinedNumber = group.JoinedNumber;
                result.AddGroupTime = group.AddGroupTime;
            }
            return result;
        }
        /// <summary>
        /// 获取参团中的订单数
        /// </summary>
        /// <param name="userId">用户编号</param>
        /// <returns></returns>
        public int CountJoiningOrder(long userId)
        {
            int jstate = FightGroupOrderJoinStatus.JoinSuccess.GetHashCode();
            return DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.JoinStatus == jstate && d.OrderUserId == userId).Count();
        }

        /// <summary>
        /// 获取用户参与的拼团数
        /// </summary>
        /// <param name="userId">用户编号</param>
        public int GetJoinAllGroupNumber(long userId)
        {
            //用户参与的团数量
            var seastatus = new List<FightGroupOrderJoinStatus>
            {
                FightGroupOrderJoinStatus.BuildFailed,
                FightGroupOrderJoinStatus.JoinSuccess,
                FightGroupOrderJoinStatus.BuildSuccess
            };


            return DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderUserId == userId && d.JoinStatus.ExIn(seastatus)).Count();
        }
        public void ExecCreated(int grouponId, int groupId, long orderId, long memberId, OrderCreating.ProductItem item)
        {
            var groupon = GetNotEnd().FirstOrDefault(p => p.Id == grouponId);
            var leader = groupId == 0;

            var model = new FightGroupOrderInfo();
            model.ActiveId = grouponId;
            model.ProductId = item.ProductId;
            model.SkuId = item.SkuId;
            model.GroupId = groupId;
            model.OrderId = orderId;
            model.OrderUserId = memberId;
            model.IsFirstOrder = leader;
            model.JoinTime = DateTime.Now;
            model.JoinStatus = leader ? FightGroupOrderJoinStatus.BuildOpening.GetHashCode() : FightGroupOrderJoinStatus.Ongoing.GetHashCode();  //开团中
            model.Quantity = item.Quantity;
            model.SalePrice = item.SalePrice;

            DbFactory.Default.InTransaction(() =>
            {
                if (leader)
                {
                    //开团
                    var data = new FightGroupInfo
                    {
                        HeadUserId = memberId,
                        ActiveId = grouponId,
                        LimitedNumber = groupon.LimitedNumber,
                        LimitedHour = groupon.LimitedHour,
                        JoinedNumber = 0,
                        IsException = false,
                        GroupStatus = (int)FightGroupBuildStatus.Opening,
                        AddGroupTime = DateTime.Now,
                        ProductId = groupon.ProductId,
                        ShopId = groupon.ProductId
                    };
                    DbFactory.Default.Add(data);
                    model.GroupId = data.Id;
                }
                //添加拼团订单记录
                DbFactory.Default.Add(model);

                //变更库存
                DbFactory.Default.Set<FightGroupActiveItemInfo>()
                    .Where(p => p.ActiveId == grouponId && p.SkuId == item.SkuId)
                    .Set(p => p.ActiveStock, p => p.ActiveStock - item.Quantity)
                    .Set(p => p.BuyCount, p => p.BuyCount + item.Quantity)
                    .Execute();
            });
        }

        public void ExecPaid(long orderId)
        {
            FightGroupOrderInfo order = null;
            for (int i = 0; i < 10; i++)
            {
                order = DbFactory.Default.Get<FightGroupOrderInfo>(p => p.OrderId == orderId).FirstOrDefault();
                if (order != null)
                    break;
                Thread.Sleep(1000);
            }
            if (order == null)
                return;

            JoinGroup(order);

            /*
            if (order.IsFirstOrder)
            {
                DbFactory.Default.Set<FightGroupActiveInfo>()
                        .Where(p => p.Id == order.ActiveId)
                        .Set(p => p.GroupCount, p => p.GroupCount + 1)
                        .Execute();
            }

            //参团状态
            DbFactory.Default.Set<FightGroupOrderInfo>()
                .Where(p => p.Id == order.Id)
                .Set(p => p.JoinStatus, FightGroupOrderJoinStatus.JoinSuccess)
                .Execute();

            //参团人数 +1
            DbFactory.Default.Set<FightGroupInfo>()
                .Where(p => p.Id == order.GroupId)
                .Set(p => p.JoinedNumber, p => p.JoinedNumber + 1)
                .Execute();

            var group = DbFactory.Default.Get<FightGroupInfo>(p => p.Id == order.GroupId).FirstOrDefault();
            if (group.JoinedNumber >= group.LimitedNumber)
            { //成团
                DbFactory.Default.Set<FightGroupInfo>()
                    .Where(p => p.Id == group.Id)
                    .Set(p => p.GroupStatus, FightGroupBuildStatus.Success)
                    .Execute();

                var orders = DbFactory.Default.Get<FightGroupOrderInfo>(p => p.GroupId == order.GroupId).ToList();

            }
            */
        }

        /// <summary>
        /// 根据原订单号获取拼团订单信息
        /// </summary>
        /// <param name="orderId">原订单号</param>
        /// <returns></returns>
        public FightGroupOrderInfo GetFightGroupOrderStatusByOrderId(long orderId)
        {
            var result = DbFactory.Default.Get<FightGroupOrderInfo>().Where(d => d.OrderId == orderId).FirstOrDefault();
            return result;
        }

        #endregion

        #region 私有方法
        /// <summary>
        /// 更新活动状态
        /// </summary>
        private void AutoUpdateActiveTimeStatus()
        {
            DbFactory.Default.InTransaction(() =>
            {
                var dtNow = DateTime.Now;
                DbFactory.Default.Set<FightGroupActiveInfo>().Set(n => n.ActiveTimeStatus, -1).Where(n => n.EndTime < dtNow).Succeed();
                DbFactory.Default.Set<FightGroupActiveInfo>().Set(n => n.ActiveTimeStatus, 0).Where(n => n.StartTime <= dtNow && n.EndTime > dtNow).Succeed();
                DbFactory.Default.Set<FightGroupActiveInfo>().Set(n => n.ActiveTimeStatus, 1).Where(n => n.StartTime > dtNow).Succeed();
            });
        }
        #endregion

        public List<FightGroupPrice> GetFightGroupPrice()
        {
            var dtNow = DateTime.Now;
            var list = DbFactory.Default
                .Get<FightGroupActiveItemInfo>()
                .LeftJoin<FightGroupActiveInfo>((fgaii, fgai) => fgaii.ActiveId == fgai.Id)
                .Where<FightGroupActiveInfo>(n => n.StartTime < dtNow && n.EndTime > dtNow)
                .Select(n => new { n.ProductId, n.ActivePrice, n.ActiveId })
                .ToList<FightGroupPrice>();
            return list;
        }

        /// <summary>
        /// 根据拼团活动Id获取拼图销量
        /// </summary>
        /// <param name="activeId">拼团活动Id</param>
        /// <returns></returns>
        public long GetFightGroupSaleVolumeByActiveId(long activeId)
        {
            var result = DbFactory.Default.Get<FightGroupActiveItemInfo>().Where(d => d.ActiveId == activeId).Sum(d => d.BuyCount);
            return result;
        }
    }
}
