using MaltalistApi.Models;

namespace MaltalistApi.Interfaces;

public interface IReportsService
{
    Task<Report> CreateReportAsync(CreateReportRequest request);
    Task<List<Report>> GetAllReportsAsync(string? status = null);
    Task<Report?> GetReportByIdAsync(int id);
    Task<List<Report>> GetReportsByListingIdAsync(int listingId);
    Task<Report?> UpdateReportStatusAsync(int id, UpdateReportStatusRequest request);
    Task<bool> DeleteReportAsync(int id);
}
