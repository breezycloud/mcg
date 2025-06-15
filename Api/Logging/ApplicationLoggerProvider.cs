using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Api.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace Api.Logging;
public class ApplicationLoggerProvider : ILoggerProvider
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public ApplicationLoggerProvider(IDbContextFactory<AppDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }
    public ILogger CreateLogger(string categoryName)
    {
        return new DatabaseLogger(_contextFactory);
    }

    public void Dispose()
    {

    }
}