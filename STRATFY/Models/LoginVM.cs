using System.ComponentModel.DataAnnotations;

namespace STRATFY.Models
{
    public class LoginVM
    {
        [Required]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

    }
}
