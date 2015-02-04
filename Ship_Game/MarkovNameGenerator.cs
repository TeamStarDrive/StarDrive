using System;
using System.Collections.Generic;

namespace Ship_Game
{
	public class MarkovNameGenerator
	{
		private Dictionary<string, List<char>> _chains = new Dictionary<string, List<char>>();

		private List<string> _samples = new List<string>();

		private List<string> _used = new List<string>();

		private Random _rnd = new Random();

		private int _order;

		private int _minLength;

		public string NextName
		{
			get
			{
				string token = null;
				string s = "";
				do
				{
					int n = this._rnd.Next(this._samples.Count);
					int nameLength = this._samples[n].Length;
					for (s = this._samples[n].Substring(this._rnd.Next(0, this._samples[n].Length - this._order), this._order); s.Length < nameLength; s = string.Concat(s, this.GetLetter(token)))
					{
						token = s.Substring(s.Length - this._order, this._order);
						if (this.GetLetter(token) == '?')
						{
							break;
						}
					}
					if (!s.Contains(" "))
					{
						s = string.Concat(s.Substring(0, 1), s.Substring(1).ToLower());
					}
					else
					{
						string[] tokens = s.Split(new char[] { ' ' });
						s = "";
						for (int t = 0; t < (int)tokens.Length; t++)
						{
							if (!string.IsNullOrEmpty(tokens[t]))
							{
								if (tokens[t].Length != 1)
								{
									tokens[t] = string.Concat(tokens[t].Substring(0, 1), tokens[t].Substring(1).ToLower());
								}
								else
								{
									tokens[t] = tokens[t].ToUpper();
								}
								if (!string.IsNullOrEmpty(s))
								{
									s = string.Concat(s, " ");
								}
								s = string.Concat(s, tokens[t]);
							}
						}
					}
				}
				while (this._used.Contains(s) || s.Length < this._minLength);
				this._used.Add(s);
				return s;
			}
		}

		public MarkovNameGenerator(string sampleNames, int order, int minLength)
		{
			if (order < 1)
			{
				order = 1;
			}
			if (minLength < 1)
			{
				minLength = 1;
			}
			this._order = order;
			this._minLength = minLength;
			string[] strArrays = sampleNames.Split(new char[] { ',' });
			for (int i = 0; i < (int)strArrays.Length; i++)
			{
				string upper = strArrays[i].Trim().ToUpper();
				if (upper.Length >= order + 1)
				{
					this._samples.Add(upper);
				}
			}
			foreach (string word in this._samples)
			{
				for (int letter = 0; letter < word.Length - order; letter++)
				{
					string token = word.Substring(letter, order);
					List<char> entry = null;
					if (!this._chains.ContainsKey(token))
					{
						entry = new List<char>();
						this._chains[token] = entry;
					}
					else
					{
						entry = this._chains[token];
					}
					entry.Add(word[letter + order]);
				}
			}
		}

		private char GetLetter(string token)
		{
			if (!this._chains.ContainsKey(token))
			{
				return '?';
			}
			List<char> letters = this._chains[token];
			return letters[this._rnd.Next(letters.Count)];
		}

		public void Reset()
		{
			this._used.Clear();
		}
	}
}