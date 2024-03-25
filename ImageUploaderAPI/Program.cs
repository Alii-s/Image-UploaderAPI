using imageUploaderAPI;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

var builder = WebApplication.CreateBuilder(args);



var app = builder.Build();

app.UseStaticFiles();

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
    Directory.CreateDirectory("data");
    var imageFilePath = Path.Combine("data", $"{data.id}{Path.GetExtension(data.Image)}");
    using (var stream = new FileStream(imageFilePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    await File.WriteAllTextAsync(filePath, json);
    context.Response.StatusCode = StatusCodes.Status200OK;
    await context.Response.WriteAsync(data.id);

});
app.MapGet("/data/{id}", async (string id, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "images.json");

    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Json Object not found");
        return Results.NotFound();
    }


    var jsonData = await File.ReadAllTextAsync(filePath);

    var picData = JsonSerializer.Deserialize<pictureData>(jsonData);
    var extension = Path.GetExtension(picData.Image);
    var imagePath = Path.Combine("data", $"{id}{extension}");
    if(File.Exists(imagePath))
    {
        var image = File.OpenRead(imagePath);
        return Results.File(image, extension);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return Results.NotFound("Image not found");
    }
});

app.MapGet("/picture/{id}", async (string id,HttpContext context) =>
{



    var filePath = Path.Combine(app.Environment.ContentRootPath, "images.json");

    if (!File.Exists(filePath))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        await context.Response.WriteAsync("Json Object not found");
        return;
    }


    var jsonData = await File.ReadAllTextAsync(filePath);

    var picData = JsonSerializer.Deserialize<pictureData>(jsonData);
    var imagePath = $"/data/{id}";
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
            <img src='{imagePath}' alt='{picData.Name}'>
        </body>
        </html>";

    context.Response.ContentType = "text/html";


    await context.Response.WriteAsync(htmlContent);
});




app.Run();
