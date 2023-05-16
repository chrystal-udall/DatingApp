using System.ComponentModel.DataAnnotations;

namespace API.DTO
{
  public class RegisterDTO
    {
        [Required] // also have phone, regex, string validators, etc
        public string Username { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 4)]
        public string Password { get; set; }
    }
}