using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Task4
{
    internal static class Program
    {
        private const string ResultsFolder = @"..\..\..\..\Tasks\bin\Debug\net5.0\results";
        private const string StemmedFolder = @"..\..\..\..\Tasks\bin\Debug\net5.0\stemmed";
        private const string IndexFilePath = @"..\..\..\..\Tasks\bin\Debug\net5.0\inverted_index.txt";
        private const string TfIdfFilePath = @"..\..\..\..\Tasks\bin\Debug\net5.0\tf_idf.txt";

        private static void Main()
        {
            var readData = File.ReadAllLines(IndexFilePath).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x =>
            {
                var split = x.Split(':');
                var key = split[0];
                var value = new List<int>(split[1].Split(',').Select(int.Parse));
                return (key, value);
            }).ToDictionary(x => x.key, x => x.value);

            var documentCount = Directory.GetFiles(ResultsFolder).Length;
            
            ConcurrentDictionary<int, Dictionary<string, decimal>> tfs = new();
            // ReSharper disable once IdentifierTypo
            ConcurrentDictionary<string, decimal> idfs = new();
            
            Parallel.ForEach(Directory.GetFiles(StemmedFolder), filePath =>
            {
                Console.WriteLine($"Processing file {filePath}...");
                var fileName = int.Parse(Path.GetFileNameWithoutExtension(filePath));
                var fileLines = File.ReadAllLines(filePath);

                var wordCountInDocument = fileLines.Length;

                foreach (var word in fileLines)
                {
                    var numberOfOccurrencesOfWordInDocument = fileLines.Count(x => x == word);
                    var wordTermFrequency =
                        Math.Round(numberOfOccurrencesOfWordInDocument / (decimal) wordCountInDocument, 5,
                            MidpointRounding.ToEven);
                    if (!tfs.ContainsKey(fileName)) tfs.TryAdd(fileName, new Dictionary<string, decimal>());
                    if (!tfs[fileName].ContainsKey(word)) tfs[fileName].Add(word, 0);
                    tfs[fileName][word] = wordTermFrequency;
                }
            });

            Parallel.ForEach(readData, pair =>
            {
                var (key, value) = pair;
                var invertedDocumentFrequency =
                    Math.Round(documentCount / (decimal) value.Count, 5, MidpointRounding.ToEven);
                if (!idfs.ContainsKey(key)) idfs.TryAdd(key, 0);
                idfs[key] = invertedDocumentFrequency;
            });
            
            if (File.Exists(TfIdfFilePath))
                File.Delete(TfIdfFilePath);

            using var streamWriter = new StreamWriter(TfIdfFilePath);

            foreach (var (document, value) in tfs)
            {
                foreach (var (word, tf) in value)
                {
                    var idf = idfs[word];
                    var tfIdf = Math.Round((decimal) ((double) tf * Math.Log2((double) idf)), 5, MidpointRounding.ToEven);
                    streamWriter.WriteLine($@"{word},{document},{tf},{idf},{tfIdf}");
                }
            }
        }
    }
}