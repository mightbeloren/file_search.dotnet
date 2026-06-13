using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Sqlite;
using indexer.Models;
using System.Linq;
using System.IO;
namespace indexer;

public class Worker : BackgroundService
{
		private readonly ILogger<Worker> _logger;
		private readonly IConfiguration _configuration;
		private readonly indexer.Models.IndexerContext _context;
		private readonly string? rootDirectory;
		private readonly string[] allowedExtensionsArray;

		public Worker(ILogger<Worker> logger,IConfiguration configuration)
		{
				_logger = logger;
				_configuration=configuration;
				var contextOptions=new DbContextOptionsBuilder<IndexerContext>() .UseSqlite(@"Datasource = app.db") .Options;;
				_context=new IndexerContext(contextOptions);

				rootDirectory=_configuration.GetValue<string>("IndexOptions:RootDirectory");
				string? allowedExtensions=_configuration.GetValue<string>("IndexOptions:AllowedExtensions");
				if(string.IsNullOrEmpty(rootDirectory)){
						_logger.LogWarning("Root Directory is not set in appsettings.json");
						Environment.Exit(1);
				}
				//check if the root directory actually exists or not
				if(!Directory.Exists(rootDirectory)){
						_logger.LogWarning("Root Directory does not exist");
						Environment.Exit(1);
				}

				if(string.IsNullOrEmpty(allowedExtensions)){
						_logger.LogWarning("Allowed Extenions is not set in appsettings.json");
						Environment.Exit(1);
				}

				allowedExtensionsArray=allowedExtensions.Split(',');
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
				//runs only once cause its a manually triggered indexer we dont wanna run this indefinitely in the background 
				if (_logger.IsEnabled(LogLevel.Information))
				{
						_logger.LogInformation("Indexer running at: {time}", DateTimeOffset.Now);
						await IndexAsync();
						_logger.LogInformation("Indexer finished");
				}
		}


		//anyway this is where the indexer function is, i dont wanna make complex files just in the worker service the entire logic so we can also access logger
		public async Task IndexAsync()
		{
				// first try to access the configuration, i dont wanna use IOptions that requires me creating seperate class and configure it at startup for small project
				try
				{
						if(!string.IsNullOrEmpty(rootDirectory))
						{
								await IndexDirectoriesAsync(rootDirectory);
						}

				}
				catch(Exception ex)
				{
						_logger.LogError(ex,"Error at IndexAsync()");
				}
		}

		//recursive method we just call it from IndexAsync by passing RootDirectory it will index files first and then call itself by passing directories 
		public async Task IndexDirectoriesAsync(string DirectoryPath)
		{
				//first lets enumerable files in this directory
				//then we enumarate individual directories
				_logger.LogInformation("Walking Directory : {Directorypath}",DirectoryPath);
				var files = Directory.EnumerateFiles(DirectoryPath).Where(file=>allowedExtensionsArray.Any(file.ToLower().EndsWith));
				foreach (var file in files)
				{
						//skip files that are not in the allowed extension
						_logger.LogInformation("File : {filePath}",file);

						//skip files that are already indexed

						var document = _context.Documents.Where(d=>d.FilePath==file).FirstOrDefault();
						if(document != null)
						{
								DateTime lastModifiedTime=System.IO.File.GetLastWriteTime(file);
								DateTime lastIndexedTime = document.IndexedTime;
								//if the last modified time is older than last indexed time we dont need to reindex it
								if(lastModifiedTime<lastIndexedTime)
								{
										_logger.LogInformation("File already indexed");
										continue;
								}
								//if it is modified after last indexing, we have to delete the document row and its word frequencies 
								else
								{
										_logger.LogInformation("File has been modified since last index, reindexing it");
										//get the word freuqncies of that document
										var wordFrequenciesOfDocument=_context.WordFrequencies.Where(wf=>wf.DocumentId==document.Id);
										//remove range
										_context.WordFrequencies.RemoveRange(wordFrequenciesOfDocument);
										//remove the document
										_context.Documents.Remove(document);
										//save
										await _context.SaveChangesAsync();

								}
						}

						//document word frequency dictionary
						Dictionary<string,int> wordFrequencies=new();
						//read entire file content to the string
						string content;
						using(StreamReader reader=new StreamReader(file))
						{
								content=await reader.ReadToEndAsync();
						}
						//now lets lowercase the content then split by spaces and store the words in the dictinoary with their no of frequencies
						content = content.ToLower();
						content=content.Replace("\n","");
						string[] words=content.Split(' ');
						int totalWordsCount=words.Length;
						foreach(string word_raw in words)
						{
								string word=word_raw.Trim();
								if(string.IsNullOrWhiteSpace(word))
								{
										continue;
								}
								//check if that word already exists in the dictionary if yes get it and increment its count then update it if not add it with count 1
								if(wordFrequencies.TryGetValue(word,out int count))
								{
										wordFrequencies[word]=count+1;
								}
								// word does not exist in the dictionary so Add it with initial count 1
								else
								{
										wordFrequencies[word]=1;
								}
						}
						//after processing the buffer we will loop through each word and create the word if does not exist in the Word table 
						foreach(KeyValuePair<string,int> kvPair in wordFrequencies)
						{
								//check if that word exists
								if(!_context.Words.Any(w=>w.WordString==kvPair.Key))
								{
										//if not insert that word
										var word=new indexer.Models.Word
										{
												WordString=kvPair.Key
										};
										_context.Words.Add(word);
										await _context.SaveChangesAsync();
								}
						}

						//then we will create document record and word frequencies record for all words with their count
						var newDocument=new indexer.Models.Document
						{
								FileName=System.IO.Path.GetFileName(file),
								FilePath=file,
								IndexedTime=DateTime.Now,
								TotalWordsCount=totalWordsCount
						};
						_context.Documents.Add(newDocument);
						int result = await _context.SaveChangesAsync();
						if(result>0)
						{
								_logger.LogInformation("Document created");
								foreach(KeyValuePair<string,int> kvPair in wordFrequencies)
								{
										var wordItemFromContext=_context.Words.Where(w=>w.WordString==kvPair.Key).FirstOrDefault();
										if(wordItemFromContext != null)
										{
												var newWordFrequency=new indexer.Models.WordFrequency
												{
														DocumentId=newDocument.Id,
														WordId=wordItemFromContext.Id,
														Count=kvPair.Value
												};
												_context.WordFrequencies.Add(newWordFrequency);
										}
								}
								int finalResult=await _context.SaveChangesAsync();
								if(finalResult>0)
								{
										_logger.LogInformation("Document indexed");
								}
						}
				}
				var directories=System.IO.Directory.GetDirectories(DirectoryPath);
				foreach(var directory in directories)
				{
						await IndexDirectoriesAsync(directory);
				}
		}
}
