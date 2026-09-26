# Ranking online — konfiguracja Google Play Games Services (etap 3 rundy 1.1.0)

Decyzja Michała (2026-09-26): ranking top 10 przez **Google Play Games Services** (leaderboardy Google,
logowanie kontem Play). Kod gry i wtyczka (`GooglePlayGamesPlugin-2.2.1`, Apache 2.0) są po stronie
projektu; poniższe kroki wykonuje się w Play Console / Google Cloud na koncie dewelopera — Claude ich nie
zrobi. Na końcu potrzebny jest **plik zasobów (XML)** z punktu 6.

## Odciski kluczy (SHA-1)

| Klucz | Do czego | SHA-1 |
|---|---|---|
| **App signing key** (Google) | wersja ze Sklepu Play (internal / closed / produkcja) | Play Console → *Test i publikowanie* → *Integralność aplikacji* → *Podpisywanie aplikacji* → „Certyfikat klucza podpisywania aplikacji”, SHA-1 |
| Upload key (`C:\NIGHT\keys\nightcafe-upload.jks`) | AAB zainstalowany ręcznie przez bundletool | `09:A5:07:15:5F:14:0D:5C:EE:E4:43:89:8A:E9:7D:F5:57:61:98:2A` |
| Debug keystore tego PC | deweloperski APK z edytora (testy na Pixelu) | `2C:62:5D:79:41:28:55:58:72:95:BC:39:EF:DE:80:EE:99:6F:EE:82` |

Pakiet: `com.mikoch81.nightcafe`.

## Kroki

1. **Play Console → Night Café → *Rozwój* (Grow users) → *Usługi gier Play* → *Konfiguracja i zarządzanie*
   → *Konfiguracja*.** Na pytanie o Google APIs: „Nie, moja gra nie używa interfejsów Google API” →
   utwórz nowy projekt usług gier, nazwa **Night Café**. Zapisz.
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
