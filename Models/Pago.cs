public class Pago{
    public int Id {get; set;}
    public decimal MontoReserva  {get; set;}
    public decimal MontoTotal  {get; set;}
    public DateTime FechaPagoReserva {get; set;}
    public DateTime? FechaPagoTotal {get; set;}
    public string MetodoPagoReserva {get; set;}
    public string? MetodoPagoTotal {get; set;}
    /*El comprobante reserva sería un campo donde 
    almacenaría un pdf del comprobante de pago con 
    alguna billetera virtual.*/
    public string? ComprobanteReserva{get; set;}
    public decimal? MontoReintegroTurnoCancelado {get; set;}
    public int Estado {get; set;}
    /*tipos de estado del pago:
        1- Pendiente
        2-Completado
        3-Rechazado
        4-Cancelado*/
}