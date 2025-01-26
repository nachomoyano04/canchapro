using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PagoController:ControllerBase{
    private readonly DataContext context;
    public PagoController(DataContext context){
        this.context = context;
    }

    [AllowAnonymous] //en un futuro lo haria el administrador logueado al update del campo fechaPagoTotal
    [HttpPatch("{idTurno}")]
    public IActionResult CompletarPagoTurno(int idTurno){
        var turno = context.Turno.FirstOrDefault(t => t.Id == idTurno);
        if(turno != null){
            var pago = context.Pago.FirstOrDefault(p => p.Id == turno.PagoId);
            if(pago != null){
                pago.FechaPagoTotal = DateTime.Now;
                pago.Estado = 2; //cambiamos el estado del pago a completado...
                turno.Estado = 2; //completamos tambien el turno!
                context.SaveChanges();
                return Ok("Pago realizado y turno completado...");
            }
        }
        return BadRequest("No se ha encontrado");
    }

    [HttpGet]
    public IActionResult ObtenerPagos(){
        var pagos = context.Pago.ToList();
        return Ok(pagos);
    }

    [HttpGet("{id}")]
    public IActionResult ObtenerPago(int id){
        var pago = context.Pago.FirstOrDefault(p => p.Id == id);
        if(pago == null){
            return BadRequest("No existe el pago");
        }
        return Ok(pago);
    }
}