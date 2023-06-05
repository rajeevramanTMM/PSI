﻿// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// ILCodeGen.cs : Compiles a PSI parse tree to IL
// ─────────────────────────────────────────────────────────────────────────────
using System.Text;
namespace PSI;

public class ILCodeGen : Visitor {
   // Generated code is gathered heres
   public readonly StringBuilder S = new ();

   public override void Visit (NProgram p) {
      Out (".assembly extern System.Runtime { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 7:0:0:0 }");
      Out (".assembly extern System.Console { .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A) .ver 7:0:0:0 }");
      Out (".assembly extern PSILib { .ver 1:0:0:0 }");
      Out ($".assembly {p.Name} {{ .ver 0:0:0:0 }}\n");

      Out (".class Program {");
      mSymbols = new SymTable { Parent = mSymbols };
      p.Block.Declarations.Accept (this);
      Out ("  .method static void Main () {");
      Out ("    .entrypoint");
      p.Block.Body.Accept (this);
      mSymbols = mSymbols.Parent;
      Out ("    ret");
      Out ("  }");
      Out ("}");
   }
   SymTable mSymbols = SymTable.Root;

   public override void Visit (NBlock b) {
      b.Declarations.Accept (this); b.Body.Accept (this);
   }

   public override void Visit (NDeclarations d) {
      Visit (d.Consts); Visit (d.Vars); Visit (d.Funcs);
   }

   public override void Visit (NConstDecl c) {
      mSymbols.Add (c);
   }

   public override void Visit (NVarDecl v) {
      mSymbols.Add (v);
      if (v.Local) Out ($"    .locals init ({TMap[v.Type]} {v.Name})");
      else Out ($"    .field static {TMap[v.Type]} {v.Name}");
   }

   public override void Visit (NFnDecl f) {
      mSymbols.Add (f);
      mSymbols = new SymTable () { Local = true, Parent = mSymbols };
      foreach (var p in f.Params) { mSymbols.Add (p); p.Assigned = p.Argument = true; }
      var retType = f.Return;
      Out ($"  .method static {TMap[retType]} {f.Name} ({f.Params.Select (a => $"{TMap[a.Type]} {a.Name}").ToCSV ()}) {{");
      var iReturn = retType != NType.Void;
      if (iReturn) Visit (new NVarDecl (f.Name, retType) { Local = true });
      f.Block?.Accept (this);
      if (iReturn) GetIdentifier (f.Name);
      Out ("    ret");
      Out ("  }");
      mSymbols = mSymbols.Parent;
   }

   public override void Visit (NCompoundStmt b) =>
      Visit (b.Stmts);

   public override void Visit (NAssignStmt a) {
      a.Expr.Accept (this);
      StoreVar (a.Name);
   }

   public override void Visit (NWriteStmt w) {
      foreach (var e in w.Exprs) {
         e.Accept (this);
         Out ($"    call void [System.Console]System.Console::Write ({TMap[e.Type]})");
      }
      if (w.NewLine) Out ("    call void [System.Console]System.Console::WriteLine ()");
   }

   public override void Visit (NIfStmt f) {
      var hasElse = f.ElsePart != null;
      (string lbl1, string lbl2) = (NextLabel (), "");
      f.Condition.Accept (this);
      Out ($"    brfalse {lbl1}");
      f.IfPart.Accept (this);
      if (hasElse) { lbl2 = NextLabel (); Out ($"    br {lbl2}"); }
      Out ($"    {lbl1}:");
      if (hasElse) { f.ElsePart!.Accept (this); Out ($" {lbl2}:"); }
   }

   public override void Visit (NForStmt f) {
      f.Start.Accept (this);
      StoreVar (f.Var);
      (string lbl1, string lbl2) = (NextLabel (), NextLabel ());
      Out ($"    br {lbl2}");
      Out ($"    {lbl1}:");
      f.Body.Accept (this);
      LoadVar (f.Var);
      Out (" ldc.i4.1");
      Out ($" {(f.Ascending ? "add" : "sub")}");
      StoreVar (f.Var);
      Out ($"    {lbl2}:");
      LoadVar (f.Var);
      f.End.Accept (this);
      Out ($"    {(f.Ascending ? "cgt" : "clt")}");
      Out ($"    brfalse {lbl1}");
   }

   public override void Visit (NReadStmt r) => throw new NotImplementedException ();

   public override void Visit (NWhileStmt w) {
      string lab1 = NextLabel (), lab2 = NextLabel ();
      Out ($"    br {lab2}");
      Out ($"  {lab1}:");
      w.Body.Accept (this);
      Out ($"  {lab2}:");
      w.Condition.Accept (this);
      Out ($"    brtrue {lab1}");
   }

   public override void Visit (NRepeatStmt r) {
      string lab = NextLabel ();
      Out ($"  {lab}:");
      Visit (r.Stmts);
      r.Condition.Accept (this);
      Out ($"    brfalse {lab}");
   }
   string NextLabel () => $"IL_{++mLabel:D4}";
   int mLabel;

   public override void Visit (NCallStmt c) => GetCallFN (c.Name, c.Params);

   public override void Visit (NLiteral t) {
      var v = t.Value;
      Out (t.Type switch {
         NType.String => $"    ldstr \"{v.Text}\"",
         NType.Integer => $"    ldc.i4 {v.Text}", 
         NType.Real => $"    ldc.r8 {v.Text}", 
         NType.Bool => $"    ldc.i4 {BoolToInt (v)}",
         NType.Char => $"    ldc.i4 {(int)v.Text[0]}",
         _ => throw new NotImplementedException (),
      });
   }

   void GetIdentifier (Token token) {
      switch (mSymbols.Find (token)) {
         case NConstDecl cd: Visit (cd.Value); break;
         case NVarDecl vd:
            var type = TMap[vd.Type];
            Out ($"{
               (vd.Argument ? $"    ldarg {vd.Name}"
               : (vd.Local ? $"    ldloc {vd.Name}"
               : (vd.StdLib ? $"    call {type} {StdLibName}::get_{vd.Name}()"
               : $"    ldsfld {type} {StdPName}::{vd.Name}")))}");
            break;
         default: throw new NotImplementedException ();
      }
   }

   public override void Visit (NIdentifier d) => GetIdentifier (d.Name);

   public override void Visit (NUnary u) {
      u.Expr.Accept (this);
      string op = u.Op.Kind.ToString ().ToLower ();
      op = op switch { "sub" => "neg", "not" => "ldc.i4.0\n ceq", _ => op };
      Out ($"    {op}");
   }

   public override void Visit (NBinary b) {
      b.Left.Accept (this); b.Right.Accept (this);
      if (b.Left.Type == NType.String) 
         Out ("    call string [System.Runtime]System.String::Concat (string, string)");
      else {
         string op = b.Op.Kind.ToString ().ToLower ();
         op = op switch { 
            "mod" => "rem", "eq" => "ceq", "lt" => "clt", "gt" => "cgt",
            "geq" => "clt\n    ldc.i4.0\n    ceq",
            "leq" => "cgt\n    ldc.i4.0\n    ceq",
            "neq" => "ceq\n    ldc.i4.0\n    ceq",
            _ => op };
         Out ($"    {op}");
      }
   }

   public override void Visit (NFnCall f) => GetCallFN (f.Name, f.Params);

   void GetCallFN (Token token, NExpr[] exprs) {
      Visit (exprs);
      var (fullName, tName, rType) = mSymbols.Find (token) switch {
         NVarDecl vd => (StdPName, vd.Name.Text, TMap[vd.Type]),
         NFnDecl fd => (fd.StdLib ? StdLibName : StdPName, fd.Name.Text, TMap[fd.Return]),
         _ => throw new NotImplementedException ()
      };
      Out ($"    call {rType} {fullName}::{tName} ({exprs.Select (a => TMap[a.Type]).ToCSV ()})");
   }

   public override void Visit (NTypeCast t) {
      t.Expr.Accept (this);
      Out ((t.Expr.Type, t.Type) switch {
         (NType.Integer, NType.Real) => "    conv.r8",
         (NType.Integer, NType.String) => "   call string [PSILib]PSILib.Helper::CIntStr (int32)",
         _ => throw new NotImplementedException ()
      });   
   }

   #region Helper routines ---------------------------------------
   // Helpers ......................................
   // Append a line to output (followed by a \n newline)
   void Out (string s) => S.Append (s).Append ('\n');

   // Append text to output (continuing on the same line)
   void OutC (string s) => S.Append (s);

   // Call Accept on a sequence of nodes
   void Visit (IEnumerable<Node> nodes) {
      foreach (var node in nodes) node.Accept (this);
   }

   int BoolToInt (Token token)
      => token.Text.EqualsIC ("TRUE") ? 1 : 0;

   void LoadVar (Token name) => ReadWriteVar (name, true);
   void StoreVar (Token name) => ReadWriteVar (name, false);

   void ReadWriteVar (Token name, bool read = false) {
      var vd = (NVarDecl)mSymbols.Find (name)!;
      var type = TMap[vd.Type];
      (string local, string global) = read ? ("ldloc", "ldsfld") : ("stloc", "stsfld");
      if (vd.Local) Out ($"    {local} {vd.Name}");
      else Out ($"    {global} {type} {StdPName}::{vd.Name}");
   }
   #endregion

   #region Members -----------------------------------------------
   const string StdLibName = "[PSILib]PSILib.Lib", StdPName = "Program";

   // Dictionary that maps PSI.NType to .Net type names
   static Dictionary<NType, string> TMap = new () {
      [NType.String] = "string", [NType.Integer] = "int32", [NType.Real] = "float64",
      [NType.Bool] = "bool", [NType.Char] = "char", [NType.Void] = "void",
   };
   #endregion
}