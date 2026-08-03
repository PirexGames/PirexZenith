using System;

namespace PirexZenith.Core
{
    /// <summary>
    /// Specialized exception thrown by PirexZenith (dependency cycles, missing bindings, etc.).
    /// </summary>
    public class DIException : Exception
    {
        public DIException(string message) : base(message)
        {
        }

        public DIException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
