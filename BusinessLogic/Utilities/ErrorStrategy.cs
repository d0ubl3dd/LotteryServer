using System;

namespace BusinessLogic.Utilities
{
    public class ErrorStrategy
    {
        public string ErrorCode { get; set; }
        public string ClientMessage { get; set; }
        public bool UseExceptionMessage { get; set; }
        public Action<string> LogAction { get; set; }
    }
}
