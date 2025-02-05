
using Microsoft.EntityFrameworkCore;
using TodoApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ToDoDbContext>(options =>
    options.UseMySql(builder.Configuration.GetConnectionString("ToDoDB"), ServerVersion.Parse("8.0.41-mysql")));

builder.Services.AddCors(options =>
    options.AddPolicy("AllowAll", p => p
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (builder.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty; 
    });
}

app.UseCors("AllowAll");

app.MapGet("/items", async (ToDoDbContext dbContext) =>
{
    return Results.Json(await dbContext.Items.ToListAsync());
});

app.MapGet("/items/{id}", async (int id, HttpContext context, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id);

    if (item == null)
    {
        return Results.NotFound(new { message = "Item not found" });
    }

    return Results.Ok(item);
});

app.MapPost("/items", async (HttpContext context, ToDoDbContext dbContext) =>
{
    var itemData = await context.Request.ReadFromJsonAsync<ItemDto>();
    if (itemData == null || string.IsNullOrWhiteSpace(itemData.Name))
    {
        return Results.BadRequest(new { message = "Invalid item data" });
    }

    var item = new Item { Name = itemData.Name, isComplete = false };
    dbContext.Items.Add(item);
    await dbContext.SaveChangesAsync();

    return Results.Created($"/items/{item.Id}", item);
});

app.MapPut("/items/{id}", async (int id, HttpContext context, ToDoDbContext dbContext) =>
{
    var itemUpdated = await dbContext.Items.FindAsync(id);
    if (itemUpdated == null)
    {
        return Results.NotFound(new { message = "Item not found" });
    }

    var isComplete = await context.Request.ReadFromJsonAsync<bool?>();
    if (isComplete == null)
    {
        return Results.BadRequest(new { message = "Invalid item data" });
    }

    itemUpdated.isComplete =isComplete.Value;

    dbContext.Update(itemUpdated);
    await dbContext.SaveChangesAsync();

    return Results.Ok(itemUpdated);
});

app.MapDelete("/items/{id}", async (int id, HttpContext context, ToDoDbContext dbContext) =>
{
    var item = await dbContext.Items.FindAsync(id);
    if (item == null)
    {
        return Results.NotFound(new { message = "Item not found" });
    }

    dbContext.Items.Remove(item);
    await dbContext.SaveChangesAsync();

    return Results.Ok(new { message = $"Item {id} was deleted" });
});

app.Run();