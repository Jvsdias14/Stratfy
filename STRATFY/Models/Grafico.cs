using System;
using System.Collections.Generic;

namespace STRATFY.Models;

public partial class Grafico
{
    public int Id { get; set; }

    public int DashboardId { get; set; }

    public string Titulo { get; set; } = null!;

    public string Tipo { get; set; } = null!;

    public string Campo1 { get; set; } = null!;

    public string Campo2 { get; set; } = null!;

    public string Cor { get; set; } = null!;

    public bool AtivarLegenda { get; set; }

    public virtual Dashboard Dashboard { get; set; } = null!;
}
