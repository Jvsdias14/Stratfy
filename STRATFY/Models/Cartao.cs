using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Cartao
{
    public int Id { get; set; }

    public int DashboardId { get; set; }

    public string Nome { get; set; } = null!;

    public string Campo { get; set; } = null!;

    public string TipoAgregacao { get; set; } = null!;

    public string Cor { get; set; } = null!;

    public virtual Dashboard Dashboard { get; set; } = null!;
}
