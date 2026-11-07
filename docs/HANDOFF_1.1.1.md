# Night Café 1.1.1 — przekazanie sesji (stan na 2026-11-07)

1.1.0 jest na produkcji w Play (178 krajów, ~16 instalacji). Ta runda to sugestie Google z Play Console
i zapis w chmurze. Gałąź `release/1.1.1` (od `release/1.1.0`). Poprzednia runda: [HANDOFF_1.1.0.md](HANDOFF_1.1.0.md).

## Co zgłosiła Play Console (wersja 2 / 1.1.0) i co z tym zrobiono

| Sugestia | Decyzja |
| --- | --- |
| Nieaktualne `androidx.fragment` 1.1.0 i `androidx.activity` 1.0.0 (przechodnie z `play-services-games-v2`) | Wymuszone 1.6.2 / 1.7.2 w `Assets/Plugins/Android/mainTemplate.gradle`, poza blokiem Android Resolvera; do tego `kotlin-stdlib-jdk7/jdk8` 1.8.20 (inaczej `checkReleaseDuplicateClasses` pada) |
| R8 wyłączone | Pominięte: mały zysk w Unity, ryzyko wycięcia klas Play Games (wymagałoby reguł keep) |
| Ograniczenie orientacji `userLandscape` na dużych ekranach | Ignorujemy: manifest ma `appCategory="game"`, Android 16 nie wymusza obrotu w grach; ekran LCD jest poziomy |
| Brak symboli natywnych | `BuildAndroid.Build` (release) ustawia `DebugSymbols` = SymbolTable, IncludeInBundle — symbole jadą w AAB |

Konsola (2026-11-07): powiadomienia e-mail o opiniach 1–5★ i zaktualizowanych włączone. Status przedsiębiorcy
był już zadeklarowany („Osoba niebędąca przedsiębiorcą”); komunikat „do 1 lutego 2027” to ogólne
powiadomienie. Vitals i oceny: brak danych (za mało użytkowników).

## Zapis w chmurze (Play Games saved games)

- `ProfileService.Merge(json)`: rekordy = max, top 10 i skiny = suma; wybrany tryb/skin zostają lokalne.
  Test `ScoreListTests.TheCloudCopyJoinsTheLocalProfile`.
- `IOnlineScores.LoadProfile / SaveProfile`, implementacja w `PlayGamesScores` (plik zapisu `profile`,
  konflikty: `UseMostRecentlySaved`; starsza strona dołącza swoje dane przy następnym starcie).
  Zapisy są ignorowane, dopóki w tej sesji nie odczytano kopii z chmury — świeża instalacja nie nadpisze historii.
- `GameLoopController`: po cichym/ręcznym logowaniu `SyncCloudProfile()`; każda zmiana profilu → `SaveProfile`.
- Play Console → Usługi gier Play → Konfiguracja → Właściwości: **Zapisane gry = Wł. zapisane jako wersja
  robocza, NIEopublikowane.** Opcji nie da się wyłączyć po publikacji. Opublikować razem z wydaniem 1.1.1
  (Usługi gier Play → Publikowanie), inaczej otwieranie zapisu zwróci błąd i chmura po cichu nie działa.
- Polityka prywatności (`docs/privacy.md`) i notatki do wydania 1.1.1 w `LISTING.md` zaktualizowane.
  Data safety: profil trafia do Google przez Play Games — sprawdzić, czy deklaracja „Inne działania w
  aplikacji” wystarcza (może dodać „Postępy w grze”).

## Stan

- Testy EditMode 195/195. `build/NightCafe.aab` 1.1.1 / versionCode 3 zbudowany (73 MB — ~58 MB to symbole
  w `BUNDLE-METADATA`, gracze ich nie pobierają). Zmiany niezacommitowane na `release/1.1.1`.

## Do zrobienia

- Test na Pixelu: zalogowany gracz → rozegrać zmianę → odinstalować → zainstalować → rekordy wracają.
- Wydanie: versionCode 3 / 1.1.1, najpierw ścieżka testu zamkniętego (raport przed publikacją), potem produkcja.
