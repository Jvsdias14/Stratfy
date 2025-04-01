using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Movimentacao
{
    public int Id { get; set; }

    public int ExtratoId { get; set; }

    public int CategoriaId { get; set; }

    public string? Descricao { get; set; }

    public string Tipo { get; set; } = null!;

    public decimal Valor { get; set; }

    public DateOnly DataMovimentacao { get; set; }

    public virtual Categoria Categoria { get; set; } = null!;

    public virtual Extrato Extrato { get; set; } = null!;
}
