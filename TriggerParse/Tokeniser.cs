using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TriggerParse
{
	// LINK STRUCTURE: (HIGH TYPE SURROGATE) (LOW TYPE SURROGATE) (HIGH ID SURROGATE) (LOW ID SURROGATE)
	public class Tokeniser
	{
		static Regex RuleExtractor = new Regex("\\/[a-zA-Z0-9]+\\/");
		public Dictionary<string, string> Types = new Dictionary<string, string>(); // type surrogate pairs to type string
		public Dictionary<string, List<Regex>> Rules = new Dictionary<string, List<Regex>>(); // type surrogate pairs to rule
		public string Run(string Input, bool compact){
			ParsedString s = new ParsedString (){ t = this };
			s.Run (Input);
			return s.genJSON (compact);
		}
		public string LoadType(string Type){
			string key;
			if(Types.Count > 0) key = char.ConvertFromUtf32 (char.ConvertToUtf32 (Types.Last().Key, 0) + 1);
			else key = "\U000F0000";
			Types [key] = Type;
			return key;
		}
		public string LookupOrCreateType(string Name){
			string TypeSurrogatePair = Types.FirstOrDefault((delegate(KeyValuePair<string, string> arg) {return arg.Value == Name;})).Key;
			if (string.IsNullOrEmpty (TypeSurrogatePair)) TypeSurrogatePair = LoadType(Name);
			return TypeSurrogatePair;
		}
		public void LoadRule(string Rule){
			string[] Sections = Rule.Split (new char[]{ ':' }, 2);
			string TypeStr = Sections [0];
			string RuleRegex = Sections[1];
			string TypeSurrogatePair = LookupOrCreateType (TypeStr);
			Match m;
			while ((m = RuleExtractor.Match(RuleRegex)).Success) {
				string MatchTypeSurrogatePair = LookupOrCreateType(m.Value.Trim(new char[]{'/'}));
				RuleRegex = m.Splice(RuleRegex, "(" + MatchTypeSurrogatePair + "..)");
			}
			RuleRegex.Replace ("\\/", "/");
			if(!Rules.ContainsKey(TypeSurrogatePair)) Rules.Add (TypeSurrogatePair, new List<Regex>());
			Rules [TypeSurrogatePair].Add (new Regex (RuleRegex));
		}
		public void LoadRules(string Rules){
			foreach (var r in Rules.Split(new string[]{Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries))
				if (r.Contains (":"))
					LoadRule (r);
		}
	}
	class ParsedString{
		public Dictionary<string, string> CreatedStrings = new Dictionary<string, string>();//id surrogate pair to string
		public Dictionary<string, string> CreatedStringTypes = new Dictionary<string, string>();//id surrogate pair to type surrogate pair
		public Tokeniser t;
		public string root;
		string AddString(string s, string type){
			string key;
			if (CreatedStrings.Count > 0) key = char.ConvertFromUtf32 (char.ConvertToUtf32 (CreatedStrings.Last().Key, 0) + 1);
			else key = "\U00100000";
			CreatedStrings.Add (key, s);
			CreatedStringTypes.Add (key, type);
			return key;
		}
		public void Run(string s){
			root = AddString (s, t.LookupOrCreateType("ROOT"));
			Run ();
		}
		void Run(){
			bool run = true;//keep running until no rules are matched
			while (run) {
				run = false;
				foreach (var p in t.Rules) {
					Match m;
					foreach (var reg in p.Value) {
						while ((m = reg.Match (CreatedStrings [root])).Success) {
							run = true;
							CreatedStrings [root] = m.Splice (CreatedStrings [root], p.Key + AddString (m.Value, p.Key));
						}
					}
				}
			}
		}
		public string debugStr(){
			string s = CreatedStrings [root];
			bool finished = false;
			while (!finished) {
				finished = true;
				foreach (var t in t.Types) {
					Regex r = new Regex("(" + t.Key +"..)");
					Match m;
					while ((m = r.Match (s)).Success) {
						finished = false;
						string ID = m.Value.Substring (2);
						s = m.Splice (s, "(" + CreatedStrings [ID] + ")");
					}
				}
			}
			return s;
		}
		public string genJSON(bool compact){
			return genJSON (root, t.Types.First(x => {return x.Value == "ROOT";}).Key, compact);
		}
		string genJSON(string ID, string Type, bool compact){
			string ret;
			if (!compact)
				ret = "{\"t\":\"" + t.Types [Type] + "\",\"c\":[";
			else
				ret = "{\"" + t.Types [Type] + "\":[";
			string current = "";
			for (int i = 0; i < CreatedStrings[ID].Length; i++) {
				if(!char.IsHighSurrogate(CreatedStrings[ID][i])){
					current += CreatedStrings [ID] [i];
					continue;
				}
				if (!string.IsNullOrEmpty (current)) {
					ret += current.EscapeJSON() + ',';
					current = "";
				}
				string SubID = CreatedStrings [ID].Substring(i + 2, 2);
				string SubType = CreatedStrings [ID].Substring(i, 2);
				ret += genJSON (SubID, SubType, compact) + ",";
				i += 3;
			}
			if (!string.IsNullOrEmpty (current)) {
				ret += current.EscapeJSON()+ ',';
				current = "";
			}
			return ret.Trim(new char[]{','}) + "]}";
		}
	}
	static class Utils{
		public static string Splice(this Match m, string Old, string New){//splice New in where the match happened
			return Old.Substring (0, m.Index) + New + Old.Substring (m.Index + m.Length);
		}
		public static string EscapeJSON (this string s){
			s = s.Replace ("\\", "\\\\");
			s = s.Replace ("\"", "\\\"");
			s = s.Replace ("/", "\\/");
			s = s.Replace ("\b", "\\b");
			s = s.Replace ("\f", "\\f");
			s = s.Replace ("\n", "\\n");
			s = s.Replace ("\r", "\\r");
			s = s.Replace ("\t", "\\t");
			return '"' + s + '"';
		}
	}
}