import json;
import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs


class FcDDataReader(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self, FcD: System.Collections.Generic.IList[object]):
        """Grasshopper Script"""
        import json;
        
        VoidRate = 0
        bluebrick = 0
        yellowbrick = 0
        normalbrick = 0
        
        for i in FcD:
            VoidRate += json.loads(i)["PatternRate"]["voidrate"] / len(FcD);
            bluebrick += json.loads(i)["PatternRate"]["bluebrick"] / len(FcD);
            yellowbrick += json.loads(i)["PatternRate"]["yellowbrick"] / len(FcD);
            normalbrick += json.loads(i)["PatternRate"]["normalbrick"] / len(FcD);
        
        
        VoidRate = str(round(VoidRate * 100) / 100) + "%"
        return VoidRate, bluebrick, yellowbrick, normalbrick
