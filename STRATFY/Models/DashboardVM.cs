using Microsoft.AspNetCore.Mvc.Rendering;
using STRATFY.Models;

public class DashboardVM
{
    public string Nome { get; set; }
    public int ExtratoId { get; set; }
    public List<SelectListItem> ExtratosDisponiveis { get; set; }
    public List<Grafico> Graficos { get; set; } = new();
    public List<Cartao> Cartoes { get; set; } = new();
}

