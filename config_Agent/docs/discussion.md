# TECHNICAL DISCUSSIONS & DECISIONS LOG

## Phiên 1 (Ngày 22/11 - 23/11)

1. **Lựa chọn GUI Framework & Nền tảng .NET:**
   - **Quyết định:** Sử dụng **C# WinForms trên nền `.NET Framework 4.7.2` (C# 7.3)** kết hợp mẫu kiến trúc **MVVM (Event-driven)**.
   - **Lý do:** Đảm bảo tương thích 100% với **Visual Studio 2019 (MSBuild 16.11)** trên máy phát triển, không phụ thuộc gói NuGet bên ngoài, dễ nắm bắt cho người mới học C# PC Control và kịp tiến độ 3 buổi.

2. **Giao thức truyền thông PC – PLC Xinje XD2-16T-E:**
   - **Quyết định:** Sử dụng giao thức **Modbus RTU** (`19200 bps, 8 Data bits, Even Parity, 1 Stop bit, Slave ID = 1`).
   - **Lý do:** Cả cổng lập trình Mini-DIN 8 chân (`COM1` dùng cáp USB-SERIAL CH340 `COM9`) lẫn cổng RS485 (`A/B`) của PLC Xinje XD2 đều hỗ trợ sẵn Modbus RTU Slave với cùng tập lệnh `FC01` (Read Coils) và `FC05` (Write Single Coil).
   - **Lưu ý kỹ thuật đã phát hiện:**
     - Phần mềm `XDPPro.exe` và dịch vụ ngầm `X_Net_Windows.exe` của Xinje sẽ khóa cứng cổng `COM9` khi chạy. Cần tắt 2 tiến trình này trước khi kết nối từ ứng dụng C#.
     - Cáp nạp USB-PLC (`CH340`) cần bật `DtrEnable = true` và `RtsEnable = true` để cấp nguồn nuôi mạch cách ly quang bên trong cáp.

3. **Giao thức ghép nối với phần Vision YOLO của Quốc:**
   - **Quyết định:** Thiết kế `VisionService` hỗ trợ song song:
     - **Chế độ giả lập (Simulation Mode):** Có sẵn 3 nút bấm giả lập `BLACK (M10)`, `BLUE (M11)`, `WHITE (M12)` và nút nạp ảnh tĩnh để Quốc test điều khiển băng tải mà chưa cần ghép code sâu.
     - **Chế độ TCP Socket Hook (`127.0.0.1:5050`):** Cho phép code Python YOLO của Quốc gửi trực tiếp chuỗi `"COLOR,CONF,X,Y,W,H\n"` sang ứng dụng C# để tự động vẽ Bounding Box, đếm sản lượng và kích xylanh PLC.
