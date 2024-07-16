using AngularDemoAppAPIS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Npgsql;
using Razorpay.Api;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificorigin",
        builder => builder.WithOrigins("http://localhost:4200")
        .AllowAnyHeader().AllowAnyMethod());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Demo API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[]{ }
        }
    });
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddLogging();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<IEmailService, EmailService>();
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
// Configuration for email settings
var configuration = builder.Configuration;
var dbConfiguration = configuration.GetSection("DBConfiguration").Get<DBConfiguration>();
var connectionString = $"Host={dbConfiguration!.Host};Port={dbConfiguration.Port};Username={dbConfiguration.Username};Password={dbConfiguration.Password};Database={dbConfiguration.Database}";
try
{
    using (var tempConnection = new NpgsqlConnection(connectionString))
    {
        tempConnection.Open();
        if (tempConnection.State == System.Data.ConnectionState.Open)
        {
            tempConnection.Close();
            Console.WriteLine("Database connection successful!");
            builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine("Database connection error...");
    Console.WriteLine(ex.Message);
}
builder.Services.Configure<JwtOption>(builder.Configuration.GetSection("Jwt"));
var key = builder.Configuration.GetSection("Jwt:key").Get<string>();
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o =>
{
    o.RequireHttpsMetadata = false;
    o.SaveToken = true;
    o.TokenValidationParameters = new TokenValidationParameters()
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new  SymmetricSecurityKey(Encoding.ASCII.GetBytes(key!)),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});
var razorpayOptions = configuration.GetSection("Razorpay");
var razorpayApiKey = razorpayOptions.GetValue<string>("ApiKey");
var razorpayApiSecret = razorpayOptions.GetValue<string>("ApiSecret");

builder.Services.AddSingleton<RazorpayClient>(_ =>
    new RazorpayClient(razorpayApiKey, razorpayApiSecret));
var app = builder.Build();
app.UseCors("AllowSpecificorigin");
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
