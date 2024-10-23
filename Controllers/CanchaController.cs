using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CanchaController:ControllerBase{
    private readonly DataContext context;
    public CanchaController(DataContext context){
        this.context = context;
    }

    //http://ip:puerto/api/cancha
    [HttpGet]
    public IActionResult GetCanchas(){
        var canchas = context.Cancha.Where(c => c.Estado == 1);
        if(canchas != null){
            return Ok(canchas);
        }
        return BadRequest("No existen canchas");
    }

    //http://ip:puerto/api/cancha
    [HttpPost]
    public IActionResult CrearCancha([FromForm] Cancha cancha){
        if(cancha != null){
            cancha.Estado = 1;
            context.Cancha.Add(cancha);
            int filasInsertadas = context.SaveChanges();
            Console.WriteLine($"Filas insertadas: {filasInsertadas}");
            if(filasInsertadas > 0){
                return Ok("Cancha creada con éxito");
            }
        }
    }
}