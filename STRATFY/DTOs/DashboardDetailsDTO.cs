// STRATFY.DTOs/DashboardDataDTOs.cs
using System;
using System.Collections.Generic;

namespace STRATFY.DTOs
{
    public class DashboardDetailsDTO
    {
        public int Id { get; set; }
        public string Descricao { get; set; }
        public string ExtratoNome { get; set; }
        public IEnumerable<MovimentacaoDTO> Movimentacoes { get; set; }
        public IEnumerable<GraficoDTO> Graficos { get; set; }
        public IEnumerable<CartaoDTO> Cartoes { get; set; }
    }

    public class MovimentacaoDTO
    {
        public DateOnly? DataMovimentacao { get; set; } // <<<<<<--- CORRIGIDO PARA DateOnly
        public string Descricao { get; set; }
        public decimal Valor { get; set; }
        public string Tipo { get; set; }
        public string Categoria { get; set; }
    }

    public class GraficoDTO
    {
        public string Titulo { get; set; }
        public string Campo1 { get; set; }
        public string Campo2 { get; set; }
        public string Tipo { get; set; }
        public string Cor { get; set; }
        public bool AtivarLegenda { get; set; }
    }

    public class CartaoDTO
    {
        public string Nome { get; set; }
        public string Campo { get; set; }
        public string TipoAgregacao { get; set; }
        public string Cor { get; set; }
    }
}