using System;
using System.Drawing;

namespace VisionSortingController.Models
{
    public enum WorkpieceColor
    {
        None = 0,
        Black = 1,
        Blue = 2,
        White = 3
    }

    public class PlcStateModel
    {
        public bool IsConnected { get; set; }
        public bool M0_Start { get; set; }
        public bool M1_Stop { get; set; }
        public bool M10_Black { get; set; }
        public bool M11_Blue { get; set; }
        public bool M12_White { get; set; }

        public bool Y0_Conveyor { get; set; }
        public bool Y1_CylinderBlack { get; set; }
        public bool Y2_CylinderBlue { get; set; }

        public int BlackCount { get; set; }
        public int BlueCount { get; set; }
        public int WhiteCount { get; set; }
        public int TotalCount { get { return BlackCount + BlueCount + WhiteCount; } }
    }

    public class VisionDetectionResult
    {
        public WorkpieceColor DetectedColor { get; set; } = WorkpieceColor.None;
        public float Confidence { get; set; }
        public Rectangle BoundingBox { get; set; }
        public Bitmap AnnotatedFrame { get; set; }
        public string Source { get; set; } = "Simulation";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
