using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class CanchaController:ControllerBase{
    private readonly DataContext context;
    public CanchaController(DataContext context){
        this.context = context;
    }
}