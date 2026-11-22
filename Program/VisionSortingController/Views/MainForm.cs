using System;
using System.Drawing;
using System.Windows.Forms;
using VisionSortingController.Models;
using VisionSortingController.ViewModels;

namespace VisionSortingController.Views
{
    public class MainForm : Form
    {
        private readonly MainViewModel _viewModel;

        // Top Header
        private Panel _headerPanel;
        private Label _lblTitle;
        private Label _lblConnectionBadge;

        // Left Column: PLC & Conveyor Controls
        private ComboBox _cboPort;
        private ComboBox _cboBaud;
        private ComboBox _cboParity;
        private NumericUpDown _numSlaveId;
        private Button _btnRefreshPorts;
        private Button _btnConnect;

        private Button _btnStartConveyor;
        private Button _btnStopConveyor;
        private Button _btnResetCylinders;

        private Button _btnSimBlack;
        private Button _btnSimBlue;
        private Button _btnSimWhite;

        // PLC Status LEDs
        private Label _ledY0;
        private Label _ledY1;
        private Label _ledY2;

        // Right Column: Vision Panel (Reserved for Quoc)
        private PictureBox _picVision;
        private Label _lblDetectedColor;
        private Label _lblConfidence;
        private Label _lblBBoxInfo;
        private Label _lblVisionSource;
        private Button _btnLoadTestImg;
        private ComboBox _cboTestImgColor;

        // Production Counters
        private Label _lblCountBlack;
        private Label _lblCountBlue;
        private Label _lblCountWhite;
        private Label _lblCountTotal;
        private Button _btnResetCounters;

        // Bottom Log
        private ListBox _lstLog;
        private Timer _pollTimer;

        public MainForm(MainViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeDashboardUi();
            PopulateComPorts();
            _picVision.Image = _viewModel.GetInitialVisionFrame();
        }

        private void InitializeDashboardUi()
        {
            Text = "Industrial Vision & PLC Sorting Controller - Xinje XD2-16T-E (Modbus RTU / YOLO)";
            Size = new Size(1240, 820);
            MinimumSize = new Size(1180, 780);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(240, 244, 248);
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            // 1. HEADER BAR
            _headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 56,
                BackColor = Color.FromArgb(22, 42, 69)
            };
            _lblTitle = new Label
            {
                Text = "HE THONG PHAN LOAI SAN PHAM THEO MAU SAC (YOLO VISION + PLC XINJE XD2-16T-E)",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(18, 15)
            };
            _lblConnectionBadge = new Label
            {
                Text = "OFFLINE (SIMULATION MODE)",
                ForeColor = Color.White,
                BackColor = Color.FromArgb(198, 40, 40),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(230, 32),
                Location = new Point(975, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            _headerPanel.Controls.Add(_lblTitle);
            _headerPanel.Controls.Add(_lblConnectionBadge);
            Controls.Add(_headerPanel);

            // 2. LEFT COLUMN: PLC & CONVEYOR & SIMULATION
            int leftX = 16;
            int leftW = 450;

            // Group 1: Modbus RTU Connection
            GroupBox grpModbus = new GroupBox
            {
                Text = "1. Cau hinh truyen thong PLC (RS485 - Modbus RTU)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(leftX, 68),
                Size = new Size(leftW, 135),
                BackColor = Color.White
            };

            grpModbus.Controls.Add(CreateLabel("COM Port:", 15, 30));
            _cboPort = new ComboBox { Location = new Point(90, 27), Width = 95, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            _btnRefreshPorts = new Button
            {
                Text = "Quet",
                Location = new Point(190, 26),
                Size = new Size(55, 27),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat
            };
            _btnRefreshPorts.Click += (s, e) => PopulateComPorts();

            grpModbus.Controls.Add(CreateLabel("Baudrate:", 255, 30));
            _cboBaud = new ComboBox { Location = new Point(325, 27), Width = 105, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            _cboBaud.Items.AddRange(new object[] { "9600", "19200", "38400", "115200" });
            _cboBaud.SelectedIndex = 1; // 19200 default Xinje

            grpModbus.Controls.Add(CreateLabel("Parity:", 15, 68));
            _cboParity = new ComboBox { Location = new Point(90, 65), Width = 95, DropDownStyle = ComboBoxStyle.DropDownList, Font = new Font("Segoe UI", 9.5f) };
            _cboParity.Items.AddRange(new object[] { "Even", "None", "Odd" });
            _cboParity.SelectedIndex = 0; // Even default Xinje

            grpModbus.Controls.Add(CreateLabel("Slave ID:", 195, 68));
            _numSlaveId = new NumericUpDown { Location = new Point(260, 65), Width = 50, Minimum = 1, Maximum = 247, Value = 1, Font = new Font("Segoe UI", 9.5f) };
            _numSlaveId.ValueChanged += (s, e) => _viewModel.SlaveId = (byte)_numSlaveId.Value;

            _btnConnect = new Button
            {
                Text = "KET NOI PLC",
                Location = new Point(320, 62),
                Size = new Size(110, 32),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnConnect.Click += OnConnectClicked;

            Label lblHint = new Label
            {
                Text = "* Mac dinh PLC Xinje XD2: 19200 bps, 8 Data bits, Even Parity, 1 Stop bit",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Location = new Point(15, 105),
                AutoSize = true
            };

            grpModbus.Controls.AddRange(new Control[] { _cboPort, _btnRefreshPorts, _cboBaud, _cboParity, _numSlaveId, _btnConnect, lblHint });
            Controls.Add(grpModbus);

            // Group 2: Conveyor Control (M0 / M1 / Y0)
            GroupBox grpConveyor = new GroupBox
            {
                Text = "2. Dieu khien Bang tai & Xy lanh (M0 / M1)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(leftX, 212),
                Size = new Size(leftW, 130),
                BackColor = Color.White
            };

            _btnStartConveyor = new Button
            {
                Text = "CHAY BANG TAI\n(M0 -> Latch Y0)",
                Location = new Point(15, 28),
                Size = new Size(135, 54),
                BackColor = Color.FromArgb(46, 125, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnStartConveyor.Click += async (s, e) => await _viewModel.StartConveyorAsync();

            _btnStopConveyor = new Button
            {
                Text = "DUNG HE THONG\n(M1 -> Ngat Y0,Y1,Y2)",
                Location = new Point(158, 28),
                Size = new Size(142, 54),
                BackColor = Color.FromArgb(198, 40, 40),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnStopConveyor.Click += async (s, e) => await _viewModel.StopConveyorAsync();

            _btnResetCylinders = new Button
            {
                Text = "THU XY LANH\n(Reset Y1, Y2)",
                Location = new Point(308, 28),
                Size = new Size(125, 54),
                BackColor = Color.FromArgb(230, 81, 0),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            _btnResetCylinders.Click += async (s, e) => await _viewModel.ResetSortingActuatorsAsync();

            Label lblLadderNote = new Label
            {
                Text = "Ladder PLC.xdp: M0 duy tri Y0 | M10 duy tri Y1 | M11 duy tri Y2 | M1 ngat toan bo",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 70, 85),
                Location = new Point(15, 95),
                AutoSize = true
            };

            grpConveyor.Controls.AddRange(new Control[] { _btnStartConveyor, _btnStopConveyor, _btnResetCylinders, lblLadderNote });
            Controls.Add(grpConveyor);

            // Group 3: Simulate Color States (Black M10 / Blue M11 / White M12)
            GroupBox grpSim = new GroupBox
            {
                Text = "3. Gia lap trang thai phan loai mau (Black / Blue / White)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(leftX, 350),
                Size = new Size(leftW, 130),
                BackColor = Color.White
            };

            _btnSimBlack = new Button
            {
                Text = "BLACK (M10)\nDay Xy lanh 1 (Y1)",
                Location = new Point(15, 30),
                Size = new Size(135, 58),
                BackColor = Color.FromArgb(38, 42, 48),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnSimBlack.Click += (s, e) => _viewModel.SimulateColorTrigger(WorkpieceColor.Black);

            _btnSimBlue = new Button
            {
                Text = "BLUE (M11)\nDay Xy lanh 2 (Y2)",
                Location = new Point(158, 30),
                Size = new Size(135, 58),
                BackColor = Color.FromArgb(21, 101, 192),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnSimBlue.Click += (s, e) => _viewModel.SimulateColorTrigger(WorkpieceColor.Blue);

            _btnSimWhite = new Button
            {
                Text = "WHITE (M12)\nDi thang cuoi bang",
                Location = new Point(301, 30),
                Size = new Size(132, 58),
                BackColor = Color.FromArgb(245, 247, 250),
                ForeColor = Color.FromArgb(30, 30, 30),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold)
            };
            _btnSimWhite.FlatAppearance.BorderColor = Color.Gray;
            _btnSimWhite.FlatAppearance.BorderSize = 2;
            _btnSimWhite.Click += (s, e) => _viewModel.SimulateColorTrigger(WorkpieceColor.White);

            Label lblSimNote = new Label
            {
                Text = "* Bam nut de gia lap ket qua tu Vision -> Tu dong ghi Coil M10/M11/M12 xuong PLC",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Color.DimGray,
                Location = new Point(15, 98),
                AutoSize = true
            };

            grpSim.Controls.AddRange(new Control[] { _btnSimBlack, _btnSimBlue, _btnSimWhite, lblSimNote });
            Controls.Add(grpSim);

            // Group 4: PLC Actuator Status Monitor (Y0, Y1, Y2)
            GroupBox grpStatus = new GroupBox
            {
                Text = "4. Trang thai Ngo ra PLC (Actuator Outputs Y0 / Y1 / Y2)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(leftX, 488),
                Size = new Size(leftW, 105),
                BackColor = Color.White
            };

            _ledY0 = CreateStatusIndicator("Y0: BANG TAI\n(OFF)", 15, 30, 132, 58);
            _ledY1 = CreateStatusIndicator("Y1: XY LANH 1\n(BLACK - OFF)", 158, 30, 132, 58);
            _ledY2 = CreateStatusIndicator("Y2: XY LANH 2\n(BLUE - OFF)", 301, 30, 132, 58);

            grpStatus.Controls.AddRange(new Control[] { _ledY0, _ledY1, _ledY2 });
            Controls.Add(grpStatus);

            // 3. RIGHT COLUMN: VISION SUBSYSTEM (RESERVED FOR QUOC) & COUNTERS
            int rightX = 480;
            int rightW = 730;

            GroupBox grpVision = new GroupBox
            {
                Text = "5. KHU VUC XU LY ANH & NHAN DIEN YOLO (VISION MODULE - QUOC)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(rightX, 68),
                Size = new Size(rightW, 412),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _picVision = new PictureBox
            {
                Location = new Point(15, 26),
                Size = new Size(515, 335),
                BackColor = Color.FromArgb(24, 28, 36),
                SizeMode = PictureBoxSizeMode.Zoom,
                BorderStyle = BorderStyle.FixedSingle
            };

            // Vision Side Info Box
            Panel pnlVisionInfo = new Panel
            {
                Location = new Point(540, 26),
                Size = new Size(175, 335),
                BackColor = Color.FromArgb(248, 250, 252),
                BorderStyle = BorderStyle.FixedSingle
            };

            Label lblVisHeader = new Label
            {
                Text = "KET QUA YOLO",
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                ForeColor = Color.FromArgb(22, 42, 69),
                Location = new Point(10, 10),
                AutoSize = true
            };

            _lblDetectedColor = new Label
            {
                Text = "WAITING",
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                BackColor = Color.FromArgb(226, 232, 240),
                ForeColor = Color.FromArgb(51, 65, 85),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 38),
                Size = new Size(153, 46)
            };

            _lblConfidence = new Label
            {
                Text = "Confidence: -- %",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(10, 98),
                AutoSize = true
            };

            _lblBBoxInfo = new Label
            {
                Text = "BBox: [X:--, Y:--]\nSize: [W:--, H:--]",
                Font = new Font("Consolas", 8.75f, FontStyle.Regular),
                Location = new Point(10, 126),
                Size = new Size(155, 42)
            };

            _lblVisionSource = new Label
            {
                Text = "Nguon: Standby\nTCP Hook: Port 5050",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Regular),
                ForeColor = Color.DimGray,
                Location = new Point(10, 174),
                Size = new Size(155, 42)
            };

            Label lblTestImg = new Label
            {
                Text = "Test anh tinh (Demo):",
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Location = new Point(10, 228),
                AutoSize = true
            };

            _cboTestImgColor = new ComboBox
            {
                Location = new Point(10, 250),
                Width = 153,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9f)
            };
            _cboTestImgColor.Items.AddRange(new object[] { "Gan nhan: BLACK", "Gan nhan: BLUE", "Gan nhan: WHITE" });
            _cboTestImgColor.SelectedIndex = 1;

            _btnLoadTestImg = new Button
            {
                Text = "Tai Anh Test BBox...",
                Location = new Point(10, 284),
                Size = new Size(153, 36),
                BackColor = Color.FromArgb(55, 71, 79),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold)
            };
            _btnLoadTestImg.Click += OnLoadTestImageClicked;

            pnlVisionInfo.Controls.AddRange(new Control[]
            {
                lblVisHeader, _lblDetectedColor, _lblConfidence, _lblBBoxInfo,
                _lblVisionSource, lblTestImg, _cboTestImgColor, _btnLoadTestImg
            });

            Label lblVisionHookHint = new Label
            {
                Text = "* Tich hop cho Quoc: (1) Goi VisionService.TriggerDetection(color, conf, bbox) hoac (2) Gui chuoi 'BLACK,0.96,x,y,w,h' qua TCP 127.0.0.1:5050",
                Font = new Font("Segoe UI", 8.25f, FontStyle.Italic),
                ForeColor = Color.FromArgb(21, 101, 192),
                Location = new Point(15, 372),
                AutoSize = true
            };

            grpVision.Controls.AddRange(new Control[] { _picVision, pnlVisionInfo, lblVisionHookHint });
            Controls.Add(grpVision);

            // Group 6: Product Counters
            GroupBox grpCounters = new GroupBox
            {
                Text = "6. Thong ke san luong phan loai (Production Counters)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(rightX, 488),
                Size = new Size(rightW, 105),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblCountBlack = CreateCounterBox("BLACK (M10 -> Y1)", "0", Color.FromArgb(38, 42, 48), Color.White, 15, 28, 150, 62);
            _lblCountBlue  = CreateCounterBox("BLUE (M11 -> Y2)", "0", Color.FromArgb(21, 101, 192), Color.White, 175, 28, 150, 62);
            _lblCountWhite = CreateCounterBox("WHITE (M12 -> Pass)", "0", Color.FromArgb(235, 240, 245), Color.Black, 335, 28, 155, 62);
            _lblCountTotal = CreateCounterBox("TONG SAN PHAM", "0", Color.FromArgb(46, 125, 50), Color.White, 500, 28, 130, 62);

            _btnResetCounters = new Button
            {
                Text = "Reset\nBo dem",
                Location = new Point(640, 28),
                Size = new Size(75, 62),
                BackColor = Color.FromArgb(226, 232, 240),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.75f, FontStyle.Bold)
            };
            _btnResetCounters.Click += (s, e) => _viewModel.ResetCounters();

            grpCounters.Controls.AddRange(new Control[] { _lblCountBlack, _lblCountBlue, _lblCountWhite, _lblCountTotal, _btnResetCounters });
            Controls.Add(grpCounters);

            // 4. BOTTOM SYSTEM LOG
            GroupBox grpLog = new GroupBox
            {
                Text = "7. Nhat ky van hanh & Truyen thong Modbus RTU / Vision Events",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Location = new Point(16, 600),
                Size = new Size(1194, 168),
                BackColor = Color.White,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            _lstLog = new ListBox
            {
                Location = new Point(12, 24),
                Size = new Size(1170, 132),
                Font = new Font("Consolas", 9.25f),
                BackColor = Color.FromArgb(20, 24, 32),
                ForeColor = Color.FromArgb(165, 243, 252),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };
            grpLog.Controls.Add(_lstLog);
            Controls.Add(grpLog);

            // Periodic timer to poll PLC Y0, Y1, Y2 when connected
            _pollTimer = new Timer { Interval = 600 };
            _pollTimer.Tick += (s, e) =>
            {
                if (_viewModel.CurrentState.IsConnected)
                {
                    _viewModel.PollPlcOutputs();
                    UpdatePlcStateOnUi(_viewModel.CurrentState);
                }
            };
            _pollTimer.Start();
        }

        private void PopulateComPorts()
        {
            _cboPort.Items.Clear();
            string[] ports = _viewModel.GetComPorts();
            if (ports.Length > 0)
            {
                _cboPort.Items.AddRange(ports);
                _cboPort.SelectedIndex = 0;
            }
            else
            {
                _cboPort.Items.Add("COM3");
                _cboPort.Items.Add("COM4");
                _cboPort.SelectedIndex = 0;
            }
        }

        private void OnConnectClicked(object sender, EventArgs e)
        {
            if (_viewModel.CurrentState.IsConnected)
            {
                _viewModel.DisconnectPlc();
            }
            else
            {
                string port = _cboPort.SelectedItem != null ? _cboPort.SelectedItem.ToString() : "COM3";
                int baud = 19200;
                if (_cboBaud.SelectedItem != null) int.TryParse(_cboBaud.SelectedItem.ToString(), out baud);
                string parity = _cboParity.SelectedItem != null ? _cboParity.SelectedItem.ToString() : "Even";
                _viewModel.ConnectPlc(port, baud, parity);
            }
        }

        private void OnLoadTestImageClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog
            {
                Title = "Chon anh phoi test cho khu vuc Vision",
                Filter = "Image Files (*.png;*.jpg;*.jpeg;*.bmp)|*.png;*.jpg;*.jpeg;*.bmp"
            })
            {
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    WorkpieceColor simColor = WorkpieceColor.White;
                    if (_cboTestImgColor.SelectedIndex == 0) simColor = WorkpieceColor.Black;
                    else if (_cboTestImgColor.SelectedIndex == 1) simColor = WorkpieceColor.Blue;
                    _viewModel.LoadTestImageForVision(ofd.FileName, simColor);
                }
            }
        }

        // Slot methods called from Program.cs wiring when ViewModel emits bottom-up events (Rule 5.1.C)
        public void UpdatePlcStateOnUi(PlcStateModel state)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdatePlcStateOnUi(state)));
                return;
            }

            if (state.IsConnected)
            {
                _lblConnectionBadge.Text = string.Format("PLC CONNECTED ({0})", _cboPort.Text);
                _lblConnectionBadge.BackColor = Color.FromArgb(46, 125, 50);
                _btnConnect.Text = "NGAT KET NOI";
                _btnConnect.BackColor = Color.FromArgb(198, 40, 40);
            }
            else
            {
                _lblConnectionBadge.Text = "OFFLINE (SIMULATION MODE)";
                _lblConnectionBadge.BackColor = Color.FromArgb(198, 40, 40);
                _btnConnect.Text = "KET NOI PLC";
                _btnConnect.BackColor = Color.FromArgb(25, 118, 210);
            }

            // Update Y0, Y1, Y2 LED indicators
            _ledY0.Text = state.Y0_Conveyor ? "Y0: BANG TAI\n(RUNNING - ON)" : "Y0: BANG TAI\n(STOPPED - OFF)";
            _ledY0.BackColor = state.Y0_Conveyor ? Color.FromArgb(46, 125, 50) : Color.FromArgb(207, 216, 220);
            _ledY0.ForeColor = state.Y0_Conveyor ? Color.White : Color.FromArgb(55, 71, 79);

            _ledY1.Text = state.Y1_CylinderBlack ? "Y1: XY LANH 1\n(BLACK - ON)" : "Y1: XY LANH 1\n(BLACK - OFF)";
            _ledY1.BackColor = state.Y1_CylinderBlack ? Color.FromArgb(230, 81, 0) : Color.FromArgb(207, 216, 220);
            _ledY1.ForeColor = state.Y1_CylinderBlack ? Color.White : Color.FromArgb(55, 71, 79);

            _ledY2.Text = state.Y2_CylinderBlue ? "Y2: XY LANH 2\n(BLUE - ON)" : "Y2: XY LANH 2\n(BLUE - OFF)";
            _ledY2.BackColor = state.Y2_CylinderBlue ? Color.FromArgb(21, 101, 192) : Color.FromArgb(207, 216, 220);
            _ledY2.ForeColor = state.Y2_CylinderBlue ? Color.White : Color.FromArgb(55, 71, 79);

            // Update counters
            _lblCountBlack.Text = string.Format("BLACK (M10 -> Y1)\n{0}", state.BlackCount);
            _lblCountBlue.Text  = string.Format("BLUE (M11 -> Y2)\n{0}", state.BlueCount);
            _lblCountWhite.Text = string.Format("WHITE (M12 -> Pass)\n{0}", state.WhiteCount);
            _lblCountTotal.Text = string.Format("TONG SAN PHAM\n{0}", state.TotalCount);
        }

        public void UpdateVisionResultOnUi(VisionDetectionResult result)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => UpdateVisionResultOnUi(result)));
                return;
            }

            if (result.AnnotatedFrame != null)
            {
                var oldImg = _picVision.Image;
                _picVision.Image = result.AnnotatedFrame;
                if (oldImg != null) oldImg.Dispose();
            }

            _lblDetectedColor.Text = result.DetectedColor.ToString().ToUpperInvariant();
            switch (result.DetectedColor)
            {
                case WorkpieceColor.Black:
                    _lblDetectedColor.BackColor = Color.FromArgb(38, 42, 48);
                    _lblDetectedColor.ForeColor = Color.White;
                    break;
                case WorkpieceColor.Blue:
                    _lblDetectedColor.BackColor = Color.FromArgb(21, 101, 192);
                    _lblDetectedColor.ForeColor = Color.White;
                    break;
                case WorkpieceColor.White:
                    _lblDetectedColor.BackColor = Color.FromArgb(235, 240, 245);
                    _lblDetectedColor.ForeColor = Color.Black;
                    break;
            }

            _lblConfidence.Text = string.Format("Confidence: {0:F1} %", result.Confidence * 100);
            _lblBBoxInfo.Text = string.Format("BBox: [X:{0}, Y:{1}]\nSize: [W:{2}, H:{3}]",
                result.BoundingBox.X, result.BoundingBox.Y, result.BoundingBox.Width, result.BoundingBox.Height);
            _lblVisionSource.Text = string.Format("Nguon: {0}\nLuc: {1:HH:mm:ss}", result.Source, result.Timestamp);
        }

        public void AppendLogOnUi(string logLine)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => AppendLogOnUi(logLine)));
                return;
            }

            _lstLog.Items.Add(logLine);
            if (_lstLog.Items.Count > 200)
                _lstLog.Items.RemoveAt(0);
            _lstLog.TopIndex = _lstLog.Items.Count - 1;
        }

        private static Label CreateLabel(string text, int x, int y)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular)
            };
        }

        private static Label CreateStatusIndicator(string text, int x, int y, int w, int h)
        {
            return new Label
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = Color.FromArgb(207, 216, 220),
                ForeColor = Color.FromArgb(55, 71, 79),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private static Label CreateCounterBox(string title, string count, Color bg, Color fg, int x, int y, int w, int h)
        {
            return new Label
            {
                Text = string.Format("{0}\n{1}", title, count),
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = bg,
                ForeColor = fg,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BorderStyle = BorderStyle.FixedSingle
            };
        }
    }
}
