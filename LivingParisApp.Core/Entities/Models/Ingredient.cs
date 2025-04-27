using System.ComponentModel.DataAnnotations;

namespace LivingParisApp.Core.Entities.Models {
    public class Ingredient {
        [Key]
        public int IngredientID { get; set; }

        [Required]
        [StringLength(255)]
        public string Name { get; set; }
    }
}