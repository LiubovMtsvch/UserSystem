using Microsoft.EntityFrameworkCore;
using WebApplication.Data;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authentication.Cookies;


var builder = Microsoft.AspNetCore.Builder.WebApplication.CreateBuilder(args);

//connect to db
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";             // ���� �� ������ ����������������� �������
        options.AccessDeniedPath = "/denied";     // ���� ��� ������� �������
        options.ExpireTimeSpan = TimeSpan.FromDays(7); // ���� ����� cookie
        options.SlidingExpiration = true;
    });

// ? ����������� (��������� �������� [Authorize])
builder.Services.AddAuthorization();


// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// controller
builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();


app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();
