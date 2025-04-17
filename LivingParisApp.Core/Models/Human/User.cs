using System.ComponentModel.DataAnnotations;

namespace LivingParisApp.Core.Models.Human {
    public class User {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(255)]
        public string LastName { get; set; }

        [Required]
        [StringLength(255)]
        public string FirstName { get; set; }

        [Required]
        [StringLength(255)]
        public string Street { get; set; }

        [Required]
        public int StreetNumber { get; set; }

        [Required]
        [StringLength(5)]
        public string Postcode { get; set; }

        [Required]
        [StringLength(255)]
        public string City { get; set; }

        [Required]
        [StringLength(10, MinimumLength = 10)]
        public string PhoneNumber { get; set; }

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Mail { get; set; }

        [StringLength(255)]
        public string ClosestMetro { get; set; }

        [Required]
        [StringLength(50)]
        public string Password { get; set; }

        [Required]
        public int IsClient { get; set; }

        [Required]
        public int IsChef { get; set; }

        // Computed property for FullName
        public string FullName => $"{FirstName} {LastName}".Trim();

        // Computed property for Roles
        public string Roles {
            get {
                var roles = new List<string>();
                if (IsClient == 1) roles.Add("Client");
                if (IsChef == 1) roles.Add("Chef");
                return string.Join(", ", roles);
            }
        }
    }
}