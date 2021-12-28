using Himall.Core;
using Himall.DTO;
using Himall.Entities;
using Himall.Service;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Himall.Application
{
    public class WXSmallProgramApplication
    {
        private static WXSmallProgramService _WXSmallProgramService = ObjectContainer.Current.Resolve<WXSmallProgramService>();
        private static ProductService _ProductService = ObjectContainer.Current.Resolve<ProductService>();
        private static WeixinMenuService _WeixinMenuService = ObjectContainer.Current.Resolve<WeixinMenuService>();


        public static void SetWXSmallProducts(string productIds)
        {
            List<Entities.ProductInfo> lProductInfo = new List<Entities.ProductInfo>();
            var lbId = _WXSmallProgramService.GetWXSmallProducts().Select(item => item.ProductId).ToList();
            if (!string.IsNullOrEmpty(productIds))
            {
                var productIdsArr = productIds.Split(',').Select(item => long.Parse(item)).ToList();
                lProductInfo = _ProductService.GetAllProductByIds(productIdsArr);
                foreach (Entities.ProductInfo item in lProductInfo)
                {
                    if (!lbId.Contains(Convert.ToInt32(item.Id)))
                    {
                        Entities.WXSmallChoiceProductInfo mProductsInfo = new Entities.WXSmallChoiceProductInfo()
                        {
                            ProductId = Convert.ToInt32(item.Id)
                        };
                        _WXSmallProgramService.AddWXSmallProducts(mProductsInfo);
                    }
                }
            }
        }

        /// <summary>
        /// 获取小程序的底部导航菜单
        /// </summary>
        /// <returns></returns>
        public static List<MobileFootMenuInfo> GetMobileFootMenuInfos(MenuInfo.MenuType type, long shopid = 0)
        {
            return _WeixinMenuService.GetFootMenus(type, shopid);
        }

        public static MobileFootMenuInfo GetFootMenusById(long id)
        {
            return _WeixinMenuService.GetFootMenusById(id);
        }

        /// <summary>
        /// 添加或修改栏目
        /// </summary>
        /// <param name="menuInfo"></param>
        public static void AddFootMenu(MobileFootMenuInfo menuInfo)
        {
            var footMenuInfo = new MobileFootMenuInfo();
            if (menuInfo.Id > 0)
            {
                footMenuInfo = _WeixinMenuService.GetFootMenusById(menuInfo.Id);
                if (footMenuInfo.ShopId != menuInfo.ShopId)
                {
                    throw new Himall.Core.HimallException("不能修改其他商家微店导航!");
                }
                footMenuInfo.Id = footMenuInfo.Id;
            }
            if (string.IsNullOrEmpty(menuInfo.Name))
                throw new Himall.Core.HimallException("栏目名称不能为空!");
            if (menuInfo.MenuIcon.StartsWith("/temp/"))
            {
                string newMenuIcon = "/Storage/master/menu/" + menuInfo.MenuIcon.Substring(menuInfo.MenuIcon.LastIndexOf("/") + 1);
                HimallIO.CopyFile(menuInfo.MenuIcon, newMenuIcon, true);
                footMenuInfo.MenuIcon = newMenuIcon;
            }
            if (menuInfo.MenuIconSel.StartsWith("/temp/"))
            {
                string newMenuIconSel = "/Storage/master/menu/" + menuInfo.MenuIconSel.Substring(menuInfo.MenuIconSel.LastIndexOf("/") + 1);
                HimallIO.CopyFile(menuInfo.MenuIconSel, newMenuIconSel, true);
                footMenuInfo.MenuIconSel = newMenuIconSel;
            }
            footMenuInfo.ShopId = menuInfo.ShopId;
            footMenuInfo.MenuIcon = menuInfo.MenuIcon;
            footMenuInfo.MenuIconSel = menuInfo.MenuIconSel;
            footMenuInfo.Url = menuInfo.Url;
            footMenuInfo.Name = menuInfo.Name;
            footMenuInfo.Type = menuInfo.Type;

            if (footMenuInfo.Id > 0)
                _WeixinMenuService.UpdateMobileFootMenu(footMenuInfo);
            else
                _WeixinMenuService.AddMobileFootMenu(footMenuInfo);
        }

        public static void DeleteFootMenu(long mid, long shopid = 0)
        {
            _WeixinMenuService.DeleteFootMenu(mid, shopid);
        }


        #region 生成下载的小程序二维码
        /// <summary>
        /// 生成小程序二维码扫描地址
        /// </summary>
        /// <param name="fileName">生成文件保存路径名称</param>
        /// <param name="appletPath">小程序对应路径</param>
        /// <param name="width">小程序二维码图片大小</param>
        /// <returns></returns>
        public static string GetWxAppletCode(string fileName, string appletPath, int width = 300)
        {
            if (fileName.IndexOf("/") == -1)
                fileName = "/Storage/Applet/" + fileName;

            //如上面图大小小于1kb说明之前生成的图是失败图，则执行重新生成图，图片已存在大小超过1kb不需重新生成；
            if (Core.HimallIO.ExistFile(fileName))
            {
                var file = Core.HimallIO.GetFileMetaInfo(fileName);
                if (file.ContentLength > 1048)
                    return fileName;
            }

            try
            {
                SiteSettings site = SiteSettingApplication.SiteSettings;
                if (!site.IsOpenMallSmallProg || string.IsNullOrEmpty(site.WeixinAppletId) || string.IsNullOrEmpty(site.WeixinAppletSecret))
                    return string.Empty;

                string accessToken = WXApiApplication.TryGetToken(site.WeixinAppletId, site.WeixinAppletSecret);//如果access_token无效则重新获取
                var data = "{\"path\":\"" + HttpUtility.UrlDecode(appletPath) + "\",\"width\":" + (width <= 0 ? 300 : width) + "}";
                Stream s = GetAppletCodeStream(fileName, data, accessToken);
                byte[] mg = StreamToBytes(s);
                #region 它accesstoken过期了，重新获取下token
                if (mg.Length <= 1024)
                {
                    MemoryStream stream = new MemoryStream(mg);
                    StreamReader reader = new StreamReader(stream);
                    Log.Error("生成小程序二维码：:" + reader.ReadToEnd());//记录日志

                    accessToken = WXApiApplication.TryGetToken(SiteSettingApplication.SiteSettings.WeixinAppletId, SiteSettingApplication.SiteSettings.WeixinAppletSecret, true);
                    s = GetAppletCodeStream(fileName, data, accessToken);
                    mg = StreamToBytes(s);
                }
                #endregion

                MemoryStream ms = new MemoryStream(mg);
                HimallIO.DeleteFile(fileName);//新删除已存在的小程序二维码图，后面再生成最新的小程序二维码图
                HimallIO.CreateFile(fileName, ms);//生成小程序二维码图

                ms.Dispose();
                ms.Close();
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }

            return fileName;
        }

        /// <summary>
        /// 获取小程序二维码Stream
        /// </summary>
        private static Stream GetAppletCodeStream(string fileName, string appletcodedata, string accessToken)
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
                    accessToken = WXApiApplication.TryGetToken(SiteSettingApplication.SiteSettings.WeixinAppletId, SiteSettingApplication.SiteSettings.WeixinAppletSecret, true);
                    return GetAppletCodeStream(fileName, appletcodedata, accessToken);
                }
                else
                    Log.Error("小程序二维码错误：" + content);

            }
            Stream s = response.GetResponseStream();
            return s;
        }


        /// <summary>
        /// 生成微店小程序二维码(其中二维码中间logo是微店自己的logo，不是平台logo)
        /// </summary>
        public static string GetWxAppletCodeVshopOwnLogo(Entities.VShopInfo vshop, long vshopid = 0)
        {
            try
            {
                vshop = vshop != null && vshop.Id > 0 ? vshop : VshopApplication.GetVShop(vshopid);
                if (vshop == null || vshop.Id <= 0)
                    return string.Empty;

                string vshopfilename = string.Format(@"/Storage/Applet/AppletShop{0}_Vshop{1}.png", vshop.ShopId, vshop.Id);//微店小程序logo
                string vcreatefilename = string.Format(@"/Storage/Applet/AppletShop{0}_Vshop{1}Create.png", vshop.ShopId, vshop.Id);//已合成的微店小程序logo
                string pagePath = "pages/vShopHome/vShopHome?id=" + vshop.Id;//小程序对应的路径

                if (Himall.Core.HimallIO.ExistFile(vcreatefilename))
                {
                    if (string.IsNullOrEmpty(vshop.WXLogo) || !HimallIO.ExistFile(vshop.WXLogo))
                        return vcreatefilename;//微店小程序二维码存在，而微店logo不存在直接返回它已合成的二维码路径

                    var fiposter = HimallIO.GetFileMetaInfo(vcreatefilename);//微店小程序二维码
                    var fi = HimallIO.GetFileMetaInfo(vshop.WXLogo);//微店logo文件

                    if (fi != null && fi.LastModifiedTime < fiposter.LastModifiedTime)
                        return vcreatefilename;
                }


                //生成小程序二维码图片
                string qrCodeImagePath = GetWxAppletCode(vshopfilename, pagePath, 300);
                if (string.IsNullOrEmpty(vshop.WXLogo) || !HimallIO.ExistFile(vshop.WXLogo) || string.IsNullOrEmpty(qrCodeImagePath))
                    return qrCodeImagePath;//而微店logo不存在直接返回它原二维码路径,或小程序还没配置正确qrCodeImagePath空返回

                #region 合成二维码
                var strurl = HttpContext.Current.Server.MapPath(vcreatefilename);
                Bitmap Qrimage = (Bitmap)Image.FromFile(HttpContext.Current.Server.MapPath(qrCodeImagePath));//小程序二维码图片中间logo合成当前小程序上


                //如微店logo是png图则先弄一个白色背景合成，便免下面透明图与二维码中间logo合成时重叠
                if (Path.GetExtension(vshop.WXLogo) == ".png")
                {
                    //中间先合成白色背景
                    System.Drawing.Image logobg = System.Drawing.Image.FromFile(HttpContext.Current.Server.MapPath("~/Images/default.png"));

                    GraphicsPath gpbg = new GraphicsPath();//转换成圆形图片
                    gpbg.AddEllipse(new Rectangle(0, 0, logobg.Width, logobg.Width));
                    Bitmap Tlogobg = new Bitmap(logobg.Width, logobg.Width);
                    using (Graphics gl = Graphics.FromImage(Tlogobg))
                    {
                        gl.SetClip(gpbg);//假设bm就是你要绘制的正方形位图，已创建好
                        gl.DrawImage(logobg, 0, 0, logobg.Width, logobg.Width);
                    }
                    logobg.Dispose();

                    Qrimage = CombinImage(Qrimage, Tlogobg, 135); //二维码图片

                    Qrimage.Save(strurl, ImageFormat.Png);
                }


                System.Drawing.Image logoimg = System.Drawing.Image.FromFile(HttpContext.Current.Server.MapPath(vshop.WXLogo));
                //转换成圆形图片
                GraphicsPath gp = new GraphicsPath();
                gp.AddEllipse(new Rectangle(0, 0, logoimg.Width, logoimg.Width));
                Bitmap Tlogoimg = new Bitmap(logoimg.Width, logoimg.Width);
                using (Graphics gl = Graphics.FromImage(Tlogoimg))
                { //假设bm就是你要绘制的正方形位图，已创建好
                    gl.SetClip(gp);
                    gl.DrawImage(logoimg, 0, 0, logoimg.Width, logoimg.Width);
                }
                logoimg.Dispose();

                Qrimage = CombinImage(Qrimage, Tlogoimg, 135); //二维码图片

                Qrimage.Save(strurl, ImageFormat.Png);
                Qrimage.Dispose();
                #endregion

                //如部署了oss，把本地生成图更新到oss上
                if (Core.HimallIO.GetHimallIO().GetType().FullName.Equals("Himall.Strategy.OSS"))
                {
                    Stream stream = File.OpenRead(strurl);
                    HimallIO.CreateFile(vcreatefilename, stream, FileCreateType.CreateNew);//更新到oss上
                }

                return vcreatefilename;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return "";
            }

        }

        /// <summary>
        /// 流转换为字节
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] StreamToBytes(Stream stream)
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

        /// <summary>
        /// 合成二维码图片
        /// </summary>
        /// <param name="imgBack"></param>
        /// <param name="img"></param>
        /// <returns></returns>
        public static Bitmap CombinImage(Bitmap QRimg, System.Drawing.Image Logoimg, int logoW)
        {
            Bitmap tbmp = new Bitmap(QRimg.Width + 20, QRimg.Height + 20);
            Graphics g = Graphics.FromImage(tbmp);
            g.Clear(System.Drawing.Color.White);
            g.DrawImage(QRimg, 10, 10, QRimg.Width, QRimg.Height);
            g.DrawImage(Logoimg, (tbmp.Width - logoW) / 2, (tbmp.Height - logoW) / 2, logoW, logoW);
            return tbmp;
        }
        #endregion
    }
}
