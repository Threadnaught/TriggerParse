using System;
using TriggerParse;

class MainClass
{
	public static void Main (string[] args)
	{
		if (args [0] == "help" || args [0] == "--help") {
			Console.WriteLine ("Trigger Parse utility;");
			Console.WriteLine ("enter .tpr file path as argument");
			Console.WriteLine ("put text to be parsed on stdin");
			Console.WriteLine ("formatted JSON will be returned on stdout");
			Console.WriteLine ("use -c to enable compact serialization");
			return;
		}
		string s = "";
		int i;
		while ((i = Console.Read ()) != -1) {
			s += (char)i;
		}
		string path = "";
		bool compact = false;
		foreach (var arg in args)
			if (arg == "-c") {
				compact = true;
			} else {
				path = arg;
			}
		Parser t = new Parser ();
		t.LoadRules (System.IO.File.ReadAllText (path));
		Console.Write (t.Run (s, compact));
	}
}

