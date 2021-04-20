using HappyTravel.MultiLanguage;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class MultiLanguageHelpers
    {
        public static MultiLanguage<string> MergeMultilingualStrings(MultiLanguage<string> first,
            MultiLanguage<string> second)
        {
            var allFromFirst = first.GetAll();
            var allFromSecond = second.GetAll();

            var result = new MultiLanguage<string>();
            foreach (var item in allFromSecond)
                if (!string.IsNullOrEmpty(item.value))
                    result.TrySetValue(item.languageCode, item.value);

            foreach (var item in allFromFirst)
                if (!allFromSecond.Contains(item))
                    result.TrySetValue(item.languageCode, item.value);

            return result;
        }
    }
}