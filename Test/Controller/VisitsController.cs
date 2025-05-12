using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Test.Model;
using Test.Service;

namespace Test.Controller;

[Route("api/[controller]")]
[ApiController]
public class VisitsController : ControllerBase
{
    private readonly IDbService _dbService;

    public VisitsController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> getVisits(int id)
    {
        try
        {
            var visit = await _dbService.getVisit(id);
            return Ok(visit);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostVisit([FromBody] InsertVisitDTO visit)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest("Invalid input data");
        }
        try
        {
            var result = await _dbService.postVisit(visit);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
        
    }
}