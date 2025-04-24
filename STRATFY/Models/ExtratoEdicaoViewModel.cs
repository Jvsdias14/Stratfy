using System.ComponentModel.DataAnnotations;

namespace STRATFY.Models
{
    public class ExtratoEdicaoViewModel
    {
        
        public int ExtratoId { get; set; }

        [Required(ErrorMessage = "O campo Nome do Extrato é obrigatório.")]
        public string NomeExtrato { get; set; } = string.Empty;
        public DateOnly DataCriacao { get; set; }
        public List<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
    }
}
