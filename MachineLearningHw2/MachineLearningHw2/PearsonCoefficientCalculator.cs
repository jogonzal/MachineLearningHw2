﻿using System;
using System.Collections.Generic;

namespace MachineLearningHw2
{
	public class PearsonCoefficientCalculator
	{
		private readonly UserCache _userCache;

		public PearsonCoefficientCalculator(UserCache userCache)
		{
			_userCache = userCache;
		}

		public UserCache GetUserCache()
		{
			return _userCache;
		}

		public double Calculate(int userId1, int userId2)
		{
			// Find the two users ratings and average ratings
			float userId1AverageRating = _userCache.CalculateMeanRatingForUser(userId1);
			IReadOnlyDictionary<int, float> userId1Ratings = _userCache.GetUserMovieRatings(userId1);
			float userId2AverageRating = _userCache.CalculateMeanRatingForUser(userId2);
			IReadOnlyDictionary<int, float> userId2Ratings = _userCache.GetUserMovieRatings(userId2);

			int moviesInCommon = 0;

			// For every movie the two users have in common, calculate numerator and denominator
			double numerator = 0;
			double denominator = 0;
			foreach (var userId1Rating in userId1Ratings)
			{
				float userId2Rating;
				if (!userId2Ratings.TryGetValue(userId1Rating.Key, out userId2Rating))
				{
					// This rating won't be counted, as userId2 doesn't have it
					continue;
				}

				double diffForUser1 = (userId1Rating.Value - userId1AverageRating);
				double diffForUser2 = (userId2Rating - userId2AverageRating);

				double localNumerator = diffForUser1 * diffForUser2;
				double localDenominator = localNumerator * localNumerator;

				numerator += localNumerator;
				denominator += localDenominator;

				moviesInCommon++;
			}

			if (denominator == 0)
			{
				// 0 can happen if one user always rates the same. In that case, there is no weight to give, as
				// there is no differentiation
				return 0;
			}

			double result = numerator / Math.Sqrt(denominator);

			if (double.IsInfinity(result) || double.IsNaN(result))
			{
				throw new InvalidOperationException();
			}

			return result;
		}
	}
}
