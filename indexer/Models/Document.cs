using System.ComponentModel.DataAnnotations;
namespace indexer.Models;

public class Document
{
		[Key]
		public int Id {get;set;}
		[Required]
		public string FileName {get;set;}=string.Empty;
		[Required]
		public string FilePath {get;set;}=string.Empty;
		[Required]
		public DateTime IndexedTime {get;set;}
		[Required]
		public int TotalWordsCount {get;set;}

		public ICollection<WordFrequency> WordFrequencies {get;set;}=new List<WordFrequency>();
}
