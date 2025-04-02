using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivingParisApp.Core.Models.Human;

namespace LivingParisApp.Core.Models.OrderInfo {
    public class Order {
        [Key]
        public int OrderID { get; set; }

        [Required]
        public int ClientID { get; set; }

        [Required]
        public int ChefID { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        [ForeignKey("ClientID")]
        public virtual User Client { get; set; }

        [ForeignKey("ChefID")]
        public virtual User Chef { get; set; }

        // Navigation property
        public virtual ICollection<OrderDish> OrderDishes { get; set; } = new List<OrderDish>();
    }
}