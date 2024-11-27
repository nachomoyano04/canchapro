using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CanchaController:ControllerBase{
    private readonly DataContext context;
    public CanchaController(DataContext context){
        this.context = context;
    }

    //http://ip:puerto/api/cancha
    [HttpGet]
    public IActionResult GetCanchas(){
        var canchas = context.Cancha.Where(c => c.Estado == 1).Include(c => c.Tipo).ToList();
        if(canchas.Count > 0){
            return Ok(canchas);
        }
        return BadRequest("No existen canchas");
    }

    //En un principio allowanonymous, despues solo los del rol administrador...
    [AllowAnonymous]
    [HttpPost]
    public IActionResult CrearCancha([FromForm] Cancha cancha){
        if(cancha != null){
            cancha.Estado = 1;
            cancha.Imagen = "default.jpg";
            context.Cancha.Add(cancha);
            int filasInsertadas = context.SaveChanges();
            Console.WriteLine($"Filas insertadas: {filasInsertadas}");
            if(filasInsertadas > 0){
                return Ok("Cancha creada con éxito");
            }
        }
        return BadRequest();
    }
    
    [AllowAnonymous]
    [HttpPatch("{idCancha}")]
    public IActionResult EditarCancha([FromForm] IFormFile imagen, int idCancha){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha != null && imagen != null && imagen.Length > 0){
            var fileName = $"{idCancha}_{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/cancha", fileName);
            using(var stream = new FileStream(path, FileMode.Create)){
                imagen.CopyTo(stream);
            }
            cancha.Imagen = fileName;
            context.SaveChanges();
            return Ok("Imagen editada");
        }
        return BadRequest("cancha o imagen no estan bien");
    }

    [HttpGet("porcentaje/{idCancha}")]
    public IActionResult PorcentajeCalificacionCancha(int idCancha){
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == idCancha);
        if(cancha != null){
            var turnosConCalificacion = context.Turno.Where(t => t.CanchaId == idCancha && t.Calificacion != null).ToList();   
            var cantidadCalificaciones = turnosConCalificacion.Count;
            if(cantidadCalificaciones > 0){
                int total = 0;
                foreach(var tcc in turnosConCalificacion){
                    total += (int) tcc.Calificacion;
                }
                var porcentaje = (double) total / cantidadCalificaciones;
                Console.WriteLine(String.Format("{0:0.0}", porcentaje));
                Console.WriteLine(porcentaje);
                return Ok(String.Format("{0:0.0}", porcentaje));
            }
            return Ok(String.Format("{0:0.0}",5));
        }
        return BadRequest("No se encontró la cancha");
    }
}