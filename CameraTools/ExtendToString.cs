using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KSP.Localization;

namespace CameraTools
{
    public static class ExtendToString
    {
        private static string StationaryCamera = Localizer.GetStringByTag("#CAMTOOL_GUI_StationaryCamera");
        private static string DogfightCamera = Localizer.GetStringByTag("#CAMTOOL_GUI_DogfightCamera");
        private static string Pathing = Localizer.GetStringByTag("#CAMTOOL_GUI_PathingCamera");
        public static string ToFriendString(ToolModes toolMode)
        {
            switch (toolMode)
            {
                case ToolModes.StationaryCamera:
                    return StationaryCamera;
                case ToolModes.DogfightCamera:
                    return DogfightCamera;
                case ToolModes.Pathing:
                    return Pathing;
                default:
                    return toolMode.ToString();
            }
        }
    }
}
