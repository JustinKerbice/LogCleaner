// KSP logfile cleaner
// Justin Kerbice 22/09/2014

using System;
using System.IO;

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
		public const ushort EMPTYLINES = 0;
		public const ushort USELESSLINES = 1;
		public const ushort PARSINGLINES = 2;
		public const ushort PLATFASSEMB = 3;
		public const ushort LOADASSEMB = 4;
		public const ushort LOADING = 5;
		public const ushort ASSEMBLOADER = 6;
		public const ushort NOPLATFASSEM = 7;
		public const ushort ADDONLOADER = 8;
		public const ushort LOADAUDIO = 9;
		public const ushort LOADTEX = 10;

		public static void Main (string[] args)
		{
			string inFile = ""; //@"d:\output_log.txt";
			string outFile = ""; //@"d:\tmp.txt";
			int remove_mask = 0;

			const string version = "1.0";

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
			int[] COUNTERS = new int[20];

			Console.WriteLine ("LogCleaner " + version + ", a Kerbal Space Program logfile cleaner.\nJustin Kerbice 24/09/2014\n");

			//Console.WriteLine ("AL=[" + args.Length.ToString () + "]");

			if (args != null &&
			    args.Length >= 1) {
				//Console.WriteLine ("args1 = [" + args[0] + "]");

				if (args [0] != null && args [0] != "" && File.Exists (args [0])) {
					inFile = args [0];
				} else {
					Console.WriteLine ("File [" + args [0] + "] does not exists !");
					Environment.Exit (2);
				}

				if (args.Length > 1 && args [1] != null && args [1] != "") {
					if (args [1] == "-") {
						Console.WriteLine ("arg 1 ignored");
					} else {
						if (File.Exists (args [1])) {
							Console.WriteLine ("Warning output file [" + args [1] + "] already exists");
						}
					}

					outFile = args[1];
				} else {
					outFile = Path.GetDirectoryName( args[0] ) + "\\" + Path.GetFileNameWithoutExtension ( args[0] ) + "_clean.txt";
					Console.WriteLine ("output file name = [" + outFile + "]");
					//Console.WriteLine ("output file not specified, abort.");
					//Environment.Exit (3);
				}

				if (args.Length > 2 && args [2] != null && args [2] != "") {
					remove_mask = Int32.Parse (args [2]);
					Console.WriteLine ("RM=[" + remove_mask.ToString () + "]");
				} else {
				}
			} else {
				Console.WriteLine ("Usage: LogCleaner <infile> <outfile>");
				Environment.Exit (1);
			}
			Console.WriteLine ("Processing " + inFile + "...");

			// + check for size/readable, writable path, ~disk space left
			using (StreamReader infile = File.OpenText(inFile)) 
			{
				using (StreamWriter outfile = new StreamWriter(outFile)) {

					string a_line = "";
					while ((a_line = infile.ReadLine ()) != null) {
						lines_read++;

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, emptyline)) {
							COUNTERS [EMPTYLINES]++;
						}

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, uselessline)) {
							COUNTERS [USELESSLINES]++;
						}

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, parsingwdca)) {
							COUNTERS [PARSINGLINES]++;
						}

						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, platassemb)) {
							COUNTERS [PLATFASSEMB]++;
						}


						if (!System.Text.RegularExpressions.Regex.IsMatch (a_line, emptyline) &&
							!System.Text.RegularExpressions.Regex.IsMatch (a_line, uselessline) &&
							!System.Text.RegularExpressions.Regex.IsMatch (a_line, parsingwdca) &&
							!System.Text.RegularExpressions.Regex.IsMatch (a_line, platassemb)
						) {

							outfile.WriteLine (a_line);
							line_written++;
						}
					}
				}
			}
				
			Console.WriteLine ("Line read/written = " + lines_read.ToString () + "/" + line_written.ToString ());

			//display statistics
			Console.Write ("empty=" + COUNTERS [EMPTYLINES] +"\n");
			Console.Write ("useless=" + COUNTERS [USELESSLINES] +"\n");
			Console.Write ("parse statements=" + COUNTERS [PARSINGLINES] +"\n");
		}
	}
}