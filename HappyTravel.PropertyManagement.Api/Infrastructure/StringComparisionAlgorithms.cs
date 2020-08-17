using System.Linq;

namespace HappyTravel.PropertyManagement.Api.Infrastructure
{
    public static class StringComparisionAlgorithms
    {

        public static float GetEqualityIndex(string first, string second) => GetJaccardIndex(first, second);
        
        private static float GetJaccardIndex(this string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) || string.IsNullOrWhiteSpace(second))
                return 0;
            
            var firstSequence = first.ToSequence();
            var secondSequence = second.ToSequence();

            //maybe comparision will work in another way
            var intersectedSequence = firstSequence.Intersect(secondSequence).ToArray();
            var unitedSequence = firstSequence.Union(secondSequence).ToArray();
            return (float)intersectedSequence.Length / unitedSequence.Length;
        }

        private static string[] ToSequence(this string value) => value.Split(" ");
    }
}