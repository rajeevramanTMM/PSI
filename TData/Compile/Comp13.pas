program Comp13;

var name :string;
width,height,area :real;
isFiller :boolean;
qty: integer;

begin
   write ("Enter part name:");
   readLn (name);
   write ("Enter width:");
   readln(width);
   write ("Enter height:");
   readln(height);
   write ("Quantity:");
   readLn(qty);
   write("Filler (true or false):");
   readLn(isFiller);
   writeln("------------------------------------");
   writeln("Part details");
   writeln("Name: ",name);
   writeln ("Width: ",width);
   writeln("Height: ",height);
   writeln("Quantity: ",qty);
   area := width * height;
   writeln("Area: ",area);
   writeln("Total area: ",area * qty);
   writeln("Filler: ",isFiller);
end.

