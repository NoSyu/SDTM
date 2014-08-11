using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDTM_v1
{
	class SDTM_v1_Tweet
	{
		public Dictionary<SDTM_v1_Word, int> word_count_table;

		public int topic;
		public int sd_level;
		public int tweet_id;

		public double[] max_ent_prob;   // lambda_0, lambda_1

		public SDTM_v1_Tweet()
		{

		}

		public void set_word_count(Dictionary<SDTM_v1_Word, int> word_count)
		{
			this.word_count_table = word_count;
		}

		public void set_tweet_id(int tweet_idx)
		{
			this.tweet_id = tweet_idx;
		}

		public void set_max_ent_prob(double max_ent_prob_0, double max_ent_prob_1)
		{
			this.max_ent_prob = new double[2];
			this.max_ent_prob[0] = max_ent_prob_0;
			this.max_ent_prob[1] = max_ent_prob_1;
		}

		public void set_topic(int topic_idx)
		{
			this.topic = topic_idx;
		}

		public void set_sd_level(int level_idx)
		{
			this.sd_level = level_idx;
		}
	}
}
