using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TurnoController:ControllerBase{
    private readonly DataContext context;
    public TurnoController(DataContext context){
        this.context = context;
    }
}