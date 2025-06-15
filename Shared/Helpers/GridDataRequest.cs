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
        public string? SearchTerm { get; set; }
        public int Page {  get; set; }
        public int PageSize { get; set; }
    }
}
