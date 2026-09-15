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

## Path to production (new developer account)

1. Internal testing: upload `build/NightCafe.aab`, add your own account as a tester, install
   from the Play link (uninstall the sideloaded debug build first: signatures differ).
2. Closed testing: 12 testers opted in, 14 days running, then apply for production access in
   the console (the questionnaire asks what you learned from the test).
3. Production: same AAB or a bumped `versionCode` (`AndroidBundleVersionCode` in
   ProjectSettings — every upload needs a higher one).
