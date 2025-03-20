using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Extrato
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Nome { get; set; } = null!;

    public DateOnly DataCriacao { get; set; }

    public virtual ICollection<Dashboard> Dashboards { get; set; } = new List<Dashboard>();

    public virtual ICollection<Movimentacao> Movimentacaos { get; set; } = new List<Movimentacao>();

    public virtual Usuario Usuario { get; set; } = null!;
}
