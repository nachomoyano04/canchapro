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
        var canchas = context.Cancha.Where(c => c.Estado == estado).ToList();
        return Ok(canchas);
    }

    //En un principio allowanonymous, despues solo los del rol administrador...
    [HttpPost]
    public IActionResult CrearCancha([FromForm] Cancha c, [FromForm] IFormFile? imagen){
        if(c != null){
            if(imagen != null && imagen.Length > 0){
                var fileName = $"{new Random().Next(1, 100)}_{Guid.NewGuid()}{Path.GetExtension(imagen.FileName)}";
                var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/cancha", fileName);
                using(var stream = new FileStream(path, FileMode.Create)){
                    imagen.CopyTo(stream);
                }
                c.Imagen = fileName;
            }else{
                c.Imagen = "default.jpg";
            }
            context.Cancha.Add(c);
            int filasInsertadas = context.SaveChanges();
            Console.WriteLine($"Filas insertadas: {filasInsertadas}");
            if(filasInsertadas > 0){
                return Ok("Cancha creada con éxito");
            }
        }
        return BadRequest();
    }
    
    [HttpPatch("estado/{id}")]
    public IActionResult CambioEstado(int id, [FromForm] int estado){
        var court = context.Cancha.FirstOrDefault(c => c.Id == id);
        if(court != null){
            court.Estado = estado;
            context.SaveChanges();
            return Ok("Cambios realizados con éxito");
        }
        return BadRequest("No se encontró la cancha");
    }

    [HttpPatch]
    public IActionResult EditarCancha([FromForm] Cancha cancha, [FromForm] IFormFile? imagen){
        var court = context.Cancha.FirstOrDefault(c => c.Id == cancha.Id);
        if(court != null){  
            court.Nombre = cancha.Nombre;
            court.CapacidadTotal = cancha.CapacidadTotal;
            court.TipoDePiso = cancha.TipoDePiso;
            Console.WriteLine($"imagen: {imagen}");
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
            if(cancha.Estado > 0){
                court.Estado = cancha.Estado;
            }
            context.SaveChanges();
            return Ok("Cambios realizados");
        }
        return BadRequest("La cancha no se encontró");
    }

    //ENDPOINTS PARTE WEB

    [HttpGet("todas")]
    public IActionResult Todas(){ //QUE ESTEN DISPONIBLES
        var canchas = context.Cancha.Where(c => c.Estado == 1).ToList();
        return Ok(canchas);
    }
}