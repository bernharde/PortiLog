using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortiLog.WindowsPhone
{
    public class LogDbContext : DataContext
    {
        public LogDbContext(string connectionString)
            : base(connectionString)
        { }

        public Table<DbEntry> Entries;
    }

    [Table]
    public class DbEntry : INotifyPropertyChanged, INotifyPropertyChanging
    {
        long _dbEntryId;

        [Column(IsPrimaryKey = true, IsDbGenerated = true, DbType = "BIGINT NOT NULL Identity", CanBeNull = false, AutoSync = AutoSync.OnInsert)]
        public long DbEntryId
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

        [Column]
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

        [Column]
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

        [Column]
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

        [Column]
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

        [Column]
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

        [Column]
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
            if (PropertyChanging != null)
                PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        public event PropertyChangingEventHandler PropertyChanging;

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
