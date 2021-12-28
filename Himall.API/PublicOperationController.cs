using Himall.Core;
using Himall.Web.Framework;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Himall.API.Model.ParamsModel;

namespace Himall.API
{
    public class PublicOperationController : BaseApiController
    {
        public object UploadPic(PublicOperationUploadPicModel value)
        {
            try
            {
                string picStr = value.picStr;

                byte[] bytes = Convert.FromBase64String(picStr);
                System.IO.MemoryStream ms = new System.IO.MemoryStream(bytes);
                // Image img = System.Drawing.Image.FromStream(ms);

                string filename = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + ".png";
                //   string DirUrl = HttpContext.Current.Server.MapPath("~/temp/");
                //if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                //{
                //    System.IO.Directory.CreateDirectory(DirUrl);
                //}
                //string path = AppDomain.CurrentDomain.BaseDirectory + "/temp/";
                //string returnpath = "/temp/" + filename;
                //img.Save(Path.Combine(path, filename));
                var fname = "/temp/" + filename;
                var ioname = Core.HimallIO.GetImagePath(fname);
                
                // files.Add(ioname);
                try
                {
                    //System.Drawing.Bitmap bitImg = new System.Drawing.Bitmap(100, 100);
                    //bitImg = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(ms);
                    ////bitImg = ResourcesHelper.GetThumbnail(bitImg, 735, 480); //处理成对应尺寸图片
                    ////iphone图片旋转
                    //switch (value.orientation)
                    //{
                    //    case 3: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone); break;
                    //    case 6: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone); break;
                    //    case 8: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone); break;
                    //    default: break;
                    //}
                   
                    //System.Drawing.Imaging.ImageFormat format = System.Drawing.Imaging.ImageFormat.Png;
                    //var ext = Path.GetExtension(filename);
                    //if (ext.ToLower() == ".jpg" || ext.ToLower() == ".jpeg")
                    //{
                    //    format = System.Drawing.Imaging.ImageFormat.Jpeg;
                    //}
                    //else if (ext.ToLower() == ".gif")
                    //{
                    //    format = System.Drawing.Imaging.ImageFormat.Gif;
                    //}
                    //else if (ext.ToLower() == ".bmp")
                    //{
                    //    format = System.Drawing.Imaging.ImageFormat.Bmp;
                    //}
                    ////将图片内容重新加载到内存
                    //bitImg.Save(ms, format);
                    Core.HimallIO.CreateFile(fname, ms, FileCreateType.Create);
                    //Core.HimallIO.CreateFile(fname, ms);
                    //file.SaveAs(Path.Combine(path, filename));
                }
                catch (Exception)
                {

                }

                return new { success = true, Src = ioname, RomoteImage = Core.HimallIO.GetRomoteImagePath(ioname) };
            }
            catch (HimallException he)
            {
                return new { success = false, Msg = he.Message };
            }
            catch (Exception e)
            {
                return new { success = false, Msg = e.Message };
            }
        }
    }
}
