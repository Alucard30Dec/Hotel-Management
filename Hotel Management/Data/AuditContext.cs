using System;

namespace HotelManagement.Data
{
    public static class AuditContext
    {
        [ThreadStatic]
        private static string _currentActor;
        [ThreadStatic]
        private static string _currentCorrelationId;

        public static string CurrentActor
        {
            get => ResolveActor(null);
            set => _currentActor = NormalizeActor(value);
        }

        public static string CurrentCorrelationId
        {
            get => ResolveCorrelationId(null);
            set => _currentCorrelationId = NormalizeCorrelationId(value);
        }

        public static string ResolveActor(string fallback)
        {
            string actor = !string.IsNullOrWhiteSpace(_currentActor)
                ? _currentActor
                : fallback;

            actor = NormalizeActor(actor);
            return string.IsNullOrWhiteSpace(actor) ? "system" : actor;
        }

        public static string ResolveCorrelationId(string fallback)
        {
            string correlationId = !string.IsNullOrWhiteSpace(_currentCorrelationId)
                ? _currentCorrelationId
                : fallback;
            correlationId = NormalizeCorrelationId(correlationId);
            return string.IsNullOrWhiteSpace(correlationId) ? null : correlationId;
        }

        public static IDisposable BeginCorrelationScope(string correlationId = null)
        {
            string previous = _currentCorrelationId;
            _currentCorrelationId = NormalizeCorrelationId(correlationId) ?? Guid.NewGuid().ToString("N");
            return new CorrelationScope(previous);
        }

        private static string NormalizeActor(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            if (value.Length <= 80) return value;
            return value.Substring(0, 80);
        }

        private static string NormalizeCorrelationId(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value.Trim();
            return value.Length <= 100 ? value : value.Substring(0, 100);
        }

        private sealed class CorrelationScope : IDisposable
        {
            private readonly string _previous;
            private bool _disposed;

            public CorrelationScope(string previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed) return;
                _currentCorrelationId = _previous;
                _disposed = true;
            }
        }
    }
}
