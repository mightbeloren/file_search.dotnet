using System.ComponentModel.DataAnnotations;
namespace indexer.Models;

public class Word
{
		[Key]
		public int Id {get;set;}
		[Required]
		public string WordString {get;set;}=string.Empty;
}
