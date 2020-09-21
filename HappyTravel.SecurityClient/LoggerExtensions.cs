using System;
using Microsoft.Extensions.Logging;

namespace HappyTravel.SecurityClient
{
    public static class LoggerExtensions
    {
        static LoggerExtensions()
        {
            DataProviderClientExceptionOccured = LoggerMessage.Define(LogLevel.Critical,
                new EventId(1300, "DataProviderClientException"),
                "CRITICAL | DataProviderClient: ");
            
            GetTokenForConnectorErrorOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(2910, "GetTokenForConnectorError"),
                "ERROR | ConnectorClient: {{message}}");
            
            UnauthorizedConnectorResponseOccured = LoggerMessage.Define<string>(LogLevel.Debug,
                new EventId(2911, "UnauthorizedConnectorResponse"),
                "DEBUG | ConnectorClient: {{message}}");
        }


        internal static void LogDataProviderClientException(this ILogger logger, Exception exception)
            => DataProviderClientExceptionOccured(logger, exception);
                
        internal static void LogGetTokenForConnectorError(this ILogger logger, string message)
            => GetTokenForConnectorErrorOccured(logger, message, null);


        internal static void LogUnauthorizedConnectorResponse(this ILogger logger, string message)
            => UnauthorizedConnectorResponseOccured(logger, message, null);

        
        private static readonly Action<ILogger, Exception> DataProviderClientExceptionOccured;

        private static readonly Action<ILogger, string, Exception> GetTokenForConnectorErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> UnauthorizedConnectorResponseOccured;
    }
}
