using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController:ControllerBase{
    private readonly DataContext context;
    public UsuarioController(DataContext context){
        this.context = context;
    }

    [HttpGet("get/{id}")]
    public IActionResult Obtener(int id){
        var usuario = context.Usuario.Find(id);
        return Ok(usuario);
    }

    [HttpGet]
    public IActionResult Listar(){
        List<Usuario> usuarios = context.Usuario.ToList();
        return Ok(usuarios);
    }

    [HttpGet("getByCorreo/{correo}")]
    public IActionResult ListarPorCorreo(string correo){
        var usuario = context.Usuario.FirstOrDefault(u => u.Correo == correo);
        return Ok(usuario);
    }

    [HttpPut]
    public IActionResult Editar([FromForm]Usuario usuario){
        context.Usuario
        return Ok();
    }

    [HttpPost]
    public IActionResult Guardar([FromForm]Usuario usuario){
        context.Usuario.Add(usuario);
        context.SaveChanges();
        return Ok();
    }
}