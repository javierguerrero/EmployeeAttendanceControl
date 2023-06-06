using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ms.employees.application.Mappers;
using ms.employees.application.Queries;
using ms.employees.domain.Repositories;
using ms.employees.infrastructure.Data;
using ms.employees.infrastructure.Repositories;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//Habilitar el ingreso del Bearer toek a través de la interfaz de Swagger
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Users Authentication Api",
        Version = "v1"
    });
    swagger.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Cabecera de autorización JWT. \r\n Introduzca ['Bearer'] [espacio] [Token].",
    });
    swagger.AddSecurityRequirement(new OpenApiSecurityRequirement {
                    {
                          new OpenApiSecurityScheme {
                              Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme
                                                                , Id = "Bearer" }
                          },new string[] {}
                    }
                });
});

// Configurar dependencias
builder.Services.AddScoped(typeof(IDapperContext), typeof(EmployeesDapperContext));
builder.Services.AddScoped(typeof(IEmployeeRepository), typeof(EmployeeRepository));

var automapperConfig = new MapperConfiguration(mapperConfig =>
{
    mapperConfig.AddMaps(typeof(EmployeesMapperProfile).Assembly);
});
IMapper mapper = automapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

builder.Services.AddMediatR(typeof(GetAllEmployeesQuery).GetTypeInfo().Assembly);


//configurar la autorización del token JWT
var provider = builder.Services.BuildServiceProvider();
var configuration = provider.GetRequiredService<IConfiguration>();
var privateKey = configuration.GetValue<string>("Authentication:JWT:Key");
builder.Services.AddAuthentication(option =>
{
    option.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    option.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(privateKey)),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        ClockSkew = TimeSpan.Zero
    };
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();