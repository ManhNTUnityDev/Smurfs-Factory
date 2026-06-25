# 🎮 Match Factory – Game Design Document

---

## 1. Tổng Quan (Overview)

**Match Factory** là game mobile thể loại **match-3 tap**, người chơi tap vào các vật phẩm (Items) trên bảng (board) để thu thập chúng vào thanh thu thập bên dưới. Khi thanh thu thập chứa đủ **3 Items giống nhau**, chúng sẽ tự động biến mất (match). Mỗi level có các **Quest** yêu cầu người chơi thu thập đủ số lượng từng loại Item trong giới hạn thời gian cho phép.

> **Điểm đặc trưng về đồ họa:** Toàn bộ Items trong game được render dưới dạng **vật thể 3D** (3D objects) với ánh sáng, bóng đổ và vật liệu (materials) thực tế. Board hiển thị các Items chồng lên nhau như một khối vật thể 3D thực sự, tạo cảm giác chiều sâu và khối lượng tự nhiên.

---

## 2. Các Thành Phần Chính (Core Components)

### 2.1 Board (Bàn chơi)
- Khu vực hiển thị toàn bộ Items của level, các Items xếp chồng lên nhau tạo thành một khối.
- Items trên board có thể bị che khuất bởi Items khác (lớp trên che lớp dưới).
- Người chơi chỉ có thể tap vào Items **có thể tiếp cận** (không bị che hoàn toàn hoặc theo quy tắc tương tác của level).
- Khi tap, Item được chọn sẽ **bay từ board về thanh thu thập** với hiệu ứng animation.

### 2.2 Items (Vật phẩm 3D)
- Mỗi Item là một **3D object** được render trong không gian 3D, có đầy đủ:
  - **Geometry (Mesh):** Hình dạng khối 3D đặc trưng của từng loại (ví dụ: trái lê, nho, banh tennis, kẹo mút, bánh cupcake, v.v.).
  - **Material / Texture:** Bề mặt có texture, màu sắc, độ bóng (specular), độ nhám (roughness) mô phỏng vật liệu thực.
  - **Lighting:** Chịu tác động của ánh sáng scene — có highlight sáng và bóng đổ (shadow).
  - **Physics Collider:** Mỗi Item có collider để xử lý va chạm khi xáo trộn (Shuffle) và khi Items rơi xuống.
- Mỗi level có nhiều loại Item 3D khác nhau theo chủ đề của level (theme).
- Mỗi loại Item có số lượng cụ thể trên board, **số lượng luôn chia hết cho 3** để đảm bảo có thể thu thập hết.
- Items **chồng lên nhau theo vật lý 3D** — Items phía trên có thể tap được; Items bị vùi sâu bên dưới sẽ không tap được cho đến khi các Items trên bị lấy đi.
- Khi tap, Item **rời khỏi khối 3D**, thu nhỏ dần và bay theo đường cong (arc) về thanh thu thập.

### 2.2.1 Rendering Pipeline (3D)
| Yếu tố | Mô tả |
|---|---|
| **Camera** | Perspective camera nhìn từ trên xuống (top-down) hoặc góc nghiêng nhẹ để thấy chiều sâu của khối Items |
| **Lighting** | Ambient light + 1–2 directional light để tạo bóng đổ mềm |
| **Shadow** | Real-time shadow hoặc baked shadow map |
| **Material** | PBR (Physically Based Rendering): Albedo, Roughness, Metallic, Normal map |
| **Anti-aliasing** | MSAA hoặc TAA để hình mịn trên mobile |
| **LOD** | Level of Detail: giảm polygon khi Items nhỏ/xa camera |

### 2.3 Quest (Nhiệm vụ)
- Hiển thị ở **góc trên bên trái** màn hình.
- Mỗi level có **tối thiểu 2 Quest, tối đa 6 Quest**.
- Mỗi Quest bao gồm:
  - **Icon** đại diện cho loại Item cần thu thập.
  - **Số đếm** thể hiện số Item còn cần thu thập (luôn chia hết cho 3 ban đầu).
- **Cơ chế hoạt động của Quest:**
  - Mỗi khi người chơi tap 1 Item thuộc loại đó, số đếm của Quest tương ứng **giảm 1**.
  - Khi số đếm **về 0**, Quest đó **hoàn thành**.
  - Quest hoàn thành sẽ có hiệu ứng: Icon **quay ra mặt sau** rồi **biến mất**.
  - Các Quest còn lại ở **bên phải** của Quest vừa biến mất sẽ **dịch chuyển sang trái** để lấp đầy khoảng trống (không có khoảng trắng giữa các Quest).
- **Điều kiện thắng:** Tất cả Quest đều được hoàn thành (số đếm = 0).

### 2.4 Thanh Thu Thập (Collection Bar)
- Nằm ở **phía dưới** màn hình.
- Chứa tối đa **7 ô**.
- Khi người chơi tap vào một Item, Item đó **bay từ board vào thanh thu thập**.
- **Cơ chế xếp Item thông minh (Smart Sorting):**
  - Khi một Item mới vào thanh, hệ thống kiểm tra xem đã có Item **cùng loại** trong thanh chưa.
  - Nếu có, Item mới sẽ được **chèn vào vị trí liền kề** với Items cùng loại (ưu tiên xếp gần nhau).
  - Nếu chưa có, Item mới được thêm vào **ô trống tiếp theo** từ trái sang phải.
- **Match 3:** Khi có **3 Items giống nhau** trong thanh, chúng **tự động biến mất** với hiệu ứng, các Items còn lại dồn lại.
- **Điều kiện thua (Thanh đầy):** Nếu cả **7 ô đều có Item** mà không có bộ ba nào, người chơi **thua cuộc**.

---

## 3. Hệ Thống Thời Gian (Time System)

| Thông số | Giá trị |
|---|---|
| Thời gian tối đa mỗi level | **3 phút 45 giây (225 giây)** |
| Hiển thị | Thanh đếm ngược / đồng hồ ở vị trí nổi bật |

### Tính sao khi hoàn thành:

| Thời gian còn lại | Số sao |
|---|---|
| > 40% tổng thời gian (> 90 giây) | ⭐⭐⭐ 3 sao |
| 20% – 40% tổng thời gian (45–90 giây) | ⭐⭐ 2 sao |
| < 20% tổng thời gian (< 45 giây) | ⭐ 1 sao |

- **Thắng:** Hoàn thành tất cả Quest trước khi hết giờ → tính sao theo thời gian còn lại.
- **Thua (hết giờ):** Đếm ngược về 0 mà chưa hoàn thành hết Quest → thất bại.

---

## 4. Điều Kiện Thắng / Thua (Win / Lose Conditions)

### ✅ Thắng khi:
- Tất cả Quest đều có số đếm = 0 (thu thập đủ số Item yêu cầu) **trước khi** thời gian đếm ngược kết thúc.

### ❌ Thua khi (một trong hai điều kiện):
1. **Hết thời gian** — đồng hồ đếm ngược về 0 mà chưa hoàn thành tất cả Quest.
2. **Thanh thu thập đầy** — cả 7 ô đều có Item mà không hình thành được bộ 3 nào để xóa.

---

## 5. Tools Hỗ Trợ (Power-ups)

Người chơi có thể sử dụng các công cụ hỗ trợ để vượt qua màn chơi khó. Mỗi tool có hiệu ứng riêng biệt:

### 🔀 Shuffle (Xáo Trộn)
- **Mô tả:** Hất tung toàn bộ Items trên board lên cao, sau đó chúng rơi xuống và sắp xếp lại ngẫu nhiên.
- **Ràng buộc:** Tất cả Items vẫn phải nằm trong phạm vi khung hình (board).
- **Mục đích:** Giúp người chơi tiếp cận các Items bị che khuất, thay đổi bố cục để tạo cơ hội mới.
- **Animation:** Items bay lên, xoay, rơi xuống vị trí mới.

### 🌀 Vacuum (Hút Vật Phẩm)
- **Mô tả:** Tự động hút **3 vật phẩm** cùng loại từ board về thanh thu thập (tạo thành 1 bộ 3 để xóa).
- **Cách chọn Item:**
  - Ưu tiên loại Item có **số lượng Quest còn lại nhiều nhất** (số đếm Quest cao nhất).
  - Nếu nhiều Quest có số điểm bằng nhau, ưu tiên Item của **Quest nằm bên trái hơn**.
- **Mục đích:** Nhanh chóng hoàn thành một phần Quest mà không cần tap thủ công.

### 🌿 Spring (Đẩy Lại Board)
- **Mô tả:** Đẩy **tất cả** Items đang có trong thanh thu thập **trở lại board**.
- **Vị trí sau khi trả về:** Các Items được trả về sẽ nằm **trên cùng** (layer cao nhất) so với các Items đang có trên board.
- **Mục đích:** Giải phóng thanh thu thập khi gần đầy, giúp người chơi có thêm không gian để tiếp tục.

### 🧊 Ice Gun (Súng Băng)
- **Mô tả:** **Dừng thời gian đếm ngược** trong **10 giây**.
- **Hiệu ứng hình ảnh:** Đồng hồ đóng băng, có hiệu ứng ice/frost.
- **Mục đích:** Cho người chơi thêm thời gian để suy nghĩ và hành động trong tình huống gấp.

---

## 6. Game Flow (Luồng Chơi)

```
[Màn hình chính / Level Select]
        │
        ▼
[Chọn Level]
        │
        ▼
[Màn hình bắt đầu Level]
  - Hiển thị số level
  - Hiển thị danh sách Quest (Item cần thu thập)
  - Nút "Play" / "Start"
        │
        ▼
[Gameplay Loop] ◄──────────────────────────────────────┐
  - Board hiển thị đầy đủ Items                        │
  - Đồng hồ bắt đầu đếm ngược (3p45s)                 │
  - Quest hiển thị ở góc trên trái                     │
  - Thanh thu thập hiển thị ở phía dưới                │
        │                                              │
        ▼                                              │
[Người chơi tap vào Item trên board]                   │
        │                                              │
        ▼                                              │
[Item bay về thanh thu thập]                           │
  - Hệ thống smart sort: xếp cạnh Items cùng loại      │
  - Số đếm Quest tương ứng giảm 1                     │
        │                                              │
        ▼                                              │
[Kiểm tra Match 3?]                                    │
  - Có 3 Items giống nhau → Xóa 3 Items đó             │
  - Hiệu ứng biến mất + điểm                           │
        │                                              │
        ▼                                              │
[Kiểm tra Quest hoàn thành?]                           │
  - Quest số đếm = 0 → hiệu ứng flip + biến mất        │
  - Các Quest còn lại dịch chuyển sang trái            │
        │                                              │
        ▼                                              │
[Kiểm tra điều kiện Thắng / Thua]                     │
  ├─ Chưa thắng / chưa thua → tiếp tục Gameplay ──────┘
  ├─ THUA: Thanh thu thập đầy 7 ô
  ├─ THUA: Hết thời gian
  └─ THẮNG: Tất cả Quest = 0
        │
        ▼
[Màn hình Kết quả]
  - Thắng: hiển thị số sao (1⭐ / 2⭐⭐ / 3⭐⭐⭐)
  - Thua: hiển thị thông báo thất bại + retry
  - Nút: "Next Level" / "Retry" / "Home"
```

---

## 7. UI Layout (Giao Diện)

```
┌─────────────────────────────────────────┐
│  [Quest 1][Quest 2][Quest 3]...         │  ← Góc trên trái (2–6 quests)
│                          [⏱ 3:45]      │  ← Đồng hồ đếm ngược
│─────────────────────────────────────────│
│                                         │
│                                         │
│              B O A R D                  │  ← Khu vực chứa Items
│         (Items chồng lên nhau)          │
│                                         │
│                                         │
│─────────────────────────────────────────│
│  [ ][ ][ ][ ][ ][ ][ ]                 │  ← Thanh thu thập (7 ô)
│─────────────────────────────────────────│
│  [🔀 Shuffle][🌀 Vacuum][🌿 Spring][🧊] │  ← Tool bar
└─────────────────────────────────────────┘
```

### Chi tiết Quest UI:
```
┌──────┐ ┌──────┐ ┌──────┐
│ Icon │ │ Icon │ │ Icon │   ← Icon đại diện loại Item
│  30  │ │  18  │ │   9  │   ← Số đếm còn lại (countdown)
└──────┘ └──────┘ └──────┘
```

---

## 8. Quy Tắc Thiết Kế Level (Level Design Rules)

| Tiêu chí | Quy tắc |
|---|---|
| Số loại Quest | Tối thiểu 2, tối đa 6 |
| Số Item mỗi Quest | Phải chia hết cho 3 (ví dụ: 3, 6, 9, 12, ..., 30) |
| Tổng Item trên Board | Tổng số Item của tất cả Quest (mỗi loại có đúng số lượng theo quest) |
| Đảm bảo khả năng thắng | Tổng số Items luôn phải thu thập được trong giới hạn thời gian |

---

## 9. Hệ Thống Animation (3D)

> Toàn bộ animation được thực hiện trên **3D objects trong scene**, kết hợp giữa physics simulation và tweening.

| Sự kiện | Animation 3D |
|---|---|
| **Tap Item** | Item **scale down nhẹ** (squish) → tách khỏi khối 3D → **bay theo arc** (Bezier curve) về thanh thu thập, có rotation ngẫu nhiên khi bay |
| **Item vào thanh** | Item **scale từ 0 lên 1** tại vị trí trong thanh (bounce ease), các Items khác slide nhường chỗ |
| **Match 3** | 3 Items **phát sáng** (emission material) → spin nhanh → **scale về 0** + particle burst (confetti 3D nhỏ) |
| **Quest hoàn thành** | Icon 3D **xoay 180° theo trục Y** (flip) → scale về 0 → biến mất; các Icon còn lại **translate sang trái** |
| **Shuffle** | Tất cả Items **bị phóng lên** bằng physics impulse (lực ngẫu nhiên), **xoay tự do** trong không khí → **rơi xuống** với gravity, va chạm vật lý với nhau và với bound của board |
| **Vacuum** | Hiệu ứng **vòng xoáy hút** (swirl particle) hướng vào 3 Items được chọn → Items bay nhanh về thanh → match & biến mất |
| **Spring** | Tất cả Items trong thanh **bật ra** với physics impulse → **rơi xuống** board (đỉnh của khối) với va chạm vật lý |
| **Ice Gun** | **Frost particle** phủ lên đồng hồ + hiệu ứng **frozen overlay** (shader ice/frost), đồng hồ **đóng băng** |
| **Thắng** | Items còn lại trên board **bắn tung lên** → 3D confetti rơi xuống toàn màn hình, hiệu ứng **star pop** (các sao 3D xuất hiện) |
| **Thua** | Màn hình **rung nhẹ** (camera shake), Items trên board **rủ xuống** dần, thông báo thua fade in |

---

## 10. Tóm Tắt Tính Năng (Feature Summary)

| Tính năng | Mô tả |
|---|---|
| **3D Items** | Vật phẩm là 3D objects với PBR material, lighting, shadow và physics collider |
| **3D Physics Board** | Items chồng nhau theo vật lý 3D thực, Shuffle dùng physics impulse |
| **Quest System** | 2–6 nhiệm vụ thu thập Item mỗi level |
| **Smart Collection Bar** | 7 ô, tự động xếp Items giống nhau gần nhau |
| **Match-3 Mechanic** | 3 Items giống nhau trong thanh → tự động xóa |
| **Timer** | Đếm ngược 3p45s, ảnh hưởng đến kết quả sao |
| **Star Rating** | 1–3 sao dựa trên % thời gian còn lại |
| **Shuffle Power-up** | Xáo trộn board bằng physics impulse 3D |
| **Vacuum Power-up** | Hút 3 Items theo ưu tiên Quest |
| **Spring Power-up** | Trả toàn bộ Items trong thanh về board |
| **Ice Gun Power-up** | Đóng băng thời gian 10s |
| **Win/Lose Detection** | Phát hiện thắng/thua theo 3 điều kiện |
| **Quest Completion Animation** | 3D flip & slide animation khi Quest hoàn thành |
| **PBR Rendering** | Physically Based Rendering với lighting, shadow, texture |
