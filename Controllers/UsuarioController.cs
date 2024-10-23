using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class UsuarioController:ControllerBase{
    
    private readonly IConfiguration configuration;
    private readonly DataContext context;
    public UsuarioController(DataContext context, IConfiguration configuration){
        this.context = context;
        this.configuration = configuration;
    }



    [HttpGet("get/{id}")]
    public IActionResult Obtener(int id){
        var usuario = context.Usuario.Find(id);
        return Ok(usuario);
    }


    [HttpPut]
    public IActionResult Editar([FromForm] Usuario usuario){
        if(usuario != null){
    
        }
        return BadRequest("Debe enviar un usuario");
    }

    [HttpPut("avatar")]
    public IActionResult EditarAvatar([FromForm] IFormFile avatar){
        
    }

    //Endpoint para registrar nuevo usuario.
    [AllowAnonymous] 
    [HttpPost]
    public IActionResult Crear([FromForm] Usuario usuario){
        context.Usuario.Add(usuario);
        context.SaveChanges();
        return Ok("Usuario creado con éxito");
    }

    private string HashearPassword(string password){
        string hasheada = Convert.ToBase64String(KeyDerivation.Pbkdf2(
        password: password,
        salt: Encoding.ASCII.GetBytes(configuration["Salt"]),
        prf: KeyDerivationPrf.HMACSHA1,
        iterationCount: 1000,
        numBytesRequested: 256 / 8));
        return hasheada;
    }

    private string GenerarToken(Usuario usuario){

    }

}