using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TipoController:ControllerBase{
    private readonly DataContext context;
    public TipoController(DataContext context){
        this.context = context;
    }

    [AllowAnonymous] //luego sería con rol administrador nada más este endpoint
    [HttpPost] //Crear nuevo tipo de cancha
    public IActionResult Crear([FromForm] Tipo tipo){
        context.Tipo.Add(tipo);
        context.SaveChanges();
        return Ok("Tipo de cancha creada correctamente");
    }

}