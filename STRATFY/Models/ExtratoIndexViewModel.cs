namespace STRATFY.Models
{
    public class ExtratoIndexViewModel
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        public DateOnly DataCriacao { get; set; }
        public bool IsFavorito { get; set; }
        public DateOnly? DataInicioMovimentacoes { get; set; }
        public DateOnly? DataFimMovimentacoes { get; set; }
        public int TotalMovimentacoes { get; set; }
    }

}
