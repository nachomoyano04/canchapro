using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Authorize]
public class ComentarioController:ControllerBase{
    private readonly DataContext context;
    private readonly int IdUsuario;
    public ComentarioController(DataContext context, IHttpContextAccessor httpContextAccessor){
        this.context = context;
        IdUsuario = Int32.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    //Crear comentario
    [HttpPost]
    public IActionResult NuevoComentario([FromForm]Comentario comentario){
        var usuario = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario);
        if(usuario != null){
            comentario.UsuarioId = IdUsuario;
        }
        return BadRequest();
    }
}