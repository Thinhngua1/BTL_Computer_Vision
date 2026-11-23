# CHECKPOINTS – TRẠNG THÁI DỰ ÁN & ĐIỂM TIẾP TỤC

**Cập nhật lần cuối:** Kết thúc Buổi 1 (23/11 - 00:36)  
**Kho chứa GitHub chính thức:** `https://github.com/Thinhngua1/BTL_Computer_Vision.git`

---

## 1. Trạng thái hiện tại của dự án
- **Phase 1 (Analysis & Architecture Design):** **HOÀN THÀNH 100%** (Đã chốt kiến trúc phần cứng, kiến trúc C# MVVM, bảng thanh ghi PLC Xinje XD2-16T-E và hoàn thiện 3 sơ đồ trong `architecture.drawio`).
- **Phase 2 (Scaffolding):** **HOÀN THÀNH 100%** (Đã dựng xong Solution C# WinForms `.NET Framework 4.7.2` chuẩn MVVM chạy trên Visual Studio 2019).
- **Phase 3 (Implementation - Phần của Thịnh):** **HOÀN THÀNH 90%** (Đã hoàn thành chương trình PLC `PLC.xdp`, giao diện điều khiển băng tải, giả lập 3 màu `Black / Blue / White`, module `ModbusRtuService` và cổng chờ `VisionService` cho Quốc).

---

## 2. Danh sách Module theo tiến độ

### A. Những module ĐÃ HOÀN THÀNH
1. **PLC Ladder Program (`Program/PLC.xdp`):**
   - Đã đấu nối phần cứng và nạp chạy thử trên PLC Xinje XD2-16T-E.
   - Quy ước bit: `M0` (Start -> giữ `Y0`), `M1` (Stop -> ngắt `Y0, Y1, Y2`), `M10` (Black -> giữ `Y1`), `M11` (Blue -> giữ `Y2`), `M12` (White -> đi thẳng).
2. **Sơ đồ kiến trúc (`config_Agent/docs/architecture.drawio`):**
   - 3 tab: `1-System-Architecture`, `2-CSharp-MVVM`, `3-Vision-Sorting-Flowchart` (Sẵn sàng xuất ảnh đưa vào Tiểu luận & Slide).
3. **Ứng dụng PC Control (`Program/VisionSortingController`):**
   - Kiến trúc MVVM với 100% Wiring tại `Program.cs`.
   - Giao diện điều khiển băng tải, giả lập phân loại 3 màu, bộ đếm sản lượng và khung hiển thị Bounding Box cho Vision.

### B. Những module ĐANG TRIỂN KHAI
1. **Tinh chỉnh kết nối cáp USB-PLC (`COM9 - CH340`) trong `ModbusRtuService.cs`:**
   - Bật `DtrEnable = true`, `RtsEnable = true` cho cáp nạp PLC và chọn mặc định `COM9`.
2. **Module Vision YOLO (Phần của Quốc):**
   - Quốc đang phát triển model YOLO nhận diện phôi trụ màu (`Black`, `Blue`, `White`) và sử dụng giao diện WinForms để test điều khiển băng tải.

### C. Những module CHƯA THỰC HIỆN
1. **Ghép nối trực tiếp luồng YOLO thực tế của Quốc vào `VisionService`** (Dự kiến: Buổi 2 - Sáng 24/11).
2. **Viết Tiểu luận Word (10–20 trang) và làm Slide báo cáo (10–15 slides)** theo yêu cầu trong `require.docx` (Dự kiến: Buổi 3 - Chiều 24/11).

---

## 3. Kiến trúc và Quyết định kỹ thuật đã chốt
- **Ngôn ngữ & UI Framework:** C# (`.NET Framework 4.7.2` tương thích VS2019) + WinForms.
- **Kiến trúc phần mềm:** MVVM (Model – View – ViewModel).
  - Top-Down (`View -> VM -> Service/Model`): Gọi phương thức trực tiếp (`Direct Call`).
  - Bottom-Up (`Service/Model -> VM -> View`): Bắt buộc dùng `C# event / Action<T>`.
  - Wiring: Tập trung duy nhất tại `Program.cs`.
- **Truyền thông PC – PLC:** Modbus RTU (`19200 bps, 8 Data bits, Even Parity, 1 Stop bit, Slave ID = 1`) qua cổng COM vật lý (cáp USB-PLC `COM9` hoặc RS485 A-B).
- **Cơ chế ghép nối với Vision (Quốc):**
  - **Cách 1 (Khuyên dùng nếu YOLO viết bằng Python):** Gửi chuỗi `"BLACK,0.96,x,y,w,h\n"` (hoặc `"BLACK\n"`, `"BLUE\n"`, `"WHITE\n"`) qua TCP Socket `127.0.0.1:5050` đã mở sẵn trong `VisionService`.
  - **Cách 2 (Nếu tích hợp trực tiếp trong C#):** Gọi hàm `VisionService.TriggerDetection(color, confidence, bbox, source)`.

---

## 4. Công việc cần tiếp tục ở phiên sau (Buổi 2 & Buổi 3 – Ngày 24/11)

### Buổi 2 (Sáng 24/11) – Ghép nối kiến trúc & Chạy Demo thực tế
1. Bổ sung `DtrEnable = true` / `RtsEnable = true` vào `ModbusRtuService.cs`, tắt `XDPPro`/`X_Net_Windows` để mở khóa `COM9`, và test điều khiển thật từ nút bấm C# xuống PLC Xinje XD2.
2. Kết nối chương trình YOLO của Quốc với app C# (qua TCP Port `5050` hoặc tích hợp trực tiếp).
3. Chạy thử nghiệm phân loại toàn trình trên băng tải (Camera chụp phôi trụ -> YOLO nhận diện Black/Blue/White -> C# hiển thị BBox & gửi Modbus -> PLC kích xylanh `Y1`/`Y2`).
4. Chụp ảnh màn hình giao diện, ảnh phần cứng, bảng thống kê kết quả và quay video demo.

### Buổi 3 (Chiều 24/11) – Làm Tiểu luận & Slide Báo cáo
1. Soạn Tiểu luận (10–20 trang):
   - Trang phân công nhiệm vụ (Thịnh & Quốc).
   - Chương 1: Giới thiệu đề tài.
   - Chương 2: Phương pháp giải quyết vấn đề (Đưa 3 sơ đồ `draw.io`, bảng địa chỉ PLC, thuật toán YOLO và kiến trúc MVVM vào).
   - Chương 3: Các kết quả đạt được (Hình ảnh thực nghiệm, độ chính xác nhận diện & phân loại).
   - Chương 4: Kết luận.
2. Soạn Slide thuyết trình (10–15 slides) bám sát thang điểm (`6đ` Xử lý ảnh YOLO, `1đ` Giao diện, `1đ` Giao tiếp phần cứng, `2đ` Tiểu luận & Báo cáo).
