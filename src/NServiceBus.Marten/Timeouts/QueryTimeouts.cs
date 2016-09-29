using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Marten;
using NServiceBus.Logging;
using NServiceBus.Timeout.Core;

namespace NServiceBus.Marten.Timeouts
{
    public class QueryTimeouts : IQueryTimeouts
    {
        static TimeoutsChunk.Timeout[] EmptyTimeouts = new TimeoutsChunk.Timeout[0];

        private readonly IDocumentStore _documentStore;
        private readonly string _endpointName;
        DateTime _lastCleanupTime = DateTime.MinValue;

        private CancellationTokenSource _shutdownTokenSource;
        private ILog _logger;
        private TimeSpan _triggerCleanupEvery;
        private TimeSpan _cleanupGapFromTimeslice;

        public QueryTimeouts(IDocumentStore documentStore, string endpointName)
        {
            _documentStore = documentStore;
            _endpointName = endpointName;

            TriggerCleanupEvery = TimeSpan.FromMinutes(2);
            CleanupGapFromTimeslice = TimeSpan.FromMinutes(1);
            _shutdownTokenSource = new CancellationTokenSource();
            _logger = LogManager.GetLogger<QueryTimeouts>();

        }

        public TimeSpan CleanupGapFromTimeslice
        {
            get { return _cleanupGapFromTimeslice; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(CleanupGapFromTimeslice));
                _cleanupGapFromTimeslice = value;
            }
        }


        public TimeSpan TriggerCleanupEvery
        {
            get { return _triggerCleanupEvery; }
            set
            {
                if (value < TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(TriggerCleanupEvery));
                _triggerCleanupEvery = value;
            }
        }

        public Func<DateTime> GetUtcNow { get; set; } = () => DateTime.UtcNow;

        public async Task<TimeoutsChunk> GetNextChunk(DateTime startSlice)
        {
            var now = GetUtcNow();
            List<TimeoutsChunk.Timeout> results;
            HashSet<string> idDedupe = null;

            // default return value for when no results are found
            var nextTimeoutToExpire = now.AddMinutes(10);

            if (CancellationRequested())
            {
                return new TimeoutsChunk(EmptyTimeouts, nextTimeoutToExpire);
            }

            // Allow for occasionally cleaning up old timeouts for edge cases where timeouts have been
            // added after startSlice have been set to a later timout and we might have missed them
            // because of stale indexes. lastCleanupTime may be DateTime.MinValue, in which case it would run.
            var nextTimeToPerformCleanup = _lastCleanupTime.Add(TriggerCleanupEvery);
            if (now > nextTimeToPerformCleanup)
            {
                results = await GetCleanupChunk(now).ConfigureAwait(false);

                // Create a HashSet of ids to avoid returning duplicate timeouts from Cleanup + Normal Query
                idDedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var timeout in results)
                {
                    idDedupe.Add(timeout.Id);
                }
            }
            else
            {
                results = new List<TimeoutsChunk.Timeout>();
            }

            if (CancellationRequested())
            {
                return new TimeoutsChunk(EmptyTimeouts, nextTimeoutToExpire);
            }

            using (var session = _documentStore.OpenSession())
            {
                var query = GetChunkQuery(session)
                    .Where(t => t.Time > startSlice && t.Time <= now)
                    .Select(to => new { to.Id, to.Time });

                foreach (var entry in await query.ToListAsync().ConfigureAwait(false))
                {
                    if (CancellationRequested())
                    {
                        return new TimeoutsChunk(EmptyTimeouts, nextTimeoutToExpire);
                    }

                    var timeoutId = entry.Id;
                    var time = entry.Time;

                    if (idDedupe != null && idDedupe.Contains(timeoutId))
                    {
                        continue;
                    }

                    results.Add(new TimeoutsChunk.Timeout(timeoutId, time));
                }

                var nextTimeout = await GetChunkQuery(session)
                        .Where(t => t.Time > now)
                        .Take(1)
                        .Select(to => new { to.Time }) // Must be anonymous type so Raven server can understand
                        .FirstOrDefaultAsync()
                        .ConfigureAwait(false);

                if (nextTimeout != null)
                {
                    // We know when the next timeout will occur, so use that time. (Although Core will query again in 1 minute max)
                    nextTimeoutToExpire = nextTimeout.Time;
                }
                else if (results.Count == 0)
                {
                    // We know we got zero results and that the index is stale. We don't want to query in a tight loop,
                    // so just delay a few seconds to ease load on the server.
                    nextTimeoutToExpire = now.AddSeconds(10);
                }
            }

            _logger.DebugFormat("Returning {0} timeouts, next due at {1:O}", results.Count, nextTimeoutToExpire);
            return new TimeoutsChunk(results.ToArray(), nextTimeoutToExpire);
        }

        public async Task<List<TimeoutsChunk.Timeout>> GetCleanupChunk(DateTime fromTime)
        {
            var cutoff = fromTime.Subtract(CleanupGapFromTimeslice);

            using (var session = _documentStore.OpenSession())
            {
                var query = await GetChunkQuery(session)
                    .Where(t => t.Time <= cutoff)
                    .Select(t => new
                    {
                        t.Id,
                        t.Time
                    })
                    .ToListAsync()
                    .ConfigureAwait(false);

                var chunk = query.Select(arg => new TimeoutsChunk.Timeout(arg.Id, arg.Time)).ToList();

                _lastCleanupTime = DateTime.UtcNow;

                return chunk;
            }
        }

        public void Shutdown()
        {
            _shutdownTokenSource.Cancel();
        }

        IQueryable<TimeoutDocument> GetChunkQuery(IDocumentSession session)
        {
            return session.Query<TimeoutDocument>()
                .OrderBy(t => t.Time)
                .Where(
                    t =>
                        t.OwningTimeoutManager == string.Empty ||
                        t.OwningTimeoutManager == _endpointName);
        }

        bool CancellationRequested()
        {
            return _shutdownTokenSource != null && _shutdownTokenSource.IsCancellationRequested;
        }
    }
}