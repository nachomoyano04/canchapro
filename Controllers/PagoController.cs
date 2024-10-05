using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class PagoController:ControllerBase{
    private readonly DataContext context;
    public PagoController(DataContext context){
        this.context = context;
    }
}