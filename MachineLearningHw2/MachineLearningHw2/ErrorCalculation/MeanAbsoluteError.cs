using System;
using System.Collections.Generic;

namespace MachineLearningHw2.ErrorCalculation
{
	public static class MeanAbsoluteError
	{
		public static double Calculate(List<MoviePrediction> predictions)
		{
			double numerator = 0;
			foreach (var moviePrediction in predictions)
			{
				double diff = moviePrediction.RealRating - moviePrediction.Prediction;
				numerator += Math.Abs(diff);
			}

			return numerator / predictions.Count;
		}
	}
}
