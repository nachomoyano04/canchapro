using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Hosting.Server.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using MimeKit;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsuarioController:ControllerBase{
    
    private readonly IConfiguration configuration;
    private readonly DataContext context;
    private readonly int IdUsuario;
    public UsuarioController(DataContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor){
        this.context = context;
        this.configuration = configuration;
        string claim = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        IdUsuario = Parsear(claim);
    }

    private int Parsear(string? claim){
        if(!claim.IsNullOrEmpty()){
            return Int32.Parse(claim);
        }
        return 0;
    }

    [AllowAnonymous]
    [HttpGet("login")]
    public IActionResult Login([FromForm]string correo,[FromForm]string password){
        if(!correo.IsNullOrEmpty() && !password.IsNullOrEmpty()){
            var usuario = context.Usuario.FirstOrDefault(u => u.Correo.ToLower() == correo.ToLower() && u.Password == HashearPassword(password));
            if(usuario != null){
                return Ok(GenerarToken(usuario));
            }
            return BadRequest("Correo y/o password incorrectos.");
        }
        return BadRequest("Debe llenar todos los campos");
    }

    [HttpPut]
    public IActionResult Editar([FromForm] Usuario usuario){
        //verificamos que el usuario a editar sea el logueado
        var usuarioDb = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario);
        if(usuarioDb != null){
            if(context.Usuario.FirstOrDefault(u => u.Dni == usuario.Dni && u.Id != IdUsuario) != null){
                return BadRequest("El dni ya existe");
            }
            if(context.Usuario.FirstOrDefault(u => u.Correo.ToLower() == usuario.Correo.ToLower() && u.Id != IdUsuario) != null){
                return BadRequest("El correo ya existe");
            }
            usuarioDb.Nombre = usuario.Nombre;
            usuarioDb.Apellido = usuario.Apellido;
            usuarioDb.Dni = usuario.Dni;
            usuarioDb.Correo = usuario.Correo;
            context.SaveChanges();
            return Ok("Perfil modificado exitosamente.");
        }
        return BadRequest("Debe enviar un usuario");
    }

    [HttpPatch("avatar")]
    public IActionResult EditarAvatar([FromForm] IFormFile avatar){
        var usuario = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario);
        if(usuario != null && avatar != null && avatar.Length > 0){
            var fileName = $"{IdUsuario}_{Guid.NewGuid()}{Path.GetExtension(avatar.FileName)}";
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/img/usuario", fileName);
            using (var stream = new FileStream(path, FileMode.Create)){
                avatar.CopyTo(stream);
            }
            usuario.Avatar = fileName;
            context.SaveChanges();
            return Ok("Avatar editado");
        }
        return BadRequest();
    }

    [HttpPatch("password")]
    public IActionResult CambiarPassword([FromForm] string passwordActual, [FromForm] string passwordNueva){
        var usuario = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario && u.Password == HashearPassword(passwordActual));
        if(usuario != null){
            usuario.Password = HashearPassword(passwordNueva);
            context.SaveChanges();
            return Ok("Password actualizada");
        }
        return BadRequest("La password actual no coincide");
    }

    [AllowAnonymous] 
    [HttpPost]
    public IActionResult Crear([FromForm] Usuario usuario){
        if(!usuario.Nombre.IsNullOrEmpty() && !usuario.Dni.IsNullOrEmpty() && !usuario.Apellido.IsNullOrEmpty() && !usuario.Correo.IsNullOrEmpty()){
            usuario.Avatar = "default.jpg";
            usuario.Password = HashearPassword(usuario.Password);
            var usuarioConDni = context.Usuario.FirstOrDefault(u => u.Dni == usuario.Dni);
            if(usuarioConDni != null){
                return BadRequest("El dni ya existe");
            }
            var usuarioConCorreo = context.Usuario.FirstOrDefault(u => u.Correo.ToLower() == usuario.Correo.ToLower());
            if(usuarioConCorreo != null){
                return BadRequest("El correo ya existe");
            }
            usuario.Estado = true;
            context.Usuario.Add(usuario);
            context.SaveChanges();
            return Ok("Usuario creado con éxito");
        }
        return BadRequest("Debe completar todos los campos");
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
        var claveSegura = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(configuration["TokenAuthentication:SecretKey"]));
        var credenciales = new SigningCredentials(claveSegura, SecurityAlgorithms.HmacSha256);
        var claims = new List<Claim>{
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString())
        };
        var token = new JwtSecurityToken(
            configuration["TokenAuthentication:Issuer"],
            configuration["TokenAuthentication:Audience"],
            claims, 
            expires: DateTime.Now.AddMinutes(15),
            signingCredentials: credenciales
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}