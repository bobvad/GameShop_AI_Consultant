using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using GameShop.Context;
using Game_Shop_AI_Assistent.Services;
using Microsoft.AspNetCore.Http.Features;
using Game_Shop_AI_Assistent.GigaChat_LLM.API_UP_02.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<GameShopContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("GameShop"),
        ServerVersion.AutoDetect(
            builder.Configuration.GetConnectionString("GameShop")
        )
    )
);

builder.Services.AddScoped<GigaChatService>();
builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddLogging();
builder.Services.AddRazorPages();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 8 * 1024 * 1024;
    options.ValueLengthLimit = 8 * 1024 * 1024;
});
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "Руководство для использования запросов",
        Description = "Полное руководство для использования запросов находящихся в проекте"
    });
    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Version = "v2",
        Title = "Руководство для использования запросов",
        Description = "Полное руководство для использования запросов находящихся в проекте"
    });
    c.SwaggerDoc("v3", new OpenApiInfo
    {
        Version = "v3",
        Title = "Руководство для использования запросов",
        Description = "Полное руководство для использования запросов находящихся в проекте"
    });
    c.SwaggerDoc("v4", new OpenApiInfo
    {
        Version = "v4",
        Title = "Руководство для использования запросов",
        Description = "Полное руководство для использования запросов находящихся в проекте"
    });

    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary",
        Description = "Файл для загрузки"
    });

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        c.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseStatusCodePages();

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Запросы GET");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "Запросы POST");
        c.SwaggerEndpoint("/swagger/v3/swagger.json", "Запросы PUT");
        c.SwaggerEndpoint("/swagger/v4/swagger.json", "Запросы DELETE");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());
app.UseHttpsRedirection();
app.UseStaticFiles(); 
app.UseRouting();
app.UseCors("AllowAll");
app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.Run();