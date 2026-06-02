# 音频播放系统

## 目标

运行时声音统一由 `Otowa.Audio.GameAudioManager` 播放。剧情脚本只引用
`AudioId`，不直接拖拽 `AudioClip`，也不为每个场景添加独立
`AudioSource`。

系统遵循以下规则：

- BGM 始终循环，同一时刻只有一个逻辑 BGM。切歌时允许两个内部
  `AudioSource` 短暂重叠，以实现淡入淡出。
- SFX 可以一次性播放，也可以循环播放。
- SFX 没有并发数量限制；播放器会按需扩展并复用内部 `AudioSource`。
- 音量、淡入、淡出都通过运行时指令调整，不需要编辑音频文件。
- `GameAudioManager` 会在场景加载前自动创建，并通过
  `DontDestroyOnLoad` 跨场景保留。

## 资源引用

集中引用资产位于：

`Assets/Resources/Audio/GameAudioCatalog.asset`

该资产由菜单自动生成：

`Tools/Otowa/Audio/Rebuild Game Audio Catalog`

检查引用是否完整：

`Tools/Otowa/Audio/Validate Game Audio Catalog`

新增或替换音频时：

1. 在 `Assets/Scripts/AudioSystem/AudioId.cs` 添加或复用 `AudioId`。
2. 在 `Assets/Editor/GameAudioCatalogBuilder.cs` 配置文件路径和默认音量。
3. 执行重建菜单。
4. 执行验证菜单，确认没有缺失引用。

业务脚本只使用 `AudioId.SwitchClick` 之类的稳定标识，不依赖文件名。
例如仓库中的文件名目前是 `swtich click.mp3`，这个历史拼写不会传播到
剧情代码。

## 常用指令

```csharp
using Otowa.Audio;

var audio = GameAudioManager.Instance;

// BGM：默认循环；切换时淡入淡出。
audio.PlayBgm(AudioId.NightWalk, fadeIn: 0.6f);
audio.CrossFadeBgm(AudioId.OtowaBlues, duration: 1.0f);
audio.FadeBgmTo(volume: 0.35f, duration: 0.5f);
audio.StopBgm(fadeOut: 0.4f);

// 临时离开地图时保存 BGM 进度，返回后从保存位置继续。
audio.StopBgm(fadeOut: 0.25f, savePosition: true);
audio.PlayBgm(AudioId.DayWalk, fadeIn: 0.35f, resumePlayback: true);

// 一次性 SFX。
audio.PlaySfxOnce(AudioId.DoorOpen);

// 循环 SFX：默认按 AudioId 去重，可跨场景按 ID 停止。
audio.PlaySfxLoop(AudioId.Wind, fadeIn: 0.4f);
audio.FadeSfxLoopTo(AudioId.Wind, volume: 0.2f, duration: 0.5f);
audio.StopSfxLoop(AudioId.Wind, fadeOut: 0.4f);
audio.StopAllSfx(); // 切入需要完全清空声音的演出时使用。

// 同一个循环 SFX 需要并发多份时，保留 handle。
var handle = audio.PlaySfxLoop(AudioId.Wind, allowDuplicate: true);
audio.StopSfxLoop(handle, fadeOut: 0.4f);

// 总线音量也支持渐变。
audio.SetMasterVolume(0.8f, duration: 0.3f);
audio.SetBgmBusVolume(0.6f, duration: 0.3f);
audio.SetSfxBusVolume(0.9f, duration: 0.3f);
```

## Intro 接入

Intro 使用与 Day 3 蒙太奇相同的集中播放方式。剧情控制器只发送
`GameAudioManager` 指令，不再在场景中拖拽 `AudioClip` 或创建独立
`AudioSource`。

| 场景 | 主要声音 |
|------|----------|
| `Intro-1  (START)` | 列车环境；进入山林后切换到鸟鸣，抵达时播放列车声 |
| `JunkoIntro` | `day-walk` 与森林环境持续播放 |
| `Intro-3` | 延续 `day-walk`，进门时停止森林环境并播放开门声；翻信件时播放翻页声 |
| `Intro-4` | 敲门声后淡入 `crisis` |
| `TutorialToRyotei` | 停止 BGM，循环播放风声 |
| `Intro-5` | 淡入 `ryotei`；倒酒、碰杯；灵感解锁后停顿、淡出并播放敲门声，Inspector 到场时淡入 `crisis`，后段切换 `decision` |

Day 1 展览教程取得 Hikaru 日志时，由
`ExhibitionDay1TutorialController` 播放一次 `jingle`。Intro-5 取得地图时，
由 `RyoteiController` 播放一次 `jingle`。

## Day 1 地图接入

Day 1 夜间探索进入 `Day1World` 时播放 `night-walk`。进入
`Day1HotSpring` 前保存地图音乐位置，室内切换到 `hot-spring`；离开温泉后
回到地图并从保存位置继续播放 `night-walk`。

Yuji 首次地图对话期间暂停地图 BGM，先循环播放 `blues beat`，随后切换为
风声；选项出现前停止风声并恢复 `night-walk`。返回车站休息后，`day1end`
梦境演出播放 `hot-spring`，长鸣文字出现时停止 BGM 并播放一次近距离汽笛。
Rin 醒来后循环播放森林环境；进入 Day 2 展览前停止环境音、播放一次列车声，
等待两秒后淡出。

## Day 2 接入

Day 2 的地图 BGM 需要跨室内场景保存进度：

| 场景 | 主要声音 |
|------|----------|
| `Day2World` 开场 | 森林环境、Inspector 脚步、`crisis` |
| `Day2World` 自由探索 | `day-walk`；从室内返回时从保存位置继续 |
| `Day2World` 与 Rintaro 对话 | 对话期间额外播放森林环境，不影响 `day-walk` |
| `Day2Ryotei` 首次进入 | 切菜声循环；短暂播放 `otowa blues`，Jiro 看到 Rin 后按键并关闭 BGM |
| `Day2Ryotei` 再次进入 | 对话未耗尽时只有切菜声；耗尽后 BGM 和 SFX 都不播放 |
| `Day2HotSpring` | `hot-spring` |
| `day2end` | 开场清空声音并循环播放虫鸣；DAY 3 标题后切换为远处汽笛和森林环境；进入 Day 3 展览前播放列车声并等待两秒 |

进入 `Day2Ryotei` 或 `Day2HotSpring` 前，`Day2InteriorEntrance` 会调用：

```csharp
GameAudioManager.Instance.StopBgm(0.25f, savePosition: true);
```

回到 `Day2World` 后，`Day2MapFlowController` 会调用：

```csharp
GameAudioManager.Instance.PlayBgm(
    AudioId.DayWalk,
    fadeIn: 0.35f,
    resumePlayback: true);
```

## Day 3 接入

Day 3 的声音触发点直接写在剧情控制器中：

| 场景 | 主要声音 |
|------|----------|
| `Day3HikaruArrival` | 森林环境、脚步、`night-walk`、唱片提示、开门、按钮、`otowa blues` |
| `Day3OtowaBluesMontage` | `otowa blues` 音量变化、列车环境、按钮、打鼾 |
| `Day3SummerFestivalSquare` | 风声、远处汽笛、近处汽笛 |
| `Day3NightTrainArrival` | 跑步、进站汽笛、`ending` |
| `Day3InspectorDecision` | 风声、逐页翻页、`ending` |
| `Day3FinaleCredits` | 烟花、`ending` 到 `otowa blues` 的交叉淡入淡出 |

祭典鼓点不播放。结局台词中仍可保留对鼓点的文字描述。

## Exhibition Day 2 / Day 3 接入

`ExhibitionDay2Scene` 和 `ExhibitionDay3Scene` 共用
`ExhibitionAudioController`。控制器会在场景加载后自动创建，不需要在场景
中添加 `AudioSource` 或拖拽 `AudioClip`。

| 触发点 | 声音 |
|--------|------|
| 进入展览场景 | 循环播放 `gameplay` BGM |
| 每个访客验证成功 | 播放一次 `inspiration unlocked` |
| 每个访客验证失败 | 播放一次 `failure` |
| 整场展览成功 | 播放一次 `jingle` |

## 设计边界

- 新的剧情声音不要直接调用 `AudioSource.Play`。
- 场景切换前显式停止不再需要的循环 SFX；需要延续的 BGM 可以保留。
- `GameAudioCatalog.asset` 是生成资产。文件改名或替换后应重建，而不是
  手工修改 YAML。
