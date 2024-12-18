using System.ComponentModel.DataAnnotations.Schema;

public class Cancha{
    public int Id {get; set;}
    public string Nombre {get; set;}
    public int CapacidadTotal {get; set;}
    public string TipoDePiso {get; set;}
    public string? Imagen {get; set;}
    public decimal PrecioPorHora {get; set;}
    public string Descripcion {get; set;}
    public decimal PorcentajeCalificacion{get; set;} = 5;
    public int Estado {get; set;}
    //1 disponible
    //2 dada de baja
    //3 en mantenimiento
}