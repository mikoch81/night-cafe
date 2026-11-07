# Ranking online — konfiguracja Google Play Games Services (etap 3 rundy 1.1.0)

Decyzja Michała (2026-09-26): ranking top 10 przez **Google Play Games Services** (leaderboardy Google,
logowanie kontem Play). Kod gry i wtyczka (`GooglePlayGamesPlugin-2.2.1`, Apache 2.0) są po stronie
projektu; poniższe kroki wykonuje się w Play Console / Google Cloud na koncie dewelopera — Claude ich nie
zrobi. Na końcu potrzebny jest **plik zasobów (XML)** z punktu 6.

## Odciski kluczy (SHA-1)

> Po każdym uaktualnieniu klucza podpisywania w Play Console dodaj nowy SHA-1 jako dane logowania Gier Play
> (klient OAuth Android w Google Cloud + *Dodaj dane logowania* + publikacja), inaczej logowanie ze Sklepu pada.

| Klucz | Do czego | SHA-1 |
|---|---|---|
| **App signing key — AKTUALNY** (Google, po uaktualnieniu klucza) | wersja ze Sklepu Play (internal / closed / produkcja) | `F4:DA:3E:3E:D6:CF:88:33:78:10:EE:BE:D0:A8:F8:01:09:57:63:FE` (dodany do danych logowania 2026-11-07; bez niego logowanie ze Sklepu kończyło się DEVELOPER_ERROR) |
| App signing key — poprzedni (Google) | starsze instalacje | `B1:EA:87:4E:F2:18:64:3B:21:0A:DC:50:F3:19:1E:25:50:1B:EF:5B` (Play Console → *Chronione przez Google Play* → *Ochrona w Sklepie Play* ⌄ → *Podpisywanie aplikacji w Google Play*) |
| Upload key (`C:\NIGHT\keys\nightcafe-upload.jks`) | AAB zainstalowany ręcznie przez bundletool | `09:A5:07:15:5F:14:0D:5C:EE:E4:43:89:8A:E9:7D:F5:57:61:98:2A` |
| Debug keystore tego PC | deweloperski APK z edytora (testy na Pixelu) | `2C:62:5D:79:41:28:55:58:72:95:BC:39:EF:DE:80:EE:99:6F:EE:82` |

Pakiet: `com.mikoch81.nightcafe`.

## Kroki

1. **Play Console → Night Café → *Rozwój* (Grow users) → *Usługi gier Play* → *Konfiguracja i zarządzanie*
   → *Konfiguracja*.** Wybierz „Utwórz nowy projekt w usługach gier Play”. Pole **„Projekt w chmurze”**
   pyta o projekt **Google Cloud**, nie o grę — przy pierwszej konfiguracji lista jest pusta. Kliknij
   „Utwórz nowy projekt w chmurze” → w Google Cloud (to samo konto Google) nazwa **Night Cafe**, lokalizacja
   „Brak organizacji” → Utwórz → wróć do Play Console → „Odśwież projekty w chmurze” → wybierz go → „Użyj”.
   (Jeden projekt w chmurze = jeden projekt usług gier; widać tylko projekty, których jesteś właścicielem.)
2. **Ekran zgody OAuth.** Konsola podsunie link do Google Cloud → *OAuth consent screen*: typ **External**,
   nazwa aplikacji „Night Café”, e-mail pomocy, e-mail dewelopera; zakresy domyślne (nic nie dodawać);
   polityka prywatności: `https://github.com/mikoch81/night-cafe/blob/master/docs/privacy.md`. Opublikuj
   (Publish app) — dla samych zakresów gier weryfikacja Google nie jest potrzebna.
3. **Dane logowania (credentials) → *Dodaj dane logowania* → Android.** Utwórz klienta OAuth na
   **App signing key** (SHA-1 z tabeli wyżej). Powtórz dla **upload key** i **debug keystore**, żeby działało
   też to, co instalujemy ręcznie. Zaznacz „Autoryzacja” / anti-piracy — **nie** (inaczej build deweloperski
   nie zaloguje się).
4. **Tabele wyników (Leaderboards) → *Utwórz tabelę wyników*** — dwie:
   - „Night Café · Tryb A” (EN: „Night Café · Mode A”), format **liczbowy**, bez miejsc po przecinku,
     kolejność **od największej**, ikona opcjonalnie (`docs/store/icon_512.png`), limit dolny 1.
   - „Night Café · Tryb B” (EN: „Night Café · Mode B”), tak samo.
5. **Testerzy → dodaj** swoje konto Google i konta testerów (lista „PEKAO”). Dopóki konfiguracja usług gier
   nie jest opublikowana, ranking działa tylko dla testerów.
6. **Tabele wyników → *Pobierz zasoby* (Get resources) → Android (XML)** — skopiuj całość i wklej Claude'owi
   (albo zapisz jako `docs/store/games-ids.xml`). Zawiera `app_id` i identyfikatory obu tabel; to nie są
   sekrety, trafią do repo.
7. Po teście — **Konfiguracja → *Sprawdź i opublikuj*** usługi gier razem z wydaniem 1.1.0.

## Co zmienia się poza konsolą

- Gra przestaje być w pełni offline: manifest dostaje `INTERNET` i `ACCESS_NETWORK_STATE` (wymaga ich
  Play Games). `docs/privacy.md`, `LISTING.md` („no internet, no data collected”) i test uprawnień idą za tym.
- **Bezpieczeństwo danych (Data safety)** w Play Console trzeba zaktualizować: dane przetwarza Google Play
  Games (identyfikator gracza, wyniki). Wypełnić według strony Google „Play Games Services — Data safety”
  (developer.android.com, sekcja PGS), nie zgadywać.
- Bez zalogowania gra działa jak dotąd; lokalne top 10 zostaje. Logowanie jest ciche przy starcie (PGS v2
  loguje automatycznie, jeśli gracz ma Gry Play); wiersz „RANKING ONLINE” w menu pokazuje ranking Google,
  a gdy gracz nie jest zalogowany — najpierw prosi o logowanie.

## Osiągnięcia (1.2.0)

Domyślny język projektu usług gier to **angielski (en-US)**, więc nazwa zasobu w XML-u powstaje z angielskiej
nazwy: „Score 100” → `achievement_score_100`. Gra rozpoznaje osiągnięcia po tej nazwie, dlatego **pole
„Nazwa” wpisz dokładnie jak w tabeli** (opis może być inny). Do publikacji wszystkie pola muszą być po
angielsku; polską wersję dodaje się potem jako tłumaczenie. Ikony 512×512 (32-bit PNG) są gotowe w
`docs/store/achievements/` (kubek ze znaczkiem, jak ikony tabel wyników). Stan początkowy: **Widoczne**.

| Nazwa (EN, dokładnie) | Opis (EN) | Ikona | Rodzaj | Punkty | PL: nazwa / opis |
|---|---|---|---|---|---|
| First shift | Finish your first shift. | first_shift.png | zwykłe | 5 | Pierwsza zmiana / Dokończ pierwszą zmianę. |
| Score 100 | Score 100 points in one shift. | score_100.png | zwykłe | 10 | Wynik 100 / Zdobądź 100 punktów w jednej zmianie. |
| Score 250 | Score 250 points in one shift. | score_250.png | zwykłe | 15 | Wynik 250 / Zdobądź 250 punktów w jednej zmianie. |
| Score 500 | Score 500 points in one shift. | score_500.png | zwykłe | 20 | Wynik 500 / Zdobądź 500 punktów w jednej zmianie. |
| Score 1000 | Pass 1000 points in one shift. | score_1000.png | zwykłe | 30 | Wynik 1000 / Przekrocz 1000 punktów w jednej zmianie. |
| Streak 25 | Catch 25 cups in a row. | streak_25.png | zwykłe | 10 | Seria 25 / Złap 25 kubków z rzędu. |
| Streak 50 | Catch 50 cups in a row. | streak_50.png | zwykłe | 20 | Seria 50 / Złap 50 kubków z rzędu. |
| Terrible ten | Survive the machine gone wild. | terrible_ten.png | zwykłe | 15 | Straszna dziesiątka / Przetrwaj szalejący ekspres. |
| Level 5 | Reach level 5. | level_5.png | zwykłe | 10 | Poziom 5 / Dojdź do poziomu 5. |
| Orders 50 | Score 50 points in mode B. | orders_50.png | zwykłe | 10 | Zamówienia 50 / Zdobądź 50 punktów w trybie B. |
| Paprika tamer | Shoo Paprika off the stool 10 times. | paprika_tamer.png | **wielostopniowe, 10 kroków** | 15 | Pogromca Papriki / Przegoń Paprikę ze stołka 10 razy. |

Razem 160 punktów (limit 2000, każda wartość wielokrotnością 5). Kolejność na liście jak w tabeli.
Potem: *Osiągnięcia* → **Pobierz zasoby → Android (XML)** → wklej Claude'owi (zastąpi
`docs/store/games-ids.xml`, dalej `NightCafe → Apply Play Games Resources`) i **opublikuj** zmiany w usługach
gier. Przed publikacją osiągnięcia działają tylko dla testerów.
