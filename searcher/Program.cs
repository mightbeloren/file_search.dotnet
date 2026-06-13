using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using indexer.Models;
namespace searcher;

class Program
{
		static void Main(string[] args)
		{
				var contextOptions=new DbContextOptionsBuilder<IndexerContext>() .UseSqlite(@"Datasource = app.db") .Options;;
				IndexerContext _context=new IndexerContext(contextOptions);

				if(args.Length<1)
				{
						Console.WriteLine("usage : searcher \"search query\"");
				}
				string query=args[0];
				//lower the query and split by spaces
				var keywords = query
						.ToLower()
						.Split(' ', StringSplitOptions.RemoveEmptyEntries)
						.Select(x => x.Trim())
						.ToList();
				//get word ids from Words table
				var wordIds = _context.Words
						.Where(w => keywords.Contains(w.WordString))
						.Select(w => w.Id)
						.ToList();
				//get frequencies of each word
				var freq = _context.WordFrequencies
						.Where(wf => wordIds.Contains(wf.WordId))
						.ToList();
				//group the frequencies by document Id
				var docGroups = freq.GroupBy(f=>f.DocumentId);
				//get documents
				var docs = _context.Documents.ToDictionary(d => d.Id);
				//calculate results
				var results = docGroups.Select(g =>
								{
								double score = 0;

								var doc = docs[g.Key];

								foreach (var wf in g)
								{
								double tf = (double)wf.Count / doc.TotalWordsCount;

								double df = _context.WordFrequencies
								.Where(x => x.WordId == wf.WordId)
								.Select(x => x.DocumentId)
								.Distinct()
								.Count();

								double idf = Math.Log((double)docs.Count / df);

								score += tf * idf;
								}

								return new
								{
										DocumentId = g.Key,
										Score = score
								};
								});
				var ranked = results.OrderByDescending(x => x.Score).ToList();
				foreach(var rank in ranked)
				{
						var doc=docs[rank.DocumentId];
						Console.WriteLine($"File : {doc.FileName} {doc.FilePath}, Rank : {rank.Score}");
				}
		}
}
