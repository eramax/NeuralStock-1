namespace twentySix.NeuralStock.Core.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Security.Cryptography;

    public static class RandomExtensions
    {
        private static readonly RNGCryptoServiceProvider CryptoServiceProvider = new RNGCryptoServiceProvider();

        public static int BetterRandomInteger(int minimumValue, int maximumValue)
        {
            var randomNumber = new byte[1];
            CryptoServiceProvider.GetBytes(randomNumber);
            var asciiValueOfRandomCharacter = Convert.ToDouble(randomNumber[0]);
            var multiplier = Math.Max(0, (asciiValueOfRandomCharacter / 255d) - 0.00000000001d);
            var range = maximumValue - minimumValue + 1;
            var randomValueInRange = Math.Floor(multiplier * range);

            return (int)(minimumValue + randomValueInRange);
        }

        public static double BetterRandomDouble(double minimumValue, double maximumValue)
        {
            var data = new byte[sizeof(uint)];
            CryptoServiceProvider.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return minimumValue + ((randUint / (uint.MaxValue + 1.0)) * (maximumValue - minimumValue));
        }

        public static decimal BetterRandomDecimal(decimal minimumValue, decimal maximumValue)
        {
            var data = new byte[sizeof(uint)];
            CryptoServiceProvider.GetBytes(data);
            var randUint = BitConverter.ToUInt32(data, 0);
            return minimumValue + (randUint / (uint.MaxValue + 1.0m)) * (maximumValue - minimumValue);
        }

        public static double NextGaussian(this Random r, double mu = 0, double sigma = 1)
        {
            var u1 = r.NextDouble();
            var u2 = r.NextDouble();

            var randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                  Math.Sin(2.0 * Math.PI * u2);

            var randNormal = mu + (sigma * randStdNormal);

            return randNormal;
        }

        public static double NextGaussianPositive(this Random r, double mu = 0, double sigma = 1)
        {
            var randNormal = NextGaussian(r, mu, sigma);

            while (randNormal <= 0)
            {
                randNormal = NextGaussian(r, mu, sigma);
            }

            return randNormal;
        }

        public static double NextGaussianLimited(this Random r, double mu = 0, double sigma = 1, double? min = null, double? max = null)
        {
            var randNormal = NextGaussian(r, mu, sigma);

            if (min == null && max == null)
            {
                return randNormal;
            }

            if (min != null && max == null)
            {
                while (randNormal < min)
                {
                    randNormal = NextGaussian(r, mu, sigma);
                }
            }
            else if (min == null)
            {
                while (randNormal > max)
                {
                    randNormal = NextGaussian(r, mu, sigma);
                }
            }
            else
            {
                while (randNormal < min || randNormal > max)
                {
                    randNormal = NextGaussian(r, mu, sigma);
                }
            }

            return randNormal;
        }

        public static int NextGaussianPositiveInteger(this Random r, double mu = 0, double sigma = 1)
        {
            var randNormal = (int)NextGaussian(r, mu, sigma);

            while (randNormal <= 0)
            {
                randNormal = (int)NextGaussian(r, mu, sigma);
            }

            return randNormal;
        }

        public static int NextGaussianIntegerLimited(this Random r, double mu = 0, double sigma = 1, int? min = null, int? max = null)
        {
            return (int)NextGaussianLimited(r, mu, sigma, min, max);
        }

        public static double NextTriangular(this Random r, double a, double b, double c)
        {
            var u = r.NextDouble();

            return u < (c - a) / (b - a)
                       ? a + Math.Sqrt(u * (b - a) * (c - a))
                       : b - Math.Sqrt((1 - u) * (b - a) * (b - c));
        }

        public static bool NextBoolean(this Random r)
        {
            return r.Next(2) > 0;
        }

        public static int NextInteger(this Random r, int min, int max)
        {
            return r.Next(min, max + 1);
        }

        public static double NextDouble(this Random r, double min, double max)
        {
            return min + r.NextDouble() * (max - min);
        }

        public static void Shuffle(this Random r, IList list)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var j = r.Next(0, i + 1);

                var temp = list[j];
                list[j] = list[i];
                list[i] = temp;
            }
        }

        public static int[] Permutation(this Random rand, int n, int k)
        {
            var result = new List<int>();
            var sorted = new SortedSet<int>();

            for (var i = 0; i < k; i++)
            {
                var r = rand.Next(1, n + 1 - i);

                foreach (var q in sorted)
                {
                    if (r >= q)
                    {
                        r++;
                    }
                }

                result.Add(r);
                sorted.Add(r);
            }

            return result.ToArray();
        }
    }
}