var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
  options.AddPolicy("dev", p => p
      .AllowAnyHeader()
      .AllowAnyMethod()
      .WithOrigins("http://localhost:5173")
  );
});

// Data directory (use content root so we read/write files in the project tree during dev)
var dataDir = Path.Combine(builder.Environment.ContentRootPath, "Data");
Directory.CreateDirectory(dataDir);

// Attempt to load flights from JSON file; fallback to defaults
List<Flight> flights;
try
{
  var flightsPath = Path.Combine(dataDir, "flights.json");
  if (File.Exists(flightsPath))
  {
    var json = File.ReadAllText(flightsPath);
    var loaded = System.Text.Json.JsonSerializer.Deserialize<List<Flight>>(json, new System.Text.Json.JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    });
    flights = loaded?.Where(f => f is not null).ToList() ?? new();
    if (!flights.Any()) throw new Exception("No flights loaded");
  }
  else
  {
    throw new FileNotFoundException("flights.json not found");
  }
}
catch
{
  flights = new List<Flight>
    {
        new(Guid.NewGuid(), "FL100", "JFK", "LAX", new DateTime(2025, 09, 05, 8, 15, 0, DateTimeKind.Utc), new DateTime(2025, 09, 05, 11, 10, 0, DateTimeKind.Utc), "On Time"),
        new(Guid.NewGuid(), "FL200", "LAX", "ORD", new DateTime(2025, 09, 05, 12, 30, 0, DateTimeKind.Utc), new DateTime(2025, 09, 05, 16, 05, 0, DateTimeKind.Utc), "Delayed"),
        new(Guid.NewGuid(), "FL300", "SEA", "DEN", new DateTime(2025, 09, 05, 9, 45, 0, DateTimeKind.Utc), new DateTime(2025, 09, 05, 13, 00, 0, DateTimeKind.Utc), "Boarding"),
    };
}

// Load reports from JSON if present, else start empty; save after create/update
var reportsPath = Path.Combine(dataDir, "reports.json");
List<Report> reports;
try
{
  if (File.Exists(reportsPath))
  {
    var json = File.ReadAllText(reportsPath);
    reports = System.Text.Json.JsonSerializer.Deserialize<List<Report>>(json, new System.Text.Json.JsonSerializerOptions
    {
      PropertyNameCaseInsensitive = true
    }) ?? new List<Report>();
  }
  else
  {
    reports = new List<Report>();
  }
}
catch
{
  reports = new List<Report>();
}

void SaveReports()
{
  try
  {
    var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
    var json = System.Text.Json.JsonSerializer.Serialize(reports, options);
    File.WriteAllText(reportsPath, json);
  }
  catch
  {
    // Swallow for now; in production log this.
  }
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
  app.UseSwagger();
  app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Apply CORS policy
app.UseCors("dev");

// GET /api/flights - all flights
app.MapGet("/api/flights", () => Results.Ok(flights))
    .WithName("GetFlights")
    .WithOpenApi();

// GET /api/reports - all reports
app.MapGet("/api/reports", () => Results.Ok(reports))
    .WithName("GetReports")
    .WithOpenApi();

// POST /api/reports - create report
app.MapPost("/api/reports", (CreateReportRequest req) =>
{
  if (string.IsNullOrWhiteSpace(req.Title))
    return Results.BadRequest("Title is required");

  var report = new Report
  {
    Id = Guid.NewGuid(),
    Title = req.Title.Trim(),
    Description = req.Description?.Trim(),
    CreatedUtc = DateTime.UtcNow,
    FlightIds = (req.FlightIds ?? new List<Guid>()).Where(id => flights.Any(f => f.Id == id)).Distinct().ToList()
  };
  reports.Add(report);
  SaveReports();
  return Results.Created($"/api/reports/{report.Id}", report);
})
    .WithName("CreateReport")
    .WithOpenApi();

// GET /api/reports/{id}
app.MapGet("/api/reports/{id:guid}", (Guid id) =>
{
  var rpt = reports.FirstOrDefault(r => r.Id == id);
  return rpt is null ? Results.NotFound() : Results.Ok(rpt);
})
    .WithName("GetReportById")
    .WithOpenApi();

// PUT /api/reports/{id}
app.MapPut("/api/reports/{id:guid}", (Guid id, UpdateReportRequest req) =>
{
  var rpt = reports.FirstOrDefault(r => r.Id == id);
  if (rpt is null) return Results.NotFound();
  if (!string.IsNullOrWhiteSpace(req.Title)) rpt.Title = req.Title.Trim();
  if (req.Description is not null) rpt.Description = req.Description.Trim();
  if (req.FlightIds is not null)
  {
    rpt.FlightIds = req.FlightIds.Where(fid => flights.Any(f => f.Id == fid)).Distinct().ToList();
  }
  rpt.UpdatedUtc = DateTime.UtcNow;
  SaveReports();
  return Results.Ok(rpt);
})
    .WithName("UpdateReport")
    .WithOpenApi();

// NOTE: DELETE intentionally not implemented as per README.

app.Run();

// Domain models / DTOs
record Flight(Guid Id, string Number, string Origin, string Destination, DateTime DepartureUtc, DateTime ArrivalUtc, string Status);

class Report
{
  public Guid Id { get; set; }
  public string Title { get; set; } = string.Empty;
  public string? Description { get; set; }
  public List<Guid> FlightIds { get; set; } = new();
  public DateTime CreatedUtc { get; set; }
  public DateTime? UpdatedUtc { get; set; }
}

record CreateReportRequest(string Title, string? Description, List<Guid>? FlightIds);
record UpdateReportRequest(string? Title, string? Description, List<Guid>? FlightIds);
