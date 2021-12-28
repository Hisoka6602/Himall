using PetaPoco;
using System.ComponentModel.DataAnnotations.Schema;

namespace Himall.Entities
{
    public partial class BusinessCategoryInfo
    {
        [ResultColumn]
        public string CategoryName { get; set; }
    }
}
