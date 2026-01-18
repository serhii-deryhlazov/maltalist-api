using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MaltalistApi.Models;
using MaltalistApi.Interfaces;

namespace MaltalistApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportsService _reportsService;

    public ReportsController(IReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    /// <summary>
    /// Create a new report for a listing
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<Report>> CreateReport([FromBody] CreateReportRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { Message = "Reason is required." });
            }

            var report = await _reportsService.CreateReportAsync(request);
            return CreatedAtAction(nameof(GetReportById), new { id = report.Id }, report);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while creating the report.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get all reports, optionally filtered by status
    /// </summary>
    [HttpGet]
    [Authorize] // Only authenticated users (admins) can view all reports
    public async Task<ActionResult<List<Report>>> GetAllReports([FromQuery] string? status = null)
    {
        try
        {
            var reports = await _reportsService.GetAllReportsAsync(status);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving reports.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific report by ID
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<Report>> GetReportById(int id)
    {
        try
        {
            var report = await _reportsService.GetReportByIdAsync(id);
            if (report == null)
            {
                return NotFound(new { Message = $"Report with ID {id} not found." });
            }

            return Ok(report);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving the report.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Get all reports for a specific listing
    /// </summary>
    [HttpGet("listing/{listingId}")]
    [Authorize]
    public async Task<ActionResult<List<Report>>> GetReportsByListingId(int listingId)
    {
        try
        {
            var reports = await _reportsService.GetReportsByListingIdAsync(listingId);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while retrieving reports.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Update the status of a report (e.g., mark as reviewed, resolved, or dismissed)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize]
    public async Task<ActionResult<Report>> UpdateReportStatus(int id, [FromBody] UpdateReportStatusRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Status))
            {
                return BadRequest(new { Message = "Status is required." });
            }

            var report = await _reportsService.UpdateReportStatusAsync(id, request);
            if (report == null)
            {
                return NotFound(new { Message = $"Report with ID {id} not found." });
            }

            return Ok(report);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while updating the report.", Details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a report
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> DeleteReport(int id)
    {
        try
        {
            var result = await _reportsService.DeleteReportAsync(id);
            if (!result)
            {
                return NotFound(new { Message = $"Report with ID {id} not found." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Message = "An error occurred while deleting the report.", Details = ex.Message });
        }
    }
}
