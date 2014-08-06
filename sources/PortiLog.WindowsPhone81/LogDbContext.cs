using SQLite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace PortiLog.WindowsPhone81
{
    //public class LogDbContext : DataContext
    //{
    //    public LogDbContext(string connectionString)
    //        : base(connectionString)
    //    { }

    //    public Table<DbEntry> Entries;
    //}

    public class LogDbContext : SQLiteAsyncConnection
    {
        public LogDbContext(string dbPath, bool useTicks) : base(dbPath, useTicks)
        {
        }
        //public LogDbContext(string connectionString)
        //{
            //SQL
            //var con = new SQLiteConnection("test.db", true);
            //if(con.ta
            //await con.CreateTableAsync<Person>();
            //var people = await con.Table<Person>().ToListAsync();
        //}#
        //public SQLiteAsyncConnection Connection { get; set; }

        public static async Task<LogDbContext> CreateAsync()
        {
            var create = false;
            try
            {
                await ApplicationData.Current.LocalFolder.GetFileAsync("portilog.db");
            }
            catch (FileNotFoundException)
            {
                create = true;
            }

            LogDbContext connection = new LogDbContext("portilog.db", true);

            if (create)
            {
                await connection.CreateTableAsync<DbEntry>();
            }

            return connection;
        }
    }

    //[Table]
    public class DbEntry : INotifyPropertyChanged
    {
        int _dbEntryId;
        
        //[Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "BIGINT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int DbEntryId
        {
            get { return _dbEntryId; }
            set
            {
                if (_dbEntryId != value)
                {
                    RaisePropertyChanging("DbEntryId");
                    _dbEntryId = value;
                    RaisePropertyChanged("DbEntryId");
                }
            }
        }

        int _id;

        public int Id
        {
            get { return _id; }
            set
            {
                if (_id != value)
                {
                    RaisePropertyChanging("Id");
                    _id = value;
                    RaisePropertyChanged("Id");
                }
            }
        }

        DateTime _created;

        public DateTime Created
        {
            get { return _created; }
            set
            {
                if (_created != value)
                {
                    RaisePropertyChanging("Created");
                    _created = value;
                    RaisePropertyChanged("Created");
                }
            }
        }

        Level _level;

        public Level Level
        {
            get { return _level; }
            set
            {
                if (_level != value)
                {
                    RaisePropertyChanging("Level");
                    _level = value;
                    RaisePropertyChanged("Level");
                }
            }
        }

        string _category;

        public string Category
        {
            get { return _category; }
            set
            {
                if (_category != value)
                {
                    RaisePropertyChanging("Category");
                    _category = value;
                    RaisePropertyChanged("Category");
                }
            }
        }

        string _message;

        public string Message
        {
            get
            {
                return _message;
            }
            set
            {
                if (_message != value)
                {
                    RaisePropertyChanging("Message");
                    _message = value;
                    RaisePropertyChanged("Message");
                }
            }
        }

        bool _dumped;

        public bool Dumped
        {
            get
            {
                return _dumped;
            }
            set
            {
                if (_dumped != value)
                {
                    RaisePropertyChanging("Dumped");
                    _dumped = value;
                    RaisePropertyChanged("Dumped");
                }
            }
        }

        void RaisePropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        void RaisePropertyChanging(string propertyName)
        {
            //if (PropertyChanging != null)
            //    PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        //public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
