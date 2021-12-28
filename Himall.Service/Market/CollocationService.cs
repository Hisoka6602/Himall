using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.DTO.CacheData;
using Himall.DTO.QueryModel;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Service
{
    public class CollocationService : ServiceBase
    {
        public void AddCollocation(Collocation info)
        {
            CheckCollocationDate(info.ShopId, info.EndTime);
            var date = DateTime.Now.Date;
            var mainId = info.Products.Where(a => a.IsMain == true).Select(a => a.ProductId).FirstOrDefault();
            var flag = DbFactory.Default
                .Get<CollocationPoruductInfo>()
                .InnerJoin<CollocationInfo>((cpi, ci) => ci.Id == cpi.ColloId)
                .Where(a => a.IsMain == true && a.ProductId == mainId)
                .Where<CollocationInfo>(a => a.EndTime > date)
                .Exist();
            if (flag)
            {
                throw new HimallException("此主商品已存在组合购，请勿重复添加！");
            }

            DbFactory.Default
                .InTransaction(() =>
                {
                    var model = new CollocationInfo
                    {
                        Title = info.Title,
                        StartTime = info.StartTime,
                        EndTime = info.EndTime,
                        ShortDesc = info.ShortDesc,
                        ShopId = info.ShopId,
                        CreateTime = DateTime.Now
                    };
                    var ret1 = DbFactory.Default.Add(model);

                    var products = info.Products.Select(p => new CollocationPoruductInfo
                    {
                        ColloId = model.Id,
                        ProductId = p.ProductId,
                        DisplaySequence = p.DisplaySequence,
                        IsMain = p.IsMain
                    }).ToList();
                    var ret2 = DbFactory.Default.AddRange(products);

                    var skus = info.Products.SelectMany(p =>
                    {
                        var pro = products.FirstOrDefault(o => o.ProductId == p.ProductId);
                        return p.SKUs.Select(s => new CollocationSkuInfo
                        {
                            ColloProductId = pro.Id,
                            Price = s.Price,
                            ProductId = p.ProductId,
                            SkuID = s.SKUId,
                            SkuPirce = s.SKUPrice,
                        });
                    });
                    var ret3 = DbFactory.Default.Add(skus);
                });
            CacheManager.ClearAvailableCollocation();//组合购清缓存
        }

        private void CheckCollocationDate(long shopId, DateTime endDate)
        {
            var co = DbFactory.Default
                .Get<ActiveMarketServiceInfo>()
                .Where(a => a.TypeId == MarketType.Collocation && a.ShopId == shopId)
                .FirstOrDefault();
            if (co == null)
            {
                throw new HimallException("您没有订购此服务");
            }
            var serviceEndTime = Himall.ServiceProvider.Instance<MarketService>.Create.GetServiceEndTime(co.Id);
            var newEnd = DateTime.Parse(serviceEndTime.ToString("yyyy-MM-dd") + " 23:59:59");
            if (newEnd < endDate)
            {
                throw new HimallException(string.Format("活动结束时间不得超过服务到期时间，<br/>您的服务到期时间为{0}", newEnd.ToString("yyyy-MM-dd HH:mm:ss")));
            }
        }

        public void EditCollocation(Collocation model)
        {
            CheckCollocationDate(model.ShopId, model.EndTime);
            var coll = DbFactory.Default.Get<CollocationInfo>().Where(p => p.Id == model.Id).FirstOrDefault();
            if (coll.EndTime < DateTime.Now.Date)
                throw new HimallException("该活动已结束，无法修改！");

            DbFactory.Default
                .InTransaction(() =>
                {
                    //修改基本信息
                    DbFactory.Default.Set<CollocationInfo>()
                    .Set(p => p.Title, model.Title)
                    .Set(p => p.StartTime, model.StartTime)
                    .Set(p => p.EndTime, model.EndTime)
                    .Set(p => p.ShortDesc, model.ShortDesc)
                    .Where(p => p.Id == model.Id)
                    .Succeed();

                    var products = model.Products.Select(p => new CollocationPoruductInfo
                    {
                        ColloId = model.Id,
                        ProductId = p.ProductId,
                        DisplaySequence = p.DisplaySequence,
                        IsMain = p.IsMain
                    }).ToList();

                    var delProducts = DbFactory.Default.Get<CollocationPoruductInfo>(p => p.ColloId == model.Id).Select(p => p.Id).ToList<long>();
                    DbFactory.Default.Del<CollocationPoruductInfo>(p => p.ColloId == model.Id);
                    DbFactory.Default.AddRange(products);

                    var skus = model.Products.SelectMany(p =>
                    {
                        var pro = products.FirstOrDefault(o => o.ProductId == p.ProductId);
                        return p.SKUs.Select(s => new CollocationSkuInfo
                        {
                            ColloProductId = pro.Id,
                            Price = s.Price,
                            ProductId = p.ProductId,
                            SkuID = s.SKUId,
                            SkuPirce = s.SKUPrice,
                        });
                    });
                    DbFactory.Default.Del<CollocationSkuInfo>(p => p.ColloProductId.ExIn(delProducts));
                    DbFactory.Default.AddRange(skus);
                });
            CacheManager.ClearAvailableCollocation();//组合购清缓存
        }

        public void CancelCollocation(long CollocationId, long shopId)
        {
            var coll = DbFactory.Default.Get<CollocationInfo>().Where(a => a.Id == CollocationId && a.ShopId == shopId).FirstOrDefault();
            if (coll != null)
                coll.EndTime = DateTime.Now.Date;
            DbFactory.Default.Update(coll);
            CacheManager.ClearAvailableCollocation();//组合购清缓存
        }

        public QueryPageModel<CollocationInfo> GetCollocationList(CollocationQuery query)
        {
            var coll = DbFactory.Default
                .Get<CollocationInfo>()
                .InnerJoin<ShopInfo>((ci, si) => si.Id == ci.ShopId)
                .InnerJoin<CollocationPoruductInfo>((ci, cpi) => ci.Id == cpi.ColloId && cpi.IsMain == true)
                .Select()
                .Select<ShopInfo>(n => n.ShopName)
                .Select<CollocationPoruductInfo>(n => n.ProductId);

            if (!string.IsNullOrEmpty(query.Title))
            {
                coll.Where(d => d.Title.Contains(query.Title));
            }
            if (query.ShopId.HasValue)
            {
                coll.Where(d => d.ShopId == query.ShopId.Value);
            }
            if (query.Status > 0)
            {
                switch (query.Status)
                {
                    case 1:
                        coll.Where(d => d.StartTime <= DateTime.Now && d.EndTime >= DateTime.Now); break;//进行中
                    case 2:
                        coll.Where(d => d.StartTime > DateTime.Now); break;//未开始
                    case 3:
                        coll.Where(d => d.EndTime < DateTime.Now); break;//已结束

                }
            }
            var datalist = coll.OrderByDescending(item => item.StartTime).ToPagedList(query.PageNo, query.PageSize);//

            var pageModel = new QueryPageModel<CollocationInfo>()
            {
                Models = datalist,
                Total = datalist.TotalRecordCount
            };
            return pageModel;
        }

        public CollocationInfo GetCollocationByProductId(long productId)
        {
            var date = DateTime.Now.Date;
            var coll = DbFactory.Default
                .Get<CollocationPoruductInfo>()
                .InnerJoin<CollocationInfo>((cpi, ci) => cpi.ColloId == ci.Id)
                .Where(a => a.ProductId == productId && a.IsMain == true)
                .Where<CollocationInfo>(a => a.StartTime <= date && a.EndTime > date)
                .Select<CollocationInfo>()
                .FirstOrDefault<CollocationInfo>();
            return coll;
        }
        public List<CollocationPoruductInfo> GetCollocationListByProductId(long productId)
        {
            var date = DateTime.Now.Date;
            var product = DbFactory.Default
                .Get<ProductInfo>()
                .Where(d => d.Id == productId && d.SaleStatus == ProductInfo.ProductSaleStatus.OnSale
                    && d.AuditStatus == ProductInfo.ProductAuditStatus.Audited && d.IsDeleted == false)
                .Exist();

            if (product)
            {
                var collItem = DbFactory.Default
                    .Get<CollocationPoruductInfo>()
                    .InnerJoin<CollocationInfo>((cpi, ci) => cpi.ColloId == ci.Id)
                    .Where(a => a.ProductId == productId)
                    .Where<CollocationInfo>(a => a.StartTime <= date && a.EndTime > date)
                    .ToList();

                if (collItem != null && collItem.Count > 0)
                {
                    return collItem;
                }
            }
            return null;
        }

        public List<CollocationInfo> GetAvailableCollocationByProduct(long product)
        {
            var now = DateTime.Now;
            return DbFactory.Default.Get<CollocationInfo>()
                .LeftJoin<CollocationPoruductInfo>((c, cp) => c.Id == cp.ColloId)
                .Where<CollocationPoruductInfo>(p => p.ProductId == product)
                .Where(p => p.StartTime <= now && p.EndTime > now)
                .ToList();
        }
        /// <summary>
        /// 未结束活动列表
        /// </summary>
        public IEnumerable<CollocationData> GetNotEnd() =>
            CacheManager.GetAvailableCollocation(() =>
            {
                var data = DbFactory.Default.Get<CollocationInfo>()
                                .LeftJoin<CollocationPoruductInfo>((c, p) => c.Id == p.ColloId && p.IsMain)
                                .Where(p => p.EndTime > DateTime.Now)
                                .Select()
                                .Select<CollocationPoruductInfo>(item => new
                                {
                                    item.ProductId,
                                })
                                .ToList<CollocationData>();

                var list = data.Select(p => p.Id).ToList();
                var products = DbFactory.Default.Get<CollocationSkuInfo>()
                        .LeftJoin<CollocationPoruductInfo>((s, p) => s.ColloProductId == p.Id)
                        .Where<CollocationPoruductInfo>(p => p.ColloId.ExIn(list))
                        .Select()
                        .Select<CollocationPoruductInfo>(p => new
                        {
                            ColloProId = p.Id,
                            p.DisplaySequence,
                            p.IsMain,
                            p.ColloId,
                            p.ProductId
                        }).ToList<CollocationProductData>();
                foreach (var item in data)
                    item.Products = products.Where(p => p.ColloId == item.Id).ToList();
                return data;
            }).Where(p => p.EndTime > DateTime.Now);

        /// <summary>
        /// 进行中活动
        /// </summary>
        public IEnumerable<CollocationData> GetGoing() =>
            GetNotEnd().Where(p => p.StartTime < DateTime.Now && p.EndTime > DateTime.Now).ToList();

        public CollocationData GetGoing(long id) =>
            GetGoing().FirstOrDefault(p => p.Id == id);


        public List<CollocationPoruductInfo> GetProducts(List<long> collocation)
        {
            return DbFactory.Default.Get<CollocationPoruductInfo>(p => p.ColloId.ExIn(collocation)).ToList();
        }

        public List<CollocationSkuInfo> GetSKUs(List<long> collProducts)
        {
            return DbFactory.Default.Get<CollocationSkuInfo>(p => p.ColloProductId.ExIn(collProducts)).ToList();
        }

        public CollocationSkuInfo GetColloSku(long colloPid, string skuid)
        {
            return DbFactory.Default
                .Get<CollocationSkuInfo>()
                .Where(a => a.ColloProductId == colloPid && a.SkuID == skuid)
                .FirstOrDefault();
        }

        public CollocationInfo GetCollocation(long Id)
        {
            var coll = DbFactory.Default
                .Get<CollocationInfo>()
                .Where(a => a.Id == Id)
                .FirstOrDefault();

            return coll;
        }

        public List<CollocationSkuInfo> GetProductColloSKU(long productid, long colloPid)
        {
            return DbFactory.Default
                .Get<CollocationSkuInfo>()
                .Where(a => a.ColloProductId == colloPid && a.ProductId == productid)
                .ToList();
        }
    }
}
