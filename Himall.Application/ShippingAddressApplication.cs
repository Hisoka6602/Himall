using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.Core;
using Himall.Service;
using Himall.Entities;
using Himall.DTO;
using Himall.DTO.CacheData;

namespace Himall.Application
{
    public class ShippingAddressApplication
    {
        private static ShippingAddressService _ShippingAddressService = ObjectContainer.Current.Resolve<ShippingAddressService>();

        /// <summary>
        /// 添加收货地址
        /// </summary>
        public static void AddShippingAddress(ShippingAddressInfo shipinfo)
        {
            _ShippingAddressService.Create(shipinfo);
        }

        /// <summary>
        /// 更新收货地址信息
        /// </summary>
        /// <param name="shipinfo"></param>
        public static void UpdateShippingAddress(ShippingAddressInfo shipinfo)
        {
            _ShippingAddressService.Save(shipinfo);
        }

        /// <summary>
        /// 获取用户的收货地址列表
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public static List<ShippingAddressInfo> GetUserShippingAddressByUserId(long userId)
        {
            return _ShippingAddressService.GetByUser(userId);
        }

        /// <summary>
        /// 获取会员默认收货地址
        /// </summary>
        /// <param name="userId">会员编号</param>
        /// <returns></returns>
        public static ShippingAddressInfo GetDefaultUserShippingAddressByUserId(long userId)
        {
            var addr = _ShippingAddressService.GetDefault(userId);
            if (addr != null)
            {
                var region = RegionApplication.GetRegion(addr.RegionId);
                if (region == null)
                {//收货地址被删除后，设置默认地址
                    addr.RegionId = RegionApplication.GetDefaultRegionId();
                    addr.RegionFullName = RegionApplication.GetFullName(addr.RegionId);
                }
            }
            return addr;
        }

        public static List<ShippingAddressData> GetAddress(long memberId)
        {
            return CacheManager.GetShippingAddress(memberId, () =>
            {
                var list = GetUserShippingAddressByUserId(memberId);
                return list.Map<List<ShippingAddressData>>();
            });
        }

        /// <summary>
        /// 获取用户的收货地址列表
        /// </summary>
        /// <param name="shippingAddressId">收货地址Id</param>
        /// <returns></returns>
        public static ShippingAddressInfo GetUserShippingAddress(long shippingAddressId)
        {
            return _ShippingAddressService.Get(shippingAddressId);
        }

        /// <summary>
        /// 设置用户的默认收获地址
        /// </summary>
        /// <param name="id"></param>
        public static void SetDefaultShippingAddress(long id, long userId)
        {
            _ShippingAddressService.SetDefault(id, userId);
        }

        /// <summary>
        /// 设置用户的收货地址为轻松购
        /// </summary>
        /// <param name="id"></param>
        public static void SetQuickShippingAddress(long id, long userId)
        {
            _ShippingAddressService.SetQuick(id, userId);
        }

        /// <summary>
        /// 删除用户的收货地址
        /// </summary>
        /// <param name="id"></param>
        /// <param name="userId"></param>
        public static void DeleteShippingAddress(long id, long userId)
        {
            _ShippingAddressService.Remove(id, userId);
        }

        /// <summary>
        /// 通过地址获取坐标
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string GetLatLngByAddress(string address)
        {
            string latLng = "";
            SiteSettings setting = SiteSettingApplication.SiteSettings;
            string qqMapApiKey = setting.QQMapAPIKey;
            if (string.IsNullOrEmpty(qqMapApiKey))
            {
                qqMapApiKey = "SYJBZ-DSLR3-IWX3Q-3XNTM-ELURH-23FTP";
            }
            string url = "https://apis.map.qq.com/ws/geocoder/v1/?address=" + address + "&key=" + qqMapApiKey;
            string result = "";
            try
            {
                result = Himall.Core.Helper.WebHelper.GetRequestData(url, "get", "");
                MapApiCoordinateResult resultobj = Newtonsoft.Json.JsonConvert.DeserializeObject<MapApiCoordinateResult>(result);
                //Newtonsoft.Json.JsonConvert..ParseFormJson<MapApiCoordinateResult>(result);
                Log.Info("GetLatLngByAddress url:" + url + ";Result:" + result);
                if (resultobj.status == 0)
                {
                    latLng = resultobj.result.location.lng + "," + resultobj.result.location.lat;
                }
                else
                {
                    Log.Info("GetLatLngByAddress status!=0 url:" + url + ";Result:" + result);
                }
            }
            catch (Exception ex)
            {
                Log.Error("GetLatLngByAddress" + ex.Message+";"+ex.StackTrace);
            }
            return latLng;
        }
    }
}
