namespace PSI;
using static Token.E;
using static Console;

// Represents a PSI language Token
public class Token {
   public Token (Tokenizer source, E kind, string text, int line, int column) 
      => (Source, Kind, Text, Line, Column) = (source, kind, text, line, column);
   public Tokenizer Source { get; }
   public E Kind { get; }
   public string Text { get; }
   public int Line { get; }
   public int Column { get; }

   // The various types of token
   public enum E {
      // Keywords
      PROGRAM, VAR, IF, THEN, WHILE, ELSE, FOR, TO, DOWNTO,
      DO, BEGIN, END, PRINT, TYPE, NOT, OR, AND, MOD, _ENDKEYWORDS,
      // Operators
      ADD, SUB, MUL, DIV, NEQ, LEQ, GEQ, EQ, LT, GT, ASSIGN, 
      _ENDOPERATORS,
      // Punctuation
      SEMI, PERIOD, COMMA, OPEN, CLOSE, COLON, 
      _ENDPUNCTUATION,
      // Others
      IDENT, INTEGER, REAL, BOOLEAN, STRING, CHAR, EOF, ERROR
   }

   // Print a Token
   public override string ToString () => Kind switch {
      EOF or ERROR => Kind.ToString (),
      < _ENDKEYWORDS => $"\u00ab{Kind.ToString ().ToLower ()}\u00bb",
      STRING => $"\"{Text}\"",
      CHAR => $"'{Text}'",
      _ => Text,
   };

   // Utility function used to echo an error to the console
   public void PrintError () {
      if (Kind != ERROR) throw new Exception ("PrintError called on a non-error token");
      OutputEncoding = new UnicodeEncoding ();
      var file = $"File: {Source.FileName}";
      WriteLine (file);
      WriteLine (new string ('\u2500', file.Length));

      if (Line > 2) {
         var (prevText, prevText1) = (Source.Lines[Line - 3], Source.Lines[Line - 2]);
         WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line - 2, "\u2502", prevText));
         WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line - 1, "\u2502", prevText1));
      }
      if (Line == 1)
         WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line - 1, "\u2502", Source.Lines[Line - 1]));

      WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line, "\u2502", Source.Lines[Line - 1]));

      ForegroundColor = ConsoleColor.Yellow;
      int padLeft = Column + 4;
      CursorLeft = padLeft;
      WriteLine ("^");
      CursorLeft = Math.Min (Math.Max (0, padLeft - Text.Length / 2), WindowWidth - Text.Length);
      WriteLine (Text);
      ResetColor ();

      var cnt = Source.Lines.Count ();
      if (Line + 1 == cnt)
         WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line + 1, "\u2502", Source.Lines[Line + 1]));
      if (Line + 2 < cnt) {
         WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line + 1, "\u2502", Source.Lines[Line + 2]));
         WriteLine (string.Format ("{0,4}{1,0}{2,0}", Line + 2, "\u2502", Source.Lines[Line + 1]));
      }
      ResetColor ();
   }

   // Helper used by the parser (maps operator sequences to E values)
   public static List<(E Kind, string Text)> Match = new () {
      (NEQ, "<>"), (LEQ, "<="), (GEQ, ">="), (ASSIGN, ":="), (ADD, "+"),
      (SUB, "-"), (MUL, "*"), (DIV, "/"), (EQ, "="), (LT, "<"),
      (LEQ, "<="), (GT, ">"), (SEMI, ";"), (PERIOD, "."), (COMMA, ","),
      (OPEN, "("), (CLOSE, ")"), (COLON, ":")
   };
}
