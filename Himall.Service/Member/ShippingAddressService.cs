using Himall.Core;
using Himall.Service;
using Himall.Entities;
using Himall.CommonModel;
using NetRube.Data;
using System.Collections.Generic;

namespace Himall.Service
{
    public class ShippingAddressService : ServiceBase
    {
        public void Create(ShippingAddressInfo address)
        {
            address.IsQuick = false;
            if (DbFactory.Default.Get<ShippingAddressInfo>().Where(a => a.UserId == address.UserId).Count() >= 20)
            {
                throw new HimallException("收货地址最多只能创建20个！");
            }
            DbFactory.Default.Add(address);
            if (address.IsDefault)
                SetDefault(address.Id, address.UserId);
            CacheManager.ClearShippingAddress(address.UserId);
        }

        public void SetDefault(long id, long userId)
        {
            DbFactory.Default.Set<ShippingAddressInfo>()
                .Where(p => p.UserId == userId)
                .Set(p => p.IsDefault, false)
                .Execute();
            DbFactory.Default.Set<ShippingAddressInfo>()
                .Where(p => p.Id == id)
                .Set(p => p.IsDefault, true)
                .Execute();
            CacheManager.ClearShippingAddress(userId);
        }


        public void SetQuick(long id, long userId)
        {
            DbFactory.Default.Set<ShippingAddressInfo>()
                .Where(p => p.UserId == userId)
                .Set(p => p.IsQuick, false)
                .Execute();
            DbFactory.Default.Set<ShippingAddressInfo>()
                .Where(p => p.Id == id)
                .Set(p => p.IsQuick, true)
                .Execute();
            CacheManager.ClearShippingAddress(userId);
        }


        public void Save(ShippingAddressInfo address)
        {
            var model = DbFactory.Default.Get<ShippingAddressInfo>().Where(a => a.Id == address.Id && a.UserId == address.UserId).FirstOrDefault();
            if (model == null)
            {
                throw new Himall.Core.HimallException("该收货地址不存在或已被删除！");
            }
            model.Phone = address.Phone;
            model.RegionId = address.RegionId;
            model.ShipTo = address.ShipTo;
            model.Address = address.Address;
            model.Latitude = address.Latitude;
            model.Longitude = address.Longitude;
            model.AddressDetail = address.AddressDetail;
            DbFactory.Default.Update(model);
            Cache.Remove(CacheKeyCollection.CACHE_SHIPADDRESS(address.Id));
            CacheManager.ClearShippingAddress(address.UserId);
        }

        public void Remove(long id, long userId)
        {
            var model = DbFactory.Default.Get<ShippingAddressInfo>().Where(a => a.Id == id && a.UserId == userId).FirstOrDefault();
            if (model == null)
            {
                throw new Himall.Core.HimallException("该收货地址不存在或已被删除！");
            }
            bool isDefault = model.IsDefault;
            DbFactory.Default.Del(model);

            if (isDefault)
            {
                var newModel = DbFactory.Default.Get<ShippingAddressInfo>().FirstOrDefault();
                if (newModel != null)
                {
                    DbFactory.Default.Set<ShippingAddressInfo>().Set(n => n.IsDefault, true).Where(p => p.Id == newModel.Id).Succeed();
                }
            }
            CacheManager.ClearShippingAddress(userId);
        }

        public List<ShippingAddressInfo> GetByUser(long userId)
        {
            var regionService = ServiceProvider.Instance<RegionService>.Create;
            var siteSettingService = Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings;
            var model = DbFactory.Default.Get<ShippingAddressInfo>().Where(a => a.UserId == userId).OrderByDescending(a => a.Id).ToList();
            foreach (var m in model)
            {
                m.RegionFullName = regionService.GetFullName(m.RegionId);
                m.RegionIdPath = regionService.GetRegionPath(m.RegionId);
                m.NeedUpdate = (m.Latitude == 0 || m.Longitude == 0) && siteSettingService.IsOpenStore;
            }
            return model;
        }


        public ShippingAddressInfo Get(long id)
        {
            string cacheKey = CacheKeyCollection.CACHE_SHIPADDRESS(id);
            if (Cache.Exists(cacheKey))
                return Cache.Get<ShippingAddressInfo>(cacheKey);
            var regionService = ServiceProvider.Instance<RegionService>.Create;
            var address = DbFactory.Default.Get<ShippingAddressInfo>().Where(p => p.Id == id).FirstOrDefault();
            if (address == null)
            {
                throw new HimallException("错误的收货地址！");
            }
            address.RegionFullName = regionService.GetFullName(address.RegionId);
            address.RegionIdPath = regionService.GetRegionPath(address.RegionId);
            address.NeedUpdate = (address.Latitude == 0 || address.Longitude == 0) && Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenStore;
            Cache.Insert(cacheKey, address, 1800);
            return address;
        }


        public ShippingAddressInfo GetDefault(long userId)
        {
            //优先选择默认地址
            ShippingAddressInfo address = DbFactory.Default.Get<ShippingAddressInfo>().Where(item => item.UserId == userId && item.IsDefault==true).FirstOrDefault();

            //默认地址不存在时，选择最后一个添加的地址
            if (address == null)
                address = DbFactory.Default.Get<ShippingAddressInfo>().Where(item => item.UserId == userId).OrderByDescending(item => item.Id).FirstOrDefault();

            if (address != null)
            {
                var regionService = ServiceProvider.Instance<RegionService>.Create;

                address.RegionFullName = regionService.GetFullName(address.RegionId);
                address.RegionIdPath = regionService.GetRegionPath(address.RegionId);
                address.NeedUpdate = (address.Latitude == 0 || address.Longitude == 0) && Himall.ServiceProvider.Instance<SiteSettingService>.Create.SiteSettings.IsOpenStore;
            }
            return address;
        }
    }
}
