# Night Café 1.2.0 — przekazanie sesji (stan na 2026-11-07)

Gałąź `release/1.2.0` (od `release/1.1.1`; 1.1.1 scalone do master przez PR #2, tag `v1.1.1`).
Plan rundy: feedback i animacje, czytelność i sterowanie, rekordy dla wszystkich, nowości w gameplay.
Opis zasad w [GDD.md](GDD.md) §2.10.

## Co zrobione

| Etap | Commit | Treść |
| --- | --- | --- |
| 1 | dfc208e | „+1 / x12” nad tacą, podskok licznika, chlapnięcie plamy, wstrząs ekranu (z wibracjami), rozwijane paski, nabijanie wyniku, oddech Miro |
| 2 | 403584e | samouczek pierwszej zmiany, MENU (większe wiersze, najbliższy wiersz, błysk), większe pola przycisków i dymki, wysyłka rekordów po zalogowaniu, wersja 1.2.0 / versionCode 4 |
| 3 | 3dc36f1, 597031f, 82f1485 | karteczki RANKING / TROFEA, 11 osiągnięć (`Services/Awards.cs`, ikony `docs/store/achievements/`) |
| 4 | 8af344d, 16d114d | wyzwanie dnia (karteczka DZIŚ, losowość z daty UTC, pierwsza próba do tabeli), statystyki pod top 10, podpowiedź skina, karteczki mieszczą tekst |
| poprawka | 1a72b42 | rekord / DZIŚ / podpowiedź odświeżają się po wczytaniu kopii z chmury |

Testy EditMode 203/203. Po każdej przebudowie sceny (`NightCafe/Build Scene Setup`) cofnąć
`ScreenGlass.mat` i `LiberationSans SDF - Fallback.asset` (zmiany uboczne).

## Play Console (usługi gier) — opublikowane 2026-11-07

- 11 osiągnięć (nazwy EN = nazwy zasobów, PL jako tłumaczenia), tabele „Night Café · Mode A / Mode B”
  (PL „Tryb A / B”) i nowa „Night Café · Daily challenge” (PL „Wyzwanie dnia”, `CgkIqdXDkJ0GEAIQEQ`).
- Polski dodany do usług gier (nazwa i opis gry). Zasoby w `docs/store/games-ids.xml`, zastosowane.
- Nazwy zasobów tabel zmieniły się na `leaderboard_night_caf__mode_a/_b` — identyfikatory bez zmian.

## Test na Pixelu (APK deweloperski, 2026-11-07)

59 fps; ciche logowanie i kopia z chmury (rekord 011 wrócił); MENU i strona top 10 ze statystykami;
osiągnięcia 1/11 z polskimi tekstami; lista trzech tabel; wyzwanie dnia zapisane (DZIŚ 002).
Na Pixelu jest build deweloperski — przed instalacją ze Sklepu odinstalować (inny podpis).

## Wydanie

- AAB `build/NightCafe.aab` (1.2.0 / versionCode 4, klucz przesyłania 09:A5:…), notatki EN/PL w
  `docs/store/LISTING.md`. **Test wewnętrzny opublikowany 2026-11-07 23:16** („Dostępna dla testerów
  wewnętrznych”). Jedyne ostrzeżenie: brak pliku mapowania R8 (R8 wyłączone, jak w 1.1.1). Obsługiwane
  urządzenia bez zmian. Przesłanie z przeglądarki: plik podany stronie z lokalnego serwera 127.0.0.1
  (narzędzie do plików ma limit 10 MB).

## Do zrobienia

- Test ze Sklepu na Pixelu (najpierw odinstalować build deweloperski), potem awans na produkcję.
- Opcjonalnie: dłuższy test ręczny (wstrząs, samouczek na nowym koncie, Paprika ×10).
