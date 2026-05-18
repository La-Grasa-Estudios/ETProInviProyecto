using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EtPro.Models;

public class BienMueble
{
    [Key]
    public int ID { get; set; }

    [Required, MaxLength(50)]
    public required string NumeroIdentificacion { get; set; }

    [Required, MaxLength(100)]
    public required string Nombre { get; set; } 

    [MaxLength(50)]
    public string? Marca { get; set; }

    [MaxLength(50)]
    public string? Modelo { get; set; }

    [MaxLength(100)]
    public string? Serial { get; set; } 

    [MaxLength(50)]
    public string? Color { get; set; }

    [MaxLength(50)]
    public string? Material { get; set; }

    [MaxLength(150)]
    public string? ObservacionesAdicionales { get; set; }

    public int Grupo { get; set; } = 2;

    [MaxLength(10)]
    public string? Subgrupo { get; set; }

    [MaxLength(10)]
    public string? Seccion { get; set; }
    public int DependenciaID { get; set; }

    [Column(TypeName = "decimal(18, 2)")]
    public decimal ValorUnitario { get; set; }

    public bool Activo { get; set; } = true;
    public bool Aprobado { get; set; } = false;
    public DateTime? FechaRegistro { get; set; }

    [NotMapped]
    public string DescripcionBM1
    {
        get
        {
            var desc = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrWhiteSpace(Material)) desc.Add($"Material: {Material}");
            if (!string.IsNullOrWhiteSpace(Color)) desc.Add($"Color: {Color}");
            if (!string.IsNullOrWhiteSpace(Marca)) desc.Add($"Marca: {Marca}");
            if (!string.IsNullOrWhiteSpace(Modelo)) desc.Add($"Modelo: {Modelo}");
            if (!string.IsNullOrWhiteSpace(Serial)) desc.Add($"Serial: {Serial}");
            if (!string.IsNullOrWhiteSpace(ObservacionesAdicionales)) desc.Add(ObservacionesAdicionales);

            return string.Join(", ", desc);
        }
    }

}