using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TipoController:ControllerBase{
    private readonly DataContext context;
    public TipoController(DataContext context){
        this.context = context;
    }
}