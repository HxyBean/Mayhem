# Mayhem — Tài liệu tổng hợp dự án

> Tài liệu này ghi lại TOÀN BỘ kiến trúc, tính năng và các bài học (bug đã sửa, gotcha của Unity) được xây dựng
> qua nhiều lượt làm việc. **Đọc file này trước khi sửa/thêm tính năng mới** để hiểu đúng convention và tránh lặp lại
> các lỗi đã từng gặp. Code comment trong project viết bằng tiếng Việt — tài liệu này cũng vậy để nhất quán.

Repo: `HxyBean/Mayhem` — Unity 2D top-down shooter/roguelite, nhân vật chọn từ Character Select, 3 Level (Scene riêng biệt), augment nâng cấp mỗi lần lên cấp.

---

## 1. Kiến trúc tổng thể (nguyên tắc cốt lõi)

**KHÔNG subclass Player/Gun theo từng nhân vật, KHÔNG duplicate scene theo từng nhân vật.** Toàn bộ khác biệt giữa
Gunner / Mage / Knight là **data-driven** qua ScriptableObject `CharacterData`, áp dụng vào các script DÙNG CHUNG
(`Player.cs`, `Gun.cs`, `KnightCombat.cs`) đã đặt sẵn 1 lần trong mỗi Scene Level. Tương tự, khác biệt độ khó giữa
các Level là qua `StageData`. Đây là convention xuyên suốt — khi thêm nhân vật/stage thứ N, đi theo đúng pattern
này, không tạo prefab/scene riêng.

Lý do chọn cách này: instantiate 1 script dùng chung + data khác nhau tránh phải nối lại tham chiếu UI/Cinemachine/
Joystick mỗi khi thêm nhân vật, và mọi Scene Level chỉ cần đặt sẵn 1 GameObject Player + 1 GameManager.

---

## 2. Luồng chơi & cấu trúc Scene

```
MainMenu (Scene)
 ├─ Main Menu Panel        (Play, How To Play, New Game, Quit)
 ├─ Stage Select Panel     (StageButton × N, khóa/mở theo GameProgress.UnlockedStageCount)
 ├─ Character Select Panel (CharacterButton × N — Gunner/Mage/Knight; có nút mở Shop + modal mua nhân vật)
 ├─ Shop Panel             (modal mở đè từ Character Select — mua chỉ số nội tại, đổi Coin→Kim cương)
 ├─ How To Play Panel      (text hướng dẫn tiếng Anh)
 └─ New Game Confirm Panel (modal Yes/No)
        │
        │ StageButton.OnClick() → GameProgress.SelectedStage = ... → ShowCharacterSelect()
        │ CharacterButton.OnClick() → GameProgress.SelectedCharacter = ... → SceneManager.LoadScene(stage.sceneName)
        ▼
Level1 / Level2 / Level3 (Scene riêng biệt — map khác nhau giữa các level)
 └─ GameManager.Start() đọc GameProgress.SelectedStage/SelectedCharacter, áp vào Player/Gun/KnightCombat/AugmentManager
```

**Quan trọng**: Nhân vật được **chọn lại mỗi lần vào 1 Level** (không phải chọn 1 lần cho cả run). Level chỉ mở khóa
tuần tự — không vào được Level N+1 nếu chưa thắng Level N ([GameProgress.cs](Assets/Scripts/Managers/GameProgress.cs)).

- `GameProgress` (static class, không phải MonoBehaviour) giữ `SelectedStage`/`SelectedCharacter` (transient, chỉ
  sống trong phiên hiện tại) và `UnlockedStageCount` (PlayerPrefs, key `Mayhem_UnlockedStageCount`, persist qua các
  lần mở game).
- Test trực tiếp 1 Scene Level trong Editor (không qua MainMenu): `GameManager` có field `debugStage`/`debugCharacter`
  dùng làm fallback khi `GameProgress.SelectedStage/SelectedCharacter == null`.
- Menu Editor tiện ích: `Mayhem/Debug/Unlock All Stages`, `Mayhem/Debug/Reset Stage Progress`.

---

## 3. Hệ thống nhân vật (CharacterData)

[`CharacterData.cs`](Assets/Scripts/Managers/CharacterData.cs) — ScriptableObject (`Create → Mayhem → Character Data`).
Asset hiện có: `GunnerData.asset`, `MageData.asset`, `KnightData.asset` (`Assets/CharacterData/`).

| Field | Ý nghĩa |
|---|---|
| `baseBulletDamage` | Sát thương gốc (đạn với Gunner/Mage, đòn chém với Knight) |
| `baseMaxHP` / `baseMoveSpeed` | Override máu/tốc độ riêng nhân vật này — **0 = giữ nguyên giá trị mặc định trên Player trong Scene** |
| `combatType` | `Ranged` (dùng `Gun.cs`) hoặc `Melee` (dùng `KnightCombat.cs`) |
| `bulletPrefab` | Prefab đạn riêng (chỉ Ranged) |
| `usesAmmo` | Bỏ tick (Robot) = bắn không tốn đạn, ẩn nút Nạp đạn, ô text đạn thành bộ đếm charge Laser |
| `shotDelay` | Nhịp bắn riêng nhân vật — **0 = giữ nguyên giá trị mặc định trên Gun trong Scene** |
| `abilityType` | `Dash` (Gunner) / `Blink` (Mage) / `None` (Knight — không có, vì "quá trâu không cần né") |
| `animatorController`, `idleSprite` | Tạo hình riêng nhân vật |
| `weaponSprite`, `shootSound`, `reloadSound`, `shootButtonIcon`, `reloadButtonIcon` | Vũ khí/âm thanh/UI riêng (chỉ Ranged) |
| `weaponOffset` | **Độ lệch** vị trí vũ khí so với chỗ đặt sẵn trong Scene (không phải toạ độ tuyệt đối) — dùng khi nhân vật cao/thấp khác nhau làm súng bị lệch. `(0,0)` = giữ nguyên |
| `exclusiveAugments` | List augment CHỈ xuất hiện khi chơi nhân vật này |
| `unlockedByDefault` | Nhân vật khởi đầu (Gunner) — luôn mở sẵn, không cần mua |
| `coinPrice` / `diamondPrice` | Giá mở khóa. **<= 0 = KHÔNG cho mua bằng loại tiền đó** (nút thanh toán tương ứng tự ẩn) |

> `characterName` còn được dùng làm **khóa lưu trạng thái đã mua** trong PlayerPrefs — đổi tên nhân vật sau khi
> người chơi đã mua sẽ làm mất trạng thái mở khóa của họ. Mỗi nhân vật phải có `characterName` khác nhau.

`GameManager.Start()` đọc `GameProgress.SelectedCharacter`, gọi `Player.ApplyCharacterData()`, bật đúng 1 trong 2
`Gun.SetActive()`/`KnightCombat.SetActive()` theo `combatType`, và `AugmentManager.ApplyCharacterData()`.

### 3.1 Gunner (Ranged, mặc định)
- Bắn/nạp đạn cơ chế chuẩn ([Gun.cs](Assets/Scripts/Weapons/Gun.cs)): auto-aim tự xoay khóa mục tiêu gần nhất
  (mobile) hoặc theo chuột (PC test), `maxAmmo`/`currentAmmo`, nạp đạn theo thời gian (`ExecuteReload`).
- Khả năng đặc biệt: **Dash** (lướt nhanh theo hướng joystick/WASD, `dashTime`, `dashCooldown`).
- Lõi riêng: Bomb (ném AOE nổ 1 lần), BurstShot (3 viên song song, giảm 20% dmg/viên, bay tối đa 5f), SplitShot
  (3 viên tỏa 15°, giảm 20% dmg/viên).

### 3.2 Mage — Pháp sư (Ranged)
- Cùng cơ chế bắn/nạp như Gunner (dùng chung `Gun.cs`), chỉ đổi **prefab đạn** — đạn Mage có `splashRadius > 0`
  trên [PlayerBullet.cs](Assets/Scripts/Weapons/PlayerBullet.cs), nổ lan gây `dmg * splashDamagePercent` (mặc định
  50%) cho các Enemy khác quanh mục tiêu trúng trực tiếp (loại trừ chính mục tiêu đó).
- Tài nguyên đạn gọi là "**mana**" — thực chất vẫn là field `currentAmmo`/`maxAmmo` của `Gun.cs`, chỉ khác tên hiển
  thị UI + có thêm cơ chế **hồi tự động theo thời gian** (`Gun.StartManaRegen()`, không có ở Gunner vốn chỉ nạp
  bằng nút).
- Khả năng đặc biệt: **Blink** — dịch chuyển tức thời (không có telegraph/độ trễ), kéo-thả để chọn điểm đến, **giới
  hạn cứng bán kính 5f** từ vị trí nhân vật (`Player.maxBlinkRange`), cooldown dài hơn Dash. Tự động không bao giờ
  đưa Player vào vật cản (`FindSafeBlinkPosition` lùi dần về phía gốc nếu điểm đích đè lên `blinkObstacleMask`).
  Có hiệu ứng animation riêng lúc biến mất/xuất hiện (`blinkStartEffectPrefab`/`blinkEndEffectPrefab`).
- Lõi riêng: SplashDamage (+10% dmg lan/lần), ManaRegen (+0.25 mana/s/lần, trần 2/s), BlinkCooldown (nhân
  `blinkCooldown *= 0.9` mỗi lần — **lưu ý: cách tính này là NHÂN DỒN (multiplicative), không phải trừ cố định**),
  Potion (mở khóa lõi ném bình thuốc — xem mục 5).

### 3.3 Robot (Ranged, không dùng đạn)
Vẫn dùng chung `Gun.cs` để bắn/auto-aim, nhưng `CharacterData.usesAmmo = false` thay toàn bộ lớp tài nguyên:
- **Không nạp đạn**: bắn không tốn gì, chỉ bị giới hạn bởi `shotDelay` (đặt 0.25–0.5s cho Robot). Mọi chỗ trừ/kiểm
  tra đạn trong `Gun.cs` đi qua `ConsumeAmmo()`/`HasAmmo()` nên chỉ cần 1 cờ là tắt được cả cơ chế. Nút Nạp đạn tự
  ẩn, thay bằng nút Laser (`ApplyCharacterData` xử lý — chạy SAU `SetActive()` nên ghi đè được).
- **Ô text đạn thành bộ đếm charge**: `UpdateAmmoText()` hiện `7/10` thay vì số đạn.
- **Laser** (nội tại, không cần lõi): đủ `laserChargeRequired` (10) đòn bắn thường thì bắn được 1 phát, reset về 0.
  **Kéo thả chọn HƯỚNG** qua [LaserButton.cs](Assets/Scripts/UI/LaserButton.cs) (xem mục 7), thả tay là bắn.
  Là chiêu **TỨC THÌ, không phải viên đạn bay** — `Physics2D.OverlapBoxAll` quét 1 hình chữ nhật
  `laserRange × laserWidth` theo hướng đã chọn rồi gây `bulletDamage × 1.5` cho MỌI Enemy trong đó (xuyên thấu,
  không bị chặn ở con đầu tiên), sau đó spawn `laserEffectPrefab` xoay đúng hướng. Có `OnDrawGizmosSelected` vẽ
  vùng trúng đòn để căn cho khớp sprite.
  > Tia bắn từ **tâm nhân vật**, KHÔNG phải từ `firePos`: súng vẫn auto-aim vào con gần nhất nên có thể đang chĩa
  > hẳn hướng khác với hướng vừa kéo, lấy nòng súng làm gốc sẽ thấy tia mọc ra từ sau lưng.
  > Sprite tia laser phải để **pivot = Left** (mép trái), nếu để giữa thì nửa tia đâm ngược ra sau nhân vật.
  > Hiệu ứng tia được **gắn làm con của Player** (`SetParent` + `localPosition = 0`) để bám theo nhân vật lúc chạy,
  > giống hiệu ứng chém của Knight. Gắn vào PLAYER chứ không phải Gun — Gun tự xoay auto-aim liên tục, gắn vào đó
  > thì tia sẽ quay theo nòng. Sát thương vẫn tính 1 lần tại thời điểm bắn (chỉ phần hình ảnh đi theo).
- **Lõi riêng — Mini Robot**: thả 3 con [MiniRobot.cs](Assets/Scripts/Weapons/MiniRobot.cs) chạy theo hướng nòng
  súng (xếp lệch nhau theo trục vuông góc như `ShootBurst`), chạm Enemy thì nổ gây `bulletDamage × 2` (có
  `explosionRadius` lan sang xung quanh, để 0 = chỉ trúng con chạm vào). Cooldown 20s, mở khóa qua augment type
  `"MiniRobot"`, theo đúng pattern `EnableBomb()`/`EnablePotion()`.

> `Start()` của Gun cố tình **KHÔNG** ẩn `laserButtonObj` (chỉ ẩn `miniRobotButtonObj`): `ApplyCharacterData()`
> mới là nơi quyết định nút Laser hiện/ẩn, mà `Start()` có thể chạy SAU hàm đó → ẩn ở `Start()` sẽ ẩn nhầm nút
> của Robot. Nút Mini Robot thì an toàn vì chỉ được bật lúc chọn lõi, rất lâu sau `Start()`.

### 3.4 Knight (Melee)
Toàn bộ cơ chế nằm ở [KnightCombat.cs](Assets/Scripts/Weapons/KnightCombat.cs) (đặt cùng GameObject Player, được
`GameManager` bật/tắt qua `SetActive()` giống `Gun.cs`). Khi chọn Knight, `Gun.SetActive(false)` sẽ tự ẩn hẳn
sprite súng + nút Bắn/Nạp đạn/Bomb/Potion (xem mục 8.2 — bug đã sửa).
- **Không có Dash/Blink** — theo yêu cầu người dùng: nhân vật "trâu" không cần skill né.
- **Chém tự động**: cứ mỗi `attackInterval` giây (mặc định 1f) tự động chém quanh bán kính `attackRadius`, gây
  `Player.bulletDamage` cho mọi Enemy trong tầm + spawn hiệu ứng máu (`bloodPrefab`) tại từng Enemy trúng đòn.
  Hiệu ứng chém (`attackEffectPrefab`) **được gắn làm con của Player** (`attachToPlayer = true`) nên luôn bám theo
  nhân vật di chuyển — KHÔNG đứng yên tại vị trí chém (bug đã sửa, xem mục 8.4).
- **Stamina**: `maxStamina = 50`, tự hồi `staminaRegenPerSecond` (augmentable), thay thế hoàn toàn cơ chế "ammo".
- **Khiên (Shield)**: giữ nút để kích hoạt — tăng `damageResistance` (`shieldResistance`, tối đa 50%, áp dụng
  chung qua `Player.SetDamageResistance()`), **đồng thời +30% tốc độ di chuyển** (`shieldSpeedMultiplier`, qua
  `Player.SetSpeedBoostMultiplier()` — multiplier TẠM THỜI, khác với `moveSpeed` vĩnh viễn của augment Speed nên
  không xung đột), tốn `shieldStaminaCostPerSecond` (mặc định 10/s, augmentable) mỗi giây giữ; hết Stamina thì tự
  động hạ khiên (`OnShieldReleased`). Nút Khiên là [ShieldButton.cs](Assets/Scripts/UI/ShieldButton.cs)
  (`IPointerDownHandler`/`IPointerUpHandler`, không phải drag-aim).
- **Xoay Kiếm (Sword Spin)** — lõi đặc biệt, phải unlock qua augment "SwordSpin" trước mới hiện nút
  (`swordSpinButtonObj` ẩn mặc định trong `Start()`): bất tử tạm thời (`Player.SetInvulnerable(true)`) + gây damage
  theo tick (`swordSpinTickInterval = 0.15s`) quanh bán kính `swordSpinRadius` (2.5f), kéo dài `swordSpinDuration`
  (5s), cooldown `swordSpinCooldown` (15s), tốn `swordSpinStaminaCost` (30). Có sub-augment
  "SwordSpinCooldown" (-2s/lần) **CHỈ xuất hiện trong pool SAU KHI đã chọn augment "SwordSpin"** — xem cơ chế
  `pendingSwordSpinCooldownAugment` ở mục 5.
- Lõi riêng: AttackSpeed (-0.1s/đòn, **trần dừng xuất hiện khi đạt 0.5s/đòn** — kiểm tra qua
  `KnightCombat.GetAttackInterval()`), StaminaRegen (+1/s/lần), ShieldCost (-1 stamina/s tốn/lần), SwordSpin (mở
  khóa), SwordSpinCooldown (sub-augment, -2s/lần).
- **Animation**: Knight chỉ có animation cho ĐÚNG 3 hành động — chém tự động, giữ khiên, xoay kiếm (qua
  `Player.PlayAnimTrigger("Attack")` và `Player.SetAnimBool("Shielding"/"SwordSpin", ...)`). **Người dùng tự làm
  hiệu ứng animation riêng bằng cách spawn prefab sprite/animation riêng (giống Explosion) — KHÔNG bake animation
  vào Animator Controller của nhân vật cho các hành động khác** (đã cố tình xóa hết trigger Dash/Shoot/Reload/
  ThrowBomb/ThrowPotion theo yêu cầu, chỉ giữ lại `Blink` + 3 hành động Knight).

---

## 4. Hệ thống Stage (StageData) & độ khó theo Level

[`StageData.cs`](Assets/Scripts/Managers/StageData.cs) — ScriptableObject (`Create → Mayhem → Stage Data`).
`stageIndex` (bắt đầu từ 1, Stage 1 luôn mở sẵn), `sceneName` (phải khớp chính xác tên trong Build Settings),
`enemyHpMultiplier`/`enemyDamageMultiplier`/`enemySpeedMultiplier`, `extraAugments`.

**Vấn đề đã giải quyết**: chỉnh thông số Inspector trên Enemy prefab sẽ ảnh hưởng TẤT CẢ Scene dùng chung prefab
đó → không thể set cứng độ khó khác nhau theo Level. Giải pháp: [Enemy.cs](Assets/Scripts/Enemies/Enemy.cs) lưu
`baseMoveSpeed`/`baseMaxHP`/`baseEnterDmg`/`baseStayDmg` (giá trị GỐC trên Inspector, capture 1 lần trong `Awake()`
qua flag `statsCaptured` để không bị nhân dồn khi Enemy tái sử dụng từ Pool), rồi mỗi lần `OnEnable()` gọi
`ApplyStageDifficulty()` nhân lại theo hệ số của `GameManager.CurrentStage` (ưu tiên) hoặc `GameProgress.SelectedStage`
(fallback khi không qua GameManager).

`GameProgress.CompleteStage(stageIndex)` được gọi trong `GameManager.WinGame()` — mở khóa Stage kế tiếp.

---

## 5. Hệ thống Augment (nâng cấp)

Toàn bộ logic nằm trong [AugmentManager.cs](Assets/Scripts/Managers/AugmentManager.cs). Class `Augment` (đơn giản,
serializable): `name`, `description`, `type` (string dùng làm "khóa" switch trong `ApplyEffect`/`CheckAndRemoveAugment`),
`requiredLevel`.

**3 nguồn augment gộp vào 1 `augmentPool` chung** lúc bắt đầu ván chơi:
1. `InitializePool()` — augment DÙNG CHUNG mọi nhân vật: Damage, Speed, Health, LifeSteal (từ cấp 5), Exp, Magnet
   (từ cấp 4).
2. `ApplyStageData(stage)` — cộng `stage.extraAugments` (lõi riêng Stage, VD "PercentDamage" hiện đang gán ở
   Stage 2-3).
3. `ApplyCharacterData(character)` — cộng `character.exclusiveAugments` (lõi riêng nhân vật). **Ngoại lệ**: augment
   type `"SwordSpinCooldown"` KHÔNG được cộng thẳng vào pool — giữ tạm ở `pendingSwordSpinCooldownAugment`, chỉ
   thêm vào pool thật khi `CheckAndRemoveAugment("SwordSpin")` chạy (tức đã chọn augment cha "SwordSpin"). Đây là
   **pattern "sub-augment"** dùng cho bất kỳ augment con nào chỉ nên xuất hiện sau khi đã mở khóa 1 augment cha.

### 5.1 Chọn augment khi lên cấp — `SetupOptions()`
- **Forced tier**: cấp 8 ép buộc chỉ hiện augment Bomb; cấp 10 ép buộc hiện BurstShot + SplitShot. **Luôn kiểm
  tra augment đó có THỰC SỰ tồn tại trong `augmentPool` không trước khi ép buộc** (`augmentPool.Find(a => a.type
  == "Bomb")`) — nếu nhân vật hiện tại không có augment đó trong `exclusiveAugments` (VD Mage không có Bomb), rơi
  về nhánh random bình thường. Thiếu bước này sẽ ra màn hình augment TRỐNG cho nhân vật không có lõi đó.
- Các cấp khác: random 3 augment không trùng nhau từ `validPool` (lọc theo `pLevel >= aug.requiredLevel`, loại
  trừ Bomb/BurstShot/SplitShot khỏi random để dành riêng cho forced tier).

### 5.2 Chọn augment — `SelectAugment(index)` → `ApplyEffect(type)` rồi `CheckAndRemoveAugment(type)`
- `ApplyEffect`: switch theo `type`, gọi đúng method trên `Player`/`Gun`/`KnightCombat`/`GameManager`/`GameUI`.
  **Mỗi `case` PHẢI có `break;`** (trừ case cuối cùng của switch — C# không bắt buộc nhưng nên thêm cho an toàn/
  nhất quán khi có case mới thêm sau này). Đây là nguồn lỗi hay gặp nhất khi tự thêm augment mới bằng tay.
- `CheckAndRemoveAugment`: xóa augment khỏi pool khi đạt TRẦN hoặc đã chọn 1 lần (loại "chỉ chọn 1 lần"). Các trần
  hiện có: LifeSteal 30%, Speed +100% (so mốc gốc nhân vật, qua `Player.GetSpeedBonusPercent()`), Magnet bán kính 4,
  ManaRegen 2/s, AttackSpeed 0.5s/đòn (Knight). Bomb/Potion/SwordSpin/BurstShot+SplitShot: xóa NGAY sau khi chọn
  (chỉ chọn được 1 lần, không có "cấp độ").

### 5.3 Danh sách augment hiện có (type → hiệu ứng)

**Chung (mọi nhân vật):**
| type | Hiệu ứng | Trần |
|---|---|---|
| `Damage` | Player.IncreaseDamage(+2.5 flat) | không |
| `Speed` | Player.ApplyMoveSpeedBoost(×1.1) | +100% so gốc |
| `Health` | Player.ApplyMaxHPBoost(+20) | không |
| `LifeSteal` | Player.AddLifeSteal(+5%) | 30% |
| `Exp` | GameManager.AddExpBoost(+20%) | không |
| `Magnet` | Player.IncreaseMagnetRadius(+1.5) — từ cấp 4 | 4f |

**Riêng Gunner:** `Bullet` (Gun.AddAmmo+10), `Regen` (Player.StartHealthRegen 2hp/s), `Reload` (Gun.ReduceReloadTime
-0.5s, tối đa 3 lần), `Bomb` (mở khóa, cấp 8), `BurstShot`/`SplitShot` (cấp 10, chọn 1 trong 2).

**Riêng Mage:** `SplashDamage` (Player.IncreaseSplashDamagePercent +10%), `ManaRegen` (Gun.StartManaRegen +0.25/s,
trần 2/s), `BlinkCooldown` (Player.ReduceBlinkCooldown ×0.9), `Potion` (Gun.EnablePotion, mở khóa lõi Potion).

**Riêng Knight:** `AttackSpeed` (-0.1s/đòn, trần 0.5s/đòn), `StaminaRegen` (+1/s), `ShieldCost` (-1 stamina/s tốn),
`SwordSpin` (mở khóa), `SwordSpinCooldown` (sub-augment, -2s, chỉ hiện sau khi có SwordSpin).

**Riêng Robot:** `MiniRobot` (Gun.EnableMiniRobot, mở khóa, chọn 1 lần). Robot KHÔNG dùng được `Bullet`/`Reload`
(không có đạn) lẫn `ManaRegen` — đừng đưa các augment đó vào `exclusiveAugments` của nó.

**Riêng Stage (VD Stage 2-3):** `PercentDamage` — Player.IncreaseDamagePercent(×1.2, tức +20% NHÂN DỒN mỗi lần
chọn — khác `Damage` là cộng flat).

> Số liệu chính xác từng augment (description hiển thị, requiredLevel) nằm trong `InitializePool()` +
> `exclusiveAugments`/`extraAugments` đã gán sẵn trên từng `.asset` (`GunnerData`/`MageData`/`KnightData`/
> `StageXData`) trong Editor — đọc trực tiếp asset hoặc code khi cần số chính xác thay vì tin tài liệu này 100%.

---

## 6. Hệ thống Enemy

[`Enemy.cs`](Assets/Scripts/Enemies/Enemy.cs) là abstract base class — TẤT CẢ enemy dùng chung logic va chạm,
chỉ override khi cần khác biệt (pattern giảm lặp code):

```
BasicEnemy    — dùng nguyên hành vi mặc định
MiniEnemy     — override DropItems (spawn ít hơn)
EnergyEnemy   — override DropItems (rơi Energy thay vì EXP)
HealEnemy     — override DropItems
ExplosionEnemy— override OnPlayerStay() = Die() ngay (nổ+chết khi chạm, KHÔNG gây stayDmg liên tục);
                override DropItems + Die() (spawn hiệu ứng nổ trước khi trả Pool)
BossEnemy     — override OnEnable/Update/DropItems/Die, thêm skill ngẫu nhiên + Teleport có telegraph
```

- `OnTriggerEnter2D`/`OnTriggerStay2D` (không override được, seal ở base) dispatch sang `OnPlayerEnter()`/
  `OnPlayerStay()` (virtual, override ở subclass nếu cần hành vi khác — đây chính là chỗ dedupe code từng lặp lại
  ở 5 subclass).
- `TakeDmg()`/`Die()` có guard `isDead` — bắt buộc để tránh chết nhiều lần trong 1 frame khi nhiều viên đạn trúng
  cùng lúc (bug cũ: Boss chết bị rớt NHIỀU USB/EXP do `Die()` chạy lại nhiều lần).
- `DropItems()`: mặc định rơi 1-3 viên `xpObject`. Boss: luôn có `usbPrefabs` + đúng 1 `xpObject` (EXP boss).

### 6.1 Boss — cơ chế hồi sinh & skill
`GameManager.AddEnergy()` gọi `CallBoss()` khi đủ `energyThreshold` → `boss.SetActive(true)`. Boss chết
(`Die()`) chỉ `SetActive(false)` (không destroy) và nhân `baseMaxHP *= 1.5f` (máu tăng dần mỗi lần hồi sinh — vì
`ApplyStageDifficulty()` tính lại `maxHP = baseMaxHP * hpMultiplier` mỗi `OnEnable()`).

**Tiến trình phá đảo tính theo SỐ PHASE BOSS ĐÃ HẠ, không phải theo vật phẩm USB nhặt được.** `BossEnemy.Die()`
gọi `GameManager.OnBossDefeated()` → `currentUSB++` + `UpdateUsbBar()` → đủ `usbThreshold` thì `WinGame()`, chưa
đủ thì `SpawnBossWarningThenCallBoss()` (hiệu ứng cảnh báo `bossRevive`, 2s sau Boss xuất hiện lại).

> Cách cũ (cộng tiến trình lúc NHẶT USB) tạo lỗ hổng: cứ bỏ viên USB nằm dưới đất là kẹt phase vĩnh viễn mà vẫn
> farm coin/kinh nghiệm từ quái thường vô hạn. Tính theo lần hạ Boss thì người chơi không còn cần gạt nào để
> trì hoãn, nên lỗ hổng biến mất về mặt cấu trúc — cơ chế hẹn giờ ép Boss respawn từng thêm vào đã được gỡ bỏ.

Vật phẩm **USB giờ chỉ còn là đồ hồi đầy máu** (`PlayerCollision` → `player.RestoreFullHP()`), không còn vai trò
tiến trình. Không muốn Boss rơi USB nữa thì bỏ trống `usbPrefabs` trên prefab Boss, không cần sửa code.

**Tên `currentUSB`/`usbThreshold`/`usbBar` giữ nguyên** dù nay mang nghĩa "phase Boss" — đổi tên field
`[SerializeField]` sẽ làm mất giá trị/tham chiếu đã gán trong Inspector mà Unity không báo lỗi gì.

**Hệ quả — `WinGame()` gom phần thưởng trước khi đóng băng**: thắng xảy ra NGAY lúc hạ Boss phase cuối, tức
`Time.timeScale = 0` ngay khi coin vừa rơi ra từ đám quái cuối cùng. Vì vậy `WinGame()` gọi
`CollectDroppedCurrency()` (quét tag `Coin`/`Diamond` còn trên bản đồ, `Collect()` rồi trả về Pool) **trước**
`CommitRunCurrency()`. Kim cương đã tránh được vấn đề này bằng cách rơi ở phase đầu, nhưng bước gom vẫn cần cho
coin — và là lưới an toàn nếu ai đó set `usbThreshold = 1` (phase đầu cũng chính là phase cuối).

**PHỤ THUỘC THỨ TỰ trong `BossEnemy.Die()`**: `DropItems()` (nơi gọi `ShouldDropDiamond()` → `IsFirstBossKill()`)
phải chạy TRƯỚC `OnBossDefeated()` (nơi tăng `currentUSB`). Nhờ vậy ở phase đầu tiên `currentUSB` vẫn đang là 0.
Đảo thứ tự 2 dòng này là kim cương không bao giờ rơi.

Boss có 5 skill random (`PickRandomSkill`): NormalAtk, CircleAtk (12 viên tỏa tròn), Heal, SpawnMini, Teleport
(có 0.25s telegraph đứng im vận chiêu, khóa vị trí đích NGAY từ đầu vận chiêu để Player có cơ hội né trong lúc
delay, gây damage vùng `teleportLandingRadius` nếu Player chưa kịp né).

### 6.2 Cơ chế damage khi chạm Player — 2 GOTCHA QUAN TRỌNG CỦA UNITY (đã fix, PHẢI hiểu khi sửa code enemy)

1. **`OnTriggerStay2D` chạy theo nhịp vật lý (~50 lần/giây, theo Fixed Timestep), KHÔNG PHẢI 1 lần/giây.** Nếu
   không tự giới hạn, `stayDmg` sẽ bị áp ~50 lần/giây thay vì đúng nghĩa "mỗi giây". Fix: `Enemy.OnPlayerStay()`
   tự cộng dồn `stayDmgTimer += Time.deltaTime` và chỉ trừ máu khi `stayDmgTimer >= stayDmgInterval` (mặc định 1f),
   rồi reset timer về 0. **Đặt throttle NGAY TRONG `OnPlayerStay()` (virtual), KHÔNG đặt trong `OnTriggerStay2D`
   dispatch** — vì `ExplosionEnemy` override `OnPlayerStay()` để phản ứng NGAY LẬP TỨC (nổ+chết), không muốn bị
   throttle theo cùng cơ chế.
2. **Rigidbody2D tự "ngủ" (sleep) sau ~0.5s đứng yên không chuyển động**, lúc đó `OnTriggerStay2D` NGỪNG BẮN dù 2
   collider vẫn đang chạm nhau → bug "chạm Player chỉ trừ máu lúc đầu, đứng yên thì không trừ nữa nữa". Fix: ép
   `rb.sleepMode = RigidbodySleepMode2D.NeverSleep;` trong CẢ `Enemy.Awake()` LẪN `Player.Awake()` (chỉ cần 1 bên
   không ngủ là đủ để Unity vẫn bắn `OnTriggerStay2D`, nhưng set cả 2 bên cho chắc).

Nếu sau này thêm loại va chạm mới (VD 1 hiệu ứng "damage theo thời gian" khác), LUÔN nhớ 2 gotcha này.

---

## 7. Hệ thống vũ khí/kỹ năng ném-nhắm (Bomb / Potion / Blink)

Cùng 1 PATTERN kéo-thả dùng chung, tách base class [`DragAimButton.cs`](Assets/Scripts/UI/DragAimButton.cs)
(abstract, `IPointerDownHandler`/`IDragHandler`/`IPointerUpHandler`):
- Aim reticle LUÔN xuất phát từ **vị trí nhân vật** (không phải vị trí nút bấm), di chuyển theo **độ lệch world
  giữa vị trí ngón tay hiện tại và lúc mới nhấn** (`worldDelta`), KHÔNG bám theo vị trí tuyệt đối ngón tay.
- `minRange`/`maxRange` (0 = không giới hạn). Set `minRange == maxRange` để khóa cứng đúng 1 khoảng cách (Blink =
  5/5).
- Có `cancelZone` (RectTransform) — thả tay trong vùng này thì HỦY, không kích hoạt `OnConfirm()`.
- Subclass chỉ cần override `CanStartDrag()` (điều kiện được phép bắt đầu kéo) và `OnConfirm(Vector3 targetPos)`
  (hành động khi thả tay ngoài vùng hủy): `BombButton` → `Gun.CanThrowBomb()`/`Gun.ThrowBomb()`, `PotionButton` →
  `Gun.CanThrowPotion()`/`Gun.ThrowPotion()`, `BlinkButton` → `Player.CanBlink()`/`Player.Blink()` (với
  `maxRange = 5f`), `LaserButton` → `Gun.CanFireLaser()`/`Gun.FireLaser(direction)`.
- **Chọn ĐIỂM vs chọn HƯỚNG**: Bomb/Potion/Blink nhắm vào 1 toạ độ nên hồng tâm chạy theo ngón tay. Laser chỉ cần
  hướng, nên `LaserButton` override thêm `ApplyAimVisual()` để mũi tên **đứng yên tại nhân vật và chỉ xoay**.
  Vì vậy base class lưu riêng `lastAimTargetPosition` và truyền biến đó vào `OnConfirm()` — KHÔNG đọc
  `aimReticle.position` như trước, vì với Laser thì vị trí GameObject hồng tâm không còn là vị trí đích nữa.

**Bomb** ([Bomb.cs](Assets/Scripts/Weapons/Bomb.cs)) và **Potion** ([Potion.cs](Assets/Scripts/Weapons/Potion.cs))
dùng chung cơ chế bay: `Vector3.MoveTowards` tới đích, chạm đích (khoảng cách < 0.1f) thì kích hoạt — Bomb nổ AOE
1 lần (`Physics2D.OverlapCircleAll` + damage tức thời), Potion spawn ra `PotionZone` (vùng tồn tại lâu hơn) rồi tự
dọn.

**PotionZone** ([PotionZone.cs](Assets/Scripts/Weapons/PotionZone.cs)): vùng tròn tồn tại `duration` (mặc định
5s, chỉnh được), gây damage theo tick mỗi `tickInterval` (0.25s = 1 tick, damage = `Player.bulletDamage *
damagePerTickMultiplier`), ĐỒNG THỜI làm chậm Enemy đứng trong vùng qua `Enemy.ApplySlow(slowPercent)` /
`RemoveSlow()` — dùng stack-count (`slowStackCount`) để an toàn khi 1 Enemy đứng chồng nhiều PotionZone cùng lúc
(chỉ trả lại tốc độ bình thường khi hết TẤT CẢ vùng đang chồng). `OnDisable()` luôn dọn slow cho enemy còn sót lại
trong `slowedEnemies` — tránh Enemy bị kẹt chậm vĩnh viễn nếu Zone bị tắt đột ngột.

---

## 8. Hệ thống Pickup / Item / Magnet

[`Pickup.cs`](Assets/Scripts/Player/Pickup.cs) gắn vào MỌI vật phẩm hút được (EXP nhỏ/to/boss, Energy, Heart):
- Trong bán kính `Player.GetMagnetRadius()` (lõi Magnet, mặc định 0 = chưa có) thì tự bay về Player.
- `ForceMagnetPull()`: hút CƯỠNG BỨC bất kể khoảng cách/lõi Magnet — dùng riêng khi nhặt EXP Boss
  (`PlayerCollision.PullAllExpOrbsToPlayer()` gọi `ForceMagnetPull()` trên MỌI orb `ExpSmall`/`ExpBig`/`ExpBoss`
  còn lại trên bản đồ qua `GameObject.FindGameObjectsWithTag`).
- Reset `isForcePulled = false` trong `OnEnable()` — bắt buộc vì object tái sử dụng qua Pool.

[`PlayerCollision.cs`](Assets/Scripts/Player/PlayerCollision.cs) xử lý toàn bộ va chạm nhặt đồ theo Tag:
`EnemyBullet` (-10hp), `Energy` (+1 energy, nếu đang gọi Boss thì +3 XP luôn — tương đương 1.5 viên EXP nhỏ),
`Heart` (heal), `USB` (AddUSB, có thể WinGame), `ExpSmall` (+2 XP), `ExpBig` (+5 XP), `ExpBoss` (+50 XP + hút hết
EXP còn lại về phía Player), `Coin`/`Diamond` (tiền tệ — xem 8.1).

### 8.1 Tiền tệ: Coin & Kim cương (Diamond)

Hai đơn vị tiền tệ **lưu vĩnh viễn qua PlayerPrefs** (`Mayhem_Coin`, `Mayhem_Diamond`), dùng để mua mở khóa
nhân vật ở màn Character Select. Vật phẩm rơi ra mang script [CurrencyPickup.cs](Assets/Scripts/Player/CurrencyPickup.cs)
(`currencyType` Coin/Diamond + `amount`); gắn thêm `Pickup.cs` nếu muốn nó bị hút theo lõi Magnet.

- **Coin**: MỌI Enemy thường rơi đúng 1 coin khi chết. Prefab coin gán vào field `coinObject` trên từng Enemy
  prefab; drop được gọi trong `Enemy.Die()` (**KHÔNG** trong `DropItems()`, vì các subclass override trọn vẹn
  `DropItems()` sẽ làm mất coin). Để trống `coinObject` = loại quái đó không rơi coin.
- **Kim cương**: chỉ Boss rơi (Boss override `Die()` không gọi `base.Die()` nên không dính coin). Điều kiện rơi
  nằm ở `GameManager.ShouldDropDiamond()`, phải thỏa **CẢ 3**:
  1. `IsFirstBossKill()` — phase Boss ĐẦU TIÊN của ván (`currentUSB == 0`). **Cố ý rơi ở phase đầu chứ không phải
     phase cuối**: hạ Boss phase cuối là thắng luôn → `Time.timeScale = 0` ngay lúc kim cương vừa rơi ra, người
     chơi không kịp chạy tới nhặt mà bỏ lỡ là mất vĩnh viễn. Rơi sớm thì có cả ván để thong thả nhặt.
  2. `!GameProgress.IsStageCompleted(stageIndex)` — lần đầu chinh phục Stage này (đánh lại không rơi nữa).
  3. `!GameProgress.IsStageDiamondClaimed(stageIndex)` — chưa từng NHẶT kim cương của Stage này.
  Điều kiện 3 được đánh dấu lúc **commit** (`CommitRunCurrency()`), nên mỗi Stage chỉ cho đúng 1 viên kim cương
  trọn đời: nhặt rồi chết vẫn được giữ (và đánh dấu luôn), còn thoát giữa chừng thì không mất vĩnh viễn.

**Ví tạm của ván (`runCoin`/`runDiamond` trên GameManager)**: tiền nhặt trong màn KHÔNG cộng thẳng vào tổng đã lưu.
`CurrencyPickup.Collect()` → `GameManager.AddRunCoin/AddRunDiamond()` (ví tạm), rồi `CommitRunCurrency()` mới đổ vào
`GameProgress` — chỉ gọi ở `WinGame()` và `GameOverMenu()` (**chết vẫn giữ tiền**). Thoát giữa chừng ở màn Pause thì
cố tình KHÔNG commit → mất trắng, nên nút Thoát của Pause đi qua modal cảnh báo `exitConfirmPanel`
(`ShowExitConfirm` → `ConfirmExitToMainMenu`); nút Back to Menu của Win/GameOver vẫn gọi thẳng `BackToMainMenu()`.
Cờ `runCurrencyCommitted` chặn cộng 2 lần.

> Kim cương được đánh dấu `MarkStageDiamondClaimed` **trong `CommitRunCurrency()`**, KHÔNG phải lúc nhặt — nếu đánh
> dấu lúc nhặt mà người chơi thoát giữa chừng thì kim cương vừa mất vừa không bao giờ rơi lại được nữa.

**Lưu trữ**: setter `Coin`/`Diamond` chỉ `PlayerPrefs.SetInt` chứ KHÔNG `Save()` (`Save()` là ghi đĩa → giật trên
mobile). Ghi thật xuống đĩa qua `GameProgress.SaveNow()` tại các mốc an toàn: cuối `CommitRunCurrency()`,
`BackToMainMenu()`, và mọi giao dịch mua bán. Unity cũng tự flush khi thoát/chuyển nền app.

**Hiển thị**: [CurrencyUI.cs](Assets/Scripts/UI/CurrencyUI.cs) có `source` = `Total` (tổng đã lưu — Main Menu /
Character Select) hoặc `ThisRun` (nhặt trong ván — HUD trong game / Win / Game Over), tự cập nhật qua
`GameProgress.OnCurrencyChanged` / `GameManager.OnRunCurrencyChanged`.

**Thanh USB**: ẩn lúc `Start()`, hiện ở `CallBoss()` (`SetUsbBarVisible`). Vì `OnBossDefeated()` hiện KHÔNG được gọi
ở đâu nên `IsBossCalled` giữ `true` từ lần Boss đầu tiên — thanh USB hiện luôn từ đó, không bị chớp tắt mỗi lần Boss
chết/hồi sinh.

**Chia trang (dùng chung cho cả 2 màn chọn)** — [CharacterSelectPager.cs](Assets/Scripts/UI/CharacterSelectPager.cs)
và [StageSelectPager.cs](Assets/Scripts/UI/StageSelectPager.cs), mỗi cái đặt trên chính panel tương ứng để
`OnEnable` chạy được mỗi lần mở màn. Các ô nút (`slots`) được dựng sẵn 1 lần và **DÙNG CHUNG cho mọi nhân vật/Level**
— mỗi lần lật trang, pager nạp lại data tương ứng vào từng ô qua `CharacterButton.SetCharacter()` /
`StageButton.SetStage()`. Vì vậy **thêm nhân vật/Level mới chỉ cần thêm 1 phần tử vào mảng `allCharacters`/`allStages`**,
không phải dựng thêm nút hay trang nào trong Editor — đúng tinh thần data-driven ở mục 1.

Hệ quả: thứ phân biệt các ô phải lấy từ data (`CharacterData.selectIcon`/`characterName`,
`StageData.previewImage`/`stageName`) chứ không gán tay từng ô nữa. Cụm `<` `>` + text số trang tự ẩn hẳn khi chỉ
có đúng 1 trang, và mờ dần ở 2 đầu (không lật vòng). Trang cuối thừa ô thì ô thừa bị ẩn hẳn.

> 2 pager này gần như trùng code nhưng **cố ý KHÔNG gộp thành generic base class**: MonoBehaviour generic khiến
> Unity serialize field của lớp cha khó đoán và Inspector dễ hiển thị thiếu — rủi ro cao hơn lợi ích cho 1 đoạn
> logic chỉ là phép tính chỉ số. Nếu sau này có màn thứ 3 cần chia trang thì cân nhắc lại.

**Mở khóa nhân vật**: danh sách nhân vật đã mua lưu dạng **CSV trong ĐÚNG 1 key** (`Mayhem_UnlockedCharacters` =
`"Mage,Knight"`), tương tự stage đã nhận kim cương (`Mayhem_DiamondClaimedStages`). Lý do gom 1 key: PlayerPrefs
không liệt kê được key đang có, nên nếu mỗi nhân vật 1 key riêng thì `ResetProgress()` (New Game) sẽ không xóa
sạch được. Luồng UI: [CharacterButton.cs](Assets/Scripts/UI/CharacterButton.cs) thấy nhân vật bị khóa → bấm vào
mở [CharacterUnlockPanel.cs](Assets/Scripts/UI/CharacterUnlockPanel.cs) (modal chọn trả bằng Coin hay Kim cương,
nút nào không đủ tiền thì bị làm mờ) → mua xong gọi lại `CharacterButton.RefreshLockState()`.
[CurrencyUI.cs](Assets/Scripts/UI/CurrencyUI.cs) hiển thị số dư, tự cập nhật qua event `GameProgress.OnCurrencyChanged`
nên đặt được ở bất kỳ panel/HUD nào mà không cần gọi Refresh thủ công.

**New Game** (`GameProgress.ResetProgress()`): Coin/Kim cương về 0, xóa luôn 2 key CSV nói trên → nhân vật bị khóa
lại từ đầu và kim cương từng Stage có thể nhận lại; đồng thời xóa hết cấp Shop (mục 8.2).

### 8.2 Shop Power Up (chỉ số nội tại mua bằng Coin)

[ShopUpgrades.cs](Assets/Scripts/Managers/ShopUpgrades.cs) — static class chứa CẢ enum `ShopStatType` lẫn bảng số
liệu. Cố ý **hard-code thay vì làm ScriptableObject** như `CharacterData`/`StageData`: đây là 1 bảng duy nhất dùng
chung toàn game, làm asset chỉ tổ phải kéo tham chiếu vào từng Scene Level mà không được thêm gì. Chỉnh số ngay
trong file này.

5 chỉ số × 5 mức, giá tịnh tiến **100/200/300/400/500** coin. Các con số dưới đây là **giá trị cộng thêm của TỪNG
mức**, không phải tổng tích lũy (`GetTotalBonus()` tự cộng dồn từ mức 1 tới cấp đang có):

| `ShopStatType` | Mức 1→5 | Tổng khi full |
|---|---|---|
| `MaxHP` | +20/40/60/80/100 | +300 |
| `Damage` | +5/5/10/15/20 | +55 |
| `MoveSpeed` | +0.2/0.2/0.2/0.4/1 | +2.0 |
| `LifeSteal` | 1%/1%/2%/2%/3% (lưu 0.01/0.01/0.02...) | 9% |
| `Regen` | 1/1/1/1/1 HP/s | 5 HP/s |

Đổi tiền: `GameProgress.TryExchangeCoinForDiamond()` — mặc định **500 Coin = 1 Kim cương**
(`ShopUpgrades.ExchangeCoinCost`/`ExchangeDiamondGain`).

Lưu cấp: mỗi chỉ số 1 key `Mayhem_Shop_<tên enum>`. KHÔNG cần gom CSV như nhân vật vì `ShopStatType` là enum cố
định → `ResetProgress()` duyệt `Enum.GetValues` là xóa đủ. **Tên các giá trị enum chính là khóa lưu** — đổi tên sẽ
làm mất cấp đã mua của người chơi.

**Áp vào Player**: `Player.ApplyShopUpgrades()` gọi từ `GameManager.Start()`, **BẮT BUỘC sau `ApplyCharacterData()`**
(hàm đó ghi đè `maxHP`/`bulletDamage`/`moveSpeed` bằng chỉ số gốc nhân vật, gọi trước là mất sạch bonus) và đặt
NGOÀI khối `if (currentCharacter != null)` để vẫn chạy khi Play thẳng Scene trong Editor. Hàm này chốt lại
`baseMoveSpeedSnapshot` sau khi cộng speed, nếu không augment Speed sẽ bị cắt trần sớm.

UI (bố cục kiểu Subway Surfers: icon + tên + thanh vạch cấp chia ô + nút giá/"Full"):
- [ShopUpgradeButton.cs](Assets/Scripts/UI/ShopUpgradeButton.cs) — 1 dòng = 1 chỉ số. `levelSegments[]` là mảng các ô
  VÀNG của thanh vạch (nên đủ 5 ô), script chỉ bật `level` ô đầu và tắt phần còn lại; khung ô trống để hiện sẵn phía
  sau. Nút mua tự làm mờ khi thiếu tiền, đổi chữ thành `Full` khi đã 5/5.
- [CoinExchangeButton.cs](Assets/Scripts/UI/CoinExchangeButton.cs) — bộ chọn số lượng: nút `+` / `-` / `Max`
  (Max = số nhiều nhất đổi được với ví hiện tại) + 2 text (lượng Coin / lượng Kim cương của giao dịch) + nút chốt Đổi.
  `selectedDiamond` luôn được kẹp trong `[1, maxAffordable]` ngay trong `Refresh()`, nên mọi nút chỉ cần đổi số rồi
  gọi `Refresh()` là an toàn. Dùng chung cho **cả 2 chiều** qua enum `direction` (`CoinToDiamond` / `DiamondToCoin`)
  — đặt 2 instance, mỗi cái 1 chiều. Kim cương LUÔN là đơn vị đếm ở cả 2 chiều, chỉ khác bên nào trả bên nào nhận;
  đổi ngược bị giới hạn bởi số Kim cương đang có thay vì số Coin. Muốn chiều ngược thiệt hơn (chống đổi qua đổi
  lại kiếm lời) thì để `coinPerDiamond` nhỏ hơn ở instance đó.

Cả hai nghe `GameProgress.OnCurrencyChanged` nên mua/đổi ở 1 chỗ là cả bảng tự cập nhật lại — không cần ai gọi
Refresh thủ công (kể cả `CurrencyUI` hiển thị số dư ở màn chọn nhân vật phía sau).

**Vị trí**: Shop nằm TRONG màn Chọn nhân vật (mua chỉ số ngay trước khi vào màn). `MainMenuUI.ShowShop()` mở ĐÈ LÊN
chứ không gọi `HideAllPanels()` — cùng kiểu modal với `newGameConfirmPanel` — nên `CloseShop()` là thấy lại ngay
màn chọn nhân vật. `shopPanel` vẫn nằm trong `HideAllPanels()` để tự đóng khi rời sang panel khác.

> **GOTCHA thứ tự Start()**: Unity KHÔNG đảm bảo thứ tự `Start()` giữa các MonoBehaviour khác nhau. Vì vậy
> (a) `regenAmount = 0f` đã phải chuyển từ `Player.Start()` sang `Player.Awake()` — nếu để ở Start nó có thể xóa
> mất lượng hồi máu Shop vừa áp; (b) `GameManager` gọi `GameUI.RefreshAllStats()` sau khi áp Shop vì `GameUI.Start()`
> có thể đã vẽ số liệu cũ trước đó.

---

## 9. Object Pooling

[`ObjectPoolManager.cs`](Assets/Scripts/Managers/ObjectPoolManager.cs) — 1 `ObjectPool<GameObject>` riêng cho MỖI
prefab (tạo động lúc `SpawnObject()` gọi lần đầu), dùng `UnityEngine.Pool.ObjectPool`.

**Gotcha đã fix — thứ tự set vị trí**: `actionOnGet` (closure) KHÔNG được set position/rotation/SetActive vì
closure chỉ tạo 1 LẦN lúc khởi tạo Pool — set ở đó sẽ khiến MỌI lần Spawn sau dùng nhầm vị trí của lần gọi đầu
tiên. Vị trí/rotation/kích hoạt phải set NGAY SAU khi `Get()` trả về, trong chính `SpawnObject()`.

**Gotcha đã fix — parent dính lại khi tái sử dụng**: `ReturnObjectToPool()` LUÔN gọi `obj.transform.SetParent(null)`
TRƯỚC khi Release — bắt buộc cho các hiệu ứng được gắn làm con Player (`attachToPlayer = true` ở KnightCombat, VD
Khiên/Xoay Kiếm) để lần Spawn tiếp theo (dùng cho object/nhân vật khác) không bị dính nhầm parent cũ.

Mọi nơi spawn effect/bullet/enemy/pickup trong project đều theo pattern: `if (ObjectPoolManager.Instance != null)
SpawnObject(...) else Instantiate(...)` (fallback an toàn nếu Pool Manager chưa có trong Scene).

---

## 10. Audio ([AudioManager.cs](Assets/Scripts/Managers/AudioManager.cs))

3 `AudioSource` ĐỘC LẬP: `effectAudioSource` (SFX), `defaultAudioSource` (nhạc nền), `bossAudioSource`. **Boss
audio dùng CHUNG slider/mute với nhạc nền (Default)** — theo yêu cầu người dùng, KHÔNG tách riêng: `SetDefaultVolume()`/
`ToggleDefaultMute()` áp dụng đồng thời lên cả `defaultAudioSource` VÀ `bossAudioSource`. `SetEffectVolume()`/
`ToggleEffectMute()` chỉ áp lên `effectAudioSource`. `SetMasterVolume()`/`ToggleMute()` dùng `AudioListener.volume`
(tổng toàn bộ game, độc lập với 2 nhóm trên nhưng nhân chồng lên chúng theo cơ chế mixing mặc định của Unity).
`PlaySFX(clip)` — dùng cho âm thanh RIÊNG theo nhân vật (VD tiếng bắn/nạp đạn Mage khác Gunner, set qua
`CharacterData.shootSound`/`reloadSound`).

---

## 11. UI Menu ([MainMenuUI.cs](Assets/Scripts/UI/MainMenuUI.cs))

5 panel loại trừ lẫn nhau (`HideAllPanels()` rồi bật đúng 1 panel): `mainMenuPanel`, `stageSelectPanel`,
`characterSelectPanel`, `howToPlayPanel`, `newGameConfirmPanel` (modal, KHÔNG bị `HideAllPanels` ẩn theo — nó nổi
lên trên panel khác, `ShowNewGameConfirm()` chỉ bật thêm chứ không ẩn panel phía sau).

`ConfirmNewGame()` → `GameProgress.ResetProgress()` (khóa lại hết Stage, xóa `SelectedStage`/`SelectedCharacter`)
→ `ShowStageSelect()`.

---

## 12. Danh sách file & vai trò (map nhanh)

```
Managers/
  GameManager.cs        — Vòng đời 1 ván chơi: energy/USB/XP, Boss call, Win/GameOver, áp CharacterData/StageData
  GameProgress.cs        — Static: tiến trình mở khóa Stage (PlayerPrefs) + lựa chọn hiện tại (transient)
  AugmentManager.cs      — Toàn bộ hệ thống augment (pool, chọn ngẫu nhiên/forced, áp hiệu ứng, cap)
  CharacterData.cs        — ScriptableObject cấu hình nhân vật
  StageData.cs             — ScriptableObject cấu hình độ khó/augment riêng theo Stage
  ShopUpgrades.cs          — Bảng số liệu + logic mua của Shop Power Up (enum ShopStatType nằm cùng file)
  AudioManager.cs         — Master/Default(+Boss)/Effect volume độc lập
  ObjectPoolManager.cs    — Pool dùng chung cho mọi prefab spawn động
  CursorManager.cs         — (chưa tài liệu hóa chi tiết — ít thay đổi)
  AutoDestroyOrPool.cs     — Tự trả 1 GameObject về Pool/Destroy sau X giây (dùng cho hiệu ứng 1 lần)

Player/
  Player.cs               — Di chuyển, HP, Dash, Blink, augment stat modifiers, animation hook trung tâm
  Pickup.cs                — Gắn vào vật phẩm hút được (Magnet)
  PlayerCollision.cs       — Xử lý va chạm nhặt đồ theo Tag
  CurrencyPickup.cs        — Vật phẩm Coin/Kim cương, cộng thẳng vào GameProgress khi nhặt

Weapons/
  Gun.cs                    — Bắn/nạp đạn Ranged (Gunner/Mage dùng chung), Bomb, Potion, ManaRegen
  PlayerBullet.cs           — Đạn người chơi, splash damage (Mage)
  KnightCombat.cs           — Toàn bộ combat Melee (Knight): chém tự động, Stamina, Khiên, Xoay Kiếm
  Bomb.cs / Potion.cs       — Object bay tới đích rồi kích hoạt (nổ / spawn zone)
  PotionZone.cs              — Vùng DoT + slow
  EnemyBullet.cs             — Đạn của Enemy/Boss

Enemies/
  Enemy.cs                  — Base abstract: HP/move/collision/stage-difficulty dùng chung
  BasicEnemy/MiniEnemy/EnergyEnemy/HealEnemy/ExplosionEnemy.cs — Override tối thiểu theo hành vi riêng
  BossEnemy.cs                — Skill ngẫu nhiên, hồi sinh, Teleport có telegraph
  EnemySpawner.cs              — Spawn định kỳ, tăng tốc độ spawn theo currentLevel (5, 10)
  HeartPickup.cs                — healValue cho vật phẩm Heart

Effects/
  Explosion.cs                 — Hiệu ứng nổ (dùng bởi Bomb, tự dọn)

UI/
  GameUI.cs                    — Cập nhật text/HUD trong ván chơi (Update...Text gọi từ AugmentManager)
  MainMenuUI.cs                — Điều hướng panel Main Menu
  StageButton.cs / CharacterButton.cs — Nút chọn Level/Nhân vật (CharacterButton kiêm trạng thái khóa + giá)
  CharacterSelectPager.cs / StageSelectPager.cs — Chia trang nhân vật/Level, lật bằng 2 nút < >
                                  (các ô nút dùng chung, nạp data theo trang)
  CharacterUnlockPanel.cs       — Modal xác nhận mua nhân vật, chọn trả bằng Coin hoặc Kim cương
  CurrencyUI.cs                 — Hiển thị số Coin/Kim cương, tự cập nhật qua event
  ShopUpgradeButton.cs          — 1 dòng chỉ số trong Shop (cấp, giá, phần thưởng mức kế, nút Mua)
  CoinExchangeButton.cs         — Ô đổi Coin sang Kim cương
  DragAimButton.cs (abstract) → BombButton.cs / PotionButton.cs / BlinkButton.cs (chọn ĐIỂM)
                              → LaserButton.cs (chọn HƯỚNG, mũi tên xoay tại chỗ)
  ShieldButton.cs               — Giữ/thả (không phải drag-aim)
  ScrollingBackground.cs        — Nền cuộn vô hạn 2 ảnh (Main Menu)
```

---

## 13. Các bug/gotcha đã gặp & fix — tra cứu nhanh khi debug vấn đề tương tự

| Triệu chứng | Nguyên nhân gốc | Fix |
|---|---|---|
| Boss chết rớt nhiều USB/EXP cùng lúc | `Die()` chạy nhiều lần trong 1 frame khi nhiều đạn trúng cùng lúc, không có guard | Thêm `isDead` guard đầu `TakeDmg()`/`Die()` |
| Enemy chạm Player chỉ trừ máu lúc đầu, đứng yên thì hết trừ | Rigidbody2D tự sleep sau ~0.5s đứng yên → `OnTriggerStay2D` ngừng bắn | `rb.sleepMode = RigidbodySleepMode2D.NeverSleep` ở cả `Enemy.Awake()` và `Player.Awake()` |
| stayDmg trừ máu quá nhanh (như ~50 lần/giây) | `OnTriggerStay2D` chạy theo nhịp vật lý, không phải 1 lần/giây | Tự throttle bằng `stayDmgTimer`/`stayDmgInterval` NGAY TRONG `OnPlayerStay()` |
| Augment "SplashDamage lõi Mage" không hoạt động, biên dịch lỗi | Gọi method trên `PlayerBullet` (object pooled) thay vì `Player` (persistent); thiếu `break` gây rơi qua case khác | Method đổi tên tồn tại phải đặt trên `Player`, đọc lại qua `Player.Instance.GetSplashDamageBonus()` mỗi lần bullet cần dùng |
| Reload/Regen theo augment tự "biến mất" hoặc không nhất quán | Field lưu trên OBJECT ĐƯỢC POOL (VD `PlayerBullet`) thay vì trên SINGLETON PERSISTENT (`Player`/`Gun`/`KnightCombat`) | Luôn lưu stat cộng dồn từ augment trên `Player`/`Gun`/`KnightCombat` — KHÔNG BAO GIỜ trên `PlayerBullet`/`Pickup`/Enemy pooled instance |
| Hiệu ứng chém/Khiên/Xoay Kiếm đứng yên không bám theo Player khi di chuyển | Spawn effect không gắn parent | `SpawnEffect(prefab, pos, attachToPlayer: true)` — set `transform.SetParent(this.transform)` |
| Hiệu ứng gắn-theo-Player bị dính lại object khác lần sau khi tái sử dụng Pool | `ReturnObjectToPool()` không gỡ parent trước khi Release | Luôn `obj.transform.SetParent(null)` trước `pool.Release(obj)` |
| Chọn Knight vẫn thấy súng/nút Bắn/Nạp đạn/Bomb/Potion hiện ra | `Gun.Start()` (nơi ẩn `bombButtonObj`/`potionButtonObj` mặc định) KHÔNG BAO GIỜ chạy nếu `Gun.enabled = false` được set TRƯỚC khi Unity gọi `Start()` | Chuyển toàn bộ logic ẩn/hiện vào `Gun.SetActive(bool)` (gọi trực tiếp từ `GameManager`), không phụ thuộc `Start()` |
| Augment ép buộc (Bomb cấp 8 / Burst-Split cấp 10) ra màn hình TRỐNG khi chơi Mage | Không kiểm tra augment đó có tồn tại trong pool của nhân vật hiện tại trước khi ép buộc | Luôn `augmentPool.Find(a => a.type == "...")` trước, `!= null` mới ép buộc, ngược lại rơi về random bình thường |
| Mana Regen tỉ lệ lẻ (VD 0.25/s) không cộng được gì | Cộng thẳng số thập phân vào `currentAmmo` (int) → làm tròn về 0 | Dùng `manaRegenAccumulator` (float) gom dần qua nhiều giây, chỉ cộng phần nguyên khi đủ ≥1 |
| Player bị giật lùi (knockback) ngay lúc bắn | `Rigidbody2D` của `PlayerBullet` để `Dynamic`, spawn đè lên Collider Player bị vật lý đẩy ra | Ép `rb.bodyType = Kinematic` trong `PlayerBullet.Awake()` |
| Aim Bomb/Potion/Blink xuất hiện dính tại nút bấm, khó kéo | Aim reticle bám theo vị trí tuyệt đối ngón tay / vị trí nút | `DragAimButton` tính `worldDelta` (độ lệch so lúc mới nhấn), cộng vào vị trí NHÂN VẬT, không phải vị trí nút |
| CS0163 "not all code paths return a value" khi tự thêm `case` mới vào switch trong `AugmentManager.ApplyEffect()` | Thiếu `break;` cuối case, rơi (fall-through) sang case kế tiếp | MỌI case (trừ case cuối switch) bắt buộc có `break;`/`return;` — kiểm tra kỹ khi tự sửa tay |
| Mua chỉ số Shop: UI lệch đúng 1 nhịp (bấm lần đầu không lên cấp), và khi full 5/5 phải thoát ra vào lại mới thấy "Full" | `OnCurrencyChanged` bắn ra NGAY LÚC trừ Coin, tức TRƯỚC khi `TryBuyUpgrade` ghi cấp mới → dòng đó vẽ lại bằng cấp cũ. Còn khi đã full thì hàm thoát sớm, không trừ Coin nên event KHÔNG bắn phát nào | Dòng vừa bấm phải tự gọi `Refresh()` sau `TryBuyUpgrade()`. **Quy tắc chung: đừng dùng event "tiền thay đổi" để vẽ lại thứ phụ thuộc trạng thái KHÁC ngoài tiền** — event có thể bắn giữa chừng giao dịch, hoặc không bắn khi giao dịch bị từ chối |

---

## 14. Quy ước khi thêm/sửa tính năng (đọc trước khi code)

1. **Thêm nhân vật mới**: tạo `CharacterData` asset mới, KHÔNG tạo script Player/Gun/KnightCombat riêng. Nếu cần
   hành vi hoàn toàn mới (không phải Ranged/Melee), cân nhắc thêm `CombatType` thứ 3 và 1 script combat mới, theo
   đúng pattern `SetActive(bool)` + `ApplyCharacterData(CharacterData)` đã có. Nhớ set `characterName` DUY NHẤT
   (dùng làm khóa lưu trạng thái đã mua) + `coinPrice`/`diamondPrice`, và để `unlockedByDefault = false` nếu
   nhân vật đó phải mua mới có.
2. **Thêm augment mới**: quyết định phạm vi (chung/riêng nhân vật/riêng stage) rồi thêm vào đúng chỗ (`InitializePool()`
   / `CharacterData.exclusiveAugments` asset / `StageData.extraAugments` asset). Luôn thêm `case` trong CẢ
   `ApplyEffect()` (hiệu ứng) VÀ (nếu có trần/chỉ-chọn-1-lần) `CheckAndRemoveAugment()`. Đừng quên `break;`. Nếu
   augment cần lưu trạng thái cộng dồn, lưu trên `Player`/`Gun`/`KnightCombat` — KHÔNG BAO GIỜ trên object pooled.
3. **Thêm Level mới**: tạo Scene mới (KHÔNG dùng chung Scene) + `StageData` asset mới trỏ đúng `sceneName`, thêm
   vào Build Settings, thêm `StageButton` mới trong `stageSelectPanel`.
4. **Thêm loại Enemy mới**: kế thừa `Enemy.cs`, chỉ override đúng phần khác biệt (theo pattern các subclass hiện
   có), đừng copy lại logic va chạm/damage-stay đã có sẵn ở base.
5. **Thêm hiệu ứng animation cho hành động mới**: theo đúng pattern hiện tại — spawn prefab sprite/animation riêng
   (`SpawnEffect`/`ObjectPoolManager.SpawnObject`), KHÔNG bake vào Animator Controller của nhân vật (người dùng tự
   quản lý animation theo cách này, đã xác nhận rõ ràng).
6. Trước khi sửa 1 file lớn (`Player.cs`, `Gun.cs`, `KnightCombat.cs`, `AugmentManager.cs`) — **luôn Read lại toàn
   bộ file trước** vì người dùng có thể đã tự sửa tay trực tiếp trong Unity/editor giữa các lượt làm việc.
7. Không có `gh` CLI trên máy này — khi cần tạo PR, dùng `git push` rồi đưa link tạo PR thủ công dạng
   `https://github.com/HxyBean/Mayhem/compare/main...<branch>?expand=1`.
