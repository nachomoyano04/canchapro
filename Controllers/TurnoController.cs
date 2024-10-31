using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

    [HttpGet("pendientes")]//turnos que vienen a partir de ahora y que no han sido cancelados
    public IActionResult MisProximosTurnos(){
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaInicio >= DateTime.Now && t.Estado == 1).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("completados")]//turnos que han sido completados
    public IActionResult TurnosPasadosCumplidos(){
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaFin <= DateTime.Now && t.Estado == 2).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("dia/{idCancha}")] //traer todos los horarios que x cancha este sin turnos x día.
    public IActionResult DisponiblesPorDia(int idCancha, [FromForm] DateTime fecha){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha == null){ // chequeamos de que la cancha exista
            return BadRequest("No existe la cancha");
        }
        //paso1: traer los horarios disponibles que tiene esa cancha ese día
        var horariosDisponible = context.HorariosDisponible.FirstOrDefault(h => h.CanchaId == idCancha && h.DiaSemanal == fecha.DayOfWeek.ToString());
        if(horariosDisponible == null){
            return BadRequest($"No existen horarios para la cancha {cancha.Id} en la fecha {fecha.Date.ToShortDateString()}.");
        }
        var horarios = context.Horarios.FirstOrDefault(h => h.Id == horariosDisponible.HorariosId);
        if(horarios == null){
            return BadRequest("El horario no existe");
        }
        //paso2: buscamos los turnos que hay ese día en esa cancha
        var turnosXDia = context.Turno.Where(t => t.CanchaId == idCancha && t.FechaInicio.Date.Equals(fecha.Date) && t.Estado != 3)
            .Select(t => new {horaInicio = t.FechaInicio.Hour, horaFin = t.FechaFin.Hour})
            .ToList();
        //paso3: armamos una lista de horariosDisponibles
        var turnosDisponibles = new List<object>();
        for(int i= horarios.HoraInicio.Hour; i < horarios.HoraFin.Hour; i++){
            bool ocupado = turnosXDia.Any(t => i >= t.horaInicio && i < t.horaFin);
            if(!ocupado){
                turnosDisponibles.Add(new {horaInicio = i, horaFin = i+1});
            }
        }
        if(turnosDisponibles.Count < 1){
            return NoContent();
        }
        return Ok(turnosDisponibles);
    }

    [HttpPost("{idCancha}")]
    public IActionResult NuevoTurno(int idCancha, [FromForm] Turno turno, [FromForm] Pago pago){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha != null){
            if(turno.FechaInicio > turno.FechaFin){
                return BadRequest("La fecha de inicio no puede ser mayor a la fecha de fin");
            }
            var diaSemanal = turno.FechaInicio.DayOfWeek.ToString();
            if(turno.FechaInicio.Date.ToString() != turno.FechaFin.Date.ToString()){
                return BadRequest("Los horarios de inicio y fin deben ser el mismo dia");
            }
            var horariosDisponible = context.HorariosDisponible.FirstOrDefault(h => h.CanchaId == idCancha && h.DiaSemanal == diaSemanal);
            if(horariosDisponible == null){
                return BadRequest($"La cancha {idCancha} no esta disponible ese día ");
            }
            //chequeamos que los horarios del turno la cancha este disponible 
            //(no por otros turnos si no por los horarios de la propia cancha)
            var horarios = context.Horarios.FirstOrDefault(h => h.Id == horariosDisponible.HorariosId);
            if(!(turno.FechaInicio.Hour >= horarios.HoraInicio.Hour && turno.FechaFin.Hour <= horarios.HoraFin.Hour)){
                return BadRequest($"Los horarios de inicio y/o fecha fin no esta disponible la cancha {idCancha}");
            } // DESPUES CUANDO ESTEN LAS VISTAS, SACARLO, PORQUE YA LO HCAE EL METODO DISPONIBLESPORDIA

            pago.MontoTotal = CalcularMontoTotalTurno(turno.FechaInicio, turno.FechaFin, cancha.PrecioPorHora);
            pago.MontoReserva = pago.MontoTotal * 10 / 100;
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
        return BadRequest("La cancha no existe.");
    }

    [HttpPatch("cancelar/{idTurno}")]
    public IActionResult CancelarTurno(int idTurno){
        var turno = context.Turno.FirstOrDefault(t => t.Id == idTurno && t.UsuarioId == IdUsuario);
        if(turno != null){
            switch(turno.Estado){
                case 1: 
                    turno.Estado = 3; 
                    context.SaveChanges();
                    return Ok("Turno cancelado");
                case 2:
                    return Ok("El turno no se puede cancelar porque ya fue completado...");
                case 3:
                    return Ok("El turno ya fue cancelado anteriormente");
            }
        }
        return BadRequest("No encontramos el turno");
    }

    public decimal CalcularMontoTotalTurno(DateTime inicio, DateTime fin, decimal porHora){
        var minutosTurno = (int)(fin-inicio).TotalMinutes;
        var horas = minutosTurno / 60;
        return porHora * horas;
    }

}