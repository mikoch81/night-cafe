# Google Play listing — Night Café

Category: **Games → Arcade**. Contains ads: **no**. In-app purchases: **no**.
Content rating (IARC questionnaire): no violence, no user interaction, no data sharing → expected
**PEGI 3 / Everyone**. Data safety: **no data collected, no data shared**; privacy policy URL →
`docs/privacy.md` published on GitHub Pages (see below). Target audience: 13+ (not designed for children).

## Assets

| Asset | Spec | Source |
|---|---|---|
| App icon | 512×512 PNG, no alpha needed | `Assets/Art/icon/icon_legacy.png` |
| Feature graphic | 1024×500 PNG/JPG | Midjourney prompt 05 (`docs/references/PROMPTS.md`) + the wordmark from `splash_logo.png` |
| Phone screenshots | 4–8, 16:9 landscape, min 1080 px | `adb exec-out screencap -p > shot.png` on the Pixel: title, mode A mid-round, mode B with an order, end of shift with NEW BEST, RETRO skin |
| 7" / 10" tablet screenshots | optional | same captures |

## Short description (max 80 characters)

- EN: `Catch the cups, keep the night shift going. A cosy lo-fi LCD-style arcade.`
- PL: `Łap kubki, dotrwaj do końca nocnej zmiany. Ciepła arcade w stylu handheldów LCD.`

## Full description

### EN

Night Café is a one-screen arcade game in the spirit of the classic LCD handhelds — played on a
virtual one, the Bréve Deck, a walnut-and-aluminium pocket console whose screen is a window onto a
rainy night café.

You are Miro, the barista. Cups slide down four counters from the espresso machines; catch them on
your tray before they reach the edge. Miss three and the shift is over. Sablé the cat mops up the
mess — and every so often takes pity on you.

- Two modes: **A** — catch everything; **B** — serve only the colour on the order card.
- The tempo climbs with every catch, the way it did on the old handhelds. Breathers every hundred.
- Roll the counter past 999 and the neon does something special.
- A real wall clock on the title screen, and a brew timer for your actual coffee.
- Unlock console finishes — ash, onyx, neon — and the amber Neo-LCD screen skin.
- Hand-painted diorama, a lo-fi record on loop, rain on the window. Haptics you can turn off.
- No ads, no accounts, no internet, no data collected. Plays offline, forever.

### PL

Night Café to jednoekranowa gra arcade w duchu klasycznych handheldów LCD — grana na wirtualnym
Bréve Decku, kieszonkowej konsoli z orzecha i aluminium, której ekran jest oknem na deszczową
nocną kawiarnię.

Jesteś Miro, baristą. Kubki zjeżdżają z czterech lad spod ekspresów; łap je na tacę, zanim spadną z
krawędzi. Trzy stłuczki i zmiana skończona. Kot Sablé sprząta — i czasem się nad Tobą lituje.

- Dwa tryby: **A** — łap wszystko; **B** — podawaj tylko kolor z karty zamówienia.
- Tempo rośnie z każdym złapaniem, jak na starych handheldach. Oddech co setkę.
- Przekręć licznik za 999, a neon zrobi coś specjalnego.
- Prawdziwy zegar na ekranie tytułowym i minutnik parzenia do Twojej własnej kawy.
- Odblokuj wykończenia konsoli — jesion, onyks, neon — i bursztynowy skin ekranu Neo-LCD.
- Malowana diorama, lo-fi na pętli, deszcz za szybą. Wibracje do wyłączenia.
- Bez reklam, kont, internetu i zbierania danych. Działa offline, zawsze.

## Release notes 1.0.0

- EN: `First shift. Modes A and B, four console finishes, the Neo-LCD skin, a brew timer.`
- PL: `Pierwsza zmiana. Tryby A i B, cztery wykończenia konsoli, skin Neo-LCD, minutnik parzenia.`

## Publishing the privacy policy

GitHub Pages from the repo's `docs/` folder is enough (Settings → Pages → Deploy from branch →
`master` / `/docs`); the URL is then `https://mikoch81.github.io/night-cafe/privacy` — the repo
is private, so either make it public or host `privacy.md` as a public Gist / page elsewhere.

## Trailer

`docs/store/trailer_16x9.mp4` (38 s, 1080p) and `trailer_9x16.mp4` (stories) — cut by
`tools/make_trailer.py` from a phone screen recording (`adb shell screenrecord`, path written as
`//sdcard/...` from Git Bash) with the ART lo-fi loop and rain underneath. For the store's promo video
upload the 16:9 file to YouTube (unlisted is fine) and paste the link in the listing; for testers send
the file straight to WhatsApp/Messenger — the end card carries the opt-in link.

## Installing the AAB on a phone (what the store will ship)

```
JAVA="C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/OpenJDK/bin/java.exe"
BT="C:/Program Files/Unity/Hub/Editor/6000.5.5f1/Editor/Data/PlaybackEngines/AndroidPlayer/Tools/bundletool-all-1.17.2.jar"
"$JAVA" -jar "$BT" build-apks --bundle=build/NightCafe.aab --output=build/NightCafe.apks --connected-device \
    --adb=C:/Tools/Sdk/platform-tools/adb.exe --ks=<jks> --ks-pass=pass:<pw> --ks-key-alias=nightcafe --key-pass=pass:<pw>
adb uninstall com.mikoch81.nightcafe          # a sideloaded debug build has another signature
"$JAVA" -jar "$BT" install-apks --apks=build/NightCafe.apks --adb=C:/Tools/Sdk/platform-tools/adb.exe
```

Verified 2026-09-15 on the Pixel: `flags` without DEBUGGABLE, requested permissions = `VIBRATE` only,
10/10 cold starts. logcat shows `ClassNotFoundException: ...play.core.assetpacks.AssetPackManager` on
start — Unity probing for Play Asset Delivery, which this bundle does not use; harmless.

Screenshots: play a real round on the release build and capture with `adb exec-out screencap -p`
(the dev build carries a "Development Build" watermark and the fps counter).

## Path to production (new developer account)

Android developer verification (Console > Weryfikacja dewelopera): package `com.mikoch81.nightcafe` registered
2026-09-15 with 3 keys. Identity tab shows the account name and address with nothing pending. Both done; unregistered apps are removed from Play after 2026-09-30.

1. Internal testing: upload `build/NightCafe.aab`, add your own account as a tester, install
   from the Play link (uninstall the sideloaded debug build first: signatures differ).
2. Closed testing: 12 testers opted in, 14 days running, then apply for production access in
   the console (the questionnaire asks what you learned from the test).
3. Production: same AAB or a bumped `versionCode` (`AndroidBundleVersionCode` in
   ProjectSettings — every upload needs a higher one).
