using System.Collections;
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

    [HttpGet("unica/{id}")]
    public IActionResult GetCancha(int id){
        // var cancha = context.Cancha.Include(c => c.Tipo).FirstOrDefault(c => c.Id == id);
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == id);
        if(cancha != null){
            return Ok(cancha);
        }
        return BadRequest("No se encontró la cancha...");
    }

    //http://ip:puerto/api/cancha
    [HttpGet("{estado}")]
    public IActionResult GetCanchas(int estado){
        // var canchas = context.Cancha.Where(c => c.Estado == estado).Include(c => c.Tipo).ToList();
        var canchas = context.Cancha.Where(c => c.Estado == estado).ToList();
        return Ok(canchas);
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
    
    [HttpPatch]
    public IActionResult EditarCancha([FromForm] Cancha cancha, [FromForm] IFormFile? imagen){
        var court = context.Cancha.FirstOrDefault(c => c.Id == cancha.Id);
        if(court != null){
            court.Nombre = cancha.Nombre;
            court.CapacidadTotal = cancha.CapacidadTotal;
            court.TipoDePiso = cancha.TipoDePiso;
            if(imagen != null && imagen.Length > 0){
                var fileName = $"{cancha.Id}_{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/cancha", fileName);
                using(var stream = new FileStream(path, FileMode.Create)){
                    imagen.CopyTo(stream);
                }
                court.Imagen = fileName;
            }
            court.PrecioPorHora = cancha.PrecioPorHora;
            court.Descripcion = cancha.Descripcion;
            court.Estado = cancha.Estado;
            context.SaveChanges();
            return Ok("Cambios realizados");
        }
        return BadRequest("La cancha no se encontró");
    }
}