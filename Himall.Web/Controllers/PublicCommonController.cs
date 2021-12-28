using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Service;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static Himall.Web.Framework.BaseController;

namespace Himall.Web.Controllers
{
    public class PublicCommonController : Controller
    {
        [HttpGet]
        public object GetMapAddressPos(string key, string keyword, int page_index = 1, int page_size = 10)
        {
            string strUrl = string.Format("https://apis.map.qq.com/ws/place/v1/suggestion?key={0}&keyword={1}&page_index{2}&page_size={3}",
                key, keyword, page_index, page_size);
            //该接口参考网址：https://lbs.qq.com/webservice_v1/guide-suggestion.html
            string res = Himall.Core.Helper.HttpHelper.HttpGet(strUrl);

            MapData resultobj = res.FromJSON<MapData>(new MapData { });
            return Json(resultobj, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// 图片转换为Base64字符流
        /// </summary>
        /// <param name="picUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public object GetBase64Pic(string picUrl)
        {
            System.Net.HttpWebRequest request = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(picUrl);
            request.Method = "GET";
            request.ContentType = "text/html;charset=UTF-8";
            request.Timeout = 10000;
            System.Net.HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            Stream myResponseStream = response.GetResponseStream();

            System.Drawing.Bitmap map = new System.Drawing.Bitmap(myResponseStream);
            MemoryStream ms = new MemoryStream();
            map.Save(ms, System.Drawing.Imaging.ImageFormat.Png);//  将图片内存流转成base64,图片以DataURI形式显示
            picUrl = "data:image/png;base64," + Convert.ToBase64String(ms.ToArray());
            ms.Dispose();

            Result result = new Result();
            result.success = true;
            result.msg = picUrl;
            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }

    #region 腾讯地图搜索后返回实体
    public class MapData
    {
        /// <summary>
        /// 状态
        /// </summary>
        public int status { get; set; }
        /// <summary>
        /// 状态说明
        /// </summary>
        public string message { get; set; }

        /// <summary>
        /// 数量
        /// </summary>
        public int count { get; set; }

        /// <summary>
        /// 逆地址解析结果
        /// </summary>
        public List<data> data { get; set; }

        public string request_id { get; set; }
    }

    /// <summary>
    /// 地址
    /// </summary>
    public class data
    {
        public string id { get; set; }
        public string title { get; set; }
        public string address { get; set; }
        public string category { get; set; }
        public int type { get; set; }
        public location location { get; set; }
        public int adcode { get; set; }
        public string province { get; set; }
        public string city { get; set; }
        public string district { get; set; }
    }

    /// <summary>
    /// 坐标
    /// </summary>
    public class location
    {
        public float lat { get; set; }
        public float lng { get; set; }
    }
    #endregion
}