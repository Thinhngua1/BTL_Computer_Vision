# ARCHITECTURE SPECIFICATION – YOLO VISION & PLC SORTING SYSTEM

## 1. Tổng quan hệ thống (System Overview)
Hệ thống phân loại phôi hình trụ theo 3 màu sắc (**Black**, **Blue**, **White**) trên băng tải công nghiệp, kết hợp giữa Thị giác máy tính (**YOLO Deep Learning**) và Bộ điều khiển khả trình (**PLC Xinje XD2-16T-E**).

- **Sơ đồ chi tiết:** Xem file `config_Agent/docs/architecture.drawio` (Gồm 3 tab: `1-System-Architecture`, `2-CSharp-MVVM`, `3-Vision-Sorting-Flowchart`).

---

## 2. Bảng quy ước địa chỉ PLC Xinje XD2-16T-E (`PLC.xdp`)

| Thiết bị PLC | Địa chỉ Modbus (Dec / Hex) | Tên / Nhãn | Chức năng trong chương trình Ladder |
| :--- | :--- | :--- | :--- |
| **`M0`** | `0` (`0x0000`) | `Start` | Kích hoạt chạy băng tải (Tự giữ ngõ ra `Y0`) |
| **`M1`** | `1` (`0x0001`) | `Stop` | Dừng băng tải & Reset toàn bộ ngõ ra (`Y0, Y1, Y2`) |
| **`M10`** | `10` (`0x000A`) | `Black` | Nhận diện phôi màu Đen -> Kích hoạt & tự giữ Xylanh 1 (`Y1`) |
| **`M11`** | `11` (`0x000B`) | `Blue` | Nhận diện phôi màu Xanh -> Kích hoạt & tự giữ Xylanh 2 (`Y2`) |
| **`M12`** | `12` (`0x000C`) | `White` | Nhận diện phôi màu Trắng -> Đi thẳng về cuối băng tải |
| **`Y0`** | `24576` (`0x6000`) | `Conveyor` | Ngõ ra điều khiển động cơ băng tải |
| **`Y1`** | `24577` (`0x6001`) | `Cylinder 1` | Ngõ ra điều khiển Xylanh 1 (Gạt phôi màu Đen) |
| **`Y2`** | `24578` (`0x6002`) | `Cylinder 2` | Ngõ ra điều khiển Xylanh 2 (Gạt phôi màu Xanh) |

---

## 3. Kiến trúc phần mềm PC Controller (C# WinForms MVVM)

```text
Program/VisionSortingController/
│
├── Models/
│   └── PlcStateModel.cs          # Lưu trạng thái M, Y, bộ đếm sản lượng & VisionDetectionResult
├── ViewModels/
│   └── MainViewModel.cs          # Điều phối logic giữa UI, VisionService và ModbusRtuService
├── Views/
│   └── MainForm.cs               # Giao diện WinForms (Không chứa business logic)
├── Services/
│   ├── ModbusRtuService.cs       # Giao tiếp Modbus RTU Master (FC01, FC05, CRC16)
│   └── VisionService.cs          # Vẽ BBox, giả lập Vision & mở TCP Hook (Port 5050) cho YOLO
├── Utils/
│   └── PlcAddressMap.cs          # Hằng số địa chỉ Modbus của PLC Xinje XD2
└── Program.cs                    # Composition Root: Khởi tạo & nối Event/Signal tập trung 100%
```
