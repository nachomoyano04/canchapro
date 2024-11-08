public class Turno{
    public int Id {get; set;}
    public int CanchaId {get; set;}
    public Cancha? Cancha {get;set;}
    public int UsuarioId {get; set;}
    public Usuario? Usuario {get; set;}
    public int PagoId {get; set;}
    public Pago? Pago {get; set;}
    public DateTime FechaInicio {get; set;}
    public DateTime FechaFin {get; set;}
    public string? Comentario {get; set;}
    public int? Calificacion {get; set;}
    public DateTime? FechaComentario{get; set;}
    public int Estado {get; set;}
    /*  Tipos de estado:
        1. Pendiente
        2.Completado
        3.Cancelado*/
}