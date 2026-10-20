using Application.WebApi.Service;
using Application.WebApi.Service.Lazada;
using Application.WebApi.Service.Shopee;
using Microsoft.AspNetCore.HttpOverrides;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


//new added
builder.Services.AddHttpClient();

//builder.Services.AddTransient<QueryService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register NetsuiteService with Dependency Injection
builder.Services.AddSingleton<NetsuiteService>();
builder.Services.AddSingleton<ZaloraImageService>();
// Lazada Services
builder.Services.AddSingleton<LazadaClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    return new LazadaClient(httpClient, config);
});
builder.Services.AddSingleton<LazadaAuthService>();
// ← Shopee Services
builder.Services.AddSingleton<ShopeeClient>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var config = sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>();
    return new ShopeeClient(httpClient, config);
});
builder.Services.AddSingleton<ShopeeAuthService>();

var app = builder.Build();

var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

// Dev-friendly: allow forwarded headers from ngrok without strict proxy IP checks
forwardedHeadersOptions.KnownNetworks.Clear();
forwardedHeadersOptions.KnownProxies.Clear();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseSwagger(); // Enable Swagger in production
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
        options.RoutePrefix = string.Empty; // Set to empty if you want Swagger at the root URL
        options.UseRequestInterceptor("(req) => { req.headers['ngrok-skip-browser-warning'] = 'true'; return req; }");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
