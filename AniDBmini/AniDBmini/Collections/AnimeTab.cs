
#region Using Statements

using System;
using System.Windows.Media.Imaging;

using AniDBmini;

#endregion Using Statements

namespace AniDBmini.Collections
{
    public class AnimeTab : IEquatable<AnimeTab>
    {

        #region Fields

        public int AnimeID { get; private set; }

        public string Year { get; private set; }
        public string Type { get; private set; }
        public string Categories { get; private set; }

        public string Romaji { get; private set; }
        public string Kanji { get; private set; }
        public string English { get; private set; }

        public int EnglishRowHeight { get { return English == null ? 0 : 42; } }

        public string Eps { get { return Episodes.ToString() + " +" + Specials.ToString(); } }

        public string AirDateText { get { return StartDate == EndDate ? "Air Date" : "Start Date"; } }
        public string StartDate { get; private set; }
        public string EndDate { get; private set; }

        public int EndDateRowHeight { get; private set; }

        public string Officialurl { get; private set; }
        public string AniDBurl { get; private set; }
        public string ANNurl { get; private set; }
        public BitmapImage Image { get; private set; }

        public string Ratings { get { return string.Format("{0:0.00}", RatingMean) + " (" + RatingCount.ToString() + ")"; } }
        public string Reviews { get { return string.Format("{0:0.00}", ReviewMean) + " (" + ReviewCount.ToString() + ")"; } }

        public bool OofujiAward { get; private set; }
        public bool AnimationAward { get; private set; }

        public int OofujiAwardRowHeight { get { return !OofujiAward ? 0 : 50; } }
        public int AnimationAwardRowHeight { get { return !AnimationAward ? 0 : 50; } }

        private double RatingMean, ReviewMean;
        private int RatingCount, ReviewCount, Episodes, Specials;

        #endregion Fields

        #region Constructor

        public AnimeTab(string result)
        {
            string[] animeInfo = result.Split('|');

            AnimeID = int.Parse(animeInfo[0]);
            Year = animeInfo[1];
            Type = animeInfo[2];

            string[] cats = animeInfo[3].Split(',');
            for (int i = 0; i < Math.Min(cats.Length, 9); i++)
                    Categories += (i != 0 ? ", " : null) + cats[i];

            Romaji = animeInfo[4];
            Kanji = animeInfo[5];
            English = string.IsNullOrEmpty(animeInfo[6]) ? null : animeInfo[6];

            Episodes = int.Parse(animeInfo[7]);

            StartDate = ExtensionMethods.UnixTimeToDateTime(animeInfo[8]).Date.ToShortDateString();
            EndDate = ExtensionMethods.UnixTimeToDateTime(animeInfo[9]).Date.ToShortDateString();
            EndDateRowHeight = string.IsNullOrEmpty(animeInfo[9]) || StartDate == EndDate ? 0 : 42;

            Officialurl = animeInfo[10];
            AniDBurl = "http://anidb.net/perl-bin/animedb.pl?show=anime&aid=" + AnimeID;
            ANNurl = "http://www.animenewsnetwork.com/encyclopedia/anime.php?id=" + animeInfo[19];
            Image = new BitmapImage(new Uri("http://img7.anidb.net/pics/anime/" + animeInfo[11]));

            RatingMean = !string.IsNullOrEmpty(animeInfo[12]) ?
                         double.Parse(animeInfo[12]) / 100 : double.Parse(animeInfo[14]) / 100;
            RatingCount = !string.IsNullOrEmpty(animeInfo[13]) ?
                          int.Parse(animeInfo[13]) : int.Parse(animeInfo[15]);

            ReviewMean = double.Parse(animeInfo[16]) / 100;
            ReviewCount = int.Parse(animeInfo[17]);

            OofujiAward = animeInfo[18].Contains("Oofuji") ? true : false;
            AnimationAward = animeInfo[18].Contains("Animation Grand") ? true : false;

            Specials = int.Parse(animeInfo[20]);
        }

        #endregion Constructor

        #region IEquatable

        public bool Equals(AnimeTab other)
        {
            return AnimeID == other.AnimeID;
        }

        #endregion IEquatable

    }
}