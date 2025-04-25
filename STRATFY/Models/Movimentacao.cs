using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace STRATFY.Models;

public partial class Movimentacao
{
    [Key]
    public int Id { get; set; }

    [ForeignKey("Extrato")]
    public int ExtratoId { get; set; }
    public virtual Extrato ?Extrato { get; set; }

    [Required(ErrorMessage = "O campo Categoria é obrigatório")]
    [ForeignKey("Categoria")]
    public int CategoriaId { get; set; }

    public virtual Categoria ?Categoria { get; set; }

    [Required(ErrorMessage = "O campo Descrição é obrigatório")]
    public string? Descricao { get; set; }

    [Required(ErrorMessage = "O campo Tipo é obrigatório")]
    public string Tipo { get; set; } = null!;

    [Required(ErrorMessage = "O campo Valor é obrigatório")]
    public decimal Valor { get; set; }

    [Required(ErrorMessage = "O campo Data é obrigatório")]
    public DateOnly? DataMovimentacao { get; set; }


}
