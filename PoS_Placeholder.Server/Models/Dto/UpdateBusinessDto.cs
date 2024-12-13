using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto
{
    public class UpdateBusinessDto
    {
        [StringLength(255, ErrorMessage = "Name must not exceed 255 characters.")]
        public string? Name { get; set; }

        [Phone(ErrorMessage = "Phone number is invalid.")]
        public string? Phone { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string? Email { get; set; }

        [StringLength(255, ErrorMessage = "Street must not exceed 255 characters.")]
        public string? Street { get; set; }

        [StringLength(255, ErrorMessage = "City must not exceed 255 characters.")]
        public string? City { get; set; }

        [StringLength(255, ErrorMessage = "Region must not exceed 255 characters.")]
        public string? Region { get; set; }

        [StringLength(255, ErrorMessage = "Country must not exceed 255 characters.")]
        public string? Country { get; set; }
    }
}
