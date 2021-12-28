using Himall.Core;
using Himall.Service;
using Himall.Entities;
using System.Collections.Generic;
using NetRube.Data;
using Himall.DTO;
using System.Linq;

namespace Himall.Service
{
    public class ShopShippersService : ServiceBase
    {

        public List<ShopShipper> GetShippers(long shopId) =>
            CacheManager.GetOrCreate($"shipper:{shopId}", () => DbFactory.Default.Get<ShopShipperInfo>(p => p.ShopId == shopId).ToList<ShopShipper>());

        /// <summary>
        /// 获得默认发货地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public ShopShipper GetDefaultSendGoodsShipper(long shopId) =>
            GetShippers(shopId).FirstOrDefault(p => p.IsDefaultSendGoods);

        /// <summary>
        /// 获得默认收货地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public ShopShipper GetDefaultGetGoodsShipper(long shopId) =>
            GetShippers(shopId).FirstOrDefault(p => p.IsDefaultGetGoods);

        /// <summary>
        /// 获得默认核销地址信息
        /// </summary>
        public ShopShipper GetDefaultVerificationShipper(long shopId) =>
            GetShippers(shopId).FirstOrDefault(p => p.IsDefaultVerification);
        
        /// <summary>
        /// 设置默认发货地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="id"></param>
        public void SetDefaultSendGoodsShipper(long shopId, long id)
        {
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Set<ShopShipperInfo>().Set(n => n.IsDefaultSendGoods, false).Where(d => d.ShopId == shopId && d.IsDefaultSendGoods == true).Succeed();
                DbFactory.Default.Set<ShopShipperInfo>().Set(n => n.IsDefaultSendGoods, true).Where(p => p.Id == id && p.ShopId == shopId).Succeed();
                CacheManager.Clear($"shipper:{shopId}");
            });
          
        }
        /// <summary>
        /// 设置默认收货地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="id"></param>
        public void SetDefaultGetGoodsShipper(long shopId, long id)
        {
            DbFactory.Default.InTransaction(() =>
            {
                var flag = DbFactory.Default.Set<ShopShipperInfo>().Set(n => n.IsDefaultGetGoods, false).Where(p => p.IsDefaultGetGoods == true && p.ShopId == shopId).Succeed();
                DbFactory.Default.Set<ShopShipperInfo>().Set(n => n.IsDefaultGetGoods, true).Where(p => p.Id == id && p.ShopId == shopId).Succeed();
                CacheManager.Clear($"shipper:{shopId}");
            });
        }
        /// <summary>
        /// 设置默认核销地址信息
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="id"></param>
        public void SetDefaultVerificationShipper(long shopId, long id)
        {
            DbFactory.Default.InTransaction(() =>
            {
                DbFactory.Default.Set<ShopShipperInfo>().Set(n => n.IsDefaultVerification, false).Where(p => p.IsDefaultVerification == true && p.ShopId == shopId).Succeed();
                DbFactory.Default.Set<ShopShipperInfo>().Set(n => n.IsDefaultVerification, true).Where(p => p.Id == id && p.ShopId == shopId).Succeed();
                CacheManager.Clear($"shipper:{shopId}");
            });
        }

        /// <summary>
        /// 获取所有发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public List<ShopShipperInfo> GetShopShippers(long shopId)
        {
            return DbFactory.Default.Get<ShopShipperInfo>().Where(d => d.ShopId == shopId).ToList();
        }

        /// <summary>
        /// 添加发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="data"></param>
        public void Add(long shopId, ShopShipperInfo data)
        {
            if (!DbFactory.Default.Get<ShopShipperInfo>().Where(d => d.ShopId == shopId).Exist())
            {
                data.IsDefaultGetGoods = true;
                data.IsDefaultSendGoods = true;
                data.IsDefaultVerification = true;
            }
            data.ShopId = shopId;
            DbFactory.Default.Add(data);
            CacheManager.Clear($"shipper:{shopId}");
        }

        /// <summary>
        /// 修改发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="data"></param>
        public void Update(long shopId, ShopShipperInfo data)
        {
            var model = DbFactory.Default.Get<ShopShipperInfo>().Where(d => d.Id == data.Id && d.ShopId == shopId).FirstOrDefault();
            if (model == null)
                throw new HimallException("错误的参数");
            model.ShipperTag = data.ShipperTag;
            model.ShipperName = data.ShipperName;
            model.TelPhone = data.TelPhone;
            model.IsDefaultGetGoods = data.IsDefaultGetGoods;
            model.IsDefaultSendGoods = data.IsDefaultSendGoods;
            model.Latitude = data.Latitude;
            model.Longitude = data.Longitude;
            model.RegionId = data.RegionId;
            model.Address = data.Address;
            model.ShopId = shopId;
            model.WxOpenId = data.WxOpenId;
            model.Zipcode = data.Zipcode;
            DbFactory.Default.Update(model);
            CacheManager.Clear($"shipper:{shopId}");
        }

        /// <summary>
        /// 删除发收货地址
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="id"></param>
        public void Delete(long shopId, long id)
        {
            var model = DbFactory.Default.Get<ShopShipperInfo>().Where(d => d.Id == id && d.ShopId == shopId).FirstOrDefault();
            if (model == null)
            {
                throw new HimallException("错误的参数");
            }
            if (model.IsDefaultGetGoods || model.IsDefaultSendGoods)
            {
                throw new HimallException("不能删除默认的发货/收货信息");
            }
            DbFactory.Default.Del(model);
            CacheManager.Clear($"shipper:{shopId}");
        }

        public ShopShipper GetShopShipper(long shopId, long id) =>
            GetShippers(shopId).FirstOrDefault(p => p.Id == id);
    }
}
