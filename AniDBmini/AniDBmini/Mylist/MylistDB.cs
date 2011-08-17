
#region Using Statements

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class MylistDB
    {

        #region Fields

        public const string dbPath = @".\data\local.db";

        private SQLiteConnection SQLConn;

        public bool isSQLConnOpen;
        public TSObservableCollection<AnimeEntry> Entries = new TSObservableCollection<AnimeEntry>();

        #endregion Fields

        #region Constructor

        public MylistDB()
        {
            if (Initialize())
                SelectEntries();
        }

        #endregion Constructor

        #region Private Methods

        private bool Initialize(bool create = false)
        {
            if (File.Exists(dbPath) != create && !isSQLConnOpen)
            {
                SQLConn = new SQLiteConnection(@"Data Source=" + dbPath + ";version=3;");
                SQLConn.Open();

                isSQLConnOpen = true;
            }

            return isSQLConnOpen;
        }

        #endregion Private Methods

        #region Public Methods

        #region CLOSE

        /// <summary>
        /// Closes the currently open databaase.
        /// </summary>
        public void Close()
        {
            if (isSQLConnOpen)
                SQLConn.Close();

            isSQLConnOpen = false;
        }

        #endregion CLOSE

        #region SELECT

        public void SelectEntries()
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT a.aid, a.type, a.title, a.nihongo, a.english, a.eps_total, a.year, SUM(f.length) AS length, IFNULL(SUM(f.size), 0) AS size,
                                           COUNT(e.spl_epno) AS spl_have, (SELECT COUNT(spl_epno) FROM episodes WHERE aid = a.aid AND watched = 1) AS spl_watched,
                                           IFNULL(MIN(COUNT(e.watched), a.eps_total), 0) AS eps_have, IFNULL(MIN(SUM(e.watched), a.eps_total), 0) AS eps_watched
                                      FROM anime AS a
                                 LEFT JOIN episodes AS e ON e.aid = a.aid
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                  GROUP BY a.aid
                                  ORDER BY a.title ASC;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        AnimeEntry entry = new AnimeEntry(reader);
                        entry.Episodes = this.SelectEpisodes(entry);
                        Entries.Add(entry);
                    }
            }
        }

        public void SelectEntry(int aid)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT a.aid, a.type, a.title, a.nihongo, a.english, a.eps_total, a.year, SUM(f.length) AS length, IFNULL(SUM(f.size), 0) AS size,
                                           COUNT(e.spl_epno) AS spl_have, (SELECT COUNT(spl_epno) FROM episodes WHERE aid = a.aid AND watched = 1) AS spl_watched,
                                           IFNULL(MIN(COUNT(e.watched), a.eps_total), 0) AS eps_have, IFNULL(MIN(SUM(e.watched), a.eps_total), 0) AS eps_watched
                                      FROM anime AS a
                                 LEFT JOIN episodes AS e ON e.aid = a.aid
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                     WHERE a.aid = @aid
                                  GROUP BY a.aid
                                  ORDER BY a.title ASC;";
                cmd.Parameters.AddWithValue("@aid", aid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                {
                    AnimeEntry entry = Entries.FirstOrDefault<AnimeEntry>(x => x.aid == aid);

                    if (entry != null)
                    {
                        int index = Entries.IndexOf(entry);
                        Entries[index] = new AnimeEntry(reader);
                        Entries[index].Episodes = this.SelectEpisodes(entry);
                    }
                    else
                    {
                        entry = new AnimeEntry(reader);
                        entry.Episodes = this.SelectEpisodes(entry);
                        Entries.Add(entry);
                    }
                }
            }
        }

        public TSObservableCollection<EpisodeEntry> SelectEpisodes(AnimeEntry m_entry)
        {
            TSObservableCollection<EpisodeEntry> _temp = new TSObservableCollection<EpisodeEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT e.*, IFNULL(f.length, 0) AS length, IFNULL(SUM(f.size), 0) AS size, f.generic,
                                           (SELECT CASE WHEN  generic < COUNT(eid) THEN 0 ELSE 1 END FROM files WHERE eid = e.eid) AS genericOnly
                                      FROM episodes AS e
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                     WHERE e.aid = @aid
                                  GROUP BY e.eid
                                  ORDER BY IFNULL(e.epno, 'S'), spl_epno;";
                cmd.Parameters.AddWithValue("@aid", m_entry.aid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        EpisodeEntry entry = new EpisodeEntry(reader);
                        entry.Files = this.SelectFiles(entry);
                        _temp.Add(entry);
                    }
            }

            return _temp;
        }

        public TSObservableCollection<FileEntry> SelectFiles(EpisodeEntry e_entry)
        {
            TSObservableCollection<FileEntry> _temp = new TSObservableCollection<FileEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT * FROM files AS f
                                 LEFT JOIN groups AS g ON g.gid = f.gid
                                     WHERE eid = @eid
                                  ORDER BY g.group_abbr;";
                cmd.Parameters.AddWithValue("@eid", e_entry.eid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        _temp.Add(new FileEntry(reader));
            }

            return _temp;
        }

        #endregion SELECT

        #region CREATE

        /// <summary>
        /// Creates an empty database with correct table structures.
        /// </summary>
        public void Create() // TODO: Add foreign keys.
        {
            Initialize(true);

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"CREATE TABLE anime ('aid' INTEGER PRIMARY KEY NOT NULL, 'type' VARCHAR NOT NULL,
                                                        'title' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'english' VARCHAR, 'year' VARCHAR NOT NULL, 'eps_total' INTEGER);
                                    CREATE TABLE episodes ('eid' INTEGER PRIMARY KEY, 'aid' INTEGER NOT NULL, 'epno' INTEGER, 'spl_epno' VARCHAR,
                                                           'english' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'romaji' VARCHAR, 'airdate' INTEGER, 'watched' INTEGER);
                                    CREATE TABLE files ('fid' INTEGER PRIMARY KEY NOT NULL, 'lid' INTEGER NOT NULL, 'eid' INTEGER, 'gid' INTEGER, 'ed2k' VARCHAR, 'watcheddate' INTEGER,
                                                        'length' INTEGER, 'size' INTEGER,'source' VARCHAR, 'acodec' VARCHAR, 'vcodec' VARCHAR, 'vres' VARCHAR, 'path' VARCHAR, 'generic' INTEGER);
                                    CREATE TABLE groups ('gid' INTEGER PRIMARY KEY NOT NULL, 'group_name' VARCHAR, 'group_abbr' VARCHAR);
                                    CREATE UNIQUE INDEX 'idx_anime_aid' ON 'anime' ('aid');
                                    CREATE UNIQUE INDEX 'idx_eps_eid' ON 'episodes' ('eid');
                                    CREATE INDEX 'idx_eps_aid' ON 'episodes' ('aid');
                                    CREATE INDEX 'idx_eps_watched' ON 'episodes' ('watched');
                                    CREATE INDEX 'idx_files_eid' ON 'files' ('eid');";
                cmd.ExecuteNonQuery();
            }
        }

        #endregion CREATE

        #region INSERT

        /// <summary>
        /// Inserts or updates a recently added file and
        /// its related entries with up to date data.
        /// </summary>
        public void InsertFileInfo(FileInfoFetchedArgs e)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO anime VALUES (@aid, @type, @title, @nihongo, @english, @year, @eps_total);";

                SQLiteParameter[] aParams = { new SQLiteParameter("@aid", e.Anime.aid),
                                              new SQLiteParameter("@type", e.Anime.type),
                                              new SQLiteParameter("@title", e.Anime.title),
                                              new SQLiteParameter("@nihongo", e.Anime.nihongo),
                                              new SQLiteParameter("@english", e.Anime.english),
                                              new SQLiteParameter("@year", e.Anime.year),
                                              new SQLiteParameter("@eps_total", e.Anime.eps_total) };

                aParams[3].IsNullable = aParams[4].IsNullable = true;
                cmd.Parameters.AddRange(aParams);

                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = @"INSERT OR REPLACE INTO episodes VALUES (@eid, @aid, @epno, @spl_epno, @english, @nihongo, @romaji, @airdate, @watched);";

                SQLiteParameter[] eParams = { new SQLiteParameter("@eid", e.Episode.eid),
                                              new SQLiteParameter("@aid", e.Anime.aid),
                                              new SQLiteParameter("@epno", e.Episode.epno),
                                              new SQLiteParameter("@spl_epno", e.Episode.spl_epno),
                                              new SQLiteParameter("@english", e.Episode.english),
                                              new SQLiteParameter("@nihongo", e.Episode.nihongo),
                                              new SQLiteParameter("@romaji", e.Episode.romaji),
                                              new SQLiteParameter("@airdate", e.Episode.airdate),
                                              new SQLiteParameter("@watched", e.Episode.watched) };

                eParams[2].IsNullable = eParams[3].IsNullable = eParams[4].IsNullable = eParams[5].IsNullable = true;

                if (e.Episode.epno == 0)
                    eParams[2].Value = null;

                cmd.Parameters.AddRange(eParams);

                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = @"INSERT OR REPLACE INTO files VALUES (@fid, @lid, @eid, @gid, @ed2k, @watcheddate, @length, @size, @source, @acodec, @vcodec, @vres, @path, @generic);";

                SQLiteParameter[] fParams = { new SQLiteParameter("@fid", e.File.fid),
                                              new SQLiteParameter("@lid", e.File.lid),
                                              new SQLiteParameter("@eid", e.Episode.eid),
                                              new SQLiteParameter("@gid", e.File.gid),
                                              new SQLiteParameter("@ed2k", e.File.ed2k),
                                              new SQLiteParameter("@watcheddate", e.File.watcheddate),
                                              new SQLiteParameter("@length", e.File.length),
                                              new SQLiteParameter("@size", e.File.size),
                                              new SQLiteParameter("@source", e.File.source),
                                              new SQLiteParameter("@acodec", e.File.acodec),
                                              new SQLiteParameter("@vcodec", e.File.vcodec),
                                              new SQLiteParameter("@vres", e.File.vres),
                                              new SQLiteParameter("@path", e.File.path),
                                              new SQLiteParameter("@generic", e.File.generic) };

                fParams[3].IsNullable = fParams[4].IsNullable = fParams[7].IsNullable =
                fParams[8].IsNullable = fParams[9].IsNullable = fParams[10].IsNullable = fParams[11].IsNullable = true;

                if (e.File.gid == 0)
                    fParams[3].Value = null;

                cmd.Parameters.AddRange(fParams);

                cmd.ExecuteNonQuery();

                InsertGroup(e.File.gid, e.File.group_name, e.File.group_abbr);
            }
        }

        /// <summary>
        /// Insert a fansub group into the database.
        /// </summary>
        public void InsertGroup(int gid, string name, string abbr)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO groups VALUES (@gid, @group_name, @group_abbr);";

                cmd.Parameters.AddWithValue("@gid", gid);
                cmd.Parameters.AddWithValue("@group_name", name);
                cmd.Parameters.AddWithValue("@group_abbr", abbr);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert a mylist entry into the database.
        /// Specifically for use when importing -- should not be used elsewhere. 
        /// </summary>
        public void InsertAnimeEntryFromImport(AnimeEntry entry)
        {
            Entries.Add(entry);

            using (SQLiteTransaction dbTrans = SQLConn.BeginTransaction())
            {
                using (SQLiteCommand cmd = SQLConn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO anime VALUES (@aid, @type, @title, @nihongo, @english, @year, @eps_total);";

                    cmd.Parameters.AddWithValue("@aid", entry.aid);
                    cmd.Parameters.AddWithValue("@type", entry.type);
                    cmd.Parameters.AddWithValue("@title", entry.title);
                    cmd.Parameters.AddWithValue("@year", entry.year);
                    cmd.Parameters.AddWithValue("@eps_total", entry.eps_total);

                    SQLiteParameter[] aParams = { new SQLiteParameter("@english", entry.english),
                                                  new SQLiteParameter("@nihongo", entry.nihongo) };
                    aParams[0].IsNullable = aParams[1].IsNullable = true;
                    cmd.Parameters.AddRange(aParams);

                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"INSERT INTO episodes VALUES (@eid, @aid, @epno, @spl_epno, @english, @nihongo, @romaji, @airdate, @watched);";

                    SQLiteParameter[] eParams = { new SQLiteParameter("@eid"),
                                                  new SQLiteParameter("@aid", entry.aid),
                                                  new SQLiteParameter("@epno"),
                                                  new SQLiteParameter("@spl_epno"),
                                                  new SQLiteParameter("@english"),
                                                  new SQLiteParameter("@nihongo"),
                                                  new SQLiteParameter("@romaji"),
                                                  new SQLiteParameter("@airdate"),
                                                  new SQLiteParameter("@watched") };

                    eParams[2].IsNullable = eParams[3].IsNullable =
                    eParams[5].IsNullable = eParams[6].IsNullable = eParams[8].IsNullable = true;
                    cmd.Parameters.AddRange(eParams);

                    using (SQLiteCommand fileCmd = SQLConn.CreateCommand())
                    {

                        fileCmd.CommandText = @"INSERT OR REPLACE INTO files VALUES (@fid, @lid, @eid, @gid, @ed2k, @watcheddate, @length, @size, @source, @acodec, @vcodec, @vres, @path, @generic);";

                        SQLiteParameter[] fParams = { new SQLiteParameter("@fid"),
                                                      new SQLiteParameter("@lid"),
                                                      new SQLiteParameter("@eid"),
                                                      new SQLiteParameter("@gid"),
                                                      new SQLiteParameter("@ed2k"),
                                                      new SQLiteParameter("@watcheddate"),
                                                      new SQLiteParameter("@length"),
                                                      new SQLiteParameter("@size"),
                                                      new SQLiteParameter("@source"),
                                                      new SQLiteParameter("@acodec"),
                                                      new SQLiteParameter("@vcodec"),
                                                      new SQLiteParameter("@vres"),
                                                      new SQLiteParameter("@path"),
                                                      new SQLiteParameter("@generic") };

                        fParams[3].IsNullable = fParams[4].IsNullable = fParams[5].IsNullable = fParams[6].IsNullable = fParams[7].IsNullable =
                        fParams[8].IsNullable = fParams[9].IsNullable = fParams[10].IsNullable = fParams[11].IsNullable = fParams[12].IsNullable = true;
                        fileCmd.Parameters.AddRange(fParams);

                        foreach (EpisodeEntry ep in entry.Episodes)
                        {
                            eParams[0].Value = ep.eid;
                            if (ep.epno != 0) eParams[2].Value = ep.epno;
                            else eParams[3].Value = ep.spl_epno;
                            eParams[4].Value = ep.english;
                            eParams[5].Value = ep.nihongo;
                            eParams[6].Value = ep.romaji;
                            eParams[7].Value = ep.airdate;
                            eParams[8].Value = ep.watched;

                            cmd.ExecuteNonQuery();

                            fParams[2].Value = ep.eid;

                            foreach (FileEntry file in ep.Files)
                            {
                                fParams[0].Value = file.fid;
                                fParams[1].Value = file.lid;
                                if (file.gid != 0) fParams[3].Value = file.gid;
                                fParams[4].Value = file.ed2k;
                                fParams[5].Value = file.watcheddate;
                                fParams[6].Value = file.length;
                                fParams[7].Value = file.size;
                                fParams[8].Value = file.source;
                                fParams[9].Value = file.acodec;
                                fParams[10].Value = file.vcodec;
                                fParams[11].Value = file.vres;
                                fParams[12].Value = file.path;
                                fParams[13].Value = file.generic;

                                fileCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    dbTrans.Commit();
                }
            }
        }

        #endregion INSERT

        #endregion Public Methods

    }
}
