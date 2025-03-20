using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Dashboard
{
    public int Id { get; set; }

    public int ExtratoId { get; set; }

    public string Descricao { get; set; } = null!;

    public virtual ICollection<Carto> Cartos { get; set; } = new List<Carto>();

    public virtual Extrato Extrato { get; set; } = null!;

    public virtual ICollection<Grafico> Graficos { get; set; } = new List<Grafico>();
}
