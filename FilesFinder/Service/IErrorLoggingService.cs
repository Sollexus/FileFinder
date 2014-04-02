using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FilesFinder.Service
{
    public interface IErrorLoggingService {
        void LogError(string message);
        string GetNextError();
    }
}
