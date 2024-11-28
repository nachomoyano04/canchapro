using System.ComponentModel.DataAnnotations.Schema;

public class Cancha{
    public int Id {get; set;}
    public int TipoId {get; set;}
    [ForeignKey("TipoId")]
    public Tipo? Tipo {get; set;}
    public string? Imagen {get; set;}
    public decimal PrecioPorHora {get; set;}
    public string Descripcion {get; set;}
    //1 disponible
    //2 en refacccion
    //3 fuera de servicio
    public decimal PorcentajeCalificacion{get; set;} = 5;
    public int Estado {get; set;}
}