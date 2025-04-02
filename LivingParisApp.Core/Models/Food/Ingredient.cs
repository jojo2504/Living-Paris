using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivingParisApp.Core.Models.Food {
    public class Ingredient {
        [Key]
        public int IngredientID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }

        // Navigation property
        public virtual ICollection<DishIngredient> DishIngredients { get; set; } = new List<DishIngredient>();
    }
}