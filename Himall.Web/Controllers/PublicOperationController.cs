using Himall.Application;
using Himall.CommonModel;
using Himall.Core;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Himall.Web.Controllers
{
    public class PublicOperationController : Controller
    {
        /// <summary>
        /// 上传文件的扩展名集合
        /// </summary>
        string[] AllowFileExtensions = new string[] { ".rar", ".zip" };

        /// <summary>
        /// 上传图片文件扩展名集合
        /// </summary>
        string[] AllowImageExtensions = new string[] { ".png", ".jpg", ".jpeg", ".gif", ".bmp" };
        /// <summary>
        /// 视频文件扩展名
        /// </summary>
        string[] AllowVideoFileExtensions = new string[] { ".mp4", ".mov", ".m4v", ".flv", ".x-flv", ".mkv", ".wmv", ".avi", ".rmvb", ".3gp" };
        /// <summary>
        /// 检查图片文件扩展名
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool CheckImageFileType(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            return AllowImageExtensions.Select(x => x.ToLower()).Contains(fileExtension);
        }

        /// <summary>
        /// 检查上传文件的扩展名
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        private bool CheckFileType(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            return AllowFileExtensions.Select(x => x.ToLower()).Contains(fileExtension);
        }
        private bool CheckVideoFileType(string filename)
        {
            var fileExtension = Path.GetExtension(filename).ToLower();
            return AllowVideoFileExtensions.Select(x => x.ToLower()).Contains(fileExtension);
        }
        // GET: PublicOperation
        [HttpPost]
        public ActionResult UploadPic()
        {
            string path = "";
            string filename = "";
            // var maxRequestLength = 15360*1024;
            List<string> files = new List<string>();
            if (Request.Files.Count == 0) return Content("NoFile", "text/html");
            else
            {
                for (var i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (null == file || file.ContentLength <= 0) return Content("格式不正确！", "text/html");
                    //if(Request.ContentLength > maxRequestLength)
                    //{
                    //    return Content("文件大小超出限制！", "text/html");
                    //}
                    Random ra = new Random();
                    filename = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + i
                        + Path.GetExtension(file.FileName);
                    if (!CheckImageFileType(filename))
                    {
                        return Content("上传的图片格式不正确", "text/html");
                    }
                    //string DirUrl = Server.MapPath("~/temp/");
                    //if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                    //{
                    //    System.IO.Directory.CreateDirectory(DirUrl);
                    //}
                    //path = AppDomain.CurrentDomain.BaseDirectory + "/temp/";

                    var fname = "/temp/" + filename;
                    var ioname = Core.HimallIO.GetImagePath(fname);
                    files.Add(ioname);
                    try
                    {
                        //System.Drawing.Bitmap bitImg = new System.Drawing.Bitmap(100, 100);
                        //bitImg = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(file.InputStream);
                        ////bitImg = ResourcesHelper.GetThumbnail(bitImg, 735, 480); //处理成对应尺寸图片
                        ////iphone图片旋转
                        //var orientation = 0;
                        //if (!string.IsNullOrEmpty(Request.Form["hidFileMaxSize"]))
                        //{
                        //    int.TryParse(Request.Form["hidFileMaxSize"], out orientation);
                        //}
                        //switch (orientation)
                        //{
                        //    case 3: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone); break;
                        //    case 6: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone); break;
                        //    case 8: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone); break;
                        //    default: break;
                        //}
                        //path = AppDomain.CurrentDomain.BaseDirectory + "/temp/";
                        //bitImg.Save(Path.Combine(path, filename));

                        Core.HimallIO.CreateFile(fname, file.InputStream, FileCreateType.Create);
                        //file.SaveAs(Path.Combine(path, filename));
                    }
                    catch (Exception ex)
                    {
                        Log.Error("上传文件错误", ex);
                    }
                }
            }
            return Content(string.Join(",", files), "text/html");
        }

        [HttpPost]
        public ActionResult UploadPicToVisual(int tid)
        {
            string path = "";
            string filename = "";
            List<string> files = new List<string>();
            if (Request.Files.Count == 0) return Content("NoFile", "text/html");
            else
            {
                for (var i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (null == file || file.ContentLength <= 0) return Content("格式不正确！", "text/html");

                    Random ra = new Random();
                    filename = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + i
                        + Path.GetExtension(file.FileName);
                    if (!CheckImageFileType(filename))
                    {
                        return Content("上传的图片格式不正确", "text/html");
                    }

                    string DirUrl = Server.MapPath("~/Storage/Plat/PageSettings/Template/" + tid + "/");
                    if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                    {
                        System.IO.Directory.CreateDirectory(DirUrl);
                    }

                    var fname = "/Storage/Plat/PageSettings/Template/" + tid + "/" + filename;
                    var ioname = Core.HimallIO.GetImagePath(fname);
                    files.Add(ioname);
                    try
                    {
                        //System.Drawing.Bitmap bitImg = new System.Drawing.Bitmap(100, 100);
                        //bitImg = (System.Drawing.Bitmap)System.Drawing.Image.FromStream(file.InputStream);
                        ////bitImg = ResourcesHelper.GetThumbnail(bitImg, 735, 480); //处理成对应尺寸图片
                        ////iphone图片旋转
                        //var orientation = 0;
                        //if (!string.IsNullOrEmpty(Request.Form["hidFileMaxSize"]))
                        //{
                        //    int.TryParse(Request.Form["hidFileMaxSize"], out orientation);
                        //}
                        //switch (orientation)
                        //{
                        //    case 3: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate180FlipNone); break;
                        //    case 6: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate90FlipNone); break;
                        //    case 8: bitImg.RotateFlip(System.Drawing.RotateFlipType.Rotate270FlipNone); break;
                        //    default: break;
                        //}
                        //path = AppDomain.CurrentDomain.BaseDirectory + "/Storage/Plat/PageSettings/Template/" + tid + "/";
                        //bitImg.Save(Path.Combine(path, filename));

                        Core.HimallIO.CreateFile(fname, file.InputStream, FileCreateType.Create);
                    }
                    catch (Exception ex)
                    {
                        Log.Error("上传文件错误", ex);
                    }
                }
            }
            return Content(string.Join(",", files), "text/html");
        }
        public ActionResult UploadPicToWeiXin()
        {
            string path = "";
            string filename = "";
            // var maxRequestLength = 15360*1024;
            List<string> files = new List<string>();
            if (Request.Files.Count == 0) return Content("NoFile", "text/html");
            else
            {
                for (var i = 0; i < Request.Files.Count; i++)
                {
                    var file = Request.Files[i];
                    if (null == file || file.ContentLength <= 0) return Content("格式不正确！", "text/html");
                    //if(Request.ContentLength > maxRequestLength)
                    //{
                    //    return Content("文件大小超出限制！", "text/html");
                    //}
                    Random ra = new Random();
                    filename = DateTime.Now.ToString("yyyyMMddHHmmssffffff") + i
                        + Path.GetExtension(file.FileName);
                    if (!CheckImageFileType(filename))
                    {
                        return Content("上传的图片格式不正确", "text/html");
                    }
                    string DirUrl = Server.MapPath("~/temp/");
                    if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                    {
                        System.IO.Directory.CreateDirectory(DirUrl);
                    }
                    path = AppDomain.CurrentDomain.BaseDirectory + "/temp/";
                    string fname = "/temp/" + filename;
                    files.Add(fname);
                    try
                    {
                        //file.SaveAs(Path.Combine(path, filename));

                        Core.HimallIO.CreateFile(fname, file.InputStream, FileCreateType.Create);
                    }
                    catch (Exception)
                    {

                    }
                }
            }
            return Content(string.Join(",", files), "text/html");
        }

        public ActionResult UploadPictures()
        {
            return View();
        }

        [HttpPost]
        public ActionResult UploadFile()
        {
            string strResult = "NoFile";
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];
                if (file.ContentLength == 0)
                {
                    strResult = "文件长度为0,格式异常。";
                }
                else
                {
                    Random ra = new Random();
                    string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ra.Next(1000, 9999)
                         //+ file.FileName.Substring(file.FileName.LastIndexOf("\\") + 1);
                         + Path.GetExtension(file.FileName);
                    if (!CheckFileType(filename))
                    {
                        return Content("上传的文件格式不正确", "text/html");
                    }
                    string DirUrl = Server.MapPath("~/temp/");
                    if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                    {
                        System.IO.Directory.CreateDirectory(DirUrl);
                    }
                    string strfile = filename;
                    try
                    {
                        var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
                        if (opcount == 0)
                        {
                            Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, 1);
                        }
                        else
                        {
                            Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount + 1);
                        }

                        if (Request["hidIsLocal"] == "1")
                        {
                            file.SaveAs(Path.Combine(DirUrl, filename));//特意指定上传到本地(如有些大文件一次使用上传如部署了oss则不需上传到oss上传本地就可)
                        }
                        else
                        {
                            string fname = "/temp/" + filename;
                            Core.HimallIO.CreateFile(fname, file.InputStream, FileCreateType.Create);
                        }

                    }
                    catch (Exception e)
                    {
                        var opcount = Core.Cache.Get<int>(CacheKeyCollection.UserImportOpCount);
                        if (opcount != 0)
                        {
                            Core.Cache.Insert(CacheKeyCollection.UserImportOpCount, opcount - 1);
                        }
                        Core.Log.Error("商品导入上传文件异常：" + e.Message);
                        strfile = "Error";
                    }
                    strResult = strfile;
                }
            }
            return Content(strResult, "text/html");
        }

        [HttpPost]
        public ActionResult UploadVideo()
        {
            string strResult = "NoFile";
            if (Request.Files.Count > 0)
            {
                var file = Request.Files[0];
                if (file.ContentLength == 0)
                {
                    strResult = "文件长度为0,格式异常。";
                }
                else
                {
                    Random ra = new Random();
                    string filename = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ra.Next(1000, 9999)
                         + Path.GetExtension(file.FileName);
                    if (!CheckVideoFileType(filename))
                    {
                        return Content("上传的视频格式不正确", "text/html");
                    }
                    string DirUrl = Server.MapPath("~/temp/");
                    if (!System.IO.Directory.Exists(DirUrl))      //检测文件夹是否存在，不存在则创建
                    {
                        System.IO.Directory.CreateDirectory(DirUrl);
                    }
                    string strfile = "/temp/" + filename;
                    try
                    {
                        //file.SaveAs(Path.Combine(DirUrl, filename));
                        //SaveFile(file, Path.Combine(DirUrl, filename));

                        Core.HimallIO.CreateFile(strfile, file.InputStream, FileCreateType.Create);
                    }
                    catch (Exception e)
                    {
                        strfile = "Error";
                        Log.Error(e);
                    }
                    strResult = Core.HimallIO.GetImagePath(strfile);
                }
            }
            return Content(strResult, "text/html");
        }

        private void SaveFile(HttpPostedFileBase postFile, string DirUrl)
        {
            int saveCount = 0;
            int totalCount = postFile.ContentLength;
            string fileSavePath = DirUrl;

            int readBufferSize = 50;
            byte[] bufferByte = new byte[readBufferSize];

            FileStream fs = new FileStream(fileSavePath, FileMode.Create);
            BinaryWriter bw = new BinaryWriter(fs);
            BinaryReader br = new BinaryReader(postFile.InputStream);

            int readCount = 0;//单次读取的字节数
            while ((readCount = br.Read(bufferByte, 0, readBufferSize)) > 0)
            {
                bw.Write(bufferByte, 0, readCount);//写入字节到文件流
                bw.Flush();
                saveCount += readCount;//已经上传的进度
                Cache.Insert(CacheKeyCollection.CACHE_UPLOADVIDEO("SellerAdmin_UploadSpeed_" + Session.SessionID), Math.Ceiling((saveCount * 1.0 / totalCount) * 100).ToString());
            }
        }
        public ActionResult TestCache()
        {
            string result = "无";
            if (!Himall.Core.Cache.Exists("tt"))
            {
                result = "失效";
                Log.Info("缓存已经失效");
                Himall.Core.Cache.Insert("tt", "zhangsan", 7000);
            }

            return Json(result, JsonRequestBehavior.AllowGet);
        }


        public ActionResult FullDiscount()
        {
            var model = FullDiscountApplication.GetOngoingActiveByProductIds(new long[] { 709, 700, 698, 696, 800, 825 }, 1);

            return Json(model, JsonRequestBehavior.AllowGet);
        }
    }
}