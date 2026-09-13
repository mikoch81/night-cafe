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
