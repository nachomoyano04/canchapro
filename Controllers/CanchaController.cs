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
        var cancha = context.Cancha.FirstOrDefault(c => c.Id == id);
        if(cancha != null){
            return Ok(cancha);
        }
        return BadRequest("No se encontró la cancha...");
    }

    //http://ip:puerto/api/cancha
    [HttpGet("{estado}")]
    public IActionResult GetCanchas(int estado){
        var canchas = context.Cancha.Where(c => c.Estado == estado).Include(c => c.Tipo).ToList();
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
}