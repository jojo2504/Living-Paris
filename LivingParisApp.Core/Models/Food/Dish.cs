using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LivingParisApp.Core.Models.Human;
using LivingParisApp.Core.Models.OrderInfo;

namespace LivingParisApp.Core.Models.Food {
    public class Dish {
        [Key]
        public int DishID { get; set; }

        [Required]
        public int ChefID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        [Required]
        [StringLength(255)]
        public string Type { get; set; } // Should be 'entree', 'main dish', or 'dessert'

        [Required]
        [Range(0, double.MaxValue)]
        public decimal DishPrice { get; set; }

        [Required]
        public DateTime FabricationDate { get; set; }

        [Required]
        public DateTime PeremptionDate { get; set; }

        [StringLength(255)]
        public string? Diet { get; set; }

        [StringLength(255)]
        public string? Origin { get; set; }

        public virtual string ChefName { get; set; }
    }
}