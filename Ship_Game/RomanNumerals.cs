using System.Text;

namespace Ship_Game
{
	internal class RomanNumerals
	{
		public static string ToRoman(int number)
		{
			if (number == 0)
				return "N";

			int[] values = { 1000, 900, 500, 400, 100, 90, 50, 40, 10, 9, 5, 4, 1 };
			string[] numerals = { "M", "CM", "D", "CD", "C", "XC", "L", "XL", "X", "IX", "V", "IV", "I" };
			var sb = new StringBuilder();
			for (int i = 0; i < 13; i++)
			{
				while (number >= values[i])
				{
					number -= values[i];
					sb.Append(numerals[i]);
				}
			}
			return sb.ToString();
		}
	}
}