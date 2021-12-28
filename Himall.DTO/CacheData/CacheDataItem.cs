using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Himall.DTO.CacheData
{
    public class CacheDataItem<T>
    {
        public DateTime Time { get; set; }
        public T Data { get; set; }
    }
}
