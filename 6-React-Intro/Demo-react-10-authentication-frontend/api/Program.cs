using Microsoft.EntityFrameworkCore;
using MyShop.DAL;
using Serilog;
using Serilog.Events;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Identity;
using MyShop.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
    // options.SerializerSettings.ContractResolver = new DefaultContractResolver();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My Shop API", Version = "v1" }); // Basic info for the API
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme // Define the Bearer auth scheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement // Require Bearer token for accessing the API
    {{ new OpenApiSecurityScheme // Reference the defined scheme
            { Reference = new OpenApiReference
                { Type = ReferenceType.SecurityScheme,
                  Id = "Bearer"}},
            new string[] {}
        }});
});
builder.Services.AddDbContext<ItemDbContext>(options =>
{
    options.UseSqlite(builder.Configuration["ConnectionStrings:ItemDbContextConnection"]);
});

builder.Services.AddDbContext<AuthDbContext>(options =>
{
    options.UseSqlite(builder.Configuration["ConnectionStrings:AuthDbContextConnection"]);
});

builder.Services.AddIdentity<AuthUser, IdentityRole>()
    .AddEntityFrameworkStores<AuthDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
    {
        builder.WithOrigins("http://localhost:4000") // Allow requests from the React frontend
               .AllowAnyMethod()
               .AllowAnyHeader()
               .AllowCredentials();
    });
});

builder.Services.AddScoped<IItemRepository, ItemRepository>();

builder.Services.AddAuthorization();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuer = true, // Validate the token issuer is correct
        ValidateAudience = true, // Validate the token reciepient is correct 
        ValidateLifetime = true, // Validate the token has not expired
        ValidateIssuerSigningKey = true, // Validate the JWT signature
        ValidIssuer = builder.Configuration["Jwt:Issuer"], // Reading the issuer of the token
        ValidAudience = builder.Configuration["Jwt:Audience"], // Reading the audience for the token
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not found in configuration.") // Reading the key from the configuration
        ))
    };
    // options.Events = new JwtBearerEvents
    // {
    //     OnMessageReceived = context =>
    //     {
    //         var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").LastOrDefault();
    //         Console.WriteLine($"OnMessageReceived - Token: {token}");
    //         return Task.CompletedTask;
    //     },
    //     OnTokenValidated = context =>
    //     {
    //         Console.WriteLine("OnTokenValidated: SUCCESS");
    //         return Task.CompletedTask;
    //     },
    //     OnAuthenticationFailed = context =>
    //     {
    //         Console.WriteLine($"OnAuthenticationFailed: {context.Exception.Message}");
    //         Console.WriteLine($"Exception Type: {context.Exception.GetType().Name}");
    //         if (context.Exception.InnerException != null)
    //         {
    //             Console.WriteLine($"Inner Exception: {context.Exception.InnerException.Message}");
    //         }
    //         return Task.CompletedTask;
    //     },
    //     OnChallenge = context =>
    //     {
    //         Console.WriteLine($"OnChallenge: {context.Error} - {context.ErrorDescription}");
    //         return Task.CompletedTask;
    //     }
    // };
});


var loggerConfiguration = new LoggerConfiguration()
    .MinimumLevel.Information() // levels: Trace< Information < Warning < Erorr < Fatal
    .WriteTo.File($"APILogs/app_{DateTime.Now:yyyyMMdd_HHmmss}.log")
    .Filter.ByExcluding(e => e.Properties.TryGetValue("SourceContext", out var value) &&
                            e.Level == LogEventLevel.Information &&
                            e.MessageTemplate.Text.Contains("Executed DbCommand"));
var logger = loggerConfiguration.CreateLogger();
builder.Logging.AddSerilog(logger);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    DBInit.Seed(app);
    app.UseSwagger();
    app.UseSwaggerUI();

}
app.UseStaticFiles();
app.UseRouting();
app.UseCors("CorsPolicy");
// app.Use(async (context, next) =>
// {
//     if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
//     {
//         var headerValue = authHeader.FirstOrDefault();
//         if (headerValue?.StartsWith("Bearer ") == true)
//         {
//             var token = headerValue.Substring("Bearer ".Length).Trim();

//             // Decode the token to see its contents (without verification)
//             var handler = new JwtSecurityTokenHandler();
//             var jsonToken = handler.ReadJwtToken(token);

//             Console.WriteLine($"--> Token Issuer: {jsonToken.Issuer}");
//             Console.WriteLine($"--> Token Audience: {jsonToken.Audiences.FirstOrDefault()}");
//             Console.WriteLine($"--> Token Expiry: {jsonToken.ValidTo}");
//             Console.WriteLine($"--> Current Time: {DateTime.UtcNow}");
//             Console.WriteLine($"--> Config Issuer: {builder.Configuration["Jwt:Issuer"]}");
//             Console.WriteLine($"--> Config Audience: {builder.Configuration["Jwt:Audience"]}");
//         }
//     }
//     await next.Invoke();
// });
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
