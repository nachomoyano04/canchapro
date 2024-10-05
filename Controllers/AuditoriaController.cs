using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class AuditoriaController:ControllerBase{
    private readonly DataContext context;
    public AuditoriaController(DataContext context){
        this.context = context;
    }

    [HttpGet]
    public IActionResult Get(){
        return Ok();
    }
}