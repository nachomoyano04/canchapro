using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
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
                // .ThenInclude(t => t.Tipo).ToList();
                .ToList();
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

    [HttpGet("pendientes")]//turnos que vienen a partir de ahora y que no han sido cancelados, y que el pago de la reserva se ha completado  (estado = 2)
    public IActionResult MisProximosTurnos(){
        // var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaInicio >= DateTime.Now && t.Estado == 1 && t.Pago.Estado == 2).Include(t => t.Pago).Include(t => t.Cancha).ThenInclude(c => c.Tipo).OrderBy(t => t.FechaInicio).ToList();
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaInicio >= DateTime.Now && t.Estado == 1 && t.Pago.Estado == 2).Include(t => t.Pago).Include(t => t.Cancha).OrderBy(t => t.FechaInicio).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("estado/{estado}")]
    public IActionResult TurnosPorUsuarioYEstado(int estado){
        // var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.Estado == estado).Include(t => t.Pago).Include(t => t.Cancha).ThenInclude(c => c.Tipo).OrderByDescending(t => t.FechaCancelacion).ToList();
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.Estado == estado).Include(t => t.Pago).Include(t => t.Cancha).OrderByDescending(t => t.FechaCancelacion).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
    }

    [HttpGet("completados")]//turnos que han sido completados
    public IActionResult TurnosPasadosCumplidos(){
        // var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaFin <= DateTime.Now && t.Estado == 2).Include(t => t.Pago).Include(t => t.Cancha).ThenInclude(t => t.Tipo).ToList();
        var turnos = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.FechaFin <= DateTime.Now && t.Estado == 2).Include(t => t.Pago).Include(t => t.Cancha).ToList();
        if(turnos.Count > 0){
            return Ok(turnos);
        }
        return NoContent();
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
            var horarios = context.Horarios.FirstOrDefault(h => h.CanchaId == idCancha && h.DiaSemanal == diaSemanal);
            if(horarios == null){
                return BadRequest($"La cancha {idCancha} no esta disponible ese día ");
            }
            using(var transaccion = context.Database.BeginTransaction()){
                try{
                    bool existeTurno = context.Turno.Any(t => t.CanchaId == idCancha && t.FechaInicio == turno.FechaInicio 
                                        && t.FechaFin == turno.FechaFin && (t.Estado == 1 || t.Estado == 2));
                    if(!existeTurno){
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
                        transaccion.Commit();
                        //El pago de la reserva esta en 1. Lo que significa que no se confirma aún...
                        return Ok($"CONFIRMAREMOS SU RESERVA POR ${pago.MontoReserva} EN BREVE...");
                    }else{
                        return Conflict();
                    }
                }catch(System.Exception){
                    transaccion.Rollback();
                    return Conflict();
                }
            }
        }
        return BadRequest("La cancha no existe.");
    }

    [HttpPatch("cancelar/{idTurno}")]
    public IActionResult CancelarTurno(int idTurno, [FromQuery] string montoReintegro){
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
                var pago = context.Pago.FirstOrDefault(p => p.Id == turno.PagoId);
                if(pago != null){
                    decimal montoDevolucion = decimal.Parse(montoReintegro);
                    turno.FechaCancelacion = DateTime.Now;
                    pago.MontoReintegroTurnoCancelado = montoDevolucion;
                    turno.Estado = 3; 
                    context.SaveChanges();
                    return Ok("Turno cancelado");
                }
                return BadRequest("No existe el pago");
            }
            return Accepted();
        }
        return BadRequest("El turno no fue encontrado");
    }

    [HttpPatch("comentario/{idTurno}")]
    public IActionResult Comentario(int idTurno, [FromForm] int calificacion, [FromForm] string? comentario){
        var turno = context.Turno.FirstOrDefault(t => t.Id == idTurno && t.UsuarioId == IdUsuario && t.Estado == 2);
        if(turno == null){
            return BadRequest("Turno no existe o incompleto o cancelado o es de otra persona.");
        }
        if(!comentario.IsNullOrEmpty()){
            turno.Comentario = comentario;
        }
        if(calificacion > 0 && calificacion <= 5){
            bool teniaCalificacion = false;
            if(turno.Calificacion != null){
                teniaCalificacion = true;
            }
            turno.Calificacion = calificacion;
            //si hay una calificacion, cambiamos el porcentaje de calificacion de la cancha...
            var cancha = context.Cancha.FirstOrDefault(c => c.Id == turno.CanchaId);
            if(cancha != null){
                var turnosConCalificacion = context.Turno.Where(t => t.CanchaId == turno.CanchaId && t.Calificacion != null).ToList();
                int cantidadTurnos = turnosConCalificacion.Count();
                int total = 0;
                turnosConCalificacion.ForEach(t => total += (int) t.Calificacion);
                var PorcentajeCalificacion = Decimal.MinValue;
                if(!teniaCalificacion){
                    PorcentajeCalificacion = (decimal)(total + calificacion) / (cantidadTurnos+1);
                }else{
                    PorcentajeCalificacion = (decimal)(total - turno.Calificacion + calificacion) / cantidadTurnos;
                }
                cancha.PorcentajeCalificacion = PorcentajeCalificacion;
            }
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
            bool existeTurno = context.Turno.Any(t => t.CanchaId == turno.CanchaId && t.FechaInicio == horaInicio 
                                && t.FechaFin == horaFin && (t.Estado == 1 || t.Estado == 2));
            if(!existeTurno){
                var pago = context.Pago.FirstOrDefault(p => p.Id == turno.PagoId);
                var cancha = context.Cancha.FirstOrDefault(c => c.Id == turno.CanchaId);
                turno.FechaInicio = horaInicio;
                turno.FechaFin = horaFin;
                var montoReservaPagado = pago.MontoReserva;
                pago.MontoTotal = CalcularMontoTotalTurno(horaInicio, horaFin, cancha.PrecioPorHora);
                pago.MontoReserva = pago.MontoTotal * 10 / 100;
                pago.FechaPagoReserva = DateTime.Now;
                if(pago.MontoReserva > montoReservaPagado){
                    context.SaveChanges();
                    return Ok($"Usted pagó ${montoReservaPagado} ahora como el total de "+
                    $"la reserva es de ${pago.MontoReserva}, debe abonar el resto que es: ${pago.MontoReserva-montoReservaPagado}");
                }else if(pago.MontoReserva == montoReservaPagado){
                    context.SaveChanges();
                    return Ok($"Su turno fue cambiado");
                }else{
                    pago.Creditos = montoReservaPagado-pago.MontoReserva;
                    context.SaveChanges();
                    return Ok($"Usted tiene ${pago.Creditos} a favor para el pago total del turno...");
                }
            }else{
                return Conflict();
            }
        }
        return BadRequest("No encontrado");
    }

    [HttpGet("cancelar/{idTurno}")]
    public IActionResult GetPoliticasDeCancelacion(int idTurno){
        var turno = context.Turno.Where(t => t.Id == idTurno && t.UsuarioId == IdUsuario && t.Estado == 1).Include(t => t.Pago).First();
        if(turno != null){
            var horasRestantesAlTurno = turno.FechaInicio - DateTime.Now.AddMinutes(-2); //Le sumamos 2 minutos de tolerancia
            Console.WriteLine($"Horas restantes al turno: {horasRestantesAlTurno}");
            decimal montoDevolucion = 0;
            if(horasRestantesAlTurno.TotalMinutes >= 1440){
                montoDevolucion = turno.Pago.MontoReserva;
            }else if(horasRestantesAlTurno.TotalMinutes >= 360){
                montoDevolucion = turno.Pago.MontoReserva * 50 / 100;
            }
            string mensaje = "Politicas de cancelación de turnos:" +
                                    "\n-Cancelando 24 horas antes o mas le devolvemos el total de su dinero" +
                                    "\n-Cancelando 6 horas antes o mas le devolvemos el 50% de su dinero" +
                                    "\n-Cancelando faltando menos de 6 horas lamentamos informarle que no se hara devolución de su dinero" +
                                    $"\n Lo que usted recibiría es ${montoDevolucion} porque faltan {Math.Floor(horasRestantesAlTurno.TotalDays)} días " +
                                    $"{horasRestantesAlTurno.Hours} horas y {horasRestantesAlTurno.Minutes} minutos";
            return Ok(new ArrayList{mensaje, montoDevolucion+""});
        }
        return BadRequest("No se encontró el turno");
    }

    public decimal CalcularMontoTotalTurno(DateTime inicio, DateTime fin, decimal porHora){
        var minutosTurno = (int)(fin-inicio).TotalMinutes;
        var horas = minutosTurno / 60;
        return porHora * horas;
    }

    //CONSULTAS PARA GESTION WEB
    [HttpGet("todos/proximos")]
    public IActionResult GetTodosLosProximosTurnos(){
        var turnos = context.Turno.Where(t => t.FechaInicio >= DateTime.Now && t.Pago.Estado == 2).Include(t => t.Pago).Include(t => t.Cancha).Include(t => t.Usuario).ToList();
        return Ok(turnos);
    } 

    [HttpGet("todos/historial")]
    public IActionResult GetHistorialDeTurnos(){
        var turnos = context.Turno.Include(t => t.Pago).Include(t => t.Cancha).Include(t => t.Usuario).ToList();
        return Ok(turnos);
    } 
}