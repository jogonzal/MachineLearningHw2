using System.Collections.Generic;

using MachineLearningHw2.Netflix;
using MachineLearningHw2.Parsing;

namespace MachineLearningHw2
{
	public class UserCache
	{
		private readonly Dictionary<int, UserRatingsCache> _userRatingsCache;
		public HashSet<int> MovieIds { get; private set; }

		public class UserRatingsCache
		{
			private int _ratingCount;
			private float _ratingsAccumulated;

			private readonly Dictionary<int, float> _movieRatings;

			public UserRatingsCache(float rating, int movieId)
			{
				_movieRatings = new Dictionary<int, float>();
				_ratingCount = 0;
				_ratingsAccumulated = 0;
				AddRating(rating, movieId);
			}

			public bool Predicted { get; set; }

			public void AddRating(float rating, int movieId)
			{
				_ratingCount++;
				_ratingsAccumulated += rating;
				_movieRatings.Add(movieId, rating);
			}

			public float GetAverageRating()
			{
				return _ratingsAccumulated/_ratingCount;
			}

			public IReadOnlyDictionary<int, float> GetMovieRatings()
			{
				return _movieRatings;
			}
		}

		public UserCache(List<UserRating> userRatings)
		{
			MovieIds = new HashSet<int>();

			// Do all the mean vote calculations right now
			_userRatingsCache = new Dictionary<int, UserRatingsCache>(); // Store accumulated vote counts and ratings
			foreach (UserRating userRating in userRatings)
			{
				MovieIds.Add(userRating.MovieId);

				UserRatingsCache ratingsCache;
				if (_userRatingsCache.TryGetValue(userRating.UserId, out ratingsCache))
				{
					ratingsCache.AddRating(userRating.Rating, userRating.MovieId);
				}
				else
				{
					_userRatingsCache.Add(userRating.UserId, new UserRatingsCache(userRating.Rating, userRating.MovieId));
				}
			}
		}

		public float CalculateMeanRatingForUser(int userId)
		{
			return _userRatingsCache[userId].GetAverageRating();
		}

		public IReadOnlyDictionary<int, float> GetUserMovieRatings(int userId)
		{
			return _userRatingsCache[userId].GetMovieRatings();
		}

		public static UserCache BuildUserCache(string path)
		{
			List<UserRating> trainingUserRatings = CsvParserUtils.ParseCsvAsList<UserRating>(path);
			return new UserCache(trainingUserRatings);
		}

		public IReadOnlyDictionary<int, UserRatingsCache> GetAllUsersAndMovieRatings()
		{
			return _userRatingsCache;
		}
	}
}
