public class HorariosDisponible{
    public int Id {get; set;}
    public int CanchaId {get; set;}
    public Cancha? Cancha {get; set;}
    public int HorariosId {get; set;}
    public Horarios? Horarios {get; set;}
    public string DiaSemanal {get; set;}
}