namespace STRATFY.Models
{
    public class MovimentacaoLoteViewModel
    {
        public int ExtratoId { get; set; }
        public string NomeExtrato { get; set; } = string.Empty;
        public List<Movimentacao> Movimentacoes { get; set; } = new List<Movimentacao>();
    }
}
