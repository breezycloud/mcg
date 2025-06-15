using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Shared.Models.Logging;
using static Microsoft.Extensions.Logging.LogLevel;
using Api.Context;
using Microsoft.EntityFrameworkCore;


namespace Api.Logging;

public class DatabaseLogger : ILogger
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public DatabaseLogger(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }

    public bool IsEnabled(LogLevel logLevel) =>
        logLevel is Error or Critical;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {

        if (!IsEnabled(logLevel))
        {
            return;
        }


        //No need to log UserId as the userId is already coming from the client error log
        LogMessage log = new()
        {
            Message = $"{logLevel} {exception?.Message} {exception?.StackTrace}",
            Source = "Server",
            CreatedAt = DateTime.Now
        };


        using var context = _contextFactory.CreateDbContext();
            context.Logs.Add(log);
            context.SaveChanges();
        }
    }