# Night Café — Game Design Document (v1.0)

Gra mobilna 2D, Unity (URP 2D), Android, orientacja **landscape**. Wirtualny handheld „Bréve Deck" z ekranem Neo-LCD. Inspiracja gatunkiem LCD „catch falling objects" — własny świat, postacie i wyrażenie.

---

## 1. Fantazja i ton

Nocna kawiarnia w deszczowym mieście. Barista **Miro** łapie na tacę kubki zjeżdżające z 4 podajników ekspresów i podaje je klientom przy barze. Kot **Sablé** sprząta stłuczki. Ton: ciepły, lo-fi, spokojny — napięcie buduje tempo, nie agresja.

## 2. Rdzeń pętli

### 2.1 Geometria
- 4 tory: **LG** (lewy górny), **LD** (lewy dolny), **PG** (prawy górny), **PD** (prawy dolny).
- Każdy tor ma **5 kroków** (K1 = głowica ekspresu, K5 = punkt złapania).
- Barista zajmuje zawsze 1 z 4 pozycji (odpowiadających K5 każdego toru).

### 2.2 Ruch kubka
- Kubek porusza się skokowo-płynnie: tween (ease-linear) między krokami; pozycje kroków z `LaneConfig` (wektory zgodne z `screen_bg.png`).
- **Czas kroku** (sekundy na 1 krok) zależny od poziomu tempa `T` (0–9):
  `stepTime = 0.90 − 0.055 × T` → 0.90s (T0) … 0.405s (T9), floor **0.40s**.
- Poziom tempa rośnie: **T+1 co 12 złapanych kubków** (licznik globalny od startu rundy, nie resetowany pudłem).
- Kubek na K5: okno złapania = obecność baristy na tym torze w chwili dotarcia (bez dodatkowego timingu — jak w klasyku: pozycja decyduje).

### 2.3 Spawner
- Interwał między spawnami: `spawnInterval = stepTime × R`, gdzie R losowe z {2, 3, 4} (ważone: 2→20%, 3→50%, 4→30%).
- Tor losowany z zakazem: **max 2 kubki jednocześnie na jednym torze**, min odstęp 2 kroków.
- Limit kubków na ekranie: T0–T2: 2 · T3–T5: 3 · T6+: 4.
- Pierwszy kubek po 1.2s od startu rundy.

### 2.4 Złapanie
- +1 pkt (Tryb A). Klatka `barista_catch` 120 ms + haptyka 15 ms + blip (patrz audio).
- **Combo:** licznik serii bez pudła. Co pełne 25 serii → bonus +5 pkt i błysk neonu baru. Seria resetuje się pudłem.

### 2.5 Pudło i kary
- Kubek mija K5 bez baristy → `cup_broken` w miejscu upadku (400 ms), pojawia się **plama** (HUD, max 3), kot Sablé przechodzi przez dół ekranu z mopem (animacja 2 klatki, 1.6s), `barista_miss` 300 ms.
- **3 plamy = koniec zmiany** (game over): ekran przygasa, wynik + rekord, restart.
- **Litość kota:** przy wyniku 200 i 500 (dokładnie w chwili przekroczenia) kot ściera **jedną** plamę (jeśli jest). Odpowiednik klasycznej mechaniki „half-miss", ale własne wyrażenie i inne progi.

### 2.6 Oddech
- Po każdym pełnym 100 pkt: **2.5s przerwy** spawnów (kubki w locie kończą bieg). Neon za oknem mruga, barista wyciera ręce (użyj `barista_down` idle).

### 2.7 Rollover 999
- Licznik 3-cyfrowy; po 999 → 0, tempo zostaje na T9. Sekretna animacja: neony miasta za oknem układają się w kota (2s). Bez odniesień do 1984.

## 3. Tryby

- **Tryb A (Zmiana dzienna):** wszystkie kubki łapiemy. Zasady jak wyżej.
- **Tryb B (Zamówienia):** nad barem panel zamówienia z kolorem kubka (tint: espresso `#ffc966`, karmel `#ff9d6e`, mleczny `#ffe9a8`, deka `#d98cff`).
  - Kubki spawnują się w losowych kolorach (kolor zamówienia ma wagę 55%, pozostałe po 15%).
  - Złap właściwy kolor: +2 pkt. Złap zły: **kara jak pudło** (plama). Przepuść zły kolor: nic (to poprawne zachowanie). Przepuść dobry: pudło.
  - Zamówienie zmienia się co 10 złapanych właściwych lub co 20s (co pierwsze).
  - Start od T2, przyspieszanie T+1 co 10 złapań.

## 4. Sterowanie

- **4 strefy dotyku** = ćwiartki ekranu telefonu (lewa-góra/lewa-dół/prawa-góra/prawa-dół). Tap = przeskok baristy na tę pozycję (natychmiast, bez tweena pozycji — LCD-owy „teleport", 1 klatka przejścia).
- Wirtualne przyciski Bréve Deck podświetlają się przy tapnięciu strefy (feedback 1:1).
- Haptyka (Android): złapanie 15 ms, pudło 60 ms, game over 3×80 ms. Wyłączalna w opcjach.
- Brak sterowania grawitacją/swipe — decyzja: prostota klasyka.

## 5. Prezentacja

Urządzenie **Bréve Deck** jest prawdziwym modelem 3D (Blender → FBX, `tools/shell_model.py`): orzechowy
korpus 22×11×1.8 z aluminiową fazą, wgłębiony ekran 60 % szerokości, kopułkowe przyciski, suwak A/B,
grawer marki. Kamera perspektywiczna (FOV 24°) patrzy od strony gracza z pochyleniem 14°; paralaksa
z żyroskopu ±3°. Scena gry jest 2D, renderowana do tekstury na ekranie urządzenia (`LcdCamera`).

### 5.1 Warstwy (sorting layers sceny ekranu)
1. `Background` — malowane tło dioramy (`bg`), lady torów (`plank`), ekspresy na startach
2. `Segments` — kubki, barista, kot, plamy, rozbity kubek, tablica zamówień (nazwa historyczna)
3. `ScreenFX` — winieta szkła, błyski, animacja 999
4. `HUD_TMP` — licznik, zegar, A/B (TextMeshPro)

Obudowa, przyciski i wajcha nie są już sprite'ami — to mesh'e na warstwie `Device`.

### 5.2 Styl ekranu „ART" (domyślny, od M4.6)
- **Diorama w oknie:** ekran to nie wyświetlacz, tylko okno na bar nocnej kawiarni. Tło i lady
  malowane gwaszem (referencje `docs/references/08_gouache_*`), postacie i rekwizyty kreską tuszem
  z płaskim kolorem (`09_ink_*`), poświata bursztynowa zostaje w neonie, HUD-zie i odblasku szkła
  (`10_mono_*`). Mieszanka wybrana przez Michała 2026-09-13.
- Assety: tło, plank lady, ekspres, arkusze Miro (5 póz) i Sablé (2 klatki) z Midjourney (plan
  Standard, prawa komercyjne subskrybenta; prompty 11–15 w `docs/references/PROMPTS.md`), wycinane
  `tools/cut_sheet.py`; kubki w 4 kolorach zamówień, rozbity kubek, plamy, tablica zamówienia —
  wektory w tej samej kresce (`tools/gen_art.py`, styl `ink`). Kubek w locie ma dokładnie kolor
  z §3 (osobne sprite'y, nie tint).
- Ruch 60 fps (tween), postacie zmieniają **pozy skokowo** — bez zmian względem Neo-LCD.
- Bez bloomu i bez duchów segmentów; szkło ekranu (winieta, delikatny odblask) zostaje, bo to szyba okna.
- HUD: font ręczny/kredowy zamiast segmentowego (OFL; wybór po pierwszym zrzucie), kolor `#ffc966`.

### 5.2a Skin ekranu „RETRO" (Neo-LCD, dawny domyślny)
- Przełączany na ekranie tytułowym (toggle obok skinu obudowy), zapisywany w ustawieniach.
  Odblokowanie: 250 pkt w trybie A, razem z Jesionem (propozycja).
- Sprite'y jednokolorowe, segmentowe (`tools/gen_art.py`, styl `lcd`), glow wypalony w PNG + bloom URP
  (intensity 0.15) na warstwach Segments/HUD, kubki tint (biały sprite × kolor zamówienia),
  font DSEG7/DSEG14, opcja „Duchy segmentów" (5 % we wszystkich slotach) tylko w tym skinie.

### 5.3 Audio
- Złapanie: blip 1050 Hz, 40 ms (jsfxr, square, decay krótki); co 25 combo: arpeggio 3 nut.
- Pudło: brzęk ceramiki (SFX generator) + niski buzz 120 ms.
- Kot: miękkie „mrau" 300 ms (rzadko, 30% przejść).
- Tło: lo-fi loop 60–90s, −18 LUFS względem SFX, wyłączalny.
- Game over: opadająca tercja, 600 ms.

## 6. Meta i easter eggi

- **Rekordy:** highscore per tryb (JSON w `Application.persistentDataPath`).
- **Skiny obudowy:** Orzech (start), Jesion (250 pkt A), Onyks (500 pkt B), Neon (999 rollover). Materiały korpusu (drewno/onyks/neonowa obwódka); **skin ekranu** ART/RETRO osobno (§5.2a).
- **Zegar nocny:** na ekranie tytułowym urządzenie pokazuje prawdziwą godzinę; **minutnik parzenia** (1–5 min) z alarmem-blipem — funkcjonalny easter egg.
- Ekran tytułowy = urządzenie z demo attract-mode (AI gra samo, jak stare LCD).

## 7. Checklista anty-plagiatowa (weryfikacja przed release)

| Element 1984 | Night Café | Status |
|---|---|---|
| Wilk (Sojuzmultfilm) | Barista Miro (własny) | ✔ inny |
| Zając w oknie | Kot Sablé z mopem | ✔ inna rola i forma |
| Jajka | Kubki kawy | ✔ inne |
| Kurczaczek po rozbiciu | Plama + kot sprzątający | ✔ inne wyrażenie |
| Kury na rampach | Głowice ekspresów | ✔ inne |
| Brązowo-beżowa obudowa IM-02 | Drewno+aluminium, inne proporcje | ✔ inny trade dress |
| Nazwa „Nu, pogodi!" | „Night Café" / „Bréve" | ✔ inna |
| Cyrylica/sowiecki sznyt | Nocne miasto, neon, lo-fi | ✔ inny klimat |
| Melodie/dźwięki | Własne SFX | ✔ inne |
| Mechanika 4 torów | Zachowana (niechroniona; klasyk sam był klonem Nintendo Egg) | ✔ dozwolone |

Weryfikacja przed wysyłką do sklepu (2026-09-15, build 1.0.0 dev na Pixelu): tytuł, gra A/B, koniec zmiany,
skin RETRO, ikona i splash obejrzane pod kątem powyższej tabeli — bez elementów z 1984. Nazwa pakietu
`com.mikoch81.nightcafe`, marka na obudowie „Bréve Deck" i opisy sklepowe w `docs/store/LISTING.md`
nie odwołują się do pierwowzoru. Do powtórzenia na buildzie release przed tagiem `v1.0.0`.

## 8. Zakres MVP (M1–M2)

MUST: Tryb A, 4 tory, tempo T0–T9, kary, litość kota, oddech, rollover, highscore, haptyka, SFX, obudowa + przyciski wirtualne.
SHOULD: Tryb B, attract-mode, duchy segmentów.
COULD: skiny, minutnik, animacja 999-kot.
WON'T (v1): leaderboardy online, reklamy, IAP.
