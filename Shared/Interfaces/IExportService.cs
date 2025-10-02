using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text;

namespace Shared.Interfaces;


public interface IExportService
{
    Task ExportToCsv<T>(List<T> data, string fileName);
     Task ExportCsvContent(byte[] csvContent, string fileName);
    Task ExportToExcel<T>(List<T> data, string fileName); // Keep Excel for other uses
    Task ExportToPdf<T>(List<T> data, string fileName);  // Keep PDF for other uses
}