using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Usuario
{
    public int Id { get; set; }

    public string? Nome { get; set; }

    public string? Email { get; set; }

    public string? Senha { get; set; }

    public virtual ICollection<Extrato> Extratos { get; set; } = new List<Extrato>();
}
