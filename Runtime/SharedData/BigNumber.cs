using UnityEngine;
using System.Numerics;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SwiftFramework.Core
{
    [Serializable]
    public struct BigNumber : ISerializationCallbackReceiver
    {
        public string stringValue;

        private BigInteger? bigValue;

        public bool FitsInFloat => Value < MAX_FLOAT;

        private static readonly BigInteger MAX_FLOAT = new BigInteger(float.MaxValue);

        public BigInteger Value
        {
            get
            {
                if (bigValue.HasValue == false)
                {
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        bigValue = 0;
                        stringValue = bigValue.Value.ToString();
                        return 0;
                    }
                    if (BigInteger.TryParse(stringValue, out BigInteger v))
                    {
                        bigValue = v;
                    }
                    else
                    {
                        Debug.LogError($"Cannot parse big int {stringValue}");
                        bigValue = 0;
                    }
                }
                return bigValue.Value;
            }
            set
            {
                if (this.bigValue != value)
                {
                    this.bigValue = value;
#if UNITY_EDITOR
                    stringValue = value.ToString();
#endif
                }
            }
        }

        public override string ToString()
        {
            return Value.ToPrettyString();
        }

        public static BigInteger FromCompressedInt(int comressedInt, int presicionDigitsCount = 3)
        {
            if (comressedInt < BigInteger.Pow(10, presicionDigitsCount))
            {
                return comressedInt;
            }
            string numString = comressedInt.ToString();
            int.TryParse(numString.Substring(0, numString.Length - presicionDigitsCount), out int zeroesCount);
            int.TryParse(numString.Substring(numString.Length - presicionDigitsCount, presicionDigitsCount), out int num);
            return num * BigInteger.Pow(10, zeroesCount);
        }

        [OnSerializing]
        internal void OnSerializing(StreamingContext context)
        {
            if (bigValue.HasValue)
            {
                stringValue = bigValue.ToString();
            }
            if (string.IsNullOrEmpty(stringValue))
            {
                stringValue = "0";
            }
        }

        public void OnBeforeSerialize()
        {
            if (bigValue.HasValue)
            {
                stringValue = bigValue.ToString();
            }
            if (string.IsNullOrEmpty(stringValue))
            {
                stringValue = "0";
            }
        }

        public void OnAfterDeserialize()
        {

            BigInteger res = new BigInteger();
            if (BigInteger.TryParse(stringValue, out res))
            {
                bigValue = res;
            }
            else
            {
                bigValue = 0;
            }
        }

        public int ConvertToCompressedInt(int presicionDigitsCount = 3)
        {
            BigInteger n = Value;

            int zeroesCount = 0;

            if (n < 10)
            {
                return 0;
            }

            while (n > 10 && n % 10 == 0)
            {
                n /= 10;
                zeroesCount++;
            }

            if (int.TryParse($"{zeroesCount}{Value.ToString().Substring(0, presicionDigitsCount)}", out int result))
            {
                return result;
            }

            return 0;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BigNumber))
            {
                return false;
            }

            var number = (BigNumber)obj;
            return number.Value == Value;
        }

        public override int GetHashCode()
        {
            return 1101458105 + Value.GetHashCode();
        }

        public BigNumber(string numberString)
        {
            if (string.IsNullOrEmpty(numberString))
            {
                numberString = "0";
            }
            if (BigInteger.TryParse(numberString, out BigInteger v) == false)
            {
                Debug.LogError($"Cannot parse big int {numberString}");
                numberString = "0";
            }
            bigValue = v;
            stringValue = numberString;
        }

        public BigNumber(BigInteger number)
        {
            stringValue = null;
            bigValue = number;
        }

        public BigNumber(int number)
        {
            stringValue = null;
            bigValue = number;
        }

        public static implicit operator BigNumber(int value)
        {
            return new BigNumber(value);
        }

        public static implicit operator BigNumber(uint value)
        {
            return new BigNumber(value);
        }

        public static implicit operator BigNumber(BigInteger value)
        {
            return new BigNumber(value);
        }


        public static implicit operator BigNumber(string value)
        {
            return new BigNumber(value);
        }

        public static BigNumber operator +(BigNumber v1, BigInteger v2)
        {
            v1.Value += v2;
            return v1;
        }

        public static BigNumber operator +(BigNumber v1, BigNumber v2)
        {
            return v1.Value += v2.Value;
        }

        public static BigNumber operator -(BigNumber v1, BigInteger v2)
        {
            v1.Value -= v2;
            return v1;
        }

        public static BigNumber operator -(BigNumber v1, BigNumber v2)
        {
            v1.Value -= v2.Value;
            return v1;
        }

        public static bool operator >(BigNumber v1, BigNumber v2)
        {
            return v1.Value > v2.Value;
        }

        public static bool operator <(BigNumber v1, BigNumber v2)
        {
            return v1.Value < v2.Value;
        }

        public static bool operator >=(BigNumber v1, BigNumber v2)
        {
            return v1.Value >= v2.Value;
        }

        public static bool operator <=(BigNumber v1, BigNumber v2)
        {
            return v1.Value <= v2.Value;
        }

        public static bool operator ==(BigNumber v1, BigNumber v2)
        {
            return v1.Value == v2.Value;
        }

        public static bool operator !=(BigNumber v1, BigNumber v2)
        {
            return v1.Value != v2.Value;
        }

        public static BigNumber operator *(BigNumber v1, BigInteger v2)
        {
            v1.Value *= v2;
            return v1;
        }

        public static BigNumber operator /(BigNumber v1, BigInteger v2)
        {
            v1.Value /= v2;
            return v1;
        }
    }

    public static class BigNumberExtensions
    {
        private static readonly Dictionary<BigInteger, string> cachedBigIntegers = new Dictionary<BigInteger, string>(100);


        public static int ToCompressedInt(this BigInteger number, int presicionDigitsCount = 3)
        {
            string numberString = number.ToString();

            int zeroesCount = numberString.Length;

            if (zeroesCount < presicionDigitsCount)
            {
                return (int)number;
            }

            string roundedNumberString = numberString.Substring(0, presicionDigitsCount);

            string zeroesAmountString = (zeroesCount - presicionDigitsCount).ToString();

            if (int.TryParse($"{zeroesAmountString}{roundedNumberString}", out int result))
            {
                return result;
            }

            return 0;
        }

        private static string GetStringModifier(int numberOfThousands)
        {
            string res = "";

            switch (numberOfThousands)
            {
                case 2:
                    res = "K";
                    break;
                case 3:
                    res = "M";
                    break;

                case 4:
                    res = "B";
                    break;

                case 5:
                    res = "T";
                    break;

                default:
                    char firstLetter = (char)((numberOfThousands - 5) / 26 + 'a');
                    char secondLetter = (char)((numberOfThousands - 5) % 26 + 'a');
                    res = firstLetter.ToString() + secondLetter.ToString();
                    break;

            }
            return res;
        }

        private static string DuplicateSymbol(char symbol, int numberOfTimes)
        {
            string result = string.Empty;
            for (int i = 0; i < numberOfTimes; i++)
            {
                result = string.Format("{0}{1}", result, symbol);
            }
            return result;
        }

        public static string ToPrettyFracturedString(this BigInteger value, int symbolsAfterComa = 1)
        {
            if (cachedBigIntegers.ContainsKey(value))
            {
                return cachedBigIntegers[value];
            }

            var str = value.ToString();

            var length = str.Length;

            if (length < 4)
            {
                return str;
            }

            var integerPartLength = length % 3;
            if (integerPartLength == 0)
                integerPartLength = 3;

            var numberOfThousands = Mathf.CeilToInt(length / 3.0f);

            var integerPart = str.Substring(0, integerPartLength);

            var fractionalPart = str.Substring(integerPartLength, symbolsAfterComa);

            var fractional = int.Parse(fractionalPart);

            string res = fractional == 0 ? $"{integerPart}{GetStringModifier(numberOfThousands)}"
                : $"{integerPart},{fractionalPart}{GetStringModifier(numberOfThousands)}";

            cachedBigIntegers.Add(value, res);

            return res;

        }

        public static string ToPrettyString(this BigInteger value)
        {
            if (cachedBigIntegers.ContainsKey(value))
            {
                return cachedBigIntegers[value];
            }

            var str = value.ToString();

            var length = str.Length;

            if (length < 4)
            {
                return str;
            }

            var integerPartLength = length % 3;
            if (integerPartLength == 0)
                integerPartLength = 3;

            var numberOfThousands = Mathf.CeilToInt(length / 3.0f);

            var integerPart = str.Substring(0, integerPartLength);
            var fractionalPart = str.Substring(integerPartLength, 2);

            var fractional = int.Parse(fractionalPart);
            string res;

            res = fractional == 0 ? $"{integerPart}{GetStringModifier(numberOfThousands)}"
                : $"{integerPart},{fractionalPart}{GetStringModifier(numberOfThousands)}";

            cachedBigIntegers.Add(value, res);

            return res;
        }

        public static string ToFracturedPrettyString(this BigInteger value)
        {
            if (cachedBigIntegers.ContainsKey(value))
            {
                return cachedBigIntegers[value];
            }

            var str = value.ToString();

            var length = str.Length;

            if (length < 4)
            {
                cachedBigIntegers.Add(value, str);
                return str;
            }

            var integerPartLength = length % 3;
            if (integerPartLength == 0)
                integerPartLength = 3;

            var numberOfThousands = Mathf.CeilToInt(length / 3.0f);

            var integerPart = str.Substring(0, integerPartLength);
            var fractionalPart = str.Substring(integerPartLength, 1);

            var fractional = int.Parse(fractionalPart);
            string res;
            res = fractional == 0 ? $"{integerPart},0{GetStringModifier(numberOfThousands)}"
                : $"{integerPart},{fractionalPart}{GetStringModifier(numberOfThousands)}";

            cachedBigIntegers.Add(value, res);

            return res;
        }

        public static BigInteger MultiplyByFloat(this BigInteger bigInt, float value, int precision = 1000)
        {
            if (value == 0)
                return 0;

            if (bigInt < 10000)
            {
                float tmpVal = (int)bigInt;

                tmpVal *= value;

                bigInt = (int)Mathf.Ceil(tmpVal);

                return bigInt;
            }

            var multiplierPercent = (int)(value * precision);
            bigInt *= multiplierPercent;
            return bigInt / precision;
        }

        public static BigInteger MultiplyByDouble(this BigInteger bigInt, double value, int precision = 1000)
        {
            if (value == 0)
                return 0;
            var multiplierPercent = (int)(value * precision);
            bigInt *= multiplierPercent;
            return bigInt / precision;
        }

        public static BigInteger DivideByFloat(this BigInteger bigInt, float value, int precision = 1000)
        {
            var big = bigInt * precision;
            var division = new BigInteger(value * precision);
            if (division == 0)
                division = 1;
            big /= division;

            return big;
        }


        public static BigInteger BigNumberPow(this float number, long power)
        {
            if (power == 0)
                return 1;

            BigInteger bigInt;
            float small = number;
            long index = 0;

            for (long i = 0; i < power - 1; i++)
            {
                if (small > 1000)
                {
                    index = i;
                    bigInt = (int)small;

                    while (index < power - 1)
                    {
                        bigInt = bigInt.MultiplyByFloat(number);
                        index++;
                    }

                    return bigInt;
                }

                small *= number;
            }

            bigInt = (int)small;

            return bigInt;
        }
    }
}
