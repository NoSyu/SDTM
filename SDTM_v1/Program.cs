using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

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

		[OptionList('b', "betas", Required = false, Separator = ':',
		  HelpText = "Beta values, Common Words:Seed words for that level:Seed words for other levels")]
		public IList<string> betas { get; set; }

		[OptionList('k', "Topics", Required = false, Separator = ':',
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
				args = new[] { "-i", "2000", "-d", "./input", "-o", "./output", "-b", "0.01:2.0:0.000001", "-k", "60:40:40" };
			}
			#endif

			var options = new Options();

			if (CommandLine.Parser.Default.ParseArguments(args, options))
			{
				Console.WriteLine(options.numIterations);

				foreach (string one_ele in options.topics)
				{
					Console.WriteLine(one_ele);
				}
			}
			else 
			{
				Console.WriteLine("Error during parsing arguments");
			}

			return 0;
		}
	}
}
