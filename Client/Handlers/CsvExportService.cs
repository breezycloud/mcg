using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;
using Microsoft.JSInterop;
using Shared.Interfaces;

namespace Client.Handlers;


public class CsvExportService : IExportService
{
    private readonly IJSRuntime _js;

    public CsvExportService(IJSRuntime js)
    {
        _js = js;
    }

    public async Task ExportToCsv<T>(List<T> data, string fileName)
    {
        try
        {
            var csvContent = GenerateCsvContent(data);
            var fileBytes = Encoding.UTF8.GetBytes(csvContent);

            // For Blazor Server - you might want to save to a temporary file
            // or stream directly to the browser
            // var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            // await File.WriteAllBytesAsync(tempPath, fileBytes);

            // Trigger file download
            await TriggerFileDownload(fileBytes, fileName, "text/csv");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to export CSV: {ex.Message}", ex);
        }
    }

    public async Task ExportCsvContent(byte[] csvContent, string fileName)
    {
        try
        {
            // For Blazor Server - save to temporary file and trigger download
            var tempPath = Path.Combine(Path.GetTempPath(), fileName);
            await File.WriteAllBytesAsync(tempPath, csvContent);
            
            await TriggerFileDownload(csvContent, fileName, "text/csv");
        }
        catch (Exception ex)
        {
            throw new Exception($"Failed to export CSV: {ex.Message}", ex);
        }
    }

    private string GenerateCsvContent<T>(List<T> data)
    {
        if (data == null || !data.Any())
            return string.Empty;

        var properties = typeof(T).GetProperties();
        var csv = new StringBuilder();

        // Add header row
        var headerNames = properties.Select(p =>
        {
            var displayAttr = p.GetCustomAttribute<DisplayAttribute>();
            return displayAttr?.Name ?? p.Name;
        });
        csv.AppendLine(string.Join(",", headerNames));

        // Add data rows
        foreach (var item in data)
        {
            var values = properties.Select(p =>
            {
                var value = p.GetValue(item);
                return EscapeCsvValue(value?.ToString() ?? string.Empty);
            });
            csv.AppendLine(string.Join(",", values));
        }

        return csv.ToString();
    }

    private string EscapeCsvValue(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        // Escape quotes and wrap in quotes if contains comma, newline, or quote
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
        {
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }

        return value;
    }

    private async Task TriggerFileDownload(byte[] fileBytes, string fileName, string contentType)
    {
        // This implementation depends on your Blazor setup (Server/WebAssembly)
        // For Blazor Server, you might use JS interop to trigger download
        // For Blazor WebAssembly, you can use the DownloadFile method

        // Example for Blazor Server with JS interop:
        // await JSRuntime.InvokeVoidAsync("downloadFile", 
        //     Convert.ToBase64String(fileBytes), fileName, contentType);
        
        await _js.InvokeVoidAsync("downloadReport", fileName,  Convert.ToBase64String(fileBytes));
    }

    public Task ExportToExcel<T>(List<T> data, string fileName)
    {
        // Keep existing Excel export logic or throw NotImplementedException
        throw new NotImplementedException("Excel export not implemented for CSV service");
    }

    public Task ExportToPdf<T>(List<T> data, string fileName)
    {
        // Keep existing PDF export logic or throw NotImplementedException
        throw new NotImplementedException("PDF export not implemented for CSV service");
    }
}