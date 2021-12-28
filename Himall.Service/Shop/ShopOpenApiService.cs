using Himall.Core;
using Himall.Service;
using Himall.Entities;
using System;
using NetRube.Data;

namespace Himall.Service
{
    /// <summary>
    /// 店铺OpenApi服务
    /// </summary>
    public class ShopOpenApiService : ServiceBase
    {
        /// <summary>
        /// 获取店铺的OpenApi配置
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public ShopOpenApiSettingInfo Get(long shopId)
        {
            if (shopId < 1)
            {
                throw new HimallException("错误的店铺编号");
            }
            return DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.ShopId == shopId).FirstOrDefault();
        }
        /// <summary>
        /// 获取店铺的OpenApi配置
        /// </summary>
        /// <param name="appkey"></param>
        /// <returns></returns>
        public ShopOpenApiSettingInfo Get(string appkey)
        {
            if (string.IsNullOrWhiteSpace(appkey))
            {
                throw new HimallException("错误的appkey");
            }
            return DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.AppKey == appkey).FirstOrDefault();
        }
        /// <summary>
        /// 生成一个OpenApi配置
        /// <para>如果店铺已生成会异常</para>
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        public ShopOpenApiSettingInfo MakeOpenApi(long shopId)
        {
            if (shopId <= 0)
            {
                throw new HimallException("[OpenApi]错误的店铺编号");
            }
            if (DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.ShopId == shopId).Exist())
            {
                throw new HimallException("[OpenApi]店铺已生成AppKey,不可以重复生成");
            }
            ShopOpenApiSettingInfo result = new ShopOpenApiSettingInfo();
            result.ShopId = shopId;
            result.AppKey = MakeAppKey(shopId);
            result.AppSecreat = MakeAppSecreat();
            result.AddDate = DateTime.Now;
            result.LastEditDate= DateTime.Now;
            result.IsEnable = false;
            result.IsRegistered = false;
            return result;
        }
        /// <summary>
        /// 生成一个appkey
        /// </summary>
        /// <param name="shopId"></param>
        /// <returns></returns>
        private string MakeAppKey(long shopId = 0)
        {
            string result = "open";
            if (shopId > 0)
            {
                result = result + shopId.ToString();
            }
            while (true)
            {
                Random rnd = new Random();
                string[] seeds = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k",
                    "l", "m", "n", "o", "p","q", "r", "s", "t", "u", "v", "w", "x", "y",
                    "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                int seedlen = seeds.Length;
                for (int _i = 0; _i < 10; _i++)
                {
                    result += seeds[rnd.Next(0, seedlen)];
                }
                if (!DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.AppKey == result).Exist())
                {
                    break;
                }
            }
            return result;
        }
        /// <summary>
        /// 生成一个AppSecreat
        /// </summary>
        /// <returns></returns>
        private string MakeAppSecreat()
        {
            string result = "oas";
            while (true)
            {
                Random rnd = new Random();
                string[] seeds = { "a", "b", "c", "d", "e", "f", "g", "h", "i", "j", "k",
                    "l", "m", "n", "o", "p","q", "r", "s", "t", "u", "v", "w", "x", "y",
                    "z", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
                int seedlen = seeds.Length;
                for (int _i = 0; _i < 18; _i++)
                {
                    result += seeds[rnd.Next(0, seedlen)];
                }
                if (!DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.AppKey == result).Exist())
                {
                    break;
                }
            }
            return result;
        }
        /// <summary>
        /// 添加店铺OpenApi配置
        /// </summary>
        /// <param name="data"></param>
        public void Add(ShopOpenApiSettingInfo data)
        {
            if (DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.ShopId == data.ShopId).Exist())
            {
                throw new HimallException("[OpenApi]店铺不可拥有多个AppKey");
            }
            if (DbFactory.Default.Get<ShopOpenApiSettingInfo>().Where(d => d.AppKey == data.AppKey).Exist())
            {
                throw new HimallException("[OpenApi]AppKey已存在");
            }
            if (string.IsNullOrWhiteSpace(data.AppKey))
            {
                throw new HimallException("[OpenApi]AppKey不可以为空");
            }
            if (string.IsNullOrWhiteSpace(data.AppSecreat))
            {
                throw new HimallException("[OpenApi]AppSecreat不可以为空");
            }
            DbFactory.Default.Add(data);
        }
        /// <summary>
        /// 修改店铺OpenApi配置
        /// </summary>
        /// <param name="data"></param>
        public void Update(ShopOpenApiSettingInfo data)
        {
            if (string.IsNullOrWhiteSpace(data.AppKey))
            {
                throw new HimallException("[OpenApi]AppKey不可以为空");
            }
            if (string.IsNullOrWhiteSpace(data.AppSecreat))
            {
                throw new HimallException("[OpenApi]AppSecreat不可以为空");
            }
            DbFactory.Default.Update(data);
        }
        /// <summary>
        /// 设置启用状态
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="state">true:开启,false:关闭</param>
        /// <returns></returns>
        public void SetEnableState(string appkey, bool state)
        {
            var data = Get(appkey);
            if(data==null)
            {
                throw new HimallException("[OpenApi]错误的appkey");
            }
            data.IsEnable = state;
            Update(data);
        }
        /// <summary>
        /// 设置启用状态
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="state">true:开启,false:关闭</param>
        /// <returns></returns>
        public void SetEnableState(long shopId, bool state)
        {
            var data = Get(shopId);
            if (data == null)
            {
                throw new HimallException("[OpenApi]错误的shopId");
            }
            data.IsEnable = state;
            Update(data);
        }
        /// <summary>
        /// 设置注册状态
        /// </summary>
        /// <param name="appkey"></param>
        /// <param name="state">true:成功,false:失败</param>
        /// <returns></returns>
        public void SetRegisterState(string appkey, bool state)
        {
            var data = Get(appkey);
            if (data == null)
            {
                throw new HimallException("[OpenApi]错误的appkey");
            }
            data.IsRegistered = state;
            Update(data);
        }
        /// <summary>
        /// 设置注册状态
        /// </summary>
        /// <param name="shopId"></param>
        /// <param name="state">true:成功,false:失败</param>
        /// <returns></returns>
        public void SetRegisterState(long shopId, bool state)
        {
            var data = Get(shopId);
            if (data == null)
            {
                throw new HimallException("[OpenApi]错误的shopId");
            }
            data.IsRegistered = state;
            Update(data);
        }

    }
}
