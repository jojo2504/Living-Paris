using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LivingParisApp.Core.Models.Food {
    public class DishIngredient {
        [Key]
        [Column(Order = 0)]
        public int DishIngredientsID { get; set; }

        [Key]
        [Column(Order = 1)]
        public int DishID { get; set; }

        [Required]
        public int IngredientID { get; set; }

        public int? Gramme { get; set; }

        public int? Pieces { get; set; }

        [ForeignKey("IngredientID")]
        public virtual Ingredient Ingredient { get; set; }

        [ForeignKey("DishID")]
        public virtual Dish Dish { get; set; }
    }
}