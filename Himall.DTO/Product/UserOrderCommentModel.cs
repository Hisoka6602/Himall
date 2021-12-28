using System;

namespace Himall.DTO
{
    public class UserOrderCommentModel
    {

       public long OrderId { set; get; }
      
       
       /// <summary>
       /// 评论时间
       /// </summary>
       public DateTime CommentTime { set; get; }
    }
}
