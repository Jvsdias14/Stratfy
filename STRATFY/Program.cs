using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using STRATFY.Helpers;
using STRATFY.Interfaces.IContexts;
using STRATFY.Interfaces.IRepositories;
using STRATFY.Interfaces.IServices;
using STRATFY.Models;
using STRATFY.Repositories;
using STRATFY.Services;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index"; 
        options.LogoutPath = "/Login/Logout";
    });
builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor(); // Necessário para acessar o usuário
builder.Services.AddScoped<UsuarioContexto>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped(typeof(IRepositoryBase<>), typeof(RepositoryBase<>));

// Repositórios
builder.Services.AddScoped<IRepositoryBase<Usuario>, RepositoryBase<Usuario>>(); // Se aplicável
builder.Services.AddScoped<IRepositoryUsuario, RepositoryUsuario>(); // << Novo registro

// Helpers/Contextos
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>(); // Essencial para HttpContext


builder.Services.AddScoped<IRepositoryUsuario, RepositoryUsuario>();
builder.Services.AddScoped<IRepositoryExtrato, RepositoryExtrato>();
builder.Services.AddScoped<IRepositoryDashboard, RepositoryDashboard>();
builder.Services.AddScoped<IRepositoryLogin, RepositoryLogin>();
builder.Services.AddScoped<IRepositoryMovimentacao, RepositoryMovimentacao>();

builder.Services.AddScoped<ICsvExportService, CsvExportService>();
builder.Services.AddScoped<IExtratoService, ExtratoService>();
builder.Services.AddScoped<IMovimentacaoService, MovimentacaoService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IUsuarioContexto, UsuarioContexto>();

builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddHttpClient();

builder.WebHost.UseUrls("http://localhost:5211");

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("pt-BR");
CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo("pt-BR");

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
