
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
        public delegate void EntryInsertedHandler(string aTitle);
        public event EntryInsertedHandler OnEntryInserted = delegate { };

        #endregion Fields

        #region Constructor

        public MylistDB()
        {
            Initialize();
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

        /// <summary>
        /// Selects all anime entries from the database,
        /// ordered by the set title language.
        /// </summary>
        /// <returns>List of AnimeEntry objects</returns>
        public List<AnimeEntry> SelectEntries()
        {
            List<AnimeEntry> Entries = new List<AnimeEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = String.Format(@"SELECT a.aid, a.type, a.romaji, a.nihongo, a.english, a.eps_total, a.year, IFNULL(SUM(f.length), 0) AS length, IFNULL(SUM(f.size), 0) AS size,
					                                     COUNT(e.spl_epno) AS spl_have, (SELECT COUNT(spl_epno) FROM episodes WHERE aid = a.aid AND watched = 1) AS spl_watched,
					                                     (SELECT COUNT(*) FROM episodes WHERE aid = a.aid AND spl_epno IS NULL) AS eps_have,
					                                     (SELECT COUNT(*) FROM episodes WHERE aid = a.aid AND spl_epno IS NULL AND watched = 1) AS eps_watched
                                                    FROM anime AS a
                                               LEFT JOIN episodes AS e ON e.aid = a.aid
                                               LEFT JOIN files AS f ON f.eid = e.eid
                                                GROUP BY a.aid
                                                ORDER BY IFNULL(a.{0}, a.romaji) COLLATE NOCASE ASC;", MainWindow.m_aLang);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        Entries.Add(new AnimeEntry(reader));
            }

            return Entries;
        }

        /// <summary>
        /// Selects all episodes of a provided anime.
        /// </summary>
        /// <param name="aid">Anime ID</param>
        /// <returns>List of EpisodeEntry objects</returns>
        public List<EpisodeEntry> SelectEpisodes(int aid)
        {
            List<EpisodeEntry> _temp = new List<EpisodeEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT e.*, IFNULL(SUM(f.length), 0) AS length, IFNULL(SUM(f.size), 0) AS size,
                                           f.generic, IFNULL(COUNT(f.eid), 0) as hasFiles
                                      FROM episodes AS e
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                     WHERE e.aid = @aid
                                  GROUP BY e.eid
                                  ORDER BY IFNULL(e.epno, 'S'), spl_epno;";
                cmd.Parameters.AddWithValue("@aid", aid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        _temp.Add(new EpisodeEntry(reader));
            }

            return _temp;
        }

        /// <summary>
        /// Selects all files of a provided episode.
        /// </summary>
        /// <param name="aid">Episode ID</param>
        /// <returns>List of FileEntry objects</returns>
        public List<FileEntry> SelectFiles(int eid)
        {
            List<FileEntry> _temp = new List<FileEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT f.*, g.*, e.epno, e.spl_epno FROM files AS f
                                      JOIN episodes AS e ON e.eid = f.eid
                                 LEFT JOIN groups AS g ON g.gid = f.gid
                                     WHERE f.eid = @eid
                                  ORDER BY g.group_abbr;";
                cmd.Parameters.AddWithValue("@eid", eid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        FileEntry f = new FileEntry(reader);
                        f.PropertyChanged += (s, e) => { UpdateFile(f); };
                        _temp.Add(f);
                    }
            }

            return _temp;
        }

        public AnimeEntry SelectAnimeFromFile(int fid)
        {
            AnimeEntry anime = null;

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT a.aid, a.type, a.romaji, a.nihongo, a.english, a.eps_total, a.year, IFNULL(SUM(f.length), 0) AS length, IFNULL(SUM(f.size), 0) AS size,
					                       COUNT(e.spl_epno) AS spl_have, (SELECT COUNT(spl_epno) FROM episodes WHERE aid = a.aid AND watched = 1) AS spl_watched,
					                       (SELECT COUNT(*) FROM episodes WHERE aid = a.aid AND spl_epno IS NULL) AS eps_have,
					                       (SELECT COUNT(*) FROM episodes WHERE aid = a.aid AND spl_epno IS NULL AND watched = 1) AS eps_watched
                                     FROM anime AS a
                                     JOIN episodes AS e ON e.aid = a.aid
                                     JOIN files AS f ON f.eid = e.eid
                                    WHERE f.fid = @fid
		                            LIMIT 1;";
                cmd.Parameters.AddWithValue("@fid", fid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    if (reader.Read())
                        anime = new AnimeEntry(reader);
            }

            return anime;
        }

        public FileEntry SelectFileFromAnime(int aid)
        {
            FileEntry file = null;

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT f.*, g.*, e.epno, e.spl_epno FROM files AS f
			                          JOIN anime AS a ON a.aid = e.aid
			                          JOIN episodes AS e ON e.eid = f.eid
                                 LEFT JOIN groups AS g ON g.gid = f.gid
		                             WHERE a.aid = @aid
	                              ORDER BY e.epno
		                            LIMIT 1;";
                cmd.Parameters.AddWithValue("@aid", aid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    if (reader.Read())
                        file = new FileEntry(reader);
            }

            return file;
        }

        public List<FileEntry> SelectFilesFromAnime(int aid)
        {
            List<FileEntry> files = new List<FileEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT f.*, g.*, e.epno, e.spl_epno FROM files AS f
			                          JOIN anime AS a ON a.aid = e.aid
			                          JOIN episodes AS e ON e.eid = f.eid
                                 LEFT JOIN groups AS g ON g.gid = f.gid
		                             WHERE a.aid = @aid AND e.spl_epno IS NULL
	                              ORDER BY e.epno;";
                cmd.Parameters.AddWithValue("@aid", aid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        FileEntry f = new FileEntry(reader);
                        f.PropertyChanged += (s, e) => { UpdateFile(f); };
                        files.Add(f);
                    }
            }

            return files;
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
                                                        'romaji' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'english' VARCHAR, 'year' VARCHAR NOT NULL, 'eps_total' INTEGER);
                                    CREATE TABLE episodes ('eid' INTEGER PRIMARY KEY, 'aid' INTEGER NOT NULL, 'epno' INTEGER, 'spl_epno' VARCHAR,
                                                           'english' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'romaji' VARCHAR, 'airdate' INTEGER, 'watched' INTEGER);
                                    CREATE TABLE files ('fid' INTEGER PRIMARY KEY NOT NULL, 'lid' INTEGER NOT NULL, 'eid' INTEGER, 'gid' INTEGER, 'ed2k' VARCHAR,
                                                        'watcheddate' INTEGER, 'length' INTEGER, 'size' INTEGER,'source' VARCHAR, 'acodec' VARCHAR, 'vcodec' VARCHAR,
                                                        'vres' VARCHAR, 'path' VARCHAR, 'state' INTEGER, 'generic' INTEGER);
                                    CREATE TABLE groups ('gid' INTEGER PRIMARY KEY NOT NULL, 'group_name' VARCHAR, 'group_abbr' VARCHAR);
                                    CREATE UNIQUE INDEX 'idx_anime_aid' ON 'anime' ('aid');
                                    CREATE UNIQUE INDEX 'idx_eps_eid' ON 'episodes' ('eid');
                                    CREATE UNIQUE INDEX 'idx_files_fid' ON 'files' ('fid');
                                    CREATE INDEX 'idx_eps_aid' ON 'episodes' ('aid');
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
            UpdateAnime(e.Anime);
            UpdateEpisode(e.Episode);
            UpdateFile(e.File);
            InsertGroup(e.File.Group);
        }

        /// <summary>
        /// Insert a fansub group into the database.
        /// </summary>
        public void InsertGroup(GroupEntry entry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO groups VALUES (@gid, @group_name, @group_abbr);";

                cmd.Parameters.AddWithValue("@gid", entry.gid);
                cmd.Parameters.AddWithValue("@group_name", entry.group_name);
                cmd.Parameters.AddWithValue("@group_abbr", entry.group_abbr);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert a all data from a mylist import.
        /// Specifically for use when importing -- should not be used elsewhere. 
        /// </summary>
        public void InsertFromImport(List<AnimeEntry> entries)
        {
            this.Create();

            using (SQLiteTransaction dbTrans = SQLConn.BeginTransaction())
            {
                using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
                {
                    List<int> insertedGroups = new List<int>();

                    foreach (AnimeEntry entry in entries)
                    {
                        cmd.CommandText = @"INSERT INTO anime VALUES (@aid, @type, @title, @nihongo, @english, @year, @eps_total);";

                        SQLiteParameter[] aParams = { new SQLiteParameter("@aid", entry.aid),
                                                      new SQLiteParameter("@type", entry.type),
                                                      new SQLiteParameter("@title", entry.title),
                                                      new SQLiteParameter("@nihongo", entry.nihongo),
                                                      new SQLiteParameter("@english", entry.english),
                                                      new SQLiteParameter("@year", entry.year),
                                                      new SQLiteParameter("@eps_total", entry.eps_total) };

                        aParams[3].IsNullable = aParams[4].IsNullable = true;

                        cmd.Parameters.Clear();
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

                        cmd.Parameters.Clear();
                        cmd.Parameters.AddRange(eParams);

                        using (SQLiteCommand fileCmd = new SQLiteCommand(SQLConn))
                        {

                            fileCmd.CommandText = @"INSERT OR REPLACE INTO files VALUES (@fid, @lid, @eid, @gid, @ed2k, @watcheddate, @length, @size, @source,
                                                                                         @acodec, @vcodec, @vres, @path, @state, @generic);";

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
                                                          new SQLiteParameter("@state"),
                                                          new SQLiteParameter("@generic") };

                            fParams[3].IsNullable = fParams[4].IsNullable = fParams[5].IsNullable = fParams[6].IsNullable = fParams[7].IsNullable = fParams[8].IsNullable =
                            fParams[9].IsNullable = fParams[10].IsNullable = fParams[11].IsNullable = fParams[12].IsNullable = fParams[14].IsNullable = true;

                            fileCmd.Parameters.AddRange(fParams);

                            foreach (EpisodeEntry ep in entry.Episodes)
                            {
                                eParams[0].Value = ep.eid;
                                if (ep.epno != 0)
                                    eParams[2].Value = ep.epno;
                                else
                                {
                                    eParams[2].Value = null;
                                    eParams[3].Value = ep.spl_epno;
                                }
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
                                    if (file.Group.gid != 0) fParams[3].Value = file.Group.gid;
                                    fParams[4].Value = file.ed2k;
                                    fParams[5].Value = file.watcheddate;
                                    fParams[6].Value = file.length;
                                    fParams[7].Value = file.size;
                                    fParams[8].Value = file.source;
                                    fParams[9].Value = file.acodec;
                                    fParams[10].Value = file.vcodec;
                                    fParams[11].Value = file.vres;
                                    fParams[12].Value = file.path;
                                    fParams[13].Value = file.state;
                                    if (file.generic) fParams[14].Value = file.generic;

                                    fileCmd.ExecuteNonQuery();
                                    OnEntryInserted(entry.title);

                                    if (file.Group.gid != 0 && !insertedGroups.Contains(file.Group.gid))
                                    {
                                        using (SQLiteCommand groupCmd = new SQLiteCommand(SQLConn))
                                        {
                                            groupCmd.CommandText = @"INSERT INTO groups VALUES (@gid, @group_name, @group_abbr);";

                                            groupCmd.Parameters.AddWithValue("@gid", file.Group.gid);
                                            groupCmd.Parameters.AddWithValue("@group_name", file.Group.group_name);
                                            groupCmd.Parameters.AddWithValue("@group_abbr", file.Group.group_abbr);

                                            groupCmd.ExecuteNonQuery();
                                            insertedGroups.Add(file.Group.gid);
                                        }
                                    }
                                }
                            }
                        }
                    }

                    dbTrans.Commit();
                }
            }
        }

        #endregion INSERT

        #region UPDATE

        private void UpdateAnime(AnimeEntry entry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO anime VALUES (@aid, @type, @title, @nihongo, @english, @year, @eps_total);";

                SQLiteParameter[] aParams = { new SQLiteParameter("@aid", entry.aid),
                                              new SQLiteParameter("@type", entry.type),
                                              new SQLiteParameter("@title", entry.title),
                                              new SQLiteParameter("@nihongo", entry.nihongo),
                                              new SQLiteParameter("@english", entry.english),
                                              new SQLiteParameter("@year", entry.year),
                                              new SQLiteParameter("@eps_total", entry.eps_total) };

                aParams[3].IsNullable = aParams[4].IsNullable = true;

                cmd.Parameters.AddRange(aParams);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateEpisode(EpisodeEntry entry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO episodes VALUES (@eid, @aid, @epno, @spl_epno, @english, @nihongo, @romaji, @airdate, @watched);";

                SQLiteParameter[] eParams = { new SQLiteParameter("@eid", entry.eid),
                                              new SQLiteParameter("@aid", entry.aid),
                                              new SQLiteParameter("@epno", entry.epno),
                                              new SQLiteParameter("@spl_epno", entry.spl_epno),
                                              new SQLiteParameter("@english", entry.english),
                                              new SQLiteParameter("@nihongo", entry.nihongo),
                                              new SQLiteParameter("@romaji", entry.romaji),
                                              new SQLiteParameter("@airdate", entry.airdate),
                                              new SQLiteParameter("@watched", entry.watched) };

                eParams[2].IsNullable = eParams[3].IsNullable = eParams[4].IsNullable = eParams[5].IsNullable = true;
                if (entry.epno == 0) eParams[2].Value = null;

                cmd.Parameters.AddRange(eParams);
                cmd.ExecuteNonQuery();
            }
        }

        private void UpdateFile(FileEntry entry)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO files VALUES (@fid, @lid, @eid, @gid, @ed2k, @watcheddate, @length, @size, @source,
                                                                         @acodec, @vcodec, @vres, @path, @state, @generic);";

                SQLiteParameter[] fParams = { new SQLiteParameter("@fid", entry.fid),
                                              new SQLiteParameter("@lid", entry.lid),
                                              new SQLiteParameter("@eid", entry.eid),
                                              new SQLiteParameter("@gid", entry.Group.gid),
                                              new SQLiteParameter("@ed2k", entry.ed2k),
                                              new SQLiteParameter("@watcheddate", entry.watcheddate),
                                              new SQLiteParameter("@length", entry.length),
                                              new SQLiteParameter("@size", entry.size),
                                              new SQLiteParameter("@source", entry.source),
                                              new SQLiteParameter("@acodec", entry.acodec),
                                              new SQLiteParameter("@vcodec", entry.vcodec),
                                              new SQLiteParameter("@vres", entry.vres),
                                              new SQLiteParameter("@path", entry.path),
                                              new SQLiteParameter("@state", entry.state),
                                              new SQLiteParameter("@generic", entry.generic) };

                fParams[3].IsNullable = fParams[4].IsNullable = fParams[7].IsNullable = fParams[8].IsNullable = fParams[9].IsNullable =
                fParams[10].IsNullable = fParams[11].IsNullable = fParams[12].IsNullable = fParams[14].IsNullable = true;

                if (entry.Group.gid == 0) fParams[3].Value = null;
                if (System.IO.File.Exists(entry.path)) fParams[13].Value = entry.state = 1;
                else if (entry.state != 3) fParams[13].Value = entry.state = 0;
                if (!entry.generic) fParams[14].Value = null;

                cmd.Parameters.AddRange(fParams);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion UPDATE

        #endregion Public Methods

    }
}
