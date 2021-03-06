﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace SDTM_v1
{
	class SDTM_v1
	{
		private int[] numTopics_arr;
		private int numTopics_Max;
		private const int numLevels = 3;
		private int numConvs;
		private int numUniqueWords;

		private Hashtable voca_dic_number = null;
        private ArrayList vocaList = null;
		private ArrayList convsList = null;
		private ArrayList[] seed_words_list_level_arr = null;

		private double alpha;
		private double[] sumAlpha;
		private double[] betas;  // betas[3]: Common Words, seed words for this level, seed words for other levels
		private double[] sumBeta;
		private double gamma;
		private double sumGamma;

		private int[,,] matrixLTW;  // Level x Topic x Words matrix, n^l_{kv}
		private int[,] sumLTW;      // Marginalized sum over word v of matrixLTW
		
		private double[,] probTable;    // Probability table for sampling multinomial distribution

		private string input_dir_path;
		private string output_dir_path;

		private const string conv_filename = "BagOfConversations.txt";
		private const string voca_dic_filename = "WordList.txt";
		private string[] seed_words_level_filename = null;
		
		private const int numProbWords = 50;
		private Random rnd;

		public SDTM_v1(ref int[] Topics, double alpha, ref double[] betas, double gamma, string input_dir_path, string output_dir_path)
		{
			this.seed_words_level_filename = new[] { "SeedWords_1.txt", "SeedWords_2.txt", "SeedWords_3.txt" };
			this.rnd = new Random();

			this.numTopics_arr = Topics;
			this.numTopics_Max = numTopics_arr.Max();

			this.alpha = alpha;
			this.betas = betas;
			this.gamma = gamma;

			this.input_dir_path = input_dir_path;
			this.output_dir_path = output_dir_path;
						
			// Read conversations from file
			Read_conversations(input_dir_path + conv_filename);

			// Read vocaburary
			Read_voca_dic(input_dir_path + voca_dic_filename);

			// Read Seed words
			Read_seed_words();

			// Initialize variables
			init();
		}


		/*
			Read conversations from file
		 */
		private void Read_conversations(string target_file_path)
		{
			this.convsList = new ArrayList();
			int numberofTweets = 0;
			int word_count_a_tweet = 0;
			string[] line_arr = null;
			SDTM_v1_Conversation one_conv = null;
			SDTM_v1_Tweet one_tweet = null;
			SDTM_v1_Word one_word = null;
			Dictionary<SDTM_v1_Word, int> word_count = null;
			string one_word_count = null;
			string[] one_word_count_arr = null;

			int conv_idx = 0;
			
			try
			{
				using (StreamReader sr = new StreamReader(target_file_path))
				{
					string line = null;

					while ((line = sr.ReadLine()) != null)
					{
						// Conversation name, userid1_userid2_convid
						one_conv = new SDTM_v1_Conversation(conv_idx);
						conv_idx++;
						line_arr = line.Split('_');
						one_conv.set_users(Convert.ToInt32(line_arr[0]), Convert.ToInt32(line_arr[1]));

						// number of tweets in the conversation
						line = sr.ReadLine();
						numberofTweets = Convert.ToInt32(line);

						// Each tweet in a conversation
						for (int tweet_idx = 0; tweet_idx < numberofTweets; tweet_idx++)
						{
                            // Line format is
							// user_id	lambda_0	lambda_1	numberofuniquewords	BagofWordsFormat
							line = sr.ReadLine();
							line_arr = line.Split(' ');
							
							one_tweet = new SDTM_v1_Tweet();
							one_tweet.set_tweet_id(Convert.ToInt32(line_arr[0]));
							one_tweet.set_max_ent_prob(Convert.ToDouble(line_arr[1]), Convert.ToDouble(line_arr[2]));

							word_count = new Dictionary<SDTM_v1_Word, int>();
                            //word_count_a_tweet = Convert.ToInt32(line_arr[3]) + 4;  // At the end of line_arr
                            word_count_a_tweet = line_arr.Length;

							for (int word_idx = 4; word_idx < word_count_a_tweet; word_idx++)
							{
								one_word_count = line_arr[word_idx];
								one_word_count_arr = one_word_count.Split(':');
								one_word = new SDTM_v1_Word(Convert.ToInt32(one_word_count_arr[0]));
								word_count.Add(one_word, Convert.ToInt32(one_word_count_arr[1]));
							}

							one_tweet.set_word_count(word_count);

							// insert tweet to conversation
							one_conv.insert_tweet(one_tweet);
						}

						// insert conversation to list
						this.convsList.Add(one_conv);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Environment.Exit(1);
			}

			this.numConvs = this.convsList.Count;
		}


		/*
			Read conversations from file
		 */
		private void Read_voca_dic(string target_file_path)
		{		
			this.voca_dic_number = new Hashtable();
            this.vocaList = new ArrayList();
			int voca_idx = 0;

			try
			{
				using (StreamReader sr = new StreamReader(target_file_path))
				{
					string line = null;

					while ((line = sr.ReadLine()) != null)
					{
                        this.vocaList.Add(line);
						this.voca_dic_number.Add(line, voca_idx);
						voca_idx++;
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
				Environment.Exit(1);
			}

			this.numUniqueWords = this.voca_dic_number.Count;
		}

		/*
			Read seed words from file
		 */
		private void Read_seed_words()
		{
			this.seed_words_list_level_arr = new ArrayList[SDTM_v1.numLevels];
			int target_seedword_idx = 0;

			for(int level_idx = 0 ; level_idx < SDTM_v1.numLevels ; level_idx++)
			{
				this.seed_words_list_level_arr[level_idx] = new ArrayList();

				try
				{
					using (StreamReader sr = new StreamReader(this.input_dir_path + this.seed_words_level_filename[level_idx]))
					{
						string line = null;

						while ((line = sr.ReadLine()) != null)
						{
                            try
                            {
                                target_seedword_idx = (int)this.voca_dic_number[line];
                            }
                            catch (Exception e)
                            {

                            }
							this.seed_words_list_level_arr[level_idx].Add(target_seedword_idx);
						}
					}
				}
				catch (Exception e)
				{
					Console.WriteLine(e);
					Environment.Exit(1);
				}
			}
		}

		/*
		 Initialize variables
		 */
		private void init()
		{
			this.probTable = new double[this.numTopics_Max, SDTM_v1.numLevels];

			this.sumAlpha = new double[SDTM_v1.numLevels];
			this.sumAlpha[0] = this.alpha * this.numTopics_arr[0];
			this.sumAlpha[1] = this.alpha * this.numTopics_arr[1];
			this.sumAlpha[2] = this.alpha * this.numTopics_arr[2];
			this.sumGamma = this.gamma * SDTM_v1.numLevels;
			this.sumBeta = new double[SDTM_v1.numLevels];
			int numSeedWords = 0;
			foreach(ArrayList one_list in seed_words_list_level_arr)
			{
				numSeedWords += one_list.Count;
			}
			double sumBetaCommon = this.betas[0] * ((double) (this.numUniqueWords - numSeedWords));
			double numTargetLevelWords = 0;
			for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
			{
				numTargetLevelWords = ((double) this.seed_words_list_level_arr[level_idx].Count);

				this.sumBeta[level_idx] = sumBetaCommon 
					+ this.betas[1] * numTargetLevelWords 
					+ this.betas[2] * ((double) (numSeedWords - numTargetLevelWords));
			}

			this.sumLTW = new int[SDTM_v1.numLevels, this.numTopics_Max];
			this.matrixLTW = new int[SDTM_v1.numLevels, this.numTopics_Max, this.numUniqueWords];

			int newLevel = 0;
			int newTopic = 0;

            // Assign initial level and topic index for each tweet
			foreach (SDTM_v1_Conversation one_conv in this.convsList)
			{
				one_conv.CLT = new int[SDTM_v1.numLevels, this.numTopics_Max];
				one_conv.sumCLT = new int[SDTM_v1.numLevels];

				// Assign each value to variable
				foreach (SDTM_v1_Tweet one_tweet in one_conv.tweet_list)
				{
					// Find whether seed words are existed in one_tweet or not
					newLevel = is_exist_seed_words_a_tweet(one_tweet);
					newTopic = this.rnd.Next(this.numTopics_arr[newLevel]);

					// Assign it
					one_tweet.set_sd_level(newLevel);
					one_tweet.set_topic(newTopic);

					foreach (KeyValuePair<SDTM_v1_Word, int> one_entry in one_tweet.word_count_table)
					{
						this.matrixLTW[newLevel, newTopic, one_entry.Key.wordidx]++;
					}
					this.sumLTW[newLevel, newTopic] += one_tweet.word_count_table.Count;

                    one_conv.CLT[newLevel, newTopic]++;
					one_conv.sumCLT[newLevel]++;
				}
			}

			Console.WriteLine("Finish initialize variables");
		}

		/*
			Gibbs sampling
		 */
		public void GibbsSampling(int numIterations)
		{
			// http://msdn.microsoft.com/en-us/library/system.diagnostics.stopwatch.elapsed%28v=vs.110%29.aspx
			Stopwatch stopWatch;
			TimeSpan ts;
			TimeSpan ts_remain;

			for (int iter_idx = 0; iter_idx < numIterations; iter_idx++)
			{
				stopWatch = new Stopwatch();
				stopWatch.Start();

				// Each conversation
				foreach (SDTM_v1_Conversation one_conv in this.convsList)
				{
					GibbsSampling_Each_conv(one_conv);
				}

				stopWatch.Stop();
				ts = stopWatch.Elapsed;
				ts_remain = new TimeSpan(ts.Ticks * ((long)(numIterations - iter_idx - 1)));

				Console.WriteLine("Iteration " + iter_idx + " tooks " + ts.TotalSeconds + "seconds. Estimated remaining time: "
					+ String.Format("{0:00}:{1:00}:{2:00}", ts_remain.Hours, ts_remain.Minutes, ts_remain.Seconds));
			}
		}


		private void GibbsSampling_Each_conv(SDTM_v1_Conversation one_conv)
		{
			// Preparing
			int oldLevel = 0;
			int oldTopic = 0;
			int newLevel = 0;
			int newTopic = 0;
			int numTopics_target_level = 0;
			double prob_part_senti_value = 0.0;
			double target_sumBeta = 0.0;
			double beta0, m0, expectLTW, beta, betaw;
			double prob_table_temp = 0.0;
			double sumProb = 0.0;

			// Each tweet
			foreach (SDTM_v1_Tweet one_tweet in one_conv.tweet_list)
			{
				sumProb = 0.0;

				// Decrease current one_tweet value
				oldLevel = one_tweet.sd_level;
				oldTopic = one_tweet.topic;

				foreach (KeyValuePair<SDTM_v1_Word, int> one_entry in one_tweet.word_count_table)
				{
					this.matrixLTW[oldLevel, oldTopic, one_entry.Key.wordidx]--;
				}
				this.sumLTW[oldLevel, oldTopic] -= one_tweet.word_count_table.Count;

				one_conv.CLT[oldLevel, oldTopic]--;
				one_conv.sumCLT[oldLevel]--;

				// Fill probability table				
				// Level 0
				numTopics_target_level = numTopics_arr[0];
				prob_part_senti_value = one_tweet.max_ent_prob[0] / (one_conv.sumCLT[0] + this.sumAlpha[0]);
				target_sumBeta = this.sumBeta[0];
				
				for (int ti = 0; ti < numTopics_target_level; ti++) 
				{
					beta0 = this.sumLTW[0, ti] + target_sumBeta;
					m0 = 0;
					expectLTW = 1.0;

					foreach (KeyValuePair<SDTM_v1_Word, int> one_entry in one_tweet.word_count_table)
					{						
						if (-1 == one_entry.Key.seed_word_level)
						{
							beta = this.betas[0];
						}
						else
						{
							beta = this.betas[2];
						}

						betaw = this.matrixLTW[0, ti, one_entry.Key.wordidx] + beta;
						
						for (int m = 0; m < (int) one_entry.Value; m++) 
						{
							expectLTW *= (betaw + m) / (beta0 + m0);
							m0++;
						}
					}

					prob_table_temp = (one_conv.CLT[0, ti] + this.alpha)
							* prob_part_senti_value
							* expectLTW;
					
					sumProb += prob_table_temp;
					this.probTable[ti, 0] = prob_table_temp;
				}

				// Level 1 and 2
				for (int level_idx = 1; level_idx < SDTM_v1.numLevels; level_idx++)
				{
					numTopics_target_level = numTopics_arr[level_idx];
					prob_part_senti_value = one_tweet.max_ent_prob[1] / (one_conv.sumCLT[level_idx] + this.sumAlpha[level_idx]);
					target_sumBeta = this.sumBeta[level_idx];

					for (int ti = 0; ti < numTopics_target_level; ti++)
					{
						beta0 = this.sumLTW[level_idx, ti] + target_sumBeta;
						m0 = 0;
						expectLTW = 1.0;

						foreach (KeyValuePair<SDTM_v1_Word, int> one_entry in one_tweet.word_count_table)
						{
							if (-1 == one_entry.Key.seed_word_level)
							{
								beta = this.betas[0];
							}
							else if (level_idx == one_entry.Key.seed_word_level)
							{
								beta = this.betas[1];
							}
							else
							{
								beta = this.betas[2];
							}

							betaw = this.matrixLTW[level_idx, ti, one_entry.Key.wordidx] + beta;

							for (int m = 0; m < (int)one_entry.Value; m++)
							{
								expectLTW *= (betaw + m) / (beta0 + m0);
								m0++;
							}
						}

						prob_table_temp = (one_conv.CLT[level_idx, ti] + this.alpha)
								* prob_part_senti_value
								* expectLTW;

						sumProb += prob_table_temp;
						this.probTable[ti, level_idx] = prob_table_temp;
					}
				}

				// Multinomial sampling 
				Multinomial_sampling(sumProb, out newLevel, out newTopic);
				
				// Assign and increase with new value
				one_tweet.set_sd_level(newLevel);
				one_tweet.set_topic(newTopic);

				foreach (KeyValuePair<SDTM_v1_Word, int> one_entry in one_tweet.word_count_table)
				{
					this.matrixLTW[newLevel, newTopic, one_entry.Key.wordidx]++;
				}
				this.sumLTW[newLevel, newTopic] += one_tweet.word_count_table.Count;
                
				one_conv.CLT[newLevel, newTopic]++;
				one_conv.sumCLT[newLevel]++;
			}
		}

		/*
			Check existence of seed words in a tweet
		 */
		private int is_exist_seed_words_a_tweet(SDTM_v1_Tweet one_tweet)
		{
			int target_level_idx = 0;

			foreach (KeyValuePair<SDTM_v1_Word, int> one_entry in one_tweet.word_count_table)
			{
				for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
				{
					if (this.seed_words_list_level_arr[level_idx].Contains(one_entry.Key.wordidx))
					{
						target_level_idx = level_idx;
						one_entry.Key.seed_word_level = level_idx;
					}
				}
			}

			// No seed words in a tweet
			return target_level_idx;
		}


		/*
			Multinomial sampling
			http://en.wikipedia.org/wiki/Multinomial_distribution#Sampling_from_a_multinomial_distribution
		 */
		private void Multinomial_sampling(double sumProb, out int newLevel, out int newTopic)
		{
			double random_value = this.rnd.NextDouble() * sumProb;
			double temp_sampling_value = 0.0;
			for (int topic_idx = 0; topic_idx < this.numTopics_Max; topic_idx++)
			{
				for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
				{
					temp_sampling_value += this.probTable[topic_idx, level_idx];
					if (temp_sampling_value >= random_value)
					{
						newLevel = level_idx;
						newTopic = topic_idx;
                        return;
					}
				}
			}

			// Never happened
			Console.WriteLine("Weird in Multinomial sampling function");
			newLevel = SDTM_v1.numLevels;
			newTopic = this.numTopics_arr[newLevel];
		}


		/*
		 Print model output result
		 */
		public void PrintOutputtoFiles(int numGibbsIters)
		{
            // Check existence of output directory
            // Create directory
            // http://msdn.microsoft.com/en-us/library/54a0at6s.aspx
            System.IO.Directory.CreateDirectory(this.output_dir_path);

			String filename_prefix = String.Format("SDTM_T-{0}-{1}-{2}_A-{3}_B-{4}-{5}-{6}_G-{7}_I-{8}",
				this.numTopics_arr[0], this.numTopics_arr[1], this.numTopics_arr[2],
				this.alpha, this.betas[0], this.betas[1], this.betas[2],
				this.gamma, numGibbsIters);

			// Phi
			// Compute Phi
            double[][][] phi = null;
            Compute_Phi(out phi);

            try
            {
                for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
                {
                    using (StreamWriter sw_out = new StreamWriter(this.output_dir_path + filename_prefix + "_Phi_L" + level_idx + ".csv"))
                    {
                        for (int word_idx = 0; word_idx < this.numUniqueWords; word_idx++)
                        {
                            for (int topic_idx = 0; topic_idx < this.numTopics_Max; topic_idx++)
                            {
                                sw_out.Write(phi[level_idx][topic_idx][word_idx] + ",");
                            }
                            sw_out.WriteLine("");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            // Most probable words of each topic
            try
            {
                double[] target_level_topic_phi_vec = null;

                for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
                {
                    int[][] top_word_topic = new int[this.numTopics_Max][];

                    // Get vector of phi by level and topic
                    // http://stackoverflow.com/questions/1760185/c-sharp-sort-list-while-also-returning-the-original-index-positions
                    for (int topic_idx = 0; topic_idx < this.numTopics_Max; topic_idx++)
                    {
                        target_level_topic_phi_vec = phi[level_idx][topic_idx];

                        var sorting_value_reserve_index = target_level_topic_phi_vec.Select((x, i) => new KeyValuePair<double, int>(x, i)).OrderByDescending(x => x.Key).ToList();

                        top_word_topic[topic_idx] = sorting_value_reserve_index.Select(x => x.Value).ToArray();
                    }

                    using (StreamWriter sw_out = new StreamWriter(this.output_dir_path + filename_prefix + "_RankWords_L" + level_idx + ".csv"))
                    {
                        for (int rank_word_idx = 0; rank_word_idx < SDTM_v1.numProbWords; rank_word_idx++)
                        {
                            for (int topic_idx = 0; topic_idx < this.numTopics_Max; topic_idx++)
                            {
                                sw_out.Write(this.vocaList[top_word_topic[topic_idx][rank_word_idx]] + ",");
                            }
                            sw_out.WriteLine("");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

			// Theta
			// Compute Theta
			try
			{
				using (StreamWriter sw_out = new StreamWriter(this.output_dir_path + filename_prefix + "_Theta.csv"))
				{
					double denom = 0;

					foreach (SDTM_v1_Conversation one_conv in this.convsList)
					{
						for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
						{
							denom = one_conv.sumCLT[level_idx] + this.sumAlpha[level_idx];
							for (int topic_idx = 0; topic_idx < this.numTopics_arr[level_idx]; topic_idx++)
							{
								sw_out.Write(
									((one_conv.CLT[level_idx, topic_idx] + this.alpha) / denom)
									+ ","
									);
							}
						}
                        sw_out.WriteLine("");
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}

			// Most probable words


			// Number of assigned SD level to conversation tweets
			try
			{
				using (StreamWriter sw_out = new StreamWriter(this.output_dir_path + filename_prefix + "_SDL_Tweet.csv"))
				{
					foreach (SDTM_v1_Conversation one_conv in this.convsList)
					{
						int[] SD_level_count = new int[SDTM_v1.numLevels];
						foreach (SDTM_v1_Tweet one_tweet in one_conv.tweet_list)
						{
							SD_level_count[one_tweet.sd_level]++;
						}
						
						sw_out.WriteLine(
							String.Join(",", new List<int>(SD_level_count).ConvertAll(i => i.ToString()).ToArray())
							);
					}					
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e);
			}
		}


		/*
			Compute Phi
		 */
        private void Compute_Phi(out double[][][] phi)
		{
            phi = new double[SDTM_v1.numLevels][][];

            double temp_val = 0.0;
            double target_beta = 0.0;
            int target_word_seed_word_or_not = 0;

            // Seed words or not
            int[] seed_words_or_not = Enumerable.Repeat(-1, this.numUniqueWords).ToArray();

            for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
            {
                foreach (int seed_word_idx in this.seed_words_list_level_arr[level_idx])
                {
                    seed_words_or_not[seed_word_idx] = level_idx;
                }
            }

            // Compute each value in Phi
            for (int level_idx = 0; level_idx < SDTM_v1.numLevels; level_idx++)
            {
                phi[level_idx] = new double[this.numTopics_Max][];

                for (int topic_idx = 0; topic_idx < this.numTopics_Max; topic_idx++)
                {
                    phi[level_idx][topic_idx] = new double[this.numUniqueWords];
                }


                for (int word_idx = 0; word_idx < this.numUniqueWords; word_idx++)
                {
                    target_word_seed_word_or_not = seed_words_or_not[word_idx];
                    if (-1 == target_word_seed_word_or_not)
                    {
                        // Common word
                        target_beta = this.betas[0];
                    }
                    else if (level_idx == target_word_seed_word_or_not)
                    {
                        // Target level's seed word
                        target_beta = this.betas[1];
                    }
                    else
                    {
                        // Other level's seed word
                        target_beta = this.betas[2];
                    }

                    for (int topic_idx = 0; topic_idx < this.numTopics_Max; topic_idx++)
                    {
                        phi[level_idx][topic_idx][word_idx] = 
                            (this.matrixLTW[level_idx, topic_idx, word_idx] + target_beta) 
                            / (this.sumLTW[level_idx, topic_idx] + this.sumBeta[level_idx]);
                    }
                }
            }
		}

		
	}
}
