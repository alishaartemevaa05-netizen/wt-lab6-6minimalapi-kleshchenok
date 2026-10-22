using System.Globalization;
using System.Text.Json.Serialization;
using wt_lab6_6minimalapi_kleshchenok.Data;
using wt_lab6_6minimalapi_kleshchenok.Helpers;
using wt_lab6_6minimalapi_kleshchenok.Models;

// Единая культура: точка как десятичный разделитель (так же, как в WT-5).
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

// Перечисления передаём в JSON строками ("Suv")
builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// OpenAPI
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseStaticFiles(); // отдаёт содержимое wwwroot/ (картинки, видео, swagger.html)

app.MapOpenApi(); // GET /openapi/v1.json

//Базовые endpoints 

// GET / 
// curl/Postman/скрипты — текст "Minimal API работает!" (как требует методичка).
app.MapGet("/", (HttpRequest request, IWebHostEnvironment env) =>
{
    var wantsHtml = request.Headers.Accept.ToString().Contains("text/html", StringComparison.OrdinalIgnoreCase);
    return wantsHtml
        ? Results.File(Path.Combine(env.WebRootPath, "index.html"), "text/html; charset=utf-8")
        : Results.Text("Minimal API работает!", "text/plain; charset=utf-8");
}).ExcludeFromDescription();

// Всегда текстовый ответ
app.MapGet("/ping", () => "Minimal API работает!")
   .WithSummary("Проверка работы приложения (текст)");

var api = app.MapGroup("/api"); // общий префикс /api для всех остальных endpoints

// GET /api/info — анонимный объект C# автоматически сериализуется в JSON.
api.MapGet("/info", () => new
{
    Application = "Web Technologies",
    Technology = "ASP.NET Core Minimal API",
    Version = "1.0",
    Domain = "Прокат автомобилей",
    Author = "А.А. Клещёнок"
}).WithTags("Info").WithSummary("Информация о приложении");

// GET /api/stats — сводка для главной страницы.
api.MapGet("/stats", () => new
{
    TotalCars = CarStore.GetAll().Count,
    AvailableCars = CarStore.GetAll().Count(c => c.IsAvailable),
    TotalCustomers = CustomerStore.GetAll().Count,
    ActiveRentals = RentalStore.GetAll().Count(r => r.Status == RentalStatus.Active)
}).WithTags("Info").WithSummary("Сводная статистика");

// CRUD для Car

var cars = api.MapGroup("/cars").WithTags("Cars");

// GET /api/cars?search=kia&category=Suv&available=true 
cars.MapGet("", (string? search, CarCategory? category, bool? available) =>
{
    var query = CarStore.GetAll().AsEnumerable();

    if (!string.IsNullOrWhiteSpace(search))
        query = query.Where(c =>
            c.Brand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            c.Model.Contains(search, StringComparison.OrdinalIgnoreCase));

    if (category.HasValue)
        query = query.Where(c => c.Category == category.Value);

    if (available.HasValue)
        query = query.Where(c => c.IsAvailable == available.Value);

    return Results.Ok(query.OrderBy(c => c.Id));            // 200 OK
}).WithSummary("Получить список автомобилей (с фильтрами)");

// GET /api/cars/{id} — параметр маршрута. 
cars.MapGet("/{id:int}", (int id) =>
{
    var car = CarStore.GetById(id);
    return car is null ? Results.NotFound() : Results.Ok(car);   // 404 / 200
}).WithSummary("Получить автомобиль по Id");

// POST /api/cars — объект приходит в теле запроса (JSON), Id назначает сервер.
cars.MapPost("", (Car car) =>
{
    var errors = ModelValidator.Validate(car);
    if (errors is not null) return Results.ValidationProblem(errors);   // 400 Bad Request

    CarStore.Add(car);                                                  // Id = max + 1
    return Results.Created($"/api/cars/{car.Id}", car);                 // 201 Created + заголовок Location
}).WithSummary("Создать автомобиль");

// PUT /api/cars/{id} — полная замена свойств существующего объекта.
cars.MapPut("/{id:int}", (int id, Car updated) =>
{
    var errors = ModelValidator.Validate(updated);
    if (errors is not null) return Results.ValidationProblem(errors);   // 400

    var car = CarStore.Update(id, updated);
    return car is null ? Results.NotFound() : Results.Ok(car);          // 404 / 200
}).WithSummary("Изменить автомобиль");

// DELETE /api/cars/{id}
cars.MapDelete("/{id:int}", (int id) =>
{
    if (CarStore.GetById(id) is null) return Results.NotFound();        // 404

    if (RentalStore.HasActiveRental(id))                                /
        return Results.Conflict(new { error = "Нельзя удалить автомобиль с активной арендой." }); // 409

    CarStore.Remove(id);
    return Results.NoContent();                                         // 204 No Content
}).WithSummary("Удалить автомобиль");

// Связанные сущности (из WT-5)

var customers = api.MapGroup("/customers").WithTags("Customers");

// Клиенты: полный CRUD, аналогично автомобилям.
customers.MapGet("", () => Results.Ok(CustomerStore.GetAll()))
         .WithSummary("Список клиентов");

customers.MapGet("/{id:int}", (int id) =>
{
    var customer = CustomerStore.GetById(id);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
}).WithSummary("Клиент по Id");

customers.MapPost("", (Customer customer) =>
{
    var errors = ModelValidator.Validate(customer);
    if (errors is not null) return Results.ValidationProblem(errors);   // 400

    CustomerStore.Add(customer);
    return Results.Created($"/api/customers/{customer.Id}", customer);  // 201
}).WithSummary("Создать клиента");

customers.MapPut("/{id:int}", (int id, Customer updated) =>
{
    var errors = ModelValidator.Validate(updated);
    if (errors is not null) return Results.ValidationProblem(errors);   // 400

    var customer = CustomerStore.Update(id, updated);
    return customer is null ? Results.NotFound() : Results.Ok(customer); // 404 / 200
}).WithSummary("Изменить клиента");

customers.MapDelete("/{id:int}", (int id) =>
{
    if (CustomerStore.GetById(id) is null) return Results.NotFound();   // 404

    if (RentalStore.HasActiveRentalForCustomer(id))
        return Results.Conflict(new { error = "Нельзя удалить клиента с активной арендой." }); // 409

    CustomerStore.Remove(id);
    return Results.NoContent();                                         // 204
}).WithSummary("Удалить клиента");

var rentals = api.MapGroup("/rentals").WithTags("Rentals");

// GET /api/rentals — возвращаем DTO с названиями вместо Id.
rentals.MapGet("", () =>
{
    var list = RentalStore.GetAll().Select(r => new RentalListItem
    {
        Id = r.Id,
        CarId = r.CarId,
        CarName = CarStore.GetById(r.CarId)?.FullName ?? "(удалён)",
        CustomerId = r.CustomerId,
        CustomerName = CustomerStore.GetById(r.CustomerId)?.FullName ?? "(удалён)",
        StartDate = r.StartDate,
        EndDate = r.EndDate,
        TotalPrice = r.TotalPrice,
        Status = r.Status
    });
    return Results.Ok(list);
}).WithSummary("Список аренд");

// POST /api/rentals — оформить аренду: сервер сам считает стоимость и «занимает» автомобиль.
rentals.MapPost("", (RentalCreateRequest req) =>
{
    var errors = ModelValidator.Validate(req) ?? new Dictionary<string, string[]>();

    if (req.EndDate.Date <= req.StartDate.Date)
        errors["endDate"] = new[] { "Дата окончания должна быть позже даты начала" };

    var car = CarStore.GetById(req.CarId);
    var customer = CustomerStore.GetById(req.CustomerId);
    if (car is null) errors["carId"] = new[] { "Автомобиль не найден" };
    if (customer is null) errors["customerId"] = new[] { "Клиент не найден" };

    if (errors.Count > 0) return Results.ValidationProblem(errors);     // 400
    if (!car!.IsAvailable) return Results.Conflict(new { error = "Автомобиль сейчас недоступен." }); // 409

    var days = (req.EndDate.Date - req.StartDate.Date).Days;
    var rental = RentalStore.Add(new Rental
    {
        CarId = car.Id,
        CustomerId = customer!.Id,
        StartDate = req.StartDate.Date,
        EndDate = req.EndDate.Date,
        TotalPrice = days * car.DailyPrice,
        Status = RentalStatus.Active
    });
    car.IsAvailable = false;
    return Results.Created($"/api/rentals/{rental.Id}", rental);        // 201
}).WithSummary("Оформить аренду");

// GET /api/rentals/{id}
rentals.MapGet("/{id:int}", (int id) =>
{
    var rental = RentalStore.GetById(id);
    return rental is null ? Results.NotFound() : Results.Ok(rental);
}).WithSummary("Аренда по Id");

// DELETE /api/rentals/{id} — удалить можно только завершённую аренду.
rentals.MapDelete("/{id:int}", (int id) =>
{
    var rental = RentalStore.GetById(id);
    if (rental is null) return Results.NotFound();                      // 404
    if (rental.Status == RentalStatus.Active)
        return Results.Conflict(new { error = "Сначала завершите аренду." }); // 409

    RentalStore.Remove(id);
    return Results.NoContent();                                         // 204
}).WithSummary("Удалить завершённую аренду");

// POST /api/rentals/{id}/complete — завершить аренду и вернуть автомобиль в парк.
rentals.MapPost("/{id:int}/complete", (int id) =>
{
    var rental = RentalStore.Complete(id);
    if (rental is null) return Results.NotFound();

    var car = CarStore.GetById(rental.CarId);
    if (car is not null) car.IsAvailable = true;
    return Results.NoContent();                                         // 204
}).WithSummary("Завершить аренду");

// ───────────────────────── Блок 7. Порт для Render ─────────────────────────

// Render передаёт порт в переменной окружения PORT; локально используем 10000.
// 0.0.0.0 — слушать на всех сетевых интерфейсах (иначе контейнер недоступен снаружи).
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
app.Run($"http://0.0.0.0:{port}");
