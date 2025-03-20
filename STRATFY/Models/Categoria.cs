using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Categoria
{
    public int Id { get; set; }

    public string Nome { get; set; } = null!;

    public virtual ICollection<Movimentacao> Movimentacaos { get; set; } = new List<Movimentacao>();
}
