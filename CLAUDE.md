# CLAUDE.md — MAGIC BLOCK

Tài liệu này là quy tắc làm việc bắt buộc dành cho Claude khi hỗ trợ phát triển project **Magic Block**. Mọi đề xuất thiết kế, câu trả lời, tài liệu và code phải tuân thủ các quy tắc dưới đây.

---

## 1. Thông tin project

### 1.1. Tổng quan

- **Tên game:** Magic Block
- **Thể loại:** Puzzle / Casual / Roguelite-inspired Puzzle
- **Nền tảng dự kiến:** Mobile (Android / iOS), màn hình dọc
- **Chế độ chơi:** Single-player – Endless Mode
- **Game tham khảo:** Block Blast
- **Ngôn ngữ giao tiếp mặc định:** Tiếng Việt
- **Repo:** https://github.com/HkTom021/MagicBlock (public, nhánh `main`)

### 1.2. Công nghệ (đã chốt — project đang dùng)

| Mục | Giá trị |
|---|---|
| Engine | Unity **6000.0.75f1** |
| Render pipeline | URP 17.0.4 (`Assets/Settings`: Mobile_RPAsset / PC_RPAsset) |
| Ngôn ngữ | C# |
| Input | Input System 1.19.0, **chỉ dùng Input System mới** (`activeInputHandler: 1`). Không dùng `UnityEngine.Input`. |
| UI | uGUI (`com.unity.ugui`), Canvas Screen Space Overlay, CanvasScaler 1080x1920 chế độ Expand |
| Text | `UnityEngine.UI.Text` + font Nunito-Black. Chưa dùng TextMeshPro. |
| Công cụ phát triển | `com.coplaydev.unity-mcp` (MCP for Unity) để Claude điều khiển Unity Editor |

Không thêm package khác khi chưa được người dùng cho phép (xem mục 8.3).

### 1.3. Ý tưởng cốt lõi

Magic Block giữ gameplay xếp block lên bảng, xóa hàng ngang/cột dọc và ghi điểm tương tự Block Blast. Game bổ sung ba hệ thống chính:

1. **End Turn:** Người chơi chủ động kết thúc lượt. Các block chưa sử dụng được chuyển vào Storage.
2. **Limited Storage:** Storage có số slot giới hạn. Người chơi phải cân nhắc block nào nên giữ lại.
3. **Buff System:** Sau một số turn nhất định, người chơi được chọn buff. Buff gồm loại dùng một lần và loại duy trì đến khi Game Over.

### 1.4. Vòng lặp gameplay

1. Nhận một nhóm 3 block mới.
2. Đặt block lên bảng chơi hoặc lấy block từ Storage để sử dụng.
3. Khi một hàng ngang hoặc cột dọc được lấp đầy, hàng/cột đó bị xóa và người chơi nhận điểm.
4. Người chơi có thể End Turn để đưa các block chưa sử dụng vào Storage.
5. Hệ thống kiểm tra mốc nhận buff; nếu đủ điều kiện, người chơi chọn một buff.
6. Bắt đầu lượt tiếp theo và tiếp tục cho đến khi không còn nước đi hợp lệ.

### 1.5. Trạng thái hiện tại

Đã có bản prototype chạy được trong Unity: core gameplay, End Turn + Storage, Buff (chọn buff, 4 consumable, 5 permanent), Game Over, High Score. Nhiều thông số và luật trong bản này do Claude **tạm chọn** khi triển khai, người dùng **chưa chốt** — xem mục 2.3.

---

## 2. CẤM ĐOÁN — QUY TẮC BẮT BUỘC

### 2.1. Không tự ý thay đổi project

1. Không tạo, sửa, di chuyển hoặc xóa file trong project khi chưa có sự đồng ý rõ ràng của người dùng.
2. Trước khi thực hiện thay đổi, phải trình bày kế hoạch gồm:
   - File sẽ tạo/sửa/xóa.
   - Mục đích của từng thay đổi.
   - Các hệ thống hoặc file có thể bị ảnh hưởng.
   - Cách kiểm thử sau khi thực hiện.
3. Chờ người dùng xác nhận rõ ràng như “đồng ý”, “ok, làm đi” trước khi thay đổi file.
4. Sự đồng ý cho một công việc không đồng nghĩa với sự đồng ý cho các thay đổi phát sinh ngoài kế hoạch. Nếu phạm vi thay đổi tăng lên, phải hỏi lại.
5. Được phép đọc, tìm kiếm, phân tích và đề xuất mà không cần xin phép. Điều này gồm cả thao tác chỉ đọc qua Unity MCP (đọc console, xem hierarchy, chụp màn hình).
6. Thay đổi trạng thái Unity Editor qua MCP (tạo/sửa GameObject, lưu scene, chạy menu item, đổi Project Settings) cũng được tính là thay đổi project và phải xin phép như sửa file. Vào/ra Play mode để kiểm thử thì được phép.

### 2.2. Không bịa thông tin

- Không bịa API, class, method, property, event hoặc tính năng của Unity/package.
- Không chắc API có tồn tại hoặc có hành vi như mô tả thì phải kiểm tra tài liệu chính thức của **Unity 6000.0** (hoặc dùng `unity_reflect` / `unity_docs` qua MCP).
- Không bịa tên, đường dẫn, license, giá hoặc tính năng của asset, plugin, package và công cụ.
- Không khẳng định code đã chạy đúng nếu chưa thực sự chạy thử.
- Khi không thể chạy project hoặc kiểm thử, phải nói rõ phần nào chưa được xác minh.
- Không tự thêm tính năng hoặc luật chơi chưa được người dùng xác nhận.
- Không trình bày giả định, đề xuất hoặc ví dụ như thể đó là quyết định đã chốt.

### 2.3. Những phần thiết kế chưa chốt

GDD chưa xác định đầy đủ các luật dưới đây. Bản prototype hiện tại đã **tạm** triển khai một phương án cho từng điểm để game chạy được. Cột “Đang triển khai” mô tả đúng code hiện có, **không phải quyết định đã chốt**.

| # | Điểm cần làm rõ | Đang triển khai (tạm) | Chỗ chỉnh |
|---|---|---|---|
| 1 | Kích thước bảng chơi | 8x8 | `GameManager.boardSize` |
| 2 | Quy tắc End Turn | Bấm được bất cứ lúc nào khi trên tay còn ít nhất 1 block chưa dùng | `GameManager.CanEndTurn` |
| 3 | Storage không đủ chỗ | Không được End Turn (nút bị khóa, có dòng nhắc). Không có chọn block để cất hay bỏ block | `GameManager.CanEndTurn` |
| 4 | Dùng block trong Storage | Dùng bất cứ lúc nào, song song với 3 block trên tay, không giới hạn số block lấy ra mỗi turn | `GameManager.TryPlace` |
| 5 | Thứ tự xử lý lượt | Lấy block từ Storage không ảnh hưởng 3 block trên tay. Turn kết thúc khi dùng hết 3 block trên tay hoặc khi End Turn | `GameManager.CompleteTurn` |
| 6 | Điều kiện nhận buff | Mỗi 5 turn hoàn thành (`TurnsCompleted % 5 == 0`). Turn được tính khi dùng hết 3 block hoặc End Turn | `GameManager.turnsPerBuff` |
| 7 | Lựa chọn buff | Chọn 1 trong 3 buff, rút ngẫu nhiên đều, không trùng nhau, từ cả 9 buff | `GameManager.buffChoices`, `OfferBuffChoice` |
| 8 | Tích trữ Consumable | Không giới hạn số lượng | `GameManager.Buffs.cs` |
| 9 | Permanent trùng lặp | Được chọn lại và cộng dồn | `BuffDefinitions.cs` |
| 10 | Điểm số | Xem mục 3.7 | `GameManager.CalculatePoints` |
| 11 | Block generation | 16 hình gốc kèm các hướng xoay, random theo trọng số; màu random 7 màu. Xem mục 3.1 | `ShapeLibrary.cs` |
| 12 | Game Over | Thua khi: không block nào (trên tay + Storage) đặt được **và** không End Turn được **và** không đang chọn buff **và** không còn consumable buff | `GameManager.Evaluate` |
| 13 | Lưu tiến trình | Chỉ lưu High Score (`PlayerPrefs`, key `MagicBlock.HighScore`). Không lưu cài đặt, thống kê hay ván đang chơi | `GameManager.AddScore` |

Quy tắc:

- Không coi các giá trị trên là đã chốt. Khi người dùng xác nhận hoặc đổi một điểm, cập nhật bảng này (ghi “Đã chốt”).
- Khi một yêu cầu mới phụ thuộc vào một điểm chưa chốt, nhắc người dùng rằng đó đang là phương án tạm và hỏi có giữ không trước khi xây thêm lên trên.
- Không tự đổi các phương án tạm này sang phương án khác khi chưa được duyệt.

### 2.4. Không tự ý mở rộng phạm vi

- Không tự thêm tiền tệ, cửa hàng, quảng cáo, nhiệm vụ ngày, đăng nhập, leaderboard online, gacha hoặc hệ thống meta-progression.
- Không tự thêm campaign, level, boss hoặc multiplayer.
- Không tự thêm cơ chế xoay block bởi người chơi, đổi màu block, chướng ngại vật hoặc vật phẩm trên board.
- Không tự thay đổi Endless Mode thành level-based mode.
- Có thể đề xuất các ý tưởng này, nhưng phải tách rõ phần đề xuất và chờ người dùng duyệt.

---

## 3. Quy tắc gameplay

Ký hiệu: **[GDD]** = có trong GDD. **[Tạm]** = đang triển khai nhưng chưa chốt (xem 2.3).

### 3.1. Main Board và Block

- [GDD] Main Board là bảng lưới nơi người chơi đặt các block.
- [GDD] Mỗi block được cấu tạo từ các ô vuông nhỏ; người chơi kéo và thả block vào vị trí hợp lệ.
- [GDD] Hàng ngang hoặc cột dọc đầy thì bị xóa. Nhiều hàng/cột đầy cùng lúc được xóa cùng lúc.
- [GDD] Một nhóm mới gồm 3 block.
- [Tạm] Board 8x8.
- [Tạm] Danh sách shape (trọng số tính cho **mỗi** hướng xoay): Dot 6, Line2 4, Line3 4, Line4 3, Line5 2, Square2 5, Square3 2, Rect2x3 2, Corner3 3, L4 2, J4 2, T4 2, S4 2, Z4 2, BigL5 2, Diag2 2. Hướng xoay được sinh sẵn trong thư viện, **người chơi không xoay block**.
- [Tạm] Block “dễ đặt” = shape có ≤ 3 ô (dùng cho Lucky Blocks).

### 3.2. End Turn

- [GDD] End Turn là hành động người chơi chủ động kích hoạt.
- [GDD] Khi End Turn, các block chưa sử dụng trong lượt hiện tại được chuyển vào Storage theo giới hạn dung lượng.
- [GDD] End Turn không xóa hoặc reset Main Board.
- Không tự thêm hình phạt, điểm thưởng hoặc hiệu ứng phụ cho End Turn nếu chưa được duyệt.
- [Tạm] Luật khi Storage không đủ chỗ: xem 2.3 mục 3.

### 3.3. Storage

- [GDD] Storage lưu các block chưa sử dụng, có giới hạn slot.
- [GDD] Mỗi block chiếm một slot, không phụ thuộc số ô vuông.
- [GDD] Người chơi có thể lấy block trong Storage để dùng; slot được giải phóng khi lấy ra.
- [GDD — đề xuất ban đầu] Sức chứa 5 slots; Expanded Storage +2 slots.

### 3.4. Turn System

- [GDD] Một turn: nhận 3 block → đặt block mới hoặc block từ Storage → có thể End Turn → block dư vào Storage → kiểm tra mốc buff → turn mới.
- [Tạm] Turn Counter tăng khi dùng hết 3 block trên tay hoặc khi End Turn. Màn hình hiện “TURN n” với n = số turn đã hoàn thành + 1.

### 3.5. Buff System

#### A. Consumable Buff — dùng một lần

| Tên | Hiệu ứng [GDD] | Cách triển khai hiện tại [Tạm] |
|---|---|---|
| Block Destroyer | Xóa một block được chọn trên bảng | Chạm vào một ô có block → xóa toàn bộ phần còn lại của **block đã đặt** chứa ô đó (board lưu id cho mỗi lần đặt) |
| Single Line Clear | Xóa một hàng hoặc cột được chọn | Nút MODE đổi hàng/cột, chạm bảng để xóa. Không cộng điểm. Hàng/cột trống thì không tiêu buff |
| Block Reroll | Đổi nhóm block hiện tại thành nhóm mới | Chỉ đổi các block **còn trên tay**, giữ nguyên số lượng; không đụng Storage |
| Storage Cleanup | Xóa một block trong Storage | Chạm vào block trong Storage để bỏ nó |

- [GDD] Buff biến mất sau khi dùng; được tích trữ nếu chưa dùng.
- [Tạm] Không giới hạn số lượng tích trữ. Không dùng được consumable khi đang mở bảng chọn buff hoặc sau Game Over.

#### B. Permanent Buff — duy trì đến Game Over

| Tên | Hiệu ứng [GDD] | Thông số hiện tại [Tạm] |
|---|---|---|
| Expanded Storage | Tăng 2 slots Storage | +2 slots mỗi lần chọn |
| Score Multiplier | Tăng điểm nhận được | +50% điểm **xóa hàng/cột** mỗi lần chọn (không nhân điểm đặt block) |
| Combo Bonus | Tăng điểm khi xóa nhiều hàng/cột | +10 vào hệ số thưởng nhiều hàng mỗi lần chọn |
| Lucky Blocks | Tăng cơ hội nhận block dễ đặt | Trọng số shape ≤ 3 ô nhân (1 + số lần chọn) |
| Clear Bonus | Thưởng điểm sau mỗi lần xóa | +20 điểm mỗi lần xóa (mỗi lần đặt block có xóa) |

- [GDD] Có hiệu lực đến khi ván kết thúc, không cần kích hoạt thủ công.
- [Tạm] Cộng dồn khi chọn trùng, không giới hạn. Thông số nằm ở đầu `Assets/MagicBlock/Scripts/Buffs/BuffDefinitions.cs`.

### 3.6. Win / Lose

- [GDD] Endless Mode, không có điều kiện chiến thắng cuối cùng.
- [GDD] Game Over khi không còn nước đi hợp lệ với block hiện tại và Storage.
- [Tạm] Cách hiểu “không còn nước đi”: xem 2.3 mục 12. End Turn và consumable buff được tính là cách thoát kẹt.
- Game Over hiện tổng điểm, High Score, số turn, số hàng/cột đã xóa, số block đã đặt và nút PLAY AGAIN.

### 3.7. Điểm số [Tạm]

Với `cells` = số ô của block vừa đặt, `L` = số hàng + cột bị xóa trong lần đặt đó:

```
điểm = cells × 1
     + (nếu L > 0) round( (10 × L + L × (L − 1) × (10 + ComboBonus)) × ScoreMultiplier ) + ClearBonus
```

Ví dụ khi chưa có buff: xóa 1 hàng = 10, 2 hàng = 40, 3 hàng = 90 (chưa tính điểm ô). Thông số nằm trong Inspector của `GameManager` (`pointsPerCell`, `pointsPerLine`, `multiLineBonusPerExtraLine`).

---

## 4. Cấu trúc hệ thống và kiến trúc code

### 4.1. Cấu trúc thư mục hiện tại

```
Assets/MagicBlock/
  Art/            Sprite (ảnh trong thư mục này tự import thành Sprite)
    Tiles/        7 màu ô block
  Audio/          Âm thanh .ogg
  Fonts/          Nunito-Black.ttf
  Scenes/Game.unity   Scene chính (build index 0)
  Scripts/
    Core/         ShapeLibrary.cs (Shape + thư viện hình), BoardModel.cs (Piece, lưới, đặt/xóa) — logic thuần, không dính UI
    GameManager.cs            Luật chơi: Hand, Storage, End Turn, điểm, turn, Game Over, event
    Buffs/        BuffDefinitions.cs (danh sách + thông số buff), GameManager.Buffs.cs (partial: chọn/dùng buff)
    UI/           GameView.cs (dựng toàn bộ uGUI lúc chạy), GameView.Buffs.cs (partial: UI buff), PieceView.cs (kéo thả)
  Editor/         MagicBlockSceneBuilder.cs (menu MagicBlock → Build Game Scene), MagicBlockSpriteImporter.cs
Asset/            (ngoài Unity, bị .gitignore) bản tách asset từ game khác, chỉ để tham khảo/lấy tạm
```

- `Assets/Scenes/SampleScene.unity` và `Assets/TutorialInfo` là file mẫu của template Unity, game không dùng.
- UI được dựng bằng code lúc vào Play mode, nên ở Edit mode scene `Game.unity` chỉ có Camera, EventSystem và GameObject `MagicBlock`.
- Scene `Game.unity` có thể dựng lại bằng menu **MagicBlock → Build Game Scene** (ghi đè scene, gán lại sprite/âm thanh/font).

### 4.2. Ranh giới trách nhiệm

| Hệ thống | Nằm ở đâu hiện tại |
|---|---|
| Game Controller, Turn, Storage, Score, Block Supply | `GameManager.cs` |
| Board, Line Clear | `Core/BoardModel.cs` |
| Block (shape, trọng số) | `Core/ShapeLibrary.cs` |
| Buff | `Buffs/BuffDefinitions.cs`, `Buffs/GameManager.Buffs.cs` |
| UI | `UI/GameView.cs`, `UI/GameView.Buffs.cs`, `UI/PieceView.cs` |
| Save | Chỉ High Score, trong `GameManager` |

- Logic (Core, GameManager) không tham chiếu UI. UI lắng nghe event của `GameManager`: `StateChanged`, `Placed`, `EndedTurn`, `BuffMilestoneReached`, `BuffChoiceOffered`, `BuffChosen`, `ConsumableUsed`, `GameEnded`.
- **Nợ kỹ thuật đã biết:** `GameManager` đang gánh nhiều hệ thống (Turn, Storage, Score, Block Supply, Buff qua partial class). Điều này đi ngược nguyên tắc “không tạo Manager khổng lồ”. Có thể đề xuất tách (ví dụ Storage, Score, Buff thành class riêng) nhưng phải được duyệt trước khi refactor.

### 4.3. Nguyên tắc chung và mở rộng

- Ưu tiên phân chia theo tính năng/hệ thống, tránh gom toàn bộ logic vào một file hoặc một class.
- Tách dữ liệu cấu hình khỏi logic xử lý.
- Tránh phụ thuộc chéo không cần thiết giữa Board, Block, Storage, Turn và Buff.
- Không tạo kiến trúc phức tạp vượt nhu cầu của game.
- Thêm block shape mới: thêm một dòng `Add(...)` trong `ShapeLibrary.Build()`.
- Thêm buff mới: thêm giá trị vào `BuffType`, thêm `BuffInfo` vào `Buffs.All` (đúng thứ tự enum), rồi thêm xử lý ở `GameManager.Buffs.cs` (và UI nếu là consumable cần chọn mục tiêu).
- Quy tắc tính điểm tập trung ở `GameManager.CalculatePoints`.
- Không copy-paste cùng một logic xử lý ở nhiều nơi.
- Chỉ áp dụng design pattern khi nó giải quyết vấn đề thực tế.

---

## 5. Quy ước code (Unity C#)

### 5.1. Quy tắc chung

- Namespace `MagicBlock` cho code game, `MagicBlock.EditorTools` cho code Editor.
- PascalCase cho class, method, property, event; field private dạng `_camelCase`; field hiện trong Inspector dùng `[SerializeField]` + camelCase (không dùng field `public`).
- Mỗi file một class chính; partial class đặt tên `TênClass.PhầnMở.cs`.
- Code Editor phải nằm trong thư mục `Editor/`.
- Comment bằng tiếng Việt, giải thích lý do hoặc logic khó hiểu, không diễn giải lại từng dòng code.
- Hàm nên có một trách nhiệm chính; kiểm tra dữ liệu đầu vào và trường hợp biên; không âm thầm bỏ qua lỗi.
- Tránh số liệu hard-code rải rác; đưa thông số cân bằng vào `[SerializeField]` hoặc hằng số tập trung (`BuffDefinitions.cs`).
- Không dùng `UnityEngine.Input` (project chỉ bật Input System mới). Thao tác kéo thả đi qua uGUI EventSystem + `InputSystemUIInputModule`.
- Không thêm dependency/package/plugin nếu chưa được người dùng cho phép.
- Không đưa secret, token, API key hoặc thông tin cá nhân vào source code.

### 5.2. Dữ liệu gameplay nên cấu hình được

Hiện cấu hình nằm ở:

- Inspector của `GameManager`: kích thước board, số block mỗi nhóm, sức chứa Storage, mốc nhận buff, số buff mỗi lần chọn, số màu, điểm.
- `ShapeLibrary.cs`: danh sách shape và trọng số.
- `BuffDefinitions.cs`: danh sách buff, mô tả, thông số buff.
- Inspector của `GameView`: kích thước hiển thị, sprite, âm thanh.

Chưa dùng ScriptableObject. Nếu muốn chuyển cấu hình sang ScriptableObject, phải đề xuất và chờ duyệt.

### 5.3. Hiệu năng

- Tránh tạo object, collection hoặc GameObject không cần thiết trong `Update`.
- Không chạy kiểm tra toàn bộ board nhiều lần trong cùng một frame nếu có thể tái sử dụng kết quả.
- Chỉ tối ưu dựa trên điểm nghẽn có thể xác định.
- Animation và hiệu ứng UI không được làm chậm thao tác đặt block.
- Hiện UI hand/Storage được tạo lại toàn bộ mỗi khi state đổi (Destroy + tạo mới). Chấp nhận được với quy mô hiện tại; nếu đo được giật trên máy thật thì mới đổi sang tái sử dụng object.
- Chưa kiểm tra trên thiết bị mobile thật. Không khẳng định hiệu năng mobile khi chưa thử.

---

## 6. UI/UX và Game Feel

### 6.1. Gameplay Feedback

| Yêu cầu [GDD] | Trạng thái |
|---|---|
| Hiển thị vị trí hợp lệ khi kéo block | Có: bóng mờ + tô sẵn hàng/cột sắp xóa |
| Phản hồi khi đặt block | Có: ô nảy nhẹ + âm thanh |
| Hiệu ứng khi xóa hàng/cột | Có: ô thu nhỏ/mờ dần, chữ Good!/Great!/… + điểm |
| Điểm cập nhật rõ ràng | Có: điểm chạy tăng dần |
| Animation block vào/ra Storage | **Chưa có** (chỉ có chữ thông báo) |
| Cảnh báo Storage gần đầy | Có: nhãn STORAGE đổi màu + dòng nhắc |
| Hiệu ứng khi dùng buff | Có mức cơ bản: hiệu ứng xóa ô + chữ thông báo |
| Game Over hiện điểm, High Score, thống kê | Có |

### 6.2. Buff UI

- Hiển thị tên, biểu tượng và mô tả hiệu ứng của buff. **Hiện chưa có biểu tượng (icon)**, chỉ có tên, mô tả và nhãn.
- Phân biệt rõ Consumable và Permanent: đã có nhãn chữ CONSUMABLE / PERMANENT kèm màu.
- Hiển thị buff dùng một lần còn lại (4 nút cạnh End Turn, có số lượng) và Permanent đang có hiệu lực (dòng chữ dưới BEST).
- Không dùng màu sắc làm dấu hiệu duy nhất để phân biệt hai loại buff.

### 6.3. Storage UI

- Phân biệt rõ block mới và block trong Storage (Storage là hàng slot riêng, block hiển thị nhỏ hơn).
- Hiển thị số slot đã dùng/tổng slot (`STORAGE x/y`).
- Thể hiện trạng thái gần đầy hoặc đầy.
- Không để khu vực Storage che hoặc gây khó thao tác trên Main Board.

### 6.4. Phong cách hình ảnh

GDD chỉ xác định: Minimalist 2D Puzzle, Clean and colorful UI, Smooth animations, phân biệt trực quan giữa block thường, block trong Storage và Buff. Chưa có palette màu, font, kích thước UI chính thức. Bố cục và màu hiện tại là tạm, không coi là quyết định cuối cùng.

### 6.5. Asset tạm

Ảnh, âm thanh và font trong `Assets/MagicBlock/Art`, `Audio`, `Fonts` được lấy từ thư mục `Asset/` — bản tách từ một game đã phát hành. Đây là asset tạm cho bài tập, **không phải tài sản của project** và phải thay trước khi phát hành. Repo đang để public nên các file này đang công khai; nhắc người dùng điều này khi bàn về phát hành hoặc chia sẻ. Không tự copy thêm asset từ `Asset/` khi chưa được đồng ý.

---

## 7. Kiểm thử bắt buộc

Không chỉ kiểm tra trường hợp chơi bình thường. Cần kiểm thử các trường hợp biên và tương tác giữa các hệ thống.

### 7.0. Cách kiểm thử trong project này

- Chưa có Unity Test Framework test. Kiểm thử hiện làm bằng cách vào Play mode qua MCP và chạy code (`execute_code`) gọi trực tiếp `GameManager` / giả lập sự kiện kéo-thả, kèm chụp màn hình.
- **Lưu ý:** khi cửa sổ Unity Editor không được focus, vòng lặp game tạm dừng: object bị `Destroy` chưa biến mất, coroutine và bộ đếm thời gian không chạy, `GameObject.Find` có thể trả về object cũ. Khi kiểm thử qua MCP phải tính đến điều này, không kết luận sai là lỗi game.
- Sau khi sửa script: chờ compile xong rồi đọc console tìm lỗi trước khi kiểm thử tiếp.
- Kéo-thả bằng chuột/cảm ứng thật phải do người dùng thử; báo rõ nếu chỉ mới giả lập.

### 7.1. Board và Block

- Đặt block tại góc, cạnh và giữa board.
- Đặt block vừa khít board.
- Thử đặt block vượt biên.
- Thử đặt block chồng lên ô đã có block.
- Đặt block để xóa một hàng; một cột; nhiều hàng/cột cùng lúc.
- Kiểm tra board sau khi xóa hàng/cột.

### 7.2. End Turn và Storage

- End Turn khi còn một block chưa dùng; khi còn nhiều block chưa dùng.
- End Turn khi Storage trống; khi Storage chỉ còn một slot; khi Storage đã đầy.
- Lấy block từ Storage ra sử dụng; kiểm tra slot được giải phóng.
- Kiểm tra không mất hoặc nhân đôi block khi chuyển giữa các khu vực.
- Kiểm tra End Turn không vô tình xóa hoặc reset board.

### 7.3. Buff

- Nhận buff đúng mốc turn.
- Chọn từng loại buff.
- Sử dụng Consumable Buff và kiểm tra buff chỉ bị tiêu thụ theo đúng quy tắc.
- Kiểm tra Permanent Buff còn hiệu lực qua nhiều turn.
- Kiểm tra hiệu ứng buff tương tác đúng với Storage, Board và Score.
- Kiểm tra buff trùng lặp theo quy tắc cộng dồn đang áp dụng.
- Kiểm tra trường hợp người chơi chưa thể sử dụng buff (Storage trống, board trống, hàng trống, đang chọn buff).

### 7.4. Game Over và điểm số

- Board gần đầy nhưng vẫn còn nước đi.
- Board không còn chỗ cho block mới nhưng Storage vẫn có block đặt được.
- Không còn block nào đặt được nhưng còn End Turn hoặc còn consumable buff.
- Không còn block nào đặt được và không còn cách thoát.
- Kiểm tra Game Over không xuất hiện quá sớm hoặc quá muộn.
- Kiểm tra điểm khi đặt block, xóa hàng/cột, combo và multiplier.
- Kiểm tra High Score được cập nhật đúng.

### 7.5. Kiểm thử thiết bị và giao diện

- Kiểm tra nhiều tỷ lệ màn hình mobile.
- Kiểm tra thao tác kéo-thả bằng cảm ứng.
- Kiểm tra UI không bị che bởi tai thỏ, thanh điều hướng hoặc vùng an toàn màn hình. **Hiện chưa xử lý Safe Area.**
- Kiểm tra chữ và nút đủ rõ trên màn hình nhỏ.
- Kiểm tra animation không cản trở thao tác liên tục.

Không báo “đã kiểm thử thành công” nếu chỉ đọc code mà chưa thực thi test.

---

## 8. Quy trình làm việc

Mọi yêu cầu phát triển phải đi theo quy trình:

1. **Đọc yêu cầu:** Xác định mục tiêu và phạm vi công việc.
2. **Đọc project hiện tại:** Kiểm tra cấu trúc, công nghệ và code liên quan trước khi đề xuất thay đổi.
3. **Làm rõ:** Hỏi người dùng nếu luật chơi, thông số hoặc hành vi chưa được xác định (đối chiếu mục 2.3).
4. **Lập kế hoạch:** Liệt kê file sẽ tạo/sửa/xóa, trách nhiệm từng file và cách kiểm thử.
5. **Chờ xác nhận:** Không thay đổi file trước khi người dùng đồng ý rõ ràng.
6. **Triển khai:** Tuân thủ kiến trúc và quy ước code đã thống nhất.
7. **Kiểm thử:** Chạy compile, Play mode hoặc test nếu môi trường cho phép.
8. **Báo cáo:** Nêu file đã thay đổi, chức năng đã làm, kết quả kiểm thử và phần chưa xác minh. Cập nhật CLAUDE.md nếu luật hoặc cấu trúc thay đổi.

### 8.1. Khi người dùng yêu cầu sửa lỗi

- Tìm nguyên nhân trước khi sửa; ưu tiên tái hiện lỗi.
- Không sửa lan sang hệ thống khác nếu không cần thiết.
- Nêu rõ nguyên nhân đã xác minh và giả thuyết chưa xác minh.
- Sau khi sửa, kiểm tra cả trường hợp gây lỗi ban đầu và các trường hợp liên quan.

### 8.2. Khi người dùng yêu cầu thêm tính năng

Trước khi code, phải mô tả:

- Tính năng hoạt động như thế nào.
- Hệ thống nào chịu trách nhiệm.
- Dữ liệu nào cần lưu hoặc thay đổi.
- Các trường hợp biên.
- Những điểm thiết kế cần người dùng quyết định.
- Danh sách file dự kiến tạo/sửa.

### 8.3. Git và dependency

- Repo: `git@github.com:HkTom021/MagicBlock.git`, nhánh `main`. `.gitignore` đã loại `Library/`, `Temp/`, `Logs/`, `UserSettings/`, file `.csproj`/`.sln` và thư mục `Asset/`.
- Không commit, push, tạo branch hoặc thay đổi lịch sử Git khi chưa được yêu cầu.
- Không cài package, plugin, addon hoặc dependency khi chưa được yêu cầu.
- Không tự tải asset từ Internet.
- Nếu đề xuất dependency mới, nêu lý do, nguồn chính thức, phiên bản và ảnh hưởng trước khi cài.

---

## 9. Nguyên tắc phản hồi

- Mặc định trả lời bằng tiếng Việt.
- Nói thẳng, rõ ràng, tránh giải thích vòng vo.
- Khi đưa code, cung cấp đủ ngữ cảnh để người dùng biết file nào cần đặt code vào.
- Không giấu lỗi hoặc giới hạn.
- Phân biệt rõ:
  - **Đã xác nhận:** thông tin có trong GDD hoặc được người dùng chốt.
  - **Tạm (đang triển khai):** đã có trong code nhưng người dùng chưa chốt.
  - **Đề xuất:** phương án Claude đưa ra để người dùng cân nhắc.
  - **Chưa xác định (TBD):** thông tin cần người dùng quyết định.
- Nếu có nhiều cách triển khai, nêu ưu/nhược điểm ngắn gọn và đề xuất một phương án, nhưng không tự triển khai khi chưa được duyệt.

---

## 10. Tham chiếu thiết kế

GDD gốc: `MagicBlockGDD.docx` (không nằm trong repo), gồm các phần: Game Objective, Game Mechanics, Game Flow Summary, Core Elements, Boosters / Power-ups, Reward System, Target Audience, Inspiration, Improvement, Newness, Feeling Reflection, Visual Mockup.

Khi GDD và yêu cầu mới của người dùng mâu thuẫn:

1. Không tự chọn một bên.
2. Nêu rõ điểm mâu thuẫn.
3. Hỏi người dùng quyết định nội dung nào là phiên bản mới nhất.
4. Sau khi được xác nhận, tuân theo quyết định mới và cập nhật CLAUDE.md.

**Nguyên tắc cốt lõi: Không đoán luật chơi, không tự ý sửa project, không khẳng định điều chưa kiểm chứng.**
