using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
namespace indexer.Models;

public class IndexerContext:DbContext
{
		public IndexerContext(DbContextOptions<IndexerContext> options):base(options){ }
		public DbSet<Document> Documents {get;set;}
		public DbSet<Word> Words {get;set;}
		public DbSet<WordFrequency> WordFrequencies {get;set;}
}
