# KeyClassify · 钥匙按地图分类

把**钥匙**和**钥匙卡**按所属地图归类：藏身处/跳蚤市场（手册）里的 `钥匙` 下面会多出 12 个地图子分类，
钥匙名字前面自动加上 `【地图名】`，描述顶部加一行地图标识，跳蚤市场的分类树也会同步跟着变。

参考 MGmod（麻瓜编辑器）的钥匙分类思路重写，独立服务端 mod，SPT 4.1.x（net10.0）。

> 下载：[GitHub Releases](https://github.com/wangbofan/SPT-KeyClassify/releases) · 源码：[wangbofan/SPT-KeyClassify](https://github.com/wangbofan/SPT-KeyClassify)

---

## 效果

```
手册 / 跳蚤市场
└─ 钥匙
   ├─ 海关        (32 把)
   ├─ 工厂        (2 把)
   ├─ 森林        (5 把)
   ├─ 立交桥      (15 把)
   ├─ 海岸线      (48 把)
   ├─ 储备站      (31 把)
   ├─ 实验室      (12 把)
   ├─ 灯塔        (17 把)
   ├─ 塔科夫街区  (39 把)
   ├─ 中心区      (5 把)
   ├─ 迷宫        (10 把)
   └─ 未知        (12 把，即不绑定具体地图的钥匙)
```

物品名变成 `【海关】公司主管办公室钥匙`，描述第一行是 `海关`（带颜色）。
语言跟随客户端：中文客户端显示中文，俄语客户端显示俄语……共支持游戏自带的 17 种语言，
地图名直接取游戏自己的地图名翻译，不需要额外维护词条。

原来的 `机械钥匙` / `电子钥匙` 两个分类在搬空后会自动删除（可关闭）。

## 分类数据怎么来的、核对过没有

`keymaps.json` 里的地图归属不是猜的，来源和核对过程如下：

- 底表取 **MGmod 自己的 `MapKey.json`**（12 张图共 241 条，含 1 条重复），只保留当前版本
  手册里真实存在、并且本来就在 `机械钥匙`/`电子钥匙` 下的 **228 条**——这正好是游戏"钥匙"分类的全部
  214 + 14 把，一把不多一把不少。
- MG 把它标记成"未知"的钥匙里，有 5 把按证据挪进了具体地图：`机械钥匙`（海关，任务/跳蚤标记）、
  `RB-PP钥匙`（储备站，描述写明"联邦储备基地防空地堡"）、`Horse饭店厕所钥匙`（中心区，描述写明
  "市中心 Horse 饭店"）、`迷宫钥匙` 与 `行刑室钥匙`（迷宫，描述写明迷宫/行刑室）。
- 剩下 **12 把留在"未知"**：折叠车钥匙、VAZ 车钥匙、占位的"钥匙 2/3/5"、Black Keycard、
  泵站前后门、武器保险柜、手枪收纳箱、Missam 叉车钥匙——描述里本身就没有地图信息，MG 也归在未知。
- 拿 **tarkov.help 的地图标记**（每把钥匙"在哪个图开什么"）做独立交叉核对：228 把 tpl 完全一一对应；
  有地图标记的 189 把里 **188 把一致**。唯一不一致的是 `Labrys 访问钥匙卡`：描述说它开"Knossos 地下
  设施"（迷宫那套米诺陶主题），MG 也放在迷宫，但 tarkov.help 把它标在海岸线的电子门上；本 mod 按
  MG/描述放在 `迷宫`，想改的话在 keymaps.json 里把它从 `labyrinth` 挪到 `shoreline` 即可。
- 其余 39 把 tarkov.help 没有地图标记，逐条按名字/描述核对过（疗养院各房间、RB-*、Concordia 公寓、
  中心区 Horse/Unity Credit、迷宫各房间等），没有发现明显错放。
- MG 的 `MapKey.json` 里还有 13 条本 mod 不处理，原因分别是：7 条不在当前手册里（已删除的旧物）、
  4 条在"任务物品"分类（`备用藏身处钥匙`/`任务测试钥匙`/`疗养院封闭房间的钥匙`/`塔科夫纪念品钥匙`）、
  2 条是"特殊装备"里的 `BBQ-S43 喷枪` 和 `阀门手轮`（名字像钥匙，其实是工具）。

## 安装

1. 把 `KeyClassify` 整个文件夹放进 `<SPT目录>/user/mods/`
2. 启动服务端，日志里出现这行就说明生效了：

```
[KeyClassify] Classified 228 key(s) into 12 map categories (12 created, 0 refreshed, 228 re-parented, 2 emptied category/categories removed).
```

> 只改服务端数据，不需要装客户端插件；卸载的话删掉文件夹、重启服务端即可。
> 已经装过的旧存档不受影响，键位、跳蚤价格、任务条件都不变，变的只是"手册里挂在哪个分类下"和显示名。

## 配置

### config/config.json

| 字段 | 默认值 | 说明 |
| --- | --- | --- |
| `enabled` | `true` | 总开关，关掉就完全不动手册 |
| `nameFormat` | `【{map}】{name}` | 物品名格式，`{map}` 本地化地图名，`{name}` 原名（必须有） |
| `addDescriptionHeader` | `true` | 是否在描述顶部插入地图标识 |
| `descriptionHeader` | `<color=#00cccc><b>{map}</b></color>` | 描述顶部那行的格式 |
| `includeKeycards` | `true` | `false` 时钥匙卡保持原样，只有普通钥匙按地图分类 |
| `createUnknownCategory` | `true` | `false` 时不生成"未知"分类，这批钥匙留在原分类 |
| `removeEmptiedOriginalCategories` | `true` | 搬空后是否删掉原来的"机械钥匙 / 电子钥匙" |
| `parentCategoryId` | `5b47574386f77428ca22b342` | 地图分类挂在哪里（默认是"钥匙"） |
| `mechanicalCategoryId` / `electronicCategoryId` | `5c518ec9…` / `5c518ed5…` | 原分类 id，用来判断钥匙卡、以及判断该不该删除 |
| `categoryColor` | `""` | 分类颜色，留空表示不上色 |

想让名字更朴素，把 `nameFormat` 改成 `[{map}] {name}` 之类的即可；改完重启服务端。

### config/keymaps.json

真正决定"哪把钥匙属于哪张地图"的地方。一个地图一个对象：

```json
{
  "id": "customs",
  "categoryId": "6f646d000000000000000001",
  "order": "101",
  "icon": "/files/handbook/icon_keys_mechanic.png",
  "localeKeys": ["Customs", "bigmap"],
  "names": { "ch": "海关" },
  "keys": ["6761a6f90575f25e020816a4", "..."]
}
```

- `id`：内部标识，只出现在日志里
- `categoryId`：手册分类 id，**必须是 24 位十六进制**，一旦有人玩过存档就别再改（它就是这个分类的身份证）
- `order`：排序，数字字符串；游戏里顶级分类用 `"0".."13"`、子分类用 `"100"`，所以 `101` 起排在默认项后面
- `icon`：分类图标，留空用 `/files/handbook/icon_keys_mechanic.png`
- `localeKeys`：去游戏语言表里查地图名的键，按顺序取第一个命中的
- `names`：按语言硬覆盖地图名（优先级最高），不写也行
- `keys`：属于这张地图的物品 tpl 列表

**加新钥匙 / 改归属**：把 tpl 从一张地图的 `keys` 数组挪到另一张即可；游戏更新加了新钥匙，补进对应数组就行，不用改代码。
**加新地图**（新 DLC 图等）：复制一个对象，换 `id`、新的 `categoryId`（自己编 24 位十六进制，别重复）、`localeKeys` 写新图在语言表里的名字键。

## 原理

- SPT 的手册数据是 `TemplateTable.Handbook`（`Categories` + `Items`），本 mod 在 `OnLoadOrder.PostLoad + 50`
  执行，也就是**最后**改手册，读完配置后：建地图分类 → 把物品的 `ParentId` 指到地图分类 → 删掉搬空的原分类。
- 跳蚤市场不需要另外处理：客户端跳蚤分类树就是手册树；服务端 `RagfairHelper.GetCategoryList` 也是按
  手册分类 id 找物品（`TemplatesWithParent` / `ChildrenCategories`），所以手册里分了组，跳蚤里自然就分开了。
- 名字和描述走语言表：给每种语言的 `LazyLoad<GlobalLocaleDictionary>` 挂一个 transformer，
  在该语言第一次被请求时改写 `"<tpl> Name"` / `"<tpl> Description"`，并写入分类名 `"<categoryId>"`。
  这样做的好处是：地图名用的是"该语言自己的"翻译，中文客户端不会看到英文图名。
- `PostLoad + 50` 排在 ItemInfo 之类会改名字的 mod 之后，因此它们加的稀有度染色、`★` 之类会留在内层，
  我们的 `【地图】` 前缀在最外层。
- 还有一步容易被忽略：SPT 在第一次有人查手册价格/分类时，会把手册复制成一份内部索引
  （`HandbookHelper` 里的 `_handbookPriceCache`）并一直用到重启。启动时别的 mod 往往已经先读过手册价格，
  这份索引里就还是"机械钥匙/电子钥匙"。所以本 mod 改完手册后会把这个缓存清空
  （`HandbookCacheRefresher`，反射置空），下次访问自动按新手册重建——否则跳蚤里点新分类会是空的。
  日志里出现 `Dropped SPT's cached handbook lookup ...` 就是在做这件事。

## 从源码构建

需要 .NET 10 SDK，并且指到你的 SPT 安装目录（默认 `E:\GAME\SPT_Runtime`）：

```
dotnet build -c Debug
dotnet build -c Debug -p:SptServerDir="D:\SPT"        # 指定别的安装位置
dotnet build -c Debug -p:SkipModCopy=true             # 只编译，不往游戏目录拷
dotnet build -c Release                               # 产物在 artifacts/ 下，可直接打包
```

Debug 构建会自动把 dll 和 config 拷到 `<SptServerDir>/user/mods/KeyClassify`。

## 常见问题

- **名字/分类没变？** 确认服务端日志有 `[KeyClassify]` 那行；没有就是 mod 没加载（看 `user/logs/`）。
  客户端如果开着，重启客户端重新拉手册。
- **和别的 mod 冲突？** 本 mod 只改手册的 `ParentId`、语言表的名字/描述；如果另一个 mod 也搬钥匙，
  后加载的赢。把本 mod 的 `PostLoad + 50` 调大/调小（`Plugin.cs`）可以改变先后。
- **想恢复原样？** 把 `enabled` 设成 `false`（或删掉 mod），重启服务端。手册是每次启动从
  `SPT_Data/database/templates/handbook.json` 重新读的，不会留下永久修改。
- **跳蚤里地图分类是空的？** 说明手册索引没被刷新（旧版本的问题，本版本已修）。看日志有没有
  `Dropped SPT's cached handbook lookup`；如果提示 `SPT changed how it caches the handbook`，
  那是 SPT 改了内部字段名，`HandbookCacheRefresher.cs` 里的 `_handbookPriceCache` 需要跟着改。
- **钥匙卡想单独放？** `includeKeycards: false`，钥匙卡会留在"电子钥匙"分类里。
