public class Horarios{
    public int Id {get; set;}
    public int CanchaId {get; set;}
    public Cancha? Cancha {get; set;}
    public TimeOnly HoraInicio {get; set;}
    public TimeOnly HoraFin {get; set;}
    public string DiaSemanal {get; set;}
}