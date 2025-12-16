using Azure.Identity;
using Azure.Storage.Blobs;
using ExamAzure.Models;
using ExamAzure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<MiniGalleryOptions>(builder.Configuration.GetSection("MiniGallery"));

builder.Services.AddApplicationInsightsTelemetry();

var connectionString = builder.Configuration.GetValue<string>("AzureStorage:ConnectionString");
builder.Services.AddSingleton(new BlobServiceClient(connectionString));

builder.Services.AddScoped<IBlobService, AzureBlobService>();
builder.Services.AddScoped<IContentInspector, BasicContentInspector>();
builder.Services.AddSingleton<IClock, SystemClock>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
app.Run();
