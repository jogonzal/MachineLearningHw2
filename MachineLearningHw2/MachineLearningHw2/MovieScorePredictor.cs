using System;
using System.Collections.Generic;
using System.Linq;

namespace MachineLearningHw2
{
	public class MoviePrediction
	{
		public double RealRating { get; set; }
		public double Prediction { get; set; }
	}

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
			IEnumerable<KeyValuePair<int, UserCache.UserRatingsCache>> usersThatRatedMovie =
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

			public PearsonCache(double weight, double accumulated)
			{
				SumOfWeights = weight;
				Accumulated = accumulated;
			}

			public void Update(double weight, double accumulated)
			{
				SumOfWeights += weight;
				Accumulated += accumulated;
			}
		}

		public IReadOnlyDictionary<int, MoviePrediction> PredictAllScores(int userId, IReadOnlyDictionary<int, float> movies)
		{
			var userCache = _pearsonCalculator.GetUserCache();

			// Find all users that rated the movie
			var usersThatRatedCommonMovies = userCache.GetAllUsersAndMovieRatings();

			Dictionary<int, PearsonCache> dict = new Dictionary<int, PearsonCache>(movies.Count);
			foreach (var userThatRatedMovie in usersThatRatedCommonMovies)
			{
				double weight = _pearsonCalculator.Calculate(userId, userThatRatedMovie.Key);

				foreach (var movieId in movies)
				{
					float movieRating;
					if (!userThatRatedMovie.Value.GetMovieRatings().TryGetValue(movieId.Key, out movieRating))
					{
						continue;
					}

					double localResult = weight * (movieRating - userThatRatedMovie.Value.GetAverageRating());

					PearsonCache pearsonCache;
					if (!dict.TryGetValue(movieId.Key, out pearsonCache))
					{
						dict.Add(movieId.Key, new PearsonCache(weight, localResult));
					}
					else
					{
						pearsonCache.Update(weight, localResult);
					}
				}
			}

			float averageRatingForUser = userCache.CalculateMeanRatingForUser(userId);

			// Calculate predictions and error
			var resultDict = new Dictionary<int, MoviePrediction>();
			foreach (var pearsonCache in dict)
			{
				double prediction = averageRatingForUser;

				// Some users get 0 weight sum, which means... we can't do a better job at estimating than its pure average rating
				if (pearsonCache.Value.SumOfWeights != 0)
				{
					prediction += (1/pearsonCache.Value.SumOfWeights)*(pearsonCache.Value.Accumulated);
				}
				else
				{
					Console.WriteLine("Could not find references in training data for predicting user {0} in movie {1}", userId, pearsonCache.Key);
				}

				if (double.IsNaN(prediction))
				{
					throw new InvalidOperationException();
				}

				resultDict.Add(pearsonCache.Key, new MoviePrediction()
				{
					Prediction = prediction,
					RealRating = movies[pearsonCache.Key]
				});
			}

			return resultDict;
		}
	}
}
