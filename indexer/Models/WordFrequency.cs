using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
namespace indexer.Models;

public class WordFrequency
{
		[Key]
		public int Id {get;set;} 

		//foreign reference to Document class/entity
		public int DocumentId {get;set;}
		[ForeignKey(nameof(DocumentId))]
		public Document Document {get;set;}=null!;

		//foreign reference to Word class/entity
		public int WordId {get;set;}
		[ForeignKey(nameof(WordId))]
		public Word Word {get;set;}=null!;

		//no of times the word appeared in the document
		public int Count {get;set;}

}
