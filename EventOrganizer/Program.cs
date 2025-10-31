using EventOrganizer.Repository;
using EventOrganizer.Repository.Interface;
using EventOrganizer.Repository.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddTransient<ILogin, LoginRepository>();
builder.Services.AddSingleton<IErrorLogs, ErrorLogsRepository>();
builder.Services.AddScoped<IHome, HomeRepository>();

builder.Services.AddHttpContextAccessor(); 


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});
// Add session support
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Set session timeout
    options.Cookie.HttpOnly = true; // Prevent client-side scripts from accessing session cookies
    options.Cookie.IsEssential = true; // Ensure session is always stored
});
// Add distributed memory cache (required for session)
builder.Services.AddDistributedMemoryCache();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("AllowAll");
app.UseRouting();
app.UseSession(); // Enable session middleware



app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
