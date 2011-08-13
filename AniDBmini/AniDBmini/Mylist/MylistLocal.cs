
#region Using Statements

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;

using AniDBmini.Collections;

#endregion Using Statements

namespace AniDBmini
{
    public class MylistLocal
    {

        #region Fields

        public const string dbPath = @".\data\local.db";

        private bool isSQLConnOpen;
        private SQLiteConnection SQLConn;

        public TSObservableCollection<MylistEntry> Entries = new TSObservableCollection<MylistEntry>();

        #endregion Fields

        #region Constructor

        public MylistLocal()
        {
            if (Initialize())
                PopulateEntries();
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

        public void PopulateEntries()
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT a.aid, a.type, a.title, a.nihongo, a.english, a.eps_total, a.year,
                                           IFNULL(MIN(COUNT(e.watched), a.eps_total), 0) AS eps_have, IFNULL(MIN(SUM(e.watched), a.eps_total), 0) AS eps_watched, IFNULL(SUM(f.size), 0) AS size
                                      FROM anime AS a
                                 LEFT JOIN episodes AS e ON e.aid = a.aid
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                  GROUP BY a.aid
                                  ORDER BY a.title ASC;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        MylistEntry entry = new MylistEntry(reader);
                        entry.Episodes = this.PopulateEpisodes(entry);
                        Entries.Add(entry);
                    }
            }
        }

        public TSObservableCollection<EpisodeEntry> PopulateEpisodes(MylistEntry m_entry)
        {
            TSObservableCollection<EpisodeEntry> _temp = new TSObservableCollection<EpisodeEntry>();

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT e.*, f.length AS seconds, f.generic,
                                           (SELECT CASE WHEN  generic < COUNT(eid) THEN 0 ELSE 1 END FROM files WHERE eid = e.eid) AS genericOnly
                                      FROM episodes AS e
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                     WHERE aid = @aid
                                  GROUP BY e.eid
                                  ORDER BY e.epno;";
                cmd.Parameters.AddWithValue("@aid", m_entry.aid);

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        m_entry.seconds += double.Parse(reader["seconds"].ToString());
                        EpisodeEntry e_entry = new EpisodeEntry(reader);
                        e_entry.Files = this.PopulateFiles(e_entry);
                        _temp.Add(e_entry);
                    }
            }

            return _temp;
        }

        public TSObservableCollection<FileEntry> PopulateFiles(EpisodeEntry e_entry)
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

        /// <summary>
        /// Closes the currently open databaase.
        /// </summary>
        public void Close()
        {
            if (isSQLConnOpen)
                SQLConn.Close();

            isSQLConnOpen = false;
        }

        /// <summary>
        /// Creates an empty database with correct table structures.
        /// </summary>
        public void Create() // TODO: Add foreign keys.
        {
            Initialize(true);

            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"CREATE TABLE anime ('aid' INTEGER PRIMARY KEY NOT NULL, 'type' VARCHAR NOT NULL,
                                                        'title' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'english' VARCHAR,
                                                        'year' VARCHAR NOT NULL, 'eps_total' INTEGER);

                                    CREATE TABLE episodes ('eid' INTEGER PRIMARY KEY, 'aid' INTEGER NOT NULL, 'epno' INTEGER NOT NULL,
                                                           'english' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'romaji' VARCHAR, 'airdate' INTEGER, 'watched' INTEGER);

                                    CREATE TABLE files ('lid' INTEGER PRIMARY KEY NOT NULL, 'eid' INTEGER, 'gid' INTEGER, 'ed2k' VARCHAR, 'watcheddate' INTEGER, 'length' INTEGER,
                                                        'size' INTEGER,'source' VARCHAR, 'acodec' VARCHAR, 'vcodec' VARCHAR, 'vres' VARCHAR, 'path' VARCHAR, 'generic' INTEGER);

                                    CREATE TABLE groups ('gid' INTEGER PRIMARY KEY NOT NULL, 'group_name' VARCHAR, 'group_abbr' VARCHAR);";
                cmd.ExecuteNonQuery();
            }
        }

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

                cmd.CommandText = @"INSERT OR REPLACE INTO episodes VALUES (@eid, @aid, @epno, @english, @nihongo, @romaji, @airdate, @watched);";

                SQLiteParameter[] eParams = { new SQLiteParameter("@eid", e.Episode.eid),
                                              new SQLiteParameter("@aid", e.Anime.aid),
                                              new SQLiteParameter("@epno", e.Episode.epno),
                                              new SQLiteParameter("@english", e.Episode.english),
                                              new SQLiteParameter("@nihongo", e.Episode.nihongo),
                                              new SQLiteParameter("@romaji", e.Episode.romaji),
                                              new SQLiteParameter("@airdate", e.Episode.airdate),
                                              new SQLiteParameter("@watched", e.Episode.watched) };

                eParams[4].IsNullable = eParams[5].IsNullable = true;
                cmd.Parameters.AddRange(eParams);

                cmd.ExecuteNonQuery();
                cmd.Parameters.Clear();

                cmd.CommandText = @"INSERT OR REPLACE INTO files VALUES (@lid, @eid, @gid, @ed2k, @watcheddate, @length, @size, @source, @acodec, @vcodec, @vres, @path, @generic);";

                SQLiteParameter[] fParams = { new SQLiteParameter("@lid", e.File.lid),
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

                fParams[4].IsNullable = fParams[7].IsNullable = fParams[8].IsNullable =
                fParams[9].IsNullable = fParams[10].IsNullable = fParams[11].IsNullable = true;
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
        public void InsertMylistEntryFromImport(MylistEntry entry)
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

                    cmd.CommandText = @"INSERT INTO episodes VALUES (@eid, @aid, @epno, @english, @nihongo, @romaji, @airdate, @watched);";

                    SQLiteParameter[] eParams = { new SQLiteParameter("@eid"),
                                                  new SQLiteParameter("@aid", entry.aid),
                                                  new SQLiteParameter("@epno"),
                                                  new SQLiteParameter("@english"),
                                                  new SQLiteParameter("@nihongo"),
                                                  new SQLiteParameter("@romaji"),
                                                  new SQLiteParameter("@airdate"),
                                                  new SQLiteParameter("@watched") };

                    eParams[2].IsNullable = eParams[5].IsNullable = eParams[7].IsNullable = true;
                    cmd.Parameters.AddRange(eParams);

                    using (SQLiteCommand fileCmd = SQLConn.CreateCommand())
                    {

                        fileCmd.CommandText = @"INSERT OR REPLACE INTO files VALUES (@lid, @eid, @gid, @ed2k, @watcheddate, @length, @size, @source, @acodec, @vcodec, @vres, @path, @generic);";

                        SQLiteParameter[] fParams = { new SQLiteParameter("@lid"),
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

                        fParams[2].IsNullable = fParams[3].IsNullable = fParams[4].IsNullable = fParams[5].IsNullable = fParams[6].IsNullable =
                        fParams[7].IsNullable = fParams[8].IsNullable = fParams[9].IsNullable = fParams[10].IsNullable = fParams[11].IsNullable = true;
                        fileCmd.Parameters.AddRange(fParams);

                        foreach (EpisodeEntry ep in entry.Episodes)
                        {
                            eParams[0].Value = ep.eid;
                            eParams[2].Value = ep.epno;
                            eParams[3].Value = ep.english;
                            eParams[4].Value = ep.nihongo;
                            eParams[5].Value = ep.romaji;
                            eParams[6].Value = ep.airdate;
                            eParams[7].Value = ep.watched;

                            cmd.ExecuteNonQuery();

                            fParams[1].Value = ep.eid;

                            foreach (FileEntry file in ep.Files)
                            {
                                fParams[0].Value = file.lid;
                                fParams[2].Value = file.gid;
                                fParams[3].Value = file.ed2k;
                                fParams[4].Value = file.watcheddate;
                                fParams[5].Value = file.length;
                                fParams[6].Value = file.size;
                                fParams[7].Value = file.source;
                                fParams[8].Value = file.acodec;
                                fParams[9].Value = file.vcodec;
                                fParams[10].Value = file.vres;
                                fParams[11].Value = file.path;
                                fParams[12].Value = file.generic;

                                fileCmd.ExecuteNonQuery();
                            }
                        }
                    }

                    dbTrans.Commit();
                }
            }
        }

        #endregion Public Methods

    }
}
