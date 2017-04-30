using System.Collections.Generic;
using MachineLearningHw2.Netflix;

namespace MachineLearningHw2
{
	public class MeanVoteCalculator
	{
		private IDictionary<int, float> MeanVoteCache { get; set; }

		private class UserRatingAccumulator
		{
			private int _ratingCount;
			private float _ratingsAccumulated;

			public UserRatingAccumulator(float rating)
			{
				_ratingsAccumulated = rating;
				_ratingCount = 1;
			}

			public void AddRating(float rating)
			{
				_ratingCount++;
				_ratingsAccumulated += rating;
			}

			public float GetAverageRatings()
			{
				return _ratingsAccumulated/_ratingCount;
			}
		}

		public MeanVoteCalculator(List<UserRating> userRatings)
		{
			// Do all the mean vote calculations right now
			var dict = new Dictionary<int, UserRatingAccumulator>(); // Store accumulated vote counts and ratings
			foreach (var userRating in userRatings)
			{
				UserRatingAccumulator ratingAccumulator;
				if (dict.TryGetValue(userRating.UserId, out ratingAccumulator))
				{
					ratingAccumulator.AddRating(userRating.Rating);
				}
				else
				{
					dict.Add(userRating.UserId, new UserRatingAccumulator(userRating.Rating));
				}
			}

			MeanVoteCache = new Dictionary<int, float>(dict.Count);
			foreach (KeyValuePair<int, UserRatingAccumulator> keyValuePair in dict)
			{
				MeanVoteCache.Add(keyValuePair.Key, keyValuePair.Value.GetAverageRatings());
			}
		}

		public float CalculateMeanRatingForUser(int userId)
		{
			return MeanVoteCache[userId];
		}
	}
}
