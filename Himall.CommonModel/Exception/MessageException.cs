using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Himall.Core;
namespace Himall.CommonModel
{
    /// <summary>
    /// 权限验证异常
    /// </summary>
    public class MessageException : HimallException
    {
        public ExceptionMessages MessageStatus { get; set; }

        public object BindData { get; set; }

        public MessageException(string message) : base(message)
        {

        }
        public MessageException(ExceptionMessages message, params string[] labels)
            : base(string.Format(message.ToDescription(), labels))
        {
            this.MessageStatus = message;
        }

        public void SetData(object data)
        {
            this.BindData = data;
        }
    }

}
