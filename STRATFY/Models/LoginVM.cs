using System.ComponentModel.DataAnnotations;

namespace STRATFY.Models
{
    public class LoginVM
    {
        [Required(ErrorMessage = "O campo e-mail é de preenchimento obrigatório!")]
        [EmailAddress(ErrorMessage = "Informe um e-mail válido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "O campo senha é de preenchimento obrigatório!")]
        [DataType(DataType.Password)]
        public string Senha { get; set; }

    }
}
