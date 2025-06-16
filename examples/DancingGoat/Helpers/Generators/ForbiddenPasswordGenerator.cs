using System.Collections.Generic;
using System.Linq;

namespace DancingGoat.Helpers.Generators
{
    /// <summary>
    /// Contains methods for generating forbidden passwords.
    /// </summary>
    public static class ForbiddenPasswordGenerator
    {
        private static readonly List<char> SpecialChars = ['!', '@', '#', '$', '%', '^', '&', '*', '(', ')', '.', '?', '-', '_', '=', '+', '[', ']', '{', '}', '\\', '|', ';', ':', '\'', '"', ',', '<', '>', '/', '~', '`'];
        private static readonly List<string> Numbers = ["0", "1", "2", "3", "4", "5", "6", "7", "8", "9"];

        /// <summary>
        /// Generates forbidden passwords based on company-specific keywords and specific number combinations.
        /// </summary>
        /// <remarks>
        /// The forbidden passwords do not include keywords without special characters and numbers, since these are already blocked by the default password policy.
        /// </remarks>
        /// <param name="companySpecificKeywords">Company specific keywords</param>
        /// <param name="specificNumberCombinations">Specific number combinations</param>
        public static HashSet<string> Generate(List<string> companySpecificKeywords, List<string> specificNumberCombinations)
        {
            var numbers = Numbers.Concat(specificNumberCombinations);

            var forbiddenPasswords =
                from keyword in companySpecificKeywords
                from specialChar in SpecialChars
                from number in numbers
                from forbiddenPassword in new[]
                {
                    keyword + specialChar + number,
                    keyword + number + specialChar,
                    specialChar + keyword + number,
                    number + keyword + specialChar
                }
                select forbiddenPassword;

            return new HashSet<string>(forbiddenPasswords);
        }
    }
}
