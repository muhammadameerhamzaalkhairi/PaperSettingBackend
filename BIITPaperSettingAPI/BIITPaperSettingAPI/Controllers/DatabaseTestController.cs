using BIITPaperSettingAPI.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BIITPaperSettingAPI.Controllers;

// ==========================================================
// DATABASE TEST CONTROLLER
// ==========================================================
// Sirf local development mein database connection check karega.
// Database ka koi data change nahi hoga.
// ==========================================================

[ApiController]
[Route("api/database-test")]
public class DatabaseTestController : ControllerBase
{
    private readonly BiitDbContext _context;
    private readonly IWebHostEnvironment _environment;

    // ======================================================
    // CONSTRUCTOR: REQUIRED SERVICES RECEIVE KARNA
    // ======================================================
    // Program.cs ki registration se context automatically milta hai.
    // Environment se pata chalta hai app Development mein hai ya nahi.
    // ======================================================

    public DatabaseTestController(
        BiitDbContext context,
        IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }

    // ======================================================
    // GET: /api/database-test
    // ======================================================
    // CanConnectAsync database connection check karta hai.
    // await ka matlab: result aane tak operation complete hone dein,
    // bina request thread ko block kiye.
    // ======================================================

    [HttpGet]
    public async Task<IActionResult> CheckConnection()
    {
        if (!_environment.IsDevelopment())
        {
            return NotFound();
        }

        bool isConnected = await _context.Database.CanConnectAsync();

        if (!isConnected)
        {
            return StatusCode(503, new
            {
                message = "Database connection failed."
            });
        }

        return Ok(new
        {
            message = "Database connected successfully."
        });
    }
}