using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Foods.Models
{
    public class OrderDetails
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        [ForeignKey("OrderId")]
        public virtual OrderHeader OrderHeader { get; set; }

        [Required]
        public int MenuItemId { get; set; }

        [ForeignKey("MenuItemId")]
        public virtual MenuItem MenuItem { get; set; }

        public int Count { get; set; }

        // You can get these properties from menuItem table but we don't want if the admin change any thing of these
        public string Name { get; set; }
        public string Description { get; set; }

        [Required]
        public double Price { get; set; }

    }
}
