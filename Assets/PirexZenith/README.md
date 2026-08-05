<div align="center">
  ðŸŒ <strong>Language / NgÃ´n ngá»¯:</strong> <a href="#-english-documentation">English</a> | <a href="#-tÃ i-liá»‡u-tiáº¿ng-viá»‡t">Tiáº¿ng Viá»‡t</a>
</div>

---

# ðŸ‡¬ðŸ‡§ English Documentation

<div align="center">

# ðŸ‘‘ PirexZenith
### The Peak of Fast, Zero-Config & Convenient Unity Dependency Injection

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Performance: O(1)](https://img.shields.io/badge/Performance-O(1)%20Auto--Baking-purple.svg)]()
[![Pooling: RexPool](https://img.shields.io/badge/Pooling-RexPool%20Integrated-orange.svg)]()

</div>

**PirexZenith** is an AAA-grade Dependency Injection & Object Pooling framework for Unity developed by **PirexGames**. It brings together the **expressive convenience of Zenject** and the **blazing execution speed of VContainer**.

---

## ðŸŒŸ Why PirexZenith?

| Feature | Zenject / Extenject | VContainer | ðŸ‘‘ **PirexZenith** |
| :--- | :--- | :--- | :--- |
| **MonoBehaviour DI Workflow** | Automatic via SceneContext | Requires manual binding (`RegisterComponentInHierarchy`) | **Zero-Config (Auto-Bakes on Save)** |
| **Runtime Performance** | Slow (Runtime Reflection) | Blazing Fast (CodeGen / IL) | **Blazing Fast (O(1) Pre-baked Targets)** |
| **Prefab & Pool Injection** | Custom Memory Pool API | `resolver.Instantiate` / Manual Factory setup | **RexPool O(1) Guard + DIPrefabCache** |
| **Editor Wiring Effort** | Heavy manual drag & drop | Manual registration required in Installers | **100% Automated (`Ctrl + S`)** |
| **Missing Data Fallback** | Crashes / Null Reference | Crashes if ungenerated or unregistered | **Bulletproof Runtime Fallback** |

---

## ðŸ“¦ Installation

### Via Unity Package Manager (Git URL)
1. Open Unity Editor and navigate to **Window âž” Package Manager**.
2. Click the **`+`** button in the top left and select **Add package from git URL...**.
3. Enter the repository URL:
   ```text
   https://github.com/PirexGames/PirexZenith.git
   ```

*(Optional)* To enable zero-allocation object pooling, install **`PirexGames.RexPool`** into your project. PirexZenith will automatically detect it and unlock pooled DI extension methods.

---

## ðŸ“š Complete Feature Guide & Code Examples

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

        // 4. Pure C# System: Automatically binds IInitializable, ITickable, IDisposable
        builder.RegisterSystem<GameplayScoreSystem>();

        // 5. Custom Factory: Dynamic creation logic
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
Tools âž” Examples âž” ðŸš€ Generate Gameplay Sample Scene & Prefab
```
This generates a fully configured scene (`GameplaySampleScene.unity`) and pooled prefab (`EnemyPrefab.prefab`) in 1 second. Press **PLAY** to test!

---
---

# ðŸ‡»ðŸ‡³ TÃ i Liá»‡u Tiáº¿ng Viá»‡t

<div align="center">

# ðŸ‘‘ PirexZenith
### ThÆ° viá»‡n Dependency Injection & Object Pooling Tá»‘c Äá»™ Cao Cho Unity

[![Unity Version](https://img.shields.io/badge/Unity-2021.3%2B-blue.svg)](https://unity.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![Performance: O(1)](https://img.shields.io/badge/Performance-O(1)%20Auto--Baking-purple.svg)]()
[![Pooling: RexPool](https://img.shields.io/badge/Pooling-RexPool%20Integrated-orange.svg)]()

</div>

**PirexZenith** lÃ  thÆ° viá»‡n Dependency Injection vÃ  Object Pooling Ä‘áº³ng cáº¥p AAA do **PirexGames** phÃ¡t triá»ƒn, mang trá»n váº¹n triáº¿t lÃ½ káº¿t há»£p **sá»± tiá»‡n lá»£i tuyá»‡t Ä‘á»‘i cá»§a Zenject** vÃ  **tá»‘c Ä‘á»™ thá»±c thi cá»±c Ä‘á»™ cá»§a VContainer**.

---

## ðŸŒŸ Táº¡i sao chá»n PirexZenith?

| TiÃªu chÃ­ | Zenject / Extenject | VContainer | ðŸ‘‘ **PirexZenith** |
| :--- | :--- | :--- | :--- |
| **Quy trÃ¬nh DI MonoBehaviour** | Tá»± Ä‘á»™ng qua SceneContext | Pháº£i Ä‘Äƒng kÃ½ tay (`RegisterComponentInHierarchy`) | **Zero-Config (Tá»± Ä‘á»™ng Bake khi Save)** |
| **Tá»‘c Ä‘á»™ Runtime** | Cháº­m (Reflection Runtime) | Cá»±c nhanh (CodeGen / IL) | **Cá»±c nhanh (O(1) Pre-baked Targets)** |
| **DI cho Prefab & Pool** | Custom Memory Pool API | `resolver.Instantiate` / Factory cáº¥u hÃ¬nh tay | **TÃ­ch há»£p RexPool O(1) + DIPrefabCache** |
| **CÃ´ng sá»©c thiáº¿t láº­p Editor** | KÃ©o tháº£ thá»§ cÃ´ng nhiá»u | Pháº£i khai bÃ¡o thá»§ cÃ´ng trong Installer | **Tá»± Ä‘á»™ng 100% khi Save (`Ctrl + S`)** |
| **Chá»‘ng lá»—i gÃ£y game** | Null Reference náº¿u quÃªn bind | Lá»—i náº¿u chÆ°a generate hoáº·c chÆ°a Ä‘Äƒng kÃ½ | **Bulletproof Fallback tá»± Ä‘á»™ng** |

---

## ðŸ“¦ HÆ°á»›ng dáº«n cÃ i Ä‘áº·t

### CÃ i Ä‘áº·t qua Unity Package Manager (Git URL)
1. Má»Ÿ Unity Editor, vÃ o **Window âž” Package Manager**.
2. Báº¥m nÃºt **`+`** á»Ÿ gÃ³c trÃªn bÃªn trÃ¡i vÃ  chá»n **Add package from git URL...**.
3. DÃ¡n Ä‘Æ°á»ng dáº«n repository:
   ```text
   https://github.com/PirexGames/PirexZenith.git
   ```

*(TÃ¹y chá»n)* Äá»ƒ kÃ­ch hoáº¡t tÃ­nh nÄƒng Object Pooling khÃ´ng tá»‘n GC allocation, hÃ£y cÃ i Ä‘áº·t gÃ³i **`PirexGames.RexPool`**. PirexZenith sáº½ tá»± Ä‘á»™ng nháº­n diá»‡n vÃ  má»Ÿ khÃ³a cÃ¡c hÃ m má»Ÿ rá»™ng Pooling DI.

---

## ðŸ“š HÆ°á»›ng dáº«n toÃ n táº­p tÃ­nh nÄƒng & VÃ­ dá»¥ mÃ£ nguá»“n

### 1. CÃ¡c kiá»ƒu Ä‘Äƒng kÃ½ Binding trong Installer
Há»— trá»£ Ä‘áº§y Ä‘á»§ Singleton, Transient, Instance, Pure C# System vÃ  Factory binding:
```csharp
using PirexZenith;using UnityEngine;

public class GameInstaller : MonoInstaller 
{
    [SerializeField] private GameConfigSO _gameConfig; // ScriptableObject trong Inspector

    public override void InstallBindings(ContainerBuilder builder) 
    {
        // 1. Singleton: Chá»‰ táº¡o 1 instance duy nháº¥t chia sáº» toÃ n bá»™ container
        builder.RegisterSingleton<AudioService, IAudioService>();

        // 2. Transient: Táº¡o má»›i 1 instance má»—i khi Ä‘Æ°á»£c gá»i Resolve hoáº·c Inject
        builder.RegisterTransient<PlayerStats>();

        // 3. Instance: ÄÄƒng kÃ½ má»™t ScriptableObject hoáº·c MonoBehaviour cÃ³ sáºµn
        builder.RegisterInstance(_gameConfig);

        // 4. Pure C# System: Tá»± Ä‘á»™ng bind cÃ¡c interface IInitializable, ITickable, IDisposable
        builder.RegisterSystem<GameplayScoreSystem>();

        // 5. Custom Factory: ÄÄƒng kÃ½ logic táº¡o object tÃ¹y biáº¿n
        builder.RegisterFactory<EnemyAI>(container => new EnemyAI(container.Resolve<IAudioService>()));
    }
}
```

---

### 2. 4 Kiá»ƒu tiÃªm Dependency (MonoBehaviour & Class C#)
Há»— trá»£ tiÃªm qua Constructor, Field, Property vÃ  Method trÃªn cáº£ MonoBehaviour láº«n class C# thÆ°á»ng:
```csharp
using PirexZenith;using UnityEngine;

public class PlayerController : MonoBehaviour 
{
    // 1. Field Injection
    [Inject] private IAudioService _audioService;

    // 2. Property Injection
    [Inject] public IGameStateService GameState { get; private set; }

    private IBattleRuleService _battleRule;

    // 3. Method Injection (ÄÆ°á»£c gá»i tá»± Ä‘á»™ng sau khi khá»Ÿi táº¡o)
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

### 3. Há»‡ thá»‘ng vÃ²ng Ä‘á»i C# thuáº§n (Pure C# Lifecycle Systems)
Viáº¿t game logic hiá»‡u nÄƒng cao báº±ng lá»›p C# thuáº§n, khÃ´ng cáº§n káº¿ thá»«a `MonoBehaviour` mÃ  váº«n tham gia vÃ²ng Ä‘á»i Unity:
* `IInitializable`: `Initialize()` cháº¡y 1 láº§n khi Scope báº¯t Ä‘áº§u.
* `ITickable`: `Tick()` cháº¡y má»—i frame trong `Update()`.
* `IFixedTickable`: `FixedTick()` cháº¡y trong `FixedUpdate()`.
* `ILateTickable`: `LateTick()` cháº¡y trong `LateUpdate()`.
* `IDisposable`: `Dispose()` tá»± Ä‘á»™ng dá»n dáº¹p khi Scope há»§y.

```csharp
using PirexZenith;using PirexZenith;using UnityEngine;

public class GameplayScoreSystem : IInitializable, ITickable, IDisposable
{
    private readonly IAudioService _audioService;

    // 4. Constructor Injection (Æ¯u tiÃªn constructor gáº¯n tháº» [Inject])
    [Inject]
    public GameplayScoreSystem(IAudioService audioService)
    {
        _audioService = audioService;
    }

    public void Initialize() => Debug.Log("System initialized!");
    public void Tick() { /* Cháº¡y má»—i frame */ }
    public void Dispose() { /* Dá»n dáº¹p tÃ i nguyÃªn */ }
}
```

---

### 4. TiÃªm vÃ o Class C# bÃ¬nh thÆ°á»ng (Non-MonoBehaviour)
Báº¡n cÃ³ thá»ƒ Ä‘á»ƒ Container tá»± táº¡o object (`Resolve<T>`) hoáº·c tiÃªm vÃ o object C# bÃ¬nh thÆ°á»ng do báº¡n tá»± táº¡o:
```csharp
// CÃ¡ch 1 (KhuyÃªn dÃ¹ng): Resolve tá»« Container (Tá»± táº¡o má»›i vÃ  Inject trá»n váº¹n Constructor/Field/Property/Method)
var ai = _container.Resolve<EnemyAIController>();

// CÃ¡ch 2: TiÃªm vÃ o má»™t object C# bÃ¬nh thÆ°á»ng Ä‘Ã£ Ä‘Æ°á»£c new sáºµn
var myNormalObj = new EnemyAIController();
_container.Inject(myNormalObj);
```

---

### 5. Zero-Config Auto-Baking (Scene & Prefab)
**Báº¡n KHÃ”NG BAO GIá»œ pháº£i Ä‘Äƒng kÃ½ tay MonoBehaviour hay kÃ©o tháº£ list target!**
* **Khi Save Prefab (`Ctrl+S`):**
  * Náº¿u Prefab chá»©a script mang tháº» `[Inject]`, há»‡ thá»‘ng `PrefabBakingSystem` tá»± Ä‘á»™ng gáº¯n `DIPrefabCache` vÃ  náº¡p danh sÃ¡ch target vá»›i tá»‘c Ä‘á»™ O(1).
* **Khi Save Scene (`Ctrl+S`):**
  * Náº¿u Scene chá»©a script mang tháº» `[Inject]` nhÆ°ng chÆ°a cÃ³ Scope, há»‡ thá»‘ng `DIEditorScanner` tá»± Ä‘á»™ng táº¡o `--- PIREX_DI_SCOPE ---` gáº¯n `LifetimeScope` vÃ  náº¡p sáºµn target.
* **Bake toÃ n bá»™ Prefab:** Menu **`Tools -> PirexZenith -> Bake All Prefabs in Project`** tá»± Ä‘á»™ng quÃ©t vÃ  Bake táº¥t cáº£ Prefab trong dá»± Ã¡n chá»‰ vá»›i 1 click.

---

### 6. Object Pooling 0-GC káº¿t há»£p DI (TÃ­ch há»£p RexPool)
Cung cáº¥p cÃ¡c hÃ m má»Ÿ rá»™ng trá»±c tiáº¿p trÃªn `DIContainer` vá»›i chá»¯ kÃ½ hÃ m giá»‘ng há»‡t `RexPoolManager`, báº£o vá»‡ báº±ng cá» `IsInjected` tá»‘c Ä‘á»™ O(1):
```csharp
// 1. Pre-warm 10 object trong pool
_container.Prepair(_enemyPrefab, 10);

// 2. Take tá»« pool (Tá»± Ä‘á»™ng bá» qua reflection DI náº¿u object tÃ¡i sá»­ dá»¥ng Ä‘Ã£ Ä‘Æ°á»£c inject trÆ°á»›c Ä‘Ã³)
GameObject enemy = _container.Take(_enemyPrefab, spawnPosition, spawnRotation);

// 3. Thu há»“i object vá» pool
_container.CleanUp(_enemyPrefab);
```
*(LÆ°u Ã½: Náº¿u khÃ´ng cÃ i RexPool, cÃ¡c hÃ m nÃ y tá»± Ä‘á»™ng fallback sang `Object.Instantiate` cá»§a Unity mÃ  khÃ´ng bÃ¡o lá»—i.)*

---

### 7. Cáº¥u trÃºc Scope, Ghi Ä‘Ã¨ Service & Tra cá»©u cha con (`nameof`)
PirexZenith sá»­ dá»¥ng **`LifetimeScope`** lÃ m component duy nháº¥t quáº£n lÃ½ Container cho má»i cáº¥p Ä‘á»™ (Project, Scene, Prefab, UI Modal).
TrÃªn cá»­a sá»• Inspector cá»§a **`LifetimeScope`**, menu dropdown **Parent Scope Class** há»— trá»£ 3 cháº¿ Ä‘á»™:
1. `[ Auto / Inherit Active Parent Scope ]` **(Máº·c Ä‘á»‹nh):** Tá»± Ä‘á»™ng káº¿ thá»«a Scope cha Ä‘ang cháº¡y gáº§n nháº¥t.
2. `[ None / Standalone Scope ]`: Hoáº¡t Ä‘á»™ng Ä‘á»™c láº­p hoÃ n toÃ n.
3. `nameof(ParentScopeClass)`: Káº¿t ná»‘i tá»›i lá»›p Scope chá»‰ Ä‘á»‹nh (`nameof(CoreSceneScope)`), an toÃ n tuyá»‡t Ä‘á»‘i khi Ä‘á»•i tÃªn báº±ng IDE Refactor.

```csharp
// VÃ­ dá»¥: GameplaySceneScope káº¿ thá»«a Service tá»« CoreSceneScope vÃ  cÃ³ thá»ƒ Ä‘Äƒng kÃ½ ghi Ä‘Ã¨
public class GameplaySceneScope : LifetimeScope
{
    public override string ParentScopeName => nameof(CoreSceneScope);
}
```

---

### 8. CÃ´ng cá»¥ Editor & One-Click Example Generator
Tráº£i nghiá»‡m ngay trong Unity Editor báº±ng cÃ¡ch vÃ o menu:
```text
Tools âž” Examples âž” ðŸš€ Generate Gameplay Sample Scene & Prefab
```
Há»‡ thá»‘ng sáº½ tá»± Ä‘á»™ng táº¡o Scene máº«u (`GameplaySampleScene.unity`) vÃ  Prefab (`EnemyPrefab.prefab`) Ä‘Ã£ Ä‘Æ°á»£c káº¿t ná»‘i trá»n váº¹n DI & Object Pooling chá»‰ trong 1 giÃ¢y. Nháº¥n **PLAY** Ä‘á»ƒ kiá»ƒm thá»­ ngay!
