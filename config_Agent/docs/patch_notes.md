# PATCH NOTES – BTL COMPUTER VISION & PLC SORTING SYSTEM

## Phiên 1 (Buổi 1) – Ngày 22/11 - Rạng sáng 23/11

### 1. Những file được tạo mới
- `.gitignore`: Cấu hình loại bỏ file rác build của C# Visual Studio (`.vs/`, `obj/`, `*.pdb`, `*.suo`), thư mục `ref/` và môi trường ảo Python/YOLO (`.venv/`, `__pycache__/`).
- `Program/VisionSortingController.sln`: Solution chuẩn Visual Studio 2019 (MSBuild 16.11).
- `Program/VisionSortingController/VisionSortingController.csproj`: Project C# WinForms `.NET Framework 4.7.2` (C# 7.3).
- `Program/VisionSortingController/Program.cs`: Composition Root thực hiện 100% MVVM Wiring tập trung.
- `Program/VisionSortingController/Utils/PlcAddressMap.cs`: Bảng ánh xạ địa chỉ Coil Modbus RTU khớp với `PLC.xdp` (Xinje XD2-16T-E).
- `Program/VisionSortingController/Models/PlcStateModel.cs`: Model quản lý trạng thái PLC (`M0, M1, M10, M11, M12`, `Y0, Y1, Y2`), bộ đếm sản phẩm (`Black, Blue, White`) và kết quả nhận diện `VisionDetectionResult`.
- `Program/VisionSortingController/Services/ModbusRtuService.cs`: Dịch vụ truyền thông RS485/RS232 Modbus RTU Master (`FC05` Write Single Coil, `FC01` Read Coils, tự tính `CRC16`), hỗ trợ chạy giả lập Offline khi chưa cắm PLC.
- `Program/VisionSortingController/Services/VisionService.cs`: Dịch vụ xử lý/hiển thị khung hình Vision, vẽ ROI & YOLO Bounding Box, đọc ảnh tĩnh test và mở cổng `TCP Server (Port 5050)` làm Hook tích hợp cho module YOLO của Quốc.
- `Program/VisionSortingController/ViewModels/MainViewModel.cs`: ViewModel điều phối trung tâm giữa Giao diện, Vision và PLC theo quy tắc gọi hàm Top-Down và bắn Event Bottom-Up.
- `Program/VisionSortingController/Views/MainForm.cs`: Giao diện Dashboard công nghiệp chia 2 phân khu: Điều khiển Băng tải/PLC/Giả lập màu (Thịnh) và Hiển thị Camera/Bounding Box/Thống kê (chừa sẵn cho Quốc).
- `config_Agent/docs/architecture.md`, `discussion.md`, `patch_notes.md`, `checkpoints.md`: Bộ tài liệu quản lý dự án theo quy chuẩn `config_Agent.txt`.

### 2. Những file được chỉnh sửa
- `config_Agent/docs/architecture.drawio`: Chuẩn hóa lại toàn bộ 3 tab sơ đồ kiến trúc:
  1. `1-System-Architecture`: Kiến trúc phần cứng và giao thức truyền thông toàn hệ thống.
  2. `2-CSharp-MVVM`: Kiến trúc phần mềm C# WinForms MVVM và quy tắc Wiring tại `Program.cs`.
  3. `3-Vision-Sorting-Flowchart`: Lưu đồ thuật toán nhận diện màu YOLO và phân loại 3 màu (`Black -> M10/Y1`, `Blue -> M11/Y2`, `White -> M12/Đi thẳng`).

### 3. Những chức năng được bổ sung
- Giải mã trực tiếp file chương trình `Program/PLC.xdp` để đồng bộ hóa 100% địa chỉ bit trung gian (`M0, M1, M10, M11, M12`) và ngõ ra vật lý (`Y0, Y1, Y2`) với phần mềm C#.
- Chức năng điều khiển băng tải (`Start M0`, `Stop M1`, `Thu xylanh Reset Y1/Y2`).
- Chức năng giả lập 3 trạng thái màu (`BLACK`, `BLUE`, `WHITE`) cho Quốc test giao diện và PLC độc lập.
- Chức năng nhận dữ liệu YOLO từ bên ngoài qua giao thức TCP Socket (`127.0.0.1:5050`, định dạng chuỗi `COLOR,CONF,X,Y,W,H`).

### 4. Những lỗi được sửa
- **Lỗi xung đột phiên bản MSBuild trên Visual Studio 2019:** Chuyển đổi project từ SDK `.NET 8/10` (yêu cầu MSBuild 18) về `.NET Framework 4.7.2` (tương thích hoàn toàn với MSBuild 16.11.6 của VS2019).
- **Lỗi thiết kế trong sơ đồ `architecture.drawio` ban đầu:**
  - Tách bạch rõ Sensor Input (`X0`) và Actuator Output (`Y0, Y1, Y2`).
  - Thay thế sơ đồ `ESP32-MVVM` (`main.cpp`) bằng `C#-WinForms-MVVM` (`Program.cs`), bổ sung `VisionService` và các mũi tên sự kiện Bottom-Up (`C# Event`).
  - Loại bỏ vòng lặp treo ở nhánh `fail` trong lưu đồ thuật toán phân loại.
- **Lỗi đẩy nhầm kho chứa GitHub:** Đã làm sạch lịch sử Git (loại bỏ cache `.vs/` và `obj/`) và đẩy code chuẩn lên kho chứa chính thức `https://github.com/Thinhngua1/BTL_Computer_Vision.git`.

### 5. Những vấn đề còn tồn tại (Cần xử lý ở phiên tiếp theo)
- **Giao tiếp vật lý qua cáp USB-PLC (`USB-SERIAL CH340 - COM9`):**
  1. Cần đóng tiến trình `XDPPro.exe` và `X_Net_Windows.exe` trước khi bấm Kết nối trên app C# để tránh lỗi `Access to the port 'COM9' is denied`.
  2. Cần bổ sung `DtrEnable = true` và `RtsEnable = true` trong `ModbusRtuService.cs` để cấp nguồn nuôi mạch cách ly quang bên trong sợi cáp nạp USB-PLC (`CH340`) và tự động ưu tiên chọn `COM9` trên giao diện.
