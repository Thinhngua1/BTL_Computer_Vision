using System;
using System.Windows.Forms;
using VisionSortingController.Models;
using VisionSortingController.Services;
using VisionSortingController.ViewModels;
using VisionSortingController.Views;

namespace VisionSortingController
{
    // Rule 5.2: 100% Centralized MVVM Wiring in Program.cs
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // 1. Instantiate Models & Bottom-Tier Services
            var plcStateModel = new PlcStateModel();
            var modbusService = new ModbusRtuService();
            var visionService = new VisionService();

            // 2. Instantiate ViewModel & Inject Dependencies
            var mainViewModel = new MainViewModel(modbusService, visionService, plcStateModel);

            // 3. Instantiate View
            var mainForm = new MainForm(mainViewModel);

            // 4. Wire Bottom-Up Signals: Service -> ViewModel (Rule 5.1.B)
            modbusService.OnConnectionChanged += mainViewModel.HandleConnectionChanged;
            modbusService.OnServiceLog += mainViewModel.EmitLog;
            visionService.OnDetectionReady += mainViewModel.HandleVisionDetection;
            visionService.OnVisionLog += mainViewModel.EmitLog;

            // 5. Wire Bottom-Up Signals: ViewModel -> View (Rule 5.1.C)
            mainViewModel.OnPlcStateChanged += mainForm.UpdatePlcStateOnUi;
            mainViewModel.OnVisionResultUpdated += mainForm.UpdateVisionResultOnUi;
            mainViewModel.OnLogMessage += mainForm.AppendLogOnUi;

            // 6. Start Vision TCP Hook (Port 5050) for external YOLO integration & emit initial status
            mainViewModel.StartVisionTcpHook(5050);
            mainViewModel.EmitLog("System initialized. Ready for PLC Modbus RTU connection or Offline Simulation.");

            // 7. Run Application
            Application.Run(mainForm);

            // 8. Cleanup
            visionService.Dispose();
            modbusService.Dispose();
        }
    }
}
