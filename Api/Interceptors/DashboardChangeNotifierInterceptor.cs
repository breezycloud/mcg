using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Models.Incidents;
using Shared.Models.Services;
using Shared.Models.Trips;

namespace Api.Interceptors;

/// <summary>
/// Pushes a lightweight "something changed" SignalR notification through DashboardHub whenever a
/// Trip, Discharge, Incident, or ServiceRequest save actually commits, so the live dashboard (and
/// Control Room) can refetch instead of only updating on a manual filter change or poll. Deliberately
/// a separate interceptor from AuditInterceptor — a bug here must never risk the audit trail, and
/// vice versa.
///
/// The changed-entity-type set is captured pre-save (SavingChanges) and only acted on post-save
/// (SavedChanges), so a rolled-back transaction never triggers a broadcast for a change that
/// didn't actually happen. State is keyed per-DbContext-instance via ConditionalWeakTable since
/// this interceptor is a singleton shared across every concurrent request's DbContext.
///
/// IHubContext&lt;DashboardHub&gt; is deliberately NOT constructor-injected. This interceptor is
/// resolved from inside AppDbContext's own AddDbContextFactory configuration callback, and
/// resolving IHubContext&lt;DashboardHub&gt; there transitively needs a logger, which — via this
/// app's custom DB-backed ApplicationLoggerProvider/DatabaseLogger — needs an IDbContextFactory
/// &lt;AppDbContext&gt; and a fresh AppDbContext, re-entering the very callback we're already
/// inside and hanging the app on startup (confirmed by instrumentation: constructing the
/// interceptor with an injected IHubContext never returns). Instead, Program.cs resolves the hub
/// context exactly once from the root provider right after the app is built — a normal,
/// non-nested resolution — and hands it here via <see cref="HubContext"/>. Deliberately also
/// avoids ILogger&lt;T&gt; for the same reason (matches AuditInterceptor); failures go to
/// Console.Error instead.
/// </summary>
public sealed class DashboardChangeNotifierInterceptor : SaveChangesInterceptor
{
    /// <summary>Set once in Program.cs right after the app is built. Never resolved lazily from
    /// inside this class — see the class-level doc comment for why.</summary>
    public static IHubContext<DashboardHub>? HubContext { get; set; }

    private static readonly ConditionalWeakTable<DbContext, HashSet<string>> PendingChanges = new();

    // Collapses bursts (bulk dispatch, imports) into a single broadcast per entity type.
    private static readonly ConcurrentDictionary<string, DateTimeOffset> LastBroadcast = new();
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromSeconds(3);

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            CaptureChangedTypes(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            CaptureChangedTypes(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        if (eventData.Context is not null)
            _ = NotifyAsync(eventData.Context);

        return base.SavedChanges(eventData, result);
    }

    public override ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            _ = NotifyAsync(eventData.Context);

        return base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private static void CaptureChangedTypes(DbContext context)
    {
        var changed = context.ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(e => e.Entity switch
            {
                Trip => "Trip",
                Discharge => "Discharge",
                Incident => "Incident",
                ServiceRequest => "ServiceRequest",
                _ => null
            })
            .Where(t => t is not null)
            .Cast<string>()
            .ToHashSet();

        if (changed.Count == 0)
            return;

        PendingChanges.AddOrUpdate(context, changed);
    }

    // Fire-and-forget from the caller's perspective — runs after SaveChanges has already
    // committed, and any failure here is swallowed, never rethrown, so it can't fail a save
    // that already succeeded.
    private static async Task NotifyAsync(DbContext context)
    {
        if (!PendingChanges.TryGetValue(context, out var changedTypes))
            return;

        PendingChanges.Remove(context);

        var hubContext = HubContext;
        if (hubContext is null)
            return; // Not wired up yet (shouldn't happen once Program.cs finishes startup)

        foreach (var entityType in changedTypes)
        {
            var last = LastBroadcast.GetValueOrDefault(entityType, DateTimeOffset.MinValue);
            if (DateTimeOffset.UtcNow - last < ThrottleWindow)
                continue;

            LastBroadcast[entityType] = DateTimeOffset.UtcNow;

            try
            {
                await hubContext.Clients.All.SendAsync("DashboardDataChanged", entityType);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[DashboardChangeNotifierInterceptor] Failed to broadcast dashboard change for {entityType}: {ex}");
            }
        }
    }
}
