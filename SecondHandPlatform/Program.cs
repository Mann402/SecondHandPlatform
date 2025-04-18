using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using SecondHandPlatform.Data;
using SecondHandPlatform.Interfaces;
using SecondHandPlatform.Models;
using SecondHandPlatform.Repositories;
using SecondHandPlatform.Services;
using SecondHandPlatformTest.Services;
using System.Net.Http;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
));

builder.Services.AddDbContext<SecondhandplatformContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 41)) // Replace with your exact MySQL version
    )
);

// Enable CORS (Allow React App to Communicate with API)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp",
        policy => policy.WithOrigins("http://localhost:3000") // React Frontend URL
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});

//HttpClient for face recognition calls
builder.Services.AddHttpClient();


// bind appsettings.json ¡ú SmtpSettings
builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("SmtpSettings"));
// register the service
builder.Services.AddSingleton<IEmailService, EmailService>();


// Configure JSON serialization to prevent circular reference errors
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    options.JsonSerializerOptions.WriteIndented = true;
});

builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "SecondHandPlatform API", Version = "v1" });
});



var app = builder.Build();
app.UseDeveloperExceptionPage();

/* Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}*/

app.UseHttpsRedirection();


//Enable Swagger in both Development and Production
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "SecondHandPlatform API v1");
    c.RoutePrefix = "swagger"; // Swagger will be available at the root URL
});

//Middleware Configuration
app.UseCors("AllowReactApp");

app.UseRouting(); // Ensures correct request routing

app.UseAuthorization();



app.UseHttpsRedirection();

app.MapControllers();

app.Run();


