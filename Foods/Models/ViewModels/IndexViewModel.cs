using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Foods.Models.ViewModels
{
    public class IndexViewModel
    {
        public IEnumerable<MenuItem> MenuItems { get; set; }
        public IEnumerable<Category> Categories { get; set; }
        public IEnumerable<Coupon> Coupons { get; set; }
    }
}
