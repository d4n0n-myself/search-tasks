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

        private static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                throw new ArgumentException("1 argument required");
            }

            var uri = new Uri(args[0]);

            while (true)
            {
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

                if (content.Any())
                {
                    var htmlDocument = new HtmlDocument();
                    htmlDocument.LoadHtml(content); 
                    var value = htmlDocument.DocumentNode.SelectNodes("//text()").Select(x => x.InnerText.Split())
                        .SelectMany(x => x.Select(y => y.Trim('\n', '\t', ':', '.', ',', ' ', '[', ']', '-', '"')))
                        .Where(x => x is not null && x.Any());
                    var wordCount = value.Count();
                    if (wordCount < 1000)
                        continue;
                    List.Add((uri, content));
                }

                var links = GetNewLinks(uri, content);

                foreach (var link in links)
                {
                    PagesToGet.Enqueue(link);
                }

                // streamResponse.Close();
                // streamReader.Close();
                // myWebResponse.Close();
                uri = null;
            }

            Console.WriteLine("Writing results to disk");
            Directory.CreateDirectory("results");

            var lines = new List<string>();

            for (var i = 0; i < List.Count; i++)
            {
                lines.Add($"{i} {List[i].Item1}");
            }

            File.WriteAllLines("results/index.txt", lines);

            for (var i = 0; i < List.Count; i++)
            {
                var (key, value) = List[i];
                try
                {
                    Console.WriteLine($"Writing {key}");
                    File.WriteAllText($"results/{i}.txt", value);
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
                    if (!temp.IsAbsoluteUri)
                    {
                        uri = new Uri(baseUrl.GetLeftPart(UriPartial.Authority) + temp);
                    }
                    else
                    {
                        uri = temp;
                    }
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