using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
var configuration = builder.Configuration;
// Add services to the container.

builder.WebHost.UseUrls("http://localhost:5021", "http://*:5021");
builder.Services.AddControllers();
builder.Services.AddDbContext<DataContext>(
	options => options.UseMySql(
		configuration["ConnectionString"],
		ServerVersion.AutoDetect(configuration["ConnectionString"])
	)
);
//agregamos la autenticación por medio de jwt bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
	.AddJwtBearer(options => {
		options.TokenValidationParameters = new TokenValidationParameters{
			ValidateIssuer = true,
			ValidateAudience = true,
			ValidateLifetime = true,
			ValidateIssuerSigningKey = true,
			ValidIssuer = configuration["TokenAuthentication:Issuer"],
			ValidAudience = configuration["TokenAuthentication:Audience"],
			IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(
				configuration["TokenAuthentication:SecretKey"]
			))
		};
		//opcion para poder pasar el token por queryparams
        options.Events = new JwtBearerEvents{
            OnMessageReceived = context => {
                //leemos el token desde el queryparams
                var access_token = context.Request.Query["access_token"];
                //ruta
                var path = context.HttpContext.Request.Path;
                if(!access_token.IsNullOrEmpty() &&
                    path.StartsWithSegments("/api/usuario/generarpassword")){
                        context.Token = access_token;
                }
                return Task.CompletedTask;
            }
        };
	});
// para acceder al context sin una llamada http y poder usar el user.claims
builder.Services.AddHttpContextAccessor(); 

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseCors(x => x
	.AllowAnyOrigin()
	.AllowAnyMethod()
	.AllowAnyHeader());
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
//para las imagenes en el wwwroot
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
