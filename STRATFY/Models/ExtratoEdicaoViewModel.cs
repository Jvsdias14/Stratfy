namespace STRATFY.Models
{
    public class ExtratoEdicaoViewModel
    {
        public int ExtratoId { get; set; }
        public string NomeExtrato { get; set; } = string.Empty;
        public DateOnly DataCriacao { get; set; }

        public List<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
    }
}
