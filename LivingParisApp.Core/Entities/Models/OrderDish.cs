using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivingParisApp.Core.Entities.Models {
    public class OrderDish {
        [Key]
        [Column(Order = 0)]
        public int DishID { get; set; }

        [Key]
        [Column(Order = 1)]
        public int OrderID { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; } = 1;

        [Required]
        public decimal OrderPrice { get; set; }

        [ForeignKey("DishID")]
        public virtual Dish Dish { get; set; }

        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
    }
}