# Night Café — handoff dla Claude Code (VS Code)

> Wklej ten plik do repo jako `docs/CLAUDE_CODE_HANDOFF.md` i podaj Claude Code
> w pierwszym prompcie: „Przeczytaj docs/CLAUDE_CODE_HANDOFF.md i docs/GDD.md, potem wykonaj Milestone 1".

## Kontekst

Tworzymy grę mobilną **Night Café** (Unity, Android, landscape) — opis i wszystkie liczby w `docs/GDD.md` (źródło prawdy; nie zmieniaj wartości bez pytania). Assety PNG + źródła SVG są w paczce `night-cafe-assets.zip` (struktura `Art/` i `Source/` + `MANIFEST.txt`).

**Ważne — pochodzenie projektu:** gra inspiruje się mechaniką klasycznych handheldów LCD, ale NIE może zawierać żadnych elementów gry „Nu, pogodi!" 1984 (postacie, grafika, nazwa, dźwięki). W `C:\UnityProjects\WolfEggs\` (stary komputer z Windows; jeśli niedostępny — pomiń) jest poprzedni prototyp: wolno zaglądać TYLKO po rozwiązania techniczne (input, pooling, build settings). Nie kopiuj z niego grafiki, nazw obiektów ani układu ekranu.

## Środowisko

- Katalog projektu: `/home/mkej/Projekty/NuPogodiModern/` (utwórz).
- Unity: **6 LTS lub 2022.3 LTS**, moduł Android (sprawdź `unityhub`/instalację; jeśli brak — poinstruuj Michała, sam nie instaluj Unity).
- Telefon Android podłączony po USB — buildy testowe: `adb devices`, `adb install -r build/NightCafe.apk`.
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
- Do zrobienia: (1) Michał generuje prompty 11–15 (`docs/references/PROMPTS.md`) → `art/midjourney/`;
  (2) `tools/cut_sheet.py` tnie arkusze do `Assets/Art/screen_v3/` (nazwy plików w `ArtSprites`);
  (3) kubki ×4, rozbity kubek, plamy, tablica zamówienia jako wektory w kresce (`gen_art.py` styl `ink`);
  (4) pivoty v3 w `SpriteAnchors` (Miro/Sablé: stopy), font HUD (OFL, ręczny), `plankHeight`, skala
  postaci; (5) zrzut z telefonu → ocena Michała → commit; (6) ekran tytułowy/game over w nowej kresce.
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
