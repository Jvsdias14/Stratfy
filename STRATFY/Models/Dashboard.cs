using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;

namespace STRATFY.Models;

public partial class Dashboard
{
    public int Id { get; set; }

    public int ExtratoId { get; set; }

    [Required(ErrorMessage = "O campo Nome é de preenchimento obrigatório!")]
    public string Descricao { get; set; } = null!;

    public virtual ICollection<Cartao> Cartoes { get; set; } = new List<Cartao>();

    public virtual Extrato Extrato { get; set; } = null!;

    public virtual ICollection<Grafico> Graficos { get; set; } = new List<Grafico>();
}
