#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Text.Json;

using Rhino;
using Rhino.Geometry;

using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
#endregion

public class Script_Instance : GH_ScriptInstance
{
    #region Notes
    /* 
      Members:
        RhinoDoc RhinoDocument
        GH_Document GrasshopperDocument
        IGH_Component Component
        int Iteration

      Methods (Virtual & overridable):
        Print(string text)
        Print(string format, params object[] args)
        Reflect(object obj)
        Reflect(object obj, string method_name)
    */
    #endregion

    private void RunScript(
	int Index,
	double VoidRate,
	double BlueBrick,
	double YellowBrick,
	double NormalBrick,
	ref object FacadeData)
    {
        // Write your logic here
        var Result = new Dictionary<string, double>();
        if(VoidRate + BlueBrick + YellowBrick + NormalBrick == 100)
        {
            Result = new Dictionary<string, double>(){
            {"voidrate", VoidRate},
            {"bluebrick", BlueBrick},
            {"yellowbrick", YellowBrick},
            {"normalbrick", NormalBrick}
        };
        }
        else if (VoidRate + BlueBrick + YellowBrick + NormalBrick > 100)
        {
            this.Component.AddRuntimeMessage(GH_RuntimeMessageLevel.Error,  "The Sum of the rates cannot larger than 100");
        
        }
        else
        {
            var Rates = RateCalculator(new double[4]{VoidRate, BlueBrick, YellowBrick, NormalBrick});
            
            Result = new Dictionary<string, double>(){
            {"voidrate", Math.Round(Rates[0], 6)},
            {"bluebrick", Math.Round(Rates[1], 6)},
            {"yellowbrick", Math.Round(Rates[2], 6)},
            {"normalbrick", Math.Round(Rates[3], 6)}
        };
        }
        
        
        var Data = JsonFormat.Set(Index, Result);
        FacadeData = Data.ToJson();
    }

    public double[] RateCalculator(double[] Rates)
    {
        var Result = new double[4];
        var BoolPattern = new bool[4]
        {IsZero(Rates[0]), IsZero(Rates[1]), IsZero(Rates[2]), IsZero(Rates[3])};
        int ValueAmount = 0;
        for (int i = 0; i < 4; i++)
        {   
            if(!BoolPattern[i])
                Result[i] = Rates[i];
            else
                ValueAmount++;
        }

        if(ValueAmount == 4 || 
        ((ValueAmount == 3 || 
        ValueAmount == 1) && BoolPattern[3]) || 
        ValueAmount == 0)
            Result[3] = 100 - (Result[2] + Result[1] + Result[0]);
        else if(ValueAmount == 3 || (ValueAmount == 2 && BoolPattern[3]))
            Result[3] = 100 - (Result[2] + Result[1] + Result[0]);
        else if((ValueAmount == 2 && !BoolPattern[3]) || (ValueAmount == 1))
        {
            
            for(int i = 0; i < 4; i++)
            {
                if(BoolPattern[i])
                {
                    Result[i] = 100 - (Result[(i+1) % 4] + Result[(i+2) % 4] + Result[(i+3) % 4]);
                    break;
                }
            }
        }
    
        return Result;
        bool IsZero(double Number) => Number == 0;
    }



}

public class JsonFormat
{
    public int Index {get;set;}
    public Dictionary<string, double> PatternRate {get; set;}
    //public Dictionary<string, int> BrickPatternNumber {get; set;}
    private JsonFormat(){}
    public static JsonFormat CreatFromJson(string Json)
        => JsonSerializer.Deserialize<JsonFormat>(Json);
    public static JsonFormat Set(int _Index, Dictionary<string, double> _PatternRate)
    {
        return new JsonFormat(){Index = _Index, PatternRate = _PatternRate};
    }
    public string ToJson()
     => JsonSerializer.Serialize(this, new JsonSerializerOptions(){WriteIndented = true});

}
