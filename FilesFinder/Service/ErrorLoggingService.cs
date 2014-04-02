using System.Collections.Concurrent;

namespace FilesFinder.Service {
    public class ErrorLoggingService : IErrorLoggingService {
        private ConcurrentStack<string> _errors = new ConcurrentStack<string>(); 

        public void LogError(string message) {
            _errors.Push(message);
        }

        public string GetNextError() {
            string error;
            return _errors.TryPop(out error) ? error : null;
        }
    }
}