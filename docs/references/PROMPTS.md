# Prompty do Midjourney (referencje, nie assety)

Zasady: obrazy z tego pliku lądują w tym katalogu jako `NN_nazwa.png` (nie w `Assets/`), są
inspiracją i materiałem sklepowym. Do gry idą wektory z `tools/gen_art.py` i rendery z Blendera.
Żadnych promptów z „Nu, pogodi!", wilkiem, zającem, jajkami, cyrylicą (GDD §7).

Wszystkie prompty zakładają `--ar 16:9 --style raw --v 7`; `--no` wycina typowe artefakty.
Po wygenerowaniu wystarczy 1 wybrany upscale na prompt, 2 przy 01 i 05.

## 01 — Obudowa (dla renderu w Blenderze)

```
product photo of a modern retro handheld LCD game console, landscape orientation, walnut wood body
with brushed aluminium bezel, recessed amber monochrome LCD screen with soft glow, two round tactile
buttons on each side, small lever switch at the bottom, warm night café lighting, dark background,
studio product photography, sharp, 8k --ar 16:9 --style raw --v 7 --no hands, text, logo, cables
```

Warianty: `ash wood body` (jesion), `black lacquered onyx body` (onyks), `translucent neon-edge
acrylic body` (neon).

## 02 — Postać: barista Miro (sylwetka do wyboru stylu)

```
single-colour amber LCD segment character on black glass, a barista in a flat cap and apron holding
a saucer, flat pictogram silhouette made of a few rounded segments separated by thin dark gaps,
Game & Watch style, side view facing right, no shading, no gradients, centered, minimal
--ar 16:9 --style raw --v 7 --no photo, 3d, face detail, text
```

## 03 — Postać: kot Sablé z mopem

```
single-colour amber LCD segment pictogram on black glass, a sitting cat pushing a mop, flat
silhouette built from rounded segments with thin dark gaps, Game & Watch style, side view,
no shading, minimal, centered --ar 16:9 --style raw --v 7 --no photo, 3d, fur texture, text
```

## 04 — Ekran gry (układ, atmosfera LCD)

```
close-up of an amber monochrome LCD handheld game screen, four diagonal rails with small coffee
cups sliding down, a barista at the bottom centre, ghosted inactive segments faintly visible,
14-segment digit counter at the top, glass reflection, macro photo, shallow depth of field
--ar 16:9 --style raw --v 7 --no colour, rgb, text, watermark
```

## 05 — Feature graphic do Google Play (1024×500, kadr 16:9 do przycięcia)

```
night café at 2 a.m. seen through a rain-streaked window, warm amber neon sign glow, a modern retro
handheld LCD game console lying on a wooden counter next to an espresso cup, lo-fi illustration,
muted dark palette with amber highlights, cinematic, no people --ar 16:9 --style raw --v 7
--no text, logo, watermark
```

## 06 — Ikona (inspiracja; finalna ikona = wektor z `lever_knob` + kubek)

```
app icon, flat amber LCD pictogram of a coffee cup with steam on a dark glass rounded square,
soft glow, single colour, minimal, centered --ar 1:1 --style raw --v 7 --no text, gradient, photo
```

## 07 — Koncept obudowy 3/4 z dokładnym układem (wzór materiałów dla modelu 3D)

```
three-quarter product photo of a modern retro handheld LCD game console lying on a dark café
counter, landscape body 2:1 in dark walnut wood with a brushed aluminium chamfered edge, a large
recessed amber monochrome LCD taking 60% of the width, two small cream domed buttons on each
side, a small aluminium slide switch below the screen with engraved A and B, engraved maker's mark
bottom left, speaker holes bottom right, soft studio light, shallow depth of field, 8k
--ar 16:9 --style raw --v 7 --no hands, text, logo, cables, d-pad
```

## 08–10 — Ekran v3: trzy kierunki stylu (do wyboru; potem ten sam styl dla wszystkich assetów)

Wspólny kadr: ekran gry widziany wprost, proporcja 1272:892 (`--ar 10:7`), bar nocnej kawiarni z
czterema ukośnymi ladami/torami (2 lewe, 2 prawe) schodzącymi do baristy na środku u dołu, ciepłe
światło lamp, neon w tle. To referencja stylu, nie gotowa plansza.

### 08 — Gwasz / lo-fi kolor (rekomendacja: najbliżej „kreski malowanej", pasuje do orzecha i bursztynu)

```
gouache painting of a cozy night café bar seen straight on, four diagonal wooden counters sloping
down towards a small barista in a flat cap at the bottom centre, coffee cups sliding along the
counters, a cat with a mop in the corner, warm lamp light, amber neon sign in the back, visible
brush strokes, soft paper texture, limited warm palette with deep browns and cream, muted teal
shadows, lo-fi illustration, no outlines --ar 10:7 --style raw --v 7 --no text, photo, 3d, ui
```

### 09 — Kreska tuszem + płaski kolor (czytelniejsze sylwetki, tańsze w produkcji klatek)

```
ink line illustration with flat colour fills, a cozy night café bar seen straight on, four
diagonal counters sloping down towards a small barista in a flat cap at the bottom centre, coffee
cups sliding along the counters, a cat with a mop, thick confident black outlines, limited warm
palette of amber, cream and dark brown, screen-print texture, minimal shading, clean vector
look --ar 10:7 --style raw --v 7 --no text, photo, 3d, gradients, ui
```

### 10 — Malowany monochrom bursztynowy (zachowuje „świecący wyświetlacz", zmienia tylko kreskę)

```
monochrome amber painted illustration on dark glass, a cozy night café bar seen straight on, four
diagonal counters sloping down towards a small barista in a flat cap at the bottom centre, coffee
cups sliding along the counters, a cat with a mop, loose brush strokes, single amber colour on
near-black, soft glow like a backlit display, lo-fi, no outlines --ar 10:7 --style raw --v 7
--no colour, rgb, text, photo, 3d, ui
```

Po wyborze kierunku: prompt na arkusz postaci Miro (5 póz: góra, dół, złapanie, pudło, wycieranie)
i kota (2 klatki) w tym samym stylu, `--sref` na wybrany obraz, tło osobno bez postaci.

Wynik (2026-09-13): `08_gouache_a-d`, `09_ink_a-d`, `10_mono_a-d` w tym katalogu (pomniejszone).
Decyzja Michała: **miks** — tło i lady w gwaszu (08), postacie/rekwizyty w kresce z płaskim kolorem (09),
poświata bursztynowa (10) zostaje w neonie, HUD-zie i szkle. Ekran = diorama w oknie, stary look
segmentowy zostaje jako odblokowywany skin ekranu „RETRO".

## 11–15 — Assety ekranu v3 (to już idzie do gry, nie tylko referencja)

Jak podać referencję stylu: otwórz swój obraz w midjourney.com → przycisk **Use** → **Style** (albo
przeciągnij obraz do paska Imagine i upuść w polu „style reference"). W tekście promptu zostaje tylko
`--sw 60` (siła stylu). Upscale: **Upscale (Subtle)** — Creative zmienia kształty.

Wynik wrzucaj do `art/midjourney/` jako `11_bg.png`, `12_plank.png`, `13_miro_sheet.png`,
`14_sable_sheet.png`, `15_machine.png` (pełna rozdzielczość po upscale). Wycinaniem z białego tła
zajmuje się `tools/cut_sheet.py` — Krita tylko do retuszu (instrukcja w `docs/art-direction.md`).

### 11 — Tło dioramy (styl: 08_gouache_c)

Kompozycja jest ważniejsza niż ładność: góra ciemna (tam siedzi licznik), środek spokojny (tam
latają kubki i tory), dół = długi bar (tam stoi Miro, plamy, kot). Bez ludzi, kubków i kota.

```
gouache painting of an empty night café bar interior seen straight on at eye level, a long
wooden bar counter running along the very bottom edge of the picture, a plain dark wood panelled
wall filling the middle of the picture with nothing hanging on it, a dark ceiling with two
pendant lamps and a small amber neon sign at the very top, an espresso machine and shelves of
bottles far off to the sides only, visible brush strokes, soft paper texture, warm palette of
deep browns and cream with muted teal shadows, no people, no cups, no cat --ar 10:7 --sw 60
--style raw --v 7 --no text, signage letters, people, cups, cat, photo, 3d, ui, frame, border
```

### 12 — Lada / półka toru (styl: 08_gouache_c; obracam ją w Unity, potrzebna prosta)

```
a single straight wooden bar shelf plank with a thin brass edge, seen from slightly above and
straight on, horizontal, isolated on a plain white background, gouache, warm walnut wood with
visible brush strokes, no other objects, nothing on the shelf --ar 3:1 --sw 60 --style raw --v 7
--no text, cups, background, shadow, photo, 3d
```

### 13 — Arkusz postaci: barista Miro, 5 póz (styl: 09_ink_a)

Wszystkie pozy w jednym obrazie, żeby postać była spójna. Jeśli jedna poza nie wyjdzie, dogeneruj ją
osobno z **Use → Omni** na arkuszu (`--ow 80`).

```
character sheet of a friendly young barista in a flat cap, rolled sleeves and a long apron, side
view facing right, five full-body poses in one row on a plain white background: holding a round
tray up at shoulder height, holding the tray down at hip height, catching a coffee cup on the
tray, flinching as a cup drops past him, wiping his hands on a towel; ink line illustration with
flat colour fills, thick confident black outlines, warm palette of amber, cream and dark brown,
same character and proportions in every pose, big readable silhouette, evenly spaced, feet on
the same line --ar 5:2 --sw 60 --style raw --v 7 --no text, labels, numbers, background,
shading, gradients, photo, 3d
```

### 14 — Arkusz kota Sablé, 2 klatki (styl: 09_ink_a)

```
two poses of the same small black cat pushing a mop along the floor, side view walking to the
right, in one row on a plain white background, walking pose A and walking pose B with the legs
swapped, ink line illustration with flat colour fills, thick black outlines, amber eyes, big
readable silhouette --ar 2:1 --sw 60 --style raw --v 7 --no text, labels, background, shading,
photo, 3d, fur texture
```

### 15 — Ekspres na starcie toru (styl: 09_ink_a)

```
a small vintage chrome espresso machine seen from the side with the spout pointing to the right,
isolated on a plain white background, ink line illustration with flat colour fills, thick black
outlines, warm palette of cream, amber and dark brown, simple readable shape --ar 1:1 --sw 60
--style raw --v 7 --no text, labels, background, shadow, photo, 3d
```

Kubki (4 kolory), rozbity kubek, plamy, tablica zamówienia, półka toru i drabinka to wektory w tej
samej kresce (`tools/gen_art_v3.py`) — kolory kubków co do heksa z GDD §3. Wynik 2026-09-13:
11, 13, 14 (drugi arkusz, koty 2 i 3), 15 użyte; 12 (plank) wyszedł jako deska w perspektywie i został
zastąpiony wektorem. Oryginały w `art/midjourney/`. Kot z 14 był czarny i ginął na ciemnej podłodze;
`cut_sheet.py --reverse-tone "#dcc39a"` odwraca mu tony (piaskowy z ciemną kreską, cienki ciemny kontur).

## Plan B na rekwizyty (2026-09-14): kubki i półka z Midjourney

Jeśli wektorowe kubki i półki (`tools/gen_art_v3.py`) dalej wyglądają słabo obok malowanego tła.
Te same zasady co wyżej: model 8.1, Style reference 09_ink_a, `--sw 60`, Upscale Subtle, PNG do
`art/midjourney/`. Wycinanie: `tools/cut_sheet.py` (kolory zamówień z GDD §3 przypinam po wycięciu
w ImageMagick, jeśli Midjourney nie trafi w odcień).

### 16 — Zestaw kubków, 4 kolory zamówień + rozbity (styl: 09_ink_a)

Jeden rząd, ta sama forma i wielkość, uszko w prawo; kolory po nazwach, bo Midjourney nie czyta heksów.
Cięcie: `--prefix cup --names espresso caramel latte decaf broken --fuzz 6`.

```
five identical small ceramic coffee cups in one row, side view with the handle on the right,
isolated on a plain white background, from left to right: a honey amber cup, a peach orange cup,
a pale cream cup, a lilac purple cup, and the same cup shattered into three pieces lying on the
ground; ink line illustration with flat colour fills, thick confident black outlines, soft paper
grain, small glossy highlight on each cup, evenly spaced, same size --ar 5:2 --sw 60 --style raw
--no text, labels, numbers, saucer, spoon, steam, coffee, background, shadow, gradients, photo, 3d
```

### 17 — Półka toru, widok wprost (styl: 08_gouache_c)

Prompt 12 dał deskę w perspektywie. Tu wprost i płasko, oba końce ucięte prosto, żeby dało się ją
rozciągać jako sprite dziewięciokrotny (importer: `spriteBorder` 96 px po bokach).

```
a long straight wooden café bar shelf seen exactly from the front at eye level, perfectly
horizontal and flat with no perspective, both ends cut square, a thin brass strip along the front
edge, isolated on a plain white background, gouache with visible brush strokes and paper texture,
warm walnut brown with amber highlights, nothing on the shelf --ar 5:1 --sw 60 --style raw
--no text, cups, objects, background, shadow, angle, perspective, photo, 3d
```

Wynik 2026-09-14: oba użyte (`art/midjourney/16_cups.png`, `17_shelf.png`). Cięcie:

```
python3 tools/cut_sheet.py art/midjourney/16_cups.png Assets/Art/screen_v3 --prefix cup \
    --names espresso caramel latte decaf broken --split 740 1350 1960 2600 --fuzz 8 --unshadow --scale 0.4875
python3 tools/cut_sheet.py art/midjourney/17_shelf.png Assets/Art/screen_v3 --names plank --fuzz 8 --pad 0 --scale 0.2632
```

`--unshadow` zdejmuje szary fotograficzny cień spod kubków (tuszowy czarny zostaje — kubek jedzie po
półce, więc cień na desce ma sens), `--split` bo pierwsza skorupa rozbitego kubka leży bliżej kubka
decaf. Kubki mają pivot na stopce (`SpriteAnchors` 0.5/0.03), bo półka wprost nie ma widocznego
blatu i kubek musi stać na jej górnej krawędzi; panel zamówienia centruje sprite po jego granicach.
Wektorowe kubki i półka zostały w `gen_art_v3.py` jako `legacy_sprites` (renderowane tylko po nazwie).

## Tytuł i koniec zmiany (2026-09-15): rekwizyty ekranu tytułowego i game over

Ekran tytułowy zostaje dioramą: kredowy szyld w oknie, toggle'e jako karteczki przypięte do ściany,
zegar na tarczy, Miro wyciera ladę (istniejąca poza `miro_wipe.png`). Game over = „koniec zmiany":
światła gasną, karta z wynikiem na ladzie, Sablé śpi. **Wszystkie rekwizyty bez napisów** — litery i
cyfry pisze TMP (Cabin Sketch / Patrick Hand), bo Midjourney nie utrzyma pisowni ani zmiennych cyfr.
Te same zasady co 13–17: model 8.1, Style reference 09_ink_a, `--sw 60`, Upscale Subtle, PNG do
`art/midjourney/18_title_props.png` i `19_sable_sleep.png`. Cięcie: `tools/cut_sheet.py` (wymaga
ImageMagick — `winget install ImageMagick.ImageMagick`).

### 18 — Arkusz rekwizytów tytułu, 5 sztuk (styl: 09_ink_a)

Jeden rząd, wszystkie **puste** (bez liter, cyfr, wskazówek). Cięcie: `--names title_sign card_a card_b
result_card clock_face --fuzz 8 --unshadow` (podział `--split` dobiorę po obejrzeniu arkusza).

```
five café props in one row on a plain white background, evenly spaced, seen straight on: a small
blank chalkboard sign hanging from a string on two nails with a thin wooden frame, a blank cream
index card pinned to the wall with a red round push pin at the top, a second blank index card
pinned slightly askew, a blank paper receipt lying flat with one folded corner, a small round
wall clock with a cream face and no hands and no numbers; ink line illustration with flat colour
fills, thick confident black outlines, soft paper grain, warm palette of cream, amber and dark
brown --ar 5:2 --sw 60 --style raw --no text, letters, numbers, words, writing, chalk marks,
clock hands, background, shadow, gradients, photo, 3d
```

### 19 — Sablé śpi, 2 klatki (styl: 09_ink_a, ten sam kot co 14)

Ten sam kot co w 14 (użyj arkusza 14 jako **Use → Omni**, `--ow 80`), żeby był to wyraźnie ten sam
zwierzak. Cięcie jak w 14: `--names sable_sleep_a sable_sleep_b --reverse-tone "#dcc39a"` (kot na
ladzie jest ciemny, kreska musi być ciemna a sierść piaskowa).

```
two poses of the same small black cat curled up asleep on a wooden bar counter next to a mop
leaning against the wall, side view, in one row on a plain white background: eyes closed with the
tail wrapped around the body, and the same pose with the head tucked deeper and one ear folded,
ink line illustration with flat colour fills, thick black outlines, big readable silhouette
--ar 2:1 --sw 60 --style raw --no text, labels, background, shading, photo, 3d, fur texture
```

Wynik 2026-09-15: oba użyte (`art/midjourney/18_title_props.png`, `19_sable_sleep.png`). Cięcie
(w tej powłoce `magick` i `inkscape` trzeba dodać do PATH: `C:\Program Files\ImageMagick-7.1.2-Q16-HDRI`,
`C:\Program Files\Inkscapein`):

```
py -3.12 tools/cut_sheet.py art/midjourney/18_title_props.png Assets/Art/screen_v3     --names title_sign card_a card_b result_card clock_face --split 865 1540 2128 2664 --fuzz 8 --unshadow
py -3.12 tools/clear_dial.py Assets/Art/screen_v3/clock_face.png      # zdejmuje narysowane wskazówki, zostawia kropki godzin
py -3.12 tools/clear_marks.py Assets/Art/screen_v3/result_card.png    # zdejmuje linie z paragonu (przechodziły przez napisy)
py -3.12 tools/lift_from_shelf.py art/midjourney/19_sable_sleep.png art/midjourney/19_sable_sleep_clean.png     --box 770 820 1400 1160 --box 1800 820 2460 1160 --ledge 1085     --erase 1355 1086 1385 1102 --erase 1795 1086 1848 1102 --erase-poly 763 1072 763 1165 858 1165
    # koty leżą na półce: półka po kolorze na biało; --erase = kikuty kreski półki przy sylwetce,
    # --erase-poly = nasada ogona kota A, który zwisał z półki (skos czyta się jak podwinięta pierś)
py -3.12 tools/cut_sheet.py art/midjourney/19_sable_sleep_clean.png Assets/Art/screen_v3 --prefix sable     --names sleep_a sleep_b --split 1600 --fuzz 8 --reverse-tone "#dcc39a"
```

Szyld miał sznurek na dwóch gwoździach — rozciągnięty dziewięciokrotnie robił się płaskim trójkątem, więc
jest zdjęty (jasne piksele w górnych 110 rzędach + wszystko powyżej rzędu 72 poza kolumnami gwoździ →
przezroczyste; Pillow, jednorazowo). Szyld i paragon są sprite'ami dziewięciokrotnymi
(`NightCafeSetup.PaintedBorders`: 80 px ramki, paragon 50/150/50/70), rozciąganymi do
`ScreenStyle.titleSignSize` / `resultCardSize`; karteczki i tarcza skalowane jednolicie (`toggleCardWidth`,
`clockFaceWidth`). Ogon kota A zwisający z półki został ucięty (`--box` kończy się na blacie).

## Dźwięk (2026-09-14): Suno + ElevenLabs Sound Effects

Wygenerowane przez Michała na płatnych planach (prawa komercyjne, `art/audio/LICENSE.md`). Surowe pliki w
`art/audio/raw/`, obróbka `tools/prep_audio.py` → `Assets/Audio/Art/`.

| Plik | Serwis | Prompt |
|---|---|---|
| `lofi_loop.wav` | Suno | Instrumental lo-fi hip hop, 72 BPM, warm Rhodes electric piano chords, soft brushed drums, muted upright bass, vinyl crackle, rainy night café mood, calm and cozy, no vocals, no melody lead, seamless loopable, consistent throughout |
| `ambience_rain_cafe*.wav` (4 warianty) | ElevenLabs | Gentle steady rain against a café window at night, distant quiet murmur of a few customers, occasional soft clink of cups, no music, seamless loop |
| `sfx_catch.wav` | ElevenLabs | Single soft ceramic coffee cup set down on a wooden tray, short, gentle, close-miked |
| `sfx_miss.wav` | ElevenLabs | Ceramic coffee cup falling and shattering on a wooden floor, single break, short |
| `sfx_combo.wav` | ElevenLabs | Three ascending warm Rhodes electric piano notes, short pleasant arpeggio, lo-fi |
| `sfx_cat.wav` | ElevenLabs | Single short soft cat meow, small cat, friendly |
| `sfx_gameover.wav` | ElevenLabs | Small brass bell above a café door rings once as the door closes, then quiet |
| `sfx_brew_alarm.wav` | ElevenLabs | Kitchen timer ding, single bright ceramic-like bell, short |
| `sfx_click.wav` | ElevenLabs | Small metal toggle switch click, single, crisp, close |
