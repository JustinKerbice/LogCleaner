// KSP logfile cleaner
// Justin Kerbice 22/09/2014

// changelog:
//	30/09/2014 and before:	CLI parsing API removed (gnu.getopt, NDESK.options, they gives more troubles than anything !)
//							simple arguments parsing done instead
//							add many filters
//							change removal loop a bit, use continue statement to improve exec time
//							add statistics display at the end
//							add a dictionary to hold filter meaning and counters
//							add help message
//
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// options:
//	search for a given pattern (module or part name)
// highlight exception or warning
// remove some lines (ATM, model/tex loaded)

// standard marks:

//	Config(ACTIVE_TEXTURE_MANAGER) ActiveTextureManagement/config/ACTIVE_TEXTURE_MANAGER
//	Config(ACTIVE_TEXTURE_MANAGER_CONFIG)
//	Config(AGENT)   PART  PROP   RESOURCE_DEFINITION     EXPERIMENT_DEFINITION    STORY_DEF INTERNAL

namespace LogCleaner
{
	class MainClass
	{
		// these 3: removed (default)
		public const ushort EMPTYLINES = 0;
		public const ushort USELESSLINES = 1;
		public const ushort PARSINGLINES = 2;

		// BEWARE: counters index and these collide !

		// options
		public const ushort PLATFASSEMB = 0;
		// 1
		public const ushort LOADASSEMB = 1;
		// 2
		public const ushort LOADING = 2;
		// 4
		public const ushort ASSEMBLOADER = 3;
		// 8
		public const ushort NOPLATFASSEM = 4;
		// 16
		public const ushort ADDONLOADER = 5;
		// 32
		public const ushort LOADAUDIO = 6;
		// 64
		public const ushort LOADTEX = 7;
		// 128
		public const ushort LOADMODEL = 8;
		// 256
		public const ushort WHEELCOLL = 9;
		// 512
		public const ushort SCREENSHOT = 10;
		// 1024
		public const ushort CANTPLAYAS = 11;
		// 2048
		public const ushort ACTTEXMAN = 12;
		// 4096
		// 8192
		public const ushort RESDEFADDB = 14;
		// 16384
		public const ushort MODMANAGER = 15;
		// 32768

		static Dictionary<string, ushort> STATSKEYS = new Dictionary<string, ushort>()
		{
			{ "platformassembly" , PLATFASSEMB },
			{ "loadassembly", LOADASSEMB },
			{ "loading", LOADING },
			{ "assemblyloader", ASSEMBLOADER },
			{ "nonplatformassembly", NOPLATFASSEM },
			{ "addonloader", ADDONLOADER },
			{ "loadaudio", LOADAUDIO },
			{ "loadtex", LOADTEX },
			{ "loadmodel", LOADMODEL },
			{ "wheelcollider", WHEELCOLL },
			{ "screenshot", SCREENSHOT },
			{ "cannotplaydisabledaudiosource", CANTPLAYAS },
			{ "Active Tex Manager", ACTTEXMAN },
			{ "resource def added", RESDEFADDB },
			{ "modulemanager", MODMANAGER },

		};

		public static void Main (string[] args)
		{
			string inFile = ""; //@"d:\output_log.txt";
			string outFile = ""; //@"d:\tmp.txt";
			int remove_mask = 0;
			//bool print_usage = false;

			BitArray bitmask;

			const string version = "1.0";
			const string release = "30/09/2014";

			const string emptyline = "^\\s*$";
			const string uselessline = "\\(Filename:\\s+.*Line:\\s+-{0,1}\\d+\\)";
			const string parsingwdca = "^Parsing\\s+\\(bool|float|string|int\\)";
			// beware: there is also (Filename: line: -1)
			const string platassemb = "^Platform\\s+assembly:";
			const string loadassem = "^Load\\(Assembly\\):";
			const string loading = "^Loading\\s+";
			const string assembloader = "^AssemblyLoader:";
			const string noplaassemb = "^Non platform assembly:";
			const string addonloader = "^AddonLoader:";
			const string loadaudio = "^Load\\(Audio\\):";
			const string loadtex = "^Load\\(Texture\\):";
			const string loadmodel = "^Load\\(Model\\):";
			const string wheelcoll = "WheelCollider requires an attached Rigidbody to function";
			const string screenshot = "^SCREENSHOT\\!\\!";
			const string cantplaydisaudiosrc = "^Can not play a disabled audio source";
			const string acttexman = "^ActiveTextureManagement:";

			const string resdefadb = "Resource RESOURCE_DEFINITION added to database";
			const string modmanager = "\\[ModuleManager\\]";

			int lines_read = 0;
			int line_written = 0;
			int[] COUNTERS_BASE = new int[3];
			int[] COUNTERS = new int[20];

			Console.WriteLine ("LogCleaner " + version + ", a Kerbal Space Program logfile cleaner.\nJustin Kerbice " + release + "\n");

			if (args != null && args.Length >= 1) 
			{
				if (args [0] != null && args [0] != "" && File.Exists (args [0])) {
						inFile = args [0];
				} else {
					Console.WriteLine ("File [" + args [0] + "] does not exists !");
					Environment.Exit (2);
				}

				if (args.Length > 1 && args [1] != null && args [1] != "" && args [1] != "-") {
					if (File.Exists (args [1])) {
						Console.WriteLine ("Warning output file [" + args [1] + "] already exists");
					}

					outFile = args [1];
				} else {
					outFile = Path.GetDirectoryName (args [0]) + "\\" + Path.GetFileNameWithoutExtension (args [0]) + "_clean.txt";
					Console.WriteLine ("output file name = [" + outFile + "]");
					//Console.WriteLine ("output file not specified, abort.");
					//Environment.Exit (3);
				}

				if (args.Length > 2 && args [2] != null && args [2] != "") {

					if (args [2] == "ALL") {
						remove_mask = Int32.MaxValue;
					}

					try
					{
						remove_mask = Int32.Parse (args [2]);
					}
					catch (Exception exception_caught) {
						Console.WriteLine ("Invalid mask provided, use the full mask. Exception was: " + exception_caught.Message);
						remove_mask = Int32.MaxValue;
					}
					Console.WriteLine ("RM=[" + remove_mask.ToString () + "]");
					bitmask = new BitArray (new int[] { remove_mask });

					// ALL = 1111111111 = ? -1 !

					//tmp display
					Console.Write ("bitmask=[");
					for (int i = 0; i < bitmask.Count; i++) {
						bool bit = bitmask.Get (i);
						Console.Write (bit ? 1 : 0);
					}
					Console.WriteLine ("]");

					if (bitmask.Get (LOADAUDIO) == true) {
						Console.WriteLine ("LOADAUDIO msg disabled");
					}
				} else {
					Console.WriteLine ("No filter prodived, use default.");
				}
			} else {
				Console.WriteLine ("Usage: LogCleaner <infile> [<outfile>] [filter mask]\n\tinfile\t\tthe file to clean\n\toutfile\t\tthe output file, default will be infile_clean.extension\n\tfilter mask\tthe line to remove from file.");
				Environment.Exit (1);
			}

			Console.WriteLine ("Processing " + inFile + "... outfile=[" + outFile + "]");

			bitmask = new BitArray (new int[] { remove_mask });

			// + check for size/readable, writable path, ~disk space left
			using (StreamReader infile = File.OpenText (inFile)) {
				using (StreamWriter outfile = new StreamWriter (outFile)) {

					string a_line = "";
					while ((a_line = infile.ReadLine ()) != null) {
						lines_read++;

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, emptyline)) {
							COUNTERS_BASE [EMPTYLINES]++;
							continue;
						}

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, uselessline)) {
							COUNTERS_BASE [USELESSLINES]++;
							continue;
						}

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, parsingwdca)) {
							COUNTERS_BASE [PARSINGLINES]++;
							continue;
						}

						if (bitmask.Get (PLATFASSEMB) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, platassemb)) {
							COUNTERS [PLATFASSEMB]++;
							continue;
						}

						if (bitmask.Get (LOADASSEMB) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, loadassem)) {
							COUNTERS [LOADASSEMB]++;
							continue;
						}

						if (bitmask.Get (LOADING) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, loading)) {
							COUNTERS [LOADING]++;
							continue;
						}

						if (bitmask.Get (ASSEMBLOADER) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, assembloader)) {
							COUNTERS [ASSEMBLOADER]++;
							continue;
						}

						if (bitmask.Get (NOPLATFASSEM) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, noplaassemb)) {
							COUNTERS [NOPLATFASSEM]++;
							continue;
						}

						if (bitmask.Get (ADDONLOADER) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, addonloader)) {
							COUNTERS [ADDONLOADER]++;
							continue;
						}

						if (bitmask.Get (LOADAUDIO) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, loadaudio)) {
							COUNTERS [LOADAUDIO]++;
							continue;
						}

						if (bitmask.Get (LOADTEX) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, loadtex)) {
							COUNTERS [LOADTEX]++;
							continue;
						}

						if (bitmask.Get (LOADMODEL) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, loadmodel)) {
							COUNTERS [LOADMODEL]++;
							continue;
						}

						if (bitmask.Get (WHEELCOLL) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, wheelcoll)) {
							COUNTERS [WHEELCOLL]++;
							continue;
						}

						if (bitmask.Get (SCREENSHOT) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, screenshot)) {
							COUNTERS [SCREENSHOT]++;
							continue;
						}

						if (bitmask.Get (CANTPLAYAS) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, cantplaydisaudiosrc)) {
							COUNTERS [CANTPLAYAS]++;
							continue;
						}

						if (bitmask.Get (ACTTEXMAN) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, acttexman)) {
							COUNTERS [ACTTEXMAN]++;
							continue;
						}

						if (bitmask.Get (RESDEFADDB) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, resdefadb)) {
							COUNTERS [RESDEFADDB]++;
							continue;
						}

						if (bitmask.Get (MODMANAGER) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, modmanager)) {
							COUNTERS [MODMANAGER]++;
							continue;
						}

//						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, )) {
//							COUNTERS []++;
//							skip_this_line = true;
//						}

						outfile.WriteLine (a_line);
						line_written++;
					}
				}
			}
				
			Console.WriteLine ("Line read/written = " + lines_read.ToString () + "/" + line_written.ToString ());

			//display statistics
			// if ()
			Console.Write ("empty=" + COUNTERS_BASE [EMPTYLINES] + "\n");
			Console.Write ("useless=" + COUNTERS_BASE [USELESSLINES] + "\n");
			Console.Write ("parse statements=" + COUNTERS_BASE [PARSINGLINES] + "\n");

			foreach (KeyValuePair<string, ushort> statspair in STATSKEYS) {
				if (bitmask.Get (statspair.Value) == true) {
					Console.Write (statspair.Key + "= " + COUNTERS [statspair.Value] + "\n");
				}
			}
		}
	}
}