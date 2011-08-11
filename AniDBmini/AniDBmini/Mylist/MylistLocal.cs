
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

        private void PopulateEntries()
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"SELECT a.aid, a.type, a.title, a.nihongo, a.english, a.eps_total, a.eps_my,
                                           a.eps_watched, IFNULL(SUM(f.size), 0) AS size
                                      FROM anime AS a
                                 LEFT JOIN episodes AS e ON e.aid = a.aid
                                 LEFT JOIN files AS f ON f.eid = e.eid
                                  GROUP BY a.aid
                                  ORDER BY a.title ASC;";

                using (SQLiteDataReader reader = cmd.ExecuteReader())
                    while (reader.Read())
                        Entries.Add(new MylistEntry(reader));
            }
        }

        #endregion Private Methods

        #region Public Methods

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
                                                        'startdate' VARCHAR NOT NULL, 'enddate' VARCHAR, 'eps_total' INTEGER, 
                                                        'eps_my' INTEGER, 'eps_watched' INTEGER, 'complete' INTEGER, 'watched' INTEGER, 'size' INTEGER);
                                    CREATE TABLE episodes ('eid' INTEGER PRIMARY KEY, 'aid' INTEGER NOT NULL, 'epno' INTEGER NOT NULL, 'type' VARCHAR,
                                                           'english' VARCHAR NOT NULL, 'nihongo' VARCHAR, 'romaji' VARCHAR, 'airdate' VARCHAR, 'watched' INTEGER);
                                    CREATE TABLE files ('lid' INTEGER PRIMARY KEY NOT NULL, 'eid' INTEGER, 'gid' INTEGER, 'ed2k' VARCHAR, 'addeddate' VARCHAR, 'watcheddate' VARCHAR, 
                                                        'length' INTEGER, 'size' INTEGER, 'source' VARCHAR, 'vcodec' VARCHAR, 'acodec' VARCHAR, 'path' VARCHAR, 'generic' INTEGER);
                                    CREATE TABLE groups ('gid' INTEGER PRIMARY KEY NOT NULL, 'name' VARCHAR, 'abbr' VARCHAR);";
                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert a fansub group into the database.
        /// </summary>
        public void InsertGroup(int gid, string name, string abbr)
        {
            using (SQLiteCommand cmd = new SQLiteCommand(SQLConn))
            {
                cmd.CommandText = @"INSERT INTO groups VALUES (@gid, @g_name, @g_abbr);";

                cmd.Parameters.AddWithValue("@gid", gid);
                cmd.Parameters.AddWithValue("@g_name", name);
                cmd.Parameters.AddWithValue("@g_abbr", abbr);

                cmd.ExecuteNonQuery();
            }
        }

        /// <summary>
        /// Insert a mylist entry into the database.
        /// </summary>
        public void InsertMylistEntry(MylistEntry entry)
        {
            Entries.Add(entry);

            using (SQLiteTransaction dbTrans = SQLConn.BeginTransaction())
            {
                using (SQLiteCommand cmd = SQLConn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO anime VALUES (@aid, @type, @title, @nihongo, @english, @startdate, @enddate,
                                                            @eps_total, @eps_my, @eps_watched, @complete, @watched, @size);";

                    cmd.Parameters.AddWithValue("@aid", entry.aid);
                    cmd.Parameters.AddWithValue("@type", entry.type);
                    cmd.Parameters.AddWithValue("@title", entry.title);
                    cmd.Parameters.AddWithValue("@startdate", entry.startdate);
                    cmd.Parameters.AddWithValue("@eps_total", entry.eps_total);
                    cmd.Parameters.AddWithValue("@eps_my", entry.eps_my);
                    cmd.Parameters.AddWithValue("@eps_watched", entry.eps_watched);
                    cmd.Parameters.AddWithValue("@complete", entry.complete);
                    cmd.Parameters.AddWithValue("@watched", entry.watched);
                    cmd.Parameters.AddWithValue("@size", entry.size);

                    SQLiteParameter[] aParams = { new SQLiteParameter("@english", entry.english),
                                                  new SQLiteParameter("@nihongo", entry.nihongo),
                                                  new SQLiteParameter("@enddate", entry.enddate) };
                    aParams[0].IsNullable = aParams[1].IsNullable = aParams[2].IsNullable = true;
                    cmd.Parameters.AddRange(aParams);

                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"INSERT INTO episodes VALUES (@eid, @aid, @epno, @type, @english, @nihongo, @romaji, @airdate, @watched);";

                    SQLiteParameter[] eParams = { new SQLiteParameter("@eid"),
                                                  new SQLiteParameter("@aid", entry.aid),
                                                  new SQLiteParameter("@epno"),
                                                  new SQLiteParameter("@type"),
                                                  new SQLiteParameter("@english"),
                                                  new SQLiteParameter("@nihongo"),
                                                  new SQLiteParameter("@romaji"),
                                                  new SQLiteParameter("@airdate"),
                                                  new SQLiteParameter("@watched") };

                    eParams[2].IsNullable = eParams[5].IsNullable = eParams[7].IsNullable = true;
                    cmd.Parameters.AddRange(eParams);

                    SQLiteCommand fileCmd = SQLConn.CreateCommand();

                    fileCmd.CommandText = @"INSERT INTO files VALUES (@lid, @eid, @gid, @ed2k, @addeddate, @watcheddate, @length, @size, @source, @vcodec, @acodec, @path, @generic);";

                    SQLiteParameter[] fParams = { new SQLiteParameter("@lid"),
                                                  new SQLiteParameter("@eid"),
                                                  new SQLiteParameter("@gid"),
                                                  new SQLiteParameter("@ed2k"),
                                                  new SQLiteParameter("@addeddate"),
                                                  new SQLiteParameter("@watcheddate"),
                                                  new SQLiteParameter("@length"),
                                                  new SQLiteParameter("@size"),
                                                  new SQLiteParameter("@source"),
                                                  new SQLiteParameter("@vcodec"),
                                                  new SQLiteParameter("@acodec"),
                                                  new SQLiteParameter("@path"),
                                                  new SQLiteParameter("@generic") };

                    fParams[2].IsNullable = fParams[3].IsNullable = fParams[5].IsNullable = fParams[6].IsNullable = 
                    fParams[7].IsNullable = fParams[8].IsNullable = fParams[9].IsNullable = fParams[10].IsNullable = true;
                    fileCmd.Parameters.AddRange(fParams);

                    foreach (EpisodeEntry ep in entry.Episodes)
                    {
                        eParams[0].Value = ep.eid;
                        eParams[2].Value = ep.epno;
                        eParams[3].Value = ep.type;
                        eParams[4].Value = ep.english;
                        eParams[5].Value = ep.nihongo;
                        eParams[6].Value = ep.romaji;
                        eParams[7].Value = ep.airdate;
                        eParams[8].Value = ep.watched;

                        cmd.ExecuteNonQuery();

                        fParams[1].Value = ep.eid;

                        foreach (FileEntry file in ep.Files)
                        {
                            fParams[0].Value = file.lid;
                            fParams[2].Value = file.gid;
                            fParams[3].Value = file.ed2k;
                            fParams[4].Value = file.addeddate;
                            fParams[5].Value = file.watcheddate;
                            fParams[6].Value = file.length;
                            fParams[7].Value = file.size;
                            fParams[8].Value = file.source;
                            fParams[9].Value = file.vcodec;
                            fParams[10].Value = file.acodec;
                            fParams[11].Value = file.path;
                            fParams[12].Value = file.generic;

                            fileCmd.ExecuteNonQuery();
                        }
                    }

                    dbTrans.Commit();
                }
            }
        }

        #endregion Public Methods

    }
}
