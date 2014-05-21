/// <reference path="../angular.js" />
/// <reference path="../angular-resource.js" />
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
.constant('jabbrUtil', { getLanguageResource: function (resource) { return window.chat.utility.getLanguageResource(resource); } })
.constant('jabbrConnection', window.jQuery.connection)
.constant('jabbrChat', window.chat)
.controller('LobbyController', ['$scope', '$sanitize', '$window', '$log', 'jabbrConnection', 'jabbrChat', function ($scope, $sanitize, $window, $log, jabbrConnection, jabbrChat) {
    var $ui = $(jabbrChat.ui);

    $scope.title = 'Lobby';
    $scope.rooms = [];
    $scope.roomSearchText = '';
    $scope.showClosedRooms = false;
    $scope.pageSize = 100;
    $scope.pagesShown = 1;

    $scope.itemsLimit = function () {
        return $scope.pageSize * $scope.pagesShown;
    };

    $scope.showMoreItems = function () {
        $scope.pagesShown++;
    }

    $scope.joinRoom = function (event, room) {
        $log.info('Joining room: ' + room.Name);
        $ui.trigger(jabbrChat.ui.events.openRoom, [room.Name]);
    };

    jabbrConnection.hub.stateChanged(function (change) {
        $log.info(change.newState);
        if (change.newState === jabbrConnection.connectionState.connected) {
            $log.info('Connected')
            jabbrConnection.chat.server.getRooms()
                .done(function (rooms) {
                    $log.info('getRooms returned: ' + rooms.length);
                    $scope.rooms = rooms;
                    $scope.$apply();
                })
                .fail(function (e) {
                    $log.error('getRooms failed: ' + e);
                });
        }
    });
}])
.controller('LobbyPublicRoomsController', ['$scope', function ($scope) {
    $scope.isPrivate = false;
    $scope.hasMoreItems = function () {
        return $scope.pagesShown < ($scope.rooms.length / $scope.pageSize);
    };

}])
.controller('LobbyPrivateRoomsController', ['$scope', function ($scope) {
    $scope.isPrivate = true;
    $scope.itemsLimit = function () {
        return $scope.rooms.length;
    };
}])
.directive('jabbrLobby', function () {
    return {
        restrict: 'A',
        templateUrl: 'Scripts/app/areas/rooms/lobby.html'
    };
})
.directive('jabbrLobbyRooms', ['$log', 'jabbrUtil', function ($log, jabbrUtil) {
    return {
        restrict: 'A',
        templateUrl: 'Scripts/app/areas/rooms/lobby-rooms.html',
        link: function ($scope, element, attrs) {
            $scope.getUserCount = function (room) {
                $log.info('getRoomUserCount');
                if (room.Count === 0) {
                    return jabbrUtil.getLanguageResource('Client_OccupantsZero');
                } else {
                    return (room.Count === 1 ? jabbrUtil.getLanguageResource('Client_OccupantsOne') : room.Count + ' ' + jabbrUtil.getLanguageResource('Client_OccupantsMany'));
                }
            };
            $scope.getTitle = function (isPrivate) {
                if (isPrivate) {
                    return jabbrUtil.getLanguageResource('Client_Rooms');
                } else {
                    return jabbrUtil.getLanguageResource('Client_OtherRooms');
                }
            };
            $scope.loadMoreTitle = jabbrUtil.getLanguageResource('Client_LoadMore');
        },
    }
}]);