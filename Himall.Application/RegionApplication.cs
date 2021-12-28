using Himall.CommonModel;
using Himall.Core;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.Linq;

using Newtonsoft.Json;
using System.Configuration;
using Himall.Core.Helper;
using System.Threading.Tasks;
using com.google.zxing;
using NetRube.FastJson;
using NetRube;

namespace Himall.Application
{
    public class RegionApplication
    {
        private const double EARTH_RADIUS = 6378137.0;//地球赤道半径(单位：m。6378137m是1980年的标准，比1975年的标准6378140少3m）
        private static RegionService _RegionService = ObjectContainer.Current.Resolve<RegionService>();

        /// <summary>
        /// 获取全部 区域数据
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<Region> GetAllRegions()
        {
            return _RegionService.GetAllRegions();
        }

        /// <summary>
        /// 直接从接从json文件获取，没有缓存
        /// </summary>
        /// <returns></returns>
        public static List<Region> GetRegionsFromDb()
        {
           return _RegionService.LoadRegionData().Where(r => r.Status == Region.RegionStatus.Normal).ToList<Region>();
        }

        /// <summary>
        /// 获取指定区域
        /// </summary>
        /// <param name="id">区域编号</param>
        /// <returns></returns>
        public static Region GetRegion(long id)
        {
            var model = _RegionService.GetRegion(id);
            if (model == null)
            {
                model = _RegionService.GetRegion(GetDefaultRegionId());
            }
            return model;
        }

        public static Region GetRegionGaoDe(long id, List<Region> RegionSource)
        {
            var model = RegionSource.FirstOrDefault(p => p.Id == id);
            if (model == null)
            {
                model = RegionSource.FirstOrDefault(region=>region.Id== GetDefaultRegionId());
            }
            return model;
        }

        /// <summary>
        /// 获取 指定下属区域
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static IEnumerable<Region> GetSubRegion(long parentId, bool trace = false)
        {
            return _RegionService.GetSubs(parentId, trace);
        }

        /// <summary>
        /// 获取下属区域到三级
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static IEnumerable<Region> GetThirdSubRegion(long parentId)
        {
            return _RegionService.GetThridSubs(parentId);
        }

        /// <summary>
        /// 获取指定区域下属 键(编号)/值(名称)
        /// </summary>
        /// <param name="parentId"></param>
        /// <returns></returns>
        public static Dictionary<int, string> GetSubMap(long parentId)
        {
            return _RegionService.GetSubs(parentId).ToDictionary(k => k.Id, v => v.Name);
        }

        /// <summary>
        ///  获取省 市 区 的编号，中间用逗号隔开
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public static string GetRegionPath(int regionId)
        {
            return _RegionService.GetRegionPath(regionId);
        }
        /// <summary>
        /// 根据地址名称反查地址全路径
        /// </summary>
        /// <param name="city">城市名</param>
        /// <param name="district">区名</param>
        /// <param name="street">街道名</param>
        /// <returns></returns>
        public static string GetAddress_Components(string city, string district, string street, out string newStreet)
        {
            return _RegionService.GetAddress_Components(city, district, street, out newStreet);
        }
        /// <summary>
        /// 根据城名称获取对应的区域模型
        /// </summary>
        /// <param name="name"></param>
        /// <param name="level"></param>
        /// <returns></returns>
        public static Region GetRegionByName(string name, Region.RegionLevel level)
        {
            var cityInfo= _RegionService.GetRegionByName(name, level);
            if (cityInfo == null && level== Region.RegionLevel.City && !string.IsNullOrEmpty(name) && name.Substring(name.Length - 1) == "市")
            {
                name = name.Substring(0, name.Length - 1) + "地区";//可能因为定位是市，而后台存的是地区，则再转换再取下，例如定位吐鲁番是“吐鲁番市”，而后台存的是“吐鲁番地区”
                cityInfo = RegionApplication.GetRegionByName(name, level);
            }
            return cityInfo;
        }
        /// <summary>
        /// 获取 区域全称,不同等级 空格 隔开
        /// </summary>
        /// <param name="regionId"></param>
        /// <param name="seperator">分隔符</param>
        /// <returns></returns>
        public static string GetFullName(int id, string seperator = " ")
        {
            return _RegionService.GetFullName(id, seperator);
        }

        /// <summary>
        /// 获取区域名称
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetRegionName(int id)
        {
            var region = _RegionService.GetRegion(id);
            return region.Name;
        }

        /// <summary>
        /// 获取 区域对应等级的名称
        /// </summary>
        /// <param name="id">区域ID</param>
        /// <param name="level">等级</param>
        /// <returns></returns>
        public static Region GetRegion(long id, Region.RegionLevel level)
        {
            return _RegionService.GetRegion(id, level);
        }


        #region  高德导航同步模块

        /// <summary>
        /// 获取高德同步状态
        /// </summary>
        /// <param name="context"></param>
        public static RegionSyncStatus GetDistrictSyncStatus()
        {
            RegionSyncStatus syncStatus = Cache.Get<RegionSyncStatus>(CacheKeyCollection.GaoDRegionSyncStatus);//是否正在同步
            if (syncStatus == null)
            {
                syncStatus = new RegionSyncStatus() { SynchronizedCount = 100, TotalCount = 100 };
                
            }
            return syncStatus;
        }

        /// <summary>
        /// 记录总数
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private static int GetDistrictCount(GaoDeMapRegionInfo result)
        {
            if (result == null)
            {
                return 0;
            }
            int count = 0;
            //数据获取成功,开户一个线程处理数据
            if (result.status == 1)
            {
                                                              //从第二开始 省份
                List<DistrictInfo> regions = result.districts[0].districts;
                //
                foreach (DistrictInfo proviceInfo in regions)
                {
                    count += 1;
                    List<DistrictInfo> regions2 = proviceInfo.districts;
                    if (regions2 != null)
                    {
                        foreach (DistrictInfo cityInfo in regions2)
                        {
                            count += 1;
                            //区县
                            List<DistrictInfo> regions3 = cityInfo.districts;
                            if (regions3 != null)
                            {
                                foreach (DistrictInfo areaInfo in regions3)
                                {
                                    count += 1;
                                    //街道
                                    List<DistrictInfo> regions4 = areaInfo.districts;
                                    if (regions4 != null)
                                    {
                                        foreach (DistrictInfo streetInfo in regions4)
                                        {
                                            count += 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return count;
        }

        internal static string GetFullName(object addressId)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// 保存省份信息
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        private static int SaveRegion(List<Region> RegionSource,DistrictInfo info,long parentId)
        {
            Region region = null;
            int regionId = 0;
            var regioninflist = GetSubs(RegionSource,parentId);
            bool update = false;
            foreach (Region regionInfo in regioninflist)
            {
                var shortname = _RegionService.GetShortAddressName(info.name);
                if (regionInfo.ShortName.Contains(shortname))//是否存在区域
                {
                    region = GetRegionGaoDe(regionInfo.Id, RegionSource);
                    if (!regionInfo.Name.Contains(info.name))//名称不完成相等需要更新
                    {
                        region.Name = info.name;
                        update = true;
                    }
                    regionId = region.Id;
                    break;
                }
            }
            if (update)//已存在并且需要更新名称
            {
                EditeRgionGaode(RegionSource,region.Name, region.Id);
            }
            else if (region == null)//不存在则创建新的
            {
                regionId=AddRegionGaoDe(RegionSource,info.name, parentId);
            }
            return regionId;
            
        }

        public static IEnumerable<Region> GetSubs(List<Region> RegionSource,long parent)
        {
            if (parent == 0)
                return RegionSource.Where(p => p.ParentId == 0);

            var region = RegionSource.FirstOrDefault(p => p.Id == parent);

            IEnumerable<Region> subs = new List<Region>();
            if (region!=null&&region.Sub != null)
                subs = region.Sub.Where(r => r.Status == Region.RegionStatus.Normal);
            return subs;//下属区域
        }

        private static int AddRegionGaoDe(List<Region> RegionSource,string regionName, long parentid)
        {
            var region = new Region();
            //var provinces = RegionSource;
            region.Id = RegionSource.Max(a => a.Id) + 1;
            if (parentid == 0)
            {
                region.Level = Region.RegionLevel.Province;
                region.Name = regionName;
                region.ShortName =_RegionService.GetShortAddressName(regionName);
                //provinces.Concat(new Region[] { region });
                RegionSource.Add(region);//上面用IEnumerable .Concat添加不进，改为list；
            }
            else
            {
                var parent = RegionSource.FirstOrDefault(p => p.Id == parentid);
                if(parent.Sub==null)
                {
                    parent.Sub = new List<Region>();
                }

                region.Level = parent.Level + 1;
                region.Name = regionName;
                region.ShortName = _RegionService.GetShortAddressName(regionName);
                region.Parent = parent;
                parent.Sub.Add(region);
                RegionSource.Add(region);
            }
            return region.Id;

        }

        private static void EditeRgionGaode(List<Region> RegionSource,string regionName, long regionId)
        {
           
            var region = GetRegionGaoDe(regionId, RegionSource);

            IEnumerable<Region> regs;
            //检查重名
            if (region.Level == Region.RegionLevel.Province)
            {
                regs = RegionSource.Where(a => a.Level == Region.RegionLevel.Province & a.Id != regionId);
            }
            else
            {
                regs = region.Parent.Sub.Where(a => a.Id != regionId);
            }
            region.Name = regionName;
            region.ShortName =_RegionService.GetShortAddressName(regionName);
        }

        private static bool IsExistRegionName(string regionname,string districtname)
        {
            string []keyWordstr= {"省", "特别行政区", "自治区", "市", "街道", "村", "镇", "乡" };
            foreach (var keyword in keyWordstr)
            {
                districtname.Replace(keyword,"");
            }

           return regionname.Contains(districtname);
        }

        /// <summary>
        /// 同步高德导航
        /// </summary>
        public static dynamic SyncGaoDeApiData() {
            dynamic result = new { success = false, msg = "获取失败"};
            RegionSyncStatus syncStatus = Cache.Get<RegionSyncStatus>(CacheKeyCollection.GaoDRegionSyncStatus);//是否正在同步
            if (syncStatus != null && syncStatus.IsSynchroning)
            {
                RegionSyncStatus status= GetDistrictSyncStatus();
                result =new {success = true,data= status };
            }
            else
            {
                syncStatus = new RegionSyncStatus();
                string gaodeApiKey = SiteSettingApplication.SiteSettings.JDRegionAppKey;//获取高德appkey
                string gaodeApiUrl = $"https://restapi.amap.com/v3/config/district?key={gaodeApiKey}&subdistrict=4";
             
                string gaodresult=HttpHelper.HttpGet(gaodeApiUrl).Replace("\"citycode\":[]", "\"citycode\":\"\"");
                GaoDeMapRegionInfo objResult = JsonConvert.DeserializeObject<GaoDeMapRegionInfo>(gaodresult);
                if (objResult == null)
                {
                    throw new HimallException("从接口获取数据失败");
                }
                else {
                    //数据获取成功,开户一个线程处理数据
                    if (objResult.status == 1)
                    {
                        syncStatus.TotalCount = GetDistrictCount(objResult);
                        Cache.Insert(CacheKeyCollection.GaoDRegionSyncStatus, syncStatus, 36000);//插入缓存，记录是否已开始同步,如果此缓存为空或者为true则表示同步完成
                        Task.Factory.StartNew(() =>
                        {
                            try
                            {
                                int provinceId = 0;
                                int cityId = 0;
                                int districtId = 0;
                                int completedCount = 0;
                                int totalCount = syncStatus.TotalCount;

                                List<DistrictInfo> regions = objResult.districts;//第一级,国家
                                                                                 //从第二开始 省份
                                regions = objResult.districts[0].districts;

                                var gaoDeRegionSource = GetRegionsFromDb();
                                foreach (DistrictInfo proviceInfo in regions)
                                {
                                    provinceId = SaveRegion(gaoDeRegionSource, proviceInfo, 0);
                                    completedCount += 1;
                                    syncStatus.SynchronizedCount = completedCount;
                                    //城市
                                    List<DistrictInfo> regions2 = proviceInfo.districts;
                                    if (regions2 != null)
                                    {
                                        foreach (DistrictInfo cityInfo in regions2)
                                        {
                                            cityId = SaveRegion(gaoDeRegionSource, cityInfo, provinceId);
                                            completedCount += 1;
                                            //区县
                                            List<DistrictInfo> regions3 = cityInfo.districts;
                                            if (regions3 != null)
                                            {
                                                foreach (DistrictInfo areaInfo in regions3)
                                                {
                                                    districtId = SaveRegion(gaoDeRegionSource, areaInfo, cityId);
                                                    completedCount += 1;
                                                    //街道
                                                    List<DistrictInfo> regions4 = areaInfo.districts;
                                                    if (regions4 != null)
                                                    {
                                                        foreach (DistrictInfo streetInfo in regions4)
                                                        {
                                                            SaveRegion(gaoDeRegionSource, streetInfo, districtId);
                                                            completedCount += 1;
                                                        }
                                                    }
                                                    SaveSyncRate(completedCount, totalCount);
                                                }
                                            }
                                        }
                                    }
                                }
                                _RegionService.SaveAllRegions(gaoDeRegionSource);
                                Cache.InsertLocal<IEnumerable<Region>>(CacheKeyCollection.Region, null,600);//同步完成后清除缓存
        
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex);
                            }
                            

                        });
                        if (objResult.status == 1)
                        {
                            result=new { success = true, total = syncStatus.TotalCount, msg = "已获数据,后台自动同步中,请等待同步完成" };
                        }
                        else
                        {
                            throw new HimallException("从接口获取数据失败");
  
                        }
                    }
                }
            }
            return result;
        }

   


        private static void SaveSyncRate(int completedCount, int totalCount)
        {
            RegionSyncStatus syncStatus = Cache.Get<RegionSyncStatus>(CacheKeyCollection.GaoDRegionSyncStatus);//同步状态获取 
            if (syncStatus != null)
            {
                syncStatus.SynchronizedCount = completedCount;
            }
            else
            {
                syncStatus = new RegionSyncStatus() { SynchronizedCount = completedCount, TotalCount = totalCount };
            }
            Cache.Insert(CacheKeyCollection.GaoDRegionSyncStatus, syncStatus, 36000);//插入缓存，记录是否已开始同步,如果此缓存为空或者为true则表示同步完成
        }

        #endregion



        /// <summary>
        /// 添加区域
        /// </summary>
        /// <param name="regionName"></param>
        /// <param name="level"></param>
        /// <param name="path"></param>
        public static long AddRegion(string regionName, long parentId)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new HimallException("区域名称不能为空");
            }
            return _RegionService.AddRegion(regionName, parentId);
        }


        /// <summary>
        /// 修改区域
        /// </summary>
        /// <param name="regionName"></param>
        /// <param name="regionId"></param>
        public static void EditRegion(string regionName, int regionId)
        {
            if (string.IsNullOrWhiteSpace(regionName))
            {
                throw new HimallException("区域名称不能为空");
            }
            _RegionService.EditRegion(regionName, regionId);
        }

        /// <summary>
        /// 获取区域简称
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static string GetRegionShortName(int id)
        {
            var region = _RegionService.GetRegion(id);
            return string.IsNullOrEmpty(region.ShortName) ? region.Name : region.ShortName;
        }

        /// <summary>
        /// 获取区域名称(多个区域)
        /// </summary>
        /// <param name="ids">编号列表</param>
        /// <param name="seperator">分割符号</param>
        /// <returns></returns>
        public static string GetRegionName(string regionIds, string seperator = ",")
        {
            var ids = regionIds.Split(new string[] { seperator }, StringSplitOptions.RemoveEmptyEntries).Select(p => long.Parse(p)).ToList();
            var regions = new List<Region>();
            foreach (var item in ids)
                regions.Add(_RegionService.GetRegion(item));

            var result = string.Empty;
            foreach (var item in regions)
            {
                if (!string.IsNullOrEmpty(result)) result += seperator;
                if (item != null) result += item.Name;
            }
            return result;
        }

        public static Region GetRegionByName(string name)
        {
            return _RegionService.GetRegionByName(name);
        }

        /// <summary>
        /// 通过ip获取地区信息
        /// <para>(数据来源：淘宝)</para>
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static long GetRegionByIPInTaobao(string ip)
        {
            return _RegionService.GetRegionByIPInTaobao(ip);
        }

        /// <summary>
        /// 重置为默认
        /// </summary>
        public static void ResetRegion()
        {
            _RegionService.ResetRegions();
        }

        /// <summary>
        /// 同步京东地址库
        /// </summary>
        public static void SysJDRegions()
        {
            var sitesetting = SiteSettingApplication.SiteSettings;
            if (string.IsNullOrEmpty(sitesetting.JDRegionAppKey))
            {
                throw new HimallException("京东地址库APPKEY不能为空");
            }
            _RegionService.SysJDRegions(sitesetting.JDRegionAppKey);
        }

        /// <summary>
        /// 删除区域
        /// </summary>
        /// <param name="regionId"></param>
        /// <returns></returns>
        public static void DelRegion(int regionId)
        {
            _RegionService.DelRegion(regionId);
        }

        /// <summary>
        /// 默认 region id
        /// </summary>
        /// <returns></returns>
        public static int GetDefaultRegionId()
        {
            return CommonConst.DEFAULT_REGION_ID;
        }

        /// <summary>
        /// 是否开启京东地址库
        /// </summary>
        /// <returns></returns>
        public static bool IsOpenJdRegion()
        {
            return _RegionService.IsOpenJdRegion();
        }

        /// <summary>
        /// 获取两点间距离
        /// </summary>
        /// <param name="fromLatLng"></param>
        /// <param name="endLatLng"></param>
        /// <returns></returns>
        public static int GetDistance(string fromLatLng, string endLatLng)
        {
            if (string.IsNullOrWhiteSpace(fromLatLng) || string.IsNullOrWhiteSpace(endLatLng))
            {
                return 0;
            }
            var aryLatlng = fromLatLng.Split(',');
            var aryToLatlng = endLatLng.Split(',');

            if (aryLatlng.Length < 2 || aryToLatlng.Length < 2)
            {
                return 0;
            }
            var fromlat = double.Parse(aryLatlng[0]);
            var fromlng = double.Parse(aryLatlng[1]);
            var tolat = double.Parse(aryToLatlng[0]);
            var tolng = double.Parse(aryToLatlng[1]);
            return GetDistance(fromlat, fromlng, tolat, tolng);
        }
        /// <summary>
        /// 获取两点间距离
        /// </summary>
        /// <param name="fromlat"></param>
        /// <param name="fromlng"></param>
        /// <param name="tolat"></param>
        /// <param name="tolng"></param>
        /// <returns></returns>
        public static int GetDistance(decimal fromlat, decimal fromlng, decimal tolat, decimal tolng)
        {
            double dfromlat = double.Parse(fromlat.ToString());
            double dfromlng = double.Parse(fromlng.ToString());
            double dtolat = double.Parse(tolat.ToString());
            double dtolng = double.Parse(tolng.ToString());
            return GetDistance(dfromlat, dfromlng, dtolat, dtolng);
        }
        /// <summary>
        /// 获取两点间距离
        /// </summary>
        /// <param name="fromlat"></param>
        /// <param name="fromlng"></param>
        /// <param name="tolat"></param>
        /// <param name="tolng"></param>
        /// <returns></returns>
        public static int GetDistance(double fromlat, double fromlng, double tolat, double tolng)
        {
            var fromRadLat = fromlat * Math.PI / 180.0;
            var toRadLat = tolat * Math.PI / 180.0;
            double a = fromRadLat - toRadLat;
            double b = (fromlng * Math.PI / 180.0) - (tolng * Math.PI / 180.0);
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) +
                Math.Cos(fromRadLat) * Math.Cos(toRadLat) * Math.Pow(Math.Sin(b / 2), 2)));
            s = s * EARTH_RADIUS;
            s = (Math.Round(s * 10000) / 10000);
            int result = (int)s;
            return result;

        }
    }
}
