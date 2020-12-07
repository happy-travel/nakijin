using System.Collections.Generic;
using System.Text.Json;
using Newtonsoft.Json;

namespace HappyTravel.StaticDataMapper.Api.Infrastructure
{
    public static class LanguageHelper
    {
        public static string GetValue(string? source, string languageCode)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;

            var jsonDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(source);

            return !jsonDictionary.TryGetValue(languageCode, out var languageValue)
                ? string.Empty
                : languageValue;
        }

        public static string GetValue(JsonDocument source, string languageCode)
        {
            var strSource = source.RootElement.ToString();
            return GetValue(strSource, languageCode);
        }
        

        public static string MergeLanguages(string? firstJsonWithLanguages, string? secondJsonWithLanguages)
        {
            if (string.IsNullOrEmpty(firstJsonWithLanguages))
                return secondJsonWithLanguages;

            if (string.IsNullOrEmpty(secondJsonWithLanguages))
                return firstJsonWithLanguages;

            var firstJsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(firstJsonWithLanguages);
            var secondJsonObject = JsonConvert.DeserializeObject<Dictionary<string, string>>(secondJsonWithLanguages);

            Dictionary<string, string> largerJsonObject;
            Dictionary<string, string> smallerJsonObject;

            if (firstJsonObject.Count > secondJsonObject.Count)
            {
                largerJsonObject = firstJsonObject;
                smallerJsonObject = secondJsonObject;
            }
            else
            {
                largerJsonObject = secondJsonObject;
                smallerJsonObject = firstJsonObject;
            }

            foreach (var languageCandidate in smallerJsonObject)
            {
                if (!largerJsonObject.ContainsKey(languageCandidate.Key))
                {
                    largerJsonObject.Add(languageCandidate.Key, languageCandidate.Value);
                }
            }


            return JsonConvert.SerializeObject(largerJsonObject);
        }
    }
}