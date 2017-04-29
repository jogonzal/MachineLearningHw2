using System.Collections.Generic;
using System.IO;
using System.Linq;

using CsvHelper;
using CsvHelper.Configuration;

namespace MachineLearningHw2.Parsing
{
	public static class CsvParserUtils
	{
		public static List<T> ParseCsvAsList<T>(string path) where T : class
		{
			using (TextReader textReader = File.OpenText(path))
			{
				var csv = new CsvReader(textReader, new CsvConfiguration()
				{
					HasHeaderRecord = false
				});
				List<T> records = csv.GetRecords<T>().ToList();
				return records;
			}
		}
	}
}
