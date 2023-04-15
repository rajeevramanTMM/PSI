using System.Xml.Linq;

namespace PSI;

public class ExprXML : Visitor<XElement> {

   public ExprXML (string expression)
      => mExpression = expression;

   public override XElement Visit (NLiteral literal) {
      var node = new XElement ("Literal");
      node.SetAttributeValue ("Value", literal.Value.Text);
      node.SetAttributeValue ("Type", literal.Type);
      mLines.AppendLine (node.ToString ());
      return node;
   }

   public override XElement Visit (NIdentifier identifier) {
      var node = new XElement ("Ident");
      node.SetAttributeValue("Name",identifier.Name);
      node.SetAttributeValue ("Type", identifier.Type);
      mLines.AppendLine (node.ToString ());
      return node;
   }

   public override XElement Visit (NUnary unary) {
      var node = new XElement ("Unary");
      node.SetAttributeValue ("Op", unary.Op.Kind);
      node.SetAttributeValue ("Type", unary.Type);
      var node1 = unary.Expr.Accept (this);
      node.Add (node1);
      mLines.Clear (); 
      mLines.Append (node.ToString ());
      return node;
   }

   public override XElement Visit (NBinary binary) {
      var exlem = new XElement ("Binary");
      exlem.SetAttributeValue ("Op", binary.Op.Kind);
      exlem.SetAttributeValue ("Type", binary.Type);
      mLines.AppendLine (exlem.ToString ());
      var a = binary.Left.Accept (this); var b = binary.Right.Accept (this);
      exlem.Add (a); exlem.Add (b);
      mLines.Clear ();
      mLines.Append (exlem.ToString ());
      return exlem;
   }

   public void SaveTo (string file) => File.WriteAllText (file, mLines.ToString ()); 
   
   readonly string mExpression;
   readonly StringBuilder mLines = new ();
}
