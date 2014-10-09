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
//	01/10/2014	add a bunch of new filter items (config, etc)
//				add config CFG_ALL item
//	03/10/2014	chg PARTLOADERCOMP 26 -> 25
//	08/10/2014	fix "Parsing *" regexp
//				chg put mods stuff together
//				add "Added [soundfile...]" regexp 
//				add some mods' messages: Mechjeb2, Kerbal Alarm Clock
//	09/10/2014	chg reordering of regexp mask to have stock first then mods
//
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;

// options:
//	search for a given pattern (module or part name)
// highlight exception or warning

namespace LogCleaner
{
	class MainClass
	{
		// these 3: removed (default)
		public const ushort EMPTYLINES = 0;
		public const ushort USELESSLINES = 1;
		public const ushort PARSINGLINES = 2;

		// Filter mask values
		public const ushort PLATFASSEMB = 0;
		public const ushort LOADASSEMB = 1;
		public const ushort LOADING = 2;
		public const ushort ASSEMBLOADER = 3;
		public const ushort NOPLATFASSEM = 4;
		public const ushort ADDONLOADER = 5;
		public const ushort LOADAUDIO = 6;
		public const ushort LOADTEX = 7;
		public const ushort LOADMODEL = 8;
		public const ushort WHEELCOLL = 9;
		public const ushort SCREENSHOT = 10;
		public const ushort CANTPLAYAS = 11;
		public const ushort RESDEFADDB = 12;
		public const ushort CONFIG_PART = 13;
		public const ushort CONFIG_RESDEF = 14;
		public const ushort CONFIG_STATIC = 15;
		public const ushort CONFIG_EXPDEF = 16;
		public const ushort CONFIG_AGENT = 17;
		public const ushort CONFIG_PROP = 18;
		public const ushort CONFIG_INTERNAL = 19;
		public const ushort CONFIG_STORDEF = 20;
		public const Int32 CONFIG_ALL = (2 << CONFIG_ATM) + (2 << CONFIG_ATMCFG) + (2 << CONFIG_PART) + (2 << CONFIG_RESDEF) + (2 << CONFIG_STATIC) + (2 << CONFIG_EXPDEF) + (2 << CONFIG_AGENT) + (2 << CONFIG_PROP) + (2 << CONFIG_INTERNAL) + (2 << CONFIG_STORDEF);
		public const ushort PARTLOADERCOMP = 21;
		public const ushort ADDED_STUFF = 22;

		public const ushort MODMANAGER = 23;
		public const ushort CONFIG_ATM = 24;
		public const ushort CONFIG_ATMCFG = 25;
		public const ushort ACTTEXMAN = 26;
		public const ushort MECHJEB2 = 27;
		public const ushort KERBALACLOCK = 28;

		// * Cannot find a PartModule of typename
		//

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
			{ "config_part", CONFIG_PART },
			{ "config_resdef", CONFIG_RESDEF },
			{ "config_static", CONFIG_STATIC },
			{ "config_expdef", CONFIG_EXPDEF },
			{ "config_agent", CONFIG_AGENT },
			{ "config_prop", CONFIG_PROP },
			{ "config_internal", CONFIG_INTERNAL },
			{ "config_stordef", CONFIG_STORDEF },
			{ "partloader_compil", PARTLOADERCOMP },
			{ "added_stuff", ADDED_STUFF },
			{ "config_atm", CONFIG_ATM },
			{ "config_atmcfg", CONFIG_ATMCFG },
			{ "modulemanager", MODMANAGER },
			{ "mechjeb2", MECHJEB2 },
			{ "kerbalaclock", KERBALACLOCK },

		};

		public static void Main (string[] args)
		{
			string inFile = "";
			string outFile = "";
			UInt32 remove_mask = 0;

			BitArray bitmask;

			const string version = "1.0";
			const string release = "01/10/2014";

			const string emptyline = "^\\s*$";
			const string uselessline = "\\(Filename:\\s+.*Line:\\s+-{0,1}\\d+\\)";
			const string parsingwdca = "^Parsing\\s+";
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
			const string config_part = "^Config\\(PART\\)";
			const string config_resdef = "^Config\\(RESOURCE_DEFINITION\\)";
			const string config_static = "^Config\\(STATIC\\)";
			const string config_expdef = "^Config\\(EXPERIMENT_DEFINITION\\)";
			const string config_agent = "^Config\\(AGENT\\)";
			const string config_prop = "^Config\\(PROP\\)";
			const string config_internal = "^Config\\(INTERNAL\\)";
			const string config_stordef = "^Config\\(STORY_DEF\\)";
			const string partloader_compil = "^PartLoader:\\s+Compiling\\s+(Part|Internal Space)";
			const string added_stuff = "^Added\\s+";

			const string modmanager = "\\[ModuleManager\\]";
			const string config_atm = "^Config\\(ACTIVE_TEXTURE_MANAGER\\)";
			const string config_atmcfg = "^Config\\(ACTIVE_TEXTURE_MANAGER_CONFIG\\)";
			const string mechjeb2 = "^\\[MechJeb2\\]\\s+";
			const string kerbalaclock = ",KerbalAlarmClock"; //,Loading:\\s+";


			int lines_read = 0;
			int line_written = 0;
			int[] COUNTERS_BASE = new int[3];
			int[] COUNTERS = new int[32];

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
						// chg ext/name
						//outFile = Path.GetDirectoryName (args [1]) + "\\" + Path.GetFileNameWithoutExtension (args [1]) + "_2.txt";
						//Console.WriteLine ("output file name = [" + outFile + "]");
					}

					outFile = args [1];
				} else {
					outFile = Path.GetDirectoryName (args [0]) + "\\" + Path.GetFileNameWithoutExtension (args [0]) + "_clean.txt";
					Console.WriteLine ("output file name = [" + outFile + "]");
				}

				if (args.Length > 2 && args [2] != null && args [2] != "") {

					if (args [2] == "ALL") {
						Console.WriteLine ("MASK ALL !");
						remove_mask = Int32.MaxValue;
					} else if (args [2] == "CFG_ALL") {
						Console.WriteLine ("MASK CONFIG ALL ! CFGALL=[" + CONFIG_ALL.ToString() + "]");
						remove_mask = CONFIG_ALL;
					} else {
						try {
							remove_mask = UInt32.Parse (args [2]);
						} catch (Exception exception_caught) {
							Console.WriteLine ("Invalid mask provided, use the full mask. Exception was: " + exception_caught.Message);
							remove_mask = Int32.MaxValue;
						}
					}

					Console.WriteLine ("RM=[" + remove_mask.ToString () + "]");
					bitmask = new BitArray (new int[] { (int)remove_mask });

					//tmp display
					Console.Write ("bitmask=[");
					for (int i = 0; i < bitmask.Count; i++) {
						bool bit = bitmask.Get (i);
						Console.Write (bit ? 1 : 0);
					}
					Console.WriteLine ("]");

				} else {
					Console.WriteLine ("No filter prodived, use default.");
					// default = 0, ~rewrtie this, convert after this block, set default... TC
				}
			} else {
				Console.WriteLine ("Usage: LogCleaner <infile> [<outfile>] [filter mask]\n\tinfile\t\tthe file to clean\n\toutfile\t\tthe output file, default will be infile_clean.extension\n\tfilter mask\tthe line to remove from file.");
				Environment.Exit (1);
			}

			Console.WriteLine ("Processing " + inFile + "... outfile=[" + outFile + "]");

			bitmask = new BitArray (new int[] { (int)remove_mask });

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
							
						if (bitmask.Get (CONFIG_PART) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_part)) {
							COUNTERS [CONFIG_PART]++;
							continue;
						}

						if (bitmask.Get (CONFIG_RESDEF) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_resdef)) {
							COUNTERS [CONFIG_RESDEF]++;
							continue;
						}

						if (bitmask.Get (CONFIG_STATIC) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_static)) {
							COUNTERS [CONFIG_STATIC]++;
							continue;
						}

						if (bitmask.Get (CONFIG_EXPDEF) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_expdef)) {
							COUNTERS [CONFIG_EXPDEF]++;
							continue;
						}

						if (bitmask.Get (CONFIG_AGENT) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_agent)) {
							COUNTERS [CONFIG_AGENT]++;
							continue;
						}

						if (bitmask.Get (CONFIG_PROP) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_prop)) {
							COUNTERS [CONFIG_PROP]++;
							continue;
						}

						if (bitmask.Get (CONFIG_INTERNAL) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_internal)) {
							COUNTERS [CONFIG_INTERNAL]++;
							continue;
						}

						if (bitmask.Get (CONFIG_STORDEF) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_stordef)) {
							COUNTERS [CONFIG_STORDEF]++;
							continue;
						}

						if (bitmask.Get (PARTLOADERCOMP) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, partloader_compil)) {
							COUNTERS [PARTLOADERCOMP]++;
							continue;
						}

						if (bitmask.Get (ADDED_STUFF) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, added_stuff)) {
							COUNTERS [ADDED_STUFF]++;
							continue;
						}

						if (bitmask.Get (CONFIG_ATM) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_atm)) {
							COUNTERS [CONFIG_ATM]++;
							continue;
						}

						if (bitmask.Get (CONFIG_ATMCFG) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, config_atmcfg)) {
							COUNTERS [CONFIG_ATMCFG]++;
							continue;
						}

						if (bitmask.Get (MODMANAGER) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, modmanager)) {
							COUNTERS [MODMANAGER]++;
							continue;
						}

						if (bitmask.Get (MECHJEB2) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, mechjeb2)) {
							COUNTERS [MECHJEB2]++;
							continue;
						}

						if (bitmask.Get (KERBALACLOCK) == true && System.Text.RegularExpressions.Regex.IsMatch (a_line, kerbalaclock)) {
							COUNTERS [KERBALACLOCK]++;
							continue;
						}

//						if (System.Text.RegularExpressions.Regex.IsMatch (a_line, )) {
//							COUNTERS []++;
//							continue;
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