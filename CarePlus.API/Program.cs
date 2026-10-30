using CarePlus.API.Data;
using CarePlus.API.Extension;
using CarePlus.API.Middleware;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using System.Text;


var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddServices(builder.Configuration);
builder.Services.AddControllers().ConfigureApiBehaviorOptions(opt =>
{
    opt.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
        .Where(e => e.Value?.Errors.Count > 0)
        .ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value!.Errors
                .Select(e => e.ErrorMessage)
                .ToArray());
        throw new ValidationException(errors);
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("Client", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5174")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Add jwt
builder.Services.AddJwtAuthentication(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseMiddleware<ExceptionHandlingMiddleware>();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

}

app.UseCors("Client");
var profileFolder = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "profiles");
Directory.CreateDirectory(profileFolder);
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/api/profiles",
    FileProvider = new PhysicalFileProvider(profileFolder)
});
var leaveDocumentFolder = Path.Combine(app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot"), "leave-documents");
Directory.CreateDirectory(leaveDocumentFolder);
app.UseStaticFiles(new StaticFileOptions
{
    RequestPath = "/api/leave-documents",
    FileProvider = new PhysicalFileProvider(leaveDocumentFolder)
});
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseAuthentication();
app.UseMiddleware<ActiveAccountMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();
