import System
import Rhino
import Grasshopper

import rhinoscriptsyntax as rs


class AveragePriceASS(Grasshopper.Kernel.GH_ScriptInstance):
    def RunScript(self,
            AverageBrickRatio: System.Collections.Generic.IList[float],
            BrickPrice: System.Collections.Generic.IList[float]):
        """Grasshopper Script"""
        
        
        AveragePrice = AverageBrickRatio[0] * BrickPrice[0] + AverageBrickRatio[1] * BrickPrice[1] + AverageBrickRatio[2] * BrickPrice[2]
        AveragePrice /= (AverageBrickRatio[0] + AverageBrickRatio[1] + AverageBrickRatio[2])
        
        return AveragePrice
