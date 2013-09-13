using JabbR.Client.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class MainViewModel
        : BaseViewModel
    {
        private readonly IJabbRClient _client;

        private string _userName;
        public string UserName
        {
            get { return _userName; }
            set
            {
                _userName = value;
                RaisePropertyChanged(() => UserName);
            }
        }

        private ObservableCollection<Room> _rooms;
        public ObservableCollection<Room> Rooms
        {
            get { return _rooms; }
            set
            {
                _rooms = value;
                RaisePropertyChanged(() => Rooms);
            }
        }

        public MainViewModel(IJabbRClient client)
        {
            _client = client;
        }

        public async void Init(string userName)
        {
            
            UserName = userName;
            var rooms = await _client.GetRooms();
            Rooms = new ObservableCollection<Room>(rooms);
        }
    }
}
