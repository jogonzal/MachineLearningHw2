using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MachineLearningHw2.Netflix
{
	public class UserRating
	{
		public int MovieId { get; set; }
		public int UserId { get; set; }
		public float Rating { get; set; }
	}
}
