using Himall.CommonModel;
using Himall.DTO.QueryModel;
using Himall.Service;
using NetRube.Data;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Himall.Entities;
using Himall.Service.Weixin;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Himall.Core;

namespace Himall.Service
{
    public class WXSmallProgramService : ServiceBase
    {
        /// <summary>
        /// 添加商品
        /// </summary>
        /// <param name="mWXSmallChoiceProductsInfo"></param>
        public void AddWXSmallProducts(WXSmallChoiceProductInfo mWXSmallChoiceProductsInfo)
        {
            DbFactory.Default.Add(mWXSmallChoiceProductsInfo);
        }

        /// <summary>
        /// 获取所有的商品
        /// </summary>GetWXSmallHomeProducts
        /// <param name="query"></param>
        /// <returns></returns>
        public QueryPageModel<Entities.ProductInfo> GetWXSmallProducts(int page, int rows, ProductQuery productQuery)
        {
            var model = new QueryPageModel<ProductInfo>();
            StringBuilder sqlsb = new StringBuilder();
            StringBuilder countsb = new StringBuilder();
            StringBuilder wheresb = new StringBuilder();
            countsb.Append(" select count(1) from Himall_Product pt left join Himall_WXSmallChoiceProduct ps on pt.Id=ps.ProductId left join Himall_Shop s on pt.ShopId=s.Id ");
            sqlsb.Append(" select pt.*,s.ShopName from Himall_Product pt left join Himall_WXSmallChoiceProduct ps on pt.Id=ps.ProductId ");
            sqlsb.Append(" left join Himall_Shop s on pt.ShopId=s.Id ");
            wheresb.Append(" where pt.IsDeleted=FALSE and ps.ProductId>0 ");
            if (!string.IsNullOrWhiteSpace(productQuery.KeyWords))
                wheresb.AppendFormat(" and pt.ProductName like '%{0}%' ", productQuery.KeyWords);
            if (!string.IsNullOrWhiteSpace(productQuery.ShopName))
                wheresb.AppendFormat(" and s.ShopName like '%{0}%' ", productQuery.ShopName);
            wheresb.Append(" order by ps.ProductId ");
            var start = (page - 1) * rows;
            var end = page * rows;
            countsb.Append(wheresb);
            sqlsb.Append(wheresb);
            sqlsb.Append(" limit " + start + "," + rows);
            var list = DbFactory.Default.Query<ProductInfo>(sqlsb.ToString()).ToList();
            //var list = Context.Database.SqlQuery<Entities.ProductInfo>(sqlsb.ToString()).ToList();
            //var shops = Context.ShopInfo;
            var products = list.ToArray().Select(item =>
            {
                var shop = DbFactory.Default.Get<ShopInfo>().Where(s => s.Id == item.ShopId).FirstOrDefault();
                if (shop != null)
                    item.ShopName = shop.ShopName;
                return item;
            });
            model.Models = products.ToList();
            var count = 0;
            count = DbFactory.Default.Query<int>(countsb.ToString()).FirstOrDefault();
            model.Total = count;
            return model;
        }

        /// <summary>
        /// 获取商品
        /// </summary>
        /// <returns></returns>
        public List<WXSmallChoiceProductInfo> GetWXSmallProducts()
        {
            return DbFactory.Default.Get<WXSmallChoiceProductInfo>().ToList();
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <param name="Id"></param>
        public void DeleteWXSmallProductById(long Id)
        {
            DbFactory.Default.Del<WXSmallChoiceProductInfo>(item => item.ProductId == Id);
        }
      
        public QueryPageModel<SearchProductInfo> GetWXSmallHomeProducts(int pageNo, int pageSize)
        {
            var data = DbFactory.Default.Get<SearchProductInfo>().Where(p => p.CanSearch == true).InnerJoin<Entities.WXSmallChoiceProductInfo>((mp, p) => mp.ProductId == p.ProductId && p.ProductId > 0).ToPagedList(pageNo, pageSize);
            return new QueryPageModel<SearchProductInfo>
            {
                Models = data,
                Total = data.TotalRecordCount
            };
        }
        /// <summary>
        /// 批量删除
        /// </summary>
        /// <param name="ids"></param>
        public void DeleteWXSmallProductByIds(List<long> ids)
        {
            DbFactory.Default.Del<WXSmallChoiceProductInfo>(item => item.ProductId.ExIn(ids));

        }

        #region 创建小程序二维码
        public void CreateAppletCode(string fileName,string appletId,string appletsecret, string appletcodedata)
        {

            if (Core.HimallIO.ExistFile(fileName))
            {
                var file = Core.HimallIO.GetFileMetaInfo(fileName);
                if (file.ContentLength > 1048)
                    return;
            }
            WXHelper wxhelper = new WXHelper();
            var accessToken = wxhelper.GetAccessToken(appletId,appletsecret);
            HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create("https://api.weixin.qq.com/wxa/getwxacode?access_token=" + accessToken);  //创建url
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] load = Encoding.UTF8.GetBytes(appletcodedata);
            request.ContentLength = load.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(load, 0, load.Length);
            HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            Stream s = response.GetResponseStream();
            byte[] mg = StreamToBytes(s);
            MemoryStream ms = new MemoryStream(mg);
            if (ms.Length < 1000)
                throw new Himall.Core.HimallException("二维码生成失败，appid或appsecret配置不对！");//它生成图大小肯定是没生成成功，弹出提示
            Core.HimallIO.CreateFile(fileName, ms, Core.FileCreateType.Create);
            //ms.Dispose();
            //return Core.HimallIO.GetImagePath(fileName);
        }


        private Stream GetAppletCodeStream(string fileName, string appletcodedata, string accessToken,string weixinAppletId,string weixinAppletSecret)
        {
            HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create("https://api.weixin.qq.com/wxa/getwxacode?access_token=" + accessToken);  //创建url
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] load = Encoding.UTF8.GetBytes(appletcodedata);
            request.ContentLength = load.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(load, 0, load.Length);
            HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            if (response.ContentType.IndexOf("json") >= 0)
            {
                StreamReader myStreamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string content = myStreamReader.ReadToEnd();
                var json = JObject.Parse(content);
                if (json["errcode"].Value<int>() == 40001)
                {
                    WXHelper wxhelper = new WXHelper();
                    accessToken = wxhelper.GetAccessToken(weixinAppletId,weixinAppletSecret, true);
                    return GetAppletCodeStream(fileName, appletcodedata, accessToken, weixinAppletId, weixinAppletSecret);
                }
                else
                    Log.Error("小程序二维码错误：" + content);

            }
            Stream s = response.GetResponseStream();
            return s;
        }
        private Stream CreateAppletCode(string fileName, string appletcodedata, string accessToken,string weixinAppletId,string weixinAppletSecret)
        {
            HttpWebRequest request = (System.Net.HttpWebRequest)WebRequest.Create("https://api.weixin.qq.com/wxa/getwxacode?access_token=" + accessToken);  //创建url
            request.Method = "POST";
            request.ContentType = "application/json;charset=UTF-8";
            byte[] load = Encoding.UTF8.GetBytes(appletcodedata);
            request.ContentLength = load.Length;
            Stream writer = request.GetRequestStream();
            writer.Write(load, 0, load.Length);
            HttpWebResponse response = (System.Net.HttpWebResponse)request.GetResponse();
            if (response.ContentType.IndexOf("json") >= 0)
            {
                StreamReader myStreamReader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                string content = myStreamReader.ReadToEnd();
                var json = JObject.Parse(content);

                if (json["errcode"].Value<int>() == 40001)
                {
                    WXHelper wxhelper = new WXHelper();
                    accessToken = wxhelper.GetAccessToken(weixinAppletId,weixinAppletSecret);
                    return GetAppletCodeStream(fileName, appletcodedata, accessToken,weixinAppletId,weixinAppletSecret);
                }
                else
                    Log.Error("小程序二维码错误：" + content);

            }
            Stream s = response.GetResponseStream();
            return s;
        }

        /// <summary>
        /// Stream转换为byte
        /// </summary>
        private byte[] StreamToBytes(Stream stream)
        {
            List<byte> bytes = new List<byte>();
            int temp = stream.ReadByte();
            while (temp != -1)
            {
                bytes.Add((byte)temp);
                temp = stream.ReadByte();
            }
            return bytes.ToArray();
        }
        #endregion
    }
}
