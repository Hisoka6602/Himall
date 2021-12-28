using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.DTO;
using Himall.Service;
using Himall.SmallProgAPI.Model;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Web.Http.Results;

namespace Himall.SmallProgAPI
{
    /// <summary>
    /// 收货地址
    /// </summary>
    public class ShippingAddressController : BaseApiController
    {
        public JsonResult<Result<dynamic>> GetShippingAddressList(string openId, long? shopBranchId = 0, bool isCanDelive = false)
        {
            CheckUserLogin();
            var branchId = shopBranchId.HasValue ? shopBranchId.Value : 0;
            var shoppingAddress = OrderApplication.GetUserAddresses(CurrentUser.Id, branchId);

            var shippingAddressList = new List<ShippingAddress>();
            var shippingAddressListCanDelive = new List<ShippingAddress>();
            foreach (var item in shoppingAddress)
            {
                ShippingAddress shippingAddress = new ShippingAddress()
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
                    AddressDetail = item.AddressDetail ?? "",
                    UserId = item.UserId,
                    NeedUpdate = item.NeedUpdate
                };
                if (isCanDelive)
                {
                    if (item.NeedUpdate)
                    {
                        shippingAddressListCanDelive.Add(shippingAddress);
                    }
                    else
                    {
                        shippingAddressList.Add(shippingAddress);
                    }
                }
                else
                {
                    if (branchId == 0)
                    {
                        shippingAddressList.Add(shippingAddress);
                    }
                    else
                    {
                        if (item.CanDelive)
                            shippingAddressList.Add(shippingAddress);
                        else
                            shippingAddressListCanDelive.Add(shippingAddress);
                    }
                }
            }
            var addressList = new
            {
                CanDeliveAddressList = shippingAddressList,
                CanNotDeliveAddressList = shippingAddressListCanDelive,
                IsOpenStore = SiteSettingApplication.SiteSettings.IsOpenStore,
            };
            dynamic result = JsonResult<dynamic>(addressList);
            return result;
        }
        /// <summary>
        /// 获取收货地址列表
        /// </summary>
        /// <param name="openId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetList(string openId)
        {
            CheckUserLogin();
            var shoppingAddress = ServiceProvider.Instance<ShippingAddressService>.Create.GetByUser(CurrentUser.Id);

            var result = shoppingAddress.Where(address=>address.Latitude>0&&address.Longitude>0).ToList().Select(item => new
            {
                ShippingId = item.Id,
                ShipTo = item.ShipTo,
                CellPhone = item.Phone,
                FullRegionName = item.RegionFullName,
                Address = item.Address,
                RegionId = item.RegionId,
                FullRegionPath = item.RegionIdPath,
                IsDefault = item.IsDefault,
                LatLng = item.Latitude + "," + item.Longitude,
                FullAddress = item.RegionFullName + " " + item.Address + " " + item.AddressDetail,
                TelPhone = "",
                RegionLocation = "",
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                AddressDetail = item.AddressDetail ?? string.Empty,
                NeedUpdate = item.NeedUpdate
            });

            var model = new {
                ShoppingAddressList = result,
                IsOpenStore = SiteSettingApplication.SiteSettings.IsOpenStore,
            };

            return JsonResult<dynamic>(model);
        }
        /// <summary>
        /// 获取收货地址
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetShippingAddress(long shippingId)
        {
            CheckUserLogin();
            var shoppingAddress = ServiceProvider.Instance<ShippingAddressService>.Create.GetByUser(CurrentUser.Id);
            var shopaddressInfo = shoppingAddress.FirstOrDefault(e => e.Id == shippingId);
            if (shopaddressInfo != null)
            {
                dynamic model = new ExpandoObject();

                model.ShippingId = shopaddressInfo.Id;
                model.ShipTo = shopaddressInfo.ShipTo;
                model.CellPhone = shopaddressInfo.Phone;
                model.FullRegionName = shopaddressInfo.RegionFullName;
                model.Address = shopaddressInfo.Address;
                model.RegionId = shopaddressInfo.RegionId;
                model.FullRegionPath = shopaddressInfo.RegionIdPath;
                model.IsDefault = shopaddressInfo.IsDefault;
                model.LatLng = shopaddressInfo.Latitude + "," + shopaddressInfo.Longitude;
                model.FullAddress = shopaddressInfo.RegionFullName + " " + shopaddressInfo.Address;
                model.TelPhone = "";
                model.RegionLocation = "";
                model.UserId = CurrentUserId;
                model.Zipcode = "";
                model.AddressDetail = shopaddressInfo.AddressDetail;
                model.NeedUpdate = shopaddressInfo.NeedUpdate;
                return JsonResult<dynamic>(model);
            }
            else
            {
                return Json(ErrorResult<dynamic>("参数错误"));
            }

        }

        /// <summary>
        /// 新增收货地址
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public JsonResult<Result<long>> PostAddAddress(ShippingAddressOperaAddressPModel value)
        {
            CheckUserLogin();
            var address = new Entities.ShippingAddressInfo();
            address.UserId = CurrentUser.Id;
            address.RegionId = value.regionId;
            address.Address = value.address;
            address.Phone = value.cellphone;
            address.ShipTo = value.shipTo;
            address.Latitude = value.lat;
            address.Longitude = value.lng;
            address.IsDefault = value.isDefault;
            address.AddressDetail = value.addressDetail;
            try
            {
                ServiceProvider.Instance<ShippingAddressService>.Create.Create(address);
            }
            catch (Exception ex)
            {
                return Json(ErrorResult<long>(ex.Message));
            }
            return JsonResult<long>(address.Id);
        }
        /// <summary>
        /// 新增收货地址(微信地址)
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public JsonResult<Result<long>> PostAddWXAddress(ShippingAddressOperaAddressPModel value)
        {
            CheckUserLogin();
            try
            {
                if (string.IsNullOrWhiteSpace(value.address))
                {
                    throw new HimallException("请填写详细地址");
                }
                if (value.regionId <= 0 && (string.IsNullOrWhiteSpace(value.city) || string.IsNullOrWhiteSpace(value.county)))
                {
                    throw new HimallException("参数错误");
                }
                if (value.regionId <= 0)
                {
                    Region _region = null;
                    if (string.IsNullOrWhiteSpace(value.city))
                    {
                        _region = ServiceProvider.Instance<RegionService>.Create.GetRegionByName(value.county, Region.RegionLevel.County);
                    }
                    else
                    {
                        _region = ServiceProvider.Instance<RegionService>.Create.GetRegionByName(value.county, value.city, Region.RegionLevel.County);
                    }
                    if (_region != null)
                    {
                        value.regionId = _region.Id;
                    }
                }
                if (value.regionId <= 0)
                {
                    throw new HimallException("错误的地区信息");
                }
            }
            catch (Exception ex)
            {
                return Json(ErrorResult<long>(ex.Message));
            }
            var latlong = ShippingAddressApplication.GetLatLngByAddress(value.provinceName+" "+value.city+" "+value.county+" "+value.address);
            string []strlatlng=null;
            if (!string.IsNullOrEmpty(latlong)) {
                strlatlng = latlong.Split(',');
            }
            #region 如果存在相同地址就返回
            var shoppingAddress = ServiceProvider.Instance<ShippingAddressService>.Create.GetByUser(CurrentUser.Id);
            var _tmp = shoppingAddress.FirstOrDefault(d => d.RegionId == value.regionId && d.Address == value.address);
            if (_tmp != null)
            {
                if (_tmp.Latitude <= 0&&strlatlng!=null) {
                    _tmp.Latitude =decimal.Parse(strlatlng[1]);
                    _tmp.Longitude = decimal.Parse(strlatlng[0]);
                    ShippingAddressApplication.UpdateShippingAddress(_tmp);
                }
                return JsonResult(_tmp.Id);
            }
            #endregion
            if (strlatlng != null) {
                value.lat =decimal.Parse(strlatlng[1]);
                value.lng = decimal.Parse(strlatlng[0]);
            }

            Entities.ShippingAddressInfo shippingAddr = new Entities.ShippingAddressInfo();
            shippingAddr.UserId = CurrentUser.Id;
            shippingAddr.RegionId = value.regionId;
            shippingAddr.Address = value.address;
            shippingAddr.Phone = value.cellphone;
            shippingAddr.ShipTo = value.shipTo;
            shippingAddr.Latitude = value.lat;
            shippingAddr.Longitude = value.lng;
            shippingAddr.AddressDetail = value.addressDetail;
            try
            {
                ServiceProvider.Instance<ShippingAddressService>.Create.Create(shippingAddr);
                if (value.isDefault)
                {
                    ServiceProvider.Instance<ShippingAddressService>.Create.SetDefault(shippingAddr.Id, CurrentUserId);
                }
            }
            catch (Exception ex)
            {
                return Json(ErrorResult<long>(ex.Message));
            }
            return JsonResult(shippingAddr.Id);
        }
        /// <summary>
        /// 修改收货地址
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public JsonResult<Result<long>> PostUpdateAddress(ShippingAddressOperaAddressPModel value)
        {
            CheckUserLogin();
            Entities.ShippingAddressInfo shippingAddr = new Entities.ShippingAddressInfo();
            shippingAddr.UserId = CurrentUser.Id;
            shippingAddr.Id = value.shippingId;
            shippingAddr.RegionId = value.regionId;
            shippingAddr.Address = value.address;
            shippingAddr.Phone = value.cellphone;
            shippingAddr.ShipTo = value.shipTo;
            shippingAddr.Latitude = value.lat;
            shippingAddr.Longitude = value.lng;
            shippingAddr.AddressDetail = value.addressDetail;
            try
            {
                ServiceProvider.Instance<ShippingAddressService>.Create.Save(shippingAddr);
                if (value.isDefault)
                {
                    ServiceProvider.Instance<ShippingAddressService>.Create.SetDefault(shippingAddr.Id, CurrentUserId);
                }
            }
            catch (Exception ex)
            {
                return Json(ErrorResult<long>(ex.Message));
            }
            return JsonResult(shippingAddr.Id);
        }
        /// <summary>
        /// 设置默认地址
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetSetDefault(long shippingId)
        {
            CheckUserLogin();
            ServiceProvider.Instance<ShippingAddressService>.Create.SetDefault(shippingId, CurrentUserId);
            return JsonResult<int>(msg: "设置成功");
        }
        /// <summary>
        /// 删除收货地址
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public JsonResult<Result<int>> GetDeleteAddress(long shippingId)
        {
            CheckUserLogin();
            ServiceProvider.Instance<ShippingAddressService>.Create.Remove(shippingId, CurrentUser.Id);
            return JsonResult<int>(msg: "删除成功");
        }

        /// <summary>
        /// 根据搜索地址反向匹配出区域信息
        /// </summary>
        /// <param name="fromLatLng"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetRegion(string fromLatLng = "")
        {
            CheckUserLogin();
            string address = string.Empty, province = string.Empty, city = string.Empty, district = string.Empty, street = string.Empty, fullPath = string.Empty, newStreet = string.Empty;
            ShopbranchHelper.GetAddressByLatLng(fromLatLng, ref address, ref province, ref city, ref district, ref street);
            if (district == "" && street != "")
            {
                district = street;
                street = "";
            }
            fullPath = RegionApplication.GetAddress_Components(city, district, street, out newStreet);
            if (fullPath.Split(',').Length <= 3) newStreet = string.Empty;//如果无法匹配街道，则置为空
            return JsonResult<dynamic>(new { fullPath = fullPath, showCity = string.Format("{0} {1} {2} {3}", province, city, district, newStreet), street = newStreet });
        }

        /// <summary>
        /// 获取省市区信息
        /// </summary>
        /// <param name="parentRegionId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetAll(long parentRegionId, bool bAddAll = false)
        {
            var regions = RegionApplication.GetSubRegion(parentRegionId);
            var models = regions.Select(p => new
            {
                p.Id,
                p.Name,
                p.ShortName,
                p.Level
            }).ToList();
            if (bAddAll && models.Any(n => n.Level == CommonModel.Region.RegionLevel.Town))
            {
                models.Insert(0, new { Id = 0, Name = "其它", ShortName = "其它", Level = CommonModel.Region.RegionLevel.Town });
            }
            return JsonResult<dynamic>(regions);
        }

        public JsonResult<Result<dynamic>> GetSubRegion(int parent, bool bAddAll = false)
        {
            var regions = RegionApplication.GetSubRegion(parent);
            var models = regions.Select(p => new
            {
                p.Id,
                p.Name,
                p.ShortName,
                p.Level
            }).ToList();
            if (bAddAll && models.Any(n => n.Level == CommonModel.Region.RegionLevel.Town))
            {
                models.Insert(0, new { Id = 0, Name = "其它", ShortName = "其它", Level = CommonModel.Region.RegionLevel.Town });
            }
            return JsonResult<dynamic>(models);
        }

        /// <summary>
        /// 获取直属子级
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetSub(long parentId)
        {
            var region = RegionApplication.GetRegion(parentId);
            if (region == null)
            {
                return Json(ErrorResult<dynamic>(msg: "错误的参数：parentId"));
            }
            var models = region.Sub.Select(p => new
            {
                id = p.Id,
                name = p.Name,
            }).ToList();

            return JsonResult<dynamic>(new
            {
                Depth = region.Level.GetHashCode(),
                Regions = models
            });
        }

        public JsonResult<Result<dynamic>> GetShopGetAddress(long shopId, long shopBranchId = 0)
        {
            if (shopBranchId <= 0)
            {
                var data = ShopShippersApplication.GetDefaultGetGoodsShipper(shopId);
                if (data == null)
                {
                    data = new DTO.ShopShipper() { };
                }
                else
                {
                    data.RegionStr = RegionApplication.GetFullName(data.RegionId);
                }
                var model = new
                {
                    Region = data.RegionStr,
                    Address = data.Address,
                    Phone = data.TelPhone,
                    ShipperName = data.ShipperName
                };
                return JsonResult<dynamic>(model);
            }
            else
            {
                var data = ShopBranchApplication.GetShopBranchById(shopBranchId);
                string redionstr = "";
                if (data != null)
                {
                    redionstr = RegionApplication.GetFullName(data.AddressId);
                }
                var model = new
                {
                    Region = redionstr,
                    Address = data.AddressDetail,
                    Phone = data.ContactPhone,
                    ShipperName = data.ContactUser
                };
                return JsonResult<dynamic>(model);
            }
        }
        
        /// <summary>
        /// 通过地址获取坐标
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public JsonResult<Result<dynamic>> GetLatLngByAddress(string address)
        {
            var latlng = ShippingAddressApplication.GetLatLngByAddress(address);
            Log.Info("GetLatLngByAddress:" + latlng);
            var arr = latlng.Split(',');
            var lat = "";
            var lng = "";
            if (arr.Length > 1) {
                lat = arr[1];
                lng = arr[0];
            }
            var model = new
            {
                lat = lat,
                lng = lng,
            };
            return JsonResult<dynamic>(model);
        }
    }
}