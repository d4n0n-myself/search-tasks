using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
        private const string StemmedFolder = @"..\..\..\..\Tasks\bin\Debug\net5.0\stemmed";
        private const string Config = @"..\..\..\..\Task2\Core14.profile.xml";

        private static void Main(string[] args)
        {
            var fileLines = File.ReadAllLines(TfIdfFilePath);
            Dictionary<int, Dictionary<string, decimal>> docVectors = new();
            ConcurrentDictionary<int, string[]> documentsTerms = new();            
            Parallel.ForEach(Directory.GetFiles(StemmedFolder), filePath =>
            {
                // Console.WriteLine($"Processing terms of file {filePath}...");
                var fileLines1 = File.ReadAllLines(filePath);
                documentsTerms.GetOrAdd(int.Parse(Path.GetFileNameWithoutExtension(filePath)), fileLines1);
            });
            // Dictionary<int, Dictionary<string, decimal>> tfs = new();
            var totalDocumentCount = 100;
            
            ConcurrentDictionary<int, List<Numbers>> tfIdfCollection = new();
            ConcurrentDictionary<string, decimal> uniqueTerms = new(); // word - tfIdf

            var factory = new RankedLanguageIdentifierFactory();
            var identifier = factory.Load(Config);

            // parse tf-idf
            foreach (var fileLine in fileLines)
            {
                var values = fileLine.Split(';', ' ').Where(x=>!string.IsNullOrWhiteSpace(x)).ToArray();

                var documentIndex = int.Parse(values[1]);
                
                if (!tfIdfCollection.ContainsKey(documentIndex))
                {
                    tfIdfCollection.GetOrAdd(documentIndex, new List<Numbers>());
                }
                
                // if (!docVectors.ContainsKey(documentIndex))
                // {
                //     docVectors.Add(documentIndex, new Dictionary<string, decimal>());
                // }
                //
                // if (!tfs.ContainsKey(documentIndex))
                // {
                //     tfs.Add(documentIndex, new Dictionary<string, decimal>());
                // }

                var word = values[0];
                var tf =  decimal.Parse(values[2]);
                var idf = decimal.Parse( values[3]);
                var tfIdf = decimal.Parse(values[4]);
                tfIdfCollection[documentIndex].Add(new Numbers() {Idf = idf, Tf = tf, Word = word, DocIndex = documentIndex, TfIdf = tfIdf});
                
                uniqueTerms.GetOrAdd(word, tfIdf);
                // docVectors[documentIndex].Add(word, decimal.Parse(tfIdf));
                // tfs[documentIndex].Add(word, decimal.Parse(tf));
            }

            var queryVector = new Numbers[uniqueTerms.Count];
            
            // Dictionary<int, List<decimal>> docResVectors = new();

            // normalize query terms
            var query = args.Select(x => PrepareWord(identifier, x)).ToArray();
            // List<decimal> queryVector = new();
            
            // step 3 - посчитать tf-idf для запроса 
            // ReSharper disable once LoopCanBeConvertedToQuery

            // Console.WriteLine(string.Join(",", uniqueTerms.Take(10)));
            // Console.WriteLine(uniqueTerms["transact"]);
            var i1 = 0;
            foreach (var (term, _) in uniqueTerms)
            {
                var numberOfOccurrences = query.Count(x => x == term);
                var tf = numberOfOccurrences / (double) query.Length;

                if (tf == 0)
                {
                    queryVector[i1] = new Numbers { Word = term};
                    i1++;
                    continue;
                }
                
                var numberOfDocumentsContainingTheTerm = documentsTerms.Count(x => x.Value.Contains(term));
                var idf = Math.Log10(totalDocumentCount / (double) numberOfDocumentsContainingTheTerm);

                var tfIdf = tf * idf;
                queryVector[i1] = new Numbers()
                {
                    Word = term, TfIdf = Round(tfIdf), Idf = Round(idf), Tf = Round(tf)
                };
                i1++;

                // queryVector.Add(tfIdf);

                // foreach (var (key, _) in docVectors)
                // {
                //     if (!docResVectors.ContainsKey(key))
                //     {
                //         docResVectors.Add(key, new List<decimal>());
                //     }
                //
                //     docResVectors[key]
                //         .Add(docVectors.ContainsKey(key)
                //             ? docVectors[key]
                //                 .ContainsKey(queryWord)
                //                 ? docVectors[key][queryWord]
                //                 : 0
                //             : 0);
                // }
            }

            Dictionary<int, double> cos = new();

            // for (var j = 0; j < query.Length; j++)
            for (var i = 0; i < totalDocumentCount; i++)
            {
                var v1 = queryVector;
                var v2 = tfIdfCollection[i];

                if (v1.Length != v2.Count) throw new Exception("Vector count not match");
                if (v1.Length != uniqueTerms.Count) throw new Exception("Vector count not match");

                var sum = WordJoin(v1, v2);
                var p1 = Math.Sqrt(WordJoin(v1, v1));
                var p2 = Math.Sqrt(WordJoin(v2, v2));

                var cosVal = sum / (p1 * p2);
                cos.Add(i, cosVal);
                
                // var preparedQueryWord = query[j];
                // var tf = tfs[i].ContainsKey(preparedQueryWord) ? tfs[i][preparedQueryWord] : 0;
                // var tfMax = tfs[i].Max(x => x.Value);
                // var idf = Math.Log10(totalNumberOfDocuments / (double) (1 + docResVectors.Select(x => x.Value[j] > 0 ? 1 : 0).Sum()));
                // var value = idf * (0.5 + 0.5 * ((double) tf / (double) tfMax));

                // if (!cos.ContainsKey(j)) cos.Add(j, new Dictionary<int, double>());
                // cos[j].Add(i, value);
            }

            cos.OrderByDescending(x => x.Value).Take(10)
                .ForEach(x => Console.WriteLine(x.Key + ";" + Round(x.Value)));

            // Console.WriteLine(String.Join(",", queryVector));
            Console.WriteLine(cos.Select(y => y.Value).Any(x => x < 0 || x > 1));
        }

        private static double WordJoin(IEnumerable<Numbers> v1, IEnumerable<Numbers> v2)
        {
            return (double) v1.Join(v2,
                    numbers => numbers.Word,
                    numbers => numbers.Word,
                    (first, second) => first.TfIdf * second.TfIdf)
                .Sum();
        }

        private static decimal Round(double val) => Math.Round((decimal) val, 5, MidpointRounding.ToEven);
        private static decimal Round(decimal val) => Math.Round(val, 5, MidpointRounding.ToEven);
        
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
