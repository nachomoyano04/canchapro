using Microsoft.EntityFrameworkCore;

public class DataContext:DbContext{
    public DataContext(DbContextOptions<DataContext> options):base(options){

    }

    public DbSet<Usuario> Usuario {get; set;}
    public DbSet<Cancha> Cancha {get; set;}
    public DbSet<Turno> Turno {get; set;}
    public DbSet<Pago> Pago {get; set;}
    public DbSet<Horarios> Horarios {get; set;}
}