// Grasshopper Script Instance
#region Usings
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Drawing;

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
	IList<Brep> Mass,
	IList<string> DataSetting,
	ref object Building,
	ref object Preview,
	ref object FcD)
    {
        // Write your logic here
        var union = Union(Mass);
        var WallList = Walls(union);
        var FacadeData_1 = new FacadeSetting(WallList);
        FacadeData_1.Setting(DataSetting);
        FcD = FacadeData_1.Print();
        Preview = WallList;
        Building = union;
    }
    public Brep Union(IEnumerable<Brep> Massive)
    {
        var Union = Brep.CreateBooleanUnion(Massive, 0.1);
        if(Union.Length != 1)
         throw new Exception("Union Failed");

         Union[0].MergeCoplanarFaces(0.1);
         return Union[0];
    }
    public List<Brep> Walls(Brep Mass)
    {
        var BrepFaces = Mass.Faces;
        var WallList = new List<Brep>();
        foreach (var face in BrepFaces)
        {
            if(!face.IsPlanar())
                WallList.Add(face.DuplicateFace(false));

            var DomU = face.Domain(0);
            var DomV = face.Domain(1);
            
            var Normal = face.NormalAt(DomU.Mid,DomV.Mid);
            if(Math.Abs(Normal * Vector3d.ZAxis) == 1
             || !face.IsPlanar() ) continue;
            WallList.Add(face.DuplicateFace(false));
        }
        return WallList;
    }
}

public class FacadeSetting
{
    private FacadeData[] _DataSetting;
    public FacadeSetting(List<Brep> facade)
    {
        int Index = 0;
        _DataSetting = facade.Select(x => {var Data = new FacadeData(x, Index); Index++; return Data;}).ToArray();
    }
    public List<string> Print()
    => this._DataSetting.Select(x => x.ToJson()).ToList();

    public void Setting(IEnumerable<string> DataSetting)
    {
        var Format = DataSetting.Select(x => JsonFormat.CreatFromJson(x)).ToList();
        foreach (var item in Format)
        {
            this._DataSetting[item.Index].SetRate(item.PatternRate);
        }
    }
}

public class JsonFormat
{
    public int Index {get;set;}
    public double Area {get; set;}
    public int BrickCount {get; set;}
    public Dictionary<string, double> PatternRate {get; set;}
    public Dictionary<string, int> BrickPatternNumber {get; set;}
    private JsonFormat()
    {
        PatternRate = new Dictionary<string, double>()
        {
            {"voidrate", 0},
            {"bluebrick", 0},
            {"yellowbrick", 0},
            {"normalbrick", 0}
        };
        BrickPatternNumber = new Dictionary<string, int>(){
            {"voidrate", 0},
            {"bluebrick", 0},
            {"yellowbrick", 0},
            {"normalbrick", 0}
        };
    }
    public static JsonFormat CreatFromJson(string Json)
    {
        Dictionary<string, object> Data = 
         JsonSerializer.Deserialize<Dictionary<string, object>>(Json);

        var JsonForm = new JsonFormat();

        if(!Data.ContainsKey("Index"))
            throw new Exception("No index setting");

        foreach (var item in Data)
        {
            switch (item.Key)
            {
                case "Index":
                    JsonForm.Index = int.Parse(item.Value.ToString()) ;
                    break;
                case "Area":
                    JsonForm.Area = double.Parse(item.Value.ToString());
                    break;
                case "BrickCount":
                    JsonForm.BrickCount = int.Parse(item.Value.ToString());
                    break;
                case "PatternRate":
                    var PatDic = JsonSerializer.Deserialize<Dictionary<string, double>>(item.Value.ToString());
                    JsonForm.PatternRate = PatDic;
                    break;
                case "BrickPatternNumber":
                    var BriDic = JsonSerializer.Deserialize<Dictionary<string, int>>(item.Value.ToString());
                    JsonForm.BrickPatternNumber = BriDic;
                    break;
                default:
                    throw new Exception("Label cannot be recognised");
            }
        }

        return JsonForm;
    }
    public JsonFormat(FacadeData Data)
    {
        this.Index = Data.Index;
        this.Area = Data.Area;
        this.BrickCount = Data.BrickCount;
        this.PatternRate = Data._PatternRate;
        this.BrickPatternNumber = Data.BrickPatternNumber;
    }
    public string ToJson()
     => JsonSerializer.Serialize(this, new JsonSerializerOptions(){WriteIndented = true});

}

public class FacadeData
{
    public string ToJson()
        => (new JsonFormat(this)).ToJson();

    public static double UnitSize = 2.5;
    private Brep Face;
    public double Area => Face.GetArea();
    public Dictionary<string, double> _PatternRate{get; private set;}
    public Dictionary<string, int> BrickPatternNumber
    {
        get
        {
            var brickPatternCount = new Dictionary<string, int>();
            int Total = 0;
            foreach (var Kvp in _PatternRate)
            {
                if(Kvp.Key == "normalbrick") continue;
                var Number = (int) Math.Round(BrickCount * Kvp.Value / 100);
                brickPatternCount.Add(Kvp.Key, Number);
                Total += Number;
            }
            brickPatternCount["normalbrick"] =  BrickCount - Total;
            return brickPatternCount;
        }
    }
    public int BrickCount {get; private set;}
    public int Index {get;}
    public FacadeData(Brep face, int index = 0)
    {
        this.Index = index;
        Face = face;
        this.BrickCount = _BrickCount();
        _PatternRate = new Dictionary<string, double>();
        _PatternRate.Add("voidrate", 0);
        _PatternRate.Add("bluebrick", 0);
        _PatternRate.Add("yellowbrick", 0);
        _PatternRate.Add("normalbrick", 100);
    }
    public void SetRate(Dictionary<string, double> PatternRate)
    {
        this._PatternRate = PatternRate;
    }
    public void SetRate(string Name, double Rate)
    {
        if(_PatternRate.ContainsKey(Name.ToLower()))
        {
            if(Rate >= 100)
                throw new Exception("Rate Setting invalid");
            else if(this._PatternRate[Name.ToLower()] != 0)
            {
                var OldValue = this._PatternRate[Name.ToLower()];
                this._PatternRate[Name.ToLower()] = 0;
                this._PatternRate["normalbrick"] += OldValue;
                this.SetRate(Name.ToLower(), Rate);
            }
            else if(RateCorrectSetting(Rate))
            {
                this._PatternRate[Name.ToLower()] = Rate;
                this._PatternRate["normalbrick"] -= Rate;
            }
            else
                throw new Exception("Rate Setting invalid. Some parameter need to be reset.");
        }
        else
            throw new Exception("The provided label isn't existed");
        bool RateCorrectSetting(double Rate)
        {
            if ( this._PatternRate["normalbrick"] - Rate < 0)
                return false;
            var Values = this._PatternRate.Values.ToList();
            double Final = 0;
            for(int i = 0; i < Values.Count; i++)
            {
                Final += Values[i];
            }
            Final += Rate;
            return Rate > 100 ? false : true;
        }
    }
    private int _BrickCount()
        => (int) Math.Round(Area / (UnitSize * UnitSize));
}

