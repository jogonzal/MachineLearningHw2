using System.Collections.Generic;
using System.Linq;

namespace MachineLearningHw2
{
	public class MovieScorePredictor
	{
		private readonly PearsonCoefficientCalculator _pearsonCalculator;

		public MovieScorePredictor(PearsonCoefficientCalculator pearsonCalculator)
		{
			_pearsonCalculator = pearsonCalculator;
		}

		public double PredictScore(int userId, int movieId)
		{
			var userCache = _pearsonCalculator.GetUserCache();

			// Find all users that rated the movie
			var usersThatRatedMovie =
				userCache.GetAllUsersAndMovieRatings().Where(k => k.Value.GetMovieRatings().ContainsKey(movieId));

			double sumOfWeights = 0;
			double accumulated = 0;
			foreach (var userThatRatedMovie in usersThatRatedMovie)
			{
				double weight = _pearsonCalculator.Calculate(userId, userThatRatedMovie.Key);

				sumOfWeights += weight;

				double localResult = weight * (userThatRatedMovie.Value.GetMovieRatings()[movieId] - userThatRatedMovie.Value.GetAverageRating());
				accumulated += localResult;
			}

			float averageRatingForUser = userCache.CalculateMeanRatingForUser(userId);
			double prediction = averageRatingForUser + (1/sumOfWeights)*(accumulated);

			return prediction;
		}

		private class PearsonCache
		{
			public double SumOfWeights { get; private set; }
			public double Accumulated { get; private set; }

			public PearsonCache(double sumOfWeights, double accumulated)
			{
				SumOfWeights = sumOfWeights;
				Accumulated = accumulated;
			}

			public void Update(double weight, double accumulated)
			{
				SumOfWeights += weight;
				Accumulated += accumulated;
			}
		}

		public IReadOnlyDictionary<int, double> PredictAllScores(int userId, IReadOnlyList<int> movieIds)
		{
			var userCache = _pearsonCalculator.GetUserCache();

			// Find all users that rated the movie
			var usersThatRatedCommonMovies = userCache.GetAllUsersAndMovieRatings();

			var dict = new Dictionary<int, PearsonCache>(movieIds.Count);
			foreach (var userThatRatedMovie in usersThatRatedCommonMovies)
			{
				double weight = _pearsonCalculator.Calculate(userId, userThatRatedMovie.Key);

				foreach (var movieId in movieIds)
				{
					float movieRating;
					if (!userThatRatedMovie.Value.GetMovieRatings().TryGetValue(movieId, out movieRating))
					{
						continue;
					}

					double localResult = weight * (userThatRatedMovie.Value.GetMovieRatings()[movieId] - userThatRatedMovie.Value.GetAverageRating());

					PearsonCache pearsonCache;
					if (!dict.TryGetValue(movieId, out pearsonCache))
					{
						dict.Add(movieId, new PearsonCache(weight, localResult));
					}
					else
					{
						pearsonCache.Update(weight, localResult);
					}
				}
			}

			float averageRatingForUser = userCache.CalculateMeanRatingForUser(userId);

			// Calculate predictions
			var resultDict = new Dictionary<int, double>();
			foreach (var pearsonCache in dict)
			{
				double prediction = averageRatingForUser + (1 / pearsonCache.Value.SumOfWeights) * (pearsonCache.Value.Accumulated);
				resultDict.Add(pearsonCache.Key, prediction);
			}

			return resultDict;
		}
	}
}
