using System.Collections.Generic;

namespace Mindlock.Datos
{
    /// <summary>
    /// Versión en inglés de cada acertijo (por id) y de los logros. Respuesta y opciones en null
    /// significan que son las mismas del español (números, letras, fracciones...).
    /// Los juegos de palabras que solo funcionan en español se adaptaron con otro acertijo equivalente.
    /// </summary>
    public static partial class CatalogoAcertijos
    {
        class Traduccion
        {
            public AcertijoEn Acertijo;
            public string[] Pistas;
        }

        static Dictionary<int, Traduccion> ingles;

        static void E(int id, string enunciado, string respuesta, string[] opciones, params string[] pistas)
        {
            ingles[id] = new Traduccion
            {
                Acertijo = new AcertijoEn { Enunciado = enunciado, Respuesta = respuesta, Opciones = opciones == null ? null : new List<string>(opciones) },
                Pistas = pistas
            };
        }

        /// <summary>Pone el texto en inglés a un acertijo recién creado (lo llama Crear).</summary>
        static void AplicarIngles(Acertijo a)
        {
            if (ingles == null) CargarIngles();
            Traduccion t;
            if (!ingles.TryGetValue(a.Id, out t)) return;
            a.En = t.Acertijo;
            for (int i = 0; i < a.Pistas.Count && i < t.Pistas.Length; i++) a.Pistas[i].TextoEn = t.Pistas[i];
        }

        static readonly Dictionary<string, string[]> LogrosIngles = new Dictionary<string, string[]>
        {
            { "primer-candado", new[] { "First Lock", "Complete level 1." } },
            { "mente-brillante", new[] { "Bright Mind", "Complete 10 levels without using hints." } },
            { "punteria", new[] { "Sharpshooter", "Solve 5 levels on the first try." } },
            { "velocista", new[] { "Speedster", "Solve a level in under 30 seconds." } },
            { "perfeccionista", new[] { "Perfectionist", "Get 3 stars on 10 levels." } },
            { "constante", new[] { "Steady", "Keep a 7-day daily streak." } },
            { "maestro-patrones", new[] { "Pattern Master", "Complete all the Patterns levels." } },
            { "medio-camino", new[] { "Halfway There", "Complete 15 levels." } },
            { "experto", new[] { "Expert", "Complete level 30." } },
        };

        static void AplicarIngles(List<Logro> logros)
        {
            foreach (var l in logros)
            {
                string[] t;
                if (LogrosIngles.TryGetValue(l.Id, out t)) { l.NombreEn = t[0]; l.DescripcionEn = t[1]; }
            }
        }

        static void CargarIngles()
        {
            ingles = new Dictionary<int, Traduccion>();

            // ---------- NIVELES ORIGINALES (1-30) ----------
            E(1, "Complete the series:\n2, 4, 6, 8, ?", null, null,
                "Look at how much each number grows.", "Each number is 2 more than the one before.", "8 + 2 = ?");
            E(2, "I have hands but I can't clap, and numbers but I'm not a calculator. What am I?", "clock;watch", null,
                "You use it to know something that changes all day.", "It can hang on a wall or sit on your wrist.", "It tells you the time.");
            E(3, "If all cats are animals and Misty is a cat, then...", "Misty is an animal",
                new[] { "Misty is an animal", "All animals are cats", "Misty is not an animal", "It can't be known" },
                "Read the first sentence carefully.", "If something is a cat, it is also an animal.", "Misty belongs to the group of cats.");
            E(4, "Which letter comes next?\nA, C, E, G, ?", null, null,
                "Think of the alphabet.", "One letter is skipped each time.", "After G comes H... and it gets skipped.");
            E(5, "I'm a big furry animal, I love honey and I sleep all winter. Who am I?", "bear", null,
                "My name has only 4 letters.", "You can find me in forests and in cartoons.", "Teddy toys are named after me.");
            E(6, "Peter is taller than Luis, and Luis is taller than Ann. Who is the shortest?", "Ann",
                new[] { "Peter", "Luis", "Ann", "It can't be known" },
                "Order the three from tallest to shortest.", "Peter is the tallest of all.", "Luis is in the middle.");
            E(7, "Complete the series:\n1, 1, 2, 3, 5, 8, ?", null, null,
                "Look at the two previous numbers.", "Each number is the sum of the two before it.", "5 + 8 = ?");
            E(8, "Which word doesn't belong?\nApple, Pear, Carrot, Grape", "Carrot",
                new[] { "Apple", "Pear", "Carrot", "Grape" },
                "Think about what almost all of them have in common.", "Three of them are fruits.", "One grows underground.");
            E(9, "A farmer has 17 sheep. All but 9 run away. How many does he have left?", "9;nine;9 sheep;nine sheep", null,
                "Read the sentence very carefully.", "'All but 9' means 9 don't run away.", "You don't need to subtract.");
            E(10, "Complete the two missing numbers:\n3, 6, 12, 24, ?, ?", null, null,
                "Compare each number with the one before.", "Each number is double the one before.", "24 x 2 = 48");
            E(11, "The more you take away from it, the bigger it gets. What is it?", "hole;pit", null,
                "Think about digging.", "You make it in the ground with a shovel.", "It starts with H.");
            E(12, "If yesterday was Monday, what day will it be the day after tomorrow?", "Thursday",
                new[] { "Wednesday", "Thursday", "Friday", "Saturday" },
                "First work out what day it is today.", "If yesterday was Monday, today is Tuesday.", "The day after tomorrow is two days after today.");
            E(13, "Sort from smallest to largest:\n7, 2, 9, 4", null, null,
                "Start with the smallest number.", "The first one is 2.", "The last one is 9.");
            E(14, "Which of these words is NOT an anagram of LISTEN?", "LISTED",
                new[] { "SILENT", "ENLIST", "TINSEL", "LISTED" },
                "An anagram uses exactly the same letters.", "LISTEN has the letters L, I, S, T, E and N.", "Look for the word with a different letter.");
            E(15, "There are three boxes: Apples, Oranges and Mixed. All the labels are wrong. You take a fruit from the 'Mixed' box and it's an apple. What does that box contain?", "Only apples",
                new[] { "Only apples", "Only oranges", "Mixed fruit", "It can't be known" },
                "All the labels are wrong.", "The 'Mixed' box can't have mixed fruit.", "You took out an apple and it isn't mixed...");
            E(16, "Complete the series:\n1, 4, 9, 16, 25, ?", null, null,
                "Think about multiplication.", "They are numbers multiplied by themselves.", "6 x 6 = ?");
            E(17, "If you say my name, I disappear. What am I?", "silence", null,
                "You can't see it or touch it.", "There's a lot of me in a library.", "It's the opposite of noise.");
            E(18, "A brick weighs 1 kg plus half a brick. How many kilos does a whole brick weigh?", "2;two;2 kg;2kg;2 kilos;two kilos", null,
                "Half a brick weighs the same as 1 kg.", "If half a brick weighs 1 kg...", "Two halves make one brick.");
            E(19, "Complete the series:\n2, 3, 5, 7, 11, ?", null, null,
                "These numbers have something special.", "They can only be divided by 1 and by themselves.", "They are prime numbers.");
            E(20, "A bottle with its cork costs $1.10. The bottle costs $1.00 more than the cork. How much does the cork cost?", null, null,
                "The quick answer is usually wrong.", "If the cork cost $0.10, the bottle would cost $1.10.", "Cork + (cork + 1.00) = 1.10");
            E(21, "What do these words have in common?\nRADAR, LEVEL, RACECAR", "They read the same backwards",
                new[] { "They read the same backwards", "They have 5 letters", "They are verbs", "They start with R" },
                "Look at the order of the letters.", "Read each word from right to left.", "They are called palindromes.");
            E(22, "Complete the two missing letters:\nZ, X, V, T, ?, ?", null, null,
                "Go through the alphabet backwards.", "One letter is skipped each time.", "After T (skipping S) comes R.");
            E(23, "5 machines make 5 parts in 5 minutes. How many minutes do 100 machines take to make 100 parts?", "5;five;5 minutes;five minutes", null,
                "How long does one machine take to make one part?", "Each machine makes 1 part in 5 minutes.", "They all work at the same time.");
            E(24, "I have cities but no houses, rivers but no water, and forests but no trees. What am I?", "map;atlas", null,
                "You use me to find your way.", "I can be on paper or on your phone.", "I show the world in miniature.");
            E(25, "What comes next?\n1, 11, 21, 1211, 111221, ?", null, null,
                "It's not a math operation.", "Each term describes the previous one out loud.", "111221 = three 1s, two 2s, one 1.");
            E(26, "Ann's father has five daughters: Lala, Lele, Lili and Lolo. What is the fifth one called?", "Ann",
                new[] { "Lulu", "Ann", "Lala", "It can't be known" },
                "Read the start of the question.", "Whose father is he?", "Only four names are mentioned...");
            E(27, "If you have me, you want to share me. If you share me, you no longer have me. What am I?", "secret", null,
                "I'm not an object.", "If you tell it, it stops being one.", "It's kept... or told.");
            E(28, "A snail is at the bottom of a 10-meter well. Each day it climbs 3 meters and each night it slides back 2. How many days does it take to get out?", "8;eight;8 days;eight days", null,
                "Each full day it only advances 1 meter.", "On the last day it doesn't slide back because it's already out.", "At the start of day 8 it's at 7 meters.");
            E(29, "Sort from smallest to largest:\n1/2, 1/3, 2/3, 1/4", null, null,
                "Imagine a pizza cut into slices.", "More slices = smaller pieces.", "The smallest is 1/4 and the largest is 2/3.");
            E(30, "There are two doors: one leads to the exit. One guard always lies and the other always tells the truth, but you don't know which is which. You can ask only one question. Which one guarantees the exit?",
                "Ask which door the other guard would point to and choose the opposite",
                new[]
                {
                    "Ask one of them which door is the exit",
                    "Ask which door the other guard would point to and choose the opposite",
                    "Ask both and choose the most voted",
                    "Ask one of them if he is a liar"
                },
                "You need a question that works with either of them.", "Combine the lie and the truth in a single answer.", "Ask what the other guard would say.");

            // ---------- ACERTIJOS DIARIOS (101-121) ----------
            E(101, "Complete the series:\n8, 16, 24, 32, ?", null, null,
                "Each number grows by the same amount.", "8 is added each time.", "32 + 8 = ?");
            E(102, "I have keys but I'm not a piano, and space but no rooms. What am I?", "keyboard", null,
                "You use it to type.", "It's usually next to a computer.", "It starts with K.");
            E(103, "If 3 cats catch 3 mice in 3 minutes, how many cats are needed to catch 100 mice in 100 minutes?", null, null,
                "How long does one cat take to catch one mouse?", "Each cat catches 1 mouse every 3 minutes.", "In 100 minutes, one cat catches about 33 mice.");
            E(104, "It goes up and down but never moves from its place. What is it?", "stairs;staircase;stair", null,
                "It's in many houses and buildings.", "It has steps.", "You use it to change floors.");
            E(105, "Which letter comes next?\nJ, F, M, A, M, J, ?", null, null,
                "Think of the calendar.", "They are the initials of something that repeats every year.", "January, February, March...");
            E(106, "Which number comes next?\n10, 9, 7, 4, ?", null, null,
                "Look at how much is subtracted each time.", "Subtract 1, then 2, then 3...", "4 - 4 = ?");
            E(107, "The more it dries, the wetter it gets. What is it?", "towel", null,
                "You use it in the bathroom.", "It's for drying yourself.", "After the shower...");
            E(108, "Complete the series:\n5, 10, 20, 40, ?", null, null,
                "Compare each number with the one before.", "Each number is double the one before.", "40 x 2 = ?");
            E(109, "If tomorrow is Saturday, what day was yesterday?", "Thursday",
                new[] { "Wednesday", "Thursday", "Friday", "Sunday" },
                "First work out what day it is today.", "If tomorrow is Saturday, today is Friday.", "Yesterday is the day before today.");
            E(110, "I have eyes but can't see, water but I don't drink it, meat but I don't eat it, and a beard but I'm not a man. What am I?", "coconut", null,
                "I'm a fruit.", "I grow on palm trees.", "My water is very refreshing.");
            E(111, "Which letter comes next?\nB, D, F, H, ?", null, null,
                "Think of the alphabet.", "One letter is skipped each time.", "After H comes I... and it gets skipped.");
            E(112, "Mary is twice as old as John, who is 6. How old will Mary be when John is 10?", null, null,
                "First work out Mary's current age.", "Mary is 12 now.", "They both get older at the same pace.");
            E(113, "It flies without wings and cries without eyes. What is it?", "cloud;clouds", null,
                "It's in the sky.", "When it 'cries', water falls.", "It can be white or grey.");
            E(114, "Sort from smallest to largest:\n15, 3, 27, 9", null, null,
                "Start with the smallest number.", "The first one is 3.", "The last one is 27.");
            E(115, "Which word doesn't belong?\nRed, Blue, Chair, Green", "Chair",
                new[] { "Red", "Blue", "Chair", "Green" },
                "Think about what almost all of them have in common.", "Three of them are colors.", "One is a piece of furniture.");
            E(116, "If you have 3 apples and you take away 2, how many apples do you have?", "2;two;2 apples;two apples", null,
                "Read the question carefully.", "It asks how many YOU have.", "The ones you took are now yours.");
            E(117, "Complete the series:\n100, 90, 81, 73, ?", null, null,
                "Look at how much is subtracted each time.", "Subtract 10, then 9, then 8...", "73 - 7 = ?");
            E(118, "In a race you overtake the person in second place. What position are you in now?", "Second",
                new[] { "First", "Second", "Third", "Last" },
                "Don't rush.", "You take the place of the person you overtook.", "They were in second place...");
            E(119, "I have a neck but no head, and I wear a cap. What am I?", "bottle", null,
                "You can find me in the kitchen.", "I hold liquids.", "You pour water from me.");
            E(120, "Complete the two missing numbers:\n1, 3, 6, 10, ?, ?", null, null,
                "Look at how much it grows each time.", "Add 2, then 3, then 4...", "10 + 5 = 15");
            E(121, "An electric train is heading north and the wind is blowing south. Which way does the smoke go?", "There is no smoke",
                new[] { "North", "South", "Up", "There is no smoke" },
                "Pay attention to the type of train.", "Electric trains don't burn fuel.", "No burning, no smoke.");

            // ---------- MODO DESAFÍO (205-230) ----------
            E(205, "Complete the series:\n2, 12, 36, 80, 150, ?", null, null,
                "Each number depends on its position (1, 2, 3...).", "Each term combines the square and the cube of its position.", "Position 6: 6³ + 6² = ?");
            E(206, "What word is spelled incorrectly in every dictionary?", "incorrectly", null,
                "It's not a printing mistake.", "The answer is hidden in the question.", "Read the question word by word.");
            E(207, "A says: \"B is lying.\" B says: \"C is lying.\" C says: \"A and B are lying.\" Each one always lies or always tells the truth. Who tells the truth?", "Only B",
                new[] { "Only A", "Only B", "Only C", "A and C" },
                "Assume C tells the truth and look for a contradiction.", "C can't be telling the truth, so A or B is.", "If C is lying, what B says is true.");
            E(208, "Complete the two missing:\nO, T, T, F, F, S, S, ?, ?", "E|N",
                new[] { "E", "N", "T", "O", "S" },
                "It's not the alphabet.", "Think of numbers written as words.", "One, Two, Three, Four...");
            E(209, "I am twice the age you were when I was the age you are now. When you are the age I am now, together we'll add up to 63 years. How old am I?",
                "28;twenty-eight;twenty eight;28 years;28 years old", null,
                "Call my age x and yours y.", "The first sentence gives 3x = 4y.", "The second sentence gives 3x - y = 63.");
            E(210, "Complete the two missing:\n8, 5, 4, 9, 1, 7, 6, ?, ?", "3|2",
                new[] { "0", "2", "3", "10" },
                "It's not a math operation.", "Write each number as a word.", "They're in alphabetical order: eight, five, four...");
            E(211, "Which number, written as a word in English, has all its letters in alphabetical order?", "forty;40", null,
                "Write the numbers out as words.", "It's between 30 and 50.", "Its letters are f, o, r, t, y.");
            E(212, "A family has two children and at least one is a boy. What is the probability that both are boys?", null, null,
                "List all the possible combinations of two children.", "They are BB, BG, GB and GG, all equally likely.", "Rule out GG: three cases remain.");
            E(213, "Complete the series:\n1, 4, 27, 256, ?", null, null,
                "Each number depends on its position.", "The position is raised to itself.", "5 to the power of 5 = ?");
            E(214, "If HOUSE is written IPVTF, how is LOGIC written?", "MPHJD",
                new[] { "MPHJD", "KNFHB", "MPHJC", "NQIKE" },
                "Compare HOUSE and IPVTF letter by letter.", "Each letter moves forward one place in the alphabet.", "L becomes M, O becomes P, G becomes H...");
            E(215, "You have 9 identical coins, but one is slightly heavier. With a two-pan balance, what is the minimum number of weighings needed to find it for sure?", null, null,
                "Each weighing has three outcomes: left, right or balanced.", "Split the coins into three groups of 3.", "With two weighings you can tell 3 x 3 = 9 cases apart.");
            E(216, "Complete the series:\n2, 6, 30, 210, 2310, ?", null, null,
                "Divide each number by the previous one.", "The factors are 3, 5, 7, 11...", "They are prime numbers: 2310 x 13 = ?");
            E(217, "The one who makes it doesn't want it, the one who buys it doesn't use it, and the one who uses it doesn't see it. What is it?", "coffin;casket", null,
                "Nobody wants it for themselves.", "It has to do with an ending.", "It's used at a funeral.");
            E(218, "A clock loses 2 minutes every hour. You set it right at 12:00 noon. What time will it show when it's really midnight? (HH:MM format)", null, null,
                "How many hours pass from noon to midnight?", "In 12 hours it loses 12 x 2 minutes.", "It loses 24 minutes: subtract them from 12:00.");
            E(219, "Complete the two missing:\n2, 5, 11, 23, 47, ?, ?", null, null,
                "Compare each number with double the previous one.", "Each term is double the previous one plus one.", "47 x 2 + 1 = 95");
            E(220, "There are 100 people in a room and 99% are men. How many men must leave so that men make up 98%?", null, null,
                "The number of women doesn't change.", "There is 1 woman, and she must be 2% of the total.", "1 is 2% of 50.");
            E(221, "John is an only child. Pointing at a portrait he says: \"That man's father is my father's son.\" Who is in the portrait?", "John's son",
                new[] { "John himself", "John's father", "John's son", "John's grandson" },
                "Who is \"my father's son\" if John has no brothers?", "\"My father's son\" is John himself.", "So the father of the man in the portrait is John.");
            E(222, "Which letter comes next?\nZ, X, U, Q, L, ?", null, null,
                "Go through the alphabet backwards.", "The jumps grow: 2, 3, 4, 5...", "From L go back 6 letters.");
            E(223, "How many times a day do the hour hand and the minute hand overlap exactly?", "22;twenty-two;twenty two;22 times", null,
                "It's not 24.", "In 12 hours they overlap 11 times.", "11 x 2 = ?");
            E(224, "The bigger it is, the less you see. What is it?", "darkness;dark", null,
                "It's not an object.", "It shows up when the light goes out.", "It's the opposite of light.");
            E(225, "Complete the series:\n1, 2, 6, 15, 31, ?", null, null,
                "Look at the differences between the numbers.", "The differences are squares: 1, 4, 9, 16...", "31 + 25 = ?");
            E(226, "Outside a closed room there are 3 switches; only one turns on the bulb inside. You can flip the switches as much as you want, but you can only go in once. How do you find out which one it is?",
                "Turn one on for a while, turn it off, turn on another and go in to touch the bulb",
                new[]
                {
                    "Turn on all three and go in",
                    "Turn one on for a while, turn it off, turn on another and go in to touch the bulb",
                    "Turn one on, go in, and if it's not lit guess between the other two",
                    "It can't be known by going in only once"
                },
                "The bulb gives more information than just its light.", "A lit bulb gets hot.", "Use the heat to identify the first switch.");
            E(227, "The poor have it, the rich need it, and if you eat it you die. What is it?", "nothing", null,
                "It's not an object.", "Think about what someone does NOT have.", "If you eat ___, you starve.");
            E(228, "In a knockout tennis tournament there are 147 players. How many matches are played until there is a champion?",
                "146;146 matches;one hundred forty-six;one hundred and forty-six", null,
                "Each match eliminates exactly one player.", "In the end only one is left undefeated.", "147 - 1 = ?");
            E(229, "Sort from smallest to largest:\n7/10, 2/3, 5/8, 9/13", null, null,
                "Convert each fraction to a decimal.", "5/8 = 0.625 and 7/10 = 0.7.", "2/3 is about 0.667 and 9/13 is about 0.692.");
            E(230, "You have a 5-liter jug and a 3-liter jug, with no marks. Counting each time you fill, empty or pour from one to the other, what is the minimum number of steps to get exactly 4 liters?", null, null,
                "Start by filling the 5-liter jug.", "Fill the 3-liter jug from the 5: 2 liters are left in the big one.", "Empty the 3, pour in the 2 liters, fill the 5 and top up the 3.");
        }
    }
}
