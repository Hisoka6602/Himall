﻿using Himall.CommonModel;
using Himall.Core;
using Himall.DTO.QueryModel;
using Himall.Service;
using System.Collections.Generic;

namespace Himall.Application
{
    public class ManagerApplication
    {
        private static ManagerService _ManagerService = ObjectContainer.Current.Resolve<ManagerService>();
        /// <summary>
        /// 更新店铺状态
        /// </summary>
        public static void UpdateShopStatus()
        {
            _ManagerService.UpdateShopStatus();
        }

        /// <summary>
        /// 根据查询条件分页获取平台管理员信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<Entities.ManagerInfo> GetPlatformManagers(ManagerQuery query)
        {
            return _ManagerService.GetPlatformManagers(query);
        }

        /// <summary>
        /// 根据查询条件分页获取商家管理员信息
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static QueryPageModel<Entities.ManagerInfo> GetSellerManagers(ManagerQuery query)
        {
            return _ManagerService.GetSellerManagers(query);
        }

        /// <summary>
        /// 根据角色ID获取平台角色下的管理员
        /// </summary>
        /// <param name="roleid"></param>
        /// <returns></returns>
        public static List<Entities.ManagerInfo> GetPlatformManagerByRoleId(long roleId)
        {
            return _ManagerService.GetPlatformManagerByRoleId(roleId);
        }
        /// <summary>
        /// 根据ShopId获取对应管理信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static Entities.ManagerInfo GetSellerManagerByShopId(long shopId)
        {
            return _ManagerService.GetSellerManagerByShopId(shopId);
        }

        /// <summary>
        /// 获取当前登录的平台管理员
        /// </summary>
        /// <returns></returns>
        public static Entities.ManagerInfo GetPlatformManager(long userId)
        {
            return _ManagerService.GetPlatformManager(userId);
        }

        /// <summary>
        /// 根据角色ID获取商家角色下的管理员
        /// </summary>
        /// <param name="roleId"></param>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static List<Entities.ManagerInfo> GetSellerManagerByRoleId(long roleId, long shopId)
        {
            return _ManagerService.GetSellerManagerByRoleId(roleId, shopId);
        }

        /// <summary>
        /// 获取当前登录的商家管理员
        /// </summary>
        /// <returns></returns>
        public static DTO.Manager GetSellerManager(long userId)
        {
            return _ManagerService.GetSellerManager(userId).Map<DTO.Manager>();
        }
        /// <summary>
        /// 添加一个平台管理员
        /// </summary>
        /// <param name="model"></param>
        public static void AddPlatformManager(Entities.ManagerInfo model)
        {
            _ManagerService.AddPlatformManager(model);
        }
        /// <summary>
        /// 添加一个商家管理员
        /// </summary>
        /// <param name="password">密码</param>
        /// <param name="username">用户名</param>
        public static Entities.ManagerInfo AddSellerManager(string username, string password, string salt)
        {
            return _ManagerService.AddSellerManager(username, password, salt);
        }

        /// <summary>
        /// 添加商家子帐号（不创建店铺）
        /// </summary>
        /// <param name="model"></param>
        ///<param name="currentSellerName">当前商家帐号用户名</param>

        public static void AddSellerManager(Entities.ManagerInfo model, string currentSellerName)
        {
            _ManagerService.AddSellerManager(model, currentSellerName);
        }
        /// <summary>
        /// 修改平台管理员密码和角色
        /// </summary>
        /// <param name="model"></param>
        public static void ChangePlatformManagerPassword(long id, string password, long roleId)
        {
            _ManagerService.ChangePlatformManagerPassword(id, password, roleId);
        }

        /// <summary>
        /// 修改商家管理员密码
        /// </summary>
        /// <param name="model"></param>
        public static void ChangeSellerManagerPassword(long id, long shopId, string password, long roleId)
        {
            _ManagerService.ChangeSellerManagerPassword(id, shopId, password, roleId);
        }


        /// <summary>
        /// 修改商家管理员
        /// </summary>
        /// <param name="model"></param>
        public static void ChangeSellerManager(Entities.ManagerInfo info)
        {
            _ManagerService.ChangeSellerManager(info);
        }

        /// <summary>
        /// 删除平台管理员
        /// </summary>
        /// <param name="id"></param>
        public static void DeletePlatformManager(long id)
        {
            _ManagerService.DeletePlatformManager(id);
        }


        /// <summary>
        /// 删除商家管理员
        /// </summary>
        /// <param name="id">管理员ID</param>
        /// <param name="shopId">店铺ID</param>
        public static void DeleteSellerManager(long id, long shopId)
        {
            _ManagerService.DeleteSellerManager(id, shopId);
        }

        /// <summary>
        /// 批量删除平台管理员
        /// </summary>
        /// <param name="ids"></param>
        public static void BatchDeletePlatformManager(long[] ids)
        {
            _ManagerService.BatchDeletePlatformManager(ids);
        }


        /// <summary>
        /// 批量删除商家管理员
        /// </summary>
        /// <param name="ids"></param>
        public static void BatchDeleteSellerManager(long[] ids, long shopId)
        {
            _ManagerService.BatchDeleteSellerManager(ids, shopId);
        }

        /// <summary>
        /// 是否存在相同的用户名
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static bool CheckUserNameExist(string userName, bool isPlatFormManager = false)
        {
            return _ManagerService.CheckUserNameExist(userName, isPlatFormManager);
        }


        /// <summary>
        /// 获取查询的管理员列表
        /// </summary>
        /// <param name="keyWords">关键字</param>
        /// <returns></returns>
        public static List<Entities.ManagerInfo> GetManagers(string keyWords)
        {
            return _ManagerService.GetManagers(keyWords);
        }

        /// <summary>
        /// 获取查询的管理员列表
        /// </summary>
        /// <param name="keyWords">关键字</param>
        /// <returns></returns>
        public static List<Entities.ManagerInfo> GetManagers(List<string> usernames)
        {
            return _ManagerService.GetManagers(usernames);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="username">用户名</param>
        /// <param name="password">密码（明文）</param>
        /// <param name="isPlatFormManager">是否为平台管理员</param>
        /// <returns></returns>
        public static DTO.Manager Login(string username, string password, bool isPlatFormManager = false)
        {
            return _ManagerService.Login(username, password, isPlatFormManager).Map<DTO.Manager>();
        }

        /// <summary>
        /// 根据商家名称获取商家信息
        /// </summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public static Entities.ManagerInfo GetSellerManager(string userName)
        {
            return _ManagerService.GetSellerManager(userName);
        }
    }
}
