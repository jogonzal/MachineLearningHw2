using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MachineLearningHw2.Netflix;
using MachineLearningHw2.Parsing;

namespace MachineLearningHw2
{
	public class Program
	{
		private static string MovieTitlesPath => Path.Combine(Directory.GetCurrentDirectory() + @"\..\..\..\..\netflix_data\movie_titles.txt");
		private static string UserRatingsTrainingPath => Path.Combine(Directory.GetCurrentDirectory() + @"\..\..\..\..\netflix_data\trainingRatings.txt");
		private static string UserRatingsTestPath => Path.Combine(Directory.GetCurrentDirectory() + @"\..\..\..\..\netflix_data\testingRatings.txt");

		static void Main(string[] args)
		{
			string errorMessage = "";
			if (!File.Exists(MovieTitlesPath))
			{
				errorMessage += $"Failed to find file ${MovieTitlesPath} - please update variable ${nameof(MovieTitlesPath)} or create that file.\n";
			}
			if (!File.Exists(UserRatingsTrainingPath))
			{
				errorMessage += $"Failed to find file ${UserRatingsTrainingPath} - please update variable ${nameof(UserRatingsTrainingPath)} or create that file.\n";
			}
			if (!File.Exists(UserRatingsTestPath))
			{
				errorMessage += $"Failed to find file ${UserRatingsTestPath} - please update variable ${nameof(UserRatingsTestPath)} or create that file.\n";
			}

			if (errorMessage != "")
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Not all files available - not running!");
				Console.WriteLine(errorMessage);
				Console.ResetColor();
				Console.WriteLine("Press any key to continue...");
				Console.ReadKey();
				return;
			}

			List<Movie> movieTitles = CsvParserUtils.ParseCsvAsList<Movie>(MovieTitlesPath);
			List<UserRating> testingUserRatings = CsvParserUtils.ParseCsvAsList<UserRating>(UserRatingsTestPath);

			UserCache trainingSetCache = UserCache.BuildUserCache(UserRatingsTrainingPath);
			PearsonCoefficientCalculator pearsonCalculator = new PearsonCoefficientCalculator(trainingSetCache);
			MovieScorePredictor predictor = new MovieScorePredictor(pearsonCalculator);

			double prediction = predictor.PredictScore(testingUserRatings[0].UserId, testingUserRatings[0].MovieId);

			UserCache testingSetCache = UserCache.BuildUserCache(UserRatingsTestPath);

			var allScores = predictor.PredictAllScores(testingUserRatings[0].UserId,
				testingSetCache.GetUserMovieRatings(testingUserRatings[0].UserId).Keys.ToList());

			var allErrors = new List<double>();
			foreach (var keyValuePair in allScores)
			{
				var realScore = testingSetCache.GetUserMovieRatings(testingUserRatings[0].UserId)[keyValuePair.Key];
				double error = Math.Abs(keyValuePair.Value - realScore);
				allErrors.Add(error);
			}

			Console.WriteLine("Press any key to quit...");
			Console.ReadKey();
		}
	}
}
