using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Task4
{
    internal static class Program
    {
        private const string ResultsFolder = @"..\..\..\..\Tasks\bin\Debug\net5.0\results";
        private const string StemmedFolder = @"..\..\..\..\Tasks\bin\Debug\net5.0\stemmed";
        // private const string IndexFilePath = @"..\..\..\..\Tasks\bin\Debug\net5.0\inverted_index.txt";
        private const string TfIdfFilePath = @"..\..\..\..\Tasks\bin\Debug\net5.0\tf_idf.txt";

        private static void Main()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            // var readData = File.ReadAllLines(IndexFilePath).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x =>
            // {
            //     var split = x.Split(':');
            //     var key = split[0];
            //     var value = new List<int>(split[1].Split(',').Select(int.Parse));
            //     return (key, value);
            // }).ToDictionary(x => x.key, x => x.value);

            // ConcurrentDictionary<int, Dictionary<string, decimal>> tfs = new();
            // ReSharper disable once IdentifierTypo
            // ConcurrentDictionary<string, decimal> idfs = new();
            ConcurrentDictionary<string, int> uniqueTerms = new();
            ConcurrentDictionary<int, string[]> documentsTerms = new();
            
            // step 1 - получить уникальный список всех term'ов
            Parallel.ForEach(Directory.GetFiles(StemmedFolder), filePath =>
            {
                Console.WriteLine($"Processing terms of file {filePath}...");
                var fileLines = File.ReadAllLines(filePath);
                documentsTerms.GetOrAdd(int.Parse(Path.GetFileNameWithoutExtension(filePath)), fileLines);
                foreach (var word in fileLines)
                {
                    if (!uniqueTerms.ContainsKey(word))
                    {
                        uniqueTerms.GetOrAdd(word, 0);
                    }
                }
            });
            
            var totalDocumentCount = Directory.GetFiles(ResultsFolder).Length;
            var tfIdfCollection = new Numbers[totalDocumentCount][];

            // step 2 - посчитать tf-idf для каждого term'a из 1 шага - 
            // Матрица - кол-во док-ов * кол-во уникальных term'ов
            // for (var i = 0; i < totalDocumentCount; i++) 
            // each(var(documentPath, terms) in documentsTerms) )// count == totalDocumentCount
            Parallel.ForEach(Enumerable.Range(0, 100), i =>
            {
                Console.WriteLine($"Processing document {i}...");
                tfIdfCollection[i] = new Numbers[uniqueTerms.Count];
                var document = documentsTerms[i];
                var totalWordCountInDocument = document.Length;
                
                var j = 0;
                foreach (var (term, _) in uniqueTerms)
                {
                    var numberOfOccurrencesInDocument = document.Count(x => x == term);
                    var tf = numberOfOccurrencesInDocument / (double) totalWordCountInDocument;

                    if (tf == 0)
                    {
                        tfIdfCollection[i][j] = new Numbers {Tf = tf, Idf = 0, TfIdf = 0, Word = term};
                        j++;
                        continue;
                    }
                    
                    var numberOfDocumentsContainingTheTerm = documentsTerms.Count(x => x.Value.Contains(term));
                    var idf = Math.Log10(totalDocumentCount / (double) numberOfDocumentsContainingTheTerm);

                    var tfIdf = tf * idf;
                    tfIdfCollection[i][j] = new Numbers {Tf = tf, Idf = idf, TfIdf = tfIdf, Word = term};
                    j++;
                }
            });
            
            // Parallel.ForEach(Directory.GetFiles(StemmedFolder), filePath =>
            // {
            //     Console.WriteLine($"Processing file {filePath}...");
            //     var fileName = int.Parse(Path.GetFileNameWithoutExtension(filePath));
            //     var fileLines = File.ReadAllLines(filePath);
            //
            //     var wordCountInDocument = fileLines.Length;
            //
            //     foreach (var word in fileLines)
            //     {
            //         var numberOfOccurrencesOfWordInDocument = fileLines.Count(x => x == word);
            //         var wordTermFrequency =
            //             Math.Round(numberOfOccurrencesOfWordInDocument / (decimal) wordCountInDocument, 5,
            //                 MidpointRounding.ToEven);
            //         if (!tfs.ContainsKey(fileName)) tfs.TryAdd(fileName, new Dictionary<string, decimal>());
            //         if (!tfs[fileName].ContainsKey(word)) tfs[fileName].Add(word, 0);
            //         tfs[fileName][word] = wordTermFrequency;
            //     }
            // });

            // Parallel.ForEach(readData, pair =>
            // {
            //     var (key, value) = pair;
            //     var invertedDocumentFrequency =
            //         Math.Round((decimal) Math.Log10(totalDocumentCount / (double) (1 + value.Select(x=>x > 0 ? 1 : 0).Sum())), 5, MidpointRounding.ToEven);
            //     if (!idfs.ContainsKey(key)) idfs.TryAdd(key, 0);
            //     idfs[key] = invertedDocumentFrequency;
            // });
            
            if (File.Exists(TfIdfFilePath))
                File.Delete(TfIdfFilePath);

            using var streamWriter = new StreamWriter(TfIdfFilePath);

            // foreach (var (document, value) in tfs)
            // {
            //     foreach (var (word, tf) in value)   
            //     {
            for (var i = 0; i < tfIdfCollection.Length; i++)
            for (var j = 0; j < tfIdfCollection[i].Length; j++)
            {
                var number = tfIdfCollection[i][j];
                streamWriter.WriteLine($@"{number.Word,25};{i,10};{Round(number.Tf),10};{Round(number.Idf),10};{Round(number.TfIdf),10}");
            }

            stopwatch.Stop();
            Console.WriteLine(stopwatch.Elapsed);
            // }
            // }
        }

        private static decimal Round(double val) => Math.Round((decimal) val, 5, MidpointRounding.ToEven);
    }
}