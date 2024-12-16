using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto
{
    public class CreateBusinessDto
    {
        [Required]
        [StringLength(255, ErrorMessage = "Name cannot exceed 255 characters.")]
        public string Name { get; set; }

        [Required]
        [Phone(ErrorMessage = "Invalid phone number format.")]
        [StringLength(30, ErrorMessage = "Phone number cannot exceed 30 characters.")]
        public string Phone { get; set; }

        [Required]
        [EmailAddress(ErrorMessage = "Invalid email address format.")]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Street address cannot exceed 255 characters.")]
        public string Street { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "City cannot exceed 255 characters.")]
        public string City { get; set; }

        [StringLength(255, ErrorMessage = "Region cannot exceed 255 characters.")]
        public string Region { get; set; }

        [Required]
        [StringLength(255, ErrorMessage = "Country cannot exceed 255 characters.")]
        public string Country { get; set; }
    }

}
