/// <reference path="../angular.js" />
/// <reference path="../angular-resource.js" />
var jabbrService = {
    getLanguageResource: function (resource) {
        return window.chat.utility.getLanguageResource(resource);
    },
    connectionState: window.jQuery.connection.connectionState,
    hub: window.jQuery.connection.hub,
    chat: window.jQuery.connection.chat,
    server: window.jQuery.connection.chat.server,
    ui: window.chat.ui,
    events: window.chat.ui.events
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
.constant('jabbrService', jabbrService)
.controller('LobbyController', ['$scope', '$sanitize', '$window', '$log', 'jabbrService', function ($scope, $sanitize, $window, $log, jabbrService) {
    var $ui = $(jabbrService.ui);

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
        $ui.trigger(jabbrService.events.openRoom, [room.Name]);
    };

    jabbrService.hub.stateChanged(function (change) {
        $log.info(change.newState);
        if (change.newState === jabbrService.connectionState.connected) {
            $log.info('Connected')
            jabbrService.server.getRooms()
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
.directive('jabbrLobbyRooms', ['$log', 'jabbrService', function ($log, jabbrService) {
    return {
        restrict: 'A',
        templateUrl: 'Scripts/app/areas/rooms/lobby-rooms.html',
        link: function ($scope, element, attrs) {
            $scope.getUserCount = function (room) {
                $log.info('getRoomUserCount');
                if (room.Count === 0) {
                    return jabbrService.getLanguageResource('Client_OccupantsZero');
                } else {
                    return (room.Count === 1 ? jabbrService.getLanguageResource('Client_OccupantsOne') : room.Count + ' ' + jabbrService.getLanguageResource('Client_OccupantsMany'));
                }
            };
            $scope.getTitle = function (isPrivate) {
                if (isPrivate) {
                    return jabbrService.getLanguageResource('Client_Rooms');
                } else {
                    return jabbrService.getLanguageResource('Client_OtherRooms');
                }
            };
            $scope.loadMoreTitle = jabbrService.getLanguageResource('Client_LoadMore');
        },
    }
}]);