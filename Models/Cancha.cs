using System.ComponentModel.DataAnnotations.Schema;

public class Cancha{
    public int Id {get; set;}
    public int TipoId {get; set;}
    [ForeignKey("TipoId")]
    public Tipo tipo {get; set;}
    public string Coordenadas {get; set;}
    //1 disponible
    //2 en refacccion
    //3 fuera de servicio
    public int Estado {get; set;}
}