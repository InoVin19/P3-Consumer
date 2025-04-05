using Consumer.Hubs;
using Consumer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Add custom services
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<QueueManager>();
builder.Services.AddSingleton<VideoStorageService>();
builder.Services.AddSingleton<ConsumerManager>();
builder.Services.AddHostedService<TcpListenerService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VideoHub>("/videoHub");

// Start consumer threads
var configService = app.Services.GetRequiredService<ConfigService>();
var consumerManager = app.Services.GetRequiredService<ConsumerManager>();
consumerManager.StartConsumers(configService.ConsumerCount, configService.QueueLimit);

app.Run();
