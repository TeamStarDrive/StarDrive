using System;

namespace Ship_Game
{
	public sealed class MarkovNameGenerator
	{
		private Map<string, Array<char>> _chains = new Map<string, Array<char>>();

		private Array<string> _samples = new Array<string>();

		private Array<string> _used = new Array<string>();

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
					int n = _rnd.Next(_samples.Count);
					int nameLength = _samples[n].Length;
					for (s = _samples[n].Substring(_rnd.Next(0, _samples[n].Length - _order), _order); s.Length < nameLength; s = string.Concat(s, GetLetter(token)))
					{
						token = s.Substring(s.Length - _order, _order);
						if (GetLetter(token) == '?')
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
						string[] tokens = s.Split(' ');
						s = "";
						for (int t = 0; t < tokens.Length; t++)
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
				while (_used.Contains(s) || s.Length < _minLength);
				_used.Add(s);
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
			_order = order;
			_minLength = minLength;
			string[] strArrays = sampleNames.Split(',');
			for (int i = 0; i < strArrays.Length; i++)
			{
				string upper = strArrays[i].Trim().ToUpper();
				if (upper.Length >= order + 1)
				{
					_samples.Add(upper);
				}
			}
			foreach (string word in _samples)
			{
				for (int letter = 0; letter < word.Length - order; letter++)
				{
					string token = word.Substring(letter, order);
					Array<char> entry = null;
					if (!_chains.ContainsKey(token))
					{
						entry = new Array<char>();
						_chains[token] = entry;
					}
					else
					{
						entry = _chains[token];
					}
					entry.Add(word[letter + order]);
				}
			}
		}

		private char GetLetter(string token)
		{
			if (!_chains.ContainsKey(token))
			{
				return '?';
			}
			Array<char> letters = _chains[token];
			return letters[_rnd.Next(letters.Count)];
		}

		public void Reset()
		{
			_used.Clear();
		}
	}
}