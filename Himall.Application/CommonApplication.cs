using Himall.Core;
using Himall.Core.Helper;
using Himall.Entities;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.Application
{
   public class CommonApplication
    {
        /// <summary>
        /// 二维码分享
        /// </summary>
        /// <param name="qurl">生成二维码图链接地址</param>
        public static string GetCreateQCode(string qurl)
        {
            if (string.IsNullOrEmpty(qurl))
                return qurl;

            var map = Core.Helper.QRCodeHelper.Create(qurl);
            MemoryStream ms = new MemoryStream();
            map.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            //  将图片内存流转成base64,图片以DataURI形式显示  
            string imgCode = "data:image/gif;base64," + Convert.ToBase64String(ms.ToArray());
            ms.Dispose();

            return imgCode;
        }
    }
}
