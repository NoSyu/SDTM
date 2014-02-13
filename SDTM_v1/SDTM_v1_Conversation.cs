using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDTM_v1
{
	class SDTM_v1_Conversation
	{
		public int conv_idx;
		public int user1_id;
		public int user2_id;

		public int[,] CLT;
		public int[] sumCLT;

		public int[] vectorCL;

		public ArrayList tweet_list;

		public SDTM_v1_Conversation(int conv_idx)
		{
			this.conv_idx = conv_idx;
			this.tweet_list = new ArrayList();
		}

		public void set_users(int user1_id, int user2_id)
		{
			this.user1_id = user1_id;
			this.user2_id = user2_id;
		}

		public void insert_tweet(SDTM_v1_Tweet one_tweet)
		{
			this.tweet_list.Add(one_tweet);
		}
	}
}
