namespace VisionSortingController.Utils
{
    // PLC Xinje XD2-16T-E Modbus RTU Coil Map (Match PLC.xdp)
    public static class PlcAddressMap
    {
        public const byte DefaultSlaveId = 1;

        // Internal Relays (M0 = 0x0000)
        public const ushort Coil_M0_Start = 0;   // M0: Start Conveyor (Latch Y0)
        public const ushort Coil_M1_Stop  = 1;   // M1: Stop Conveyor & Reset Y0, Y1, Y2
        public const ushort Coil_M10_Black = 10; // M10: Black color -> Latch Y1 (Cylinder 1)
        public const ushort Coil_M11_Blue  = 11; // M11: Blue color  -> Latch Y2 (Cylinder 2)
        public const ushort Coil_M12_White = 12; // M12: White color -> Pass straight

        // Output Coils (Y0 = 0x6000 = 24576 in Xinje XD Series)
        public const ushort Coil_Y0_Conveyor  = 0x6000;
        public const ushort Coil_Y1_Cylinder1 = 0x6001;
        public const ushort Coil_Y2_Cylinder2 = 0x6002;
    }
}
