using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using MachineLearningHw2.ErrorCalculation;
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

			Console.WriteLine("Parsing the input files...");
			//List<Movie> movieTitles = CsvParserUtils.ParseCsvAsList<Movie>(MovieTitlesPath);
			UserCache trainingSetCache = UserCache.BuildUserCache(UserRatingsTrainingPath);
			UserCache testingSetCache = UserCache.BuildUserCache(UserRatingsTestPath);

			Console.WriteLine("Initializing predictors...");
			PearsonCoefficientCalculator pearsonCalculator = new PearsonCoefficientCalculator(trainingSetCache);
			MovieScorePredictor predictor = new MovieScorePredictor(pearsonCalculator);

			// Get the list of users to make predictions on
			IReadOnlyDictionary<int, UserCache.UserRatingsCache> listOfUsersToPredictScoresOn = testingSetCache.GetAllUsersAndMovieRatings();

			Console.WriteLine("Making predictions...");
			// Predict ratings for all the users in parallel
			List<MoviePrediction> predictions = listOfUsersToPredictScoresOn.AsParallel().Select(l =>
			{
				// Make the prediction for this users movies
				var returnValue = predictor.PredictAllScores(l.Key, l.Value.GetMovieRatings());

				// This is simply to update the console on the current progress
				l.Value.Predicted = true;
				int predicted = listOfUsersToPredictScoresOn.Values.Count(n => n.Predicted);
				Console.Write("\r{0}/{1}", predicted, listOfUsersToPredictScoresOn.Count);

				// Return the prediction
				return returnValue;
			}).SelectMany(s => s.Values).ToList();

			Console.WriteLine(Environment.NewLine);

			Console.WriteLine("Calculating errors...");
			var rootMeanSquareError = RootMeanSquareError.Calculate(predictions);
			var meanAbsoluteError = MeanAbsoluteError.Calculate(predictions);

			Console.WriteLine("=========================================");
			Console.WriteLine("Root mean square error: {0}", rootMeanSquareError);
			Console.WriteLine("Mean absolute error: {0}", meanAbsoluteError);

			Console.WriteLine("Press any key to quit...");
			Console.ReadKey();
		}

		// NOTE: Only used for testing
		private void RunLocalTests(MovieScorePredictor predictor)
		{
			List<UserRating> testingUserRatings = CsvParserUtils.ParseCsvAsList<UserRating>(UserRatingsTestPath);
			double prediction = predictor.PredictScore(testingUserRatings[0].UserId, testingUserRatings[0].MovieId);

			UserCache testingSetCache = UserCache.BuildUserCache(UserRatingsTestPath);

			var allScores = predictor.PredictAllScores(testingUserRatings[0].UserId,
				testingSetCache.GetUserMovieRatings(testingUserRatings[0].UserId));

			var allErrors = new List<double>();
			foreach (var keyValuePair in allScores)
			{
				var realScore = testingSetCache.GetUserMovieRatings(testingUserRatings[0].UserId)[keyValuePair.Key];
				double error = Math.Abs(keyValuePair.Value.Prediction - realScore);
				allErrors.Add(error);
			}
		}
	}
}
