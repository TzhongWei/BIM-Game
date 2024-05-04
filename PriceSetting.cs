// Grasshopper Script Instance
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
	IList<string> FcD,
	IList<double> UnitScale,
	double AveragePrice,
	IList<double> PriceWeight,
	ref object Data,
	ref object FacadeTotalPrice)
    {   
        var Bricks = BrickFromJson(FcD);
        var Total = Bricks[0] + Bricks[1] + Bricks[2];
        var Rate = AdjustRatio(UnitScale);
  
        var EightUnit = Math.Round(Rate[0], 2 ) != 0 ? (int) Math.Round( Total * Rate[0] / 800.0 ) : 0;
        var SixUnit = Math.Round(Rate[1], 2 ) != 0 ? (int) Math.Round( Total * Rate[1] / 600.0 ) : 0;
        var TwoUnit = Math.Round(Rate[2], 2 ) != 0 ? (int) Math.Round( Total * Rate[2] / 200.0 ) : 0;

        if(EightUnit * 8 + SixUnit * 6 + TwoUnit * 2 < Total)
        {
            TwoUnit += (Total - (EightUnit * 8 + SixUnit * 6 + TwoUnit * 2)) / 2;
        }

        double EightPrice = AveragePrice * PriceWeight[0];
        double SixPrice = AveragePrice * PriceWeight[1];
        double TwoPrice = AveragePrice * PriceWeight[2];
        
        FacadeTotalPrice = Math.Round(EightPrice * EightUnit + SixPrice * SixUnit + TwoPrice * TwoUnit);
        var JsonObj = new Dictionary<string, object>{
            {"TotalUnitCount", Total},
            {"Ratio", Rate},
            {"Price", new Dictionary<string, double>(){
                {"EightUnit_AveragePrice", EightPrice},
                {"SixUnit_AveragePrice", SixPrice}, 
                {"TwoUnit_AveragePrice", TwoPrice}
            }
            },
            {"UnitCount", new Dictionary<string, int>(){
                    {"EightUnit", EightUnit},
                    {"SixUnit", SixUnit}, 
                    {"TwoUnit", TwoUnit}
            }}
        };
        var JsonString = JsonSerializer.Serialize(JsonObj, new JsonSerializerOptions(){WriteIndented = true});
        Data = JsonString;
    }
    public int[] BrickFromJson(IEnumerable<string> FcD)
    {
        int bluebrickAmount = 0;
        int yellowbrickAmount = 0;
        int normalbrickAmount = 0;
        foreach (var item in FcD)
        {
            var FcDData = JsonSerializer.Deserialize<Dictionary<string, object>>(item);
            var BrickData = JsonSerializer.Deserialize<Dictionary<string, int>>(FcDData["BrickPatternNumber"].ToString());
            bluebrickAmount += BrickData["bluebrick"];
            yellowbrickAmount += BrickData["yellowbrick"];
            normalbrickAmount += BrickData["normalbrick"];
        }
        return new int[3]{bluebrickAmount, yellowbrickAmount, normalbrickAmount};
    }
    public double[] AdjustRatio(IEnumerable<double> Ratio)
    {
        var RatioArr = Ratio.ToArray();
        if(Ratio.ToArray().Length != 3)
            return new double[3]{0,0,100};
        else if(RatioArr[0] + RatioArr[1] + RatioArr[2] > 100)
            return new double[3]{0,0,100};
        else if (RatioArr[0] + RatioArr[1] + RatioArr[2] == 100)
            return RatioArr;
        else
        {
            int IsZero = 0;
            var Result = new double[3];
            for(int i = 0; i < 3; i++)
            {
                if(RatioArr[i] != 0)
                    Result[i] = RatioArr[i];
                else
                    IsZero ++;
            }
            if(IsZero == 0 || ((IsZero == 1 || IsZero == 2) && RatioArr[2] == 0))
                Result[2] = 100 - RatioArr[0] - RatioArr[1];
            else if(IsZero == 1 || IsZero == 2)
            {
                for(int i = 0 ; i < 3 ; i++)
                {
                    if(RatioArr[i] == 0)
                    {
                        Result[i] = 100 - RatioArr[(i + 1) % 3] - RatioArr[(i + 2) % 3];
                        break;
                    }
                }
            }
            else
                Result = new double[3]{0,0,100};
            return Result;
        }

    }
}
