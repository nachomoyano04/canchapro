using System.Collections;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

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
    public IActionResult HorariosInicio(int idCancha, [FromQuery] DateTime fecha, [FromQuery] TimeOnly horaInicio, [FromQuery] TimeOnly horaFin){
        var turnosPorDia = context.Turno.Where(t => t.CanchaId == idCancha && t.UsuarioId == IdUsuario && t.FechaInicio.Date == fecha.Date).ToList();
        var horarios = new ArrayList();
        int diferenciaHorarios = Math.Abs(horaInicio.Hour-horaFin.Hour);
        for(int i = 0; i < diferenciaHorarios; i++){
            // bool hayTurno = turnosPorDia.Any(t => TimeOnly.FromDateTime(t.FechaInicio) <= horaInicio.AddHours(i) && TimeOnly.FromDateTime(t.FechaInicio) >= horaInicio.AddHours(i)); 
             bool hayTurno = turnosPorDia.Any(t => 
            (TimeOnly.FromDateTime(t.FechaInicio) < horaInicio.AddHours(i) && TimeOnly.FromDateTime(t.FechaFin) > horaInicio.AddHours(i))
            || (TimeOnly.FromDateTime(t.FechaInicio) < horaInicio.AddHours(i).AddMinutes(30) && TimeOnly.FromDateTime(t.FechaFin) > horaInicio.AddHours(i).AddMinutes(30))
            );
            if(!hayTurno){
                horarios.Add(horaInicio.AddHours(i));
                horarios.Add(horaInicio.AddHours(i).AddMinutes(30));
            }
        }
        return Ok(horarios);
    }

    [HttpGet("horariosFin/{idCancha}")]
    public IActionResult HorariosFin(int idCancha, [FromQuery] DateTime fecha, [FromQuery] TimeOnly horaInicio, [FromQuery] TimeOnly horaFin){
        var turnosPorDia = context.Turno.Where(t => t.UsuarioId == IdUsuario && t.CanchaId == idCancha && t.FechaInicio.Date == fecha.Date).ToList();
        var horarios = new List<TimeOnly>();
        var diferenciaHorarios = Math.Abs(horaInicio.Hour - horaFin.Hour);
        for(int i = 1; i < diferenciaHorarios; i++){
            var hora = horaInicio.AddHours(i);
            var horaYMedia = hora.AddMinutes(30);
            // bool hayTurno = turnosPorDia.Any(t => (hora > TimeOnly.FromDateTime(t.FechaInicio) && hora < TimeOnly.FromDateTime(t.FechaFin)) 
            // || (horaYMedia > TimeOnly.FromDateTime(t.FechaInicio) && horaYMedia < TimeOnly.FromDateTime(t.FechaFin)));
             bool hayTurno = turnosPorDia.Any(t => 
            // Verifica si el intervalo propuesto se solapa con un turno existente
            (hora > TimeOnly.FromDateTime(t.FechaInicio) && hora < TimeOnly.FromDateTime(t.FechaFin)) ||
            (horaYMedia > TimeOnly.FromDateTime(t.FechaInicio) && horaYMedia < TimeOnly.FromDateTime(t.FechaFin)) ||
            // Si la horaYMedia coincide exactamente con el fin de un turno, no la consideramos disponible
            (horaYMedia == TimeOnly.FromDateTime(t.FechaFin))
            );
            if(!hayTurno){
                horarios.Add(hora);
                horarios.Add(horaYMedia);
            }
        }
        return Ok(horarios);
    }
}