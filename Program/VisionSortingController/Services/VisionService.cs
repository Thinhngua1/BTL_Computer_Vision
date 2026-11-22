using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VisionSortingController.Models;

namespace VisionSortingController.Services
{
    // Vision Service: Provides simulation frame generator + TCP listener hook for Quoc's YOLO module
    public class VisionService : IDisposable
    {
        private TcpListener _tcpListener;
        private CancellationTokenSource _cts;

        // Bottom-up signals (Rule 5.1.B)
        public event Action<VisionDetectionResult> OnDetectionReady;
        public event Action<string> OnVisionLog;

        // Start a lightweight TCP server (default 127.0.0.1:5050) so Quoc's Python YOLO script can send "BLACK,0.96,210,120,160,180"
        public void StartTcpHook(int port = 5050)
        {
            try
            {
                StopTcpHook();
                _cts = new CancellationTokenSource();
                _tcpListener = new TcpListener(IPAddress.Any, port);
                _tcpListener.Start();
                if (OnVisionLog != null)
                    OnVisionLog(string.Format("Vision TCP Hook listening on port {0} (Format: COLOR,CONF,X,Y,W,H)", port));

                Task.Run(async () =>
                {
                    while (_cts != null && !_cts.IsCancellationRequested)
                    {
                        try
                        {
                            var client = await _tcpListener.AcceptTcpClientAsync();
                            var unused = Task.Run(() => HandleClient(client));
                        }
                        catch (ObjectDisposedException) { break; }
                        catch { }
                    }
                });
            }
            catch (Exception ex)
            {
                if (OnVisionLog != null)
                    OnVisionLog("Vision TCP Hook error: " + ex.Message);
            }
        }

        private void HandleClient(TcpClient client)
        {
            using (client)
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    ParseExternalPayload(line.Trim());
                }
            }
        }

        // Parse string payload from Python YOLO: e.g. "BLACK" or "BLUE,0.95,220,110,150,180"
        public void ParseExternalPayload(string payload)
        {
            if (string.IsNullOrWhiteSpace(payload)) return;

            string[] parts = payload.Split(',');
            string colorStr = parts[0].Trim().ToUpperInvariant();

            WorkpieceColor color = WorkpieceColor.None;
            if (colorStr == "BLACK" || colorStr == "1" || colorStr == "M10")
                color = WorkpieceColor.Black;
            else if (colorStr == "BLUE" || colorStr == "2" || colorStr == "M11")
                color = WorkpieceColor.Blue;
            else if (colorStr == "WHITE" || colorStr == "3" || colorStr == "M12")
                color = WorkpieceColor.White;

            if (color == WorkpieceColor.None) return;

            float conf = 0.96f;
            float parsedConf;
            if (parts.Length > 1 && float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out parsedConf))
                conf = parsedConf;

            Rectangle bbox = new Rectangle(220, 105, 190, 195);
            int bx, by, bw, bh;
            if (parts.Length >= 6 &&
                int.TryParse(parts[2], out bx) &&
                int.TryParse(parts[3], out by) &&
                int.TryParse(parts[4], out bw) &&
                int.TryParse(parts[5], out bh))
            {
                bbox = new Rectangle(bx, by, bw, bh);
            }

            TriggerDetection(color, conf, bbox, "YOLO External (TCP)");
        }

        // Called either by Simulation buttons or External YOLO hook
        public void TriggerDetection(WorkpieceColor color, float confidence, Rectangle? customBbox = null, string source = "Simulation")
        {
            Rectangle bbox = customBbox.HasValue ? customBbox.Value : new Rectangle(230, 95, 180, 200);
            Bitmap frame = RenderSyntheticVisionFrame(color, confidence, bbox, source);

            var result = new VisionDetectionResult
            {
                DetectedColor = color,
                Confidence = confidence,
                BoundingBox = bbox,
                AnnotatedFrame = frame,
                Source = source,
                Timestamp = DateTime.Now
            };

            if (OnDetectionReady != null)
                OnDetectionReady(result);
        }

        // Load custom test image file and overlay YOLO bounding box
        public void ProcessImageFile(string imagePath, WorkpieceColor simulatedColor = WorkpieceColor.Blue)
        {
            try
            {
                using (var loaded = new Bitmap(imagePath))
                {
                    Bitmap canvas = new Bitmap(640, 400);
                    using (Graphics g = Graphics.FromImage(canvas))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(loaded, 0, 0, 640, 400);
                        Rectangle bbox = new Rectangle(190, 80, 240, 240);
                        DrawYoloOverlay(g, simulatedColor, 0.94f, bbox, "Image File Test");
                    }

                    var result = new VisionDetectionResult
                    {
                        DetectedColor = simulatedColor,
                        Confidence = 0.94f,
                        BoundingBox = new Rectangle(190, 80, 240, 240),
                        AnnotatedFrame = canvas,
                        Source = Path.GetFileName(imagePath),
                        Timestamp = DateTime.Now
                    };
                    if (OnDetectionReady != null)
                        OnDetectionReady(result);
                }
            }
            catch (Exception ex)
            {
                if (OnVisionLog != null)
                    OnVisionLog("Failed to load image: " + ex.Message);
            }
        }

        public Bitmap RenderIdleFrame()
        {
            return RenderSyntheticVisionFrame(WorkpieceColor.None, 0f, Rectangle.Empty, "Standby / Waiting ROI Trigger");
        }

        private Bitmap RenderSyntheticVisionFrame(WorkpieceColor color, float confidence, Rectangle bbox, string source)
        {
            Bitmap bmp = new Bitmap(640, 400);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.FromArgb(24, 28, 36));

                // Draw Conveyor belt top-view background
                using (var beltBrush = new SolidBrush(Color.FromArgb(42, 48, 58)))
                {
                    g.FillRectangle(beltBrush, 0, 75, 640, 250);
                }
                using (var edgePen = new Pen(Color.FromArgb(90, 100, 115), 3))
                {
                    g.DrawLine(edgePen, 0, 75, 640, 75);
                    g.DrawLine(edgePen, 0, 325, 640, 325);
                }

                // Draw Camera ROI dashed zone
                Rectangle roi = new Rectangle(170, 55, 300, 290);
                using (var roiPen = new Pen(Color.FromArgb(140, 0, 210, 255), 1.5f) { DashStyle = DashStyle.Dash })
                {
                    g.DrawRectangle(roiPen, roi);
                }
                using (var fontSmall = new Font("Consolas", 9f, FontStyle.Bold))
                using (var roiBrush = new SolidBrush(Color.FromArgb(180, 0, 210, 255)))
                {
                    g.DrawString("CAMERA ROI ZONE (640x400)", fontSmall, roiBrush, 175, 36);
                    g.DrawString(string.Format("SOURCE: {0} | {1:HH:mm:ss.fff}", source.ToUpper(), DateTime.Now), fontSmall, Brushes.Silver, 14, 12);
                }

                if (color != WorkpieceColor.None)
                {
                    Color bodyColor = Color.Gray;
                    if (color == WorkpieceColor.Black) bodyColor = Color.FromArgb(35, 35, 40);
                    else if (color == WorkpieceColor.Blue) bodyColor = Color.FromArgb(25, 118, 210);
                    else if (color == WorkpieceColor.White) bodyColor = Color.FromArgb(238, 242, 246);

                    Rectangle cylRect = new Rectangle(bbox.X + 25, bbox.Y + 25, bbox.Width - 50, bbox.Height - 50);
                    using (var bodyBrush = new SolidBrush(bodyColor))
                    using (var borderPen = new Pen(color == WorkpieceColor.Black ? Color.LightGray : Color.Black, 2f))
                    {
                        g.FillEllipse(bodyBrush, cylRect);
                        g.DrawEllipse(borderPen, cylRect);
                    }

                    DrawYoloOverlay(g, color, confidence, bbox, source);
                }
                else
                {
                    using (var waitFont = new Font("Segoe UI", 12f, FontStyle.Italic))
                    {
                        g.DrawString("Waiting for workpiece / YOLO frame...", waitFont, Brushes.Gray, 195, 185);
                    }
                }
            }
            return bmp;
        }

        private void DrawYoloOverlay(Graphics g, WorkpieceColor color, float confidence, Rectangle bbox, string source)
        {
            Color boxColor = Color.Lime;
            if (color == WorkpieceColor.Black) boxColor = Color.FromArgb(255, 193, 7);
            else if (color == WorkpieceColor.Blue) boxColor = Color.FromArgb(0, 229, 255);
            else if (color == WorkpieceColor.White) boxColor = Color.FromArgb(76, 175, 80);

            using (var boxPen = new Pen(boxColor, 3f))
            {
                g.DrawRectangle(boxPen, bbox);
            }

            string label = string.Format("YOLO: {0} {1:F1}%", color.ToString().ToUpper(), confidence * 100);
            using (var labelFont = new Font("Segoe UI", 10f, FontStyle.Bold))
            {
                SizeF textSize = g.MeasureString(label, labelFont);
                RectangleF tagRect = new RectangleF(bbox.X, Math.Max(0, bbox.Y - textSize.Height - 6), textSize.Width + 12, textSize.Height + 4);
                using (var tagBrush = new SolidBrush(boxColor))
                {
                    g.FillRectangle(tagBrush, tagRect);
                }
                g.DrawString(label, labelFont, Brushes.Black, tagRect.X + 6, tagRect.Y + 2);
            }
        }

        public void StopTcpHook()
        {
            try
            {
                if (_cts != null) _cts.Cancel();
                if (_tcpListener != null) _tcpListener.Stop();
            }
            catch { }
        }

        public void Dispose()
        {
            StopTcpHook();
        }
    }
}
