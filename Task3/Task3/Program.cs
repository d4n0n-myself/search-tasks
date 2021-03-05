using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Task3
{
	internal static class Program
	{
		private const string StemmedFolder = @"..\..\..\..\..\Tasks\bin\Debug\net5.0\stemmed";

		private static void Main(string[] args)
		{
			var invertedIndex = new ConcurrentDictionary<string, List<int>>();

			Console.WriteLine("Reading...");
			Parallel.ForEach(Directory.GetFiles(StemmedFolder), file =>
			{
				var groups = File.ReadAllLines(file).GroupBy(x => x);
				var fileIndex = int.Parse(Path.GetFileNameWithoutExtension(file));
				foreach (var grouping in groups)
				{
					invertedIndex.AddOrUpdate(grouping.Key,
						_ => new List<int>(fileIndex),
						(_, ints) =>
						{
							ints.Add(fileIndex);
							return ints;
						});
				}
			});

			Console.WriteLine("Enter a query:");
			while (true)
			{
				var input = Console.ReadLine();

				if (string.IsNullOrEmpty(input))
				{
					Console.WriteLine("empty string");
					Console.WriteLine("Enter a query:");
					continue;
				}

				var operators = input.Where(x => x == '&' || x == '|').ToArray();
				if (operators.Length != 2)
				{			
					Console.WriteLine("bad count boolean operators!");
					Console.WriteLine("Enter a query:");
					continue;
				}

				var words = input.Split('&', '|')
					.Select(x => x.Trim())
					.Where(x => !string.IsNullOrWhiteSpace(x))
					.ToArray();

				if (words.Length != 3)
				{
					Console.WriteLine("bad count words!");
					Console.WriteLine("Enter a query:");
					continue;
				}
				
				IEnumerable<int> result;

				var docs = new List<List<int>>
				{
					invertedIndex[words[0]], 
					invertedIndex[words[1]], 
					invertedIndex[words[2]]
				};

				for (var i = 0; i < words.Length; i++)
				{
					if (words[i].StartsWith('!'))
					{
						docs[i] = GetInvertedValue(docs[i]);
					}
				}

				if (operators[0] == '&')
				{
					if (operators[1] == '&')
					{
						result = docs[0]
							.Join(docs[1], i => i, i => i, (i, _) => i)
							.Join(docs[2], i => i, i => i, (i, _) => i);
					}
					else // '|'
					{
						result = docs[0]
							.Join(docs[1], i => i, i => i, (i, _) => i)
							.ToList()
							.Concat(docs[2]);
					}
				}
				else // operator[0] == '|'
				{
					if (operators[1] == '&')
					{
						result = docs[1]
							.Join(docs[2], i => i, i => i, (i, _) => i)
							.ToList()
							.Concat(docs[0]);
					}
					else // '|'
					{
						result = docs[0]
							.Concat(docs[1])
							.Concat(docs[2]);
					}
				}
				
				Console.WriteLine($"Result: {string.Join(", ", result)}");				
				Console.WriteLine("Enter a query:");
			}
		}

		private static List<int> GetInvertedValue(IEnumerable<int> values)
		{
			return Enumerable.Range(0, 99).Except(values).ToList();
		}
	}
}