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
    public async Task<IActionResult> GetVisits(int id)
    {
        try
        {
            var visit = await _dbService.GetVisit(id);
            return Ok(visit);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpPost]
    public async Task<IActionResult> PostVisit([FromBody] InsertVisitDTO visit)
    {
        try
        {
            await _dbService.PostVisit(visit);
            return CreatedAtAction(nameof(PostVisit), new {id=visit.VisitId}, visit.VisitId);
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
        
    }
}