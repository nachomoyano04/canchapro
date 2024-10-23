using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TipoController:ControllerBase{
    private readonly DataContext context;
    public TipoController(DataContext context){
        this.context = context;
    }

}