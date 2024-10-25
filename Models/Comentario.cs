public class Comentario{
    public int Id {get; set;}
    public int UsuarioId {get; set;}
    public Usuario usuario {get; set;}
    public int CanchaId {get; set;}
    public Cancha cancha {get; set;}
    public int Calificacion {get; set;}
    public string Descripcion {get; set;}
    public DateTime Fecha {get; set;}
}