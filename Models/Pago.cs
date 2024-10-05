public class Pago{
    public int Id {get; set;}
    public Decimal MontoReserva  {get; set;}
    public Decimal MontoTotal  {get; set;}
    public DateTime FechaPagoReserva {get; set;}
    public DateTime FechaPagoTotal {get; set;}
    public int Estado {get; set;}
}