using AutoMapper;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ms.attendances.api.Consumers;
using ms.attendances.api.Mappers;
using ms.attendances.application.Commands;
using ms.attendances.application.Mappers;
using ms.attendances.domain.Repositories;
using ms.attendances.infrastructure.Data;
using ms.attendances.infrastructure.Mappers;
using ms.attendances.infrastructure.Repositories;
using ms.rabbitmq.Consumers;
using System.Reflection;
using System.Text;
using ms.rabbitmq.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

//Habilitar el ingreso del Bearer toek a través de la interfaz de Swagger
builder.Services.AddSwaggerGen(swagger =>
{
    swagger.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Historical Attendance Api",
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

builder.Services.AddSingleton(typeof(IConsumer), typeof(AttendancesConsumer));

builder.Services.AddScoped(typeof(IAttendanceContext), typeof(AttendanceMongoContext));
builder.Services.AddScoped(typeof(IAttendanceRepository), typeof(AttendanceRepository));

builder.Services.AddTransient(typeof(IAttendanceContext), typeof(AttendanceMongoContext));
builder.Services.AddTransient(typeof(IAttendanceRepository), typeof(AttendanceRepository));

var automapperConfig = new MapperConfiguration(mapperConfig =>
{
    mapperConfig.AddMaps(typeof(AttendanceProfile).Assembly);
    mapperConfig.AddMaps(typeof(AttendanceMongoProfile).Assembly);
    mapperConfig.AddProfile(typeof(EventMapperProfile));
});
IMapper mapper = automapperConfig.CreateMapper();
builder.Services.AddSingleton(mapper);

builder.Services.AddMediatR(typeof(CreateAttendanceCommand).GetTypeInfo().Assembly);

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

var consumer = app.Services.GetRequiredService<IConsumer>();
app.UseRabbitConsumer(consumer);

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
