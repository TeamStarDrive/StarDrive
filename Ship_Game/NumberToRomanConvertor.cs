using System;
using System.Text;

namespace Ship_Game
{
	internal class NumberToRomanConvertor
	{
		public NumberToRomanConvertor()
		{
		}

		public static string NumberToRoman(int number)
		{
			if (number == 0)
			{
				return "N";
			}
			int[] values = new int[] { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
			string[] strArrays = new string[] { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
			string[] numerals = strArrays;
			StringBuilder result = new StringBuilder();
			for (int i = 0; i < 13; i++)
			{
				while (number >= values[i])
				{
					number = number - values[i];
					result.Append(numerals[i]);
				}
			}
			return result.ToString();
		}
	}
}