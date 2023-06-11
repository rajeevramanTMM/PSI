program Comp14;

var
   i, j, k: integer;

begin
  for i := 1 to 20 do 
  begin
        WriteLn();
        Write ("Value of i: ",i);
	  for j := 2 to 10 do 
	     begin
		     WriteLn();
             Write("Value of j: ",j);
            for k := 3 to 8 do
                begin
                    WriteLn();
                    Write("Value of k: ",k);
                        if (k > 2) then
						    break 3;
				end;
            end;
    end; 
WriteLn();	
end.

