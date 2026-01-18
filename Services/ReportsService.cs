using Microsoft.EntityFrameworkCore;
using MaltalistApi.Models;
using MaltalistApi.Interfaces;

namespace MaltalistApi.Services;

public class ReportsService : IReportsService
{
    private readonly MaltalistDbContext _db;

    public ReportsService(MaltalistDbContext db)
    {
        _db = db;
    }

    public async Task<Report> CreateReportAsync(CreateReportRequest request)
    {
        // Verify the listing exists
        var listing = await _db.Listings.FindAsync(request.ListingId);
        if (listing == null)
        {
            throw new ArgumentException($"Listing with ID {request.ListingId} not found.");
        }

        var report = new Report
        {
            ListingId = request.ListingId,
            ReporterName = request.ReporterName,
            ReporterEmail = request.ReporterEmail,
            Reason = request.Reason,
            Description = request.Description,
            Status = "pending",
            CreatedAt = DateTime.UtcNow
        };

        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        return report;
    }

    public async Task<List<Report>> GetAllReportsAsync(string? status = null)
    {
        var query = _db.Reports
            .Include(r => r.Listing)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(r => r.Status == status);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Report?> GetReportByIdAsync(int id)
    {
        return await _db.Reports
            .Include(r => r.Listing)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<List<Report>> GetReportsByListingIdAsync(int listingId)
    {
        return await _db.Reports
            .Where(r => r.ListingId == listingId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Report?> UpdateReportStatusAsync(int id, UpdateReportStatusRequest request)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report == null)
        {
            return null;
        }

        // Validate status
        var validStatuses = new[] { "pending", "reviewed", "resolved", "dismissed" };
        if (!validStatuses.Contains(request.Status.ToLower()))
        {
            throw new ArgumentException($"Invalid status. Must be one of: {string.Join(", ", validStatuses)}");
        }

        report.Status = request.Status.ToLower();
        report.ReviewedAt = DateTime.UtcNow;
        report.ReviewedBy = request.ReviewedBy;
        report.ReviewNotes = request.ReviewNotes;

        await _db.SaveChangesAsync();

        return report;
    }

    public async Task<bool> DeleteReportAsync(int id)
    {
        var report = await _db.Reports.FindAsync(id);
        if (report == null)
        {
            return false;
        }

        _db.Reports.Remove(report);
        await _db.SaveChangesAsync();

        return true;
    }
}
