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
.controller('LobbyController', ['$scope', '$sanitize', function ($scope, $sanitize) {
    var connection = window.jQuery.connection;
    var chat = connection.chat;

    $scope.title = 'Lobby';
    $scope.privateRooms = [];
    $scope.publicRooms = [];

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
                        $scope.publicRooms.push(value);
                        $scope.$apply();
                    });
                })
                .fail(function (e) {
                    console.log('getRooms failed: ' + e);
                });
        }
    });
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
        scope: {
            rooms: '=',
            title: '='
        }
    }
});