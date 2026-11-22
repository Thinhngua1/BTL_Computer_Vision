using System;
using System.Drawing;
using System.IO.Ports;
using System.Threading.Tasks;
using VisionSortingController.Models;
using VisionSortingController.Services;
using VisionSortingController.Utils;

namespace VisionSortingController.ViewModels
{
    // MainViewModel: Coordinates PLC ModbusService, VisionService, and PlcStateModel.
    // Rule 5.1.C: Never references MainForm or any System.Windows.Forms View class.
    public class MainViewModel
    {
        private readonly ModbusRtuService _modbusService;
        private readonly VisionService _visionService;
        private readonly PlcStateModel _plcState;

        public byte SlaveId { get; set; } = PlcAddressMap.DefaultSlaveId;

        // Bottom-up signals to View (Rule 5.1.C)
        public event Action<PlcStateModel> OnPlcStateChanged;
        public event Action<VisionDetectionResult> OnVisionResultUpdated;
        public event Action<string> OnLogMessage;

        public MainViewModel(ModbusRtuService modbusService, VisionService visionService, PlcStateModel plcState)
        {
            _modbusService = modbusService;
            _visionService = visionService;
            _plcState = plcState;
        }

        public PlcStateModel CurrentState { get { return _plcState; } }

        public string[] GetComPorts() { return _modbusService.GetAvailablePorts(); }

        public Bitmap GetInitialVisionFrame() { return _visionService.RenderIdleFrame(); }

        // Top-down calls from View -> ViewModel -> Service (Rule 5.1.A)
        public bool ConnectPlc(string portName, int baudRate, string parityStr)
        {
            Parity parity = Parity.Even; // Xinje default: 19200, 8, Even, 1
            string pUpper = parityStr.ToUpperInvariant();
            if (pUpper == "NONE") parity = Parity.None;
            else if (pUpper == "ODD") parity = Parity.Odd;

            bool ok = _modbusService.Connect(portName, baudRate, parity, 8, StopBits.One);
            _plcState.IsConnected = ok;
            if (ok)
            {
                PollPlcOutputs();
            }
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
            return ok;
        }

        public void DisconnectPlc()
        {
            _modbusService.Disconnect();
            _plcState.IsConnected = false;
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        public void StartVisionTcpHook(int port = 5050)
        {
            _visionService.StartTcpHook(port);
        }

        // Ladder Row 0-1: LD M0 OR Y0, LDI M1 -> OUT Y0
        public async Task StartConveyorAsync()
        {
            EmitLog("Command: START Conveyor (Pulse M0=ON, M1=OFF -> Latch Y0=ON)");
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M1_Stop, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M0_Start, true);

            _plcState.M0_Start = true;
            _plcState.M1_Stop = false;
            _plcState.Y0_Conveyor = true;
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);

            await Task.Delay(250);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M0_Start, false);
            _plcState.M0_Start = false;

            PollPlcOutputs();
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        // Ladder: M1 (LDI M1) breaks latch for Y0, Y1, Y2
        public async Task StopConveyorAsync()
        {
            EmitLog("Command: STOP System (Pulse M1=ON -> Unlatch Y0, Y1, Y2)");
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M0_Start, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M10_Black, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M11_Blue, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M12_White, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M1_Stop, true);

            _plcState.M0_Start = false;
            _plcState.M1_Stop = true;
            _plcState.M10_Black = false;
            _plcState.M11_Blue = false;
            _plcState.M12_White = false;
            _plcState.Y0_Conveyor = false;
            _plcState.Y1_CylinderBlack = false;
            _plcState.Y2_CylinderBlue = false;
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);

            await Task.Delay(300);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M1_Stop, false);
            _plcState.M1_Stop = false;

            PollPlcOutputs();
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        // Reset Cylinder Latches (Y1, Y2) and resume conveyor Y0 if it was running
        public async Task ResetSortingActuatorsAsync()
        {
            bool conveyorWasRunning = _plcState.Y0_Conveyor;
            EmitLog("Command: RESET Cylinders (Clear M10/M11/M12 & Unlatch Y1/Y2)");

            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M10_Black, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M11_Blue, false);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M12_White, false);

            // Pulse M1 briefly to unlatch Y1 & Y2 in PLC ladder
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M1_Stop, true);
            await Task.Delay(180);
            _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M1_Stop, false);

            _plcState.M10_Black = false;
            _plcState.M11_Blue = false;
            _plcState.M12_White = false;
            _plcState.Y1_CylinderBlack = false;
            _plcState.Y2_CylinderBlue = false;

            if (conveyorWasRunning)
            {
                _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M0_Start, true);
                await Task.Delay(180);
                _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M0_Start, false);
                _plcState.Y0_Conveyor = true;
            }

            PollPlcOutputs();
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        // Simulate or Trigger Color from UI Button
        public void SimulateColorTrigger(WorkpieceColor color)
        {
            float simConf = 0.0f;
            if (color == WorkpieceColor.Black) simConf = 0.97f;
            else if (color == WorkpieceColor.Blue) simConf = 0.95f;
            else if (color == WorkpieceColor.White) simConf = 0.96f;

            _visionService.TriggerDetection(color, simConf, null, "Manual / UI Simulation");
        }

        public void LoadTestImageForVision(string filePath, WorkpieceColor simulatedColor)
        {
            _visionService.ProcessImageFile(filePath, simulatedColor);
        }

        public void ResetCounters()
        {
            _plcState.BlackCount = 0;
            _plcState.BlueCount = 0;
            _plcState.WhiteCount = 0;
            EmitLog("Counters reset to 0.");
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        // Handler wired in Program.cs when VisionService emits a detection (either Simulation or Quoc's YOLO)
        public async void HandleVisionDetection(VisionDetectionResult result)
        {
            if (OnVisionResultUpdated != null) OnVisionResultUpdated(result);

            switch (result.DetectedColor)
            {
                case WorkpieceColor.Black:
                    _plcState.BlackCount++;
                    _plcState.M10_Black = true;
                    _plcState.M11_Blue = false;
                    _plcState.M12_White = false;
                    _plcState.Y1_CylinderBlack = true;
                    EmitLog(string.Format("[VISION -> PLC] Detected BLACK ({0:F1}%) -> Write M10=ON -> Cylinder 1 (Y1=ON)", result.Confidence * 100));

                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M11_Blue, false);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M12_White, false);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M10_Black, true);
                    if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);

                    await Task.Delay(300);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M10_Black, false);
                    _plcState.M10_Black = false;
                    break;

                case WorkpieceColor.Blue:
                    _plcState.BlueCount++;
                    _plcState.M10_Black = false;
                    _plcState.M11_Blue = true;
                    _plcState.M12_White = false;
                    _plcState.Y2_CylinderBlue = true;
                    EmitLog(string.Format("[VISION -> PLC] Detected BLUE ({0:F1}%) -> Write M11=ON -> Cylinder 2 (Y2=ON)", result.Confidence * 100));

                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M10_Black, false);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M12_White, false);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M11_Blue, true);
                    if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);

                    await Task.Delay(300);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M11_Blue, false);
                    _plcState.M11_Blue = false;
                    break;

                case WorkpieceColor.White:
                    _plcState.WhiteCount++;
                    _plcState.M10_Black = false;
                    _plcState.M11_Blue = false;
                    _plcState.M12_White = true;
                    EmitLog(string.Format("[VISION -> PLC] Detected WHITE ({0:F1}%) -> Write M12=ON -> Pass Straight (Y1=OFF, Y2=OFF)", result.Confidence * 100));

                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M10_Black, false);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M11_Blue, false);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M12_White, true);
                    if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);

                    await Task.Delay(300);
                    _modbusService.WriteSingleCoil(SlaveId, PlcAddressMap.Coil_M12_White, false);
                    _plcState.M12_White = false;
                    break;
            }

            PollPlcOutputs();
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        // Poll actual Y0, Y1, Y2 coils from PLC Xinje when connected
        public void PollPlcOutputs()
        {
            if (!_modbusService.IsConnected) return;

            bool[] yStates = _modbusService.ReadCoils(SlaveId, PlcAddressMap.Coil_Y0_Conveyor, 3);
            if (yStates != null && yStates.Length >= 3)
            {
                _plcState.Y0_Conveyor = yStates[0];
                _plcState.Y1_CylinderBlack = yStates[1];
                _plcState.Y2_CylinderBlue = yStates[2];
            }
        }

        public void HandleConnectionChanged(bool connected, string port)
        {
            _plcState.IsConnected = connected;
            if (OnPlcStateChanged != null) OnPlcStateChanged(_plcState);
        }

        public void EmitLog(string message)
        {
            if (OnLogMessage != null)
                OnLogMessage(string.Format("[{0:HH:mm:ss}] {1}", DateTime.Now, message));
        }
    }
}
