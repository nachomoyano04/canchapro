public class Comentario{
    public int Id {get; set;}
    public int UsuarioId {get; set;}
    public Usuario? Usuario {get; set;}
    public int TurnoId {get; set;}
    public Turno? Turno {get; set;}
    public int Calificacion {get; set;}
    public string Descripcion {get; set;}
    public DateTime Fecha {get; set;}
}