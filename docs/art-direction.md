# Night Café — kierunek artystyczny (M4, Art v2)

Decyzje z 2026-09-12: **Neo-LCD dopracowany** — paleta i zasady z GDD §5.2 zostają (jeden kolor
bursztynu, glow wypalony w PNG + bloom 0.15), zmienia się jakość sylwetek i „prawdziwość" LCD.
Narzędzia darmowe (Inkscape, Krita, potrace, Blender) + Midjourney tylko do referencji i grafik
sklepowych. Claude generuje (`tools/gen_art.py`), Michał ocenia.

## Arkusz do wyboru: `art-direction/sheet.png`

Kolumny: obecna grafika (v1) i trzy warianty tej samej pozy baristy (`barista_up`) oraz kota
(`cat_a`). Dolny rząd = 45 % — mniej więcej rozmiar na ekranie telefonu.

| Wariant | Co go definiuje | Za | Przeciw |
|---|---|---|---|
| **A — segmentowy LCD** | jedna sylwetka pocięta ciemnymi rowkami na segmenty (głowa, tors, fartuch, ręka, nogi), jak w prawdziwym wyświetlaczu | najbardziej „autentyczny" handheld; rowki dają czytelność bez detali; najlepiej współgra z duchami segmentów i shaderem `LcdSegment` | najmniej „osobowości"; rowki muszą być spójne w każdej pozie (więcej pracy przy 4 pozach baristy) |
| **B — Game & Watch** | jedna ciężka, zwarta sylwetka; detale tylko jako wycięcia (oko, obrys fartucha) | najlepsza czytelność z daleka i w ruchu; najmniej krawędzi = najczystszy glow | najbardziej „klockowy"; ryzyko, że wygląda jak grafika v1 tylko grubsza |
| **C — kreskowy** | smuklejsza figura, więcej opowieści: wąsy, muszka, kieszeń, kubek z parą, wąsy i obroża kota, strzępy mopa | najwięcej charakteru, dobrze wygląda w powiększeniu (ikona, sklep) | cienkie detale giną przy 1.6× na telefonie i zamazują się pod bloomem; najdalej od estetyki LCD |

Moja rekomendacja: **A** dla wszystkiego, co jest na ekranie LCD (barista, kot, kubki, plamy,
głowice), z 2–3 detalami z C tam, gdzie się mieszczą (kubek z parą na spodku, strzępy mopa).
Do ikony i grafiki sklepowej — C.

**Decyzja (2026-09-13): A z detalami z C — wg rekomendacji.** Zrobione: wszystkie sprite'y,
barista dodatkowo z proporcjami wg `references/02_barista_a.png` (duża głowa, płaska czapka,
fartuch do kolan, taca).

## Co się dzieje po wyborze

1. `tools/gen_art.py sprites` generuje w wybranym stylu wszystkie sprite'y z `Assets/Art/sprites`
   (4 pozy baristy, 2 klatki kota, kubek, kubek rozbity, plama, głowica, panel zamówień) —
   źródła SVG do `Assets/Art/Source/sprites`, PNG @4× do `Assets/Art/sprites`.
   `SpriteAnchors` dostaje nowe pivoty (test pilnuje zgodności).
2. Licznik i zegar przechodzą na DSEG14 (już w `Assets/Fonts`, OFL); zegar na tytule może dostać
   DSEG7 — do oceny na zrzucie.
3. Shader `LcdSegment` (Shader Graph): glow, lekki ghosting, tint szkła — zastępuje quady `ScreenFX`.
4. Obudowa v2 z Blendera (`tools/shell_render.py`): drewno z `art/textures` (walnut / ash / onyx),
   ramka z Metal009, wgłębienie LCD, przyciski z fazką; render ortograficzny w dzisiejszych
   canvasach. Skiny = 4 rendery.
5. Animacje z GDD: pochylenie kubka ±14°, oddech (barista wyciera ręce, neon mruga), 999 → kot
   z neonów, minutnik parzenia.

Każda partia: `capture_game_view` z edytora + zrzut z Pixela obok siebie w jednym PNG.

## Referencje

`docs/references/` — obrazy z Midjourney wygenerowane z promptów w `references/PROMPTS.md`.
Służą wyłącznie jako inspiracja i materiał sklepowy; do gry trafiają tylko wektory i rendery.
Nic z Nu, pogodi! 1984 (GDD §7).
