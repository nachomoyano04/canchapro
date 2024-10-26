using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HorariosController:ControllerBase{
    public readonly DataContext context;
    public HorariosController(DataContext context){
        this.context = context;
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
}