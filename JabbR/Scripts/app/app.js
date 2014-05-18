/// <reference path="../angular.js" />
/// <reference path="../angular-resource.js" />

// Ensure calls to console.log don't break IE
if (typeof console === "undefined" || typeof console.log === "undefined") {
    console = {};
    console.log = function () { };
}

var app = angular.module('jabbrApp', [
    'ngRoute',
    'ngResource',
    'ngSanitize'
])
.config(function ($routeProvider) {
    $routeProvider.when('/rooms/lobby', {
        templateUrl: 'areas/rooms/lobby.html',
        title: 'Lobby',
        controller: 'LobbyController'
    });
})
.controller('LobbyController', ['$scope', '$sanitize', '$window', function ($scope, $sanitize, $window) {
    var connection = $window.jQuery.connection;
    var chat = connection.chat;
    var ui = $window.chat.ui;
    var $ui = $(ui);

    $scope.title = 'Lobby';
    $scope.privateRooms = [];
    $scope.publicRooms = [];

    $scope.joinRoom = function (event, room) {
        console.log('Joining room: ' + room.Name);
        $ui.trigger(ui.events.openRoom, [room.Name]);
    };

    connection.hub.stateChanged(function (change) {
        console.log(change.newState);
        if (change.newState === connection.connectionState.connected) {
            console.log('Connected')
            chat.server.getRooms()
                .done(function (rooms) {
                    console.log('getRooms');
                    console.log(rooms.length);
                    angular.forEach(rooms, function (value, key) {
                        console.log(value);
                        value.getUserCount = function () {
                            if (this.Count === 0)
                                return $window.chat.utility.getLanguageResource('Client_OccupantsZero');
                            else
                                return (this.Count === 1 ? $window.chat.utility.getLanguageResource('Client_OccupantsOne') : this.Count + ' ' + $window.chat.utility.getLanguageResource('Client_OccupantsMany'));
                        }
                        if (value.Private) {
                            $scope.privateRooms.push(value);
                        }
                        else {
                            $scope.publicRooms.push(value);
                        }
                        $scope.$apply();
                    });
                })
                .fail(function (e) {
                    console.log('getRooms failed: ' + e);
                });
        }
    });
}])
.controller('LobbyPublicRoomsController', ['$scope', '$window', function($scope, $window) {
    $scope.rooms = $scope.$parent.publicRooms;
    $scope.title = $window.chat.utility.getLanguageResource('Client_OtherRooms');
    $scope.loadMoreTitle = $window.chat.utility.getLanguageResource('Client_LoadMore');
}])
.controller('LobbyPrivateRoomsController', ['$scope', '$window', function ($scope, $window) {
    $scope.rooms = $scope.$parent.privateRooms;
    $scope.title = $window.chat.utility.getLanguageResource('Client_Rooms');
}])
.directive('jabbrLobby', function () {
    return {
        restrict: 'A',
        templateUrl: 'Scripts/app/areas/rooms/lobby.html'
    };
})
.directive('jabbrLobbyRooms', function () {
    return {
        restrict: 'A',
        templateUrl: 'Scripts/app/areas/rooms/lobby-rooms.html',
    }
});