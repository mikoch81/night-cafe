using System;
using NightCafe.Core;

namespace NightCafe.Services
{
    /// <summary>Moments Miro has something to say about (1.1.0, review 2026-09-26).</summary>
    public enum Quip
    {
        CatOnLadder,
        CatShooed,
        CatLeft,
        LadderCreak,
        LadderBroke,
        LadderBlocked,
        LadderSlowClimb,
        LadderToppled,
        LadderRepaired,
        RushStart,
        RushEnd,
        QuietStart,
        QuietEnd,
        FrenzyStart,
        FrenzyEnd,
        Count
    }

    /// <summary>
    /// What Miro says in his speech bubble, in English or Polish (<see cref="Loc.Current"/>).
    /// The first times something happens in a shift he says what to do about it (a hint, where
    /// the moment has one); after that he jokes, never the same line twice in a row. Pure: the
    /// view only shows the string.
    /// </summary>
    public sealed class BaristaQuips
    {
        /// <summary>How many times per shift a moment gets its hint before the jokes take over.</summary>
        public const int HintTimes = 2;

        const int Languages = 2;
        static readonly string[][] Hints = { new string[(int)Quip.Count], new string[(int)Quip.Count] };
        static readonly string[][][] Jokes = { new string[(int)Quip.Count][], new string[(int)Quip.Count][] };

        static BaristaQuips()
        {
            En(Quip.CatOnLadder, "Paprika's on my stool!\nTap its top button.",
                "Not you again, Paprika!", "That's MY stool, you menace.", "Ginger trouble, top shelf.", "Cats don't tip, you know.");
            Pl(Quip.CatOnLadder, "Paprika na moim stołku!\nWciśnij górny przycisk.",
                "Znowu ty, Paprika?!", "To MÓJ stołek, rudzielcu.", "Rude kłopoty na górze.", "Koty nie dają napiwków.");

            En(Quip.CatShooed, null, "Off you go, furball!", "Shoo! And stay out!", "Towel of justice!", "Go bother the pigeons!");
            Pl(Quip.CatShooed, null, "Wynocha, sierściuchu!", "A kysz! I nie wracaj!", "Ręcznik sprawiedliwości!", "Idź gonić gołębie!");

            En(Quip.CatLeft, null, "Fine, keep your dignity.", "He got bored. Phew.", "Thank you, your majesty.");
            Pl(Quip.CatLeft, null, "Znudziło mu się. Uff.", "No i poszedł. Łaskawca.", "Dziękuję, wasza wysokość.");

            En(Quip.LadderCreak, "Noir shook my stool!\nGet off it, quick!",
                "Noir, you little menace!", "That creak sounds expensive.", "Hold together, old friend...");
            Pl(Quip.LadderCreak, "Noir trzęsie stołkiem!\nZejdź z niego, szybko!",
                "Noir, ty łobuzie!", "Ten trzask brzmi drogo.", "Trzymaj się, staruszku...");

            En(Quip.LadderBroke, null, "Ow. My dignity.", "I'm fine! The cup isn't.", "Note to self: new stool.");
            Pl(Quip.LadderBroke, null, "Au. Moja godność.", "Ja cały! Filiżanka nie.", "Notatka: nowy stołek.");

            En(Quip.LadderBlocked, "It's broken.\nThe other side, quick!", "Nope, not that one.", "Stool's in pieces.", "Still broken. Sadly.");
            Pl(Quip.LadderBlocked, "Połamany.\nNa drugą stronę, szybko!", "Nie, nie ten.", "Stołek w kawałkach.", "Dalej połamany. Niestety.");

            En(Quip.LadderSlowClimb, "Noir knocked it askew.\nClimb early, I'm slow.",
                "Easy... easy...", "Who taught that cat to shove?", "Steady hands, wobbly legs.");
            Pl(Quip.LadderSlowClimb, "Noir przekrzywił stołek.\nWchodź wcześniej, to trwa.",
                "Spokojnie... spokojnie...", "Kto uczy te koty pchać?", "Pewne ręce, miękkie nogi.");

            En(Quip.LadderToppled, "Noir tipped my stool!\nTap its top button twice.", "Timber!", "Noir! Really?", "Who raised these cats?");
            Pl(Quip.LadderToppled, "Noir przewrócił stołek!\nGórny przycisk 2 razy!", "Leci!", "Noir! Serio?", "Kto wychował te koty?");

            En(Quip.LadderRepaired, null, "Good as new. Almost.", "Up you go!", "Stool: fixed. Me: tired.");
            Pl(Quip.LadderRepaired, null, "Jak nowy. Prawie.", "Hop do góry!", "Stołek: naprawiony. Ja: padnięty.");

            En(Quip.RushStart, "Rush hour!\nFaster cups, stay sharp!", "Here they come!", "Latte storm incoming!", "Full house! Breathe...");
            Pl(Quip.RushStart, "Godzina szczytu!\nSzybsze kubki, uwaga!", "Nadciągają!", "Burza latte!", "Pełna sala! Oddychaj...");

            En(Quip.RushEnd, null, "Phew. Rush over.", "Survived. Barely.", "Who ordered all that?");
            Pl(Quip.RushEnd, null, "Uff. Po szczycie.", "Przeżyłem. Ledwo.", "Kto to wszystko zamówił?");

            En(Quip.QuietStart, "A quiet spell.\nCatch your breath.", "Ahh, a slow minute.", "Quiet... too quiet.", "Time for a sip.");
            Pl(Quip.QuietStart, "Chwila spokoju.\nZłap oddech.", "Ach, wolna minutka.", "Cicho... za cicho.", "Czas na łyczek.");

            En(Quip.QuietEnd, null, "Break's over!", "Back to work!", "Here we go again!");
            Pl(Quip.QuietEnd, null, "Koniec przerwy!", "Do roboty!", "No to jedziemy!");

            En(Quip.FrenzyStart, "The machine's gone wild!\nStay under its shelf!", "Who pressed turbo?!", "Too much pressure!", "It's alive!!");
            Pl(Quip.FrenzyStart, "Ekspres oszalał!\nStój pod jego półką!", "Kto wcisnął turbo?!", "Za duże ciśnienie!", "On żyje!!");

            En(Quip.FrenzyEnd, null, "Phew. Descaled.", "Someone call a technician.", "Ten seconds of pure panic.");
            Pl(Quip.FrenzyEnd, null, "Uff. Odkamienione.", "Wezwijcie serwisanta.", "Dziesięć sekund paniki.");
        }

        static void En(Quip quip, string hint, params string[] jokes) => Set(Language.English, quip, hint, jokes);

        static void Pl(Quip quip, string hint, params string[] jokes) => Set(Language.Polish, quip, hint, jokes);

        static void Set(Language language, Quip quip, string hint, string[] jokes)
        {
            Hints[(int)language][(int)quip] = hint;
            Jokes[(int)language][(int)quip] = jokes;
        }

        readonly IRandom _rng;
        readonly int[] _times = new int[(int)Quip.Count];
        readonly int[] _lastJoke = new int[(int)Quip.Count];

        public BaristaQuips(IRandom rng)
        {
            _rng = rng;
            Reset();
        }

        /// <summary>A new shift: the hints come back.</summary>
        public void Reset()
        {
            Array.Clear(_times, 0, _times.Length);
            for (int i = 0; i < _lastJoke.Length; i++)
                _lastJoke[i] = -1;
        }

        public static bool HasHint(Quip quip) => Hints[(int)Language.English][(int)quip] != null;

        /// <summary>Both languages have a line for every moment, hints for the same ones (a test checks this).</summary>
        public static bool Complete(out Quip missing)
        {
            for (int q = 0; q < (int)Quip.Count; q++)
            {
                bool hinted = Hints[0][q] != null;
                for (int l = 0; l < Languages; l++)
                {
                    if (Jokes[l][q] == null || Jokes[l][q].Length == 0 || (Hints[l][q] != null) != hinted)
                    {
                        missing = (Quip)q;
                        return false;
                    }
                }
            }

            missing = Quip.Count;
            return true;
        }

        public string Line(Quip quip)
        {
            int index = (int)quip;
            int language = (int)Loc.Current;
            if (_times[index]++ < HintTimes && Hints[language][index] != null)
                return Hints[language][index];

            string[] jokes = Jokes[language][index];
            int pick = _rng.NextInt(0, jokes.Length);
            if (pick == _lastJoke[index] && jokes.Length > 1)
                pick = (pick + 1) % jokes.Length;
            _lastJoke[index] = pick;
            return jokes[pick];
        }
    }
}
