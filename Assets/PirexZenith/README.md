<div align="center">
  🌐 <strong>Language / Ngôn ngữ:</strong> <a href="#-english-documentation">English</a> | <a href="#-tài-liệu-tiếng-việt">Tiếng Việt</a>
</div>

---

# 🇬🇧 English Documentation

<div align="center">

# 👑 PirexZenith
### The Peak of Fast, Zero-Config & Convenient Unity Dependency Injection

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Performance: O(1)](https://img.shields.io/badge/Performance-O(1)%20Auto--Baking-purple.svg)]()
[![Pooling: RexPool](https://img.shields.io/badge/Pooling-RexPool%20Integrated-orange.svg)]()

</div>

**PirexZenith** is an AAA-grade Dependency Injection & Object Pooling framework for Unity developed by **PirexGames**. It brings together the **expressive convenience of Zenject** and the **blazing execution speed of VContainer**.

---

## 🌟 Why PirexZenith?

| Feature | Zenject / Extenject | VContainer | 👑 **PirexZenith** |
| :--- | :--- | :--- | :--- |
| **MonoBehaviour DI Workflow** | Automatic via SceneContext | Requires manual binding (`RegisterComponentInHierarchy`) | **Zero-Config (Auto-Bakes on Save)** |
| **Runtime Performance** | Slow (Runtime Reflection) | Blazing Fast (CodeGen / IL) | **Blazing Fast (O(1) Pre-baked Targets)** |
| **Prefab & Pool Injection** | Custom Memory Pool API | `resolver.Instantiate` / Manual Factory setup | **RexPool O(1) Guard + DIPrefabCache** |
| **Editor Wiring Effort** | Heavy manual drag & drop | Manual registration required in Installers | **100% Automated (`Ctrl + S`)** |
| **Missing Data Fallback** | Crashes / Null Reference | Crashes if ungenerated or unregistered | **Bulletproof Runtime Fallback** |

### 🚀 Performance Benchmark
*Measured on Unity 2022.3 (Mono/IL2CPP). Lower time is better.*

| Test Name | 👑 PirexZenith | VContainer | Reflex |
| :--- | :--- | :--- | :--- |
| **1. Container Build** (100 Svc) | **0.07ms \| 0 B \| GC:0** 🏆 | 0.39ms \| 0 B \| GC:0 | 0.12ms \| 0 B \| GC:0 |
| **2. 50,000x Transient Resolve** | **25.48ms \| 0 B \| GC:0** 🏆 | 144.62ms \| 0 B \| GC:0 | 93.60ms \| 0 B \| GC:0 |
| **3. 100,000x Singleton Resolve** | **13.34ms \| 0 B \| GC:0** 🏆 | 16.20ms \| 0 B \| GC:0 | 20.18ms \| 0 B \| GC:0 |
| **4. 5,000x GameObject DI Inject** | **93.99ms \| 0 B \| GC:0** 🏆 | 108.18ms \| 0 B \| GC:0 | 97.21ms \| 0 B \| GC:0 |
| **5. 20,000x Deep Tree** (5-Level) | **18.79ms \| 0 B \| GC:0** 🏆 | 101.05ms \| 0 B \| GC:0 | 61.01ms \| 0 B \| GC:0 |
| **6. 10,000x ResolveAll\<T>** (Multi-Bind) | **5.17ms \| 0 B \| GC:0** 🏆 | 30.72ms \| 0 B \| GC:0 | 17.64ms \| 0 B \| GC:0 |

---

## 📦 Installation

### Via Unity Package Manager (Git URL)
1. Open Unity Editor and navigate to **Window ➔ Package Manager**.
2. Click the **`+`** button in the top left and select **Add package from git URL...**.
3. Enter the repository URL:
   ```text
   https://github.com/PirexGames/PirexZenith.git
   ```

*(Optional)* To enable zero-allocation object pooling, install **`PirexGames.RexPool`** into your project. PirexZenith will automatically detect it and unlock pooled DI extension methods.

---

## 📚 Complete Feature Guide & Code Examples

### 1. Registering Bindings in Installers
PirexZenith supports Singleton, Transient, Instance, Pure C# System, and Factory bindings:
```csharp
using PirexZenith;using UnityEngine;

public class GameInstaller : MonoInstaller 
{
    [SerializeField] private GameConfigSO _gameConfig; // ScriptableObject in Inspector

    public override void InstallBindings(ContainerBuilder builder) 
    {
        // 1. Singleton: One shared instance across the container
        builder.RegisterSingleton<AudioService, IAudioService>();

        // 2. Transient: A fresh new instance created every time it is resolved or injected
        builder.RegisterTransient<PlayerStats>();

        // 3. Instance: Bind an existing ScriptableObject or scene MonoBehaviour
        builder.RegisterInstance(_gameConfig);

        // 4. Custom Factory: Dynamic creation logic
        builder.RegisterFactory<EnemyAI>(container => new EnemyAI(container.Resolve<IAudioService>()));
    }
}
```

---

### 2. Four Injection Styles (MonoBehaviours & C# Classes)
PirexZenith supports Constructor, Field, Property, and Method injection on both MonoBehaviours and regular C# classes:
```csharp
using PirexZenith;using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    // 1. Field Injection
    [Inject] private IAudioService _audioService;

    // 2. Property Injection
    [Inject] public IGameStateService GameState { get; private set; }

    private IBattleRuleService _battleRule;

    // 3. Method Injection (Invoked automatically after instantiation / construction)
    [Inject]
    public void Construct(IBattleRuleService battleRule)
    {
        _battleRule = battleRule;
    }

    private void Start() 
    {
        _audioService.PlayMusic("BattleTheme");
    }
}
```

---

### 3. Pure C# Lifecycle Systems (No MonoBehaviour Needed)
Write high-performance game logic as pure C# classes that participate in Unity's frame loop without inheriting from `MonoBehaviour`:
* `IInitializable`: `Initialize()` called once when scope starts.
* `ITickable`: `Tick()` called every frame in `Update()`.
* `IFixedTickable`: `FixedTick()` called in `FixedUpdate()`.
* `ILateTickable`: `LateTick()` called in `LateUpdate()`.
* `IDisposable`: `Dispose()` called automatically when scope is destroyed.

```csharp
using PirexZenith;using PirexZenith;using UnityEngine;

public class GameplayScoreSystem : IInitializable, ITickable, IDisposable
{
    private readonly IAudioService _audioService;

    // 4. Constructor Injection (Prioritizes constructor marked with [Inject])
    [Inject]
    public GameplayScoreSystem(IAudioService audioService)
    {
        _audioService = audioService;
    }

    public void Initialize() => Debug.Log("System initialized!");
    public void Tick() { /* Runs every frame */ }
    public void Dispose() { /* Cleans up resources */ }
}
```

---

### 4. Injecting into Normal C# Classes (Non-MonoBehaviour)
You can inject into normal C# classes either by letting the Container create them (`Resolve<T>`) or by injecting an existing instance (`container.Inject`):
```csharp
// Option A (Recommended): Resolve from container (Auto-creates and injects Constructor/Fields/Properties/Methods)
var ai = _container.Resolve<EnemyAIController>();

// Option B: Inject into an existing normal C# instance that you created manually
var myNormalObj = new EnemyAIController();
_container.Inject(myNormalObj);
```

---

### 5. Zero-Config Auto-Baking (Scenes & Prefabs)
**You NEVER need to manually register MonoBehaviours or drag-and-drop target lists!**
* **When Saving a Prefab (`Ctrl+S`):**
  * If the Prefab contains `[Inject]` scripts, `PrefabBakingSystem` automatically attaches `DIPrefabCache` and bakes target components for O(1) instantiation speed.
* **When Saving a Scene (`Ctrl+S`):**
  * If `[Inject]` scripts are detected without a Scope, `DIEditorScanner` automatically creates `--- PIREX_DI_SCOPE ---` with a `LifetimeScope` component and pre-populates `bakedTargets`.
* **Bake All Prefabs:** Menu command **`Tools -> PirexZenith -> Bake All Prefabs in Project`** bakes all prefabs in your project with 1 click.

---

### 6. Zero-Allocation Object Pooling & DI (RexPool Integration)
PirexZenith provides native extension methods for `DIContainer` matching `RexPoolManager` signatures, protected by an O(1) DI injection guard (`IsInjected`):
```csharp
// 1. Pre-warm 10 instances in pool
_container.Prepair(_enemyPrefab, 10);

// 2. Take from pool (Skips DI reflection if the recycled object was already injected once)
GameObject enemy = _container.Take(_enemyPrefab, spawnPosition, spawnRotation);

// 3. Return object to pool
_container.CleanUp(_enemyPrefab);
```
*(Note: If RexPool is not installed, these methods automatically fallback to standard Unity `Object.Instantiate` without errors.)*

---

### 7. Scope Hierarchy, Overrides & Parent Lookup (`nameof`)
PirexZenith uses **`LifetimeScope`** as the unified container component across all hierarchy levels (Project Scope, Scene Scope, Prefabs, UI Modals).
In the **`LifetimeScope`** Unity Inspector, configure how scopes inherit dependencies using the **Parent Scope Class** dropdown:
1. `[ Auto / Inherit Active Parent Scope ]` **(Default):** Automatically links to the currently active parent scope in `ActiveScopes`.
2. `[ None / Standalone Scope ]`: Operates independently without inheriting from any parent container.
3. `nameof(ParentScopeClass)`: Explicitly links to a named scope class (`nameof(CoreSceneScope)`), keeping references safe during IDE refactoring.

```csharp
// Example: GameplaySceneScope inherits bindings from CoreSceneScope and can override them
public class GameplaySceneScope : LifetimeScope
{
    public override string ParentScopeName => nameof(CoreSceneScope);
}
```

---

### 8. Editor Tools & One-Click Example Generator
Test PirexZenith instantly in Unity Editor by navigating to:
```text
Tools ➔ Examples ➔ 🚀 Generate Gameplay Sample Scene & Prefab
```
This generates a fully configured scene (`GameplaySampleScene.unity`) and pooled prefab (`EnemyPrefab.prefab`) in 1 second. Press **PLAY** to test!

---
---

# 🇻🇳 Tài Liệu Tiếng Việt

<div align="center">

# 👑 PirexZenith
### Thư viện Dependency Injection & Object Pooling Tốc Độ Cao Cho Unity

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Performance: O(1)](https://img.shields.io/badge/Performance-O(1)%20Auto--Baking-purple.svg)]()
[![Pooling: RexPool](https://img.shields.io/badge/Pooling-RexPool%20Integrated-orange.svg)]()

</div>

**PirexZenith** là thư viện Dependency Injection và Object Pooling đẳng cấp AAA do **PirexGames** phát triển, mang trọn vẹn triết lý kết hợp **sự tiện lợi tuyệt đối của Zenject** và **tốc độ thực thi cực độ của VContainer**.

---

## 🌟 Tại sao chọn PirexZenith?

| Tiêu chí | Zenject / Extenject | VContainer | 👑 **PirexZenith** |
| :--- | :--- | :--- | :--- |
| **Quy trình DI MonoBehaviour** | Tự động qua SceneContext | Phải đăng ký tay (`RegisterComponentInHierarchy`) | **Zero-Config (Tự động Bake khi Save)** |
| **Tốc độ Runtime** | Chậm (Reflection Runtime) | Cực nhanh (CodeGen / IL) | **Cực nhanh (O(1) Pre-baked Targets)** |
| **DI cho Prefab & Pool** | Custom Memory Pool API | `resolver.Instantiate` / Factory cấu hình tay | **Tích hợp RexPool O(1) + DIPrefabCache** |
| **Công sức thiết lập Editor** | Kéo thả thủ công nhiều | Phải khai báo thủ công trong Installer | **Tự động 100% khi Save (`Ctrl + S`)** |
| **Chống lỗi gãy game** | Null Reference nếu quên bind | Lỗi nếu chưa generate hoặc chưa đăng ký | **Bulletproof Fallback tự động** |

### 🚀 Bảng đo lường hiệu năng (Benchmark)
*Đo trên Unity 2022.3 (Mono/IL2CPP). Thời gian càng thấp càng tốt.*

| Tên bài test | 👑 PirexZenith | VContainer | Reflex |
| :--- | :--- | :--- | :--- |
| **1. Container Build** (100 Svc) | **0.07ms \| 0 B \| GC:0** 🏆 | 0.39ms \| 0 B \| GC:0 | 0.12ms \| 0 B \| GC:0 |
| **2. 50,000x Transient Resolve** | **25.48ms \| 0 B \| GC:0** 🏆 | 144.62ms \| 0 B \| GC:0 | 93.60ms \| 0 B \| GC:0 |
| **3. 100,000x Singleton Resolve** | **13.34ms \| 0 B \| GC:0** 🏆 | 16.20ms \| 0 B \| GC:0 | 20.18ms \| 0 B \| GC:0 |
| **4. 5,000x GameObject DI Inject** | **93.99ms \| 0 B \| GC:0** 🏆 | 108.18ms \| 0 B \| GC:0 | 97.21ms \| 0 B \| GC:0 |
| **5. 20,000x Deep Tree** (5-Level) | **18.79ms \| 0 B \| GC:0** 🏆 | 101.05ms \| 0 B \| GC:0 | 61.01ms \| 0 B \| GC:0 |
| **6. 10,000x ResolveAll\<T>** (Multi-Bind) | **5.17ms \| 0 B \| GC:0** 🏆 | 30.72ms \| 0 B \| GC:0 | 17.64ms \| 0 B \| GC:0 |

---

## 📦 Hướng dẫn cài đặt

### Cài đặt qua Unity Package Manager (Git URL)
1. Mở Unity Editor, vào **Window ➔ Package Manager**.
2. Bấm nút **`+`** ở góc trên bên trái và chọn **Add package from git URL...**.
3. Dán đường dẫn repository:
   ```text
   https://github.com/PirexGames/PirexZenith.git
   ```

*(Tùy chọn)* Để kích hoạt tính năng Object Pooling không tốn GC allocation, hãy cài đặt gói **`PirexGames.RexPool`**. PirexZenith sẽ tự động nhận diện và mở khóa các hàm mở rộng Pooling DI.

---

## 📚 Hướng dẫn toàn tập tính năng & Ví dụ mã nguồn

### 1. Các kiểu đăng ký Binding trong Installer
Hỗ trợ đầy đủ Singleton, Transient, Instance, Pure C# System và Factory binding:
```csharp
using PirexZenith;using UnityEngine;

public class GameInstaller : MonoInstaller 
{
    [SerializeField] private GameConfigSO _gameConfig; // ScriptableObject trong Inspector

    public override void InstallBindings(ContainerBuilder builder) 
    {
        // 1. Singleton: Chỉ tạo 1 instance duy nhất chia sẻ toàn bộ container
        builder.RegisterSingleton<AudioService, IAudioService>();

        // 2. Transient: Tạo mới 1 instance mỗi khi được gọi Resolve hoặc Inject
        builder.RegisterTransient<PlayerStats>();

        // 3. Instance: Đăng ký một ScriptableObject hoặc MonoBehaviour có sẵn
        builder.RegisterInstance(_gameConfig);

        // 4. Pure C# System: Tự động bind các interface IInitializable, ITickable, IDisposable
        builder.RegisterSystem<GameplayScoreSystem>();

        // 5. Custom Factory: Đăng ký logic tạo object tùy biến
        builder.RegisterFactory<EnemyAI>(container => new EnemyAI(container.Resolve<IAudioService>()));
    }
}
```

---

### 2. 4 Kiểu tiêm Dependency (MonoBehaviour & Class C#)
Hỗ trợ tiêm qua Constructor, Field, Property và Method trên cả MonoBehaviour lẫn class C# thường:
```csharp
using PirexZenith;using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    // 1. Field Injection
    [Inject] private IAudioService _audioService;

    // 2. Property Injection
    [Inject] public IGameStateService GameState { get; private set; }

    private IBattleRuleService _battleRule;

    // 3. Method Injection (Được gọi tự động sau khi khởi tạo)
    [Inject]
    public void Construct(IBattleRuleService battleRule)
    {
        _battleRule = battleRule;
    }

    private void Start() 
    {
        _audioService.PlayMusic("BattleTheme");
    }
}
```

---

### 3. Hệ thống vòng đời C# thuần (Pure C# Lifecycle Systems)
Viết game logic hiệu năng cao bằng lớp C# thuần, không cần kế thừa `MonoBehaviour` mà vẫn tham gia vòng đời Unity:
* `IInitializable`: `Initialize()` chạy 1 lần khi Scope bắt đầu.
* `ITickable`: `Tick()` chạy mỗi frame trong `Update()`.
* `IFixedTickable`: `FixedTick()` chạy trong `FixedUpdate()`.
* `ILateTickable`: `LateTick()` chạy trong `LateUpdate()`.
* `IDisposable`: `Dispose()` tự động dọn dẹp khi Scope hủy.

```csharp
using PirexZenith;using PirexZenith;using UnityEngine;

public class GameplayScoreSystem : IInitializable, ITickable, IDisposable
{
    private readonly IAudioService _audioService;

    // 4. Constructor Injection (Ưu tiên constructor gắn thẻ [Inject])
    [Inject]
    public GameplayScoreSystem(IAudioService audioService)
    {
        _audioService = audioService;
    }

    public void Initialize() => Debug.Log("System initialized!");
    public void Tick() { /* Chạy mỗi frame */ }
    public void Dispose() { /* Dọn dẹp tài nguyên */ }
}
```

---

### 4. Tiêm vào Class C# bình thường (Non-MonoBehaviour)
Bạn có thể để Container tự tạo object (`Resolve<T>`) hoặc tiêm vào object C# bình thường do bạn tự tạo:
```csharp
// Cách 1 (Khuyên dùng): Resolve từ Container (Tự tạo mới và Inject trọn vẹn Constructor/Field/Property/Method)
var ai = _container.Resolve<EnemyAIController>();

// Cách 2: Tiêm vào một object C# bình thường đã được new sẵn
var myNormalObj = new EnemyAIController();
_container.Inject(myNormalObj);
```

---

### 5. Zero-Config Auto-Baking (Scene & Prefab)
**Bạn KHÔNG BAO GIỜ phải đăng ký tay MonoBehaviour hay kéo thả list target!**
* **Khi Save Prefab (`Ctrl+S`):**
  * Nếu Prefab chứa script mang thẻ `[Inject]`, hệ thống `PrefabBakingSystem` tự động gắn `DIPrefabCache` và nạp danh sách target với tốc độ O(1).
* **Khi Save Scene (`Ctrl+S`):**
  * Nếu Scene chứa script mang thẻ `[Inject]` nhưng chưa có Scope, hệ thống `DIEditorScanner` tự động tạo `--- PIREX_DI_SCOPE ---` gắn `LifetimeScope` và nạp sẵn target.
* **Bake toàn bộ Prefab:** Menu **`Tools -> PirexZenith -> Bake All Prefabs in Project`** tự động quét và Bake tất cả Prefab trong dự án chỉ với 1 click.

---

### 6. Object Pooling 0-GC kết hợp DI (Tích hợp RexPool)
Cung cấp các hàm mở rộng trực tiếp trên `DIContainer` với chữ ký hàm giống hệt `RexPoolManager`, bảo vệ bằng cờ `IsInjected` tốc độ O(1):
```csharp
// 1. Pre-warm 10 object trong pool
_container.Prepair(_enemyPrefab, 10);

// 2. Take từ pool (Tự động bỏ qua reflection DI nếu object tái sử dụng đã được inject trước đó)
GameObject enemy = _container.Take(_enemyPrefab, spawnPosition, spawnRotation);

// 3. Thu hồi object về pool
_container.CleanUp(_enemyPrefab);
```
*(Lưu ý: Nếu không cài RexPool, các hàm này tự động fallback sang `Object.Instantiate` của Unity mà không báo lỗi.)*

---

### 7. Cấu trúc Scope, Ghi đè Service & Tra cứu cha con (`nameof`)
PirexZenith sử dụng **`LifetimeScope`** làm component duy nhất quản lý Container cho mọi cấp độ (Project, Scene, Prefab, UI Modal).
Trên cửa sổ Inspector của **`LifetimeScope`**, menu dropdown **Parent Scope Class** hỗ trợ 3 chế độ:
1. `[ Auto / Inherit Active Parent Scope ]` **(Mặc định):** Tự động kế thừa Scope cha đang chạy gần nhất.
2. `[ None / Standalone Scope ]`: Hoạt động độc lập hoàn toàn.
3. `nameof(ParentScopeClass)`: Kết nối tới lớp Scope chỉ định (`nameof(CoreSceneScope)`), an toàn tuyệt đối khi đổi tên bằng IDE Refactor.

```csharp
// Ví dụ: GameplaySceneScope kế thừa Service từ CoreSceneScope và có thể đăng ký ghi đè
public class GameplaySceneScope : LifetimeScope
{
    public override string ParentScopeName => nameof(CoreSceneScope);
}
```

---

### 8. Công cụ Editor & One-Click Example Generator
Trải nghiệm ngay trong Unity Editor bằng cách vào menu:
```text
Tools ➔ Examples ➔ 🚀 Generate Gameplay Sample Scene & Prefab
```
Hệ thống sẽ tự động tạo Scene mẫu (`GameplaySampleScene.unity`) và Prefab (`EnemyPrefab.prefab`) đã được kết nối trọn vẹn DI & Object Pooling chỉ trong 1 giây. Nhấn **PLAY** để kiểm thử ngay!
