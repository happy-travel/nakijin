using HappyTravel.MultiLanguage;

namespace HappyTravel.Nakijin.Api.Infrastructure
{
    public static class MultiLanguageHelpers
    {
        public static MultiLanguage<T> Merge<T>(MultiLanguage<T> first, MultiLanguage<T> second)
        {
            var allFromFirst = first.GetAll();
            var allFromSecond = second.GetAll();

            var result = new MultiLanguage<T>();
            foreach (var item in allFromSecond)
                result.TrySetValue(item.languageCode, item.value);

            foreach (var item in allFromFirst)
                if (!allFromSecond.Contains(item))
                    result.TrySetValue(item.languageCode, item.value);

            return result;
        }
    }
}