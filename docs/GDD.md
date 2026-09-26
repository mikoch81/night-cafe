# Night Café — Game Design Document (v1.0)

Gra mobilna 2D, Unity (URP 2D), Android, orientacja **landscape**. Wirtualny handheld „Bréve Deck" z malowanym ekranem-dioramą. Inspiracja gatunkiem LCD „catch falling objects" — własny świat, postacie i wyrażenie.

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
- Przy 1000 sekretna animacja: neony miasta za oknem układają się w kota (2s); próg litości kota uzbraja się co 1000. Bez odniesień do 1984.
- Od 1.1.0 licznik **nie zeruje się**: po 999 dostaje czwartą cyfrę (testerzy brali 000 za błąd, a rekord powyżej 1000 był nieczytelny).

### 2.8 Poziomy i wydarzenia w kawiarni (od 1.1.0)
- **Poziomy** (uwagi Michała z 2026-09-26: „czuć nudę, jak się czeka tak długo"): poz. 2 od 25 pkt, poz. 3
  od 50, poz. 4 od 100, poz. 5 od 140, dalej co 50 pkt (tryb B: progi ×2 — 50/100/200/280, potem co 100, bo
  kubek daje 2 pkt). `LV n` obok litery trybu, napis `LEVEL n` na pasku i dzwonek przy awansie.
- **Odblokowania:** poz. 2 — kot na drabinie; poz. 3 — trzy wypadki z drabinami; poz. 4 — godzina szczytu.
  Nowo odblokowane wydarzenie pojawia się ~3 s po awansie, a w danej zmianie najpierw losowane są te jeszcze
  niewidziane (kot i trzy drabiny — każde trafi się wcześnie), potem wszystkie po równo; jedno naraz.
  Odstęp 14 s na poz. 2–3, 11 s na poz. 4, 8 s od poz. 5 (**poz. 5 = pełny ogień**). Do poz. 5 godzina
  szczytu wstrzymuje małe wydarzenia, od poz. 5 mogą się nakładać.
- **Cicha chwila** (od poz. 5): co losowe 40–80 pkt (B: 80–160) kawiarnia na 20 pkt (B: 40) zwalnia —
  kubki o 35 % wolniejsze, jeden mniej na ekranie, żadnych nowych wydarzeń; pasek `QUIET SPELL`, potem
  `FULL SPEED` i znów pełne tempo. Nie zaczyna się w godzinie szczytu.
- **Godzina szczytu:** 60 s, kubki o 15 % szybsze (także ponad podłogą tempa T9), +1 kubek na ekranie,
  dzwonek, gwar gości i głośniejszy szum sali, pasek `RUSH HOUR 42`. Pierwsza 10 s po wejściu na poz. 4,
  kolejne 75 s po końcu poprzedniej na poz. 4, 60 s od poz. 5.
- **Trzy koty** (runda 2 uwag, 2026-09-26): **Sablé** (biała, z mopem) tylko sprząta skorupki i śpi po
  zmianie; **Paprika** (rudy, wredny) wyleguje się na taborecie; **Noir** (czarny) szturcha taborety.
- **Rudy na taborecie:** Paprika wbiega i wskakuje na jeden z taboretów (zawsze nie na ten, na którym stoi
  Miro) i się rozkłada; pierwsze naciśnięcie tego przycisku go przegania — Miro podchodzi pod taboret
  (dolna pozycja tej strony) i trzaska ręcznikiem, kot leci z obrotem na zewnątrz, ląduje i ucieka; dopiero
  drugie naciśnięcie wprowadza Miro. Sam schodzi po 12 s.
- **Drabiny (Noir):** każdy wypadek zaczyna się od Noira, który skrada się z boku ekranu do taboretu i
  szturcha go barkiem — skutek następuje w chwili szturchnięcia:
  - **drżenie** — tylko pod Miro (losowane wyłącznie, gdy Miro stoi na taborecie; jeśli w chwili
    szturchnięcia stoi na celowanym taborecie, każdy wypadek staje się drżeniem). Drży 6 s; kto stoi na nim
    2 s bez przerwy, spada: plama, stłuczony kubek z tacy, Miro ląduje niżej, taboret leży **połamany na
    kawałki** 10 s — na jego pozycję nie da się wejść, a z tego toru nie lecą wtedy kubki;
  - **krzywy taboret** — 12 s wejście na tę pozycję trwa 0,45 s (Miro wspina się szczebel po szczeblu; w
    tym czasie nie łapie). Drżenie celowane w Miro, który zdążył zejść, kończy się krzywym taboretem;
  - **przewrócony taboret** — leży **w całości**, **do środka**, ukośnie: siedzisko na podłodze, nogi
    uciekają w głąb sali (skrót perspektywiczny, przyciemniony, za drugim taboretem); dwa naciśnięcia jego
    przycisku (Miro podchodzi i dźwiga: do połowy, postawienie); kubki z toru lecą dalej. Po 20 s kawiarnia
    sama go stawia. Złamany (w kawałkach, też do środka): naciśnięcie = Miro kręci głową.
  - **Jak Noir to robi** (runda 3 uwag): krzywy taboret — szturchnięcie barkiem; przewrócenie — staje na
    tylnych łapach i pcha przednimi; drżenie — odwraca się tyłem i smaga taboret ogonem.
- **Układ:** Miro na dolnych pozycjach stoi obok taboretu (x ±1.6), nie przed nim; dolne rampy kończą się
  w K5 x ±2.35, pod jego tacą. Rampy (deski) kończą się w K5 (wcześniej po podmianie sprite'a wystawały
  o ~1 jednostkę i kubki „spadały przed końcem").
- **Straszna dziesiątka** (od poz. 5; runda 3 uwag: „niektórym testerom brakuje emocji"): jeden losowy
  ekspres szaleje przez **10 s**: drży, rozgrzewa się, bucha parą, syczy; gdy jego tor opustoszeje, wypuszcza
  kubki seriami (nowy, gdy poprzedni jest 2 kroki dalej); pozostałe trzy ekspresy mają w tym czasie łącznie
  najwyżej 1 kubek na ekranie. Pasek `TERRIBLE TEN 7` / `STRASZNA DZIESIĄTKA 7`. W trakcie nic innego się
  nie zaczyna. **Narasta z poziomem** (propozycja z 2026-09-26): kubki szybsze o 35 % na poz. 5, 40 % na 6,
  45 % na 7, 50 % od 8; przerwa 75 s na poz. 5, −5 s na poziom, min. 50 s; pierwsza 15 s po wejściu na
  poz. 5. Zegar tyka poza godziną szczytu i chwilą spokoju; gdy wybije w trakcie kota/Noira, nowe
  wydarzenia czekają, a dziesiątka wchodzi, gdy tylko się uspokoi. Po niej 5 s oddechu, zanim wrócą koty;
  chwila spokoju nie zaczyna się w trakcie. **Tryb B:** szalejący ekspres parzy tylko zamówiony kolor, a
  zamówienie nie zmienia się do końca dziesiątki (złapania z tego czasu liczą się potem). Liczby w
  `ModeConfig` (sekcja „The terrible ten seconds”), wspólne dla A i B.
- **Tryb B** dostaje wszystkie wydarzenia jak A, przy tej samej liczbie kubków: progi poziomów i chwile
  spokoju w punktach ×2 (kubek = 2 pkt), wydarzenia liczone w sekundach bez zmian.
- **Dymki Miro:** przy każdym wydarzeniu Miro coś mówi w dymku w pasie pod tablicą wyniku (tam nie jeżdżą
  kubki). Dwa pierwsze razy w zmianie — krótka podpowiedź, co zrobić („Noir tipped my stool! Tap its top
  button twice."), potem żarty bez powtórki pod rząd; reakcje (przegnany kot, upadek, naprawa, koniec szczytu,
  cicha chwila) od razu żartem. Teksty w `Services/BaristaQuips.cs`.
- Wydarzenia nie występują w demie. Liczby w `ModeConfig` (sekcja „Levels and café events”).

### 2.9 Języki (od 1.1.0)
- Angielski i polski; przy pierwszym uruchomieniu język telefonu (polski → PL, każdy inny → EN), potem
  wybór z menu (wiersz `LANGUAGE EN` / `JĘZYK PL`), zapamiętany. Wszystkie teksty w `Core/Loc.cs`, kwestie
  Miro w `Services/BaristaQuips.cs`. Napisy na tablicy wyniku (Cabin Sketch, bez polskich znaków) są ASCII:
  `REKORD`, `POZ`; reszta (Patrick Hand) ma pełne polskie znaki.

## 3. Tryby

- **Tryb A (Zmiana dzienna):** wszystkie kubki łapiemy. Zasady jak wyżej.
- **Tryb B (Zamówienia):** nad barem panel zamówienia z kolorem kubka (tint: espresso `#ffc966`, karmel `#ff9d6e`, mleczny `#ffe9a8`, deka `#d98cff`).
  - Kubki spawnują się w losowych kolorach (kolor zamówienia ma wagę 55%, pozostałe po 15%).
  - Złap właściwy kolor: +2 pkt. Złap zły: **kara jak pudło** (plama). Przepuść zły kolor: nic (to poprawne zachowanie). Przepuść dobry: pudło.
  - Zamówienie zmienia się co 10 złapanych właściwych lub co 20s (co pierwsze).
  - Start od T2, przyspieszanie T+1 co 10 złapań.

## 4. Sterowanie

- **4 przyciski** na obudowie = 4 pozycje baristy. Od 1.1.0 liczy się tylko dotyk w nakładkę (do pierścienia wokół niej) — tap w inne miejsce obudowy nic nie robi (wcześniej ćwiartki całego ekranu telefonu). Tap = przeskok baristy na tę pozycję (natychmiast, bez tweena pozycji — LCD-owy „teleport", 1 klatka przejścia). Ekran tytułowy startuje zmianę tapnięciem w dowolne miejsce.
- **MENU** (pastylka pod prawym dolnym przyciskiem; także systemowe „Wstecz”): pauza (zamrożony czas gry), muzyka, dźwięk, tryb A/B (w trakcie zmiany kończy ją i zapisuje wynik), top 10, zakończ zmianę, wyjdź z gry. Zejście aplikacji w tło w trakcie zmiany otwiera MENU.
- Ekran końca zmiany: tap = restart, dźwignia = tryb + ekran tytułowy, bez dotyku powrót do tytułu po 4 s.
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
- Ruch 60 fps (tween), postacie zmieniają **pozy skokowo**.
- Bez bloomu i bez duchów segmentów; szkło ekranu (winieta, delikatny odblask) zostaje, bo to szyba okna.
- HUD: font ręczny/kredowy zamiast segmentowego (OFL; wybór po pierwszym zrzucie), kolor `#ffc966`.

### 5.2a Skin ekranu „RETRO" — usunięty w 1.1.0
- Segmentowy Neo-LCD (dawny domyślny) wypadł po teście zamkniętym razem z opcją duchów segmentów i
  chiptune'em z `gen_audio.py`. Wektorowe sprite'y z `Assets/Art/sprites` zostają jako zapas dla
  brakujących plików dioramy i dla animacji neonowego kota.

### 5.3 Audio
- Złapanie: blip 1050 Hz, 40 ms (jsfxr, square, decay krótki); co 25 combo: arpeggio 3 nut.
- Pudło: kubek spada z końca lady na podłogę (~0,4 s) i dopiero tam brzęk ceramiki, plama i kot (od 1.1.0).
- Kot: miękkie „mrau" 300 ms (rzadko, 30% przejść).
- Tło: lo-fi loop 60–90s, −18 LUFS względem SFX, wyłączalny.
- Game over: opadająca tercja, 600 ms.

## 6. Meta i easter eggi

- **Rekordy:** highscore per tryb i lokalna lista **top 10** per tryb z datą (JSON w `Application.persistentDataPath`, schemat 2; profil z 1.0.0 dostaje swój rekord jako pierwszy wpis). Karta końca zmiany pokazuje miejsce na liście.
- **Skiny obudowy:** Orzech (start), Jesion (250 pkt A), Onyks (500 pkt B), Neon (999 rollover). Materiały korpusu (drewno/onyks/neonowa obwódka).
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
