using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STRATFY.Models;

public partial class Extrato
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Usuario")]
    public int UsuarioId { get; set; }
    public virtual Usuario? Usuario { get; set; } 

    public string Nome { get; set; } = null!;

    public DateOnly DataCriacao { get; set; }

    public virtual ICollection<Dashboard> ?Dashboards { get; set; }


    public virtual ICollection<Movimentacao> ?Movimentacaos { get; set; } 

}
