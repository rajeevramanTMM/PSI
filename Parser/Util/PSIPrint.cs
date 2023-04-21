// ⓅⓈⒾ  ●  Pascal Language System  ●  Academy'23
// PSIPrint.cs ~ Prints a PSI syntax tree in Pascal format
// ─────────────────────────────────────────────────────────────────────────────
namespace PSI;

public class PSIPrint : Visitor<StringBuilder> {
   public override StringBuilder Visit (NProgram p) {
      Write ($"program {p.Name}; ");
      Visit (p.Block);
      return Write (".");
   }

   public override StringBuilder Visit (NBlock b) 
      => Visit (b.Decls, b.Body);

   public override StringBuilder Visit (NDeclarations d) {
      if (d.Vars.Length > 0) {
         NWrite ("var"); N++;
         foreach (var g in d.Vars.GroupBy (a => a.Type))
            NWrite ($"{g.Select (a => a.Name).ToCSV ()} : {g.Key};");
         N--;
      }
      if (d.Funcs.Any ()) d.Funcs.ForEach (f => Visit (f));
      if (d.Proc.Any ()) d.Proc.ForEach (p => Visit (p));
         return S;
   }

   public override StringBuilder Visit (NVarDecl d)
      => NWrite ($"{d.Name} : {d.Type}");

   public override StringBuilder Visit (NCompoundStmt b) {
      NWrite ("begin"); N++;  Visit (b.Stmts); N--; return NWrite ("end"); 
   }

   public override StringBuilder Visit (NAssignStmt a) {
      NWrite ($"{a.Name} := "); a.Expr.Accept (this); return Write (";");
   }

   public override StringBuilder Visit (NWriteStmt w) {
      NWrite (w.NewLine ? "WriteLn (" : "Write (");
      for (int i = 0; i < w.Exprs.Length; i++) {
         if (i > 0) Write (", ");
         w.Exprs[i].Accept (this);
      }
      return Write (");");
   }

   public override StringBuilder Visit (NLiteral t)
      => Write (t.Value.ToString ());

   public override StringBuilder Visit (NIdentifier d)
      => Write (d.Name.Text);

   public override StringBuilder Visit (NUnary u) {
      Write (u.Op.Text); return u.Expr.Accept (this);
   }

   public override StringBuilder Visit (NBinary b) {
      Write ("("); b.Left.Accept (this); Write ($" {b.Op.Text} ");
      b.Right.Accept (this); return Write (")");
   }

   public override StringBuilder Visit (NFnCall f) {
      Write ($"{f.Name} (");
      for (int i = 0; i < f.Params.Length; i++) {
         if (i > 0) Write (", "); f.Params[i].Accept (this);
      }
      return Write (")");
   }

   StringBuilder Visit (params Node[] nodes) {
      nodes.ForEach (a => a.Accept (this));
      return S;
   }

   // Writes in a new line
   StringBuilder NWrite (string txt) 
      => Write ($"\n{new string (' ', N * 3)}{txt}");
   int N;   // Indent level

   // Continue writing on the same line
   StringBuilder Write (string txt) {
      Console.Write (txt);
      S.Append (txt);
      return S;
   }

   public override StringBuilder Visit (NReadStmt r) {
      NWrite ("Read (");
      r.List.ForEach (x => Write (x.Text));
      return Write (");");
   }

   public override StringBuilder Visit (NCallStmt c) {
      NWrite (c.Name.Text);
      Write (" (");
      c.Expr.ForEach (x => x.Accept (this));
      return Write (");");
   }

   public override StringBuilder Visit (NIfStmt ni) {
      NWrite ("If ");
      Visit (ni.Expr);
      Write (" Then");
      if (ni.Stmts.Length >= 1) Visit (ni.Stmts[0]);
      if (ni.Stmts.Length >= 2) { NWrite ("else "); Visit (ni.Stmts[1]); }
      return (Write (""));
   }

   public override StringBuilder Visit (NWhileStmt w) {
      NWrite ("While ");
      Visit (w.Expr);
      Write (" do");
      N++;
      for (int i = 0; i < w.Stmts.Length; i++)
         Visit (w.Stmts[i]);
      N--;
      return Write (";");
   }

   public override StringBuilder Visit (NRepeatStmt r) {
      NWrite ("Repeat");
      N++; Visit (r.Stmts); N--;
      NWrite ("Until ");
      return Visit (r.Expr);
   }

   public override StringBuilder Visit (NForStmt nf) {
      NWrite ($"for {nf.Name.Text} := ");
      Visit (nf.Exprs.Expr);
      Write (nf.To ? " To " : " Downto ");
      Visit (nf.Exprs.Expr1);
      Write (" do");
      N++;
      Visit (nf.Stmt);
      N--;
      return Write ("");
   }

   public override StringBuilder Visit (NProcDecl p) {
      NWrite ($"procedure {p.Name.Text} := ");
      foreach (var g in p.Vars.GroupBy (a => a.Type))
         Write ($" ({g.Select (a => a.Name).ToCSV ()} : {g.Key});");
      Visit (p.Block);
      return Write (";");
   }

   public override StringBuilder Visit (NFnDecl n) {
      NWrite ("function ");
      Write (n.Name.Text);
      foreach (var g in n.Vars.GroupBy (a => a.Type))
         Write ($" ({g.Select (a => a.Name).ToCSV ()} : {g.Key});");
      Visit (n.Block);
      return Write (";");
   }

   readonly StringBuilder S = new ();
}