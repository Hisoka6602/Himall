using Himall.API.Model.ParamsModel;
using Himall.Application;
using Himall.Core;
using Himall.Entities;
using Himall.Service;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Himall.API
{
    public class BranchShippingAddressController : BaseApiController
    {
        public object GetShippingAddressList(long shopBranchId)
        {
            if (shopBranchId == 0)
                throw new HimallException("获取门店ID失败");
            CheckUserLogin();
            var shoppingAddress = ServiceProvider.Instance<ShippingAddressService>.Create.GetByUser(CurrentUser.Id);

            var shippingAddressList = new List<ShippingAddressInfo>();
            var shopBranchInfo = ServiceProvider.Instance<ShopBranchService>.Create.GetShopBranchById(shopBranchId);
            if (shopBranchInfo != null && shopBranchInfo.IsStoreDelive)
            {
                foreach (var item in shoppingAddress)
                {
                    if (shopBranchInfo.ServeRadius>0)
                    {
                        string form = string.Format("{0},{1}", item.Latitude, item.Longitude);//收货地址的经纬度
                        if (form.Length > 1)//地址不含经纬度的不可配送
                        {
                            double Distances = ServiceProvider.Instance<ShopBranchService>.Create.GetLatLngDistancesFromAPI(form, string.Format("{0},{1}", shopBranchInfo.Latitude, shopBranchInfo.Longitude));
                            if (Distances < shopBranchInfo.ServeRadius && Distances != 0)//距离超过配送距离的不可配送,距离计算失败不可配送
                            {
                                item.CanDelive = true;
                            }
                        }
                    }
                    ShippingAddressInfo shippingAddress = new ShippingAddressInfo()
                    {
                        Id = item.Id,
                        ShipTo = item.ShipTo,
                        Phone = item.Phone,
                        RegionFullName = item.RegionFullName,
                        Address = item.Address,
                        RegionId = item.RegionId,
                        RegionIdPath = item.RegionIdPath,
                        IsDefault = item.IsDefault,
                        Latitude = item.Latitude,
                        Longitude = item.Longitude,
                        CanDelive = item.CanDelive
                    };
                    shippingAddressList.Add(shippingAddress);
                }
            }
            else
                throw new HimallException("门店ID错误,或不支持配送");

            return new { success = true, ShippingAddress = shippingAddressList };
        }
        public object GetShippingAddress(long id, long shopBranchId)
        {
            if (shopBranchId == 0)
                throw new HimallException("获取门店ID失败");
            CheckUserLogin();
            var shoppingAddress = ServiceProvider.Instance<ShippingAddressService>.Create.GetByUser(CurrentUser.Id);
            var shopaddressInfo = shoppingAddress.FirstOrDefault(e => e.Id == id);
            var shopBranchInfo = ServiceProvider.Instance<ShopBranchService>.Create.GetShopBranchById(shopBranchId);
            if (shopaddressInfo != null && shopBranchInfo != null && shopBranchInfo.IsStoreDelive)
            {
                if (shopBranchInfo.ServeRadius>0)
                {
                    string form = string.Format("{0},{1}", shopaddressInfo.Latitude, shopaddressInfo.Longitude);//收货地址的经纬度
                    if (form.Length > 1)//地址不含经纬度的不可配送
                    {
                        double Distances = ServiceProvider.Instance<ShopBranchService>.Create.GetLatLngDistancesFromAPI(form, string.Format("{0},{1}", shopBranchInfo.Latitude, shopBranchInfo.Longitude));
                        if (Distances < shopBranchInfo.ServeRadius && Distances != 0)//距离超过配送距离的不可配送,距离计算失败不可配送
                        {
                            shopaddressInfo.CanDelive = true;
                        }
                    }
                }
                var model = new ShippingAddressInfo()
                {
                    Id = shopaddressInfo.Id,
                    ShipTo = shopaddressInfo.ShipTo,
                    Phone = shopaddressInfo.Phone,
                    RegionFullName = shopaddressInfo.RegionFullName,
                    Address = shopaddressInfo.Address,
                    RegionId = shopaddressInfo.RegionId,
                    RegionIdPath = shopaddressInfo.RegionIdPath,
                    Latitude = shopaddressInfo.Latitude,
                    Longitude = shopaddressInfo.Longitude,
                    CanDelive = shopaddressInfo.CanDelive
                };
                return new { success = true, ShippingAddress = model };
            }
            else
            {
                return new { success = true, ShippingAddress = new ShippingAddressInfo() };
            }

        }
        //新增收货地址
        public object PostAddShippingAddress(ShippingAddressAddModel value)
        {
            if ( value.shopbranchid == 0)
                throw new HimallException("获取门店ID失败");
            long shopBranchId = value.shopbranchid;
            CheckUserLogin();
            Entities.ShippingAddressInfo shippingAddr = new Entities.ShippingAddressInfo();
            shippingAddr.UserId = CurrentUser.Id;
            shippingAddr.RegionId = value.regionId;
            shippingAddr.Address = value.address;
            shippingAddr.Phone = value.phone;
            shippingAddr.ShipTo = value.shipTo;
            shippingAddr.Latitude = value.latitude;
            shippingAddr.Longitude = value.longitude;

            var shopBranchInfo = ServiceProvider.Instance<ShopBranchService>.Create.GetShopBranchById(shopBranchId);
            if (shopBranchInfo != null && shopBranchInfo.IsStoreDelive)
            {
                if (shopBranchInfo.ServeRadius>0)
                {
                    string form = string.Format("{0},{1}", shippingAddr.Latitude, shippingAddr.Longitude);//收货地址的经纬度
                    if (form.Length <= 1)//地址不含经纬度的不可配送
                        throw new HimallApiException("地址经纬度获取失败");

                    double Distances = ServiceProvider.Instance<ShopBranchService>.Create.GetLatLngDistancesFromAPI(form, string.Format("{0},{1}", shopBranchInfo.Latitude, shopBranchInfo.Longitude));
                    if (Distances > shopBranchInfo.ServeRadius)//距离超过配送距离的不可配送,距离计算失败不可配送
                        throw new HimallApiException("距离超过门店配送距离的不可配送");
                }
            }
            else
                return ErrorResult("门店不提供配送服务");
            try
            {
                ServiceProvider.Instance<ShippingAddressService>.Create.Create(shippingAddr);
            }
            catch (Exception ex)
            {
                return ErrorResult(ex.Message);
            }
            return SuccessResult();
        }
        //删除收货地址
        public object PostDeleteShippingAddress(ShippingAddressDeleteModel value)
        {
            CheckUserLogin();
            ServiceProvider.Instance<ShippingAddressService>.Create.Remove(value.id, CurrentUser.Id);
            return SuccessResult();
        }
        //编辑收货地址
        public object PostEditShippingAddress(ShippingAddressEditModel value)
        {
            if (value.shopbranchid == 0)
                throw new HimallException("获取门店ID失败");
            long shopBranchId = value.shopbranchid;
            CheckUserLogin();
            Entities.ShippingAddressInfo shippingAddr = new Entities.ShippingAddressInfo();
            shippingAddr.UserId = CurrentUser.Id;
            shippingAddr.Id = value.id;
            shippingAddr.RegionId = value.regionId;
            shippingAddr.Address = value.address;
            shippingAddr.Phone = value.phone;
            shippingAddr.ShipTo = value.shipTo;
            shippingAddr.Longitude = value.longitude;
            shippingAddr.Latitude = value.latitude;

            var shopBranchInfo = ServiceProvider.Instance<ShopBranchService>.Create.GetShopBranchById(shopBranchId);
            if (shopBranchInfo != null && shopBranchInfo.IsStoreDelive)
            {
                if (shopBranchInfo.ServeRadius>0)
                {
                    string form = string.Format("{0},{1}", shippingAddr.Latitude, shippingAddr.Longitude);//收货地址的经纬度
                    if (form.Length <= 1)//地址不含经纬度的不可配送
                        throw new HimallApiException("收货地址经纬度获取失败");

                    double Distances = ServiceProvider.Instance<ShopBranchService>.Create.GetLatLngDistancesFromAPI(form, string.Format("{0},{1}", shopBranchInfo.Latitude, shopBranchInfo.Longitude));
                    if (Distances > shopBranchInfo.ServeRadius)//距离超过配送距离的不可配送,距离计算失败不可配送
                        throw new HimallApiException("距离超过门店配送距离的不可配送");
                }
            }
            else
                throw new HimallApiException("门店不提供配送服务");
            try
            {
                ServiceProvider.Instance<ShippingAddressService>.Create.Save(shippingAddr);
            }
            catch (Exception ex)
            {
                throw new HimallApiException(ex.Message);
            }
            return SuccessResult();
        }
        //设为默认收货地址
        public object PostSetDefaultAddress(ShippingAddressSetDefaultModel value)
        {
            CheckUserLogin();
            long addId = value.addId;
            ServiceProvider.Instance<ShippingAddressService>.Create.SetDefault(addId, CurrentUser.Id);
            return SuccessResult();
        }

        /// <summary>
        /// 根据搜索地址反向匹配出区域信息
        /// </summary>
        /// <param name="fromLatLng"></param>
        /// <returns></returns>
        public object GetRegion(string fromLatLng = "")
        {
            string address = string.Empty, province = string.Empty, city = string.Empty, district = string.Empty, street = string.Empty, fullPath = string.Empty, newStreet = string.Empty;
            ShopbranchHelper.GetAddressByLatLng(fromLatLng, ref address, ref province, ref city, ref district, ref street);
            if (district == "" && street != "")
            {
                district = street;
                street = "";
            }
            fullPath = RegionApplication.GetAddress_Components(city, district, street, out newStreet);
            if (fullPath.Split(',').Length <= 3) newStreet = string.Empty;//如果无法匹配街道，则置为空
            return new { success = true, fullPath = fullPath, showCity = string.Format("{0} {1} {2}", province, city, district), street = newStreet };
        }
    }
}
