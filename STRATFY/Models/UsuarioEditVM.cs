using System.ComponentModel.DataAnnotations;

namespace STRATFY.Models
{
    public class UsuarioEditVM
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        [StringLength(100, ErrorMessage = "O nome não pode exceder 100 caracteres.")]
        public string Nome { get; set; }

        [Required(ErrorMessage = "O e-mail é obrigatório.")]
        [EmailAddress(ErrorMessage = "Formato de e-mail inválido.")]
        [StringLength(150, ErrorMessage = "O e-mail não pode exceder 150 caracteres.")]
        public string Email { get; set; }

        // Nova Senha (opcional na edição)
        [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nova Senha")]
        public string? NovaSenha { get; set; }

        // Confirmação da Nova Senha (obrigatória se NovaSenha for preenchida)
        [DataType(DataType.Password)]
        [Display(Name = "Confirme a Nova Senha")]
        [Compare("NovaSenha", ErrorMessage = "A nova senha e a confirmação não coincidem.")]
        public string? ConfirmarNovaSenha { get; set; }
    }
}