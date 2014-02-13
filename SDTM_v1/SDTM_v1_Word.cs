using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SDTM_v1
{
	class SDTM_v1_Word
	{
		public int wordidx;
		public int seed_word_level;

		public SDTM_v1_Word(int word_No)
		{
			this.wordidx = word_No;
			this.seed_word_level = -1;
		}

		public SDTM_v1_Word(int word_No, int seed_word_level)
		{
			this.wordidx = word_No;
			this.seed_word_level = seed_word_level;
		}

		public void set_seed_word_level(int seed_word_level)
		{
			this.seed_word_level = seed_word_level;
		}
	}
}
