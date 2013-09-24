using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Cirrious.MvvmCross.ViewModels;
using JabbR.Client.Models;
using JabbR.Client.UI.Core.Interfaces;

namespace JabbR.Client.UI.Core.ViewModels
{
    public class MainViewModel
        : BaseViewModel
    {
        public class NavigationParameters : NavigationParametersBase
        {
            public User CurrentUser { get; set; }
            public IEnumerable<Room> Rooms { get; set; }
        }

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

        public MainViewModel(IJabbRClient client, IGlobalProgressIndicator progress)
            : base(progress)
        {
            _client = client;
        }

        public async void Init(NavigationParameters parameters)
        {
            CurrentUser = parameters.CurrentUser;
            CurrentRooms = new ObservableCollection<Room>(parameters.Rooms);
            try
            {
                Progress.SetStatus("Loading...", true);
                var rooms = await _client.GetRooms();
                Rooms = new ObservableCollection<Room>(rooms);
            }
            catch (Exception ex)
            {

            }
            finally
            {
                Progress.ClearStatus();
            }
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
            ShowViewModel<RoomViewModel>(new RoomViewModel.NavigationParameter { RoomName = room.Name });
        }

        private async void DoJoinRoom(Room room)
        {
            await _client.JoinRoom(room.Name);
            DoOpenRoom(room);
        }

        private void DoSignOut()
        {
            Deactivate();
            RequestClose();
        }

        public override void Deactivate()
        {
            _client.LogOut();
        }
    }
}
