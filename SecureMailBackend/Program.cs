using Microsoft.EntityFrameworkCore;
using SecureMailBackend.Data;
using SecureMailBackend.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

var builder = WebApplication.CreateBuilder(args);

FirebaseApp.Create(new AppOptions()
{
    Credential = GoogleCredential.FromFile("firebase-adminsdk.json"),
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddHttpClient();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddHttpClient<IVirusTotalService, VirusTotalService>();
builder.Services.AddScoped<IEmailAnalyzerService, EmailAnalyzerService>();
builder.Services.AddSingleton<IYaraScannerService, YaraScannerService>();
builder.Services.AddScoped<IImapValidatorService, ImapValidatorService>();
builder.Services.AddHostedService<SecureMailBackend.BackgroundServices.GmailPollingService>();
builder.Services.AddHostedService<SecureMailBackend.BackgroundServices.ImapPollingService>();

// ---    CORS  ---
builder.Services.AddCors(options => {
    options.AddPolicy("AllowAll", builder => {
        builder.AllowAnyOrigin() //      ()
               .AllowAnyMethod()    
               .AllowAnyHeader(); 
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseCors("AllowAll");

app.UseAuthorization();
app.MapControllers();
app.Run();