# TriggerParse
A utility for people who are tired of overcomplicated parsers. Takes in (a slightly modified version of) regex, a file, and outputs a JSON formatted AST.
# Usage
## Example rule file: (Demo.tpr)
```
word:\w+
sentence:/word/( /word/)+
```
## In order to use this file to parse something, you write (in bash);
```
#./TriggerParseConsole.exe Demo.tpr <<< "hello world"
{"t":"ROOT","c":[{"t":"sentence","c":[{"t":"word","c":["hello"]}," ",{"t":"word","c":["world"]}]},"\n"]}
```
## Or, more compactly (but harder to process);
```
#./TriggerParseConsole.exe -c Demo.tpr <<< "hello world"
{"ROOT":[{"sentence":[{"word":["hello"]}," ",{"word":["world"]}]},"\n"]}
```
