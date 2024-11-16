using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

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

    [HttpGet]
    public IActionResult TurnosPorUsuario(){ 
        // turnos completados o turno cancelados de un usuario
        var turnos = context.Turno.
                Where(t => t.UsuarioId == IdUsuario && t.Estado == 2 || t.Estado == 3)
                .Include(t => t.Pago)
                .Include(t => t.Cancha)
                .ThenInclude(t => t.Tipo).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
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
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaInicio >= DateTime.Now && t.Estado == 1).Include(t => t.Cancha).ThenInclude(c => c.Tipo).OrderBy(t => t.FechaInicio).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("estado/{estado}")]
    public IActionResult TurnosPorUsuarioYEstado(int estado){
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.Estado == estado).Include(t => t.Pago).Include(t => t.Cancha).ThenInclude(c => c.Tipo).OrderBy(t => t.FechaInicio).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("completados")]//turnos que han sido completados
    public IActionResult TurnosPasadosCumplidos(){
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaFin <= DateTime.Now && t.Estado == 2).Include(t => t.Pago).Include(t => t.Cancha).ThenInclude(t => t.Tipo).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("dia/{idCancha}/{fecha}")] //traer todos los horarios que x cancha este sin turnos x día.
    public IActionResult DisponiblesPorDia(int idCancha, DateTime fecha){
        Console.WriteLine($"Fecha {fecha}");
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
            .ToList();
        return Ok(turnosXDia);
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
            turno.Comentario = null;
            turno.Calificacion = null;
            turno.FechaComentario = null;
            context.Turno.Add(turno);
            context.SaveChanges();
            return Ok($"Total de la reserva del pago: ${pago.MontoReserva}");
        }
        return BadRequest("La cancha no existe.");
    }

    [HttpPatch("cancelar/{idTurno}")]
    public IActionResult CancelarTurno(int idTurno){
        var turno = context.Turno.FirstOrDefault(t => t.Id == idTurno && t.UsuarioId == IdUsuario);
        if(turno != null){
            //chequeamos que el turno se cancele como maximo 1 hora antes...
            var minutosRestantes = turno.FechaInicio.Subtract(DateTime.Now).TotalMinutes; 
            if(minutosRestantes <= 0){
                return BadRequest("El turno ya paso...");
            }
            if(turno.Estado != 1){
                return BadRequest("El turno ya fue completado o cancelado");
            }
            if(minutosRestantes > 60){
                turno.Estado = 3; 
                context.SaveChanges();
                return Ok("Turno cancelado");
            }
            return Accepted();
        }
        return BadRequest("El turno no fue encontrado");
    }

    [HttpPatch("comentario/{idTurno}")]
    public IActionResult Comentario(int idTurno, [FromForm] int calificacion, [FromForm] string comentario){
        var turno = context.Turno.FirstOrDefault(t => t.Id == idTurno && t.UsuarioId == IdUsuario && t.Estado == 2);
        if(turno == null){
            return BadRequest("Turno no existe o incompleto o cancelado o es de otra persona.");
        }
        if(!comentario.IsNullOrEmpty()){
            turno.Comentario = comentario;
        }
        if(calificacion > 0 && calificacion <= 5){
            turno.Calificacion = calificacion;
        }
        if(!comentario.IsNullOrEmpty() || (calificacion > 0 && calificacion <= 5)){
            turno.FechaComentario = DateTime.Now;
            context.SaveChanges();
            return Ok("Comentario guardado");
        }
        return BadRequest("Campos requeridos");
    }

    [HttpPatch("editar/{idTurno}")]
    public IActionResult EditarTurno(int IdTurno, [FromForm] DateTime horaInicio, [FromForm] DateTime horaFin){
        var turno = context.Turno.FirstOrDefault(t => t.UsuarioId == IdUsuario && t.Id == IdTurno);
        if(turno != null){
            var pago = context.Pago.FirstOrDefault(p => p.Id == turno.PagoId);
            var cancha = context.Cancha.FirstOrDefault(c => c.Id == turno.CanchaId);
            turno.FechaInicio = horaInicio;
            turno.FechaFin = horaFin;
            pago.MontoTotal = CalcularMontoTotalTurno(horaInicio, horaFin, cancha.PrecioPorHora);
            pago.MontoReserva = pago.MontoTotal * 10 / 100;
            pago.FechaPagoReserva = DateTime.Now;
            context.SaveChanges();
            return Ok("Cambios realizados");
        }
        return BadRequest("No encontrado");
    }

    public decimal CalcularMontoTotalTurno(DateTime inicio, DateTime fin, decimal porHora){
        var minutosTurno = (int)(fin-inicio).TotalMinutes;
        var horas = minutosTurno / 60;
        return porHora * horas;
    }
}