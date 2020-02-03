using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foods.Models.ViewModels
{
    public class SubCategoryVM
    {
        public List<SubCategory> SubCategories { get; set; }
        public PagingInfo PagingInfo { get; set; }
    }
}
