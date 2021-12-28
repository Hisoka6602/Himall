using Himall.Core;
using Himall.Service;
using System.Linq;
using System.Collections.Generic;
using Himall.CommonModel;
using NetRube.Data;
using Himall.Entities;
using Himall.DTO.CacheData;

namespace Himall.Service
{
    public class FreightTemplateService : ServiceBase
    {
        public List<FreightTemplateInfo> GetShopFreightTemplate(long shop)
        {
            return DbFactory.Default.Get<FreightTemplateInfo>().Where(p => p.ShopID == shop).ToList();
        }

        public FreightTemplateInfo GetFreightTemplate(long templateId)
        {
            if (templateId <= 0)
                return null;
            return CacheManager.GetFreightTemplateInfo(templateId, () =>
            {
                var result = DbFactory.Default.Get<FreightTemplateInfo>().Where(e => e.Id == templateId).FirstOrDefault();
                if (result == null)
                    return null;//当前运费模板不存直接返回null

                result.FreightAreaContentInfo = DbFactory.Default.Get<FreightAreaContentInfo>().Where(e => e.FreightTemplateId == result.Id).ToList();
                foreach (var area in result.FreightAreaContentInfo)
                    area.FreightAreaDetailInfo = DbFactory.Default.Get<FreightAreaDetailInfo>().Where(a => a.FreightAreaId == area.Id).ToList();
                return result;
            });
        }

        public List<FreightAreaDetailInfo> GetFreightAreaDetail(long TemplateId)
        {
            string cacheKey = CacheKeyCollection.CACHE_FREIGHTAREADETAIL(TemplateId);
            if (Cache.Exists(cacheKey))
                return Cache.Get<List<FreightAreaDetailInfo>>(cacheKey);
            var result = DbFactory.Default.Get<FreightAreaDetailInfo>().Where(a => a.FreightTemplateId == TemplateId).ToList();
            Cache.Insert(cacheKey, result, 1800);
            return result;
        }

        public List<FreightAreaContentInfo> GetFreightAreaContent(long TemplateId)
        {
            return DbFactory.Default.Get<FreightAreaContentInfo>().Where(e => e.FreightTemplateId == TemplateId).ToList();
        }

        public void UpdateFreightTemplate(FreightTemplateInfo templateInfo)
        {
            FreightTemplateInfo model;
            if (templateInfo.Id == 0)
            {
                DbFactory.Default.InTransaction(() =>
                {
                    var ret1 = DbFactory.Default.Add(templateInfo);

                    foreach (var t in templateInfo.FreightAreaContentInfo)
                    {
                        t.FreightTemplateId = templateInfo.Id;
                    }

                    if (templateInfo.FreightAreaContentInfo.Count() > 0)
                    {
                        var ret2 = DbFactory.Default.Add<FreightAreaContentInfo>(templateInfo.FreightAreaContentInfo);
                    }

                    var areaDetailList = new List<FreightAreaDetailInfo>();
                    foreach (var t in templateInfo.FreightAreaContentInfo)
                    {
                        foreach (var d in t.FreightAreaDetailInfo)
                        {
                            d.FreightAreaId = t.Id;
                            d.FreightTemplateId = t.FreightTemplateId;
                            areaDetailList.Add(d);
                        }
                    }

                    if (areaDetailList.Count > 0)
                    {
                        var ret3 = DbFactory.Default.Add<FreightAreaDetailInfo>(areaDetailList);
                    }

                    #region 指定地区包邮
                    if (templateInfo.ShippingFreeGroupInfo != null)
                    {
                        foreach (var t in templateInfo.ShippingFreeGroupInfo)
                        {
                            t.TemplateId = templateInfo.Id;//模板ID

                            DbFactory.Default.Add(t);
                            if (t.Id > 0)
                            {
                                foreach (var item in t.ShippingFreeRegionInfo)
                                {
                                    item.GroupId = t.Id;//组ID
                                    item.TemplateId = templateInfo.Id;//模板ID
                                    DbFactory.Default.Add(item);
                                }
                            }
                        }
                    }
                });
                #endregion
            }
            else
            {
                model = DbFactory.Default.Get<FreightTemplateInfo>().Where(e => e.Id == templateInfo.Id).FirstOrDefault();
                model.Name = templateInfo.Name;
                model.IsFree = templateInfo.IsFree;
                model.ValuationMethod = templateInfo.ValuationMethod;
                model.ShopID = templateInfo.ShopID;
                model.SourceAddress = templateInfo.SourceAddress;
                model.SendTime = templateInfo.SendTime;


                var flag = DbFactory.Default.InTransaction(() =>
                {
                    DbFactory.Default.Update(model);
                    //先删除
                    DbFactory.Default.Del<FreightAreaContentInfo>(e => e.FreightTemplateId == model.Id);
                    //删除详情表
                    DbFactory.Default.Del<FreightAreaDetailInfo>(e => e.FreightTemplateId == model.Id);

                    if (model.IsFree == FreightTemplateType.SelfDefine)
                    {
                        //重新插入地区运费
                        templateInfo.FreightAreaContentInfo.ForEach(e =>
                        {
                            e.FreightTemplateId = model.Id;
                        });

                        if (templateInfo.FreightAreaContentInfo.Count > 0)
                        {
                            DbFactory.Default.Add<FreightAreaContentInfo>(templateInfo.FreightAreaContentInfo);
                        }

                        var detailList = new List<FreightAreaDetailInfo>();
                        foreach (var t in templateInfo.FreightAreaContentInfo)
                        {
                            foreach (var d in t.FreightAreaDetailInfo)
                            {
                                d.FreightAreaId = t.Id;
                                d.FreightTemplateId = model.Id;
                                detailList.Add(d);
                            }
                        }
                        if (detailList.Count > 0)
                        {
                            DbFactory.Default.Add<FreightAreaDetailInfo>(detailList);
                        }
                    }

                    #region 指定地区包邮
                    DbFactory.Default.Del<ShippingFreeGroupInfo>(e => e.TemplateId == model.Id);
                    DbFactory.Default.Del<ShippingFreeRegionInfo>(e => e.TemplateId == model.Id);

                    if (templateInfo.ShippingFreeGroupInfo != null)
                    {
                        foreach (var t in templateInfo.ShippingFreeGroupInfo)
                        {
                            t.TemplateId = model.Id;//模板ID
                            DbFactory.Default.Add(t);
                            if (t.Id > 0)
                            {
                                foreach (var item in t.ShippingFreeRegionInfo)
                                {
                                    item.GroupId = t.Id;//组ID
                                    item.TemplateId = model.Id;//模板ID

                                    DbFactory.Default.Add(item);
                                }
                            }
                        }
                    }
                    #endregion
                    return true;
                });

            }
            CacheManager.ClearFreightTemplate(templateInfo.Id);
        }

        public FreightTemplateData GetTempalteData(long templateId)
        {
            return CacheManager.GetFreightTemplate(templateId, () =>
            {
                var regionService = ServiceProvider.Instance<RegionService>.Create;
                var model = DbFactory.Default.Get<FreightTemplateInfo>(p => p.Id == templateId).FirstOrDefault<FreightTemplateData>();
                if (model == null) return null;
                var content = DbFactory.Default.Get<FreightAreaContentInfo>(p => p.FreightTemplateId == templateId).ToList();
                model.Rules = content.Select(p => new FreightTemplateRuleData
                {
                    Id = p.Id,
                    FirstUnit = p.FirstUnit,
                    FirstUnitMonry = (decimal)p.FirstUnitMonry,
                    AccumulationUnit = p.AccumulationUnit,
                    AccumulationUnitMoney = (decimal)p.AccumulationUnitMoney,
                    IsDefault = p.IsDefault == 1
                }).ToList();
                var areas = DbFactory.Default.Get<FreightAreaDetailInfo>(p => p.FreightTemplateId == templateId).ToList();
                var map1 = new Dictionary<int, long>();
                if (areas != null && areas.Count > 0)
                {
                    foreach (var item in areas)
                    {
                        if (!map1.ContainsKey(item.ProvinceId))
                            map1.Add(item.ProvinceId, item.FreightAreaId);
                        if (!map1.ContainsKey(item.CityId))
                            map1.Add(item.CityId, item.FreightAreaId);
                        if (item.CountyId > 0)
                        {
                            if (!map1.ContainsKey(item.CountyId))
                                map1.Add(item.CountyId, item.FreightAreaId);
                        }
                        else
                        { //所有区
                            var city = regionService.GetRegion(item.CityId);
                            if (city != null && city.Sub != null && city.Sub.Count > 0)
                            {
                                foreach (var county in city.Sub)
                                {
                                    if (!map1.ContainsKey(county.Id))
                                        map1.Add(county.Id, item.FreightAreaId);
                                    if (county.Sub == null)
                                        continue;
                                    foreach (var town in county.Sub)
                                        if (!map1.ContainsKey(town.Id))
                                            map1.Add(town.Id, item.FreightAreaId);
                                }
                            }
                            continue;
                        }

                        if (!string.IsNullOrEmpty(item.TownIds))
                        {
                            foreach (var i in item.TownIds.Split(',').Select(p => int.Parse(p)))
                                if (!map1.ContainsKey(i))
                                    map1.Add(i, item.FreightAreaId);
                        }
                        else
                        { //默认所有街道
                            var county = regionService.GetRegion(item.CountyId);
                            if (county != null && county.Sub != null && county.Sub.Count > 0)
                            {
                                foreach (var i in county.Sub)
                                    if (!map1.ContainsKey(i.Id))
                                        map1.Add(i.Id, item.FreightAreaId);
                            }
                        }
                    }
                }
                model.RulesMap = map1;
                var free = DbFactory.Default.Get<ShippingFreeGroupInfo>(p => p.TemplateId == templateId).ToList();
                model.FreeRules = free.Select(p =>
                {
                    var item = new FreightTemplateFreeRuleData
                    {
                        Serial = p.Id,
                        FreeType = (FreightTempateFreeType)p.ConditionType,
                    };
                    if (item.FreeType == FreightTempateFreeType.Amount)
                        item.Amount = decimal.Parse(p.ConditionNumber);
                    else if (item.FreeType == FreightTempateFreeType.Piece)
                        item.Piece = int.Parse(p.ConditionNumber);
                    else
                    {
                        var vals = p.ConditionNumber.Split('$');
                        item.Piece = int.Parse(vals[0]);
                        item.Amount = decimal.Parse(vals[1]);
                    }
                    return item;
                }).ToList();
                var freeDetail = DbFactory.Default.Get<ShippingFreeRegionInfo>(p => p.TemplateId == templateId).ToList();
                var map2 = new Dictionary<long, long>();
                foreach (var item in freeDetail)
                {
                    if (!map2.ContainsKey(item.RegionId))
                        map2.Add(item.RegionId, item.GroupId);
                    if (!string.IsNullOrEmpty(item.RegionPath))
                    {
                        foreach (var i in item.RegionPath.Split(',').Select(p => int.Parse(p)))
                            if (!map2.ContainsKey(i))
                                map2.Add(i, item.GroupId);
                    }
                    var regionSubList = ServiceProvider.Instance<RegionService>.Create.GetSubsNew(item.RegionId, true).Select(a => a.Id).ToList();
                    if (regionSubList.Count() > 0)
                    {
                        foreach (var i in regionSubList)
                            if (!map2.ContainsKey(i))
                                map2.Add(i, item.GroupId);
                    }
                }
                model.FreeRulesMap = map2;
                return model;
            });
        }

        public void DeleteFreightTemplate(long templateId)
        {
            var flag = DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Delete<FreightTemplateInfo>(templateId);
                DbFactory.Default.Del<FreightAreaContentInfo>(e => e.FreightTemplateId == templateId);
                DbFactory.Default.Del<FreightAreaDetailInfo>(e => e.FreightTemplateId == templateId);
                DbFactory.Default.Del<ShippingFreeGroupInfo>(e => e.TemplateId == templateId);
                DbFactory.Default.Del<ShippingFreeRegionInfo>(e => e.TemplateId == templateId);
                return true;
            });
            CacheManager.ClearFreightTemplate(templateId);
        }

        /// <summary>
        /// 是否有商品使用过该运费模板
        /// </summary>
        /// <param name="TemplateId"></param>
        /// <returns></returns>
        public bool IsProductUseFreightTemp(long TemplateId)
        {
            return DbFactory.Default.Get<ProductInfo>().Where(item => item.FreightTemplateId == TemplateId && item.IsDeleted == false).Exist();
        }

        public List<ShippingFreeRegionInfo> GetShippingFreeRegions(long TemplateId)
        {
            return DbFactory.Default.Get<ShippingFreeRegionInfo>().Where(a => a.TemplateId == TemplateId).ToList();
        }

        public List<ShippingFreeGroupInfo> GetShippingFreeGroups(long templateId)
        {
            return DbFactory.Default.Get<ShippingFreeGroupInfo>().Where(p => p.TemplateId == templateId).ToList();
        }
        public List<ShippingFreeGroupInfo> GetShippingFreeGroupInfos(long TemplateId, List<long> groupIds)
        {
            var result = DbFactory.Default.Get<ShippingFreeGroupInfo>().Where(a => a.TemplateId == TemplateId);
            if (groupIds != null && groupIds.Count > 0)
            {
                result.Where(a => a.Id.ExIn(groupIds));
            }
            return result.ToList();
        }

        /// <summary>
        /// 获取运费模板列表
        /// </summary>
        /// <param name="templateIds">id集合</param>
        /// <returns></returns>
        public List<FreightTemplateInfo> GetFreightTemplateList(List<long> templateIds)
        {
            if (templateIds == null || templateIds.Count <= 0)
                return null;

            var result = DbFactory.Default.Get<FreightTemplateInfo>().Where(e => e.Id.ExIn(templateIds)).ToList();
            return result;
        }
    }
}
