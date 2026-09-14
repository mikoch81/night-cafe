# Night Café — handoff dla Claude Code (VS Code)

> Wklej ten plik do repo jako `docs/CLAUDE_CODE_HANDOFF.md` i podaj Claude Code
> w pierwszym prompcie: „Przeczytaj docs/CLAUDE_CODE_HANDOFF.md i docs/GDD.md, potem wykonaj Milestone 1".

## Kontekst

Tworzymy grę mobilną **Night Café** (Unity, Android, landscape) — opis i wszystkie liczby w `docs/GDD.md` (źródło prawdy; nie zmieniaj wartości bez pytania). Assety PNG + źródła SVG są w paczce `night-cafe-assets.zip` (struktura `Art/` i `Source/` + `MANIFEST.txt`).

**Ważne — pochodzenie projektu:** gra inspiruje się mechaniką klasycznych handheldów LCD, ale NIE może zawierać żadnych elementów gry „Nu, pogodi!" 1984 (postacie, grafika, nazwa, dźwięki). W `C:\UnityProjects\WolfEggs\` (stary komputer z Windows; jeśli niedostępny — pomiń) jest poprzedni prototyp: wolno zaglądać TYLKO po rozwiązania techniczne (input, pooling, build settings). Nie kopiuj z niego grafiki, nazw obiektów ani układu ekranu.

## Środowisko

**Od 2026-09-14 projekt żyje na PC z Windows 11: `C:\NIGHT\night-cafe`** (wcześniej Linux,
`/home/mkej/Projekty/NuPogodiModern/`; stąd shebangi `#!/usr/bin/env python3` w `tools/`).

- Unity **6000.5.5f1** (`C:\Program Files\Unity\Hub\Editor\6000.5.5f1`, moduł Android z OpenJDK/SDK/NDK).
  Nie otwieraj projektu innymi wersjami z Huba (są 6000.0 i 6000.4).
- CLI `unity` (1.0.0-beta.6, pakiet `com.unity.pipeline`) rozmawia z otwartym edytorem: `unity status`,
  `unity cmd console_status`, `unity cmd run_tests editor` (tryb pozycyjnie: editor|playmode|all),
  `unity cmd menu "NightCafe/Build Scene Setup"`, `unity cmd menu "NightCafe/Build Android APK"`,
  `unity cmd capture_game_view --camera LcdCamera --save_path Temp/x.png` (zapisuje pod `Assets/Temp/` —
  usuń przed commitem), `unity cmd eval --code '...'` (np. wymuszony reimport FBX).
- Python: `py -3.12` (samo `python` to zaślepka ze Sklepu Windows). Blender 5.2 przez winget:
  `"C:\Program Files\Blender Foundation\Blender 5.2\blender.exe" -b -P tools/shell_model.py`.
  Inkscape jeszcze nie zainstalowany (potrzebny do `gen_art*.py`).
- Telefon Android po USB — `adb` z `C:\Tools\Sdk\platform-tools`: `adb devices`,
  `adb install -r build/NightCafe.apk`. Debug keystore jest inny niż na Linuksie — pierwsza instalacja
  z nowego komputera wymaga `adb uninstall com.mikoch81.nightcafe`.
- Git: driver `unityyamlmerge` wskazuje na `UnityYAMLMerge.exe` z 6000.5.5f1 (ustawione w `git config`).
- GitHub: konto **mikoch81**. Utwórz repo `gh repo create mikoch81/night-cafe --private --source . --push` po pierwszym commicie. `.gitignore` = standardowy Unity (Library/, Temp/, Logs/, obj/, Build/, UserSettings/). Git LFS dla `*.png` w `Assets/Art/`.

## Struktura projektu (docelowa)

```
NuPogodiModern/
├── docs/                      # GDD.md, ten plik, art-direction
├── Assets/
│   ├── Art/                   # PNG z paczki (sprites/, screen/, device/)
│   ├── Art/Source/            # SVG źródłowe (poza buildem: label "EditorOnly" zbędny, po prostu nie referencuj)
│   ├── Audio/                 # SFX (do wygenerowania: jsfxr/CC0)
│   ├── Fonts/                 # font mono do TMP (np. JetBrains Mono OFL)
│   ├── Prefabs/
│   ├── Scenes/                # Boot.unity, Game.unity
│   ├── Scripts/
│   └── Settings/              # URP asset, SpriteAtlas
└── ProjectSettings/
```

## Architektura skryptów (Assets/Scripts/)

| Skrypt | Odpowiedzialność |
|---|---|
| `GameLoopController` | maszyna stanów: Title → Playing → Breather → GameOver; attract-mode na Title |
| `LaneConfig` (SO) | 4 tory × 5 punktów kroków (Vector2), zgodne z `screen_bg.png` |
| `LaneSpawner` | interwały/wagi/limity z GDD §2.3; object pooling kubków |
| `CupController` | tween K1→K5, kolor (Tryb B), zdarzenia dotarcia do K5 |
| `PlayerPositionController` | 4 pozycje, input (4 strefy dotyku + klawiatura WSAD/strzałki w edytorze), flip X dla pozycji lewych |
| `ScoreService` | punkty, combo 25→+5, rollover 999, progi litości kota (200/500), oddech co 100 |
| `PenaltyService` | plamy 0–3, animacja kota, game over |
| `TempoService` | poziom T z liczby złapań, `stepTime` wg wzoru GDD §2.2 |
| `ModeConfig` (SO) | parametry Trybu A i B |
| `OrderService` | Tryb B: kolor zamówienia, rotacja co 10 złapań / 20s |
| `AudioService` | SFX + lo-fi loop, miksowanie −18 LUFS, mute |
| `HapticsService` | wibracje 15/60/3×80 ms, toggle |
| `SaveService` | highscore per tryb + ustawienia, JSON w persistentDataPath |
| `DeviceShellView` | podświetlanie wirtualnych przycisków, wajcha A/B, skiny |
| `ClockWidget` | zegar rzeczywisty + minutnik parzenia (Title) |

## Milestones i kryteria akceptacji

**M1 — grywalny prototyp (bez grafiki finalnej):**
- Scena Game: 4 tory z placeholderami, spawner, tempo, złapania/pudła, 3 plamy = game over, restart. Działa w edytorze (klawiatura) i na telefonie (dotyk). FPS ≥ 60 na telefonie.

**M2 — wygląd i czucie:**
- Assety z paczki, warstwy wg GDD §5.1, bloom, haptyka, SFX (wygeneruj jsfxr; nie pobieraj plików o niejasnej licencji), licznik TMP, ekran tytułowy z prawdziwym zegarem.

**M3 — Tryb B + meta:**
- Zamówienia kolorami, highscore, attract-mode, duchy segmentów (opcja), skiny (jeśli czas).

**M3.5 — hardening (po przeglądzie kodu, 2026-09-12):** ✅
- Bloom faktycznie w buildzie (`sharedProfile` + sub-asset), ASTC na Androida, bilinear, jeden event wejścia + mysz w edytorze, lockout po game over, timing kubków/spawnera niezależny od FPS, atomowy zapis profilu, `ModeConfigAssetsTests` pilnuje zgodności assetów z GDD, `PaletteConfig`, wejście Android = klasyczny `Activity`, `RoundStateMachine` z testami.

**M4 — Art v2 „Neo-LCD dopracowany":** (w toku, stan 2026-09-13 niżej)
- Kierunek: GDD §5.2 zostaje (jednokolorowe bursztynowe segmenty), ale lepsze sylwetki i prawdziwy look LCD. Tylko darmowe narzędzia; Claude generuje (`tools/gen_art.py` → SVG → PNG @4x przez Inkscape CLI), Michał ocenia arkusze porównawcze.
- Zakres: arkusz kierunku (3 warianty baristy/kota), font 14-segmentowy DSEG (OFL) do licznika i zegara, shader `LcdSegment` (Shader Graph: glow, ghosting, szkło), `SpriteAtlas`, obudowa v2 (tekstura drewna, ramka), pochylenie kubka ±14°, oddech (barista wyciera ręce, neon mruga), animacja 999 → kot z neonów, minutnik parzenia.
- Referencje AI wyłącznie lokalnie (ComfyUI + FLUX/SDXL na RTX 4060) i tylko jako inspiracja / grafiki sklepowe — do gry idą wektory.

**M4.6 — Ekran v3 „malowany" (w toku, decyzje Michała 2026-09-13: miks stylów, diorama w oknie,
RETRO jako odblokowywany skin ekranu, GDD §5 przepisane):**
- Powód: przy realistycznym 3D urządzeniu imitacja starego LCD wygląda obco. Mechanika bez zmian.
- Zrobione: `ScreenStyle` (asset ART + RETRO w `Assets/Settings`, generowane przez setup), `ScreenStyleApplier`
  na `ScreenRoot` (sprite'y, tinty, fonty, bloom, planki lad), `CupSkin` dla puli kubków, piąty toggle
  ART/RETRO na tytule (`SettingsService.RetroScreen`, RETRO odblokowany razem z Jesionem), testy.
  ART jest `complete` dopiero gdy w `Assets/Art/screen_v3/` są wszystkie pliki z `NightCafeSetup.ArtSprites`;
  do tego czasu gra pokazuje RETRO, a toggle jest przygaszony.
- Assety ART są w `Assets/Art/screen_v3/` (komplet, styl `complete`): tło z Midjourney (prompt 11,
  kadr 1868×1310 z oryginału tak, by lada trafiła na dół ekranu), Miro 5 póz i Sablé 2 klatki wycięte
  `tools/cut_sheet.py` (Miro: `--floor 1265`; Sablé: `--split 540 1078 --floor 594 --floor-keep 955 1085
  1415 1536`), ekspres (`--fuzz 5`), a kubki ×4/rozbity/plama/tablica/półka/drabinka to wektory z
  `tools/gen_art_v3.py` (plank z Midjourney wyszedł perspektywicznie, nieużyty). Fonty OFL: Cabin Sketch
  (licznik, nagłówki) i Patrick Hand (reszta) w `Assets/Fonts`. Pivoty v3 w `SpriteAnchors.PaintedPivots`
  (stopy Miro liczone z alfy). W ART oba sloty używają pozy „taca w dół" — Miro stoi na drabince przy
  górnej półce (`step.png`), oryginalna poza „taca w górę" (`miro_up.png`) jest niewykorzystana.
- Ocena Michała (2026-09-14): „ekran pasuje". Poprawki wdrożone: poza „taca w dół" w arkuszu była
  narysowana w lewo (`cut_sheet.py --flip down`, pivot 0.2535) — Miro stoi teraz twarzą do rampy;
  tablica kredowa za licznikiem (`scoreboard.png`, `ScreenStyle.scoreBoard`) — cyfry nie zlewają się
  z neonem; kot 2× i trucht (`catBob`/`catTilt`, `CatCrossingView.SetMotion`); podskok przy zmianie
  slotu (`moveSeconds` 0.12 s, `PlayerPositionController` — slot logiczny zmienia się natychmiast,
  GDD §4); półki z fototeksturą drewna (ambientCG Wood027, CC0, `-paint`) pod kreską, grubsze (0.6);
  kubki z gradientem, ziarnem papieru i „drżącym" konturem (filtry SVG w `gen_art_v3.py`).
- Runda 2 (2026-09-14, uwagi: „wajcha nie działa", kot niewidoczny, marka niewidoczna, plan B):
  wajcha działała (adb tap), ale (a) po 8 s tytuł przechodzi w demo, a tam każdy tap startował rundę —
  teraz `RoundStateMachine.Press` w demo z `consumedByTitleUi` wraca do tytułu (wajcha działa w demo);
  (b) toggle ART/RETRO przy zablokowanym Jesionie milczał — teraz mruga „LOCKED" 1,4 s
  (`TitleToggleView.lockedHintSeconds`); na Pixelu Michała Jesion odblokowany ręcznie w
  `profile.json` (adb), żeby mógł obejrzeć RETRO. Kot: `cut_sheet.py --reverse-tone "#dcc39a"`
  (piaskowy, ciemna kreska, kontur 3 px; czarny oryginał ginął na podłodze). Marka i litery A/B:
  `shell_model.py engrave()` wycina tekst booleanem w blacie (0,12) i kładzie inlay 0,06 (`Brand`,
  `LabelA/B`); `DeviceShellView.engravings` + `SkinMaterials.inlay` — krem na ciemnych skinach,
  atrament na Jesionie; `sharpen()` (sharp od 40°) chroni cieniowanie blatu przy ściankach graweru.
  Plan B spisany: prompty 16 (zestaw kubków) i 17 (półka wprost) w `docs/references/PROMPTS.md`.
- Runda 3 (2026-09-14): drewno zniknęło z obudowy po grawerze — warstwa UV z bmesh nazywała się
  „Float2", boolean dodał „UVMap" z tekstu, a eksporter FBX wziął tę drugą; `shell_model.py` ma teraz
  jedną warstwę `UV_LAYER = "UVMap"` (`uv_layer_of`) i `engrave()` rzuca błąd, gdy jest ich więcej.
  Plan B wykonany: kubki (4 kolory + rozbity) i półka wprost z Midjourney (prompty 16/17, cięcie
  opisane w PROMPTS.md: `--split`, `--unshadow`, `--scale`); pivot kubków na stopce (0.5/0.03),
  `OrderPanelView` centruje kubek po `sprite.bounds`; wektorowe wersje w `gen_art_v3.legacy_sprites`.
- Runda 4 (2026-09-14, pierwsza na Windows; uwagi: iskry na krawędziach, „za bardzo pochylone",
  napis mniej kontrastowy i grubszy, kubki latają nad rampą): (a) iskry = aliasing spekularny
  aluminium bez MSAA → URP `m_MSAA: 4` (setup ustawia zawsze, nie tylko przy tworzeniu renderera);
  (b) pochylenie kamery 14° → 8°, światło kluczowe od strony gracza (`(0.35, 0.50, 0.80)`): przednia
  ścianka oświetlona, cień pada za korpus; (c) w cieniu na blacie prześwitywały litery graweru —
  shadow caster wycinał ścianki booleana → `ShadowCastingMode.TwoSided` na częściach obudowy;
  (d) **faza aluminiowa nigdy nie istniała** (0.004 zamiast 0.35): `bevel_top` bevelował też płaskie
  promieniowe krawędzie pierścienia blatu i `clamp_overlap` zbijał ją do zera; teraz tylko krawędzie
  blat↔ścianka, `CHAMFER = 0.25` (0.35 wyglądało jak chromowana rura); (e) grawer: DejaVu Sans Bold
  (`art/fonts/`, licencja DejaVu; `curve.offset` psuł booleana i gubił glify), inlay jasny orzech
  matowy zamiast kremu; (f) kubki w ART jadą po prostej desce K1→K5 (`CupController.FootFor`;
  tor ma załamanie pod szynę RETRO i w środku wisiał 0.22 nad deską), stoją pod kątem deski i kołyszą
  się (`ScreenStyle.cupsRideStraightRail/cupWobble 3°/cupWobbleHz 7/cupBob 0.03`, RETRO zera);
  kubki i rozbity kubek 70 % (`cupScale`, `brokenCupScale`), kubek w panelu zamówienia bez zmian.
  Ocena Michała: „jest ok".
- Runda 5 — dźwięk (2026-09-14): chiptune z `gen_audio.py` pasował do RETRO, nie do dioramy.
  Michał wygenerował (płatne plany, prawa komercyjne — `art/audio/LICENSE.md`): loop lo-fi w Suno,
  SFX i deszcz w ElevenLabs; surowe pliki w `art/audio/raw/` (LFS), prompty w PROMPTS.md.
  `tools/prep_audio.py` (numpy/scipy/soundfile/pyloudnorm) tnie je do `Assets/Audio/Art/`:
  SFX −3 dBFS z korektą per dźwięk (po odsłuchu: złapanie −5, stłuczka −9, kot −6, klik −1),
  loop szukany po siatce beatów (tempo z autokorelacji, szew oceniany po obwiedni onsetów +
  paśmie <2 kHz, długość doprecyzowana co 5 ms) → 19 taktów = 64.2 s, crossfade 80 ms, −14 LUFS;
  ambient = 4 warianty deszczu sklejone crossfade'ami w 36 s pętlę, −16 LUFS.
  Dźwięk jest częścią skinu: `ScreenStyle.sounds` → `AudioConfig_Art`, RETRO = bazowy `AudioConfig`
  (chiptune); `AudioService.SetSoundSet` przełącza klipy razem z ekranem, poziomy i haptyka zostają
  w bazowym configu. Trzecie źródło `AmbienceSource` (`AudioConfig.ambience`, `ambienceOffsetDb` −23)
  pod toggle'em „~" razem z muzyką; muzyka −18 dB (GDD). ART muzyka zostaje stereo, reszta mono.
- Do zrobienia: (1) ekran tytułowy/game over w nowej kresce; (2) M5.
- Krita: tylko retusz, instrukcja w `docs/art-direction.md`.

**M5 — release candidate:**
- Ikona + splash (wygeneruj z `device/lever_knob` + kubek), IL2CPP ARM64 **release** AAB bez pakietu `com.unity.pipeline` i bez dev flags, README, checklista §7 GDD, test na telefonie, tag `v1.0.0`.

Po każdym milestone: commit + push, krótki raport co działa/czego brakuje.

## Stan (2026-09-13)

M4 w większości zrobione i sprawdzone na Pixelu (10/10 zimnych startów, 59 fps): `tools/gen_art.py`
generuje wszystkie sprite'y LCD w stylu A (segmentowy, wybór Michała) z detalami z C; DSEG7/DSEG14
w liczniku, BEST, trybie i zegarze; shader `NightCafe/LcdGlass` (winieta + odblask); obudowa
renderowana w Blenderze (`tools/shell_render.py`, 4 skiny jako osobne sprite'y, tekstury CC0 w
`art/textures`); pochylenie kubka ±14°; oddech (wycieranie rąk + mruganie neonu); animacja 999 →
neonowy kot; minutnik parzenia (tap w zegar na tytule). Referencje z Midjourney w `docs/references`.
**M4.5 (2026-09-13, po drugiej ocenie Michała: „makieta, nie urządzenie"): obudowa jest prawdziwym
modelem 3D w Unity.** `tools/shell_model.py` (Blender, headless) → `Assets/Art/device/breve_deck.fbx`
(korpus 22×11×1.8 z aluminiową fazą i zaokrągloną dolną krawędzią, wgłębiony LCD 13.2×9.26 = 60 % szerokości, kopułkowe przyciski
w kołnierzach, suwak z grawerem A/B, grawer BRÉVE DECK, kratka). Dwie kamery: `LcdCamera` (2D,
Renderer2D, bloom) renderuje scenę gry 1:1 do `Assets/Settings/LcdRT.renderTexture`, `DeviceCamera`
(perspektywa, `UniversalRenderer.asset`) patrzy na model z tą teksturą na ekranie. Dotyk: ćwiartki
bez zmian; toggle'e/zegar przez `LcdPointer` (raycast → UV ekranu → scena LCD), wajcha = collider.
`DeviceShellView` porusza prawdziwymi capami (skok + podświetlenie) i gałką (overshoot + klik),
skiny = materiały korpusu (orzech/jesion/onyks/neon z fioletową obwódką). HDRI Poly Haven (CC0)
w `Assets/Art/device/env`, paralaksa z żyroskopu (`DeviceTilt`). Sprite'y obudowy z v2 usunięte.
Ocena Michała (2026-09-13 wieczór): urządzenie „w 95 %" dobre; poprawki: (1) korpus wyglądał jak tafla —
kamera patrzyła od górnej krawędzi (ścianka widoczna u góry, na dole nic); teraz `DeviceLayout.CameraOffset`
stawia kamerę po stronie gracza (−y) z pochyleniem 14°, korpus ma 1.8 grubości i widać przednią ściankę
z cieniem; kadr liczony po 8 rogach bryły; (2) kolor kubka w locie ≠ kolor w panelu zamówienia — `cup.png`
jest teraz biały i barwiony przez `SpriteRenderer` (bursztyn w A, kolor zamówienia w B, dokładnie hex z GDD §3);
(3) drewno zostaje jak jest. Pułapka: pojedyncza ściana w bmesh dostaje losową normalną —
`rounded_face` wymusza +Z, a po eksporcie Unity bywa trzeba wymusić reimport FBX (stary mesh w cache).
**Decyzja Michała: sam ekran (segmentowy Neo-LCD) już nie pasuje do urządzenia — nowy etap M4.6 niżej.**
Zostaje przed M4.6: szlif modelu (ryflowana gałka, kopułki) — drobne.

M1–M3.5 zrobione i przetestowane na Pixelu 10. Sterowanie w edytorze: W/S (lewe tory), ↑/↓ (prawe), Spacja/Enter (start), **mysz** = tap (działa też na wajchę i toggle'e na tytule). Edytor może być otwarty podczas pracy z CLI — pakiet `com.unity.pipeline` + `unity cmd` (testy, `menu NightCafe/Build Scene Setup`, `build`).

## Zasady

1. `docs/GDD.md` = źródło prawdy dla liczb i zasad.
2. Nie dodawaj SDK reklam/analityki.
3. Kod i komentarze po angielsku, commity konwencjonalne (`feat:`, `fix:`).
4. Wszystkie wartości balansu w ScriptableObjects/konfigach — nie hardkoduj w logice.
5. Przy wątpliwościach projektowych — pytaj, nie zgaduj.
