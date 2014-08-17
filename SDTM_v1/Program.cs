using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

/*
 * Author: JinYeong Bak (jy.bak@kaist.ac.kr)
 * Program: Self-disclosure topic model
 */

namespace SDTM_v1
{
	// Define a class to receive parsed values
	// http://commandline.codeplex.com/
	class Options
	{
		[Option('i', "iterations", Required = true,
		  HelpText = "The number of Gibbs sampling iteration.")]
		public int numIterations { get; set; }

		[Option('t', "threads", Required = false, DefaultValue = 1,
		  HelpText = "The number of threads.")]
		public int numThreads { get; set; }

		[Option('d', "inputDir", Required = true,
		  HelpText = "Input directory")]
		public string inputDir { get; set; }

		[Option('o', "outputDir", Required = true,
		  HelpText = "Output directory")]
		public string outputDir { get; set; }

		[Option('a', "alpha", Required = false, DefaultValue = 0.1,
		  HelpText = "Alpha value")]
		public double alpha { get; set; }

		[Option('g', "gamma", Required = false, DefaultValue = 0.1,
		  HelpText = "Gamma value")]
		public double gamma { get; set; }

        [OptionList('b', "betas", Required = true, Separator = ':',
		  HelpText = "Beta values, Common Words:Seed words for that level:Seed words for other levels")]
		public IList<string> betas { get; set; }

		[OptionList('k', "Topics", Required = true, Separator = ':',
		  HelpText = "The number of topics for each level")]
		public IList<string> topics { get; set; }

		[ParserState]
		public IParserState LastParserState { get; set; }

		[HelpOption]
		public string GetUsage()
		{
			return HelpText.AutoBuild(this,
			  (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
		}
	}

	class Program
	{
		static int Main(string[] args)
		{
			#if DEBUG
			if (0 == args.Length)
			{
                args = new[] { "-i", "1000", "-d", "./input_data", "-o", "./output_data", "-b", "0.01:2.0:0.000001", "-k", "6:4:4" };
			}
			#endif

			var options = new Options();
			
			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				string input_dir_path = null;
				string output_dir_path = null;
                if (options.inputDir.EndsWith("/") || options.inputDir.EndsWith("\\"))
				{
					input_dir_path = options.inputDir;
				}
				else
				{
					input_dir_path = options.inputDir + "/";
				}
                if (options.outputDir.EndsWith("/") || options.outputDir.EndsWith("\\"))
				{
					output_dir_path = options.outputDir;
				}
				else
				{
                    output_dir_path = options.outputDir + "/";
				}

				int[] topic_arr = new int[options.topics.Count];
				for(int idx = 0 ; idx < options.topics.Count ; idx++)
				{
					topic_arr[idx] = Convert.ToInt32(options.topics[idx]);
				}
				double[] beta_arr = new double[options.betas.Count];
				for(int idx = 0 ; idx < options.betas.Count ; idx++)
				{
					beta_arr[idx] = Convert.ToDouble(options.betas[idx]);
				}
				

				// Make SDTM instance
				SDTM_v1 SDTM_instance = new SDTM_v1(ref topic_arr, options.alpha, ref beta_arr, options.gamma, input_dir_path, output_dir_path);

				// Run SDTM
				SDTM_instance.GibbsSampling(options.numIterations);

				// Print output
				SDTM_instance.PrintOutputtoFiles(options.numIterations);

			}
			else 
			{
				Console.WriteLine("Error during parsing arguments");
                Console.ReadLine(); // Prevent closing console automatically
			}

			return 0;
		}
	}
}
