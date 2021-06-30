using System;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Nakijin.Api.Infrastructure.Logging
{
    public static class LoggerExtensions
    {
        static LoggerExtensions()
        {
            MappingAccommodationsStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90000, "MappingAccommodationsStart"),
                $"INFORMATION | AccommodationMapper: {{message}}");
            
            MappingAccommodationsOfSpecifiedCountryStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90001, "MappingAccommodationsOfSpecifiedCountryStart"),
                $"INFORMATION | AccommodationMapper: {{message}}");
            
            MappingAccommodationsFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90002, "MappingAccommodationsFinish"),
                $"INFORMATION | AccommodationMapper: {{message}}");
            
            MappingAccommodationsOfSpecifiedCountryFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90003, "MappingAccommodationsOfSpecifiedCountryFinish"),
                $"INFORMATION | AccommodationMapper: {{message}}");
            
            MappingAccommodationsCancelOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90004, "MappingAccommodationsCancel"),
                $"INFORMATION | AccommodationMapper: {{message}}");
            
            MappingAccommodationsErrorOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(90005, "MappingAccommodationsError"),
                $"ERROR | AccommodationMapper: ");
            
            MappingLocationsStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90100, "MappingLocationsStart"),
                $"INFORMATION | LocationMapper: {{message}}");
            
            MappingLocationsFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90101, "MappingLocationsFinish"),
                $"INFORMATION | LocationMapper: {{message}}");
            
            MappingLocationsCancelOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90102, "MappingLocationsCancel"),
                $"INFORMATION | LocationMapper: {{message}}");
            
            MappingLocationsErrorOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(90103, "MappingLocationsError"),
                $"ERROR | LocationMapper: ");
            
            MappingCountriesStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90104, "MappingCountriesStart"),
                $"INFORMATION | CountriesMapper: {{message}}");
            
            MappingCountriesFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90105, "MappingCountriesFinish"),
                $"INFORMATION | CountriesMapper: {{message}}");
            
            MappingLocalitiesStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90106, "MappingLocalitiesStart"),
                $"INFORMATION | LocalitiesMapper: {{message}}");
            
            MappingLocalitiesFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90107, "MappingLocalitiesFinish"),
                $"INFORMATION | LocalitiesMapper: {{message}}");
            
            MappingLocalitiesOfSpecifiedCountryStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90108, "MappingLocalitiesOfSpecifiedCountryStart"),
                $"INFORMATION | LocalitiesMapper: {{message}}");
            
            MappingLocalitiesOfSpecifiedCountryFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90109, "MappingLocalitiesOfSpecifiedCountryFinish"),
                $"INFORMATION | LocalitiesMapper: {{message}}");
            
            MappingLocalityZonesStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90110, "MappingLocalityZonesStart"),
                $"INFORMATION | LocalityZonesMapper: {{message}}");
            
            MappingLocalityZonesFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90110, "MappingLocalityZonesFinish"),
                $"INFORMATION | LocalityZonesMapper: {{message}}");
            
            MappingLocalityZonesOfSpecifiedCountryStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90111, "MappingLocalityZonesOfSpecifiedCountryStart"),
                $"INFORMATION | LocalityZonesMapper: {{message}}");
            
            MappingLocalityZonesOfSpecifiedCountryFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90112, "MappingLocalityZonesOfSpecifiedCountryFinish"),
                $"INFORMATION | LocalityZonesMapper: {{message}}");
            
            MappingInvalidLocalityOccured = LoggerMessage.Define<string>(LogLevel.Warning,
                new EventId(90113, "MappingInvalidLocality"),
                $"WARNING | LocalityMapper: {{message}}");
            
            MergingAccommodationsDataStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90200, "MergingAccommodationsDataStart"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            MergingAccommodationsDataFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90201, "MergingAccommodationsDataFinish"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            MergingAccommodationsDataCancelOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90202, "MergingAccommodationsDataCancel"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            MergingAccommodationsDataErrorOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(90203, "MergingAccommodationsDataError"),
                $"ERROR | AccommodationDataMerger: ");
            
            CalculatingAccommodationsDataStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90204, "CalculatingAccommodationsDataStart"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            CalculatingAccommodationsDataFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90205, "CalculatingAccommodationsDataFinish"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            CalculatingAccommodationsDataCancelOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90206, "CalculatingAccommodationsDataCancel"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            CalculatingAccommodationsDataErrorOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(90207, "CalculatingAccommodationsDataError"),
                $"ERROR | AccommodationDataMerger: ");
            
            CalculatingAccommodationsBatchOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90208, "CalculatingAccommodationsBatch"),
                $"INFORMATION | AccommodationDataMerger: {{message}}");
            
            PreloadingAccommodationsStartOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90300, "PreloadingAccommodationsStart"),
                $"INFORMATION | AccommodationPreloader: {{message}}");
            
            PreloadingAccommodationsFinishOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90301, "PreloadingAccommodationsFinish"),
                $"INFORMATION | AccommodationPreloader: {{message}}");
            
            PreloadingAccommodationsCancelOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90302, "PreloadingAccommodationsCancel"),
                $"INFORMATION | AccommodationPreloader: {{message}}");
            
            PreloadingAccommodationsErrorOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(90303, "PreloadingAccommodationsError"),
                $"ERROR | AccommodationPreloader: ");
            
            ConnectorClientErrorOccured = LoggerMessage.Define(LogLevel.Error,
                new EventId(90400, "ConnectorClientError"),
                $"ERROR | ConnectorClient: ");
            
            SameAccommodationInOneSupplierErrorOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(90500, "SameAccommodationInOneSupplierError"),
                $"ERROR | AccommodationMapper: {{message}}");
            
            NotValidCoordinatesInAccommodationOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(90501, "NotValidCoordinatesInAccommodation"),
                $"ERROR | AccommodationMapper: {{message}}");
            
            NotValidDefaultNameOfAccommodationOccured = LoggerMessage.Define<string>(LogLevel.Error,
                new EventId(90502, "NotValidDefaultNameOfAccommodation"),
                $"ERROR | AccommodationMapper: {{message}}");
            
            LocationsPublishedOccured = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90502, "LocationsPublished"),
                $"INFORMATION | PredictionsUpdateService: {{message}}");
            
        }
    
                
         public static void LogMappingAccommodationsStart(this ILogger logger, string message)
            => MappingAccommodationsStartOccured(logger, message, null);
                
         public static void LogMappingAccommodationsOfSpecifiedCountryStart(this ILogger logger, string message)
            => MappingAccommodationsOfSpecifiedCountryStartOccured(logger, message, null);
                
         public static void LogMappingAccommodationsFinish(this ILogger logger, string message)
            => MappingAccommodationsFinishOccured(logger, message, null);
                
         public static void LogMappingAccommodationsOfSpecifiedCountryFinish(this ILogger logger, string message)
            => MappingAccommodationsOfSpecifiedCountryFinishOccured(logger, message, null);
                
         public static void LogMappingAccommodationsCancel(this ILogger logger, string message)
            => MappingAccommodationsCancelOccured(logger, message, null);
                
         public static void LogMappingAccommodationsError(this ILogger logger, Exception exception)
            => MappingAccommodationsErrorOccured(logger, exception);
                
         public static void LogMappingLocationsStart(this ILogger logger, string message)
            => MappingLocationsStartOccured(logger, message, null);
                
         public static void LogMappingLocationsFinish(this ILogger logger, string message)
            => MappingLocationsFinishOccured(logger, message, null);
                
         public static void LogMappingLocationsCancel(this ILogger logger, string message)
            => MappingLocationsCancelOccured(logger, message, null);
                
         public static void LogMappingLocationsError(this ILogger logger, Exception exception)
            => MappingLocationsErrorOccured(logger, exception);
                
         public static void LogMappingCountriesStart(this ILogger logger, string message)
            => MappingCountriesStartOccured(logger, message, null);
                
         public static void LogMappingCountriesFinish(this ILogger logger, string message)
            => MappingCountriesFinishOccured(logger, message, null);
                
         public static void LogMappingLocalitiesStart(this ILogger logger, string message)
            => MappingLocalitiesStartOccured(logger, message, null);
                
         public static void LogMappingLocalitiesFinish(this ILogger logger, string message)
            => MappingLocalitiesFinishOccured(logger, message, null);
                
         public static void LogMappingLocalitiesOfSpecifiedCountryStart(this ILogger logger, string message)
            => MappingLocalitiesOfSpecifiedCountryStartOccured(logger, message, null);
                
         public static void LogMappingLocalitiesOfSpecifiedCountryFinish(this ILogger logger, string message)
            => MappingLocalitiesOfSpecifiedCountryFinishOccured(logger, message, null);
                
         public static void LogMappingLocalityZonesStart(this ILogger logger, string message)
            => MappingLocalityZonesStartOccured(logger, message, null);
                
         public static void LogMappingLocalityZonesFinish(this ILogger logger, string message)
            => MappingLocalityZonesFinishOccured(logger, message, null);
                
         public static void LogMappingLocalityZonesOfSpecifiedCountryStart(this ILogger logger, string message)
            => MappingLocalityZonesOfSpecifiedCountryStartOccured(logger, message, null);
                
         public static void LogMappingLocalityZonesOfSpecifiedCountryFinish(this ILogger logger, string message)
            => MappingLocalityZonesOfSpecifiedCountryFinishOccured(logger, message, null);
             
         public static void LogMappingInvalidLocality(this ILogger logger, string message)
             => MappingInvalidLocalityOccured(logger, message, null);
         
         public static void LogMergingAccommodationsDataStart(this ILogger logger, string message)
            => MergingAccommodationsDataStartOccured(logger, message, null);
                
         public static void LogMergingAccommodationsDataFinish(this ILogger logger, string message)
            => MergingAccommodationsDataFinishOccured(logger, message, null);
                
         public static void LogMergingAccommodationsDataCancel(this ILogger logger, string message)
            => MergingAccommodationsDataCancelOccured(logger, message, null);
                
         public static void LogMergingAccommodationsDataError(this ILogger logger, Exception exception)
            => MergingAccommodationsDataErrorOccured(logger, exception);
                
         public static void LogCalculatingAccommodationsDataStart(this ILogger logger, string message)
            => CalculatingAccommodationsDataStartOccured(logger, message, null);
                
         public static void LogCalculatingAccommodationsDataFinish(this ILogger logger, string message)
            => CalculatingAccommodationsDataFinishOccured(logger, message, null);
                
         public static void LogCalculatingAccommodationsDataCancel(this ILogger logger, string message)
            => CalculatingAccommodationsDataCancelOccured(logger, message, null);
                
         public static void LogCalculatingAccommodationsDataError(this ILogger logger, Exception exception)
            => CalculatingAccommodationsDataErrorOccured(logger, exception);

         public static void LogCalculatingAccommodationsBatch(this ILogger logger, string message)
             => CalculatingAccommodationsBatchOccured(logger, message, null);
             
         public static void LogPreloadingAccommodationsStart(this ILogger logger, string message)
            => PreloadingAccommodationsStartOccured(logger, message, null);
                
         public static void LogPreloadingAccommodationsFinish(this ILogger logger, string message)
            => PreloadingAccommodationsFinishOccured(logger, message, null);
                
         public static void LogPreloadingAccommodationsCancel(this ILogger logger, string message)
            => PreloadingAccommodationsCancelOccured(logger, message, null);
                
         public static void LogPreloadingAccommodationsError(this ILogger logger, Exception exception)
            => PreloadingAccommodationsErrorOccured(logger, exception);
                
         public static void LogConnectorClientError(this ILogger logger, Exception exception)
            => ConnectorClientErrorOccured(logger, exception);
                
         public static void LogSameAccommodationInOneSupplierError(this ILogger logger, string message)
            => SameAccommodationInOneSupplierErrorOccured(logger, message, null);
                
         public static void LogNotValidCoordinatesInAccommodation(this ILogger logger, string message)
            => NotValidCoordinatesInAccommodationOccured(logger, message, null);
                
         public static void LogNotValidDefaultNameOfAccommodation(this ILogger logger, string message)
            => NotValidDefaultNameOfAccommodationOccured(logger, message, null);
                
         public static void LogLocationsPublished(this ILogger logger, string message)
            => LocationsPublishedOccured(logger, message, null);
    
    
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsOfSpecifiedCountryStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsOfSpecifiedCountryFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsCancelOccured;
        
        private static readonly Action<ILogger, Exception> MappingAccommodationsErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocationsStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocationsFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocationsCancelOccured;
        
        private static readonly Action<ILogger, Exception> MappingLocationsErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingCountriesStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingCountriesFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalitiesStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalitiesFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalitiesOfSpecifiedCountryStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalitiesOfSpecifiedCountryFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalityZonesStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalityZonesFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalityZonesOfSpecifiedCountryStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalityZonesOfSpecifiedCountryFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MappingInvalidLocalityOccured;
        
        private static readonly Action<ILogger, string, Exception> MergingAccommodationsDataStartOccured;
        
        private static readonly Action<ILogger, string, Exception> MergingAccommodationsDataFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> MergingAccommodationsDataCancelOccured;
        
        private static readonly Action<ILogger, Exception> MergingAccommodationsDataErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsDataStartOccured;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsDataFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsDataCancelOccured;
        
        private static readonly Action<ILogger, Exception> CalculatingAccommodationsDataErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsBatchOccured;
        
        private static readonly Action<ILogger, string, Exception> PreloadingAccommodationsStartOccured;
        
        private static readonly Action<ILogger, string, Exception> PreloadingAccommodationsFinishOccured;
        
        private static readonly Action<ILogger, string, Exception> PreloadingAccommodationsCancelOccured;
        
        private static readonly Action<ILogger, Exception> PreloadingAccommodationsErrorOccured;
        
        private static readonly Action<ILogger, Exception> ConnectorClientErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> SameAccommodationInOneSupplierErrorOccured;
        
        private static readonly Action<ILogger, string, Exception> NotValidCoordinatesInAccommodationOccured;
        
        private static readonly Action<ILogger, string, Exception> NotValidDefaultNameOfAccommodationOccured;
        
        private static readonly Action<ILogger, string, Exception> LocationsPublishedOccured;
    }
}