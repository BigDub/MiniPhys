using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;

namespace WindowsGame
{
    class F
    {
        public static String TrimFloat(float flTrim, int dec)
        {
            String toTrim = "";
            if (flTrim >= 0)
            {
                toTrim = "+";
            }
            toTrim += flTrim.ToString();
            if (toTrim.Length > dec + 2)
            {
                toTrim = toTrim.Substring(0, dec + 2);
            }
            else
            {
                Boolean b = true;
                while (toTrim.Length < dec + 2)
                {
                    if ((float)(int)flTrim == flTrim && b)
                    {
                        toTrim += ".";
                        b = false;
                    }
                    toTrim = toTrim + "0";
                }
            }
            return (toTrim);
        }
        public static String StringVector3(Vector3 target, int dec)
        {
            return ("{X:" + TrimFloat(target.X, dec) + " Y:" + TrimFloat(target.Y, dec) + " Z:" + TrimFloat(target.Z, dec) + "}");
        }
    }
}
