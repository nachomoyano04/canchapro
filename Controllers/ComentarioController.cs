using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComentarioController:ControllerBase{
    private readonly DataContext context;
    private readonly int IdUsuario;
    public ComentarioController(DataContext context, IHttpContextAccessor httpContextAccessor){
        this.context = context;
        IdUsuario = Int32.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    //CHEQUEADO
    [HttpPost]
    public IActionResult NuevoComentario([FromForm] Comentario comentario){
        var usuario = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario);
        if(usuario == null){
            return BadRequest("El usuario debe loguearse...");
        }
        var turno = context.Turno.FirstOrDefault(t => t.Id == comentario.TurnoId && t.UsuarioId == IdUsuario);
        if(turno == null){
            return BadRequest("El turno no existe o no esta relacionado con el usuario logueado...");
        }
        if(turno.Estado != 2){
            return BadRequest("El turno no se ha completado aún o se ha cancelado...");
        }
        var comment = context.Comentario.FirstOrDefault(c => c.UsuarioId == IdUsuario && c.TurnoId == turno.Id);
        if(comment != null){
            return BadRequest("Ya existe un comentario de este usuario y este turno. Editelo");
        }
        comentario.Fecha = DateTime.Now;
        comentario.UsuarioId = IdUsuario;
        context.Comentario.Add(comentario);
        context.SaveChanges();
        return Ok("Gracias por dejar tu reseña!");
    }

    //CHEQUEADO
    [HttpPut("{id}")]
    public IActionResult EditarComentario(int id, [FromForm] Comentario comentario){
        var comment = context.Comentario.FirstOrDefault(c => c.Id == id && c.UsuarioId == IdUsuario);
        if(comment != null){
            comment.Calificacion = comentario.Calificacion;
            comment.Descripcion = comentario.Descripcion;
            comment.Fecha = DateTime.Now;
            context.SaveChanges();
            return Ok("Comentario actualizado!");
        }
        return BadRequest("Debe estar logueado para editar este comentario.");
    }

    //CHEQUEADO
    [HttpDelete("{id}")]
    public IActionResult BorrarComentario(int id){
        //eliminamos el comentario de una, pero en produccion deberia
        //persistirse como auditoria el comentario eliminado.
        var comentario = context.Comentario.FirstOrDefault(c => c.Id == id && c.UsuarioId == IdUsuario);
        if(comentario != null){
            context.Remove(comentario);
            context.SaveChanges();
            return Ok("Comentario borrado con éxito");
        }
        return BadRequest("Debe estar logueado para borrar este comentario.");
    }

    //CHEQUEADO
    [HttpGet]
    public IActionResult MisComentarios(){
        var comentarios = context.Comentario.Where(c => c.UsuarioId == IdUsuario).ToList();
        if(comentarios.Count > 0){
            return Ok(comentarios);
        }
        return NoContent();
    }

    [HttpGet("{idCancha}")]
    public IActionResult ComentariosPorCancha(int idCancha){
        var comentarios = context.Comentario.Where(c => c.Turno.CanchaId == idCancha && c.UsuarioId == IdUsuario).ToList();
        if(comentarios.Count > 0){
            return Ok(comentarios);
        }
        return NoContent();
    }
}
