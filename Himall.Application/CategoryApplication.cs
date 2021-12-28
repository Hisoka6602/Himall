﻿using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using System.Collections.Generic;
using System.Linq;

namespace Himall.Application
{
    /// <summary>
    /// 商品类别
    /// </summary>
    public class CategoryApplication
    {
        private static CategoryService _iCategoryService = ObjectContainer.Current.Resolve<CategoryService>();
        /// <summary>
        /// 获取类别下所有子类
        /// </summary>
        /// <returns></returns>
        public static List<Category> GetSubCategories()
        {
            var categories = _iCategoryService.GetCategories().ToArray();
            var model = categories
                .Where(item => item.ParentCategoryId == 0 && item.IsShow)
                .Select(item => new Category()
                {
                    Id = item.Id,
                    Name = item.Name,
                    SubCategories = GetSubCategories(categories, item.Id, 1),
                    Depth = 0,
                    DisplaySequence = item.DisplaySequence
                }).OrderBy(c => c.DisplaySequence).ToList();
            return model;
        }


        /// <summary>
        /// 递归获取类别下所有子类
        /// </summary>
        /// <param name="allCategoies">所有分类</param>
        /// <param name="categoryId">分类ID</param>
        /// <param name="depth"></param>
        /// <returns></returns>
        public static List<Category> GetSubCategories(IEnumerable<Himall.Entities.CategoryInfo> allCategoies, long categoryId, int depth)
        {
            var categories = allCategoies
                .Where(item => item.ParentCategoryId == categoryId &&  item.IsShow)
                .Select(item =>
                {
                    string image = string.Empty;
                    if (depth == 2)
                    {
                        image = item.Icon;
                        if (string.IsNullOrWhiteSpace(image))
                            image = string.Empty;
                    }
                    return new Category()
                    {
                        Id = item.Id,
                        Name = item.Name,
                        Image = HimallIO.GetImagePath(image),
                        SubCategories = GetSubCategories(allCategoies, item.Id, depth + 1),
                        Depth = 1,
                        DisplaySequence = item.DisplaySequence
                    };
                })
                   .OrderBy(c => c.DisplaySequence).ToList();
            return categories;
        }

        public static string GetEffectCategoryName(long shopId, long typeId)
        {
            return _iCategoryService.GetEffectCategoryName(shopId, typeId);
        }

        /// <summary>
        /// 获取所有主分类
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Entities.CategoryInfo> GetMainCategory()
        {
            return _iCategoryService.GetMainCategory();
        }

        /// <summary>
        /// 获取指定分类下面的子级分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static IEnumerable<Entities.CategoryInfo> GetCategoryByParentId(long id)
        {
            return _iCategoryService.GetCategoryByParentId(id);
        }

        /// <summary>
        /// 获取可以做为经营类目的子级分类
        /// </summary>
        /// <param name="id">父级Id</param>
        /// <returns></returns>
        public static List<Entities.CategoryInfo> GetValidBusinessCategoryByParentId(long id)
        {
            return _iCategoryService.GetValidBusinessCategoryByParentId(id);
        }

        /// <summary>
        /// 添加一个分类
        /// </summary>
        /// <param name="model"></param>
        public static void AddCategory(Entities.CategoryInfo model)
        {
            _iCategoryService.AddCategory(model);
        }

        /// <summary>
        /// 获取一个分类信息
        /// </summary>
        /// <param name="id">分类Id</param>
        /// <returns></returns>
        public static Entities.CategoryInfo GetCategory(long id)
        {
            return _iCategoryService.GetCategory(id);
        }

        /// <summary>
        /// 获取所有分类
        /// </summary>
        /// <returns></returns>
        public static List<Category> GetCategories()
        {
            return _iCategoryService.GetCategories().Map<List<Category>>();
        }

        /// <summary>
        /// 获取指定分类的顶级分类
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static Category GetTopCategory(List<Category> source, long id)
        {
            var category = source.FirstOrDefault(p => p.Id == id);
            if (category == null) return new Category();
            if (category.ParentCategoryId > 0) return GetTopCategory(source, category.ParentCategoryId);
            return category;
        }


        public static List<Category> GetCategoryPath(List<Category> source, long id)
        {
            var result = new List<Category>();
            var category = source.FirstOrDefault(p => p.Id == id);
            if (category == null) return result;
            if (category.ParentCategoryId > 0)
                result.AddRange(GetCategoryPath(source, category.ParentCategoryId));
            result.Add(category);
            return result;
        }

        /// <summary>
        /// 更新指定分类的名称
        /// </summary>
        /// <param name="id">分类Id</param>
        /// <param name="name">分类的名称</param>
        public static void UpdateCategoryName(long id, string name)
        {
            _iCategoryService.UpdateCategoryName(id, name);
        }
        /// <summary>
        /// 更新分类佣金比率
        /// </summary>
        /// <param name="id"></param>
        /// <param name="commis"></param>
        public static void UpdateCategoryCommis(long id, decimal commis)
        {
            _iCategoryService.UpdateCategoryCommis(id, commis);
        }
        /// <summary>
        /// 更新指定分类的显示顺序
        /// </summary>
        /// <param name="id">分类Id</param>
        /// <param name="displaySequence">分类的顺序</param>
        public static void UpdateCategoryDisplaySequence(long id, long displaySequence)
        {
            _iCategoryService.UpdateCategoryDisplaySequence(id, displaySequence);
        }

        /// <summary>
        /// 获取所有一二级节点分类
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Entities.CategoryInfo> GetFirstAndSecondLevelCategories()
        {
            return _iCategoryService.GetFirstAndSecondLevelCategories();
        }

        /// <summary>
        /// 获取指定一级分类下所有二三级节点分类
        /// </summary>
        /// <param name="ids">指定的一级分类</param>
        /// <returns></returns>
        public static IEnumerable<Entities.CategoryInfo> GetSecondAndThirdLevelCategories(params long[] ids)
        {
            return _iCategoryService.GetSecondAndThirdLevelCategories(ids);
        }

        /// <summary>
        /// 获取传入分类的所有一级分类
        /// </summary>
        /// <param name="categoryIds">传入分类的id</param>
        /// <returns></returns>
        public static IEnumerable<Entities.CategoryInfo> GetTopLevelCategories(IEnumerable<long> categoryIds)
        {
            return _iCategoryService.GetTopLevelCategories(categoryIds);
        }


        /// <summary>
        /// 获取数据库中Id最大的分类
        /// </summary>
        /// <returns></returns>
        public static long GetMaxCategoryId()
        {
            return _iCategoryService.GetMaxCategoryId();
        }
        /// <summary>
        /// 根据Category模型更新
        /// </summary>
        /// <param name="model"></param>
        public static void UpdateCategory(Entities.CategoryInfo model)
        {
            _iCategoryService.UpdateCategory(model);
        }
        /// <summary>
        /// 根据ID删除分类（递归删除子分类）
        /// </summary>
        /// <param name="id"></param>
        public static void DeleteCategory(long id)
        {
            _iCategoryService.DeleteCategory(id);
        }

        /// <summary>
        /// 获取一个分类信息
        /// </summary>
        /// <param name="id">分类Id</param>
        /// <returns></returns>
        public static List<Himall.Entities.CategoryInfo> GetCateogryListByIds(List<long> ids)
        {
            return _iCategoryService.GetCateogryListByIds(ids);
        }
    }
}
