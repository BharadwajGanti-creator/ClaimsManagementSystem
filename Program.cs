using Learning_Project.Interfaces;
using Learning_Project.Middleware;
using Learning_Project.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
//Register the NameValidationService
builder.Services.AddScoped<INameValidationService, NameValidationService>();
//Register the ErrorResponseService
builder.Services.AddScoped<IErrorResponseService, ErrorResponseService>();
//Register the ExceptionMiddleware
builder.Services.AddScoped<ExceptionMiddleware>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
