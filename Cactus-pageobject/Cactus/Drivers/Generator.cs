using System;

namespace Cactus.Drivers
{
    /// <summary>
    /// Class Generator
    /// </summary>
    public class Generator
    {
        /// <summary>
        /// The _random
        /// </summary>
        static readonly Random _random = new Random();

        /// <summary>
        /// The alpha chars
        /// </summary>
        const string AlphaChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

        /// <summary>
        /// The numeric chars
        /// </summary>
        const string NumericChars = "0123456789";

        /// <summary>
        /// The alpha numeric chars
        /// </summary>
        const string AlphaNumericChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        /// <summary>
        /// The special valid email chars
        /// </summary>
        const string SpecialValidEmailChars = "-_.";

        /// <summary>
        /// All valid chars
        /// </summary>
        const string AllValidChars =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789,./?;:'*&^%$#@!~` ";

        /// <summary>
        /// Randoms the string.
        /// </summary>
        /// <param name="maxLength">Length of the max.</param>
        /// <param name="characterSet">The character set.</param>
        /// <returns>System.String.</returns>
        public static string RandomString(int maxLength, string characterSet)
        {
            var buffer = new char[maxLength];

            for (var i = 0; i < maxLength; i++)
            {
                buffer[i] = characterSet[_random.Next(characterSet.Length)];
            }

            return new string(buffer);
        }

        /// <summary>
        /// Returns a random boolean value
        /// </summary>
        /// <returns>Random boolean value</returns>
        public static bool RandomBool()
        {
            return (_random.NextDouble() > 0.5);
        }

        /// <summary>
        /// Randoms the string.
        /// </summary>
        /// <param name="maxLength">Length of the max.</param>
        /// <param name="characterGroup">The character group.</param>
        /// <returns>System.String.</returns>
        public static string RandomString(int maxLength, RandomCharacterGroup characterGroup)
        {
            switch (characterGroup)
            {
                case RandomCharacterGroup.AlphaOnly:
                    return RandomString(maxLength, AlphaChars);
                case RandomCharacterGroup.NumericOnly:
                    return RandomString(maxLength, NumericChars);
                case RandomCharacterGroup.AlphaNumericOnly:
                    return RandomString(maxLength, AlphaNumericChars);
                default:
                    return RandomString(maxLength, AllValidChars);

            }

        }

        /// <summary>
        /// Enum RandomCharacterGroup
        /// </summary>
        public enum RandomCharacterGroup
        {
            /// <summary>
            /// The alpha only
            /// </summary>
            AlphaOnly,

            /// <summary>
            /// The numeric only
            /// </summary>
            NumericOnly,

            /// <summary>
            /// The alpha numeric only
            /// </summary>
            AlphaNumericOnly,

            /// <summary>
            /// Any character
            /// </summary>
            AnyCharacter
        }

        /// <summary>
        /// Generates a random Email address using the supplied top level domain.
        /// </summary>
        /// <param name="tld">Top Level Domain (e.g. "com", "net", "org", etc)</param>
        /// <returns>A randomly generated email address with the top level domain passed in.</returns>
        public static string RandomEmailAddress(string tld)
        {
            return string.Format("{0}@{1}.{2}", RandomString(10, RandomCharacterGroup.AlphaOnly),
                RandomString(15, RandomCharacterGroup.AlphaNumericOnly), tld);
        }

        /// <summary>
        /// Randoms the email address.
        /// </summary>
        /// <returns>System.String.</returns>
        public static string RandomEmailAddress()
        {
            return string.Format("{0}@{1}.{2}", RandomString(10, RandomCharacterGroup.AlphaOnly),
                RandomString(15, RandomCharacterGroup.AlphaNumericOnly), "com");
        }

        /// <summary>
        /// Randoms the day func.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <returns>Func{DateTime}.</returns>
        public static Func<DateTime> RandomDayFunc(DateTime startDate)
        {

            Random gen = new Random();
            var timeSpan = DateTime.Today - startDate;
            {
                int range = ((TimeSpan) timeSpan).Days;

                return () => startDate.AddDays(gen.Next(range));
            }
        }

        /// <summary>
        /// Randoms the int32.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int RandomInt32()
        {
            unchecked
            {
                int firstBits = _random.Next(0, 1 << 4) << 28;
                int lastBits = _random.Next(0, 1 << 28);
                return firstBits | lastBits;
            }
        }

        /// <summary>
        /// Randoms the int32.
        /// </summary>
        /// <returns>System.Int32.</returns>
        public static int RandomInt(int min, int max)
        {
            return _random.Next(min, max);
        }

        /// <summary>
        /// Randoms the decimal.
        /// </summary>
        /// <param name="nonNegative">if set to <c>true</c> [non negative].</param>
        /// <returns>System.Decimal.</returns>
        public static decimal RandomDecimal(bool nonNegative)
        {
            var scale = (byte) _random.Next(29);
            return new decimal(RandomInt32(), RandomInt32(), RandomInt32(), nonNegative, scale);
        }

        /// <summary>
        /// Randoms the decimal.
        /// </summary>
        /// <param name="low">The low.</param>
        /// <param name="mid">The mid.</param>
        /// <param name="high">The high.</param>
        /// <param name="nonNegative">if set to <c>true</c> [non negative].</param>
        /// <returns>System.Decimal.</returns>
        public static decimal RandomDecimal(int low, int mid, int high, bool nonNegative)
        {
            var scale = (byte) _random.Next(29);
            return new decimal(low, mid, high, nonNegative, scale);
        }

        /// <summary>
        /// Random the decimal
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static decimal RandomDecimal(decimal min, decimal max)
        {
            decimal result = Convert.ToDecimal(_random.Next((int) (min*100), (int) (max*100)))/100;
            return result;
        }



        /// <summary>
        /// Random IP address
        /// </summary>
        /// <returns></returns>
        public static string RandomIPAddress()
        {
            return RandomString(3, RandomCharacterGroup.NumericOnly) + "." +
                   RandomString(3, RandomCharacterGroup.NumericOnly) + "." +
                   RandomString(3, RandomCharacterGroup.NumericOnly) + "." +
                   RandomString(3, RandomCharacterGroup.NumericOnly);
        }

        /// <summary>
        /// Random url
        /// </summary>
        /// <returns></returns>
        public static string RandomURL()
        {
            return "http://" + RandomString(4, RandomCharacterGroup.AlphaNumericOnly) + "/" +
                   RandomString(5, RandomCharacterGroup.AlphaNumericOnly);
        }

        /// <summary>
        /// Random UPC code
        /// </summary>
        /// <returns></returns>
        public static string RandomUPC()
        {
            string upc = "";
            int j;
            for (int i = 0; i < 12; i++)
            {
                j = _random.Next(0, 9)*10 ^ i;
                upc += j.ToString();
            }
            return upc;
        }

        /// <summary>
        /// Random State
        /// </summary>
        /// <returns></returns>
        public static string RandomState()
        {
            string[] state =
            {
                "AL", "AK", "AS", "AZ", "AR", "CA", "CO", "CT", "DE", "DC", "FL", "GA", "GU", "HI", "ID",
                "IL", "IN", "IA", "KS", "KY", "LA", "ME", "MD", "MH", "MA", "MI", "FM", "MN", "MS", "MO", "MT", "NE",
                "NV", "NH", "NJ", "NM", "NY", "NC", "ND", "MP", "OH", "OK", "OR", "PW", "PA", "PR", "RI", "SC", "SD",
                "TN", "TX", "UT", "VT", "VA", "VI", "WA", "WV", "WI", "WY"
            };

            return state[new Random().Next(0, state.Length)];
        }

        public static string RandomCompanyName()
        {
            string[] companyName =
            {
                "Idea", "Ideaa", "Aedi", "Idea Idea", "Idea Ine", "Idea Wiki", "Idea Leader",
                "Idea Canvas", "Idea Workshop", "Idea Horizon", "Idea Simple", "Idea Niche", "Idea Lens", "Idea Cent",
                "Idea Vine", "Idea Systems", "Idea Strategy", "Idea Emporium", "Ideaa", "Ideaar", "Ideamow", "Idea Next",
                "Idea Alliance", "Idea Technology", "Idea Crowd", "Ide Ine", "Ide Wiki", "Ide Leader", "Idea Cube",
                "Idea Network", "Idea Capital", "Idea Live", "Ide Niche", "Ide Lens", "Ide Cent", "Idea Venture",
                "Idea Affiliate", "Idea Future", "Idea Dev", "Idea", "Idear", "Idemow", "Idea Consultancy",
                "Idea Professionals", "Idea Topia", "Idea Strategies", "Id Ine", "Id Wiki", "Id Leader"
            };

            return companyName[new Random().Next(0, companyName.Length)];
        }



    }

}