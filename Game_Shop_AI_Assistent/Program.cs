using Microsoft.OpenApi.Models;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers(); 

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

    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));
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

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers(); 

app.Run();