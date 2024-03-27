using imageUploaderAPI;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using System.Xml.Linq;
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAntiforgery();
var app = builder.Build();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapGet("/", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context);
    var htmlContent = $@"
            <!DOCTYPE html>
            <html lang=""en"">

            <head>
                <meta charset=""UTF-8"">
                <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                <title>Image Uploader</title>
                <link href=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/css/bootstrap.min.css"" rel=""stylesheet""
                    integrity=""sha384-QWTKZyjpPEjISv5WaRU9OFeRpok6YctnYmDr5pNlyT2bRjXh0JMhjY6hW+ALEwIH"" crossorigin=""anonymous"">
                <link rel=""stylesheet"" href=""index.css"">
            </head>

            <body>
                <div class=""container-fluid my-4 d-flex justify-content-center squareBox align-items-center"">
                    <form action=""/api"" method=""post"" enctype=""multipart/form-data"" class=""card p-4 needs-validation"">
                        <input name=""{token.FormFieldName}"" type=""hidden"" value=""{token.RequestToken}"" />
                        <div class=""mb-3"">
                            <label for=""imageName"" class=""form-label"">Image Title</label>
                            <input type=""text"" class=""form-control"" id=""imageName"" name=""name"" required>
                            <div class=""nameValidation validation"">
                                Name is required.
                            </div>
                        </div>
                        <div class=""mb-3"">
                            <label for=""imgExtension"" class=""form-label"">Choose an image</label>
                            <input type=""file"" accept=""image"" class=""form-control"" id=""imgExtension"" name=""img"" required>
                            <div class=""imgValidation validation"">
                                Image is required. PNG or JPEG only.
                            </div>
                        </div>
                        <button type=""submit"" class=""btn btn-dark"">Submit</button>
                    </form>
                </div>
                <script src=""https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js""
                        integrity=""sha384-YvpcrYf0tY3lHB60NNkmXc5s9fDVZLESaAA55NDzOxhy9GkcIdslK1eN7N6jIeHz""
                        crossorigin=""anonymous""></script>
                <script src=""https://cdn.jsdelivr.net/npm/axios/dist/axios.min.js""></script>
                <script>
                    const imgInput = document.querySelector(""#imgExtension"");
                    const nameInput = document.querySelector(""#imageName"");
                    const form = document.querySelector(""form"");
                    function validate() {{
                    if (nameInput.value === """") {{
                        document.querySelector("".nameValidation"").classList.remove(""d-none"");
                        return false;
                    }} else {{
                        document.querySelector("".nameValidation"").classList.add(""d-none"");
                    }}
                    if (imgInput.value === """" || !imgInput.value.match(/\.(jpeg|png)$/)) {{
                        document.querySelector("".imgValidation"").classList.remove(""d-none"");
                        return false;
                    }} else {{
                    document.querySelector("".imgValidation"").classList.add(""d-none"");
                    }}
                    return true;
                    }}

                    imgInput.addEventListener(""change"", validate);
                    nameInput.addEventListener(""change"", validate);
                </script>
            </body>

            </html>
    ";
    return Results.Content(htmlContent, "text/html");
});

app.MapPost("/api", async (IFormFile img, [FromForm] string name,HttpContext context, IAntiforgery antiforgery) =>
{
    try
    {
        await antiforgery.ValidateRequestAsync(context);
        if (string.IsNullOrWhiteSpace(name) || img == null)
        {
            return Results.BadRequest("Name and Image are required.");
        }

        if (img != null)
        {
            var allowedExtensions = new[] { ".png", ".gif", ".jpeg" };
            var fileExtension = Path.GetExtension(img.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                return Results.BadRequest("Only PNG, GIF, and JPEG images are allowed.");
            }
        }

        var data = new pictureData
        {
            Name = name,
            Image = img.FileName,
            id = Guid.NewGuid().ToString()
        };
        var json = JsonSerializer.Serialize(data);
        var filePath = Path.Combine(app.Environment.ContentRootPath, "images.json");
        Directory.CreateDirectory("data");
        var imageFilePath = Path.Combine("data", $"{data.id}{Path.GetExtension(data.Image)}");
        using (var stream = new FileStream(imageFilePath, FileMode.Create))
        {
            await img.CopyToAsync(stream);
        }
        await File.WriteAllTextAsync(filePath, json);
        return Results.Redirect($"/picture/{data.id}");
    }catch(AntiforgeryValidationException e)
    {
        return Results.BadRequest("Invalid anti-forgery token");
    }
});
app.MapGet("/data/{id}", async (string id, HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "images.json");

    if (!File.Exists(filePath))
    {
        return Results.NotFound("Json Object not found");
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
        return Results.NotFound("Image not found");
    }
});

app.MapGet("/picture/{id}", async (string id,HttpContext context) =>
{
    var filePath = Path.Combine(app.Environment.ContentRootPath, "images.json");

    if (!File.Exists(filePath))
    {
        return Results.NotFound("Json Object not found");
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
    return Results.Content(htmlContent,"text/html");
});

app.Run();
