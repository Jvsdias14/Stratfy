using CsvHelper.Configuration.Attributes;

namespace STRATFY.Models // Ajuste o namespace se necessário
{
    public class MovimentacaoCsvModel
    {
        [Name("Data da Movimentação")]
        public string DataMovimentacao { get; set; }

        [Name("Descrição")]
        public string Descricao { get; set; }

        [Name("Valor")]
        public string Valor { get; set; }

        [Name("Tipo")]
        public string Tipo { get; set; }

        [Name("Categoria")]
        public string Categoria { get; set; }
    }
}