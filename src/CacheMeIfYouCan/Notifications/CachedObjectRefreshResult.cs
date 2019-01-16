using System;

namespace CacheMeIfYouCan.Notifications
{
    public abstract class CachedObjectRefreshResult
    {
        private protected CachedObjectRefreshResult(
            DateTime start,
            TimeSpan duration,
            CachedObjectRefreshException exception,
            int refreshAttemptCount,
            int successfulRefreshCount,
            DateTime lastRefreshAttempt,
            DateTime lastSuccessfulRefresh)
        {
            Start = start;
            Duration = duration;
            Success = exception == null;
            Exception = exception;
            RefreshAttemptCount = refreshAttemptCount;
            SuccessfulRefreshCount = successfulRefreshCount;
            LastRefreshAttempt = lastRefreshAttempt;
            LastSuccessfulRefresh = lastSuccessfulRefresh;
        }
        
        public DateTime Start { get; }
        public TimeSpan Duration { get; }
        public bool Success { get; }
        public CachedObjectRefreshException Exception { get; }
        public int RefreshAttemptCount { get; }
        public int SuccessfulRefreshCount { get; }
        public DateTime LastRefreshAttempt { get; }
        public DateTime LastSuccessfulRefresh { get; }
    }

    public sealed class CachedObjectRefreshResult<T> : CachedObjectRefreshResult
    {
        internal CachedObjectRefreshResult(
            DateTime start,
            TimeSpan duration,
            CachedObjectRefreshException<T> exception,
            T newValue,
            int refreshAttemptCount,
            int successfulRefreshCount,
            DateTime lastRefreshAttempt,
            DateTime lastSuccessfulRefresh)
            : base(start, duration, exception, refreshAttemptCount, successfulRefreshCount, lastRefreshAttempt, lastSuccessfulRefresh)
        {
            NewValue = newValue;
        }
        
        public T NewValue { get; }
    }
}