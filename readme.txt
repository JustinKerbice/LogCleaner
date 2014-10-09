LogCleaner 1.0


1. About

This is a little tool to remove useless, annoying, irrelevant and bandwidth consuming lines in the KSP log files (<KSP_folder>/KSP_Data/output_log.txt).
The standard mode remove empty lines (which contains only a carriage return character) and others like that:

(Filename: C:/BuildAgent/work/d63dfc6385190b60/artifacts/StandalonePlayerGenerated/UnityEngineDebug.cpp Line: 49)

It could be also an useful help for modders who have to deal with reports from the users and have some trouble to get relevant info into the mess of the standard logfile, by removing unneeded and exceeding data.

This is my first steps in C# (but not in programming) and I still have a lot to learn, so this code may not be very reliable and may crash in some case like invalid characters in filename, no space left on device, or any failure case which will happened.


2. Requirements

dotnet 3.5 (should be already installed if you play KSP)
It maybe or maybe not compatible on other OSes than windows, let me know it it is the case or not.


3. how to install it ?

Copy the executable LogCleaner.exe wherever you want, like in a location set in system PATH variable to be able to run it from any cmd window.


4. how to remove it ?

Delete LogCleaner.exe from where you have copy it.


5. usage

LogCleaner.exe <input_file> [<output_file>] [options]

input_file: mandatory, the log file to clean
output_file: optional, the name of the cleaned logfile, if not specified, it will be <input_file>_clean.txt (extension of source file is removed).
if you don't want to provide a filename, but want ot use mask, just use a '-' instead, it will be considered as a non existent argument, ie default will be used.

options:
for now, only a filter mask exists as option

Special masks:
ALL:		remove all messages given below
CFG_ALL:	remove all the CONFIG_* items below FROM STOCK game, so config messages from plug-ins are not filtered unless specified

Filter mask value (power of 2):
mask value			line removed (starting from) exactly as they are typed below
0		Platform assembly:
1		Load(Assembly):
2		Loading_		(*)
3		AssemblyLoader:
4		Non platform assembly:
5		AddonLoader:
6		Load(Audio):
7		Load(Texture):
8		Load(Model):
9		WheelCollider requires an attached Rigidbody to function
10		SCREENSHOT!!
11		Can not play a disabled audio source
12		Resource RESOURCE_DEFINITION added to database
13		Config(PART)
14		Config(RESOURCE_DEFINITION)
15		Config(STATIC)
16		Config(EXPERIMENT_DEFINITION)
17		Config(AGENT)
18		Config(PROP)
19		Config(INTERNAL)
20		Config(STORY_DEF)
21		PartLoader: Compiling (Part) or PartLoader: Compiling (Internal Space)
22		Added <things> (ex: "Added sound_explosion_low to FXGroup flameout")

23		[ModuleManager]
24		Config(ACTIVE_TEXTURE_MANAGER)
25		Config(ACTIVE_TEXTURE_MANAGER_CONFIG)
26		ActiveTextureManagement:
27		Mechjeb2
28		Kerbal Alarm Clock


(*) _ = space character


example: to remove from logfile only screenshots, loadaudio and "can't play a disabled audio source" messages, use:
 2^6 + 2^10 + 2^11 = 3136

Another example: CFG_ALL mask is equal to 67043328 which is
2^17 + 2^18 + ... + 2^24 

Most of the filtered messages comes from what I've seen with the mods I use, it is very likely possible some plug-ins are very verbose and could be filtered, if so, just send me some samples from your logfiles to include them in a further release.
You can also create a patch and submit it on my Github project page.


6. known issues

none. It doesn't means there is no issue at all !


7. Future plans

- custom regexp for at least modders who know what they're doing,
- do the opposite: extract only a subset of lines, useful for modders to have ONLY lines from theirs plug-ins,
- maybe a GUI with a fancy listview to set the filter.


8. Github page

https://github.com/JustinKerbice/LogCleaner


A. changelog

1.0  08/10/2014 initial release


B. Contact

you can reach me directly on the KSP official forum as well as at JustinKerbice@hotmail.fr


C. license

This parts set and all its content, including this readme, is licensed under the whatever license 1.1, see included file for more details.