using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace STRATFY.Models;

public partial class Usuario
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O campo Nome é obrigatório")]
    public string? Nome { get; set; }

    [Required(ErrorMessage = "O campo Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Informe um Email válido")]
    public string? Email { get; set; }

    [StringLength(100, MinimumLength = 6, ErrorMessage = "A senha deve ter entre 6 e 100 caracteres.")]
    [Required(ErrorMessage = "O campo Senha é obrigatório")]
    [DataType(DataType.Password)]
    public string? Senha { get; set; }

    public virtual ICollection<Extrato> Extratos { get; set; } = new List<Extrato>();
}
