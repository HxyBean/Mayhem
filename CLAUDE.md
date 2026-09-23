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
  hạn cứng bán kính 5f** từ vị trí nhân vật (`Player.maxBlinkRange`), cooldown dài hơn Dash.
  Có hiệu ứng animation riêng lúc biến mất/xuất hiện (`blinkStartEffectPrefab`/`blinkEndEffectPrefab`).

  **`FindSafeBlinkPosition()` quét CẢ ĐƯỜNG ĐI bằng `Physics2D.CircleCast`, không chỉ kiểm tra điểm đích.**
  Vật cản trở nên ĐẶC hoàn toàn: không xuyên qua được, không đáp vào trong được, dày hay mỏng đều chặn như nhau.
  Gặp vật cản thì **dừng ngay trước nó** chứ không hủy chiêu — đi được bao xa hay bấy nhiêu, vẫn hơn là đứng yên
  mà vẫn mất lượt hồi chiêu.

  > **Bài học — đừng để 1 tham số gánh 2 việc mâu thuẫn.** Cách cũ chỉ `OverlapCircle` tại đích rồi lùi dần, nên
  > `blinkCheckRadius` vừa phải NHỎ (để lách khe giữa các chướng ngại vật) vừa phải LỚN (để không nhảy xuyên
  > tường dày 2 đơn vị). Không có giá trị nào đúng cả: để 1 thì kẹt ở mọi khe hẹp, giảm xuống dưới 0.95 thì lọt
  > hẳn vào trong cụm 2 rock chồng nhau. Quét đường đi tách 2 việc ra: `blinkCheckRadius` **chỉ còn là bề ngang
  > nhân vật** (để nhỏ, khớp Collider của Player), việc chặn xuyên tường do chính phép quét lo.
  >
  > Hệ quả về gameplay: Blink giờ **chỉ tới được chỗ nhìn thẳng tới được** — không "nhảy cóc" qua đá nữa.
  >
  > Nếu vẫn đáp được vào trong đá sau thay đổi này thì lỗi KHÔNG còn ở code: kiểm tra `Collider2D` của prefab đá
  > có phủ đúng phần hình vẽ không (collider nhỏ hơn sprite là có khoảng trống nhìn thì đặc mà vật lý thì rỗng),
  > và Layer của đá có nằm trong `blinkObstacleMask` không.
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
RangedEnemy   — override Update: ngoài stopRadius (8) thì MoveToPlayer(), vào trong thì đứng yên FlipEnemy()
                + bắn EnemyBullet mỗi attackCoolDown; override DropItems (0-1 viên to + 1-2 viên nhỏ)
PoisonEnemy   — Tiếp cận như RangedEnemy nhưng bắn đạn CẦU VỒNG có cảnh báo điểm rơi, để lại vùng độc.
                Animation bắn chạy TRƯỚC rồi mới nhả đạn (xem 6.4)
USBEnemy      — CHỈ override DropItems (rơi vật phẩm USB); KHÔNG spawn ngẫu nhiên (xem 6.3). Chiêu Lướt nằm ở
                component EnemyDashSkill gắn kèm, không phải trong file này
BossEnemy     — override OnEnable/Update/DropItems/Die, skill ngẫu nhiên + Teleport có telegraph + Lướt
                (dùng chung EnemyDashSkill với USBEnemy, kiểu kích hoạt thủ công)
```

> `RangedEnemy` là ví dụ chuẩn của việc override `Update()`: nó KHÔNG gọi `base.Update()` (vì base luôn
> `MoveToPlayer()`), mà tự quyết định lúc nào đuổi lúc nào đứng bắn. Vẫn dùng lại `MoveToPlayer()`/`FlipEnemy()`
> của base thay vì viết lại.

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
gọi `GameManager.OnBossDefeated()` → `bossPhaseCount++` + `UpdateUsbBar()` → đủ `usbThreshold` thì `WinGame()`, chưa
đủ thì `SpawnBossWarningThenCallBoss()` (hiệu ứng cảnh báo `bossRevive`, 2s sau Boss xuất hiện lại).

> Cách cũ (cộng tiến trình lúc NHẶT USB) tạo lỗ hổng: cứ bỏ viên USB nằm dưới đất là kẹt phase vĩnh viễn mà vẫn
> farm coin/kinh nghiệm từ quái thường vô hạn. Tính theo lần hạ Boss thì người chơi không còn cần gạt nào để
> trì hoãn, nên lỗ hổng biến mất về mặt cấu trúc — cơ chế hẹn giờ ép Boss respawn từng thêm vào đã được gỡ bỏ.

Vật phẩm **USB giờ chỉ còn là đồ hồi đầy máu** (`PlayerCollision` → `player.RestoreFullHP()`), không còn vai trò
tiến trình. Không muốn Boss rơi USB nữa thì bỏ trống `usbPrefabs` trên prefab Boss, không cần sửa code.

**Tên `usbThreshold`/`usbBar` giữ nguyên** dù nay mang nghĩa "phase Boss" — đổi tên field `[SerializeField]` sẽ
làm mất giá trị/tham chiếu đã gán trong Inspector mà Unity không báo lỗi gì. Riêng `currentUSB` đã đổi thành
`bossPhaseCount` (được, vì nó là field `private` thuần, Inspector không đụng tới) để khỏi lẫn với vật phẩm USB
thật ở mục 6.3.

**Hệ quả — `WinGame()` gom phần thưởng trước khi đóng băng**: thắng xảy ra NGAY lúc hạ Boss phase cuối, tức
`Time.timeScale = 0` ngay khi coin vừa rơi ra từ đám quái cuối cùng. Vì vậy `WinGame()` gọi
`CollectDroppedCurrency()` (quét tag `Coin`/`Diamond` còn trên bản đồ, `Collect()` rồi trả về Pool) **trước**
`CommitRunCurrency()`. Kim cương đã tránh được vấn đề này bằng cách rơi ở phase đầu, nhưng bước gom vẫn cần cho
coin — và là lưới an toàn nếu ai đó set `usbThreshold = 1` (phase đầu cũng chính là phase cuối).

**PHỤ THUỘC THỨ TỰ trong `BossEnemy.Die()`**: `DropItems()` (nơi gọi `ShouldDropDiamond()` → `IsFirstBossKill()`)
phải chạy TRƯỚC `OnBossDefeated()` (nơi tăng `bossPhaseCount`). Nhờ vậy ở phase đầu tiên `bossPhaseCount` vẫn đang là 0.
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

### 6.3 USBEnemy & tài nguyên USB

**Spawn theo mốc số mạng, không ngẫu nhiên**: [EnemySpawner.cs](Assets/Scripts/Enemies/EnemySpawner.cs) giữ
`killCount`; `Enemy.Die()` (base) gọi `EnemySpawner.Instance.OnEnemyKilled()`, đủ `killsPerUsbEnemy` (50) thì
spawn `usbEnemyPrefab` tại 1 spawn point ngẫu nhiên rồi reset bộ đếm. **Đừng cho prefab này vào mảng `enemies`**
của spawner, nếu không nó sẽ vừa spawn theo mốc vừa spawn ngẫu nhiên. Boss KHÔNG tính vào bộ đếm (Boss override
`Die()` không gọi `base.Die()`) — cố ý, vì mốc này thưởng cho việc dọn quái thường.

**Chiêu Lướt** — nằm ở component RỜI [EnemyDashSkill.cs](Assets/Scripts/Enemies/EnemyDashSkill.cs), KHÔNG viết
trong `USBEnemy.cs`. Gắn component vào prefab nào thì con đó có chiêu; hiện USBEnemy và **Boss** đều dùng chung.
Thêm chiêu cho loại quái mới = Add Component, không sửa dòng code nào (cùng kiểu opt-in với `DamageFlash`/
`MinimapMarker`).

2 kiểu kích hoạt qua `autoTriggerByProximity`:
- **Bật** (USBEnemy): tự lướt khi Player vào trong `triggerRadius` (8).
- **Tắt** (Boss): chờ `TryDash()` được gọi — Boss gọi từ bộ chọn chiêu ngẫu nhiên (`PickRandomSkill` case 5),
  vì lượt dùng chiêu do bộ chọn quyết định chứ không phải cứ tới gần là lướt. Còn trong thời gian hồi chiêu thì
  `TryDash()` trả `false` và lượt đó coi như bỏ lỡ, đúng kiểu `Teleport()` đã làm sẵn.

Hướng lướt được **khóa ngay từ đầu lúc vận sức** chứ không cập nhật liên tục — giống chiêu Teleport của Boss, để
Player có trọn `chargeTime` né sang bên thay vì bị chiêu bám dính. `OnEnable()` reset cờ `isCharging`/`isDashing`
(Pool tái sử dụng), `OnDisable()` dọn hiệu ứng cảnh báo — chết giữa lúc vận sức mà không dọn thì vệt cảnh báo
nằm lại vĩnh viễn trên bản đồ.

> **Chốt chặn di chuyển đặt trong `Enemy.MoveToPlayer()`** (`if (IsDashing) return;`), KHÔNG phải trong `Update()`
> của base: mọi subclass đều đi qua `MoveToPlayer()` dù có override `Update()` hay không, nên đúng 1 dòng đó là
> cả 8 loại quái xử lý đúng mà không phải sửa gì. Boss cần thêm `IsDashing` vào điều kiện thoát sớm của
> `Update()` — không phải để chặn di chuyển (đã có rồi) mà để không tung chiêu khác đè lên lúc đang lướt.

> **`Random.Range(0, n)` của Boss KHÔNG bao gồm cận trên.** Thêm `case` mới vào `PickRandomSkill()` mà quên tăng
> `n` thì chiêu mới không bao giờ được chọn — và không có lỗi nào báo ra.

> Hiệu ứng cảnh báo **cố ý KHÔNG gắn làm con của quái** (khác hiệu ứng chém của Knight), mà spawn ở world space
> rồi tự kéo theo. Lý do ở quy tắc chung tại mục 9. Gắn làm con còn kéo theo một bug thứ 2: `FlipEnemy()` lật quái
> bằng `localScale.x = -1`, con thừa hưởng scale âm nên sprite bị **soi gương** — lướt sang trái mà mũi tên nhìn
> vẫn như chĩa sang phải, trông y hệt "không xoay theo hướng".
>
> Việc đồng bộ vị trí/góc xoay đặt ở **`LateUpdate()`**, KHÔNG phải `Update()`: nếu prefab cảnh báo có Animator mà
> clip lỡ có key Rotation (hay gặp khi record animation hiệu ứng), Animator chạy sau `Update()` và ghi đè sạch góc
> xoay vừa set. Áp dụng cho MỌI hiệu ứng cần giữ góc xoay do code quyết định.

**Tài nguyên USB** (`GameManager.collectedUsb`, đọc qua `CollectedUsb`): là tài nguyên **TIÊU HAO trong ván**,
KHÔNG lưu qua ván như Coin/Kim cương. `PlayerCollision` nhặt USB → `GameManager.AddUsb(1)` **rồi mới** báo
`NPC.OnUSBCollected()` (đảo thứ tự là nhiệm vụ luôn thiếu đúng 1 viên, vì NPC đọc lại chính con số đó). Giao
nhiệm vụ cho NPC → `SpendUsb(usbRequired)` trừ đi. Hiển thị qua `usbText` trên `CurrencyUI` (không phụ thuộc
`source` vì USB luôn là của ván hiện tại).

> **ĐỪNG NHẦM `collectedUsb` với `bossPhaseCount`.** `bossPhaseCount` (trước tên là `currentUSB`, đã đổi để bớt
> nhầm) là số phase Boss đã hạ — tiến trình thanh "USB" trên HUD. Còn `collectedUsb` mới là vật phẩm USB thật.
> Hai thứ không liên quan gì nhau; chỉ trùng chữ "USB" vì các field `[SerializeField]` `usbThreshold`/`usbBar`
> trót đặt tên vậy từ trước và đổi tên sẽ mất tham chiếu trong Inspector.

### 6.4 PoisonEnemy — đạn cầu vồng + vùng độc

3 file: [PoisonEnemy.cs](Assets/Scripts/Enemies/PoisonEnemy.cs) (con quái) +
[PoisonProjectile.cs](Assets/Scripts/Weapons/PoisonProjectile.cs) (viên đạn + vệt cảnh báo) +
[PoisonZone.cs](Assets/Scripts/Weapons/PoisonZone.cs) (vùng độc để lại).

Cơ chế tiếp cận giống `RangedEnemy` (ngoài `stopRadius` = 8 thì đuổi, vào trong thì đứng bắn), `attackCoolDown`
= 5s. Khác ở 3 điểm:

**1. Animation bắn chạy TRƯỚC rồi mới nhả đạn** (`ShootRoutine` → trigger animation → `WaitForSeconds
(shootAnimationDelay)` → `FireProjectile()`). Bắn ngay lúc kích hoạt trigger thì đạn bay ra trước cả khi con quái
kịp vung tay, nhìn như bị lỗi. `shootAnimationDelay` là con số **duy nhất** quyết định animation có ăn khớp hay
không — chỉnh cho trùng đúng khung hình vung tay trong clip.

> Cờ `isAttacking` làm con quái đứng im trong lúc vận đòn (`Update()` thoát sớm). **BẮT BUỘC reset trong
> `OnEnable()`**: con trước chết ngay giữa lúc vận đòn thì coroutine bị giết, cờ còn sót lại, và con mới lấy từ
> Pool sẽ đứng đơ vĩnh viễn vì `Update()` luôn thoát sớm.

> **`OnEnable()` đặt `nextAttackTime = 0f` (sẵn sàng bắn ngay), KHÔNG phải `Time.time + attackCoolDown`.**
> Cách sau làm đồng hồ hồi chiêu chạy từ lúc SPAWN chứ không phải từ lúc bắn — con quái đi tới nơi rồi vẫn phải
> đứng chờ nốt phần thời gian còn lại, nhìn y như bị đơ. Với cooldown 5s thì quãng chờ đó dài tới mức không thể
> không để ý. Bắn ngay lúc vào tầm vẫn công bằng vì người chơi còn cả animation vung tay + thời gian đạn bay +
> vệt cảnh báo để né.
>
> `RangedEnemy` vẫn đang giữ kiểu cũ (`Time.time + attackCoolDown`) — cooldown của nó chỉ 2s nên ít lộ hơn,
> nhưng về bản chất là cùng một vấn đề. Sửa nếu thấy con đó cũng khựng lúc mới vào tầm.

**2. Đạn bay theo VÒNG CUNG tới 1 điểm đã khóa**, có vệt cảnh báo đứng tại điểm rơi suốt thời gian bay (giống
telegraph của chiêu Teleport Boss). Đạn **không có Collider và không gây sát thương DỌC ĐƯỜNG BAY** — nó bay qua
đầu mọi thứ. Sát thương chia 2 chặng: **`impactDamage` ngay khi chạm đất** (đứng lì trong vệt cảnh báo là ăn
đòn luôn, không phải chờ tick đầu của vùng độc) + DoT của vùng độc để lại.

> `impactRadius` **nên để bằng bán kính vùng độc và khớp sprite vệt cảnh báo**. Người chơi coi vệt cảnh báo là
> vùng nguy hiểm — 3 con số này lệch nhau là ăn đòn ở chỗ nhìn như an toàn, kiểu bất công khó chịu nhất.
>
> `DealImpactDamage()` đo từ `targetPosition` chứ không phải `transform.position`: 2 giá trị trùng nhau ở khoảnh
> khắc chạm đất, nhưng lấy đúng điểm đã cảnh báo thì sát thương luôn khớp với thứ người chơi nhìn thấy, kể cả
> sau này có sửa cách tính đường bay.

> **Điểm rơi khóa tại lúc NHẢ ĐẠN, không phải lúc bắt đầu animation.** Nghĩa là người chơi di chuyển trong lúc
> animation chạy vẫn bị ngắm trúng — chủ ý, vì nếu khóa từ đầu animation thì chỉ cần đi bộ là né được và
> animation chỉ còn là trang trí. Cơ hội né thật sự là `PoisonProjectile.flightDuration` (1.2s): để quá ngắn là
> không né được, quá dài thì né quá dễ.

> Vệt cảnh báo đứng YÊN tại đích, **không gắn làm con của viên đạn** — vừa vì nó phải nằm im ở đích trong khi
> đạn còn bay, vừa vì gắn làm con của object sắp về Pool là dính đúng lỗi ở mục 9. `OnDisable()` của đạn dọn vệt
> cảnh báo, nếu không thì đạn bị tắt giữa chừng sẽ để lại vệt nằm vĩnh viễn và người chơi né mãi một chỗ chẳng
> bao giờ có gì rơi xuống.

**3. Vùng độc** (`PoisonZone`) tồn tại `duration` (3s), gây `damagePerTick` cho Player mỗi `tickInterval`.

> **CỐ Ý KHÔNG tái dùng `PotionZone`** (mục 7): vùng đó là đồ của người chơi nên quét tag `Enemy` và lấy damage
> từ `Player.bulletDamage`. Đây là hướng ngược lại hoàn toàn — gộp chung sẽ phải nhét cờ "bên nào" vào giữa và
> làm cả 2 khó đọc.
>
> Khác `PotionZone` thêm 1 điểm: đo khoảng cách thẳng tới `Player.Instance` thay vì Collider + `OverlapCircle`.
> Cả màn chỉ có đúng 1 Player nên quét vật lý là thừa, và nhờ vậy prefab vùng độc **KHÔNG cần Collider2D/
> Rigidbody2D** — bớt 2 thứ dễ quên khi dựng.
>
> Tick đầu tiên của vùng độc lùi lại 1 nhịp, vì cú nổ lúc chạm đất (`impactDamage`) đã lo phần "đứng lì thì ăn
> đòn ngay" rồi. Cộng thêm 1 tick ngay khoảnh khắc đó nữa là ăn 2 lần trong cùng 1 frame mà không có cách nào
> phản ứng. Nhịp lùi lại này chính là cơ hội chạy khỏi vũng độc còn sót lại.

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

**Vật phẩm văng ra khi quái chết** — [ItemDropMotion.cs](Assets/Scripts/Effects/ItemDropMotion.cs), gắn vào prefab
vật phẩm nào muốn có (không gắn thì rơi đứng im như cũ, `Enemy.SpawnItem()` tự kiểm tra). `SpawnItem()` spawn vật
phẩm **ngay tại xác quái** rồi `Launch()` bay theo vòng cung tới chỗ đáp — chính việc spawn tại xác quái mới tạo
ra cảm giác "văng ra", chứ không phải hiện sẵn ở chỗ đáp.

> **`Pickup.Update()` phải nhường chỗ trong lúc vật phẩm còn đang bay** (`if (dropMotion.IsFlying) return;`).
> Cả 2 script đều ghi thẳng `transform.position` mỗi frame, chạy song song là vật phẩm giật qua giật lại giữa
> 2 đích. Cùng kiểu chốt chặn với `Enemy.MoveToPlayer()` và `IsDashing` ở mục 6.3.

> **`spinSpeed` mặc định = 0**: prefab vật phẩm thường có Animator, mà clip animation có thể keyed Rotation và
> sẽ ghi đè góc xoay do code set (cùng cái bẫy với hiệu ứng cảnh báo của `USBEnemy` ở mục 6.3). Chỉ bật khi chắc
> clip không đụng tới Rotation.

**Chỗ đáp phải TRÁNH VẬT CẢN** (`Enemy.GetRandomDropPosition()`): rơi vào trong đá là vật phẩm coi như mất trắng —
Player không đi tới được, chỉ nhặt được nếu tình cờ đã có lõi Magnet đủ xa. Hàm thử tối đa 10 chỗ ngẫu nhiên, chỗ
nào `OverlapCircle` không dính `itemDropObstacleMask` thì lấy; thử hết vẫn không được thì **rơi ngay dưới chân
quái** — chỗ đó chắc chắn đi tới được vì con quái vừa đứng ở đấy.

> Layer vật cản cấu hình ở **`GameManager.itemDropObstacleMask`** (1 chỗ mỗi Scene, không phải trên từng prefab
> quái — để trên prefab thì thêm loại quái mới lại phải nhớ set, quên là loại đó lặng lẽ rơi đồ vào đá).
> **Để trống thì tự mượn lại `blinkObstacleMask` của Player**, vì đó cũng chính là danh sách Layer vật cản của
> Scene — khỏi phải khai cùng một thứ ở 2 nơi. Cả 2 đều trống thì bỏ qua kiểm tra, hành vi y như trước.

[`PlayerCollision.cs`](Assets/Scripts/Player/PlayerCollision.cs) xử lý toàn bộ va chạm nhặt đồ theo Tag:
`EnemyBullet` (-10hp), `Energy` (+1 energy, nếu đang gọi Boss thì +3 XP luôn — tương đương 1.5 viên EXP nhỏ),
`Heart` (heal), `USB` (cộng tài nguyên USB + báo NPC), `ExpSmall` (+2 XP), `ExpBig` (+5 XP), `ExpBoss` (+50 XP +
hút hết EXP còn lại về phía Player), `Coin`/`Diamond` (tiền tệ — xem 8.1), `Magnet` / `Chest` (xem ngay dưới).

**Magnet & Chest** — 2 vật phẩm này **KHÔNG có script riêng**, chỉ là 2 nhánh Tag trong `PlayerCollision` dùng
chung `CurrencyPickup`. Muốn đổi giá trị thì chỉnh `currencyType`/`amount` ngay trên prefab, không đụng code.

| Tag | Hành vi |
|---|---|
| `Magnet` | `CurrencyPickup.Collect()` + `PullOrbsWithTag("Coin")` — hút CƯỠNG BỨC mọi Coin trên bản đồ về Player, bất kể lõi Magnet có hay không |
| `Chest` | Chỉ `CurrencyPickup.Collect()` — thực chất là 1 túi tiền lớn, khác Coin thường đúng ở chỗ `amount` để cao |

> **Vật phẩm Magnet chỉ hút COIN**, không hút EXP/Energy/Heart/Kim cương. Muốn hút thêm loại nào thì thêm 1 dòng
> `PullOrbsWithTag("<Tag>")`. (Khác hẳn `ExpBoss`, vốn gọi `PullAllExpOrbsToPlayer()` để hút cả 3 loại EXP.)

> **`PullOrbsWithTag()` chỉ hút được object có gắn `Pickup.cs`** — nó tìm `GetComponent<Pickup>()` rồi bỏ qua nếu
> null. Prefab `Coin` hiện có ĐỦ cả `Pickup` lẫn `CurrencyPickup` nên chạy đúng; nhưng nếu sau này thêm loại vật
> phẩm mới mà quên gắn `Pickup`, vật phẩm Magnet sẽ **im lặng không hút được nó** mà không báo lỗi gì.

> **CẢNH BÁO — 2 Tag này chưa được khai báo trong `ProjectSettings/TagManager.asset`** (danh sách hiện có dừng ở
> `Coin`, `Diamond`). `CompareTag()` với Tag chưa khai báo **ném `UnityException`**, mà 2 nhánh này nằm CUỐI chuỗi
> `else if` nên mọi va chạm không khớp các Tag phía trên (VD chạm Enemy) đều rơi xuống đó và ném lỗi. Nếu Unity
> đang mở mà chưa Save Project thì file trên đĩa chỉ là bản cũ — kiểm tra lại
> **Edit → Project Settings → Tags and Layers** xem đã có `Magnet` và `Chest` chưa.

### 8.1 Tiền tệ: Coin & Kim cương (Diamond)

Hai đơn vị tiền tệ **lưu vĩnh viễn qua PlayerPrefs** (`Mayhem_Coin`, `Mayhem_Diamond`), dùng để mua mở khóa
nhân vật ở màn Character Select. Vật phẩm rơi ra mang script [CurrencyPickup.cs](Assets/Scripts/Player/CurrencyPickup.cs)
(`currencyType` Coin/Diamond + `amount`); gắn thêm `Pickup.cs` nếu muốn nó bị hút theo lõi Magnet.

- **Coin**: MỌI Enemy thường rơi đúng 1 coin khi chết. Prefab coin gán vào field `coinObject` trên từng Enemy
  prefab; drop được gọi trong `Enemy.Die()` (**KHÔNG** trong `DropItems()`, vì các subclass override trọn vẹn
  `DropItems()` sẽ làm mất coin). Để trống `coinObject` = loại quái đó không rơi coin.
- **Kim cương**: chỉ Boss rơi (Boss override `Die()` không gọi `base.Die()` nên không dính coin). Điều kiện rơi
  nằm ở `GameManager.ShouldDropDiamond()`, phải thỏa **CẢ 3**:
  1. `IsFirstBossKill()` — phase Boss ĐẦU TIÊN của ván (`bossPhaseCount == 0`). **Cố ý rơi ở phase đầu chứ không phải
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

## 8.3 NPC & hệ thống nhiệm vụ trong màn chơi

[NPC.cs](Assets/Scripts/NPC/NPC.cs) (singleton `NPC.Instance`) + [NPCDialogueUI.cs](Assets/Scripts/UI/NPCDialogueUI.cs)
+ [QuestNotificationUI.cs](Assets/Scripts/UI/QuestNotificationUI.cs).

Máy trạng thái `NPCQuestState`: `Idle` → `QuestAccepted` → `QuestReadyToComplete` → `Companion`.
- **Idle/QuestAccepted/QuestReadyToComplete**: mỗi frame đo khoảng cách tới Player, trong `interactionRadius` thì
  bật `interactionButtonObj` (nút world-space gắn trên chính NPC).
- Bấm nút → `OnInteract()` → `NPCDialogueUI.ShowDialogue(tên, các trang, nhãn nút hành động, callback, ảnh)`.
  **Cửa sổ hội thoại DÙNG CHUNG cho mọi NPC**, nên ảnh chân dung phải do từng NPC truyền vào (`InteractableNPC.portrait`)
  chứ KHÔNG gán cứng sprite lên ô `Image` trong Editor — gán cứng thì mọi NPC đều hiện chung một mặt. Tham số
  `portrait` để cuối và có giá trị mặc định `null`, truyền `null` thì ô ảnh tự ẩn hẳn (để nguyên object mà chỉ xóa
  sprite sẽ ra một ô trắng đặc, hoặc tệ hơn là còn nguyên mặt NPC vừa nói chuyện lần trước).
  Hội thoại **dừng game bằng `Time.timeScale = 0`** rồi fade in bằng `Time.unscaledDeltaTime` — cùng cơ chế với
  màn chọn augment. Nút `Next` hiện ở mọi trang trừ trang cuối; trang cuối thay bằng nút hành động
  (`Accept`/`Complete`). Nút `Exit` đóng hội thoại và trả `timeScale` về 1.
- Nhặt USB → `PlayerCollision` gọi `NPC.Instance.OnUSBCollected()`; đủ `usbRequired` thì chuyển sang
  `QuestReadyToComplete` + `QuestNotificationUI.ShowNotification()` (banner góc màn hình, tự ẩn sau `displayDuration`).
  Việc kiểm tra nằm ở `CheckQuestProgress()` và được gọi từ **3 chỗ**: lúc nhặt USB, lúc vừa nhận nhiệm vụ
  (`AcceptQuest`), và mỗi lần bắt đầu nói chuyện (`OnInteract`) — lý do ở ghi chú bên dưới.
- **Companion**: ẩn nút tương tác, tắt Collider để không cản Player, bám theo Player (giữ `followDistance`) và tự
  bắn con Enemy gần nhất mỗi `shootCooldown` (2s) với `Player.GetCurrentDamage() * damageMultiplier` (0.8).
  Animation chạy/đứng qua bool **`isRun`** — cùng tên tham số với Animator của Player.

> **Điều kiện nhiệm vụ phải kiểm tra lại LÚC CẦN DÙNG, không chỉ lúc có sự kiện.** USB là tài nguyên dùng chung
> của ván nên số lượng thay đổi độc lập với NPC: người chơi có thể gom đủ 3 viên TRƯỚC khi gặp NPC lần đầu (nhận
> nhiệm vụ xong không có sự kiện nhặt nào bắn ra nữa → kẹt ở `QuestAccepted`, NPC đòi "thêm 0 viên", phải nhặt dư
> 1 viên mới thoát), hoặc tiêu USB vào việc khác sau khi đã đủ (trạng thái `QuestReadyToComplete` thành lỗi thời).
> Vì vậy `OnInteract()` luôn đồng bộ lại 2 chiều với số USB thực tế trước khi chọn hội thoại. Áp dụng cho mọi
> nhiệm vụ/điều kiện dựa trên tài nguyên tiêu hao sau này.

> **Hysteresis khi bám theo**: dùng 2 ngưỡng khác nhau cho lúc bắt đầu đi (`dist > followDistance`) và lúc dừng
> (`dist <= followDistance - followStopBuffer`). Chỉ dùng 1 ngưỡng thì khi Player đi chậm hơn NPC, khoảng cách
> liên tục nhảy qua lại quanh mốc đó → NPC đi-dừng mỗi frame và animation nháy idle/run. Mọi cơ chế "đuổi theo rồi
> giữ khoảng cách" sau này đều cần kiểu 2 ngưỡng này.

**USB giờ là vật phẩm nhiệm vụ + tiền tệ trao đổi**, không còn là mốc tiến trình phá đảo (mục 6.1) và cũng không
nên rơi từ Boss nữa — bỏ trống `usbPrefabs` trên prefab Boss là xong, không cần sửa code. Loại Enemy rơi USB là
`USBEnemy` (mục 6.3).

---

## 8.4 NPCComputer & hệ thống mã độc (Malware)

Trạm dừng nghỉ trong màn chơi: nói vài câu rồi mở màn hình mua "mã độc" — vật phẩm tăng sức mạnh cho Player hoặc
gây hại Enemy, **có hiệu lực trong một khoảng thời gian** rồi hết.

**Tài nguyên thanh toán là của VÁN HIỆN TẠI, không phải tổng đã lưu**: `GameManager.CollectedUsb` (USB) và
`GameManager.RunCoin` (coin nhặt trong ván). Tiêu coin ở đây thì cuối ván `CommitRunCurrency()` cộng vào tổng ít
đi đúng bấy nhiêu — đó chính là đánh đổi cố ý: mạnh ngay trong ván, hay để dành mua chỉ số vĩnh viễn ở Shop (8.2).

> Giao dịch dùng `TrySpendUsb()`/`TrySpendRunCoin()` — **trả về `false` và KHÔNG trừ gì khi thiếu**, khác hẳn
> `SpendUsb()` cũ vốn kẹp về 0 (dùng cho nhiệm vụ NPC, nơi đã chắc chắn đủ). Dùng nhầm `SpendUsb()` cho mua bán
> nghĩa là người chơi trả thiếu vẫn nhận được hàng.

[MalwareData.cs](Assets/Scripts/Managers/MalwareData.cs) — ScriptableObject (`Create → Mayhem → Malware Data`),
mỗi món 1 asset. **Giá `<= 0` = KHÔNG mua được bằng loại tài nguyên đó** và nút thanh toán tương ứng tự ẩn — đúng
quy ước `CharacterData.coinPrice`/`diamondPrice`, nhờ vậy 3 trường hợp "chỉ USB" / "chỉ Coin" / "cả hai" diễn đạt
được mà không cần thêm enum nào.

| `MalwareEffectType` | Hiệu ứng | Tham số |
|---|---|---|
| `SlowAura` | Làm chậm mọi Enemy trong `radius` quanh Player | `slowPercent` (0.4 = giảm 40%) |
| `DotAura` | Sát thương theo tick cho mọi Enemy trong `radius` quanh Player | `tickInterval`, `damagePercentPerTick` (0.2 = 20% sát thương Player) |

[MalwareManager.cs](Assets/Scripts/Managers/MalwareManager.cs) (singleton, đặt 1 cái trong mỗi Scene Level) giữ
danh sách mã độc đang chạy và đếm ngược. **Vùng hiệu lực BÁM THEO NHÂN VẬT** (khác `PotionZone` là vùng đứng yên
tại chỗ ném), nên KHÔNG dùng Collider + `OnTriggerEnter/Exit` mà quét lại `Physics2D.OverlapCircleAll` quanh
Player mỗi `scanInterval` (0.1s — quét mỗi frame là thừa).

> **Slow phải ĐỐI CHIẾU danh sách, không được cứ thấy trong vùng là `ApplySlow()`.** `Enemy.ApplySlow()`/
> `RemoveSlow()` đếm theo stack (mục 7), áp lại mỗi lần quét sẽ làm stack phình vô hạn và con quái không bao giờ
> hết chậm. Mỗi lần quét: con mới vào vùng → `ApplySlow` + thêm vào list; con đã ra khỏi vùng → `RemoveSlow` +
> bỏ khỏi list.
>
> Con đã chết và về Pool thì **chỉ bỏ khỏi list, KHÔNG gọi `RemoveSlow()`** — `Enemy.OnEnable()` đã tự reset
> `slowStackCount = 0`, gọi thêm chỉ trừ nhầm stack của kiếp sau.

Mua lại đúng mã độc đang chạy = **làm mới thời gian**, không chồng thêm bản thứ 2 (slow không cộng dồn, mà chồng
2 bản còn làm thanh đếm ngược trên HUD bị nhân đôi).

**UI**: [MalwareShopUI.cs](Assets/Scripts/UI/MalwareShopUI.cs) (dừng game `timeScale = 0`, các ô hàng dựng sẵn
dùng chung — nạp data theo `stock`, y hệt pattern 2 pager ở mục 8.1) +
[MalwareShopItemButton.cs](Assets/Scripts/UI/MalwareShopItemButton.cs) (2 nút thanh toán riêng USB/Coin) +
[MalwareTimerUI.cs](Assets/Scripts/UI/MalwareTimerUI.cs) (thanh đếm ngược trên HUD).

**Danh sách hàng bán nằm trên `NPCComputer.stock`, KHÔNG nằm trong `MalwareShopUI`** — nhờ vậy đặt được nhiều
trạm bán các món khác nhau (hoặc mỗi Level bán một kiểu) mà vẫn dùng chung đúng 1 màn hình shop. Thêm món mới =
thêm asset vào mảng đó, không sửa code.

### Base class `InteractableNPC`

[InteractableNPC.cs](Assets/Scripts/NPC/InteractableNPC.cs) giữ phần dùng chung của MỌI NPC đứng trong màn:
`interactionRadius`, `interactionButtonObj`, đo khoảng cách tới Player để hiện/ẩn nút, và nối `onClick`. Cả
`NPC` (nhiệm vụ) lẫn `NPCComputer` đều kế thừa nó và chỉ override `OnInteract()`. Cùng tinh thần với `Enemy.cs`.
**Thêm NPC mới thì kế thừa class này, đừng chép lại đoạn dò khoảng cách/nối nút** — riêng việc nối nút đã chứa
sẵn cái bẫy `GetComponentInChildren<Button>(true)` ở mục 13.

---

## 8.5 Minimap (la bàn chỉ hướng NPC)

[MinimapUI.cs](Assets/Scripts/UI/MinimapUI.cs) + [MinimapMarker.cs](Assets/Scripts/UI/MinimapMarker.cs).

Minimap tròn, Player LUÔN ở tâm, mỗi object mang `MinimapMarker` hiện thành 1 mũi tên chỉ về phía nó. Trong tầm
`worldRange` thì mũi tên đứng đúng vị trí tương đối; ra ngoài tầm thì **kẹp lại đúng trên viền nhưng giữ nguyên
hướng** — đó chính là thứ biến nó thành mũi tên chỉ đường thay vì cái mốc biến mất. Mũi tên xoay theo hướng NPC
kể cả khi đang ở trong tầm.

**Cố ý KHÔNG dùng camera phụ + Render Texture** để vẽ bản đồ thật: mục đích chỉ là tìm lại NPC, mà thêm 1 camera
render mỗi frame là cái giá quá đắt trên mobile so với vài phép tính vector.

**Đăng ký qua danh sách TĨNH trên `MinimapMarker`, không phải `MinimapUI.Register()`**: Unity không đảm bảo thứ
tự `Awake`/`OnEnable` giữa các MonoBehaviour nên marker rất dễ bật TRƯỚC khi `MinimapUI` tồn tại. Danh sách tĩnh
không phụ thuộc thứ tự, và tự đúng với object tái sử dụng qua Pool (`OnEnable`/`OnDisable` chạy mỗi lần). Muốn
thêm loại mốc mới (Boss, rương đồ, USBEnemy…) chỉ cần gắn `MinimapMarker` + chọn màu, không sửa code.

> **Biến `static` KHÔNG tự reset khi bấm Play** nếu project bật "Enter Play Mode Options" (tắt Domain Reload) —
> lần Play thứ 2 trở đi danh sách còn sót marker của lần chạy trước và minimap hiện mốc ma. Vì vậy
> `MinimapMarker` có `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` để tự dọn. **Mọi collection
> `static` thêm sau này đều cần bước này.**

Hai chỗ dễ sai khi dựng trong Editor, đã chặn sẵn bằng code:
- **Neo/pivot của mũi tên bị ép về giữa** (`anchorMin/anchorMax/pivot = 0.5`) ngay lúc Instantiate —
  `anchoredPosition` chỉ mang nghĩa "lệch so với tâm" khi neo ở giữa, prefab neo ở góc là lệch hết sang một bên.
- **`iconPointsUp`**: tick nếu sprite mũi tên vẽ chĩa LÊN ở góc 0°, bỏ tick nếu chĩa sang PHẢI. Sai ô này là mọi
  mũi tên lệch đúng 90° (cùng họ với cái bẫy sprite cảnh báo của `USBEnemy` ở mục 6.3).

Chấm Player ở tâm chỉ là 1 Image đặt sẵn giữa khung trong Editor, không cần code.

---

## 8.6 Game juice — phản hồi khi ăn sát thương

Hai component rời, gắn vào là chạy, **KHÔNG viết riêng cho Player và Enemy**:

[DamageFlash.cs](Assets/Scripts/Effects/DamageFlash.cs) — nháy màu sprite. `Player.TakeDmg()` và
`Enemy.TakeDmg()` đều `GetComponent<DamageFlash>()` trong `Awake()` rồi gọi `Flash()`; không gắn component thì
không có gì xảy ra (`null` check), nên bật/tắt hiệu ứng cho từng loại quái chỉ bằng việc gắn hay không gắn.

Có 2 chế độ (`DamageFlashMode`):

| Mode | Cách hoạt động | Setup cần |
|---|---|---|
| `Tint` | Đổi `SpriteRenderer.color` | Không cần gì |
| `Brighten` | Đổi `_FlashAmount` của shader | SpriteRenderer phải dùng material shader `Mayhem/SpriteFlash` |

> **`SpriteRenderer.color` là màu NHÂN** nên chế độ `Tint` chỉ làm TỐI đi hoặc ngả màu được (đỏ/cam), **không
> bao giờ làm SÁNG lên** — để trắng (1,1,1) là không thấy gì. Muốn nháy trắng xóa thì bắt buộc dùng `Brighten`.

**Shader [SpriteFlash.shader](Assets/Shaders/SpriteFlash.shader)** là bản sao của URP
`Sprite-Lit-Default`, chỉ thêm `_FlashColor`/`_FlashAmount` và 1 dòng `lerp` ở fragment. Chép từ bản **Lit** chứ
không phải Unlit vì enemy trong project đang dùng `Sprite-Lit-Default` + Scene có Global Light 2D — dùng unlit
sẽ làm quái mất sáng tối VĨNH VIỄN chứ không chỉ lúc nháy.

> Khi sửa shader này: (a) giữ nguyên `UnityFlipSprite(...)` ở cả 3 pass — đó là chỗ xử lý `flipX` của
> SpriteRenderer, bỏ đi là sprite lật ngược; (b) `CBUFFER UnityPerMaterial` phải **giống hệt nhau ở cả 3 pass**,
> lệch là SRP Batcher bỏ qua shader; (c) chỉ đổi `rgb`, giữ nguyên `alpha` — đổi alpha là lúc nháy sẽ thấy cả
> khối chữ nhật của texture thay vì hình con quái.

> **Chế độ `Brighten` dùng `MaterialPropertyBlock`, TUYỆT ĐỐI không đụng vào `renderer.material`.** Chỉ cần ĐỌC
> `.material` là Unity nhân bản material cho riêng renderer đó — mỗi con quái một bản sao, vừa rò rỉ bộ nhớ theo
> số lần spawn từ Pool vừa phá batching. `MaterialPropertyBlock` đổi được giá trị cho từng renderer mà vẫn dùng
> chung đúng 1 material. Luôn `GetPropertyBlock()` trước khi sửa, nếu không sẽ xóa sạch property khác đã set.

> **Màu gốc chụp đúng 1 LẦN ở `Awake()`, TUYỆT ĐỐI không chụp lại ở đầu mỗi `Flash()`**: trúng 2 phát đạn liền
> nhau thì lần thứ 2 sẽ chụp nhầm màu ĐANG nháy làm "màu gốc" và sprite kẹt màu đỏ vĩnh viễn. Cùng nguyên tắc
> với `baseMaxHP`/`statsCaptured` của `Enemy` (mục 4).

> **Đếm giờ trong `Update()` chứ KHÔNG dùng Coroutine**: quái chết giữa lúc đang nháy sẽ bị trả về Pool →
> `SetActive(false)` → coroutine bị giết ngang, màu không kịp trả lại, con quái đó lần sau spawn ra vẫn đỏ lòm.
> `OnEnable`/`OnDisable` cũng khôi phục màu để bịt nốt kẽ hở.

[HealthBarJuice.cs](Assets/Scripts/UI/HealthBarJuice.cs) — vệt "máu vừa mất" trôi chậm phía sau + nháy màu
thanh. **Tự quan sát `fillAmount` của `mainFill` mỗi frame** thay vì bắt `Player`/`Enemy` gọi vào: nhờ vậy
KHÔNG phải sửa `UpdateHPBar()` của bên nào, và gắn được lên bất kỳ thanh `Filled` nào trong project (máu Player,
máu Enemy, sau này là khiên/stamina) mà bên kia không cần biết component này tồn tại. Chỉ phản ứng khi fill
GIẢM; tăng (hồi máu) thì kéo vệt lên theo ngay.

**Âm thanh — LUÔN dùng `AudioManager.PlaySFXThrottled()` cho tiếng trúng đòn**, không dùng `PlaySFX()` thường:

> 1 viên đạn nổ lan của Mage, 1 phát Laser xuyên thấu, hay 1 tick DOT aura (mục 8.4) có thể gây damage cho hàng
> chục con quái trong **CÙNG 1 FRAME** → bấy nhiêu lần `PlayOneShot` chồng lên nhau, nghe như tiếng rè và âm
> lượng bị đội lên gấp mấy chục lần. Hàm này đếm riêng theo TỪNG clip (để tiếng Player ăn đòn không bị tiếng
> trúng quái nuốt mất) và dùng `Time.unscaledTime` vì `Time.time` đứng yên khi `timeScale = 0`.

Clip đặt ở `Enemy.hitSound` (mỗi loại quái một tiếng) và `Player.hurtSound`.

**Thứ tự trong `TakeDmg()`**: nháy + kêu phải chạy **TRƯỚC** khi kiểm tra `currentHP <= 0`/`Die()`. Đặt sau thì
đòn kết liễu im re và không nháy gì — mà đó lại đúng là lúc cần phản hồi rõ nhất (với Enemy thì object còn đã bị
trả về Pool rồi). Ở Player, đoạn juice nằm sau `isInvulnerable` nên lúc Xoay Kiếm bất tử sẽ không nháy — đúng ý
đồ: không nháy = không mất máu.

### Số sát thương bay lên & hiệu ứng hồi máu

[DamagePopup.cs](Assets/Scripts/Effects/DamagePopup.cs) (con số bay lên + mờ dần) +
[DamagePopupSpawner.cs](Assets/Scripts/Effects/DamagePopupSpawner.cs) (singleton giữ prefab, đặt 1 cái mỗi Scene
Level). Cả `Enemy.TakeDmg()` lẫn `Player.TakeDmg()` đều gọi, màu khác nhau để phân biệt ai đang ăn đòn.

> **Prefab phải dùng `TextMeshPro` (world space), KHÔNG phải `TextMeshProUGUI`** (loại nằm trong Canvas) — số
> phải ở world space mới bám đúng vị trí con quái. Code đọc qua `TMP_Text` (lớp cha) nên kiểu nào cũng biên dịch
> được, sai kiểu thì chỉ phát hiện lúc chạy.

> **2 mặc định SAI của TextMeshPro world-space, đã ép lại trong `DamagePopup.Awake()`** — cả 2 đều gây lỗi rất
> khó đoán nguyên nhân nên đừng gỡ ra:
> - **Căn lề**: object `TextMeshPro` 3D mặc định có RectTransform rộng **20 đơn vị** và căn **Top-Left**, nên chữ
>   vẽ ở mép trái khung → con số hiện lệch cả chục đơn vị sang trái so với chỗ spawn, trông y như tính sai vị trí
>   trong khi vị trí hoàn toàn đúng. Ép `alignment = Center` + `pivot = (0.5, 0.5)`.
> - **Color Gradient**: `TMP.color` là màu NHÂN với gradient đỉnh. Prefab bật `Color Gradient` (nhất là gradient
>   tối) sẽ nuốt sạch màu do code set → con số luôn ra **đen**. Ép `enableVertexGradient = false`.
>
> Còn 1 nguồn "luôn đen" nữa mà code không ép được: **Face Color của material font TMP**. `.color` cũng nhân với
> nó, nên material face đen thì mọi màu đều ra đen — phải sửa trong material.

> Script `DamagePopup` phải nằm ở **object GỐC** của prefab. Đặt nhầm vào object con thì `Setup()` không bao giờ
> chạy, con số giữ nguyên nội dung + màu gõ sẵn trong prefab và trông y như code set màu sai. Spawner có tìm
> thêm ở con và log cảnh báo nếu không thấy component nào.

> **Prefab giữ ở SPAWNER chứ không phải trên từng prefab quái**: để field trên mỗi Enemy thì thêm loại quái mới
> lại phải nhớ kéo prefab vào, quên là con đó im lặng không hiện số. Gom về 1 chỗ thì quên gán là KHÔNG con nào
> hiện số — sai là thấy ngay.

> Sát thương lẻ (tick DOT 20% của 10 dmg, hoặc 0.4) làm tròn về 0 trông như đánh hụt, nên popup luôn hiện tối
> thiểu **1** khi thực sự có gây damage.

**Hiệu ứng hồi máu** (`Player.healEffectPrefab`): spawn trong `Player.Heal()` và `RestoreFullHP()` nên phủ cả 3
nguồn hồi máu (hút máu, Heart, USB) mà không phải sửa `PlayerCollision`. Gắn làm con của Player để bám theo.

> **BẮT BUỘC có `healEffectMinInterval`**: hút máu kích hoạt theo MỖI viên đạn trúng quái, mà 1 phát nổ lan của
> Mage trúng cả đàn thì `Heal()` bị gọi hàng chục lần trong 1 frame. Cùng họ với `PlaySFXThrottled` ở trên.
>
> Lời gọi nằm TRONG khối `if (currentHP < maxHP)` nên máu đầy thì không hiện gì — không hồi được thì đừng báo
> là có. `RestoreFullHP()` còn kiểm tra `currentHP > 0` vì `ApplyCharacterData`/`ApplyShopUpgrades` cũng gọi nó
> lúc khởi tạo màn chơi, khi đó chưa có gì để "hồi".

### Đẩy lùi Enemy khi trúng đòn (knockback)

`Enemy` có 2 nạp chồng: **`TakeDmg(dmg)` = KHÔNG đẩy lùi**, **`TakeDmg(dmg, sourcePosition)` = có đẩy lùi**, hướng
tính từ nguồn sát thương ra. Chỉnh độ mạnh bằng `knockbackDistance` (0.15) / `knockbackDuration` (0.08) trên từng
prefab; **để `knockbackDistance = 0` là miễn nhiễm** — dùng cho Boss, không cần sửa code.

> **Tách 2 nạp chồng thay vì tự suy hướng từ vị trí Player** vì sát thương THEO TICK bắt buộc phải gọi bản không
> hướng: `PotionZone`, DOT aura của mã độc (1 phút = 120 tick) và Xoay Kiếm của Knight (~33 tick) mà đẩy lùi mỗi
> tick thì sẽ hất con quái ra khỏi bản đồ, biến mọi vùng DOT thành tường chắn. Đang gọi bản CÓ hướng: đạn thường
> + nổ lan (`PlayerBullet`), `Bomb`, `Explosion`, `MiniRobot`, Laser (từ tâm nhân vật), chém thường của Knight.

> **Đẩy lùi xử lý ở `LateUpdate()` CHỨ KHÔNG PHẢI `Update()`** — đây là điểm mấu chốt khiến nó chạy cho MỌI loại
> quái mà không phải sửa subclass nào: `RangedEnemy`/`USBEnemy`/`BossEnemy` đều override `Update()` và phần lớn
> KHÔNG gọi `base.Update()`, nên nhét vào `Update()` của base là mất tác dụng với đúng những con thú vị nhất.
> `LateUpdate` chạy sau mọi `Update` nên ghi đè lên bất kỳ kiểu di chuyển nào con đó vừa thực hiện.
>
> **Hệ quả**: subclass nào cần `LateUpdate()` riêng thì PHẢI khai báo `protected override` + gọi `base.LateUpdate()`
> (xem `USBEnemy`). Khai báo lại thành `private void LateUpdate()` sẽ CHE mất hàm của base — Unity chỉ gọi hàm ở
> lớp dẫn xuất nhất — và con quái đó âm thầm miễn nhiễm đẩy lùi mà không báo lỗi gì.

---

## 8.7 Panel giới thiệu nhân vật & điều hướng menu từ trong màn chơi

### Panel giới thiệu nhân vật

[CharacterInfoPanel.cs](Assets/Scripts/UI/CharacterInfoPanel.cs). Bấm vào **bất kỳ** nhân vật nào (khóa hay chưa)
đều mở panel giới thiệu: tên, Ranged/Melee, chỉ số, chiêu, lõi riêng. Mở ĐÈ LÊN màn chọn nhân vật.

Panel tự đổi mặt theo trạng thái khóa:
- **Đã mở khóa** → hiện nút Vào chơi, ẩn 2 nút thanh toán.
- **Chưa mở khóa** → ẩn nút Vào chơi, hiện nút trả bằng Coin / Kim cương **ngay tại panel này**.

Mua xong panel **KHÔNG đóng** mà vẽ lại thành trạng thái đã mở khóa, để bấm Vào chơi luôn.

> **Phải xem được info của nhân vật CHƯA mở khóa** — đó chính là thông tin để quyết định có mua hay không.
> Bắt mua trước rồi mới cho xem là ngược.

**Panel này gộp luôn vai trò của [CharacterUnlockPanel.cs](Assets/Scripts/UI/CharacterUnlockPanel.cs) cũ** (modal
mua riêng). Script cũ vẫn còn trong project và `CharacterButton` vẫn giữ ô `unlockPanel`, nhưng chỉ còn là nhánh
dự phòng khi chưa gán `Info Panel`. **`LoadScene` chỉ còn ở đúng 1 chỗ: nút Vào chơi của panel này.**

> **Listener của 4 nút được nối bằng code trong `Awake()`** (Vào chơi / Quay lại / trả Coin / trả Kim cương).
> ĐỪNG gán thêm hàm vào `OnClick` của chúng trong Inspector — mỗi cú bấm sẽ chạy 2 lần. `TryUnlock()` có guard
> `IsCharacterUnlocked` chặn mua lần 2 nên không mất tiền oan, nhưng guard đó là để chống bấm nhanh 2 nhịp trên
> mobile chứ không phải để dung túng việc nối listener đôi.

> **Chỉ số hiện ra là chỉ số GỐC của nhân vật, CỐ Ý không cộng bonus Shop**: đây là bảng so sánh giữa các nhân
> vật với nhau, mà bonus Shop áp cho mọi nhân vật như nhau nên cộng vào chỉ làm nhiễu phần khác biệt thật sự.
>
> `baseMaxHP`/`baseMoveSpeed` = 0 nghĩa là "giữ nguyên giá trị trên Player trong Scene" (mục 3), mà panel này
> không đọc được Scene Level — nên có 2 ô `fallbackMaxHP`/`fallbackMoveSpeed` **phải khai lại cho khớp Player
> trong Scene Level**, nếu không nhân vật đó hiện HP = 0.
>
> **`CharacterData.baseRegen` là field MỚI** thêm cùng panel này: trước đó regen hoàn toàn không phải chỉ số
> riêng của nhân vật (chỉ đến từ Shop + augment), nên panel sẽ hiện 0 cho tất cả. Nó là chỉ số THẬT — 
> `Player.ApplyCharacterData()` gọi `StartHealthRegen(baseRegen)`, và vì hàm đó CỘNG DỒN nên lượng hồi máu mua ở
> Shop (áp sau) vẫn cộng thêm chứ không bị ghi đè.

**Danh sách lõi tự sinh từ `CharacterData.exclusiveAugments`**, nhưng **CHỈ lấy các type nằm trong
`featuredCoreTypes`** (mặc định `Bomb`, `Potion`, `SwordSpin`, `MiniRobot`). `exclusiveAugments` còn chứa cả đống
augment chỉ nâng chỉ số (`Bullet`, `Reload`, `AttackSpeed`, `StaminaRegen`, `BlinkCooldown`…) — liệt kê hết thì
phần này dài lê thê và mất luôn ý nghĩa "lõi đặc biệt". Đúng 4 type đó cũng chính là nhóm augment "mở khóa lõi,
chỉ chọn 1 lần" ở mục 5.2. Thêm nhân vật mới có lõi riêng thì thêm type vào mảng đó trong Inspector, không sửa code.

**Chiêu đặc biệt**: `CharacterData.abilityDescription` (tự viết) luôn thắng. Để trống thì panel tự sinh mô tả cho
4 nhân vật hiện có — Gunner `Dash`, Mage `Blink`, Knight **Khiên** (chống chịu + tăng tốc), Robot **Laser**
(bắn thường tích năng lượng).

> Phần tự sinh KHÔNG thể chỉ dựa vào `abilityType`: nó chỉ có `Dash`/`Blink`/`None`, mà **Knight lẫn Robot đều
> rơi vào `None`** dù chiêu của 2 đứa hoàn toàn khác nhau. Panel phải nhìn thêm `combatType == Melee` (Knight) và
> `usesAmmo == false` (Robot) — đúng những field mà `Player.cs`/`Gun.cs` cũng đang dùng để phân biệt chúng.
> Nhân vật thứ 5 không khớp 4 khuôn này thì **phải** điền `abilityDescription`.

### Điều hướng từ Pause / Thua / Thắng

Scene Level và Scene MainMenu là 2 Scene khác nhau nên không gọi thẳng hàm của nhau được. Các nút đặt cờ
`GameProgress.PendingPanel` (+ `AdvanceToNextStage`) rồi `LoadScene`, `MainMenuUI.Start()` đọc cờ và mở đúng
panel. Cờ được **xóa ngay sau khi dùng** (`ConsumePendingPanel`) để lần mở game sau không bị nhảy panel bất ngờ.

| Màn hình | Nút | Hàm trên `GameManager` |
|---|---|---|
| Pause | Chơi lại | `ShowRestartConfirm()` → modal → `RestartLevel()` |
| Pause | Thoát | `ShowExitConfirm()` → modal → `BackToMainMenu()` |
| Thua | Chơi lại / Chọn nhân vật / Chọn Level | `RestartLevel()` / `BackToCharacterSelect()` / `BackToStageSelect()` |
| Thắng | Chơi lại / Màn tiếp theo / Chọn Level | `RestartLevel()` / `NextLevel()` / `BackToStageSelect()` |

> **Nút Chơi lại ở Pause CŨNG phải đi qua modal cảnh báo**, không gọi thẳng `RestartLevel()`: chơi lại giữa chừng
> cũng là bỏ dở ván nên mất sạch coin/kim cương y hệt như thoát ra (mục 8.1). Modal `exitConfirmPanel` được dùng
> CHUNG cho cả 2 nút, `pendingConfirmAction` nhớ nút nào vừa bấm và `confirmMessageText` đổi nội dung theo.
> Ở màn Thua/Thắng thì gọi thẳng `RestartLevel()` vì `CommitRunCurrency()` đã chạy rồi.

> **`ConfirmExitToMainMenu()` giữ nguyên tên** dù nay xử lý cả 2 hành động — nó đã được nối sẵn vào nút Yes trong
> Editor, đổi tên là nút mất tham chiếu mà Unity không báo lỗi gì.

> **Việc tra ra Level kế tiếp làm ở `MainMenuUI`, KHÔNG phải trong Scene Level**: danh sách toàn bộ Level chỉ tồn
> tại ở `StageSelectPager.allStages` bên Scene MainMenu. `GetStageByIndex()` tra theo `stageIndex` chứ không theo
> vị trí trong mảng — mảng có thể bị xếp lộn hoặc thiếu một Level, lúc đó dùng vị trí sẽ nhảy sang nhầm màn mà
> không báo lỗi. Không tìm được Level kế tiếp (vừa phá đảo Level cuối) thì rơi về màn chọn Level, chứ KHÔNG mở
> màn chọn nhân vật với Stage cũ — làm vậy người chơi sẽ chơi lại đúng màn vừa thắng mà tưởng đang sang màn mới.

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

**Gotcha đã fix — CHỈ được gắn hiệu ứng làm con của object KHÔNG BAO GIỜ VÀO POOL**: Unity CẤM đổi parent của một
object trong lúc parent của nó đang được bật/tắt. Vì `ReturnObjectToPool()` luôn `SetParent(null)` (xem trên), nên
chuỗi "quái chết → trả quái về Pool → `SetActive(false)` → `OnDisable()` của quái → trả hiệu ứng-con về Pool" sẽ
ném `Cannot set the parent of the GameObject 'X' while activating or deactivating the parent GameObject 'Y'`.

> **Quy tắc**: gắn hiệu ứng làm con của **Player** thì an toàn (Player không bao giờ vào Pool). Gắn làm con của
> **Enemy/đạn/bất kỳ object pooled nào** thì KHÔNG — thay vào đó spawn ở world space rồi tự cập nhật vị trí trong
> `Update()` (xem `USBEnemy.SpawnWarning()` ở mục 6.3). Không có parent thì lúc chết chẳng có gì phải gỡ.

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
  MalwareData.cs            — ScriptableObject 1 món mã độc bán ở NPCComputer (mục 8.4)
  MalwareManager.cs         — Đếm ngược + chạy hiệu ứng các mã độc đang có hiệu lực (vùng bám theo Player)
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
  PotionZone.cs              — Vùng DoT + slow (của NGƯỜI CHƠI, gây damage cho Enemy)
  PoisonProjectile.cs         — Đạn cầu vồng tới điểm đã khóa + vệt cảnh báo điểm rơi (mục 6.4)
  PoisonZone.cs               — Vùng độc của ENEMY, gây damage cho Player theo tick (mục 6.4)
  EnemyBullet.cs             — Đạn của Enemy/Boss

Enemies/
  Enemy.cs                  — Base abstract: HP/move/collision/stage-difficulty dùng chung
  BasicEnemy/MiniEnemy/EnergyEnemy/HealEnemy/ExplosionEnemy/RangedEnemy.cs — Override tối thiểu theo hành vi riêng
  PoisonEnemy.cs              — Bắn đạn cầu vồng có cảnh báo điểm rơi, để lại vùng độc (mục 6.4)
  USBEnemy.cs                 — Rơi vật phẩm USB; spawn theo mốc 50 mạng (mục 6.3)
  EnemyDashSkill.cs           — Component chiêu Lướt dùng chung (USBEnemy tự kích hoạt, Boss gọi tay) — mục 6.3
  BossEnemy.cs                — Skill ngẫu nhiên, hồi sinh, Teleport có telegraph
  EnemySpawner.cs              — Spawn định kỳ + đếm mạng để spawn USBEnemy; tăng tốc spawn theo currentLevel (5, 10)
  HeartPickup.cs                — healValue cho vật phẩm Heart

Effects/
  Explosion.cs                 — Hiệu ứng nổ (dùng bởi Bomb, tự dọn)
  DamageFlash.cs               — Nháy màu/nháy sáng sprite khi ăn đòn, dùng chung Player/Enemy (mục 8.6)
  DamagePopup.cs               — Con số sát thương bay lên rồi mờ dần (mục 8.6)
  DamagePopupSpawner.cs        — Singleton giữ prefab con số + màu riêng cho Player/Enemy (mục 8.6)
  ItemDropMotion.cs            — Vật phẩm văng ra theo vòng cung khi quái chết (mục 8)

Shaders/
  SpriteFlash.shader           — Bản sao URP Sprite-Lit-Default + _FlashColor/_FlashAmount (mục 8.6)

NPC/
  InteractableNPC.cs           — Base abstract: dò khoảng cách Player + hiện/nối nút tương tác (dùng chung mọi NPC)
  NPC.cs                       — Máy trạng thái nhiệm vụ + chế độ đồng hành (bám theo, tự bắn) — xem mục 8.3
  NPCComputer.cs               — Trạm dừng nghỉ: chào hỏi rồi mở shop mã độc — xem mục 8.4

UI/
  GameUI.cs                    — Cập nhật text/HUD trong ván chơi (Update...Text gọi từ AugmentManager)
  MainMenuUI.cs                — Điều hướng panel Main Menu
  StageButton.cs / CharacterButton.cs — Nút chọn Level/Nhân vật (CharacterButton kiêm trạng thái khóa + giá)
  CharacterSelectPager.cs / StageSelectPager.cs — Chia trang nhân vật/Level, lật bằng 2 nút < >
                                  (các ô nút dùng chung, nạp data theo trang)
  CharacterUnlockPanel.cs       — Modal xác nhận mua nhân vật, chọn trả bằng Coin hoặc Kim cương
  CharacterInfoPanel.cs         — Panel giới thiệu nhân vật trước khi vào màn; nơi DUY NHẤT gọi LoadScene (mục 8.7)
  CurrencyUI.cs                 — Hiển thị số Coin/Kim cương, tự cập nhật qua event
  HealthBarJuice.cs             — Vệt máu vừa mất trôi chậm + nháy thanh; tự quan sát fillAmount (mục 8.6)
  ShopUpgradeButton.cs          — 1 dòng chỉ số trong Shop (cấp, giá, phần thưởng mức kế, nút Mua)
  CoinExchangeButton.cs         — Ô đổi Coin sang Kim cương
  DragAimButton.cs (abstract) → BombButton.cs / PotionButton.cs / BlinkButton.cs (chọn ĐIỂM)
                              → LaserButton.cs (chọn HƯỚNG, mũi tên xoay tại chỗ)
  NPCDialogueUI.cs              — Cửa sổ hội thoại NPC (dừng game, lật trang, nút hành động ở trang cuối)
  MalwareShopUI.cs              — Màn hình giao dịch mã độc (dừng game, ô hàng dựng sẵn nạp theo stock)
  MalwareShopItemButton.cs      — 1 ô hàng: icon/mô tả + 2 nút thanh toán USB/Coin, ẩn nút nếu giá <= 0
  MalwareTimerUI.cs             — Thanh đếm ngược thời gian còn lại của mã độc đang chạy (HUD)
  MinimapUI.cs                  — Minimap tròn kiểu la bàn: mũi tên chỉ hướng NPC, dính viền khi ngoài tầm (mục 8.5)
  MinimapMarker.cs              — Gắn vào object muốn hiện trên minimap (màu + icon riêng), tự ghi danh
  UIFollowWorldTarget.cs        — Kéo 1 phần tử UI bám theo vị trí world của 1 object (nút tương tác trên đầu NPC)
  QuestNotificationUI.cs        — Banner thông báo nhiệm vụ ở góc màn hình, tự ẩn
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
| `Cannot set the parent of the GameObject 'X' while activating or deactivating the parent GameObject 'Y'` khi Enemy chết giữa lúc đang có hiệu ứng gắn kèm | Unity CẤM đổi parent trong lúc parent đang bật/tắt. Enemy chết → về Pool → `SetActive(false)` → `OnDisable()` → trả hiệu ứng-con về Pool → `SetParent(null)` ngay giữa lúc parent đang tắt | ĐỪNG gắn hiệu ứng làm con của object pooled (Enemy/đạn). Spawn ở world space rồi tự đồng bộ vị trí trong `Update()`. Chỉ Player mới được làm parent vì không bao giờ vào Pool — xem mục 9 |
| Chọn Knight vẫn thấy súng/nút Bắn/Nạp đạn/Bomb/Potion hiện ra | `Gun.Start()` (nơi ẩn `bombButtonObj`/`potionButtonObj` mặc định) KHÔNG BAO GIỜ chạy nếu `Gun.enabled = false` được set TRƯỚC khi Unity gọi `Start()` | Chuyển toàn bộ logic ẩn/hiện vào `Gun.SetActive(bool)` (gọi trực tiếp từ `GameManager`), không phụ thuộc `Start()` |
| Augment ép buộc (Bomb cấp 8 / Burst-Split cấp 10) ra màn hình TRỐNG khi chơi Mage | Không kiểm tra augment đó có tồn tại trong pool của nhân vật hiện tại trước khi ép buộc | Luôn `augmentPool.Find(a => a.type == "...")` trước, `!= null` mới ép buộc, ngược lại rơi về random bình thường |
| Mana Regen tỉ lệ lẻ (VD 0.25/s) không cộng được gì | Cộng thẳng số thập phân vào `currentAmmo` (int) → làm tròn về 0 | Dùng `manaRegenAccumulator` (float) gom dần qua nhiều giây, chỉ cộng phần nguyên khi đủ ≥1 |
| Player bị giật lùi (knockback) ngay lúc bắn | `Rigidbody2D` của `PlayerBullet` để `Dynamic`, spawn đè lên Collider Player bị vật lý đẩy ra | Ép `rb.bodyType = Kinematic` trong `PlayerBullet.Awake()` |
| Aim Bomb/Potion/Blink xuất hiện dính tại nút bấm, khó kéo | Aim reticle bám theo vị trí tuyệt đối ngón tay / vị trí nút | `DragAimButton` tính `worldDelta` (độ lệch so lúc mới nhấn), cộng vào vị trí NHÂN VẬT, không phải vị trí nút |
| CS0163 "not all code paths return a value" khi tự thêm `case` mới vào switch trong `AugmentManager.ApplyEffect()` | Thiếu `break;` cuối case, rơi (fall-through) sang case kế tiếp | MỌI case (trừ case cuối switch) bắt buộc có `break;`/`return;` — kiểm tra kỹ khi tự sửa tay |
| Mua chỉ số Shop: UI lệch đúng 1 nhịp (bấm lần đầu không lên cấp), và khi full 5/5 phải thoát ra vào lại mới thấy "Full" | `OnCurrencyChanged` bắn ra NGAY LÚC trừ Coin, tức TRƯỚC khi `TryBuyUpgrade` ghi cấp mới → dòng đó vẽ lại bằng cấp cũ. Còn khi đã full thì hàm thoát sớm, không trừ Coin nên event KHÔNG bắn phát nào | Dòng vừa bấm phải tự gọi `Refresh()` sau `TryBuyUpgrade()`. **Quy tắc chung: đừng dùng event "tiền thay đổi" để vẽ lại thứ phụ thuộc trạng thái KHÁC ngoài tiền** — event có thể bắn giữa chừng giao dịch, hoặc không bắn khi giao dịch bị từ chối |
| Nút tương tác NPC hiện ra bình thường khi lại gần nhưng bấm KHÔNG có phản ứng gì | `GetComponentInChildren<T>()` mặc định **bỏ qua object đang tắt, kể cả chính object gốc**. `NPC.Awake()` gọi `SetActive(false)` TRƯỚC rồi mới tìm `Button` → luôn ra null → `onClick.AddListener` không bao giờ chạy | Truyền `GetComponentInChildren<Button>(true)` (và nên lấy component TRƯỚC khi tắt object). Áp dụng cho MỌI chỗ tìm component trên UI ẩn sẵn |
| Đạn do NPC/script khác bắn tự biến mất giữa đường | Đạn lấy từ Pool còn giữ `maxRange` của lần bắn Burst Shot trước | Nơi nào spawn `PlayerBullet` cũng phải tự set lại `maxRange` (0 = không giới hạn), y như `Gun.SpawnBullet()` |
| `Coroutine couldn't be started because the game object 'X' is inactive!` khi mở panel | Coroutine chỉ chạy được khi **object gắn script** đang bật. Hai biến thể: (a) script nằm trên chính object nó tự `SetActive(false)` trong `Awake()`, mà lệnh bật lại nằm BÊN TRONG coroutine; (b) `panelRoot` trỏ tới object CON còn object cha gắn script mới là cái đang tắt — **bật con KHÔNG làm cha sống lại** | Trong hàm public, TRƯỚC `StartCoroutine`: bật **cả `gameObject` của script lẫn panel** (2 cái có thể khác nhau), set `Time.timeScale`, rồi mới `StartCoroutine` và chỉ để coroutine lo phần fade/đếm giờ. Thêm guard `if (!gameObject.activeInHierarchy)` để lỡ có object cha đang tắt thì bỏ hiệu ứng chứ không ném lỗi |
| Màn hình mở ra từ nút hành động của hội thoại NPC không dừng được game (`timeScale` tự về 1) | `NPCDialogueUI.OnAction()` gọi callback TRƯỚC rồi mới `CloseDialogue()` — mà hàm đó set `Time.timeScale = 1f`, ghi đè luôn giá trị 0 mà callback vừa set | Đóng hội thoại TRƯỚC, gọi callback SAU (giữ lại tham chiếu callback trước khi đóng). Quy tắc: hàm dọn dẹp khôi phục trạng thái toàn cục phải chạy TRƯỚC callback của người dùng, không phải sau |
| Gom đủ USB TRƯỚC khi nhận nhiệm vụ NPC → nhận xong quay lại NPC vẫn đòi "thêm 0 viên", phải nhặt dư 1 viên mới hoàn thành được | Điều kiện hoàn thành CHỈ được kiểm tra trong sự kiện nhặt USB. Đã đủ từ trước thì lúc nhận nhiệm vụ không có sự kiện nào bắn ra để kích hoạt kiểm tra | Tách ra `CheckQuestProgress()` và gọi thêm ở `AcceptQuest()` + `OnInteract()`. **Quy tắc: điều kiện dựa trên tài nguyên dùng chung phải kiểm tra lại LÚC CẦN DÙNG, đừng chỉ dựa vào sự kiện thay đổi tài nguyên** |
| Một loại Enemy cụ thể không bị đẩy lùi (hoặc không chạy logic nào đó của base) trong khi các loại khác vẫn bình thường, không có lỗi nào | Subclass khai báo `private void LateUpdate()` (hay `Update`/`OnEnable`) trùng tên với hàm của base → **CHE** mất hàm base, Unity chỉ gọi hàm ở lớp dẫn xuất nhất | Subclass phải `protected override` + gọi `base.<hàm>()`. Đây là lỗi im lặng hoàn toàn: C# chỉ cảnh báo, Unity không báo gì |
| Nút trong Canvas **World Space** hiện ra nhưng bấm không ăn, dù đã đủ Graphic Raycaster + Event Camera | Canvas **Screen Space - Overlay** (GameUI) LUÔN ăn raycast trước World Space. Chỉ cần 1 Image của GameUI phủ lên vùng đó với `Raycast Target` bật (kể cả trong suốt) là click không xuống tới nơi | Kiểm chứng: Play mode → tắt GameObject `GameUI` → bấm lại. Cách tránh hẳn: để nút TRONG GameUI rồi gắn [UIFollowWorldTarget.cs](Assets/Scripts/UI/UIFollowWorldTarget.cs) cho nó bám theo vị trí world của object |

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
3. **Thêm Level mới**: tạo Scene mới (KHÔNG dùng chung Scene) + `StageData` asset mới trỏ đúng `sceneName`,
   **thêm Scene vào Build Settings**, và thêm `StageData` vào mảng `allStages` của `StageSelectPager`
   (không phải dựng thêm nút — các ô nút dùng chung, xem mục 8.1).

   > **Quên bước Build Settings là lỗi im lặng.** `SceneManager.LoadScene()` chỉ load được Scene nằm trong
   > Build Settings; thiếu thì nó KHÔNG ném exception mà chỉ lặng lẽ không làm gì — nút "Vào chơi" bấm như
   > không bấm, và trong bản build thì không có Console để nhìn ra. Đã từng mất thời gian vì Level4/Level5 có
   > file Scene, có `StageData` đúng, nằm trong `allStages`, nhưng không được thêm vào Build Settings.
   > `CharacterInfoPanel` giờ kiểm tra `Application.CanStreamedLevelBeLoaded()` trước và log lỗi chỉ đúng chỗ.
4. **Thêm loại Enemy mới**: kế thừa `Enemy.cs`, chỉ override đúng phần khác biệt (theo pattern các subclass hiện
   có), đừng copy lại logic va chạm/damage-stay đã có sẵn ở base.
5. **Thêm hiệu ứng animation cho hành động mới**: theo đúng pattern hiện tại — spawn prefab sprite/animation riêng
   (`SpawnEffect`/`ObjectPoolManager.SpawnObject`), KHÔNG bake vào Animator Controller của nhân vật (người dùng tự
   quản lý animation theo cách này, đã xác nhận rõ ràng).
6. Trước khi sửa 1 file lớn (`Player.cs`, `Gun.cs`, `KnightCombat.cs`, `AugmentManager.cs`) — **luôn Read lại toàn
   bộ file trước** vì người dùng có thể đã tự sửa tay trực tiếp trong Unity/editor giữa các lượt làm việc.
7. Không có `gh` CLI trên máy này — khi cần tạo PR, dùng `git push` rồi đưa link tạo PR thủ công dạng
   `https://github.com/HxyBean/Mayhem/compare/main...<branch>?expand=1`.
