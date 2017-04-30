using System;
using System.Collections.Generic;

namespace MachineLearningHw2.ErrorCalculation
{
	public static class RootMeanSquareError
	{
		public static double Calculate(List<MoviePrediction> predictions)
		{
			double numerator = 0;
			foreach (var moviePrediction in predictions)
			{
				double diff = moviePrediction.RealRating - moviePrediction.Prediction;
				numerator += diff*diff;
			}

			return Math.Sqrt(numerator / predictions.Count);
		}
	}
}
