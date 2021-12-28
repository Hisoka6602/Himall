using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.Entities;
using System.Collections.Generic;

namespace Himall.Application
{
    public class ShopShippersApplication
    {
        static ShopShippersService _iShopShippersServiceService = ObjectContainer.Current.Resolve<ShopShippersService>();

        /// <summary>
        /// 获得默认发货地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static ShopShipper GetDefaultSendGoodsShipper(long shopId) =>
            _iShopShippersServiceService.GetDefaultSendGoodsShipper(shopId);
        /// <summary>
        /// 获得默认收货地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static ShopShipper GetDefaultGetGoodsShipper(long shopId) =>
            _iShopShippersServiceService.GetDefaultGetGoodsShipper(shopId);
        /// <summary>
        /// 获得默认核销地址信息
        /// </summary>
        /// <param name="shopId"></param>
        public static ShopShipper GetDefaultVerificationShipper(long shopId) =>
            _iShopShippersServiceService.GetDefaultVerificationShipper(shopId);
        /// <summary>
        /// 设置默认发货地址信息
        /// </summary>
        public static void SetDefaultSendGoodsShipper(long shopId, long id) =>
            _iShopShippersServiceService.SetDefaultSendGoodsShipper(shopId, id);
        /// <summary>
        /// 设置默认收货地址信息
        /// </summary>
        public static void SetDefaultGetGoodsShipper(long shopId, long id) =>
            _iShopShippersServiceService.SetDefaultGetGoodsShipper(shopId, id);
        /// <summary>
        /// 设置默认核销地址信息
        /// </summary>
        public static void SetDefaultVerificationShipper(long shopId, long id) =>
            _iShopShippersServiceService.SetDefaultVerificationShipper(shopId, id);
        /// <summary>
        /// 获取发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static ShopShipper GetShopShipper(long shopId, long id) =>
            _iShopShippersServiceService.GetShopShipper(shopId, id);
        /// <summary>
        /// 获取所有发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public static List<ShopShipper> GetShopShippers(long shopId) =>
            _iShopShippersServiceService.GetShippers(shopId);
        /// <summary>
        /// 添加发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="data"></param>
        public static void Add(long shopId, ShopShipper data)
        {
            var _d = AutoMapper.Mapper.Map<ShopShipperInfo>(data);
            _iShopShippersServiceService.Add(shopId, _d);
        }
        /// <summary>
        /// 修改发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="data"></param>
        public static void Update(long shopId, ShopShipper data)
        {
            var _d = AutoMapper.Mapper.Map<ShopShipperInfo>(data);
            _iShopShippersServiceService.Update(shopId, _d);
        }
        /// <summary>
        /// 删除发收货地址
        /// </summary>
        public static void Delete(long shopId, long id) =>
            _iShopShippersServiceService.Delete(shopId, id);
    }
}
