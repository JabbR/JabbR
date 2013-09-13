using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class MainViewModel
        : BaseViewModel
    {
        private readonly IJabbRClient _client;

        private User _currentUser;
        public User CurrentUser
        {
            get { return _currentUser; }
            set
            {
                _currentUser = value;
                RaisePropertyChanged(() => CurrentUser);
            }
        }

        private ObservableCollection<Room> _currentRooms;
        public ObservableCollection<Room> CurrentRooms
        {
            get { return _currentRooms; }
            set
            {
                _currentRooms = value;
                RaisePropertyChanged(() => CurrentRooms);
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

        public async void Init(string userJson, string roomsJson)
        {
            CurrentUser = JsonConvert.DeserializeObject<User>(userJson);
            CurrentRooms = new ObservableCollection<Room>(JsonConvert.DeserializeObject<IEnumerable<Room>>(roomsJson));
            var rooms = await _client.GetRooms();
            Rooms = new ObservableCollection<Room>(rooms);
        }

        private ICommand _openRoomCommand;
        public ICommand OpenRoomCommand
        {
            get { return _openRoomCommand ?? new MvxCommand<Room>(DoOpenRoom); }
        }

        private ICommand _joinRoomCommand;
        public ICommand JoinRoomCommand
        {
            get { return _joinRoomCommand ?? new MvxCommand<Room>(DoJoinRoom); }
        }

        private ICommand _signOutCommand;
        public ICommand SignOutCommand
        {
            get { return _signOutCommand ?? new MvxCommand(DoSignOut); }
        }

        private void DoOpenRoom(Room room)
        {
            ShowViewModel<RoomViewModel>(new { roomName = room.Name });
        }

        private void DoJoinRoom(Room room)
        {
            _client.JoinRoom(room.Name);
            ShowViewModel<RoomViewModel>(new { roomJson = JsonConvert.SerializeObject(room) });
        }

        private void DoSignOut()
        {
            _client.LogOut();
            RequestClose();
        }
    }
}
