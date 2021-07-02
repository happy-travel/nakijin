using System;
using Microsoft.Extensions.Logging;

namespace HappyTravel.Nakijin.Api.Infrastructure.Logging
{
    public static class LoggerExtensions
    {
        static LoggerExtensions()
        {
            MappingAccommodationsStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90000, "MappingAccommodationsStart"),
                "Started mapping of '{supplier}' accommodations");
            
            MappingAccommodationsOfSpecifiedCountryStart = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(90001, "MappingAccommodationsOfSpecifiedCountryStart"),
                "Started mapping of '{supplier}' accommodations of country with code '{countryCode}'");
            
            MappingAccommodationsFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90002, "MappingAccommodationsFinish"),
                "Finished mapping of '{supplier}' accommodations");
            
            MappingAccommodationsOfSpecifiedCountryFinish = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(90003, "MappingAccommodationsOfSpecifiedCountryFinish"),
                "Finished mapping of '{supplier}' accommodations of country with code '{countryCode}'");
            
            MappingAccommodationsCancel = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90004, "MappingAccommodationsCancel"),
                "Mapping accommodations of '{supplier}' was canceled by client request.");
            
            MappingAccommodationsError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90005, "MappingAccommodationsError"),
                "");
            
            MappingLocationsStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90100, "MappingLocationsStart"),
                "Started Mapping locations of '{supplier}'.");
            
            MappingLocationsFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90101, "MappingLocationsFinish"),
                "Finished Mapping locations of '{supplier}'");
            
            MappingLocationsCancel = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90102, "MappingLocationsCancel"),
                "Mapping locations of '{supplier}' was canceled by client request.");
            
            MappingLocationsError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90103, "MappingLocationsError"),
                "");
            
            MappingCountriesStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90104, "MappingCountriesStart"),
                "Started Mapping countries of '{supplier}'.");
            
            MappingCountriesFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90105, "MappingCountriesFinish"),
                "Finished Mapping countries of '{supplier}'.");
            
            MappingLocalitiesStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90106, "MappingLocalitiesStart"),
                "Started Mapping localities of '{supplier}'.");
            
            MappingLocalitiesFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90107, "MappingLocalitiesFinish"),
                "Finished Mapping localities of '{supplier}'");
            
            MappingLocalitiesOfSpecifiedCountryStart = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(90108, "MappingLocalitiesOfSpecifiedCountryStart"),
                "Started Mapping localities of '{supplier}' of country with code '{countryCode}'.");
            
            MappingLocalitiesOfSpecifiedCountryFinish = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(90109, "MappingLocalitiesOfSpecifiedCountryFinish"),
                "Finished Mapping localities of '{supplier}' of country {countryCode}");
            
            MappingLocalityZonesStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90110, "MappingLocalityZonesStart"),
                "Started Mapping locality zones of '{supplier}'.");
            
            MappingLocalityZonesFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90110, "MappingLocalityZonesFinish"),
                "Finished Mapping locality zones of '{supplier}'.");
            
            MappingLocalityZonesOfSpecifiedCountryStart = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(90111, "MappingLocalityZonesOfSpecifiedCountryStart"),
                "Started Mapping locality zones of '{supplier}' of country with code '{countryCode}'.");
            
            MappingLocalityZonesOfSpecifiedCountryFinish = LoggerMessage.Define<string, string>(LogLevel.Information,
                new EventId(90112, "MappingLocalityZonesOfSpecifiedCountryFinish"),
                "Finished Mapping locality zones of '{supplier}' of country with code {countryCode}.");
            
            MappingInvalidLocality = LoggerMessage.Define<string, string, string, string, string>(LogLevel.Warning,
                new EventId(90113, "MappingInvalidLocality"),
                "Locality '{defaultLocalityName}' of the country '{defaultCountryName}' is invalid and has been skipped. Supplier '{supplier}', Country '{serializedCountry}', Locality '{serializedLocality}'");
            
            MergingAccommodationsDataStart = LoggerMessage.Define(LogLevel.Information,
                new EventId(90200, "MergingAccommodationsDataStart"),
                "Started merging accommodations data");
            
            MergingAccommodationsDataFinish = LoggerMessage.Define(LogLevel.Information,
                new EventId(90201, "MergingAccommodationsDataFinish"),
                "Finished merging accommodations data");
            
            MergingAccommodationsDataCancel = LoggerMessage.Define(LogLevel.Information,
                new EventId(90202, "MergingAccommodationsDataCancel"),
                "Merging accommodations was canceled by client request.");
            
            MergingAccommodationsDataError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90203, "MergingAccommodationsDataError"),
                "");
            
            CalculatingAccommodationsDataStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90204, "CalculatingAccommodationsDataStart"),
                "'Started calculation accommodations data of supplier '{supplier}'");
            
            CalculatingAccommodationsDataFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90205, "CalculatingAccommodationsDataFinish"),
                "Finished calculation of supplier '{supplier}' data.");
            
            CalculatingAccommodationsDataCancel = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90206, "CalculatingAccommodationsDataCancel"),
                "Calculating data of supplier '{supplier}' was cancelled by client request");
            
            CalculatingAccommodationsDataError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90207, "CalculatingAccommodationsDataError"),
                "");
            
            CalculatingAccommodationsBatch = LoggerMessage.Define<int, string>(LogLevel.Information,
                new EventId(90208, "CalculatingAccommodationsBatch"),
                "{skip} {supplier} accommodations have been calculated");
            
            PreloadingAccommodationsStart = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90300, "PreloadingAccommodationsStart"),
                "Started Preloading accommodations of '{supplier}'.");
            
            PreloadingAccommodationsFinish = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90301, "PreloadingAccommodationsFinish"),
                "Finished Preloading accommodations of '{supplier}'.");
            
            PreloadingAccommodationsCancel = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90302, "PreloadingAccommodationsCancel"),
                "Preloading accommodations of '{supplier}' was canceled by client request.");
            
            PreloadingAccommodationsError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90303, "PreloadingAccommodationsError"),
                "");
            
            ConnectorClientError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90400, "ConnectorClientError"),
                "");
            
            SameAccommodationInOneSupplierError = LoggerMessage.Define<string, string, string>(LogLevel.Error,
                new EventId(90500, "SameAccommodationInOneSupplierError"),
                "'{supplier}' have the same accommodations with codes '{firstAccommodationSupplierCode}' and '{secondAccommodationSupplierCode}'");
            
            NotValidCoordinatesInAccommodation = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(90501, "NotValidCoordinatesInAccommodation"),
                "'{supplier}' have the accommodation with not valid coordinates, which code is '{accommodationSupplierCode}'");
            
            NotValidDefaultNameOfAccommodation = LoggerMessage.Define<string, string>(LogLevel.Error,
                new EventId(90502, "NotValidDefaultNameOfAccommodation"),
                "'{supplier}' have the accommodation with not valid default name, which code is '{accommodationSupplierCode}'");
            
            SingleLocationPublished = LoggerMessage.Define<string>(LogLevel.Information,
                new EventId(90600, "SingleLocationPublished"),
                "Location with htId '{htId}' has been published");
            
            LocationsPublished = LoggerMessage.Define<int>(LogLevel.Information,
                new EventId(90601, "LocationsPublished"),
                "Locations with count '{count}' has been published");
            
            AccommodationsDuplicatesRemoveError = LoggerMessage.Define(LogLevel.Error,
                new EventId(90700, "AccommodationsDuplicatesRemoveError"),
                "Remove of accommodations duplicates failed.");
            
        }
    
                
         public static void LogMappingAccommodationsStart(this ILogger logger, string supplier, Exception exception = null)
            => MappingAccommodationsStart(logger, supplier, exception);
                
         public static void LogMappingAccommodationsOfSpecifiedCountryStart(this ILogger logger, string supplier, string countryCode, Exception exception = null)
            => MappingAccommodationsOfSpecifiedCountryStart(logger, supplier, countryCode, exception);
                
         public static void LogMappingAccommodationsFinish(this ILogger logger, string supplier, Exception exception = null)
            => MappingAccommodationsFinish(logger, supplier, exception);
                
         public static void LogMappingAccommodationsOfSpecifiedCountryFinish(this ILogger logger, string supplier, string countryCode, Exception exception = null)
            => MappingAccommodationsOfSpecifiedCountryFinish(logger, supplier, countryCode, exception);
                
         public static void LogMappingAccommodationsCancel(this ILogger logger, string supplier, Exception exception = null)
            => MappingAccommodationsCancel(logger, supplier, exception);
                
         public static void LogMappingAccommodationsError(this ILogger logger, Exception exception = null)
            => MappingAccommodationsError(logger, exception);
                
         public static void LogMappingLocationsStart(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocationsStart(logger, supplier, exception);
                
         public static void LogMappingLocationsFinish(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocationsFinish(logger, supplier, exception);
                
         public static void LogMappingLocationsCancel(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocationsCancel(logger, supplier, exception);
                
         public static void LogMappingLocationsError(this ILogger logger, Exception exception = null)
            => MappingLocationsError(logger, exception);
                
         public static void LogMappingCountriesStart(this ILogger logger, string supplier, Exception exception = null)
            => MappingCountriesStart(logger, supplier, exception);
                
         public static void LogMappingCountriesFinish(this ILogger logger, string supplier, Exception exception = null)
            => MappingCountriesFinish(logger, supplier, exception);
                
         public static void LogMappingLocalitiesStart(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocalitiesStart(logger, supplier, exception);
                
         public static void LogMappingLocalitiesFinish(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocalitiesFinish(logger, supplier, exception);
                
         public static void LogMappingLocalitiesOfSpecifiedCountryStart(this ILogger logger, string supplier, string countryCode, Exception exception = null)
            => MappingLocalitiesOfSpecifiedCountryStart(logger, supplier, countryCode, exception);
                
         public static void LogMappingLocalitiesOfSpecifiedCountryFinish(this ILogger logger, string supplier, string countryCode, Exception exception = null)
            => MappingLocalitiesOfSpecifiedCountryFinish(logger, supplier, countryCode, exception);
                
         public static void LogMappingLocalityZonesStart(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocalityZonesStart(logger, supplier, exception);
                
         public static void LogMappingLocalityZonesFinish(this ILogger logger, string supplier, Exception exception = null)
            => MappingLocalityZonesFinish(logger, supplier, exception);
                
         public static void LogMappingLocalityZonesOfSpecifiedCountryStart(this ILogger logger, string supplier, string countryCode, Exception exception = null)
            => MappingLocalityZonesOfSpecifiedCountryStart(logger, supplier, countryCode, exception);
                
         public static void LogMappingLocalityZonesOfSpecifiedCountryFinish(this ILogger logger, string supplier, string countryCode, Exception exception = null)
            => MappingLocalityZonesOfSpecifiedCountryFinish(logger, supplier, countryCode, exception);
                
         public static void LogMappingInvalidLocality(this ILogger logger, string defaultLocalityName, string defaultCountryName, string supplier, string serializedCountry, string serializedLocality, Exception exception = null)
            => MappingInvalidLocality(logger, defaultLocalityName, defaultCountryName, supplier, serializedCountry, serializedLocality, exception);
                
         public static void LogMergingAccommodationsDataStart(this ILogger logger, Exception exception = null)
            => MergingAccommodationsDataStart(logger, exception);
                
         public static void LogMergingAccommodationsDataFinish(this ILogger logger, Exception exception = null)
            => MergingAccommodationsDataFinish(logger, exception);
                
         public static void LogMergingAccommodationsDataCancel(this ILogger logger, Exception exception = null)
            => MergingAccommodationsDataCancel(logger, exception);
                
         public static void LogMergingAccommodationsDataError(this ILogger logger, Exception exception = null)
            => MergingAccommodationsDataError(logger, exception);
                
         public static void LogCalculatingAccommodationsDataStart(this ILogger logger, string supplier, Exception exception = null)
            => CalculatingAccommodationsDataStart(logger, supplier, exception);
                
         public static void LogCalculatingAccommodationsDataFinish(this ILogger logger, string supplier, Exception exception = null)
            => CalculatingAccommodationsDataFinish(logger, supplier, exception);
                
         public static void LogCalculatingAccommodationsDataCancel(this ILogger logger, string supplier, Exception exception = null)
            => CalculatingAccommodationsDataCancel(logger, supplier, exception);
                
         public static void LogCalculatingAccommodationsDataError(this ILogger logger, Exception exception = null)
            => CalculatingAccommodationsDataError(logger, exception);
                
         public static void LogCalculatingAccommodationsBatch(this ILogger logger, int skip, string supplier, Exception exception = null)
            => CalculatingAccommodationsBatch(logger, skip, supplier, exception);
                
         public static void LogPreloadingAccommodationsStart(this ILogger logger, string supplier, Exception exception = null)
            => PreloadingAccommodationsStart(logger, supplier, exception);
                
         public static void LogPreloadingAccommodationsFinish(this ILogger logger, string supplier, Exception exception = null)
            => PreloadingAccommodationsFinish(logger, supplier, exception);
                
         public static void LogPreloadingAccommodationsCancel(this ILogger logger, string supplier, Exception exception = null)
            => PreloadingAccommodationsCancel(logger, supplier, exception);
                
         public static void LogPreloadingAccommodationsError(this ILogger logger, Exception exception = null)
            => PreloadingAccommodationsError(logger, exception);
                
         public static void LogConnectorClientError(this ILogger logger, Exception exception = null)
            => ConnectorClientError(logger, exception);
                
         public static void LogSameAccommodationInOneSupplierError(this ILogger logger, string supplier, string firstAccommodationSupplierCode, string secondAccommodationSupplierCode, Exception exception = null)
            => SameAccommodationInOneSupplierError(logger, supplier, firstAccommodationSupplierCode, secondAccommodationSupplierCode, exception);
                
         public static void LogNotValidCoordinatesInAccommodation(this ILogger logger, string supplier, string accommodationSupplierCode, Exception exception = null)
            => NotValidCoordinatesInAccommodation(logger, supplier, accommodationSupplierCode, exception);
                
         public static void LogNotValidDefaultNameOfAccommodation(this ILogger logger, string supplier, string accommodationSupplierCode, Exception exception = null)
            => NotValidDefaultNameOfAccommodation(logger, supplier, accommodationSupplierCode, exception);
                
         public static void LogSingleLocationPublished(this ILogger logger, string htId, Exception exception = null)
            => SingleLocationPublished(logger, htId, exception);
                
         public static void LogLocationsPublished(this ILogger logger, int count, Exception exception = null)
            => LocationsPublished(logger, count, exception);
                
         public static void LogAccommodationsDuplicatesRemoveError(this ILogger logger, Exception exception = null)
            => AccommodationsDuplicatesRemoveError(logger, exception);
    
    
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsStart;
        
        private static readonly Action<ILogger, string, string, Exception> MappingAccommodationsOfSpecifiedCountryStart;
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsFinish;
        
        private static readonly Action<ILogger, string, string, Exception> MappingAccommodationsOfSpecifiedCountryFinish;
        
        private static readonly Action<ILogger, string, Exception> MappingAccommodationsCancel;
        
        private static readonly Action<ILogger, Exception> MappingAccommodationsError;
        
        private static readonly Action<ILogger, string, Exception> MappingLocationsStart;
        
        private static readonly Action<ILogger, string, Exception> MappingLocationsFinish;
        
        private static readonly Action<ILogger, string, Exception> MappingLocationsCancel;
        
        private static readonly Action<ILogger, Exception> MappingLocationsError;
        
        private static readonly Action<ILogger, string, Exception> MappingCountriesStart;
        
        private static readonly Action<ILogger, string, Exception> MappingCountriesFinish;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalitiesStart;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalitiesFinish;
        
        private static readonly Action<ILogger, string, string, Exception> MappingLocalitiesOfSpecifiedCountryStart;
        
        private static readonly Action<ILogger, string, string, Exception> MappingLocalitiesOfSpecifiedCountryFinish;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalityZonesStart;
        
        private static readonly Action<ILogger, string, Exception> MappingLocalityZonesFinish;
        
        private static readonly Action<ILogger, string, string, Exception> MappingLocalityZonesOfSpecifiedCountryStart;
        
        private static readonly Action<ILogger, string, string, Exception> MappingLocalityZonesOfSpecifiedCountryFinish;
        
        private static readonly Action<ILogger, string, string, string, string, string, Exception> MappingInvalidLocality;
        
        private static readonly Action<ILogger, Exception> MergingAccommodationsDataStart;
        
        private static readonly Action<ILogger, Exception> MergingAccommodationsDataFinish;
        
        private static readonly Action<ILogger, Exception> MergingAccommodationsDataCancel;
        
        private static readonly Action<ILogger, Exception> MergingAccommodationsDataError;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsDataStart;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsDataFinish;
        
        private static readonly Action<ILogger, string, Exception> CalculatingAccommodationsDataCancel;
        
        private static readonly Action<ILogger, Exception> CalculatingAccommodationsDataError;
        
        private static readonly Action<ILogger, int, string, Exception> CalculatingAccommodationsBatch;
        
        private static readonly Action<ILogger, string, Exception> PreloadingAccommodationsStart;
        
        private static readonly Action<ILogger, string, Exception> PreloadingAccommodationsFinish;
        
        private static readonly Action<ILogger, string, Exception> PreloadingAccommodationsCancel;
        
        private static readonly Action<ILogger, Exception> PreloadingAccommodationsError;
        
        private static readonly Action<ILogger, Exception> ConnectorClientError;
        
        private static readonly Action<ILogger, string, string, string, Exception> SameAccommodationInOneSupplierError;
        
        private static readonly Action<ILogger, string, string, Exception> NotValidCoordinatesInAccommodation;
        
        private static readonly Action<ILogger, string, string, Exception> NotValidDefaultNameOfAccommodation;
        
        private static readonly Action<ILogger, string, Exception> SingleLocationPublished;
        
        private static readonly Action<ILogger, int, Exception> LocationsPublished;
        
        private static readonly Action<ILogger, Exception> AccommodationsDuplicatesRemoveError;
    }
}