using System.ComponentModel.DataAnnotations;

namespace STRATFY.Models
{
    public class LoginVM
    {
        [Required(ErrorMessage = "O campo Email é de preenchimento obrigatório!")]
        [EmailAddress(ErrorMessage = "Informe um Email válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo senha é de preenchimento obrigatório!")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

    }
}
