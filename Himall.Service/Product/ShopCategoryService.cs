using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using NetRube.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Service
{
    public class ShopCategoryService : ServiceBase
    {
        public List<ShopCategoryInfo> GetMainCategory(long shopId)
        {
            return DbFactory.Default.Get<ShopCategoryInfo>().Where(t => t.ParentCategoryId == 0 && t.ShopId == shopId).OrderBy(a => a.DisplaySequence).ToList();
        }

        /// <summary>
        /// 获取所有商品分类并缓存
        /// </summary>
        /// <returns></returns>
        IEnumerable<ShopCategoryInfo> GetCategories()
        {
            return DbFactory.Default.Get<ShopCategoryInfo>().ToList();
        }

        public void AddCategory(ShopCategoryInfo model)
        {
            if (null == model)
                throw new ArgumentNullException("model", "添加一个商品分类时，Model为空");

            var obja = DbFactory.Default.Get<Entities.ShopCategoryInfo>().Where(r => r.Name.Equals(model.Name) && r.ShopId == model.ShopId && r.ParentCategoryId == model.ParentCategoryId);
            if (obja.Count() > 0)
                throw new HimallException("分类名称已经存在");

            DbFactory.Default.Add(model);
            CacheManager.ClearCategories();
        }

        public ShopCategoryInfo GetCategoryByProductId(long id)
        {
            var model = DbFactory.Default.Get<ProductShopCategoryInfo>().Where(p => p.ProductId == id).OrderByDescending(a => a.ShopCategoryId).FirstOrDefault();
            if (model != null)
                return DbFactory.Default.Get<ShopCategoryInfo>(p => p.Id == model.ShopCategoryId).FirstOrDefault();
            else
                return new ShopCategoryInfo { Name = "" };
        }

        /// <summary>
        ///批量转移商品
        /// </summary>
        /// <param name="cid1"></param>
        /// <param name="cid2"></param>
        /// <returns></returns>
        public bool BatchMoveProductCategory(long cid1, long cid2)
        {
            var flag = DbFactory.Default.Set<ProductShopCategoryInfo>().Set(p => p.ShopCategoryId, cid2).Where(e => e.ShopCategoryId == cid1).Succeed();
            return flag;
        }

        /// <summary>
        /// 通过商品编号取得店铺所属分类和分类名称集合
        /// </summary>
        /// <param name="productIds">商品id集合</param>
        /// <returns></returns>
        public List<Entities.ShopInfo.ShopCategoryAndProductIdModel> GetCategoryNameAndProductIdByProductId(IEnumerable<long> productIds)
        {
            if (productIds.Count() <= 0)
                throw new ArgumentNullException("id", string.Format("获取一个商品分类时，ids={0}", productIds));

            var psclist = DbFactory.Default.Get<Entities.ProductShopCategoryInfo>().InnerJoin<Entities.ShopCategoryInfo>((ii, oo) => oo.Id == ii.ShopCategoryId).Where(p => p.ProductId.ExIn(productIds))
                .OrderByDescending(a => a.ShopCategoryId).GroupBy(a => a.ProductId).GroupBy(a => a.ShopCategoryId)
                .Select<Entities.ProductShopCategoryInfo>(t => new { t.ProductId, t.ShopCategoryId })
                .Select<Entities.ShopCategoryInfo>(p => new { CategoryName = p.Name }).ToList<Entities.ShopInfo.ShopCategoryAndProductIdModel>();

            return psclist;
        }

        public List<ShopCategoryInfo> GetCategorysByProductId(long id)
        {
            var model = DbFactory.Default.Get<ProductShopCategoryInfo>().Where(p => p.ProductId == id).ToList();
            if (model != null)
            {
                var ids = model.Select(p => p.ShopCategoryId).ToList();
                return DbFactory.Default.Get<ShopCategoryInfo>(p => p.Id.ExIn(ids)).ToList();
            }
            else
                return new List<ShopCategoryInfo>();
        }

        public List<ProductShopCategory> GetProductShopCategorys(List<long> products)
        {
            return DbFactory.Default.Get<ProductShopCategoryInfo>()
                .LeftJoin<ShopCategoryInfo>((psc, sc) => sc.Id == psc.ShopCategoryId)
                .Where(p => p.ProductId.ExIn(products))
                .Select().Select<ShopCategoryInfo>(p => new { ShopCategoryName = p.Name })
                .ToList<ProductShopCategory>();
        }
        public ShopCategoryInfo GetCategory(long id)
        {
            if (id <= 0)
                throw new ArgumentNullException("id", string.Format("获取一个商品分类时，id={0}", id));

            var model = GetCategories().Where(t => t.Id == id).FirstOrDefault();
            return model;

        }



        public void UpdateCategoryName(long id, string name)
        {
            //TODO:FG (注意)参数验证应交由UI层，减少无意义参数验证
            if (id <= 0)
                throw new ArgumentNullException("id", string.Format("更新一个商品分类的名称时，id={0}", id));
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentNullException("name", "更新一个商品分类的名称时，name为空");

            var category = DbFactory.Default.Get<Entities.ShopCategoryInfo>().Where(p => p.Id == id).FirstOrDefault();
            if (null == category || category.Id != id)
                throw new HimallException(string.Format("更新一个商品分类的名称时，找不到id={0} 的商品分类", id));


            var obja = DbFactory.Default.Get<Entities.ShopCategoryInfo>().Where(r => r.Name.Equals(name) && r.ShopId == category.ShopId && r.ParentCategoryId == category.ParentCategoryId && r.Id != id);
            if (obja.Count() > 0)
                throw new HimallException("分类名称已经存在");

            category.Name = name;
            DbFactory.Default.Update(category);
            CacheManager.ClearCategories();
        }

        public void UpdateCategoryDisplaySequence(long id, long displaySequence)
        {
            if (id <= 0)
                throw new ArgumentNullException("id", string.Format("更新一个商品分类的显示顺序时，id={0}", id));
            if (0 >= displaySequence)
                throw new ArgumentNullException("displaySequence", "更新一个商品分类的显示顺序时，displaySequence小于等于零");

            var category = DbFactory.Default.Get<Entities.ShopCategoryInfo>().Where(p => p.Id == id).FirstOrDefault();
            if (null == category || category.Id != id)
                throw new Exception(string.Format("更新一个商品分类的显示顺序时，找不到id={0} 的商品分类", id));

            category.DisplaySequence = displaySequence;
            DbFactory.Default.Update(category);

            CacheManager.ClearCategories();
        }

        public void UpdateCategorysShow(bool isShow, List<long> ids)
        {
            DbFactory.Default.Set<Entities.ShopCategoryInfo>()
               .Set(n => n.IsShow, isShow).Where(e => e.Id.ExIn(ids)).Succeed();

            CacheManager.ClearCategories();
        }


        public IEnumerable<Entities.ShopCategoryInfo> GetCategoryByParentId(long id)
        {
            if (id < 0)
                throw new ArgumentNullException("id", string.Format("获取子级商品分类时，id={0}", id));

            if (id == 0)
            {
                return GetCategories().Where(c => c.ParentCategoryId == 0);
            }
            else
            {
                var category = GetCategories().Where(c => c.ParentCategoryId == id);
                if (category == null)
                    return null;
                return category.OrderBy(c => c.DisplaySequence).ToList();
            }
        }



        private void ProcessingDeleteCategory(long id, long shopId)
        {
            var subIds = DbFactory.Default.Get<Entities.ShopCategoryInfo>().Where(c => c.ParentCategoryId == id && c.ShopId == shopId).Select(c => c.Id).ToList<long>();

            var existProduct = DbFactory.Default.Get<Entities.ProductShopCategoryInfo>()
                                .Where(p => p.ShopCategoryId == id || p.ShopCategoryId.ExIn(subIds))
                                .InnerJoin<Entities.ProductInfo>((ti, pi) => ti.ProductId == pi.Id && pi.IsDeleted == false)    //已删除的商品不算
                                .Exist();
            if (existProduct)
                throw new HimallException("删除失败，因为有商品与该分类或子分类关联");
            DbFactory.Default.InTransaction(() =>
            {
                if (subIds.Count == 0)
                {
                    DbFactory.Default.Del<Entities.ProductShopCategoryInfo>(n => n.ShopCategoryId == id);
                    DbFactory.Default.Del<Entities.ShopCategoryInfo>(p => p.Id == id);
                    return;
                }
                else
                {
                    foreach (var item in subIds.ToList())
                    {
                        ProcessingDeleteCategory(item, shopId);
                    }
                }
                DbFactory.Default.Del<Entities.ProductShopCategoryInfo>(n => n.ShopCategoryId == id);
                DbFactory.Default.Del<Entities.ShopCategoryInfo>(p => p.Id == id);

            });
        }

        public void DeleteCategory(long id, long shopId)
        {
            ProcessingDeleteCategory(id, shopId);
            CacheManager.ClearCategories();
        }

        public List<CategoryInfo> GetBusinessCategory(long shopId)
        {
            var categories = ServiceProvider.Instance<CategoryService>.Create.GetCategories();
            var businessCategories = CacheManager.GetCategories(shopId, () =>
            {
                var shop = DbFactory.Default.Get<ShopInfo>(p => p.Id == shopId).FirstOrDefault();
                if (shop.IsSelf)
                    return new BusinessCategories { Self = true };
                else
                {
                    var list = DbFactory.Default.Get<BusinessCategoryInfo>(p => p.ShopId == shopId).Select(p => p.CategoryId).ToList<long>();
                    var result = new List<long>();
                    foreach (var item in list)
                    {
                        var category = categories.FirstOrDefault(p => p.Id == item);
                        if (category != null)
                        {
                            result.AddRange(category.Path.Split('|').Select(p => long.Parse(p)));
                            result.Add(item);
                        }
                    }
                    return new BusinessCategories { Categories = result.Distinct().ToList() };
                }
            });
            if (businessCategories.Self)
                return categories;
            else
                return categories.Where(p => businessCategories.Categories.Contains(p.Id)).ToList();
        }

        public List<ShopCategoryInfo> GetShopCategory(long shopId)
        {
            return DbFactory.Default.Get<Entities.ShopCategoryInfo>().Where(s => s.ShopId == shopId).OrderBy(a => a.DisplaySequence).ToList();
        }



        public IEnumerable<Entities.ShopCategoryInfo> GetCategoryByParentId(long id, long shopId)
        {
            if (id < 0)
                throw new HimallException(string.Format("获取子级分类时，id={0}", id));
            return GetCategories().Where(c => c.ShopId == shopId && c.ParentCategoryId == id && c.IsShow).OrderBy(t => t.DisplaySequence);
        }

        public IEnumerable<Entities.ShopCategoryInfo> GetSecondAndThirdLevelCategories(params long[] ids)
        {
            var categoies = GetCategories().Where(item => ids.Contains(item.ParentCategoryId));
            var categoryList = new List<Entities.ShopCategoryInfo>(categoies);

            foreach (var categoryId in categoies.Select(item => item.Id).ToList())
            {
                var category = GetCategories().Where(item => item.ParentCategoryId == categoryId);
                categoryList.AddRange(category);
            }
            return categoryList;
        }

        public IEnumerable<Entities.ShopCategoryInfo> GetParentCategoryById(long id, bool isshow)
        {
            List<Entities.ShopCategoryInfo> categoryList = new List<Entities.ShopCategoryInfo>();
            var parentId = id;
            do
            {
                var pInfo = GetCategories().Where(item => item.Id == parentId).FirstOrDefault();
                if (pInfo != null)
                {
                    categoryList.Add(pInfo);
                    if (isshow)
                    {
                        var sameCount = GetCategories().Where(item => item.ParentCategoryId == pInfo.ParentCategoryId && item.IsShow == isshow).Count();

                        if (sameCount == 1)
                        {
                            parentId = pInfo.ParentCategoryId;
                        }
                        else
                        {
                            parentId = 0;
                        }
                    }
                    else
                    {
                        parentId = pInfo.ParentCategoryId;
                    }
                }
                else
                {
                    parentId = 0;
                }
            }
            while (parentId > 0);
            return categoryList;
        }
    }
}
