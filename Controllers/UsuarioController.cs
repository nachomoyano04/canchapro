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
    private IWebHostEnvironment environment;
    public UsuarioController(DataContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment){
        this.context = context;
        this.configuration = configuration;
        this.environment = environment;
        string claim = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        IdUsuario = Parsear(claim);
    }

    private int Parsear(string? claim){
        if(!claim.IsNullOrEmpty()){
            return Int32.Parse(claim);
        }
        return 0;
    }

    [HttpGet]
    public IActionResult GetUsuario(){
        var usuario = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario);
        if(usuario != null){
            return Ok(usuario);
        }
        return BadRequest("No hay usuario logueado");
    }

    
    [AllowAnonymous]
    [HttpPost("login")]
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
        Console.WriteLine($"avatar: {avatar}");
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

    [HttpPut("NuevaPassword")]
    public IActionResult NuevaPassword([FromForm] string nuevaPassword){
        var usuario = context.Usuario.FirstOrDefault(u => u.Id == IdUsuario);
        if(usuario != null){
            usuario.Password = HashearPassword(nuevaPassword);
            context.SaveChanges();
            return Ok("Password actualizada...");
        }
        return BadRequest("No existe propietario.");
    }

    [AllowAnonymous]
    [HttpPost("recuperarpass")]
    public IActionResult RecuperarPassword([FromForm] string correo){
        var usuario = context.Usuario.FirstOrDefault(u => u.Correo.ToLower().Equals(correo.ToLower()));
        if(usuario != null){
            string dominio = "";
            if(environment.IsDevelopment()){
                // dominio = HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
                dominio = "http://192.168.1.9:5021/api/usuario/nuevapassword";
            }else{
                dominio = "www.canchapro.com";
            }
            string token = GenerarToken(usuario);
            dominio += $"?access_token={token}";
            string mensajeEnHtml = $"<h1>Hola {usuario.Nombre}!</h1>"
            +"<p>Para elegir una nueva password toque el siguiente boton:</p>"
            +$"<a href='{dominio}'><button style='background-color:#007bff; padding: 5px; margin: 10px 0px;'>Nueva password</button></a>";
            EnviarMail("CanchaPro","nachomoyag@gmail.com", usuario.Nombre+" "+usuario.Apellido, correo, mensajeEnHtml);
            return Ok("Email de recuperacion enviado");
        }
        return BadRequest("Registrese por favor, no conocemos ese correo.");
    }

    [AllowAnonymous] 
    [HttpPost]
    public IActionResult Registrar([FromForm] Usuario usuario){
        if(!usuario.Dni.IsNullOrEmpty() && !usuario.Nombre.IsNullOrEmpty() && !usuario.Dni.IsNullOrEmpty() && !usuario.Apellido.IsNullOrEmpty() && !usuario.Correo.IsNullOrEmpty()){
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

    private void EnviarMail(string emisor, string emisorCorreo, string destinatario, string destinatarioCorreo, string mensajeEnHtml){
        var mensaje = new MimeKit.MimeMessage();
        mensaje.To.Add(new MailboxAddress(destinatario, destinatarioCorreo));
        mensaje.From.Add(new MailboxAddress(emisor, emisorCorreo));
        mensaje.Subject = "Mensaje de recuperacion de contraseña";
        mensaje.Body = new TextPart("html"){
            Text = mensajeEnHtml
        };
        MailKit.Net.Smtp.SmtpClient client = new MailKit.Net.Smtp.SmtpClient();
        client.ServerCertificateValidationCallback = (object sender,
                System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors sslPolicyErrors) =>
            { return true; };
            client.Connect("smtp.gmail.com", 465, MailKit.Security.SecureSocketOptions.Auto);
            client.Authenticate(configuration["SMPT:User"], configuration["SMPT:Password"]);//estas credenciales deben estar en el user secrets
            client.Send(mensaje);
    }
}