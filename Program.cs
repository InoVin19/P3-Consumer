using Consumer.Hubs;
using Consumer.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on all interfaces for Docker
builder.WebHost.UseUrls("http://*:5000");

// Add services to the container
builder.Services.AddLogging();
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// Add custom services
builder.Services.AddSingleton<ConfigService>();
builder.Services.AddSingleton<QueueManager>();
builder.Services.AddSingleton<VideoStorageService>();
builder.Services.AddSingleton<ConsumerManager>();
builder.Services.AddSingleton<TcpListenerService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<ConsumerManager>());
builder.Services.AddHostedService(provider => provider.GetRequiredService<TcpListenerService>());

var app = builder.Build();

// Set the hub context for VideoHub
var hubContext = app.Services.GetRequiredService<IHubContext<VideoHub>>();
VideoHub.SetHubContext(hubContext);

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Use top level route registrations instead of UseEndpoints
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Map SignalR hub
app.MapHub<VideoHub>("/videoHub");

// Note: ConsumerManager and TcpListenerService are now started automatically as hosted services
// No need to manually call StartConsumers

app.Run();
