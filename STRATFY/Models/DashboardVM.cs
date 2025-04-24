using Microsoft.AspNetCore.Mvc.Rendering;
using STRATFY.Models;
using System.ComponentModel.DataAnnotations;

public class DashboardVM
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O campo Nome é de preenchimento obrigatório!")]
    [StringLength(50, ErrorMessage = "O campo Nome deve ter no máximo 50 caracteres.")]
    [Display(Name = "Nome do Dashboard")]
    [DataType(DataType.Text)]

    public string Nome { get; set; }
    public int ExtratoId { get; set; }

    public List<SelectListItem> ExtratosDisponiveis { get; set; }
    public List<Grafico> Graficos { get; set; } = new();
    public List<Cartao> Cartoes { get; set; } = new();
}
