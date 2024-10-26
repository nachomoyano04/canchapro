public class Comentario{
    public int Id {get; set;}
    public int UsuarioId {get; set;}
    public Usuario? Usuario {get; set;}
    public int CanchaId {get; set;}
    public Cancha? Cancha {get; set;}
    public int Calificacion {get; set;}
    public string Descripcion {get; set;}
    public DateTime Fecha {get; set;}
}