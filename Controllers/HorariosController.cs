using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Org.BouncyCastle.Asn1.Cms;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HorariosController:ControllerBase{
    private readonly DataContext context;
    private readonly int IdUsuario; 
    public HorariosController(DataContext context, IHttpContextAccessor accessor){
        this.context = context;
        IdUsuario = Parsear(accessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);    
    }

    private int Parsear(string? value){
        if(value != null){
            return Int32.Parse(value);
        }
        return 0;
    }


    //Definir horarios para las canchas, esto luego lo va a hacer una sola alta de canchas
    //se crearia la cancha, y los horarios desde y hasta para cada dia
    [AllowAnonymous]
    [HttpPost("{idCancha}")]
    public IActionResult DefinirHorariosCanchas(int idCancha, [FromForm] HorariosDisponible horariosDisponible){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha != null){
            horariosDisponible.CanchaId = idCancha;
            context.HorariosDisponible.Add(horariosDisponible);
            context.SaveChanges();
            return Ok("Horarios definidos correctamente");
        }
        return BadRequest("La cancha no existe");
    }

    [AllowAnonymous]
    [HttpPost]
    public IActionResult DefinirJornadaLaboralDiaria([FromForm] Horarios horario){
        var yaExiste = context.Horarios.FirstOrDefault(h => h.HoraInicio.Equals(horario.HoraInicio) && h.HoraFin.Equals(horario.HoraFin));
        if(yaExiste == null){
            context.Horarios.Add(horario);
            context.SaveChanges();
            return Ok("Jornada laboral diaria creada correctamente");
        }
        return BadRequest("El horario ya existe");
    }

    [HttpGet("{idCancha}")]
    public IActionResult HorariosDesdeYHastaPorCancha(int idCancha, [FromQuery] DateTime fecha){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha == null){
            return BadRequest("La cancha no existe");
        }
        var horariosDisponible = context.HorariosDisponible.FirstOrDefault(h => h.CanchaId == idCancha && h.DiaSemanal.Equals(fecha.DayOfWeek.ToString()));
        if(horariosDisponible == null){
            return BadRequest("No hay horarios para este dia");
        }
        var horarios = context.Horarios.FirstOrDefault(h => h.Id == horariosDisponible.HorariosId);
        return Ok(horarios);
    }

    [HttpGet("horariosInicio/{idCancha}")]
    public IActionResult HorariosInicio(int idCancha, [FromQuery] DateTime fecha, [FromQuery] bool editar){
        //Consulta: para saber los turnos que tiene una cancha x día
        var turnosPorDia = context.Turno.Where(t => t.CanchaId == idCancha && t.FechaInicio.Date == fecha.Date).ToList();
        //Consulta: para saber los horarios disponible que tiene la cancha x día
        var horariosDisponibles = context.HorariosDisponible.FirstOrDefault(h => h.CanchaId == idCancha && h.DiaSemanal == fecha.DayOfWeek.ToString());
        if(horariosDisponibles == null){
            return BadRequest("No hay horarios para este día");
        }
        //Consulta: para saber los el horario desde y hasta que tiene la cancha x día
        var horario = context.Horarios.FirstOrDefault(h => h.Id == horariosDisponibles.HorariosId);
        if(horario == null){
            return BadRequest("No hay horarios para este día");
        }
        var horaActual = TimeOnly.FromDateTime(DateTime.Now);
        var horaInicio = horario.HoraInicio;
        //si la hora actual es mayor a la hora de fin del ultimo turno disponible para la cancha, devolvemos vacio directamente
        if(horaActual > horario.HoraFin.AddHours(-1) && fecha.Date == DateTime.Now.Date){
            return Ok(new ArrayList());
        }
        //Chequeamos lo sig
        if(horaInicio <= horaActual && fecha.Date == DateTime.Now.Date){
            if(!(horaActual.Minute == 0 && horaActual.Second == 0)){
                horaInicio = new TimeOnly(horaActual.AddHours(1).Hour, 0, 0);
            }else{
                horaInicio = horaActual;
            }
        }
        var horaFin = horario.HoraFin;
        if(editar){
            //Consulta: todos los turnos que tiene la cancha x dia pero sin tener en cuenta
            // el horario de inicio que tiene la fecha a editar... 
            var horaInicioEditar = fecha.Hour;
            Console.WriteLine($"Hora inicio editar {horaInicioEditar}");
            turnosPorDia = context.Turno.Where(t => t.CanchaId == idCancha && t.FechaInicio.Date == fecha.Date && t.FechaInicio.Hour != horaInicioEditar).ToList();
        }
        var horarios = new ArrayList();
        int diferenciaHorarios = Math.Abs(horaInicio.Hour-horaFin.Hour);
        for(int i = 0; i < diferenciaHorarios; i++){
            var horaIteracion = horaInicio.AddHours(i);
            //Consulta: si hay algun turno con la hora de inicio desde que arranca la jornada hasta que termina...
            bool hayTurno = turnosPorDia.Any(t => 
            horaIteracion >= TimeOnly.FromDateTime(t.FechaInicio) && horaIteracion < TimeOnly.FromDateTime(t.FechaFin) && (t.Estado == 1 || t.Estado == 2));
            if(!hayTurno){
                horarios.Add(horaInicio.AddHours(i));
            }
        }
        return Ok(horarios);
    }

    [HttpGet("horariosFin/{idCancha}")]
    public IActionResult HorariosFin(int idCancha, [FromQuery] DateTime fecha, [FromQuery] TimeOnly horaInicio, [FromQuery] bool editar, [FromQuery] int idTurno){
        //Consulta: para saber los turnos que tiene una cancha x día
        var turnosPorDia = context.Turno.Where(t => t.CanchaId == idCancha && t.FechaInicio.Date == fecha.Date && t.Estado == 1).ToList();
        //Consulta: para saber los horarios disponibles que tiene una cancha x día
        var horariosDisponibles = context.HorariosDisponible.FirstOrDefault(h => h.CanchaId == idCancha && h.DiaSemanal == fecha.DayOfWeek.ToString());
        if(horariosDisponibles == null){
            return BadRequest("No hay horarios para este día");
        }
        //Consulta: para saber los horarios desde y hata que tiene una cancha x día
        var horario = context.Horarios.FirstOrDefault(h => h.Id == horariosDisponibles.HorariosId);
        if(horario == null){
            return BadRequest("No hay horarios para este día");
        }
        var horaFin = horario.HoraFin;
        var horarios = new List<TimeOnly>();
        var diferenciaHorarios = Math.Abs(horaInicio.Hour - horaFin.Hour);
        for(int i = 1; i <= diferenciaHorarios; i++){
            var horaFinalizacion = horaInicio.AddHours(i);
            //Consulta: para saber los horarios desde y hata que tiene una cancha x día
            // LOGICA horaInicio < turno.HoraFin && horaFin > turno.HoraInicio;
            bool hayTurno = turnosPorDia.Any(t => horaInicio < TimeOnly.FromDateTime(t.FechaFin) && horaFinalizacion > TimeOnly.FromDateTime(t.FechaInicio) && t.Estado == 1);
            if(editar){
                hayTurno = turnosPorDia.Any(t => horaInicio < TimeOnly.FromDateTime(t.FechaFin) && horaFinalizacion > TimeOnly.FromDateTime(t.FechaInicio) && t.Estado == 1 && t.Id != idTurno);
            }
            if(!hayTurno){
                horarios.Add(horaFinalizacion);
            }
        }
        return Ok(horarios);
    }
}