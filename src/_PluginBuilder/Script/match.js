if (WScript.Arguments.Count() !== 2) {
  WScript.Echo("Syntax: match.js regex string");
  WScript.Quit(1);
}
var rx = new RegExp(WScript.Arguments(0), "i");
var str = WScript.Arguments(1);
WScript.Quit(str.match(rx) ? 0 : 1);