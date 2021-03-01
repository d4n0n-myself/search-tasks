using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Iveonik.Stemmers;
using NTextCat;

namespace Task2
{
    internal static class Program
    {
        private const string ResultsFolder = @"C:\repos\search-tasks\Tasks\bin\Debug\net5.0\results";
        private const string StemmedFolder = @"C:\repos\search-tasks\stemmed";
        
        private static readonly char[] Splitters = {
            '\n', '\t', '\r', ':', ';', '(', ')', '.', ',', ' ', '-', '"', '{', '}', '!', '?', '@', '$', '='
        };

        private static void Main()
        {
            if (!Directory.Exists(StemmedFolder))
            {
                Directory.CreateDirectory(StemmedFolder);
            }

            foreach (var file in Directory.GetFiles(ResultsFolder))
            {
                Console.WriteLine($"Processing {file}");
                var text = File.ReadAllText(file);
                var text1 = Regex.Replace(text, new string(Splitters), " ");
                var sb = new StringBuilder(text1);
                sb = sb.ReplaceAll(Splitters.Select(x => x.ToString()));
                var wordsByWhiteSpace = sb.ToString();
                
                var factory = new RankedLanguageIdentifierFactory();
                var identifier = factory.Load(@"C:\repos\search-tasks\Task2\Core14.profile.xml");
                var languages = identifier.Identify(wordsByWhiteSpace);
                var mostCertainLanguage = languages.FirstOrDefault();
                var langCode = mostCertainLanguage?.Item1.Iso639_3;
                Console.WriteLine($"Lang code: {langCode}");
                IStemmer stemmer = langCode switch
                {
                    "eng" => new EnglishStemmer(),
                    "rus" => new RussianStemmer(),
                    _ => throw new NotSupportedException()
                };
                var stemmedFile = file.Replace(ResultsFolder, StemmedFolder);
                if (File.Exists(stemmedFile)) 
                    File.Delete(stemmedFile);

                using var stemmedFileWriter = File.AppendText(stemmedFile);
                foreach (var word in wordsByWhiteSpace.Split(" ").Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var stemmed = stemmer.Stem(word);
                    stemmedFileWriter.WriteLine(stemmed);
                    // Console.WriteLine($"{word} -> {stemmed}");
                }    
            }
        }
    }
}