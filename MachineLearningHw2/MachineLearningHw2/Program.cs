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

			var startTime = DateTime.Now;
			Console.WriteLine(startTime);
			Console.WriteLine("Parsing the input files...");
			Dictionary<int, string> movieTitles = CsvParserUtils.ParseCsvAsList<Movie>(MovieTitlesPath).ToDictionary(n => n.MovieId, n => n.Title);
			UserCache trainingSetCache = UserCache.BuildUserCache(UserRatingsTrainingPath);
			UserCache testingSetCache = UserCache.BuildUserCache(UserRatingsTestPath);

			Console.WriteLine("Initializing predictors...");
			PearsonCoefficientCalculator pearsonCalculator = new PearsonCoefficientCalculator(trainingSetCache);
			MovieScorePredictor predictor = new MovieScorePredictor(pearsonCalculator);

			// NOTE: feel free to comment out any of the lines below depending on what you want to execute
			MakePredictionsOnTestFileAndCalculateError(predictor, testingSetCache);
			MakeMovieRecommendationsForUser999999InTrainingSet(predictor, trainingSetCache, movieTitles);

			var endTime = DateTime.Now;
			Console.WriteLine(endTime);
			var totalMinutes = (endTime - startTime).TotalMinutes;
			Console.WriteLine("Took {0} minutes.", totalMinutes);
			Console.WriteLine("Press any key to quit...");
			Console.ReadKey();
		}

		private static void MakePredictionsOnTestFileAndCalculateError(MovieScorePredictor predictor, UserCache testingSetCache)
		{
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
		}

		private static void MakeMovieRecommendationsForUser999999InTrainingSet(MovieScorePredictor predictor, UserCache trainingSetCache, IDictionary<int, string> movieTitles)
		{
			// Make recommendations
			Console.WriteLine("Making movie recommendations...");

			if (!trainingSetCache.GetAllUsersAndMovieRatings().ContainsKey(999999))
			{
				Console.WriteLine("User 999999 must be present in traning set to be able to make recommendations. Please add your ratings as that user");
				return;
			}

			// To make recommendations, we will predict all movies for this user. Once we do that prediction, we will obtain the
			// movies with the top score, and display them here
			IReadOnlyDictionary<int, MoviePrediction> movieScores = predictor.PredictAllScores(999999, null);
			var bestPredictionsFiltered = movieScores.Where(n => !trainingSetCache.GetUserMovieRatings(999999).ContainsKey(n.Key));
			var bestPredictions = bestPredictionsFiltered.Select(n => new
			{
				MovieId = n.Key,
				MovieScore = n.Value.Prediction
			}).OrderByDescending(n => n.MovieScore).Take(10).ToList();
			Console.WriteLine("For user {0}, our best predictions are...");
			for (int index = 0; index < bestPredictions.Count; index++)
			{
				var bestPrediction = bestPredictions[index];
				Console.WriteLine("{0}: Id:'{1}', {2}", index, bestPrediction.MovieId, movieTitles[bestPrediction.MovieId]);
			}
			Console.WriteLine();
		}

		// For recommendations, the following movies were used
		//17622, 999999, 5.0
		//10362, 999999, 5.0
		//5448, 999999, 5.0
		//14, 999999, 4.0
		//21, 999999, 1.0
		//69, 999999, 2.0
		//68, 999999, 4.0
		//209, 999999, 1.0
		//215, 999999, 5.0
		//731, 999999, 1.0
		//6205, 999999, 5.0
		//12293, 999999, 5.0
		//12298, 999999, 1.0
		//12311, 999999, 1.0
		//12954, 999999, 1.0
		//11726, 999999, 5.0
		//13501, 999999, 5.0
		//13595, 999999, 1.0
		//13617, 999999, 1.0
		//13638, 999999, 2.0
		//7624, 999999, 4.0

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
