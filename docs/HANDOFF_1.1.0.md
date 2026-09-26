# Night Café 1.1.0 — przekazanie sesji (stan na 2026-09-26, wieczór)

Plik dla następnej sesji Claude Code i dla Michała. Opisuje rundę poprawek po teście zamkniętym
(wersja 1.1.0): co ustalono, co zrobiono, co jest w toku i co dalej. Ogólna historia projektu:
[CLAUDE_CODE_HANDOFF.md](CLAUDE_CODE_HANDOFF.md), zasady gry: [GDD.md](GDD.md) (§2.7, §2.8, §4, §5.2a, §6
już zaktualizowane pod 1.1.0).

## 1. Skąd ta runda

Testerzy zamkniętego testu (1.0.0 w Play: Internal + Closed „Alpha”) zgłosili 2026-09-26:

1. filiżanki powinny spadać na samą ziemię i tam się tłuc,
2. brak pauzy / wznowienia (przycisk Pause/Menu z boku handhelda),
3. boczne przyciski mają być zmapowane precyzyjnie (dotyk obok nie może wywoływać akcji),
4. wyrzucić z projektu wersję RETRO,
5. brak zapisywania wyników,
6. za długie czekanie po końcu gry na menu (zmiana A/B itd.); w trakcie gry menu z dźwiękiem/muzyką,
7. przy liczniku 999 „coś się psuje”,
8. brak levelowania i akcji na ekranie: (a) łamiące się drabiny utrudniające zbieranie, (b) „godzina
   szczytu” — przyspieszenie na ~minutę z sygnałem, (c) kot wskakuje na drabinę i trzeba go przegonić,
9. ranking online top 10 testerów,
10. inne propozycje ulepszeń mile widziane,
11. na końcu pomoc z wrzuceniem do Google Play.

Dopisane przez Michała w trakcie: **brak wyjścia z gry** (użytkownicy ubijali aplikację).

## 2. Decyzje Michała (nie pytać ponownie)

| Temat | Decyzja |
|---|---|
| Ranking online | **Google Play Games Services** (leaderboardy Google, logowanie kontem Play) |
| Licznik po 999 | **4 cyfry, bez zerowania**, gra trwa dalej, rekordy >1000; kot z neonów przy 1000 zostaje jako nagroda |
| Przycisk MENU | **na obudowie, prawy dół**, pod prawym dolnym przyciskiem, nad kratką głośnika; + „wyjdź z gry” |
| Tryb pracy | **etapami, build na telefon po każdym**: (1) poprawki, (2) wydarzenia + poziomy, (3) ranking, potem Play |
| Drabiny | **wszystkie trzy warianty**, losowane w gameplayu (trzeszcząca→łamie się pod Miro; krzywa→wolne wejście; przewrócona→naprawa naciśnięciami) |
| Poziomy | **co 50 pkt**; po 4. poziomie wydarzenia coraz częściej, **ostrożnie z balansem late game** |
| Dźwięki | **ElevenLabs** (konto Michała, płatny plan) |
| Po etapie 1 | przycisk MENU za mały → powiększony; skorupki nie mają znikać same — **zbiera je kot** (zrobione) |

## 3. Gałąź i commity

- Gałąź robocza: **`release/1.1.0`** (utworzona z `master` @ `2f796ee`). `master` bez zmian do wydania.
- `6615732` — etap 1 (commit, przetestowany przez Michała na Pixelu: „etap 1 działa wszystko dobrze”).
- **Etap 2 — NIESCOMMITOWANY.** Pierwsza runda uwag Michała naniesiona (§5 „Uwagi Michała do etapu 2");
  czeka na test na telefonie i akceptację.
  Commit po naniesieniu uwag i akceptacji, w stylu: `feat(1.1.0): stage 2 - levels, rush hour, ladder
  mishaps, cat on the ladder`, z linią `Co-Authored-By` z aktualnego przypomnienia systemowego.

## 4. Etap 1 — zrobione (commit 6615732, zaakceptowane)

- **Przyciski precyzyjne:** dotyk nie jest już ćwiartką ekranu. `LaneInput` wysyła tylko pozycję;
  `GameLoopController.ResolveDeviceButton` robi raycast z `DeviceCamera` w zderzacze nakładek
  (`SphereCollider` na `Cap_*`, promień ×`CapHitScale` 1.2 = do aluminiowego kołnierza) i pastylki MENU.
  Dotyk obok nie robi nic w grze; na ekranie tytułowym tap gdziekolwiek startuje zmianę. Klawiatura:
  W/S, ↑/↓, Spacja/Enter, **Esc/M = MENU** (Esc = systemowe „Wstecz” na Androidzie).
- **Pastylka MENU** (budowana w `Setup.Device.cs` z prymitywów Unity, bo Blendera nie ma na tym PC):
  środek (9.0, −3.35, −1.8) w świecie urządzenia, długość 1.5, szerokość 0.62 (powiększona po teście),
  zderzacz szerszy (promień 0.62, długość 1.95), grawer „MENU” (TMP, kolor inkrustacji).
  Animacja przycisków w `DeviceShellView` liczona w czasie nieskalowanym (działa w pauzie).
- **Menu** (`UI/PauseMenuView.cs`, karta z papieru paragonu 6.8×8.5 + ciemna zasłona, warstwa HUD
  order 40–50): RESUME/CLOSE, MUSIC, SOUND (osobno), MODE A/B (w trakcie zmiany kończy ją, zapisuje wynik,
  przechodzi na tytuł w nowym trybie), TOP 10 (druga strona z listą i BACK), END SHIFT (tylko w zmianie →
  ekran końca), QUIT GAME (`Application.Quit`). Pauza = `Time.timeScale = 0` (cała gra na czasie skalowanym).
  Zejście aplikacji w tło w trakcie zmiany (`OnApplicationPause/Focus`) otwiera menu. W demie MENU wraca na tytuł.
- **Filiżanki spadają:** `CupController.Drop` (grawitacja 30, prędkość boczna 1.5, obrót 420°/s) z końca
  lady na `LaneConfig.barLineY`; `LaneSpawner.CupLanded` → `GameLoopController.BreakCup` (skorupki, dźwięk,
  haptyka, kot, plama w chwili uderzenia). Spadające kubki nie liczą się do limitów toru ani pilota dema.
- **Skorupki zbiera kot:** `TimedSpriteFx.Show(pos)` bez limitu czasu; `SweepBrokenCups` chowa skorupkę,
  gdy mop (`CatCrossingView.MopX` = pozycja kota + 0.35) ją minie w tej klatce; jeśli skorupka upadła za
  kotem, kot przechodzi ponownie. Po końcu zmiany skorupki zostają przy śpiącym kocie.
  (`ModeConfig.brokenCupDuration` jest teraz nieużywane.)
- **Licznik:** `ScoreService.DisplayScore` = pełny wynik (4+ cyfry), `HudView.SetBest(total)` bez modulo;
  rollover (1000) nadal odpala neonowego kota, odblokowanie skina Neon i ponowne progi litości.
- **Lista top 10:** `ProfileService.SubmitScore(mode, score, date, out rank)`, `TopScores(mode)`;
  `ProfileData` schemat **2** (`topScores`: mode/score/date yyyy-MM-dd); profil 1.0.0 dostaje swój rekord
  jako pierwszy wpis (bez daty). Remis — starszy wpis wyżej; wynik 0 nie trafia na listę.
  Karta końca: `#3 ON LIST · BEST 1050`.
- **Koniec gry:** powrót do tytułu po **4 s** (było 6); dźwignia A/B działa na ekranie końca (zmienia tryb
  i przechodzi na tytuł); MENU działa wszędzie.
- **RETRO usunięte:** styl `ScreenStyle_Retro.asset`, przełącznik, „duchy segmentów”, bloom (wolumen LCD
  ma `weight = 0`), pola `monochrome/bloom/ghostsAllowed`, ustawienia `RetroScreen/Ghosts`, chiptune
  `Assets/Audio/*.wav` i `tools/gen_audio.py`. Bazowy `AudioConfig.asset` trzyma tylko poziomy i haptykę.
  Wektorowe sprite'y `Assets/Art/sprites` zostają (zapas dla brakujących plików dioramy + neonowy kot).
  Karteczki na tytule: ♪ (muzyka), SFX, ~ (wibracje), skórka — 4 zamiast 5.
- **Wersja 1.1.0, versionCode 2** (`NightCafeSetup.ApplyProjectSettings`, test `ReleaseSettingsTests`).
- Dokumenty: README, GDD, `docs/store/LISTING.md` (punkty EN/PL + szkic release notes 1.1.0).

## 5. Etap 2 — zrobione w kodzie, NIESCOMMITOWANE, na Pixelu do testu

Wszystko przetestowane jednostkowo (174/174 EditMode) i wizualnie w edytorze (każde wydarzenie wymuszone
i obejrzane na zrzutach). Michał jeszcze nie przekazał uwag z telefonu.

### Poziomy
- `EventSettings.LevelFor(total) = 1 + total / levelEvery`; **A: 50 pkt, B: 100 pkt** (w B kubek = 2 pkt,
  więc ta sama liczba kubków na poziom — Michałowi to zaznaczono; jeśli chce dosłownie 50 w B, zmienić
  `config.levelEvery = 100` w `Setup.Configs.CreateModeConfigB`).
- HUD: `LV n` (`LevelText`, (3.25, 3.55), font liter), pasek `Banner` (ciemne szkło 7.4×1 na y 2.55) z
  `LEVEL n` przez 1.8 s i dźwiękiem `sfx_level_up`; pasek pokazuje też `RUSH HOUR 42`.

### Wydarzenia (`Services/ShiftEvents.cs`, czysta logika)
- `ShiftEventDirector.Tick(dt, level, smallEventActive, rushActive)` — tylko w `Playing`, nie w oddechu
  ani w demie. Odblokowania: **poz. 2 kot, poz. 3 godzina szczytu, poz. 4 drabiny**. Małe wydarzenia
  (kot + 3 drabiny) po równo, jedno naraz; odstęp liczony od startu, tyka tylko gdy nic nie trwa.
- Odstępy: `eventGap` 20 s, od poziomu `eventsSpeedUpFromLevel` 4 −`1 s`/poziom, minimum **11 s**.
  Godzina szczytu: pierwsza po `firstRushDelay` 8 s na poz. 3, trwa **60 s**, kolejna **90 s po końcu**
  poprzedniej (od poz. 5 −4 s/poziom, min. **60 s**). Do poziomu **8** (`eventsOverlapFromLevel`) rush
  wstrzymuje małe wydarzenia, od 8 mogą się nakładać.
- **Godzina szczytu:** `TempoService.StepFactor` 0.85 (także poniżej podłogi T9: 0.405→0.344 s/krok),
  `LaneSpawner.ExtraCups` +1, dzwonek `sfx_rush_bell`, `AudioService.SetRushHour(true)`: szum sali +6 dB
  i losowe klipy gwaru `sfx_crowd_1..4` co 1.2–2.6 s (−8 dB; czas nieskalowany, pomija pauzę), mruganie neonu.
- **Drabiny** (`LadderService`, strona 0 = lewy taboret pod LeftUp, 1 = prawy pod RightUp):
  - Trzeszcząca (`Creaking`, 6 s): celuje w drabinę Miro, jeśli na jakiejś stoi. Stanie na niej **2 s bez
    przerwy** → `Broken`: Miro spada na dolną pozycję, miss pose, skorupki z tacy, plama, kot, dźwięk
    złamania. Złamana 10 s: nie da się wejść (`Blocked`), **tor nie dostaje nowych kubków** (`LaneClosed`).
  - Krzywa (`Wobbly`, 12 s): wejście trwa `ladderClimbDelay` 0.45 s (`PlayerPositionController.BeginClimb`,
    szczebel po szczeblu); w trakcie wspinania Miro **nie jest na żadnej pozycji** (nie łapie).
  - Przewrócona (`Toppled`): `ladderRepairPresses` 2 naciśnięcia (pierwsze podnosi do 45°), kubki lecą dalej;
    po 20 s kawiarnia sama ją stawia.
  - Kot (`CatOn`, 12 s): Sablé śpi na taborecie (klatka `catAsleepA`, 0.55 rozmiaru z końca zmiany);
    pierwsze naciśnięcie = `Shoo` (prychnięcie, kot zeskakuje), drugie wprowadza Miro; sama schodzi po 12 s.
  - Wypadki inne niż trzeszczenie trafiają w **drugą** drabinę niż ta, na której stoi Miro.
- Widok: `Gameplay/LadderView.cs` (po jednym na taboret, `Props/Ladder_0|1` z dzieckiem `LadderCat`):
  drżenie, przechył, leżenie na podłodze **na zewnątrz** (pod ekspresami), ciemniejszy odcień gdy złamana.
  Dom taboretu zapamiętywany przy pierwszym wydarzeniu (aplikator stylu ustawia go na starcie).
- Liczby w `ModeConfig` → sekcja **„Levels and café events (1.1.0)”** (`Events` → `EventSettings`).
- Pliki etapu 2: `Services/ShiftEvents.cs` (nowy), `Gameplay/LadderView.cs` (nowy), `Tests/EditMode/
  ShiftEventTests.cs` (nowy, 11 testów), zmiany w `GameLoopController` (sekcja „café events”),
  `ModeConfig`, `TempoService`, `SpawnDirector` (przeciążenie z `extraCups`/`laneClosed`), `LaneSpawner`,
  `PlayerPositionController`, `HudView`, `AudioConfig` (nowe `GameSfx` 7–12, `rushCrowd[]`), `AudioService`,
  `Setup.Scene(.Hud).cs`, `Setup.Configs.cs`, `Setup.ScreenStyle.cs`, `tools/prep_audio.py` (efekty +
  `crowd()`), `art/audio/LICENSE.md`, `docs/GDD.md` §2.8.

### Dźwięki (ElevenLabs)
- Flow **„Night Café 1.1.0 SFX”** (`uhkiGVYqNjM0YTPPBf1C`) na koncie Michała, model
  `eleven_text_to_sound_v2`, po 2 warianty; koszt ok. 5 centów. Model sam wybiera długość (1–3 s),
  pętli dłuższej nie da się wymusić — stąd losowe klipy gwaru zamiast pętli.
- Surowe pliki: `art/audio/raw/sfx_rush_bell|ladder_creak|ladder_break|ladder_knock|cat_hiss|level_up.wav`,
  `ambience_crowd[2-4].wav`; gotowe: `Assets/Audio/Art/sfx_*` i `sfx_crowd_1..4.wav`
  (`py tools/prep_audio.py effects crowd`; stare efekty wychodzą bajt w bajt takie same).

### Uwagi Michała do etapu 2 (2026-09-26, wieczór) — naniesione, NIESCOMMITOWANE
Uwagi: (1) kot na taborecie bez animacji zrzucenia i wskoku, brak reakcji Miro; (2) poziomy za długie,
nuda — progi 25/50/100/140, od poz. 5 „pełny hard", czasem losowe zwolnienie na 20 pkt; (3) drabiny
pojawiały się dopiero na poz. 4–5 — mają od 3; (4) do każdego wydarzenia dymek Miro z krótką podpowiedzią;
(5) więcej ruchu Miro przy wydarzeniach; (6) przy MENU dopisać PAUSE. Zrobione (GDD §2.8 przepisane):
- **Poziomy:** `ModeConfig.levelThresholds` {25, 50, 100, 140} + `levelEvery` 50 dalej (B: {50,100,200,280},
  100 — ta sama liczba kubków; `Setup.Configs.CreateModeConfigB`). `EventSettings.LevelFor` liczy progi.
- **Odblokowania:** kot 2, **drabiny 3**, godzina szczytu 4; odstęp 14 → 11 → 8 s (min. od poz. 5), szczyt
  75 → 60 s, nakładanie od poz. 5. `ShiftEventDirector`: po awansie odblokowującym coś nowego następne
  wydarzenie za `eventIntroDelay` 3 s; w zmianie najpierw niewidziane (kot + 3 drabiny poznane w 4 pierwszych).
- **Cicha chwila** (`QuietSpellDirector`, od poz. 5): co losowe 40–80 pkt, na 20 pkt: `StepFactor` 1.35,
  `ExtraCups` −1, brak nowych wydarzeń; pasek QUIET SPELL / FULL SPEED. Liczona w punktach, nie w czasie.
- **Dymki Miro:** `UI/SpeechBubbleView` (papier 9-slice + ogonek z `tools/gen_bubble.py` →
  `screen_v3/speech_bubble.png`, `speech_tail.png`), tekst Patrick Hand w kolorze `cardInk`. Stoi w pasie
  y 1.7–3.05 pod tablicą wyniku, odsunięty od głowy ku środkowi — nie zasłania kubków ani taboretów.
  Teksty: `Services/BaristaQuips.cs` — 2 pierwsze razy w zmianie podpowiedź, potem żarty (bez powtórki pod
  rząd); reakcje od razu żartem. Tylko w żywej zmianie (nie demo).
- **Kot:** `LadderView` — rozbieg po podłodze + skok na taboret (klatki chodu `sable_a/b`, 0.8 rozmiaru z
  końca zmiany), drzemka z oddechem; przegoniony (`ShooCat`) leci z obrotem na zewnątrz, ląduje i ucieka
  za ekran; znudzony zeskakuje i odchodzi spacerem.
- **Miro** (`PlayerPositionController`, warstwa akcji nad pozycją — slot bez zmian): `Shoo` (macha, poza
  „taca w górę" `miro_up` = nowy `ScreenStyle.baristaReach`), `Lift` (przysiad + dźwignięcie), `Fall`
  (upadek z taboretu z obrotem i odbiciem), `Shrug` (złamana drabina), kołysanie przy wspinaczce.
  **Zmiana zasad:** przeganianie kota i podnoszenie drabiny przenoszą Miro na dolną pozycję tej strony
  (stoi przy taborecie) — do potwierdzenia przez Michała na telefonie.
- **Obudowa:** napis pod pastylką „MENU · PAUSE" (`Setup.Device.BuildMenuButton`).
- Testy: 179/179 EditMode (nowe: progi, odblokowanie z wstępem, „najpierw niewidziane", cicha chwila,
  kwestie Miro). Wszystkie animacje obejrzane na zrzutach w edytorze (czas ×0.2).
- Znane: gdy w tej samej chwili kot sprząta skorupki, na ekranie są dwie Sablé (sprzątająca i na taborecie).

### Uwagi Michała do etapu 2, runda 2 (2026-09-26, noc) — naniesione, NIESCOMMITOWANE
Uwagi: **nie budować APK przed końcem etapu**; Miro ma przeganiać kota ręcznikiem; kot na taborecie
był tą samą Sablé co sprząta (z mopem, podwajała się) — nowy rudy wredny kot na taboret, biała zostaje do
sprzątania; drżenie i przewracanie robi trzeci, czarny kot, drżenie tylko gdy Miro stoi na drabinie;
drabiny nachodziły na końce ramp, Miro na dole nachodził na drabinę, kubki spadały przed końcem ramp;
drabina złamana po drżeniu ma leżeć w kawałkach (przewrócona — w całości). Zrobione (GDD §2.8):
- **Grafika** (Higgsfield GPT Image 2.5, 3 generacje × 2,75 kr., ref. stylu = nasze arkusze Midjourney;
  `art/generated/` + LICENSE, prompty 20–22 w PROMPTS.md): Paprika (kłus ×2, leżenie, skok), Noir
  (skradanie ×2, szturchnięcie; jasna obwódka jak u Sablé), Miro z ręcznikiem ×2. `tools/finish_cast.py`
  skaluje, przygasza rudego, obrysowuje czarnego, liczy pivoty (`SpriteAnchors`). Połamany taboret =
  wektor `step_broken` w `tools/gen_art_v3.py`. Nowe sloty `ScreenStyle` (sekcja „Café troublemakers”),
  ładowane opcjonalnie (`Setup.ScreenStyle.ArtCastSprites`) — bez plików gra wraca do Sablé.
- **Noir** (`Gameplay/BlackCatView`): skrada się z zewnętrznej strony do taboretu, szturcha
  (`Bumped`) i odchodzi. `GameLoopController` trzyma wypadek jako `_pendingBump` i stosuje go przy
  szturchnięciu przez `LadderService.BumpOutcome` (Miro na tym taborecie → drżenie; drżenie bez Miro →
  krzywy). `ShiftEventDirector.Tick(..., baristaUp)` nie losuje drżenia, gdy Miro jest na dole.
  `LadderService.PickSide/StartOn` rozdzielone.
- **Paprika** w `LadderView` (`SetCat` z klatkami leżenia/kłusa/skoku), stan `Broken` rysuje
  `step_broken` na podłodze (`SetBroken`), `Toppled` dalej obraca cały taboret.
- **Miro:** akcja `Shoo` przełącza pozy ręcznika A/B (`ScreenStyle.baristaShooA/B`).
- **Rampy:** `ScreenStyleApplier` gubił długości desek — podmiana sprite'a resetuje rozmiar 9-slice do
  natywnych 6.21 j. (miało być 4.33); teraz długości z setupu są zapamiętane (`_plankLengths`).
- **Układ:** `LaneConfig` — dolne pozycje Miro x ±1.60 (było ±1.16, jak taboret), dolne K5 x ±2.35
  (było ±1.95), zgięcie ±3.39/−1.19. Taca trafia w koniec rampy (sprawdzone na zrzutach).
- Testy 181/181 (nowe: Noir tylko z Miro na górze, wynik szturchnięcia, `StartOn`). Wszystko obejrzane na
  zrzutach z trybu gry (czas ×0.1–0.5). APK **nie** budowany (uwaga Michała).

### Uwagi Michała do etapu 2, runda 3 (2026-09-26, noc) — naniesione, NIESCOMMITOWANE
Uwagi: taborety mają upadać do środka, ukośnie, w perspektywie; Noir przewraca na tylnych łapach, a trzęsie
ogonem; imiona Paprika/Noir OK; polska wersja; od poz. 5 „straszne 10 sekund" — jeden ekspres paruje i
wyrzuca filiżanki w super tempie („niektórym testerom brakuje emocji"). Zrobione (GDD §2.8, nowy §2.9):
- **Leżący taboret** (`LadderView.LayDown`): do środka, 108° od pionu (siedzisko na podłodze, nogi w górę
  ekranu = w głąb), długość ×0.62, +0.14 w górę podłogi, przyciemniony, `sortingOrder` 27 (za drugim
  taboretem i Miro); kawałki złamanego też do środka. Miro dźwiga w stronę środka (`Lift(-Outward)`).
- **Noir** (generacja 23 z referencją jego arkusza, 2,75 kr.): `noir_rear`, `noir_tail_a/b`.
  `BlackCatView` pyta po dojściu `DecideShove` (GameLoop liczy `BumpOutcome` w tej chwili) → `NoirShove`
  Shoulder / Rear / Tail; cios wywołuje `Bumped` (ogon: pierwszy smagnięcie, potem 1,6 s smagania).
- **Straszna dziesiątka** (`ShiftEvent.MachineFrenzy`): zegar w `ShiftEventDirector` (tylko gdy spokojnie,
  wstrzymuje wszystko w trakcie, `FrenzyEnded`), `LaneSpawner.StartFrenzy/StopFrenzy` (własny strumień
  kubków na torze z `stepTime × 0.55`, najpierw czeka aż tor opustoszeje; inne tory ≤1 kubek),
  `Gameplay/SteamFx` (16 obłoków z `tools/gen_steam.py`, drżenie i rozgrzanie głowicy), dźwięk
  `sfx_machine_frenzy` (ElevenLabs, ten sam flow; co 2,2 s), pasek z odliczaniem (`HudView.SetCountdown`),
  kwestie `FrenzyStart/End`. Liczby: `ModeConfig` „The terrible ten seconds”.
- **Polski:** `Core/Loc.cs` (enum `Txt`, tabele EN/PL, skórki i zamówienia), `SettingsService.Language`
  (pierwszy start = język telefonu, `ToggleLanguage`, klucz `nightcafe.polish`), wiersz `JĘZYK PL` w menu
  (8 wierszy co 0,8), `HudView.Relocalize` (+ `titleText`), `BaristaQuips` w obu językach. Cabin Sketch nie
  ma polskich znaków → na tablicy ASCII (`REKORD`, `POZ`). Na tym PC system jest polski — edytor startuje
  po polsku. Notatki wydania 1.1.0 w `LISTING.md` uzupełnione.
- Testy 187/187 (`LanguageTests`, straszna dziesiątka). Pułapka: pierwszy plik testów (`LocalizationTests.cs`)
  Unity nie dopisał do zestawu mimo odświeżania — pomogło usunięcie przez AssetDatabase i utworzenie pod
  nową nazwą. Wszystko obejrzane na zrzutach (PL i EN). APK nie budowany.

### Runda 4 (2026-09-26, noc): moja propozycja trudności + tryb B — NIESCOMMITOWANE
- Straszna dziesiątka narasta: `ModeConfig.FrenzyStepFactorAt` (0.65 → −0.05/poziom → min 0.5),
  `EventSettings.FrenzyGapAt` (75 s → −5 s/poziom → min 50 s); `frenzyBreather` 5 s po niej
  (`ShiftEventDirector.FrenzyEnded(level)`); gdy wybije w trakcie kota/Noira — `_frenzyDue` wstrzymuje nowe
  małe wydarzenia i odpala ją po nich (wcześniej zegar tykał tylko w pełnym spokoju i mogła nie przyjść);
  chwila spokoju nie startuje w trakcie (`_quiet.Tick(..., _rush || _frenzy)`).
- Tryb B: `OrderService.Held` (bez rotacji zamówienia w trakcie; złapania liczą się po zwolnieniu),
  kubki szalejącego ekspresu malowane na zamówiony kolor (`PaintCup`). `ModeConfigAssetsTests` pilnuje, że
  B ma progi/chwile spokoju ×2 i te same wydarzenia. Obejrzane na zrzucie (B, seria espresso).
- Testy 190/190.

### Etap 2 zamknięty
Commit `fdfc9f9` (2026-09-26, wypchnięty na `release/1.1.0`); APK etapu 2 zainstalowany na Pixelu.

## 5a. Etap 3 — ranking online (w toku, NIESCOMMITOWANY)
- Wtyczka **Google Play Games 2.2.1** (Apache 2.0, z `current-build` repo playgameservices) + EDM4U 1.2.182:
  `Assets/GooglePlayGames`, `Assets/ExternalDependencyManager`, `Assets/Plugins/Android`
  (`mainTemplate.gradle` z `play-services-games-v2:22.0.0` i `play-services-nearby:18.5.0` — Force Resolve,
  `gradleTemplate.properties`, `GooglePlayGamesManifest.androidlib`), `ProjectSettings/GvhProjectSettings.xml`,
  `GooglePlayGameSettings.txt`. Zagnieżdżoną starą paczkę 2.2.0 z `current-build` usunięto.
- Kod: `Services/OnlineScores.cs` (interfejs `IOnlineScores`, `OfflineScores`), `Scripts/PlayGames/`
  (osobny asmdef Android+Editor: `PlayGamesScores` instaluje się przed sceną tylko na urządzeniu i tylko gdy
  jest app id i `PlayGamesIds` — inaczej gra zostaje offline). Ciche logowanie w `Start`, wynik wysyłany w
  `EnterGameOver` (i przy zmianie trybu w trakcie zmiany), wiersz menu `ONLINE TOP 10` / `RANKING ONLINE`
  (9 wierszy co 0,72; wyszarzony bez konfiguracji; niezalogowanego najpierw prosi o logowanie).
- Manifest: zdejmujemy już tylko `ACCESS_LOCAL_NETWORK`; `INTERNET` i `ACCESS_NETWORK_STATE` zostają.
  `docs/privacy.md` przepisane (obowiązuje od 1.1.0 — na `master` dopiero z wydaniem), `LISTING.md`, `README`.
- **Czeka na Michała:** konfiguracja w Play Console wg `docs/store/PLAY_GAMES_SETUP.md` (odciski SHA-1
  upload i debug są tam wpisane; app signing trzeba odczytać w konsoli) i plik zasobów XML →
  `docs/store/games-ids.xml` → menu **NightCafe/Apply Play Games Resources** (`Editor/PlayGamesSetup.cs`:
  setup wtyczki + `PlayGamesIds`). Potem test na Pixelu (APK deweloperski — klucz debug musi być w
  danych logowania), Data safety i IARC w konsoli, publikacja konfiguracji usług gier z wydaniem.

## 6. Etap 3 i wydanie — do zrobienia

- **Ranking Google Play Games:** plugin Play Games Services dla Unity, konfiguracja w Play Console
  (projekt gier, leaderboardy A i B, ekran zgody OAuth, SHA-1 kluczy: upload + Play App Signing),
  logowanie przy starcie (ciche), wysyłanie wyniku przy końcu zmiany, pozycja „ONLINE TOP 10” w menu.
  **Zmienia prywatność:** `docs/privacy.md`, deklaracja Data safety w Play Console, `LISTING.md`
  (obecnie „No ads, no accounts, no internet, no data collected”), uprawnienie INTERNET w manifeście
  (`AndroidManifestPostProcessor.cs`), test `ReleaseSettingsTests` (sprawdza uprawnienia).
- **Wydanie 1.1.0 w Play:** release AAB `powershell -File tools/build_release.ps1` przy zamkniętym
  edytorze (`unity cmd eval "UnityEditor.EditorApplication.Exit(0)"`), klucz `build/keystore.local.json`
  (keystore w `C:/NIGHT/keys`), upload na ścieżkę Closed testing „Alpha”, release notes z `LISTING.md`,
  nowe zrzuty sklepu (opcjonalnie: MENU, godzina szczytu), tag `v1.1.0`, merge `release/1.1.0` → `master`.
- Stan konta Play (z pamięci): 1.0.0 na Internal i Closed „Alpha”, zmiany wysłane do sprawdzenia
  2026-09-18, lista testerów „PEKAO”; warunek produkcji — 12 testerów z opt-in przez 14 dni
  (najwcześniej ~2–3 X 2026); ostrzeżenie Play o blokadzie orientacji na dużych ekranach odłożone;
  kategoria w Console „Rekreacyjne” vs „Arcade” w LISTING.md — do decyzji Michała.

## 7. Pixel i środowisko

- Na Pixelu (`5C160DLCR004AU`) jest **deweloperski APK 1.1.0** (etap 2, zbudowany 19:42). Wersja ze
  Sklepu Play została odinstalowana za zgodą Michała (skasowało to rekordy na telefonie). **Przed
  testem wersji ze Sklepu trzeba odinstalować build deweloperski** (inny podpis).
- Build deweloperski ma ~111 MB (libil2cpp z pakietem `com.unity.pipeline`); release go pomija.
- Edytor 6000.5.5f1 otwarty z `Assets/Scenes/Game.unity`; CLI: `%LOCALAPPDATA%/Unity/bin/unity`
  (`unity cmd recompile` + `recompile_status`, `unity cmd menu --path "NightCafe/Build Scene Setup"`,
  `unity cmd run_tests editor`, `unity cmd menu --path "NightCafe/Build Android APK"`), adb:
  `C:/Tools/Sdk/platform-tools/adb`. `py` = Python 3.12 (numpy, scipy, soundfile, pyloudnorm), ffmpeg jest.
- Sprawdzanie w trybie gry przez CLI: `Application.runInBackground = true` (inaczej edytor w tle nie
  liczy klatek), `capture_game_view --save_path Temp/x.png` zapisuje do **`Assets/Temp`** (usunąć przez
  `AssetDatabase.DeleteAsset("Assets/Temp")`), przed zrzutem pauzy `Camera.Render()` na kamerach z RT.
  Wymuszanie wydarzeń: refleksja na `GameLoopController` (`StartEvent`, `ResetShiftEvents`, `MoveBarista`,
  pole `_penalty = new PenaltyService(999)`, żeby zmiana się nie kończyła).
- Heredoc w Git Bash potrafi się wyłożyć na znakach spoza ASCII — lepiej Edit/Write albo skrypt `.py`.
