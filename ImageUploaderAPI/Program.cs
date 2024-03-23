using imageUploaderAPI;
using System.Text.Json;
using System.Xml;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
// Configure the HTTP request pipeline.

app.MapPost("/api", async (HttpContext context) =>
{
    var form = await context.Request.ReadFormAsync();
    var file = form.Files.GetFile("img");
    if (string.IsNullOrWhiteSpace(form["name"]) || form.Files["img"]==null)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsync("Name and Image are required.");
        return;
    }

    if (file != null)
    {
        var allowedExtensions = new[] { ".png", ".gif", ".jpeg" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(fileExtension))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Only PNG, GIF, and JPEG images are allowed.");
            return;
        }
    }

    var data = new pictureData
    {
        Name = form["name"],
        Image = file.FileName,
        id = Guid.NewGuid().ToString()
    };
    var json = JsonSerializer.Serialize(data);
    var filePath = Path.Combine(app.Environment.ContentRootPath,"images.json");
    var imageFilePath = Path.Combine(app.Environment.ContentRootPath, "data", $"{data.id}{Path.GetExtension(data.Image)}");
    using (var stream = new FileStream(imageFilePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    await File.WriteAllTextAsync(filePath, json);
    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync(data.id);

});


app.MapGet("/picture/{id}", async (HttpContext context) =>
{

    var uniqueId = context.Request.RouteValues["id"]?.ToString();


    var filePath = Path.Combine(app.Environment.ContentRootPath, "images.json");

    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Json Object not found");
        return;
    }


    var jsonData = await File.ReadAllTextAsync(filePath);

    var picData = JsonSerializer.Deserialize<pictureData>(jsonData);
    var imagePath = $"{uniqueId}{Path.GetExtension(picData.Image)}";

    var htmlContent = $@"
        <!DOCTYPE html>
        <html lang='en'>
        <head>
            <meta charset='UTF-8'>
            <meta name='viewport' content='width=device-width, initial-scale=1.0'>
            <title>Picture</title>
        </head>
        <body>
            <h1>{picData.Name}</h1>
            <img src='../data/{imagePath}' alt='{picData.Name}'>
        </body>
        </html>";

    context.Response.ContentType = "text/html";


    await context.Response.WriteAsync(htmlContent);
});

app.UseHttpsRedirection();

app.UseAuthorization();


app.Run();
