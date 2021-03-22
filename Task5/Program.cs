using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Iveonik.Stemmers;
using NTextCat;
using NTextCat.Commons;
// ReSharper disable CollectionNeverQueried.Local
// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Task5
{
    internal static class Program
    {
        private const string TfIdfFilePath = @"..\..\..\..\Tasks\bin\Debug\net5.0\tf_idf.txt";
        private const string Config = @"..\..\..\..\Task2\Core14.profile.xml";

        private static void Main(string[] args)
        {
            var fileLines = File.ReadAllLines(TfIdfFilePath);
            Dictionary<int, Dictionary<string, decimal>> docVectors = new();
            Dictionary<int, Dictionary<string, decimal>> tfs = new();

            var factory = new RankedLanguageIdentifierFactory();
            var identifier = factory.Load(Config);

            foreach (var fileLine in fileLines)
            {
                var values = fileLine.Split(';', ' ').Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();

                var documentIndex = int.Parse(values[1]);
                if (!docVectors.ContainsKey(documentIndex))
                {
                    docVectors.Add(documentIndex, new Dictionary<string, decimal>());
                }

                if (!tfs.ContainsKey(documentIndex))
                {
                    tfs.Add(documentIndex, new Dictionary<string, decimal>());
                }

                var word = values[0];
                var tfIdf = values[4];
                var tf = values[2];
                docVectors[documentIndex].Add(word, decimal.Parse(tfIdf));
                tfs[documentIndex].Add(word, decimal.Parse(tf));
            }

            Dictionary<int, List<decimal>> docResVectors = new();

            var query = args.Select(x => PrepareWord(identifier, x)).ToArray();
            List<decimal> queryVector = new();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var queryWord in query)
            {
                const int idf = 1;
                var numberOfOccurrences = query.Count(x => x == queryWord);
                var tf = Math.Round(numberOfOccurrences / (decimal) query.Length, 5, MidpointRounding.ToEven);
                var tfIdf = Math.Round((decimal) ((double) tf * Math.Log2(idf)), 5, MidpointRounding.ToEven);
                queryVector.Add(tfIdf);

                foreach (var (key, _) in docVectors)
                {
                    if (!docResVectors.ContainsKey(key))
                    {
                        docResVectors.Add(key, new List<decimal>());
                    }

                    docResVectors[key]
                        .Add(docVectors.ContainsKey(key)
                            ? docVectors[key]
                                .ContainsKey(queryWord)
                                ? docVectors[key][queryWord]
                                : 0
                            : 0);
                }
            }

            Dictionary<int, Dictionary<int, double>> cos = new();

            for (var j = 0; j < query.Length; j++)
            for (var i = 0; i < docResVectors.Count; i++)
            {
                var preparedQueryWord = query[j];
                var tf = tfs[i].ContainsKey(preparedQueryWord) ? tfs[i][preparedQueryWord] : 0;
                var tfMax = tfs[i].Max(x => x.Value);
                var n = docVectors.Count;
                var idf = Math.Log2(n / (double) docResVectors.Select(x => x.Value[j] > 0 ? 1 : 0).Sum());
                var value = idf * (0.5 + 0.5 * ((double) tf / (double) tfMax));

                if (!cos.ContainsKey(j)) cos.Add(j, new Dictionary<int, double>());
                cos[j].Add(i, value);
            }

            cos.SelectMany(x => x.Value.Select(y => y)).OrderByDescending(x => x.Value).Take(10)
                .ForEach(x => Console.WriteLine(x.Key + ";" + x.Value));
        }

        private static string PrepareWord(RankedLanguageIdentifier identifier, string or)
        {
            var languages = identifier.Identify(or).ToArray();
            var myLanguages = languages.Where(x => x.Item1.Iso639_3 == "eng" || x.Item1.Iso639_3 == "rus").ToArray();
            if (!myLanguages.Any())
            {
                throw new NotSupportedException();
            }

            var max = myLanguages.Min(x => x.Item2);
            var lang = myLanguages.First(x => x.Item2 == max);
            var langCode = lang?.Item1.Iso639_3;
            IStemmer stemmer = langCode switch
            {
                "eng" => new EnglishStemmer(),
                "rus" => new RussianStemmer(),
                _ => throw new Exception()
            };
            var stemmed = stemmer.Stem(or);
            return stemmed;
        }
    }
}
