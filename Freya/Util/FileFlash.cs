using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Freya.Util
{
    public class FileFlash : INotifyPropertyChanged
    {
        [field: NonSerialized()]
        private string _filename;
        public string FileName
        {
            set
            {
                _filename = value;
                OnPropertyChanged("FileName");
            }
            get
            {
                return _filename;
            }
        }

        [field: NonSerialized()]
        private bool _enable;
        public bool Enable
        {
            set
            {
                _enable = value;
                OnPropertyChanged("Enable");
            }
            get
            {
                return _enable;
            }
        }

        [field: NonSerialized()]
        private string _filepath;
        public string FilePath
        {
            set
            {
                _filepath = value;
                OnPropertyChanged("FilePath");
            }
            get
            {
                return _filepath;
            }
        }

        [field: NonSerialized()]
        private long _rawsize;
        public long RawSize
        {
            set
            {
                _rawsize = value;
                OnPropertyChanged("RawSize");
            }
            get
            {
                return _rawsize;
            }
        }

        [field: NonSerialized()]
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
