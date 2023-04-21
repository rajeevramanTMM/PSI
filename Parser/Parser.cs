﻿// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// Parser.cs ~ Recursive descent parser for Pascal Grammar
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;
using static Token.E;
using static NType;

public class Parser {
   // Interface -------------------------------------------
   public Parser (Tokenizer tokenizer)
      => mToken = mPrevPrev = mPrevious = (mTokenizer = tokenizer).Next ();

   public NProgram Parse () {
      var node = Program ();
      if (mToken.Kind != EOF) Unexpected ();
      return node;
   }

   #region Declarations ------------------------------------
   // program = "program" IDENT ";" block "." .
   NProgram Program () {
      Expect (PROGRAM); var name = Expect (IDENT); Expect (SEMI);
      var block = Block (); Expect (PERIOD);
      return new (name, block);
   }

   // block = declarations compound-stmt .
   NBlock Block ()
      => new (Declarations (), CompoundStmt ());

   // declarations = [var-decls] [procfn-decls] .
   NDeclarations Declarations () {
      List<NVarDecl> vars = new ();
      List<NProcDecl> procs = new ();
      List<NFnDecl> funcs = new ();
      if (Match (VAR))
         do { vars.AddRange (VarDecls ()); Expect (SEMI); } while (Peek (IDENT));
      while (Match (PROCEDURE) || Match (FUNCTION)) {
         var (retType, prevToken, nIdent, nParams) = (Unknown, mPrevious, Expect (IDENT), new List<NVarDecl> { });
         Expect (OPEN);
         while (Peek (IDENT)) { nParams.AddRange (VarDecls ()); Match (SEMI); }
         Expect (CLOSE);
         if (prevToken.Kind == FUNCTION) { Match (COLON); retType = Type (); }
         Expect (SEMI); var block = Block (); Expect (SEMI);
         if (prevToken.Kind == FUNCTION)
            funcs.Add (new NFnDecl (nIdent, nParams.ToArray (), retType, block));
         else
            procs.Add (new NProcDecl (nIdent, nParams.ToArray (), block));
      }
      return new (vars.ToArray (), procs.ToArray (), funcs.ToArray ());
   }

   // ident-list = IDENT { "," IDENT }
   Token[] IdentList () {
      List<Token> names = new ();
      do { names.Add (Expect (IDENT)); } while (Match (COMMA));
      return names.ToArray (); 
   }

   // var-decl = ident-list ":" type
   NVarDecl[] VarDecls () {
      var names = IdentList (); Expect (COLON); var type = Type ();
      return names.Select (a => new NVarDecl (a, type)).ToArray ();
   }

   // type = integer | real | boolean | string | char
   NType Type () {
      var token = Expect (INTEGER, REAL, BOOLEAN, STRING, CHAR);
      return token.Kind switch {
         INTEGER => Int, REAL => Real, BOOLEAN => Bool, 
         STRING => String, _ => Char,
      };
   }
   #endregion
   
   #region Statements ---------------------------------------
   // statement         =  write-stmt | read-stmt | assign-stmt | call-stmt |
   //                      goto-stmt | if-stmt | while-stmt | repeat-stmt |
   //                      compound-stmt | for-stmt | case-stmt
   NStmt Stmt () {
      if (Match (WRITE, WRITELN)) return WriteStmt ();
      if (Match (IDENT)) {
         if (Match (ASSIGN)) return AssignStmt ();
         else return CallStmt ();
      }
      if (Match (READ)) return ReadStmt ();
      if (Match (REPEAT)) return RepeatStmt ();
      if (Match (WHILE)) return WhileStmt ();
      if (Match (IF)) return IfStmt ();
      if (Match (FOR)) return ForStmt ();
      if (Peek (BEGIN)) { var compStmt = CompoundStmt (); Expect (SEMI); return compStmt; }
      Unexpected ();
      return null!;
   }

   NCallStmt CallStmt () => new (Prev, ArgList ());

   NForStmt ForStmt () {
      var token = Expect (IDENT, "Expecting identifier");
      Expect (ASSIGN);
      var expr = Expression ();
      bool to = Match (TO);
      if (!to) Expect (DOWNTO);
      var expr1 = Expression ();
      Expect (DO, "Expecting do statement");
      var stm = Stmt ();
      return new NForStmt (token, to, (expr, expr1), stm);
   }

   NIfStmt IfStmt () {
      var stmts = new List<NStmt> ();
      var expr = Expression ();
      Expect (THEN, "Expecting then statement");
      stmts.Add (Stmt ()); Match (SEMI);
      if (Match (ELSE)) { stmts.Add (Stmt ()); Match (SEMI); }
      return new NIfStmt (expr, stmts.ToArray ());
   }

   NWhileStmt WhileStmt () {
      var stm = new List<NStmt> ();
      var expr = Expression ();
      Expect (DO, "Expecting do statement");
      while (!Peek (END)) { stm.Add (Stmt ()); Match (SEMI); }
      return new (expr, stm.ToArray ());
   }

   NRepeatStmt RepeatStmt () {
      var stm = new List<NStmt> ();
      while (!Peek (UNTIL)) { stm.Add (Stmt ()); Match (SEMI); }
      Expect (UNTIL, "Expecting until condition");
      var expr = Expression ();
      return new (stm.ToArray (), expr);
   }

   // compound-stmt = "begin" [ statement { ";" statement } ] "end" .
   NCompoundStmt CompoundStmt () {
      Expect (BEGIN);
      List<NStmt> stmts = new ();
      while (!Match (END)) { stmts.Add (Stmt ()); Match (SEMI); }
      return new (stmts.ToArray ());
   }

   // write-stmt =  ( "writeln" | "write" ) arglist .
   NWriteStmt WriteStmt () 
      => new (Prev.Kind == WRITELN, ArgList ());

   // assign-stmt = IDENT ":=" expr .
   NAssignStmt AssignStmt () 
      => new (PrevPrev, Expression ());

   NReadStmt ReadStmt () {
      Expect (OPEN, "Expecting open brace");
      var args = IdentList ();
      Expect (CLOSE, "Expecting close brace");
      return new (args.ToArray ());
   }
   #endregion

   #region Expression --------------------------------------
   // expression = equality .
   NExpr Expression () 
      => Equality ();

   // equality = equality = comparison [ ("=" | "<>") comparison ] .
   NExpr Equality () {
      var expr = Comparison ();
      if (Match (EQ, NEQ)) 
         expr = new NBinary (expr, Prev, Comparison ());
      return expr;
   }

   // comparison = term [ ("<" | "<=" | ">" | ">=") term ] .
   NExpr Comparison () {
      var expr = Term ();
      if (Match (LT, LEQ, GT, GEQ))
         expr = new NBinary (expr, Prev, Term ());
      return expr;
   }

   // term = factor { ( "+" | "-" | "or" ) factor } .
   NExpr Term () {
      var expr = Factor ();
      while  (Match (ADD, SUB, OR)) 
         expr = new NBinary (expr, Prev, Factor ());
      return expr;
   }

   // factor = unary { ( "*" | "/" | "and" | "mod" ) unary } .
   NExpr Factor () {
      var expr = Unary ();
      while (Match (MUL, DIV, AND, MOD)) 
         expr = new NBinary (expr, Prev, Unary ());
      return expr;
   }

   // unary = ( "-" | "+" ) unary | primary .
   NExpr Unary () {
      if (Match (ADD, SUB))
         return new NUnary (Prev, Unary ());
      return Primary ();
   }

   // primary = IDENTIFIER | INTEGER | REAL | STRING | "(" expression ")" | "not" primary | IDENTIFIER arglist .
   NExpr Primary () {
      if (Match (IDENT)) {
         if (Peek (OPEN)) return new NFnCall (Prev, ArgList ());
         return new NIdentifier (Prev);
      }
      if (Match (L_INTEGER, L_REAL, L_BOOLEAN, L_CHAR, L_STRING)) return new NLiteral (Prev);
      if (Match (NOT)) return new NUnary (Prev, Primary ());
      Expect (OPEN, "Expecting identifier or literal");
      var expr = Expression ();
      Expect (CLOSE);
      return expr;
   }

   // arglist = "(" [ expression { , expression } ] ")"
   NExpr[] ArgList () {
      List<NExpr> args = new ();
      Expect (OPEN);
      if (!Peek (CLOSE)) args.Add (Expression ());
      while (Match (COMMA)) args.Add (Expression ());
      Expect (CLOSE);
      return args.ToArray ();
   }
   #endregion

   #region Helpers -----------------------------------------
   // Expect to find a particular token
   Token Expect (Token.E kind, string message) {
      if (!Match (kind)) Throw (message);
      return mPrevious;
   }

   Token Expect (params Token.E[] kinds) {
      if (!Match (kinds)) 
         Throw ($"Expecting {string.Join (" or ", kinds)}");
      return mPrevious;
   }

   // Like Match, but does not consume the token
   bool Peek (params Token.E[] kinds)
      => kinds.Contains (mToken.Kind);

   // Match and consume a token on match
   bool Match (params Token.E[] kinds) {
      if (kinds.Contains (mToken.Kind)) {
         mPrevPrev = mPrevious; mPrevious = mToken; 
         mToken = mTokenizer.Next ();
         return true;
      }
      return false;
   }

   [DoesNotReturn]
   void Throw (string message) {
      throw new ParseException (mTokenizer.FileName, mTokenizer.Lines, mToken.Line, mToken.Column, message);
   }

   [DoesNotReturn]
   void Unexpected () {
      string message = $"Unexpected {mToken}";
      if (mToken.Kind == ERROR) message = mToken.Text;
      Throw (message);
   }

   // The 'previous' two tokens we've seen
   Token Prev => mPrevious;
   Token PrevPrev => mPrevPrev;

   Token mToken, mPrevious, mPrevPrev;
   readonly Tokenizer mTokenizer;
   #endregion 
}