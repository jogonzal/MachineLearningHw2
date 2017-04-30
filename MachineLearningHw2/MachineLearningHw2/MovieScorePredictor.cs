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

		public class WeightAndContribution
		{
			public WeightAndContribution(double weight, double contribution)
			{
				Weight = weight;
				Contribution = contribution;
			}

			public double Weight { get; private set; }
			public double Contribution { get; private set; }
		}

		private class PearsonCache
		{
			public List<WeightAndContribution> WeightAndContributions { get; private set; }

			public PearsonCache(double weight, double contribution)
			{
				WeightAndContributions = new List<WeightAndContribution>()
				{
					new WeightAndContribution(weight, contribution)
				};
			}

			public void Update(double weight, double contribution)
			{
				WeightAndContributions.Add(new WeightAndContribution(weight, contribution));
			}

			public double GetTop(int k)
			{
				var selected = WeightAndContributions.OrderByDescending(n => n.Weight).Take(k);

				double sumOfWeights = 0;
				double accumulatedContributions = 0;
				foreach (var weightAndContribution in selected)
				{
					sumOfWeights += weightAndContribution.Weight;
					accumulatedContributions += weightAndContribution.Contribution;
				}

				double result = (1/sumOfWeights)*(accumulatedContributions);

				return result;
			}
		}

		public IReadOnlyDictionary<int, MoviePrediction> PredictAllScores(int userId, IReadOnlyDictionary<int, float> moviesWithRealScore, int k)
		{
			var userCache = _pearsonCalculator.GetUserCache();

			IList<int> moviesWithoutScore;

			if (moviesWithRealScore == null)
			{
				// if no movie was specified, we'll make a prediction for all movies
				moviesWithoutScore = userCache.MovieIds.ToList();
			}
			else
			{
				moviesWithoutScore = moviesWithRealScore.Keys.ToList();
			}

			// Find all users that rated the movie
			var usersThatRatedCommonMovies = userCache.GetAllUsersAndMovieRatings();

			Dictionary<int, PearsonCache> dict = new Dictionary<int, PearsonCache>(moviesWithoutScore.Count);
			foreach (var userThatRatedMovie in usersThatRatedCommonMovies)
			{
				double weight = _pearsonCalculator.Calculate(userId, userThatRatedMovie.Key);

				foreach (var movieId in moviesWithoutScore)
				{
					float movieRating;
					if (!userThatRatedMovie.Value.GetMovieRatings().TryGetValue(movieId, out movieRating))
					{
						continue;
					}

					double localResult = weight * (movieRating - userThatRatedMovie.Value.GetAverageRating());

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

			// Calculate predictions and error
			var resultDict = new Dictionary<int, MoviePrediction>();
			foreach (var pearsonCache in dict)
			{
				double prediction = averageRatingForUser;

				// Some users get 0 weight sum, which means... we can't do a better job at estimating than its pure average rating
				if (pearsonCache.Value.WeightAndContributions.Count >= 0)
				{
					prediction += pearsonCache.Value.GetTop(k);
				}
				else
				{
					Console.WriteLine("Warning: Could not find references in training data for predicting user {0} in movie {1}", userId, pearsonCache.Key);
				}

				if (double.IsNaN(prediction))
				{
					throw new InvalidOperationException();
				}

				resultDict.Add(pearsonCache.Key, new MoviePrediction()
				{
					Prediction = prediction,
					RealRating = moviesWithRealScore?[pearsonCache.Key] ?? 0
				});
			}

			return resultDict;
		}
	}
}
