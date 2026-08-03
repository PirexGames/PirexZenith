# 👑 Hướng dẫn sử dụng & Ví dụ PirexZenith (Examples & Tutorials)

Thư mục `Examples` này chứa một mẫu mô phỏng **Gameplay Sample** giúp lập trình viên nhanh chóng hiểu và làm chủ các tính năng cốt lõi của **PirexZenith** trong Unity.

---

## 📂 Cấu trúc thư mục ví dụ

```
Examples/
├── README_EXAMPLES.md                    # Tài liệu hướng dẫn sử dụng (File này)
├── PirexZenith.Examples.asmdef           # Assembly Definition cho code ví dụ
└── GameplaySample/                       # Ví dụ Gameplay Sample
    ├── Services/                         # Các dịch vụ logic (IAudioService, IGameStateService, ...)
    ├── Models/                           # Các lớp dữ liệu/Transient (PlayerStats)
    ├── Installers/                       # Installer đăng ký binding vào Container (GameplaySceneInstaller)
    └── Scripts/                          # Các MonoBehaviour sử dụng [Inject] và Instantiate Prefab
```

---

## 🎯 1. Các khái niệm minh họa trong Gameplay Sample

### A. Định nghĩa Service (Singleton & Interface Binding)
- `IAudioService` & `AudioService`: Dịch vụ phát âm thanh SFX / Music được đăng ký **Singleton** theo Interface.
- `IGameStateService` & `GameStateService`: Dịch vụ lưu điểm số và trạng thái trận đấu được đăng ký **Singleton** theo Interface.

### B. Định nghĩa Model (Transient Binding)
- `PlayerStats`: Lớp dữ liệu thuần C# chứa thông số người chơi (Máu, Năng lượng, Tốc độ). Được đăng ký **Transient** — mỗi lần gọi `Resolve` hoặc tiêm (`[Inject]`) sẽ tạo ra một thể hiện mới hoàn toàn.

### C. Cấu hình Binding qua Installer (`GameplaySceneInstaller`)
- Kế thừa từ `MonoInstaller` và override phương thức `InstallBindings`:
  ```csharp
  public override void InstallBindings(ContainerBuilder builder)
  {
      builder.RegisterSingleton<AudioService, IAudioService>();
      builder.RegisterSingleton<GameStateService, IGameStateService>();
      builder.RegisterTransient<PlayerStats>();
  }
  ```

### D. Tiêm phụ thuộc vào MonoBehaviour (`[Inject]`)
- **Field Injection**:
  ```csharp
  [Inject] private IAudioService _audioService;
  ```
- **Property Injection**:
  ```csharp
  [Inject] public IGameStateService GameState { get; private set; }
  ```

### E. Tiêm phụ thuộc khi tạo Prefab lúc Runtime (`container.Instantiate`)
- Trong `EnemySpawner.cs`, chúng ta tiêm trực tiếp `DIContainer` và gọi phương thức mở rộng `Instantiate`:
  ```csharp
  [Inject] private DIContainer _container;
  
  public GameObject SpawnEnemy(GameObject enemyPrefab, Vector3 position)
  {
      // Prefab sinh ra sẽ tự động được Inject phụ thuộc cho mọi MonoBehaviour con
      return _container.Instantiate(enemyPrefab, position, Quaternion.identity, null);
  }
  ```
- Component `EnemyUnit` gắn trên Prefab Enemy cũng có các thẻ `[Inject]` và được hệ thống tiêm ngay lập tức sau khi spawn.

### F. Tích hợp Object Pooling & DI (RexPool Integration)
- Nếu dự án có cài package **RexPool** (`com.pirexgames.rexpool`), PirexZenith sẽ tự động bật bộ API mở rộng với tên hàm **giống hệt 100% RexPoolManager** trên `DIContainer`:
  ```csharp
  // 1. Pre-warm pool với 5 instance không kích hoạt (O(1) allocation)
  _container.Prepair(enemyPrefab, 5);

  // 2. Lấy object từ pool và tự động Inject phụ thuộc cho instance mới (O(1) reuse)
  GameObject enemy = _container.Take(enemyPrefab, position, Quaternion.identity);

  // 3. Trả về pool (O(1) Push vào free pool)
  enemy.SetActive(false);

  // 4. Xoá toàn bộ pool của prefab
  _container.CleanUp(enemyPrefab);
  ```
- Khi **không cài đặt RexPool**, toàn bộ các hàm trên được ẩn khỏi IntelliSense và bạn tiếp tục sử dụng `_container.Instantiate(...)` như chuẩn Unity thông thường.
- Trong `EnemySpawner.cs` và `EnemyUnit.cs`, ví dụ sử dụng `#if PIREX_REXPOOL_ENABLED` để vừa minh họa tốc độ tái sử dụng **O(1)** (Enemy tự động Despawn sau 4 giây, nhấn **E** để Spawn, nhấn **C** để CleanUp), vừa tương thích hoàn toàn khi không cài gói.

---

## 🚀 2. Hướng dẫn thiết lập và trải nghiệm trong Unity Editor

### ⚡ CÁCH NHANH NHẤT: Tạo tự động bằng 1 nút bấm (One-Click Generator)
Bạn không cần phải thiết lập tay từng GameObject! Thư viện cung cấp sẵn một nút tạo tự động:
1. Trở lại Unity Editor, trên thanh menu chọn: **`Tools -> PirexZenith -> Examples -> 🚀 Generate Gameplay Sample Scene & Prefab`**.
2. Hệ thống sẽ tự động:
   - Tạo file `EnemyPrefab.prefab` đã có gắn `EnemyUnit` và `DIPrefabCache`.
   - Tạo mới Scene `GameplaySampleScene.unity` chứa đầy đủ `SceneScope`, `GameplaySceneInstaller`, `PlayerController` và `EnemySpawner`.
   - Tự động **Auto-Bake** danh sách target vào `SceneScope` và lưu Scene.
3. Nhấn **PLAY** để trải nghiệm ngay!

---

### 🛠 CÁCH THỦ CÔNG: Thiết lập từng bước (dành cho người muốn tìm hiểu chi tiết)

1. **Tạo GameObject cho Scope & Installer**:
   - Trong một Scene trống, tạo một GameObject trống tên là `--- PIREX_DI_SCOPE ---`.
   - Gắn component **`SceneScope`** (`PirexZenith.Unity.SceneScope`).
   - Gắn component **`GameplaySceneInstaller`** (`PirexZenith.Examples.GameplaySample.GameplaySceneInstaller`).
   - *Lưu ý*: Bạn có thể kéo thả `GameplaySceneInstaller` vào danh sách Installers của `SceneScope`, hoặc để trống (`SceneScope` tự tìm tự động trong Scene).

2. **Tạo Player GameObject**:
   - Tạo một Cube hoặc GameObject trong Scene tên là `Player`.
   - Gắn component **`PlayerController`** (`PirexZenith.Examples.GameplaySample.PlayerController`).
   - Lúc này trên `PlayerController` có các trường `[Inject]` như `_audioService`, `GameState`, `_stats`.

3. **Tạo Enemy Spawner & Prefab**:
   - Tạo một GameObject trong Scene tên là `EnemySpawner`.
   - Gắn component **`EnemySpawner`** (`PirexZenith.Examples.GameplaySample.EnemySpawner`).
   - Tạo một Prefab Enemy trong thư mục dự án, gắn component **`EnemyUnit`**.
   - (Tùy chọn tối ưu hoá cao): Gắn thêm component **`DIPrefabCache`** vào Prefab Enemy và kéo `EnemyUnit` vào danh sách Cached Targets để tiết kiệm thời gian quét hierarchy con.
   - Kéo Prefab Enemy vào trường `Enemy Prefab` trên inspector của `EnemySpawner`.

4. **Trải nghiệm cơ chế Auto-Baking (O(1) Runtime)**:
   - Nhấn phím **Ctrl + S** (Save Scene) trong Unity Editor.
   - Kiểm tra Inspector của component `SceneScope`: Bạn sẽ thấy danh sách **Baked Targets** tự động lấp đầy bởi `PlayerController` và `EnemySpawner`!
   - Nhấn **Play Game**: Các service được tiêm tức thì với độ phức tạp **O(1)** không cần quét Reflection hay Hierarchy lúc game chạy.

5. **Trải nghiệm cơ chế Bulletproof Fallback**:
   - Click phải vào component `SceneScope` hoặc chọn menu **Tools -> PirexZenith -> Clear Baked Data** để làm trống danh sách `bakedTargets`.
   - Nhấn **Play Game**: Ngay cả khi chưa Bake, `SceneScope` sẽ tự động chuyển sang chế độ **Bulletproof Fallback** (quét runtime) để đảm bảo game chạy an toàn và không bao giờ bị lỗi `NullReferenceException`.

---

## 🛠 Menu hỗ trợ trong Editor

Bạn có thể sử dụng các công cụ tích hợp sẵn trên thanh menu **Tools -> PirexZenith**:
- **Bake Current Scene**: Quét và lưu thủ công danh sách `[Inject]` trong Scene hiện tại vào `SceneScope`.
- **Clear Baked Data**: Xóa dữ liệu Bake của Scene để kiểm tra Fallback Runtime.
- **Clear Editor Scan Cache**: Xóa bộ đệm Reflection Cache của Editor Scanner.
