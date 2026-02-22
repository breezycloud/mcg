using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Shared.Models;

namespace Shared.Helpers
{
    public class GridDataRequest
    {
        public Guid? Id { get; set; }
        public Guid? UserId { get; set; }
        public string? SearchTerm { get; set; }
        public DateOnly? Date { get; set; }
        public string? Status { get; set; }
        public int Paging => Page * PageSize;
        public int Page { get; set; }
        public int PageSize { get; set; } = 10;
        public string? Product { get; set; }
        public string? EntityType { get; set; }
        public string? Action { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
