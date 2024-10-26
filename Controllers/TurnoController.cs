using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TurnoController:ControllerBase{
    private readonly DataContext context;
    private readonly int IdUsuario;
    public TurnoController(DataContext context, IHttpContextAccessor hca){
        this.context = context;
        IdUsuario = Int32.Parse(hca.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    // turnos disponibles para x cancha
    [HttpGet("{idCancha}")]
    public IActionResult EstaDisponible(int idCancha, [FromForm] DateTime fechaInicio, [FromForm] DateTime fechaFin){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha != null){
            var turnoEnEseHorario = context.Turno.FirstOrDefault(t => t.CanchaId == idCancha && fechaInicio <= t.FechaFin && fechaFin >= t.FechaInicio);
            if(turnoEnEseHorario != null){
                return BadRequest("Ya existe un turno en ese horario");
            }
            return Ok(true);
        }
        return BadRequest("La cancha no existe");
    }

    [HttpPost("{idCancha}")]
    public IActionResult NuevoTurno(int idCancha, [FromForm] Turno turno, [FromForm] Pago pago){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha != null){
            var minutosTurno = (turno.FechaFin-turno.FechaInicio).TotalMinutes;
            //el monto de la reserva es el 10% del precio total del turno
            double precioTotalTurno = Double.Parse(cancha.PrecioPorHora.ToString()) * (minutosTurno/60);
            decimal precioReserva = Decimal.Parse((precioTotalTurno * 10 / 100).ToString());
            pago.MontoReserva = precioReserva;
            pago.MontoTotal = Decimal.Parse(precioTotalTurno.ToString());
            pago.FechaPagoReserva = DateTime.Now;
            pago.FechaPagoTotal = null;
            pago.ComprobanteReserva = ""; //luego pondriamos la ruta al pdf guardado localmente...
            pago.Estado = 1; // ponemos el estado del pago como "pendiente" porque falta el resto...
            context.Pago.Add(pago);
            context.SaveChanges();
            //una vez cargado el pago creamos el turno
            turno.CanchaId = idCancha;
            turno.PagoId = pago.Id;
            turno.UsuarioId = IdUsuario; 
            turno.Estado = 1;
            context.Turno.Add(turno);
            context.SaveChanges();
            return Ok(turno);
        }
        return BadRequest();
    }
}