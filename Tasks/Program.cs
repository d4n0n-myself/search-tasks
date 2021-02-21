using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace Tasks
{
    internal static class Program
    {
        private static readonly List<(Uri, string)> List = new();
        private static readonly Queue<Uri> PagesToGet = new();
        private const string ResultsFolder = "results";

        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("1 argument required");
            }

            var uri = new Uri(args[0]);
            var pageCounter = 0;
            while (true)
            {
                pageCounter++;
                if (List.Count >= 100)
                {
                    break;
                }

                if (uri == null)
                {
                    if (!PagesToGet.TryDequeue(out var newUri))
                    {
                        Console.WriteLine("cant get the page");
                    }

                    uri = newUri;
                }

                if (List.Any(x => x.Item1 == uri))
                {
                    uri = null;
                    continue;
                }

                var myWebRequest = WebRequest.Create(uri!);
                Console.WriteLine($"Reading {uri}");

                string content;
                try
                {
                    using var myWebResponse = myWebRequest.GetResponse();
                    using var streamResponse = myWebResponse.GetResponseStream();

                    if (streamResponse == null)
                    {
                        throw new ArgumentException("oops stream is null");
                    }

                    using var streamReader = new StreamReader(streamResponse);
                    content = streamReader.ReadToEnd();
                }
                catch (WebException e)
                {
                    Console.WriteLine($"Read failed: {e.Status}, skipping");
                    uri = null;
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    uri = null;
                    continue;
                }

                var links = GetNewLinks(uri, content);

                foreach (var link in links)
                {
                    PagesToGet.Enqueue(link);
                }

                if (content.Any())
                {
                    var splitters = new[]
                    {
                        '\n', '\t', ':', ';', '(', ')', '.', ',', ' ', '[', ']', '-', '"', '{', '}', '!', '?', '@', '$', '='
                    };
                    // var splitters = new[] {"\n", "\t", ":", ".", ",', ' ', '[', ']', '-', '"'};
                    // var htmlDocument = new HtmlDocument();
                    // htmlDocument.LoadHtml(content);
                    // var value = htmlDocument.DocumentNode
                    //     .SelectNodes("//text()")
                    //     .Select(x => x.InnerText.Split(splitters))
                    //     .SelectMany(x => x.Select(y => y.Trim(splitters)))
                    //     .Where(x => x is not null && x != string.Empty);
                    // var words = value as string[] ?? value.ToArray();
                    // var wordCount = words.Length;
                    // if (wordCount < 1000)
                    // {
                    //     uri = null;
                    //     continue;
                    // }

                    string pattern = @"<(.|\n)*?>";

                    var step1 = Regex.Replace(content, "<script.*?script>", " ", RegexOptions.Singleline);
                    var step2 = Regex.Replace(step1, "<style.*?style>", " ", RegexOptions.Singleline);
                    var step3 = Regex.Replace(step2, "&#.*?;", "");
                    var step4 = Regex.Replace(step3, "\n*", "\n");
                    // var step3 = step2
                    //     .Replace("&#160;", "")
                    //     .Replace("&#32;", "")
                    //     .Replace("&#91;", "")
                    //     .Replace("6&#93;", "");
                    var textOnly = Regex.Replace(step4, pattern, string.Empty);

                    var words = textOnly.Split(splitters)
                        .Where(x=> x != string.Empty && x.All(y => !char.IsDigit(y)))
                        .ToArray();
                    if (words.Length < 1000)
                    {
                        Console.WriteLine("Not enough words, skipping...");
                        uri = null;
                        continue;
                    }
                    List.Add((uri, textOnly));
                }

                uri = null;
            }

            Console.WriteLine($"Scanned {pageCounter} pages");
            Console.WriteLine("Writing results to disk");
            Directory.CreateDirectory(ResultsFolder);

            var lines = new List<string>();

            for (var i = 0; i < List.Count; i++)
            {
                lines.Add($"{i} {List[i].Item1}");
            }

            File.WriteAllLines($"{ResultsFolder}/../index.txt", lines);

            for (var i = 0; i < List.Count; i++)
            {
                var (key, value) = List[i];
                try
                {
                    Console.WriteLine($"Writing {key}");
                    File.WriteAllText(
                        $"{ResultsFolder}/{i}.txt",
                        value //Regex.Replace(value, "<*>", " ")
                    );
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        private static IEnumerable<Uri> GetNewLinks(Uri baseUrl, string content)
        {
            var regexLink = new Regex("(?<=<a\\s*?href=(?:'|\"))[^'\"]*?(?=(?:'|\"))");

            ISet<Uri> newLinks = new HashSet<Uri>();
            foreach (var match in regexLink.Matches(content))
            {
                var value = match.ToString();
                if (value == null)
                {
                    continue;
                }

                Uri uri = null;

                try
                {
                    var temp = new Uri(value, UriKind.RelativeOrAbsolute);
                    uri = !temp.IsAbsoluteUri
                        ? new Uri(baseUrl.GetLeftPart(UriPartial.Authority).TrimEnd('/') + '/' +
                                  temp.ToString().TrimStart('/'))
                        : temp;
                }
                catch
                {
                    // ignored
                }

                if (uri == null)
                    continue;

                if (!newLinks.Contains(uri))
                {
                    newLinks.Add(uri);
                }
            }

            return newLinks;
        }
    }
}