using Himall.Core;
using Himall.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Himall.Application
{
   public class PhotoSpaceApplication : BaseApplicaion<PhotoSpaceService>
    {
        public static void DeletePhotoCategory(long categoryId, long shopId, string type) {
            if (type == "2") { //彻底删除图片
                var photospacelist = Service.GetPhotoSpaceByCategoryId(categoryId);
                foreach (var photo in photospacelist) {
                    var filepath = HttpContext.Current.Server.MapPath(photo.PhotoPath);
                    Log.Info(filepath);
                    File.Delete(filepath);
                };
                var ids = photospacelist.Select(p => p.Id);
                Service.DeletePhoto(ids);//删除图片
            }
            Service.DeletePhotoCategory(categoryId,shopId);//删除文件夹
        }
    }
}
